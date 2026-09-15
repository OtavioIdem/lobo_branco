using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace LoboBranco.Net
{
    /// <summary>
    /// Autoridade: nenhuma. Este componente so abre e fecha a sessao. Quem decide
    /// qualquer coisa de jogo e o host, conforme a ADR 0008.
    ///
    /// O transporte e direto por IP de proposito. O risco X8 do doc 13 diz que depender
    /// da nuvem para testar trava o desenvolvimento, entao a rede tem que subir sem
    /// servico nenhum. O Relay da tarefa 1.9h entra por cima deste mesmo transporte e
    /// nao encosta em nenhum sistema de jogo.
    ///
    /// Duas instancias na mesma maquina: a principal sobe como host sozinha, e a virtual
    /// do Multiplayer Play Mode recebe <c>-lb-client</c> nos argumentos e entra como
    /// cliente. Sem isso, as duas subiriam como host e nenhuma veria a outra.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkManager))]
    public sealed class NetLauncher : MonoBehaviour
    {
        [Header("Conexao direta")]
        [Tooltip("Endereco do host. 127.0.0.1 para duas instancias na mesma maquina.")]
        [SerializeField] string address = "127.0.0.1";

        [SerializeField] ushort port = 7777;

        [Header("Inicio automatico")]
        [Tooltip("Sobe como host ao entrar em Play, a menos que -lb-client venha na linha de comando.")]
        [SerializeField] bool autoStart = true;

        const string ArgHost = "-lb-host";
        const string ArgClient = "-lb-client";
        const string ArgAddress = "-lb-address";
        const string ArgPort = "-lb-port";

        NetworkManager _net;

        // ---------------------------------------------------------------- leitura

        public string Address
        {
            get => address;
            set => address = value;
        }

        public ushort Port
        {
            get => port;
            set => port = value;
        }

        /// <summary>Ultima falha de conexao, para o painel de debug mostrar. Vazio quando nao houve.</summary>
        public string LastError { get; private set; } = string.Empty;

        // ------------------------------------------------------------------ ciclo

        void Awake()
        {
            _net = GetComponent<NetworkManager>();
            ReadCommandLine();
        }

        void Start()
        {
            if (!autoStart || _net == null || _net.IsListening) return;

            if (WantsClient()) StartClient();
            else StartHost();
        }

        // ------------------------------------------------------------------ acoes

        public void StartHost()
        {
            if (!Prepare()) return;

            if (!_net.StartHost())
                Fail("Falha ao subir como host. A porta ja pode estar ocupada por outra instancia.");
        }

        public void StartClient()
        {
            if (!Prepare()) return;

            if (!_net.StartClient())
                Fail($"Falha ao conectar em {address}:{port}.");
        }

        public void Shutdown()
        {
            if (_net != null && _net.IsListening)
                _net.Shutdown();
        }

        // ---------------------------------------------------------------- interno

        /// <summary>
        /// Escreve endereco e porta no transporte. Precisa acontecer antes de cada
        /// <c>Start</c>, e nao so uma vez no Awake, porque o painel de debug deixa
        /// trocar o endereco depois de uma tentativa que falhou.
        /// </summary>
        bool Prepare()
        {
            if (_net == null)
            {
                Fail($"{nameof(NetLauncher)} sem {nameof(NetworkManager)} no mesmo objeto.");
                return false;
            }

            if (_net.NetworkConfig.NetworkTransport is not UnityTransport transport)
            {
                Fail("O transporte configurado nao e UnityTransport. Ver ADR 0008.");
                return false;
            }

            transport.SetConnectionData(address, port);
            LastError = string.Empty;
            return true;
        }

        void Fail(string message)
        {
            LastError = message;
            Debug.LogError($"[Rede] {message}", this);
        }

        bool WantsClient()
        {
            string[] args = Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], ArgClient, StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(args[i], ArgHost, StringComparison.OrdinalIgnoreCase)) return false;
            }

            return false;
        }

        void ReadCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], ArgAddress, StringComparison.OrdinalIgnoreCase))
                    address = args[i + 1];

                else if (string.Equals(args[i], ArgPort, StringComparison.OrdinalIgnoreCase) &&
                         ushort.TryParse(args[i + 1], out ushort parsed))
                    port = parsed;
            }
        }
    }
}
