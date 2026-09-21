using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace LoboBranco.Net
{
    /// <summary>
    /// Autoridade: nenhuma. Este componente so abre a porta da sessao; quem decide
    /// qualquer coisa de jogo continua sendo o host, conforme a ADR 0008.
    ///
    /// O caminho de convite: o host cria uma alocacao no Relay, recebe um codigo de seis
    /// caracteres e manda esse codigo para os amigos por fora do jogo. Quem entra troca o
    /// codigo por um endereco de servidor de retransmissao. Ninguem precisa saber o IP de
    /// ninguem e ninguem precisa abrir porta no roteador, que era o pedido original.
    ///
    /// Ele fica ao lado do <see cref="NetLauncher"/>, e nao dentro dele, porque os dois sao
    /// caminhos alternativos para a mesma sessao: o IP direto e o do dia a dia, que sobe
    /// sem nuvem nenhuma, e este e o de jogar com gente de fora. O risco X8 do doc 13 diz
    /// que a rede nao pode depender de servico de nuvem para ser testada, e manter os dois
    /// separados e o que garante isso: se o Relay estiver fora do ar, o IP direto continua.
    ///
    /// Tudo aqui e assincrono porque toda chamada e uma viagem ate a nuvem. Erro nao
    /// derruba nada: ele vira texto em <see cref="LastError"/>, porque o unico jeito de
    /// depurar uma falha de servico e ler o que o servico respondeu.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkManager))]
    public sealed class NetRelaySession : MonoBehaviour
    {
        /// <summary>Em que pe esta a conversa com a nuvem. A sala mostra isto.</summary>
        public enum Phase
        {
            Idle = 0,
            Working = 1,
            Ready = 2,
            Failed = 3,
        }

        [Tooltip("Prefere DTLS, que e cifrado. Desligue so para depurar com captura de pacote.")]
        [SerializeField] bool preferDtls = true;

        [Tooltip("Teto de participantes quando nao ha NetSpawnRing por perto. Doc 13 secao 1: de 2 a 4.")]
        [SerializeField] int fallbackMaxPlayers = 4;

        NetworkManager _net;
        NetSpawnRing _spawnRing;

        // ---------------------------------------------------------------- leitura

        /// <summary>Codigo de convite da sessao corrente. Vazio quando nao hospedamos por Relay.</summary>
        public string JoinCode { get; private set; } = string.Empty;

        public Phase Status { get; private set; } = Phase.Idle;

        /// <summary>Ultima falha, em portugues e ja explicada. Vazio quando nao houve.</summary>
        public string LastError { get; private set; } = string.Empty;

        /// <summary>Verdadeiro enquanto uma chamada a nuvem esta em voo. A sala desabilita os botoes.</summary>
        public bool IsBusy => Status == Phase.Working;

        // ------------------------------------------------------------------ ciclo

        void Awake()
        {
            _net = GetComponent<NetworkManager>();
            _spawnRing = GetComponent<NetSpawnRing>();
        }

        // ------------------------------------------------------------------ acoes

        /// <summary>
        /// Cria a sessao e devolve o codigo de convite em <see cref="JoinCode"/>.
        /// O host conta como participante, mas nao como conexao: a alocacao pede o numero
        /// de gente que vai entrar, e nao o tamanho da sala.
        /// </summary>
        public async Task<bool> HostAsync()
        {
            if (!CanStart()) return false;

            Begin();

            try
            {
                await SignInAsync();

                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers - 1);
                string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                if (this == null) return false;

                RelayServerEndpoint endpoint = SelectEndpoint(allocation.ServerEndpoints);
                if (endpoint == null) return Fail("O Relay nao devolveu endereco de servidor.");

                if (!TryGetTransport(out UnityTransport transport)) return false;

                transport.SetRelayServerData(
                    endpoint.Host,
                    (ushort)endpoint.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData,
                    hostConnectionDataBytes: null,
                    isSecure: endpoint.Secure);

                if (!_net.StartHost())
                    return Fail("O Relay respondeu, mas o host nao subiu. Ver o Console.");

                JoinCode = code;
                Status = Phase.Ready;
                return true;
            }
            catch (Exception exception)
            {
                return Fail(Explain(exception));
            }
        }

        /// <summary>Entra em uma sessao existente pelo codigo de convite.</summary>
        public async Task<bool> JoinAsync(string joinCode)
        {
            if (!CanStart()) return false;

            if (string.IsNullOrWhiteSpace(joinCode))
                return Fail("Sem codigo de convite nao da para entrar.");

            Begin();

            try
            {
                await SignInAsync();

                JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim());

                if (this == null) return false;

                RelayServerEndpoint endpoint = SelectEndpoint(allocation.ServerEndpoints);
                if (endpoint == null) return Fail("O Relay nao devolveu endereco de servidor.");

                if (!TryGetTransport(out UnityTransport transport)) return false;

                // A diferenca para o host e a ultima linha: quem entra precisa saber com
                // quem falar do outro lado da retransmissao.
                transport.SetRelayServerData(
                    endpoint.Host,
                    (ushort)endpoint.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData,
                    allocation.HostConnectionData,
                    endpoint.Secure);

                if (!_net.StartClient())
                    return Fail("O codigo foi aceito, mas a conexao nao subiu. Ver o Console.");

                JoinCode = joinCode.Trim().ToUpperInvariant();
                Status = Phase.Ready;
                return true;
            }
            catch (Exception exception)
            {
                return Fail(Explain(exception));
            }
        }

        /// <summary>Encerra a sessao e esquece o codigo. O <see cref="NetLauncher"/> faz o resto.</summary>
        public void Shutdown()
        {
            JoinCode = string.Empty;
            Status = Phase.Idle;
            LastError = string.Empty;

            if (_net != null && _net.IsListening)
                _net.Shutdown();
        }

        // ---------------------------------------------------------------- interno

        int MaxPlayers => _spawnRing != null ? _spawnRing.MaxPlayers : fallbackMaxPlayers;

        /// <summary>
        /// Autenticacao anonima: o Relay exige um jogador identificado, e sessao privada
        /// entre amigos nao precisa de conta. Quem ja entrou nao entra de novo, senao cada
        /// tentativa de hospedar viraria uma viagem a mais ate a nuvem.
        /// </summary>
        static async Task SignInAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        bool CanStart()
        {
            if (_net == null)
                return Fail($"{nameof(NetRelaySession)} sem {nameof(NetworkManager)} no mesmo objeto.");

            if (_net.IsListening)
                return Fail("Ja existe sessao aberta. Saia dela antes de abrir outra.");

            // O erro de projeto nao ligado chega da nuvem como uma excecao generica de
            // configuracao, e ninguem descobre por ela o que fazer. Perguntar antes custa
            // nada e devolve a instrucao em vez do sintoma.
            if (string.IsNullOrEmpty(Application.cloudProjectId))
                return Fail(
                    "Este projeto nao esta ligado ao Unity Gaming Services, entao o Relay nao " +
                    "responde. Ligue em Project Settings > Services, ou usa o IP direto.");

            return true;
        }

        void Begin()
        {
            Status = Phase.Working;
            LastError = string.Empty;
            JoinCode = string.Empty;
        }

        bool Fail(string message)
        {
            Status = Phase.Failed;
            LastError = message;
            Debug.LogError($"[Relay] {message}", this);
            return false;
        }

        bool TryGetTransport(out UnityTransport transport)
        {
            transport = _net.NetworkConfig.NetworkTransport as UnityTransport;

            if (transport != null) return true;

            Fail("O transporte configurado nao e UnityTransport. Ver ADR 0008.");
            return false;
        }

        /// <summary>
        /// DTLS quando existe, UDP quando nao. O Relay lista mais de um endereco por
        /// alocacao e pegar o primeiro da lista funciona ate o dia em que a ordem muda.
        /// </summary>
        RelayServerEndpoint SelectEndpoint(List<RelayServerEndpoint> endpoints)
        {
            if (endpoints == null || endpoints.Count == 0) return null;

            RelayServerEndpoint udp = null;

            for (int i = 0; i < endpoints.Count; i++)
            {
                RelayServerEndpoint endpoint = endpoints[i];

                if (preferDtls && endpoint.ConnectionType == RelayServerEndpoint.ConnectionTypeDtls)
                    return endpoint;

                if (endpoint.ConnectionType == RelayServerEndpoint.ConnectionTypeUdp)
                    udp = endpoint;
            }

            return udp ?? endpoints[0];
        }

        /// <summary>
        /// Traduz a falha para algo que diga o que fazer. As tres primeiras cobrem quase
        /// tudo que acontece de verdade: sem internet, codigo errado, projeto sem servico.
        /// </summary>
        static string Explain(Exception exception)
        {
            return exception switch
            {
                RelayServiceException relay =>
                    $"O Relay recusou: {relay.Reason}. Codigo errado ou sessao ja encerrada.",
                AuthenticationException auth =>
                    $"A autenticacao anonima falhou: {auth.Message}",
                RequestFailedException request =>
                    $"O servico respondeu com erro {request.ErrorCode}: {request.Message}",
                _ => $"Falha inesperada ao falar com a nuvem: {exception.Message}",
            };
        }
    }
}
