using System;
using LoboBranco.Combat;
using LoboBranco.Core;
using LoboBranco.Stats;
using UnityEngine;

namespace LoboBranco.AI
{
    /// <summary>
    /// Autoridade: o host, inteira e sem divisao (ADR 0008). O golpe do monstro nao tem
    /// dono do outro lado da rede para pedir nada: quem decide que ele sai, quem conta a
    /// linha do tempo e quem resolve o dano e a mesma maquina. E por isso que este
    /// componente e bem menor que o <c>PlayerMeleeAttacker</c>, que precisa dos tres
    /// pedidos por golpe so para atravessar a rede.
    ///
    /// O que ele compartilha com o do jogador e tudo que importa: a mesma
    /// <see cref="AttackTimeline"/>, a mesma <see cref="MeleeHitbox"/> sem alocacao e o
    /// mesmo <see cref="DamagePipeline"/> de onze estagios. Um monstro que batesse por um
    /// caminho proprio teria o proprio balanceamento, e o docs/03 secao 9 deixaria de
    /// valer para metade dos golpes do jogo.
    ///
    /// Nenhum numero mora aqui. O golpe, a arma natural e a pausa vem do
    /// <see cref="MonsterDef"/>, o dano cru vem do <c>AttackDamage</c> da folha de
    /// atributos, e os multiplicadores vem do <see cref="CombatTuningDef"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterVitals))]
    public sealed class EnemyMeleeAttacker : MonoBehaviour, IMeleeAttacker, IDamageDealer
    {
        [Header("Dados")]
        [Tooltip("A especie: de onde saem o golpe, a arma natural e a pausa entre golpes.")]
        [SerializeField] MonsterDef monster;

        [Tooltip("Os multiplicadores do pipeline. Sem ele o componente se desliga.")]
        [SerializeField] CombatTuningDef tuning;

        [Header("Alvos")]
        [Tooltip("Deixe em Nothing para usar a mascara padrao de GameLayers.EnemyAttackTargets.")]
        [SerializeField] LayerMask targetMask;

        [Tooltip("Teto de colisores por consulta. Estourar faz alvos alem do teto ficarem invisiveis.")]
        [SerializeField] int maxCollidersPerQuery = 8;

        const int MaxTargetsPerSwing = 4;

        DamagePipeline _pipeline;
        MeleeHitbox _hitbox;
        IDamageable[] _results;
        CharacterVitals _vitals;
        ControlStatus _control;
        LayerMask _resolvedMask;

        readonly AttackTimeline _timeline = new AttackTimeline();

        bool _windowOpen;
        bool _queriedSinceOpen;
        float _cooldownRemaining;

        // ---------------------------------------------------------------- leitura

        public StatSheet Stats => _vitals != null ? _vitals.Stats : null;

        /// <summary>O golpe da especie, ou nulo quando ela nao tem nenhum configurado.</summary>
        public AttackDef Attack => monster != null ? monster.meleeAttack : null;

        /// <summary>
        /// Alcance util do golpe. E o alcance do proprio asset, e nao um numero a parte:
        /// um segundo numero aqui poderia discordar da hitbox, e o sintoma seria a
        /// criatura parando fora do alcance e batendo no vazio para sempre.
        /// </summary>
        public float Reach => Attack != null ? Attack.reach : 0f;

        /// <summary>Verdadeiro do inicio da anticipacao ao fim da recuperacao.</summary>
        public bool Swinging => _timeline.Running;

        /// <summary>
        /// Fase do golpe corrente, na contagem de quem resolve. O telegrafo nao le isto: no
        /// cliente esta contagem nao existe, e o aviso de la sai de <see cref="SwingStarted"/>.
        /// </summary>
        public AttackPhase Phase => _timeline.CurrentPhase;

        /// <summary>Falso durante a pausa entre golpes, durante um golpe, e depois de cair.</summary>
        public bool Ready => Attack != null && !Swinging && _cooldownRemaining <= 0f && !IsDown && !IsIncapacitated;

        /// <summary>Quem resolve: o host, ou eu mesmo quando nao ha rede.</summary>
        public bool CanResolve => _vitals == null || _vitals.CanResolve;

        bool IsDown => _vitals != null && _vitals.IsDown;

        bool IsIncapacitated => _control != null && _control.IsIncapacitated;

        /// <summary>Um golpe comecou. Dispara so em quem resolve, e e o que o telegrafo manda pela rede.</summary>
        public event Action SwingStarted;

        /// <summary>
        /// Um golpe foi cortado antes do fim. O fim natural nao dispara nada: cada maquina
        /// chega nele sozinha pela mesma linha do tempo.
        /// </summary>
        public event Action SwingInterrupted;

        // ------------------------------------------------------------------ ciclo

        void Awake()
        {
            _vitals = GetComponent<CharacterVitals>();
            _control = GetComponent<ControlStatus>();

            if (tuning == null)
            {
                Debug.LogError($"{name}: {nameof(EnemyMeleeAttacker)} sem {nameof(CombatTuningDef)}. Golpes desligados.", this);
                enabled = false;
                return;
            }

            _pipeline = new DamagePipeline(tuning);
            _hitbox = new MeleeHitbox(maxCollidersPerQuery, MaxTargetsPerSwing);
            _results = new IDamageable[MaxTargetsPerSwing];

            // LayerMask.GetMask aloca um vetor de strings a cada chamada, entao ela nunca
            // pode acontecer dentro da janela de dano.
            _resolvedMask = targetMask.value != 0 ? targetMask : GameLayers.EnemyAttackTargets;
        }

