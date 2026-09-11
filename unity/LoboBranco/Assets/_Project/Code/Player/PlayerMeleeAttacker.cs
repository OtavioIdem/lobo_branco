using System.Text;
using LoboBranco.Combat;
using LoboBranco.Core;
using LoboBranco.Stats;
using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// A ponte entre o estado de ataque e o pipeline de dano: abre a janela, consulta a
    /// fisica e entrega cada alvo novo ao <see cref="DamagePipeline"/>.
    ///
    /// Toda a decisao de <em>quando</em> mora no <see cref="AttackState"/>, e todo o
    /// <em>quanto</em> mora no pipeline. Este componente so sabe <em>onde</em> e
    /// <em>em quem</em>, e e por isso que ele nao tem nenhum numero: o dano vem do
    /// <see cref="MeleeWeaponDef"/>, a geometria vem do <see cref="AttackDef"/> e os
    /// multiplicadores vem do <see cref="CombatTuningDef"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerMeleeAttacker : MonoBehaviour, IMeleeAttacker, IDamageDealer
    {
        [Header("Dados")]
        [SerializeField] CombatTuningDef tuning;
        [SerializeField] StatBlockDef statBlock;
        [SerializeField] MeleeWeaponDef weapon;

        [Header("Golpes")]
        [SerializeField] AttackDef lightAttack;
        [SerializeField] AttackDef heavyAttack;

        [Header("Alvos")]
        [Tooltip("Deixe em Nothing para usar a mascara padrao de GameLayers.PlayerAttackTargets.")]
        [SerializeField] LayerMask targetMask;

        [Tooltip("Teto de colisores por consulta. Estourar faz alvos alem do teto ficarem invisiveis.")]
        [SerializeField] int maxCollidersPerQuery = 16;

        [Header("Depuracao (cena de sandbox)")]
        [Tooltip("Registra cada multiplicador aplicado. Aloca string por golpe: ligado so aqui.")]
        [SerializeField] bool logDamageBreakdown = true;

        [Tooltip("Simula a secao Vulnerabilidades destravada, para ver o 1,25x antes do bestiario existir.")]
        [SerializeField] bool assumeBestiaryKnowledge;

        [Tooltip("Simula um oleo aplicado na lamina, para ver o 1,5x antes da alquimia existir.")]
        [SerializeField] OilClass appliedOil = OilClass.None;

        const int MaxTargetsPerSwing = 8;

        DamagePipeline _pipeline;
        MeleeHitbox _hitbox;
        IDamageable[] _results;
        StatSheet _stats;
        LayerMask _resolvedMask;

        AttackDef _current;
        bool _windowOpen;

        // ---------------------------------------------------------------- leitura

        public StatSheet Stats => _stats;
        public AttackDef LightAttack => lightAttack;
        public AttackDef HeavyAttack => heavyAttack;

        /// <summary>Verdadeiro enquanto a janela de dano esta aberta. O painel de debug mostra isto.</summary>
        public bool HitboxOpen => _windowOpen;

        /// <summary>Quantos alvos distintos o golpe corrente ja acertou.</summary>
        public int HitsThisSwing => _hitbox?.HitCount ?? 0;

        /// <summary>Resumo do ultimo golpe conectado. Vazio enquanto <c>logDamageBreakdown</c> estiver desligado.</summary>
        public string LastHitSummary { get; private set; } = string.Empty;

        // ------------------------------------------------------------------ ciclo

        void Awake()
        {
            if (tuning == null)
            {
                Debug.LogError($"{nameof(PlayerMeleeAttacker)} sem {nameof(CombatTuningDef)}. Ataques desligados.", this);
                enabled = false;
                return;
            }

            _pipeline = new DamagePipeline(tuning) { LoggingEnabled = logDamageBreakdown };
            _hitbox = new MeleeHitbox(maxCollidersPerQuery, MaxTargetsPerSwing);
            _results = new IDamageable[MaxTargetsPerSwing];
            _stats = statBlock != null ? statBlock.CreateSheet() : new StatSheet();

            // LayerMask.GetMask aloca um vetor de strings a cada chamada, entao ela nunca
            // pode acontecer dentro da janela de dano.
            _resolvedMask = targetMask.value != 0 ? targetMask : GameLayers.PlayerAttackTargets;
        }

        // ------------------------------------------------------------ IMeleeAttacker

        public void BeginSwing(AttackDef attack)
        {
            _current = attack;
            _windowOpen = false;
            _hitbox?.BeginSwing();
        }

        public void OpenHitbox() => _windowOpen = true;

        public void CloseHitbox() => _windowOpen = false;

        public void CancelSwing()
        {
            _windowOpen = false;
            _current = null;
        }

        public void TickHitbox()
        {
            if (!_windowOpen || _current == null || _hitbox == null) return;

            int count = _hitbox.Query(transform.position, transform.forward, _current, _resolvedMask, _results);

            for (int i = 0; i < count; i++)
            {
                Strike(_results[i]);
                _results[i] = null;
            }
        }

        // ------------------------------------------------------------ IDamageDealer

        /// <summary>
        /// Sempre falso ate o bestiario existir (tarefa 3.16). O booleano de depuracao
        /// existe para conferir o estagio 7 do pipeline dentro do jogo, e nao so no teste.
        /// </summary>
        public bool HasBestiaryKnowledge(CreatureClass creatureClass) => assumeBestiaryKnowledge;

        // ---------------------------------------------------------------- interno

        void Strike(IDamageable target)
        {
            if (target == null) return;

            var request = new DamageRequest(
                this,
                target,
                weapon != null ? weapon.baseDamage : 0f,
                weapon != null ? weapon.damageType : DamageType.Slash,
                _current.stance,
                weapon != null ? weapon.material : WeaponMaterial.Steel,
                appliedOil,
                flowChain: 0,      // Fluxo e a tarefa 1.12; ate la toda corrente vale 1,0x
                isCritical: false); // Critico depende de CritChance, que entra com o equipamento

            // Reavaliado a cada golpe para que o interruptor do Inspector valha durante
            // a sessao, que e quando ele serve para alguma coisa.
            _pipeline.LoggingEnabled = logDamageBreakdown;

            DamageResult result = _pipeline.Deal(request);

            if (_pipeline.LoggingEnabled)
                RecordSummary(target, result);
        }

        /// <summary>
        /// Monta o texto do ultimo golpe. So roda com o log ligado, porque concatenar
        /// string em combate viola a regra 5 do CLAUDE.md e o preco so vale durante a
        /// afinacao de balanceamento.
        /// </summary>
        void RecordSummary(IDamageable target, in DamageResult result)
        {
            var sb = new StringBuilder(192);
            sb.Append(target is UnityEngine.Object unityTarget ? unityTarget.name : "alvo");
            sb.Append("  ").Append(result.Amount.ToString("0.0"));
            sb.Append("  (x").Append(result.TotalMultiplier.ToString("0.00")).Append(")\n");
            sb.Append(_pipeline.LastContext.DescribeLog());

            LastHitSummary = sb.ToString();
            Debug.Log($"[Golpe] {LastHitSummary}", this);
        }

#if UNITY_EDITOR
        /// <summary>Desenha o alcance do golpe leve no editor. Sem isso, afinar 'reach' e adivinhacao.</summary>
        void OnDrawGizmosSelected()
        {
            AttackDef preview = lightAttack != null ? lightAttack : heavyAttack;
            if (preview == null) return;

            Vector3 center = transform.position + Vector3.up * preview.heightOffset;

            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(center + transform.forward * preview.radius, preview.radius);
            Gizmos.DrawWireSphere(
                center + transform.forward * Mathf.Max(preview.radius, preview.reach - preview.radius),
                preview.radius);

            Quaternion left = Quaternion.Euler(0f, -preview.arcDegrees * 0.5f, 0f);
            Quaternion right = Quaternion.Euler(0f, preview.arcDegrees * 0.5f, 0f);

            Gizmos.DrawLine(center, center + left * transform.forward * preview.reach);
            Gizmos.DrawLine(center, center + right * transform.forward * preview.reach);
        }
#endif
    }
}
