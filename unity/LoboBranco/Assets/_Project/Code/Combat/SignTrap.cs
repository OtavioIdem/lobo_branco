using LoboBranco.Core;
using Unity.Netcode;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Autoridade: o host (ADR 0008, tech/adr/0012). Ele faz nascer a armadilha, varre quem esta
    /// dentro, aplica a lentidao e a destroi no fim. As outras maquinas so a enxergam.
    ///
    /// E o primeiro sinal que deixa alguma coisa no mundo em vez de agir num alvo (tarefa 1.18f), e
    /// por isso e um objeto de rede: quatro pessoas precisam ver o mesmo circulo no mesmo chao.
    ///
    /// <b>O que viaja e so o instante em que ela some.</b> Raio e forca da lentidao vem do prefab, e
    /// o prefab e o mesmo em todas as maquinas: replicar seria mandar o que o outro lado ja tem. A
    /// lentidao em si ja viaja sozinha, no <see cref="ControlStatus"/> de cada criatura.
    ///
    /// <b>A lentidao e reaplicada em pulsos curtos</b>, e nao aplicada uma vez pelos 12 s. E o que
    /// faz sair da armadilha devolver a velocidade sem ninguem precisar avisar: quem esta dentro
    /// recebe de novo antes de a anterior vencer, e quem sai simplesmente para de receber. O
    /// <see cref="ControlState"/> ja resolve reaplicar sem somar.
    ///
    /// Sem rede, ela nasce local e funciona igual (risco X8 do doc 13).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SignTrap : NetworkBehaviour
    {
        [Header("Area (docs/03 secao 8)")]
        [Tooltip("Raio em metros. docs/03 secao 8: 4.")]
        [Min(0f)] [SerializeField] float radius = 4f;

        [Tooltip("Fracao da velocidade que a armadilha tira. docs/03 secao 8: 0,6.")]
        [Range(0f, 1f)] [SerializeField] float slowFraction = 0.6f;

        [Header("Pulso")]
        [Tooltip("De quanto em quanto tempo ela varre quem esta dentro. Nao e balanceamento.")]
        [Min(0.05f)] [SerializeField] float sweepSeconds = 0.25f;

        [Header("Alvos")]
        [Tooltip("Deixe em Nothing para usar a mascara padrao de GameLayers.PlayerAttackTargets.")]
        [SerializeField] LayerMask targetMask;

        [Tooltip("Teto de colisores por varredura. Estourar faz criaturas alem do teto nao serem pegas.")]
        [SerializeField] int maxCollidersPerSweep = 16;

        [Header("Greybox")]
        [Tooltip("O disco que mostra a area. Escalado pelo raio no Awake, para o desenho nunca mentir.")]
        [SerializeField] Transform disc;

        readonly NetworkVariable<double> _endsAt = new NetworkVariable<double>(
            0d,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        double _soloEndsAt;
        Collider[] _overlap;
        LayerMask _resolvedMask;
        float _untilNextSweep;

        // ---------------------------------------------------------------- leitura

        /// <summary>Quem varre e destroi: o host, ou eu mesmo sem rede.</summary>
        public bool CanResolve => !IsSpawned || IsServer;

        double Now => IsSpawned ? NetworkManager.ServerTime.Time : Time.timeAsDouble;

        double EndsAt => IsSpawned ? _endsAt.Value : _soloEndsAt;

        public float Radius => radius;

        public float SlowFraction => slowFraction;

        /// <summary>Ainda de pe. Vale em todas as maquinas.</summary>
        public bool IsActive => Now < EndsAt;

        /// <summary>Segundos ate sumir, nunca negativo.</summary>
        public float Remaining => Now < EndsAt ? (float)(EndsAt - Now) : 0f;

        // ------------------------------------------------------------- autoridade

        /// <summary>
        /// Arma a armadilha por tantos segundos. So o host chama, e antes do nascimento em rede: e
        /// por isso que o valor vai para o campo solo e so vira variavel replicada no spawn, como a
        /// vida do <see cref="CharacterVitals"/>.
        /// </summary>
        public void Arm(float seconds)
        {
            double endsAt = Now + Mathf.Max(0f, seconds);

            if (IsSpawned && IsServer) _endsAt.Value = endsAt;
            else _soloEndsAt = endsAt;
        }

        // ------------------------------------------------------------------ ciclo

        void Awake()
        {
            _overlap = new Collider[Mathf.Max(1, maxCollidersPerSweep)];

            // LayerMask.GetMask aloca, entao ela nunca acontece dentro da varredura.
            _resolvedMask = targetMask.value != 0 ? targetMask : GameLayers.PlayerAttackTargets;

            // O desenho sai do numero, e nao o contrario: um disco que nao corresponde ao raio
            // ensina a mira errada, e em coop ensina errado para quatro pessoas.
            if (disc != null) disc.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
        }

        public override void OnNetworkSpawn()
        {
            // O relogio de referencia deixa de ser o local e passa a ser o do servidor.
            if (IsServer) _endsAt.Value = NetworkManager.ServerTime.Time + (_soloEndsAt - Time.timeAsDouble);
        }

        void Update() => Step(Now, Time.deltaTime);

        /// <summary>
        /// Um passo da armadilha: varre quem esta dentro no pulso, e some quando a hora chega. O
        /// <c>Update</c> chama com a hora desta maquina; o teste chama com a hora que quiser.
        /// </summary>
        public void Step(double now, float deltaTime)
        {
            if (!CanResolve) return;

            // Sem prazo nao e prazo vencido: uma armadilha que ninguem armou fica parada em vez de
            // se destruir no primeiro quadro. Em jogo o efeito arma na mesma linha em que a faz
            // nascer, mas depender dessa ordem e o tipo de coisa que quebra quando o spawn da rede
            // ou um objeto posto na cena chega um quadro depois.
            if (EndsAt <= 0d) return;

            if (now >= EndsAt)
            {
                Dismiss();
                return;
            }

            _untilNextSweep -= deltaTime;
            if (_untilNextSweep > 0f) return;

            _untilNextSweep = sweepSeconds;
            Sweep();
        }

        // ---------------------------------------------------------------- interno

        void Sweep()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, radius, _overlap, _resolvedMask, QueryTriggerInteraction.Collide);

            // A lentidao dura mais que o pulso de proposito: sem essa sobra, a criatura piscaria
            // entre lenta e normal entre uma varredura e a seguinte.
            float hold = sweepSeconds * 2f;

            for (int i = 0; i < count; i++)
            {
                Collider collider = _overlap[i];
                if (collider == null) continue;

                if (!collider.TryGetComponent(out ControlStatus status))
                    status = collider.GetComponentInParent<ControlStatus>();

                if (status != null) status.ApplySlow(slowFraction, hold);
            }
        }

        void Dismiss()
        {
            if (IsSpawned)
            {
                if (IsServer) NetworkObject.Despawn();
                return;
            }

            Destroy(gameObject);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 0.4f, 1f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
#endif
    }
}
