using Unity.Netcode;
using UnityEngine;

namespace LoboBranco.Net
{
    /// <summary>
    /// Autoridade: o host. A aprovacao de conexao so roda no servidor, e e ele quem
    /// escolhe onde cada personagem nasce.
    ///
    /// Existe porque o prefab de jogador tem uma posicao so. Sem isto, quatro bruxos
    /// nascem dentro um do outro e o <c>CharacterController</c> empurra todo mundo para
    /// fora em um chute, o que parece bug de rede e nao e.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkManager))]
    public sealed class NetSpawnRing : MonoBehaviour
    {
        [Tooltip("Centro do circulo de nascimento. O padrao e onde o jogador solo nascia na sandbox.")]
        [SerializeField] Vector3 center = new Vector3(0f, 0.1f, -8f);

        [Tooltip("Raio em metros. Maior que o dobro do raio do CharacterController, senao eles se tocam.")]
        [SerializeField] float radius = 1.5f;

        [Tooltip("Teto de participantes. Doc 13 secao 1: de 2 a 4 pessoas.")]
        [SerializeField] int maxPlayers = 4;

        NetworkManager _net;

        /// <summary>
        /// Teto de participantes da sessao. Mora aqui porque e aqui que ele e cobrado, na
        /// aprovacao da conexao; o Relay le este mesmo numero para nao existir uma sala de
        /// quatro lugares com alocacao para seis.
        /// </summary>
        public int MaxPlayers => maxPlayers;

        void Awake()
        {
            _net = GetComponent<NetworkManager>();

            // Precisa estar ligado nos dois lados: o valor entra no hash de configuracao
            // que o cliente e o servidor comparam no aperto de mao.
            _net.NetworkConfig.ConnectionApproval = true;

            // Atribuicao direta, e nao +=: o NGO so aceita um aprovador e lanca excecao se
            // a lista de invocacao passar de um.
            _net.ConnectionApprovalCallback = Approve;
        }

        void OnDestroy()
        {
            if (_net != null && _net.ConnectionApprovalCallback == Approve)
                _net.ConnectionApprovalCallback = null;
        }

        void Approve(NetworkManager.ConnectionApprovalRequest request,
                     NetworkManager.ConnectionApprovalResponse response)
        {
            int taken = _net.ConnectedClientsIds.Count;

            if (taken >= maxPlayers)
            {
                response.Approved = false;
                response.Reason = $"Sessao cheia: {maxPlayers} participantes.";
                return;
            }

            Vector3 position = PositionFor(taken);
            Vector3 facing = center - position;

            response.Approved = true;
            response.CreatePlayerObject = true;
            response.Position = position;
            response.Rotation = facing.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(facing)
                : Quaternion.identity;
        }

        /// <summary>Posicao do enesimo participante no circulo, comecando pelo host.</summary>
        public Vector3 PositionFor(int index)
        {
            if (maxPlayers <= 1) return center;

            float angle = index * Mathf.PI * 2f / maxPlayers;
            return center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
        }
    }
}
