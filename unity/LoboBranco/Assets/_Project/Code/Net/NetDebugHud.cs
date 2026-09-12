using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LoboBranco.Net
{
    /// <summary>
    /// Autoridade: nenhuma. So le e mostra.
    ///
    /// O doc 13 secao 11, risco X10, pede isto desde o primeiro dia: sem ver papel, dono
    /// e ida-e-volta na tela, erro de rede se disfarca de bug de jogabilidade e o tempo
    /// vai embora procurando no lugar errado.
    ///
    /// IMGUI de proposito, pelo mesmo motivo do <c>PlayerDebugOverlay</c>: custo zero e
    /// nao concorre com a UI de verdade. Nao vai para o jogo final, entao a alocacao de
    /// string por quadro aqui nao conflita com a regra 5 do CLAUDE.md, que fala de combate.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkManager))]
    public sealed class NetDebugHud : MonoBehaviour
    {
        [Tooltip("F2 alterna o painel em tempo de execucao.")]
        [SerializeField] bool visible = true;

        [SerializeField] NetLauncher launcher;

        NetworkManager _net;
        GUIStyle _style;
        readonly StringBuilder _text = new StringBuilder(512);

        void Awake()
        {
            _net = GetComponent<NetworkManager>();
            if (launcher == null) launcher = GetComponent<NetLauncher>();
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
                visible = !visible;
        }

        void OnGUI()
        {
            if (!visible || _net == null) return;

            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                richText = false,
                alignment = TextAnchor.UpperLeft,
            };

            GUILayout.BeginArea(new Rect(Screen.width - 330f, 10f, 320f, 420f), GUI.skin.box);

            GUILayout.Label(Describe(), _style);

            GUILayout.EndArea();
        }

        string Describe()
        {
            _text.Clear();

            if (!_net.IsListening)
            {
                _text.Append("REDE: desligada\n");
                _text.Append("F3 abre a sala\n");

                if (launcher != null && launcher.LastError.Length > 0)
                    _text.Append(launcher.LastError).Append('\n');

                return _text.ToString();
            }

            _text.Append("REDE: ").Append(Role()).Append('\n');
            _text.Append("Eu sou o cliente ").Append(_net.LocalClientId).Append('\n');

            if (_net.IsClient && !_net.IsServer)
                _text.Append("Ida e volta ").Append(RoundTripMilliseconds()).Append(" ms\n");

            _text.Append("Conectados ").Append(_net.ConnectedClientsIds.Count).Append('\n');
            _text.Append("---- personagens ----\n");

            foreach (NetworkObject spawned in _net.SpawnManager.SpawnedObjectsList)
            {
                if (!spawned.IsPlayerObject) continue;

                _text.Append(spawned.IsLocalPlayer ? "> " : "  ");
                _text.Append(spawned.name.Replace("(Clone)", string.Empty));
                _text.Append("  dono ").Append(spawned.OwnerClientId);

                // O que manda a posicao e o dono; o que manda o dano e o host. Essa e a
                // divisao da ADR 0008, e ver as duas juntas e o que torna ela depuravel.
                _text.Append(spawned.IsOwner ? "  [movo eu]" : "  [assisto]");
                _text.Append(_net.IsServer ? "  [resolvo dano]" : string.Empty);
                _text.Append('\n');
            }

            return _text.ToString();
        }

        string Role()
        {
            if (_net.IsHost) return "host";
            if (_net.IsServer) return "servidor";
            return _net.IsConnectedClient ? "cliente" : "conectando";
        }

        int RoundTripMilliseconds()
        {
            NetworkTransport transport = _net.NetworkConfig?.NetworkTransport;
            return transport != null ? (int)transport.GetCurrentRtt(NetworkManager.ServerClientId) : -1;
        }
    }
}