        void Update()
        {
            if (!CanResolve) return;

            // Criatura que cai no meio do golpe para de golpear na hora. Sem isto, quem
            // dirige o golpe e a arvore, e uma arvore que continua rodando deixaria um
            // barghest morto terminar a garrada e acertar o bruxo que acabou de mata-lo. Atordoar
            // e derrubar cortam pelo mesmo motivo (tarefa 1.18a), e o corte viaja: o telegrafo
            // para de avisar em todas as maquinas um golpe que nao vai mais sair.
            if (Swinging && (IsDown || IsIncapacitated))
                EndSwing();

            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= Time.deltaTime;
        }

        // --------------------------------------------------------------- comandos

        /// <summary>
        /// Comeca o golpe da especie. Devolve falso quando nao dava, e nao dar e o caso
        /// normal: durante a pausa, durante outro golpe, ou depois de cair.
        /// </summary>
        public bool TryBeginSwing()
        {
            if (!CanResolve || !Ready) return false;

            _timeline.Begin(Attack, this);
            SwingStarted?.Invoke();

            return true;
        }

        /// <summary>
        /// Envelhece o golpe corrente. Devolve falso quando ele terminou, e e nesse
        /// momento que a pausa entre golpes comeca a contar.
        /// </summary>
        public bool TickSwing(float deltaTime)
        {
            if (!CanResolve) return false;
            if (_timeline.Tick(deltaTime)) return true;

            EndSwing();
            return false;
        }

        /// <summary>
        /// Encerra o golpe. Chamado pelo fim natural, por quem cortar o golpe no meio, que e
        /// a arvore desistindo do no de ataque, e pela propria criatura ao cair.
        /// </summary>
        public void EndSwing()
        {
            // Perguntar antes de encerrar: depois do End a linha do tempo ja nao sabe se
            // chegou ao fim sozinha ou foi cortada.
            bool interrupted = _timeline.Running;

            _timeline.End();
            _windowOpen = false;

            if (monster != null)
                _cooldownRemaining = monster.attackCooldown;

            if (interrupted)
                SwingInterrupted?.Invoke();
        }

        // ----------------------------------------------------------- IMeleeAttacker

        public void BeginSwing(AttackDef attack)
        {
            _windowOpen = false;
            _queriedSinceOpen = false;
            _hitbox?.BeginSwing();
        }

        public void OpenHitbox()
        {
            _windowOpen = true;
            _queriedSinceOpen = false;
        }

        public void CloseHitbox()
        {
            // Uma janela mais curta que um quadro ainda produz exatamente uma consulta.
            // Sem isto, um golpe curto de mais nunca acertaria, e o sintoma seria
            // "as vezes o monstro atravessa voce sem tirar vida". A guarda evita o oposto:
            // no quadro em que a janela abre e fecha junto, consultar duas vezes custaria
            // uma varredura de fisica a toa em cada golpe de cada criatura.
            if (_windowOpen && !_queriedSinceOpen) Query();

            _windowOpen = false;
        }

        public void TickHitbox()
        {
            if (!_windowOpen) return;

            Query();
        }

        public void CancelSwing() => _windowOpen = false;

        // ----------------------------------------------------------- IDamageDealer

        /// <summary>
        /// Sempre falso. O bonus do estagio 7 e o que o bruxo aprendeu no bestiario, e
        /// monstro nao le bestiario.
        /// </summary>
        public bool HasBestiaryKnowledge(CreatureClass creatureClass) => false;

        // ---------------------------------------------------------------- interno

        void Query()
        {
            _queriedSinceOpen = true;

            AttackDef attack = Attack;
            if (attack == null || _hitbox == null) return;

            int count = _hitbox.Query(
                transform.position, transform.forward, attack, _resolvedMask, _results);

            for (int i = 0; i < count; i++)
            {
                Strike(_results[i]);
                _results[i] = null;
            }
        }

        void Strike(IDamageable target)
        {
            if (target == null || target.IsDown) return;

            MeleeWeaponDef weapon = monster != null ? monster.naturalWeapon : null;

            var request = new DamageRequest(
                this,
                target,
                weapon != null ? weapon.baseDamage : 0f,
                weapon != null ? weapon.damageType : DamageType.Slash,
                Attack.stance,
                weapon != null ? weapon.material : WeaponMaterial.Steel,
                OilClass.None,   // Oleo e coisa de lamina de bruxo (docs/03 secao 3)
                flowChain: 0,    // Fluxo e do jogador: encadear e a recompensa de quem le o ritmo
                isCritical: false);

            _pipeline.Deal(request);
        }

#if UNITY_EDITOR
        /// <summary>Desenha o alcance do golpe no editor. Sem isso, afinar distancia e adivinhacao.</summary>
        void OnDrawGizmosSelected()
        {
            AttackDef preview = Attack;
            if (preview == null) return;

            Gizmos.color = new Color(0.9f, 0.2f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * preview.heightOffset, preview.reach);
        }
#endif
    }
}
