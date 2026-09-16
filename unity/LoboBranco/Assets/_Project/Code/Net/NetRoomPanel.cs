using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LoboBranco.Net
{
    /// <summary>
    /// Autoridade: nenhuma. Botao nao decide nada, so chama quem decide.
    ///
    /// A sala: hospedar, mandar o codigo para os amigos, colar codigo, entrar, e ver quem
    /// ja esta dentro. E a menor coisa que fecha o ciclo de convite do doc 13 secao 8.
    ///
    /// IMGUI, e de proposito. A UI de verdade e UI Toolkit e comeca na tarefa 2.3, com o
    /// inventario, que e onde vale a pena montar a fundacao de UXML e USS. Fazer a
    /// primeira tela de UI Toolkit ser uma sala provisoria, antes do portao M1, seria
    /// construir fundacao para uma tela que o menu principal da tarefa 4.12 vai substituir.
    /// Enquanto isso, esta aqui custa zero e desaparece com um F3.
    ///
    /// A divisao com o <see cref="NetDebugHud"/> e simples: a sala e como se entra, o
    /// painel e o que esta acontecendo na rede depois que se entrou.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkManager))]
    public sealed class NetRoomPanel : MonoBehaviour
    {
        [Tooltip("F3 alterna a sala em tempo de execucao.")]
        [SerializeField] bool visible = true;

        [SerializeField] NetLauncher launcher;
        [SerializeField] NetRelaySession relay;

        const int JoinCodeLength = 6;

        NetworkManager _net;
        GUIStyle _style;
        GUIStyle _codeStyle;
        string _codeInput = string.Empty;
        string _copied = string.Empty;

        readonly StringBuilder _roster = new StringBuilder(256);

        void Awake()
        {
            _net = GetComponent<NetworkManager>();

            if (launcher == null) launcher = GetComponent<NetLauncher>();
            if (relay == null) relay = GetComponent<NetRelaySession>();
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
                visible = !visible;
        }

        void OnGUI()
        {
            if (!visible || _net == null) return;

            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 13 };
            _codeStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };

            GUILayout.BeginArea(new Rect(Screen.width * 0.5f - 190f, 10f, 380f, 300f), GUI.skin.box);
            GUILayout.Label("SALA  —  F3 esconde", _style);
            GUILayout.Space(4f);

            if (_net.IsListening) DrawInsideRoom();
            else DrawDoor();

            DrawError();

            GUILayout.EndArea();
        }

        // ------------------------------------------------------------------ fora

        void DrawDoor()
        {
            bool busy = relay != null && relay.IsBusy;

            GUI.enabled = !busy;

            GUILayout.Label("Jogar com amigos", _style);

            if (relay != null)
            {
                if (GUILayout.Button(busy ? "Falando com o Relay..." : "Hospedar e gerar codigo"))
                    _ = relay.HostAsync();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Codigo", _style, GUILayout.Width(50f));

                // Maiusculo na entrada porque o codigo do Relay e maiusculo, e quem digita
                // minusculo levaria uma recusa sem entender o motivo.
                _codeInput = GUILayout.TextField(_codeInput, JoinCodeLength).ToUpperInvariant();

                if (GUILayout.Button("Entrar", GUILayout.Width(70f)))
                    _ = relay.JoinAsync(_codeInput);

                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label($"Sem {nameof(NetRelaySession)} no objeto: so IP direto.", _style);
            }

            GUILayout.Space(8f);
            GUILayout.Label("Na mesma maquina ou na mesma rede", _style);

            GUILayout.BeginHorizontal();
            GUILayout.Label("IP", _style, GUILayout.Width(50f));

            if (launcher != null)
                launcher.Address = GUILayout.TextField(launcher.Address);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Hospedar")) launcher?.StartHost();
            if (GUILayout.Button("Entrar")) launcher?.StartClient();
            GUILayout.EndHorizontal();

            GUI.enabled = true;
        }

        // ---------------------------------------------------------------- dentro

        void DrawInsideRoom()
        {
            string code = relay != null ? relay.JoinCode : string.Empty;

            if (code.Length > 0)
            {
                GUILayout.Label("Codigo do convite", _style);

                GUILayout.BeginHorizontal();
                GUILayout.Label(code, _codeStyle);

                if (GUILayout.Button("Copiar", GUILayout.Width(70f), GUILayout.Height(30f)))
                {
                    GUIUtility.systemCopyBuffer = code;
                    _copied = "Codigo copiado. Manda para quem vai jogar.";
                }

                GUILayout.EndHorizontal();
            }
            else
            {
                // Sessao por IP direto nao tem codigo, e dizer isso evita a pergunta.
                GUILayout.Label("Sessao por IP direto, sem codigo de convite.", _style);
            }

            if (_copied.Length > 0)
                GUILayout.Label(_copied, _style);

            GUILayout.Space(6f);
            GUILayout.Label(DescribeRoster(), _style);

            if (GUILayout.Button("Sair da sessao"))
            {
                _copied = string.Empty;

                if (relay != null) relay.Shutdown();
                else launcher?.Shutdown();
            }
        }

        /// <summary>
        /// Quem esta dentro. A lista de participantes chega nos clientes tambem, entao
        /// todo mundo ve a sala inteira e nao so o host.
        /// </summary>
        string DescribeRoster()
        {
            _roster.Clear();
            _roster.Append("Na sala: ").Append(_net.ConnectedClientsIds.Count).Append('\n');

            foreach (ulong clientId in _net.ConnectedClientsIds)
            {
                _roster.Append("  bruxo ").Append(clientId);

                if (clientId == NetworkManager.ServerClientId) _roster.Append("  (host)");
                if (clientId == _net.LocalClientId) _roster.Append("  (voce)");

                _roster.Append('\n');
            }

            if (!_net.IsConnectedClient && !_net.IsServer)
                _roster.Append("  conectando...\n");

            return _roster.ToString();
        }

        void DrawError()
        {
            string error = relay != null && relay.LastError.Length > 0
                ? relay.LastError
                : launcher != null ? launcher.LastError : string.Empty;

            if (string.IsNullOrEmpty(error)) return;

            GUILayout.Space(6f);
            GUILayout.Label(error, _style);
        }
    }
}
