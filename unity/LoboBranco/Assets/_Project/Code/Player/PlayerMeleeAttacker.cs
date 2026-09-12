using System.Text;
using LoboBranco.Combat;
using LoboBranco.Core;
using LoboBranco.Stats;
using Unity.Netcode;
using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// Autoridade dividida (ADR 0008): o dono pede, o host resolve. O dono e quem sabe
    /// que um golpe comecou e quando a janela de dano abre e fecha, porque a maquina de
    /// estados dele e que conta o tempo; o host e quem consulta a fisica e roda o
    /// <see cref="DamagePipeline"/>, e ninguem mais. Um golpe resolvido em duas maquinas
    /// tira vida duas vezes, ou nenhuma.
    ///
    /// Sao tres pedidos por golpe, e nao um por quadro: comecou, abriu, fechou. Entre
    /// abrir e fechar quem consulta a fisica e o host, no proprio <c>Update</c>, usando a
    /// posicao que o <c>NetworkTransform</c> ja traz do dono. Mandar a consulta por RPC a
    /// cada quadro custaria uma dezena de mensagens por golpe para chegar no mesmo lugar.
    ///
    /// O contrato <see cref="IMeleeAttacker"/> nao mudou, e isso e o ponto: o
    /// <see cref="AttackState"/> continua sem saber que rede existe, e quando o Animator
    /// entrar no M4 (tech/adr/0007) sao os eventos do clipe que vao chamar
    /// <see cref="OpenHitbox"/> e <see cref="CloseHitbox"/>, sem tocar em nada daqui.
    ///
    /// Sem rede ligada, o mesmo objeto e o dono e o host, e o caminho e o de antes: a
    /// maquina de estados dirige a janela quadro a quadro (risco X8 do doc 13).
    ///
    /// Toda a decisao de <em>quando</em> mora no <see cref="AttackState"/>, e todo o
    /// <em>quanto</em> mora no pipeline. Este componente so sabe <em>onde</em> e
    /// <em>em quem</em>, e e por isso que ele nao tem nenhum numero: o dano vem do
    /// <see cref="MeleeWeaponDef"/>, a geometria vem do <see cref="AttackDef"/> e os
    /// multiplicadores vem do <see cref="CombatTuningDef"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterVitals))]
    public sealed class PlayerMeleeAttacker : NetworkBehaviour, IMeleeAttacker, IDamageDealer
    {
        [Header("Dados")]
        [SerializeField] CombatTuningDef tuning;
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

        /// <summary>Indice que nao corresponde a golpe nenhum. Vai no lugar de null pela rede.</summary>
        const byte NoAttack = byte.MaxValue;

        DamagePipeline _pipeline;
        MeleeHitbox _hitbox;
        IDamageable[] _results;
        CharacterVitals _vitals;
        LayerMask _resolvedMask;

        // Estado do lado de quem pede. Serve so para o painel de debug do dono.
        bool _windowOpen;

        // Estado do lado de quem resolve. Em uma sessao de quatro, estes tres campos no
        // host sao a unica verdade sobre quem esta acertando o que.
        AttackDef _resolvingAttack;
        bool _resolvingWindowOpen;
        bool _queriedSinceOpen;

        // ---------------------------------------------------------------- leitura

        public StatSheet Stats => _vitals != null ? _vitals.Stats : null;
        public AttackDef LightAttack => lightAttack;
        public AttackDef HeavyAttack => heavyAttack;

        /// <summary>Verdadeiro enquanto a janela de dano esta aberta. O painel de debug mostra isto.</summary>
        public bool HitboxOpen => _windowOpen;

        /// <summary>Quantos alvos distintos o golpe corrente ja acertou. So o host conta.</summary>
        public int HitsThisSwing => _hitbox?.HitCount ?? 0;

        /// <summary>Resumo do ultimo golpe conectado. Vazio enquanto <c>logDamageBreakdown</c> estiver desligado.</summary>
        public string LastHitSummary { get; private set; } = string.Empty;

        /// <summary>
        /// Quem pode pedir um golpe: o dono, ou eu mesmo quando nao ha rede. A ordem
        /// importa, porque <c>IsOwner</c> le o NetworkManager e fora de rede nao ha um.
        /// </summary>
        public bool CanRequest => !IsSpawned || IsOwner;

        /// <summary>Quem consulta a fisica e roda o pipeline: o host, ou eu mesmo sem rede.</summary>
        public bool CanResolve => !IsSpawned || IsServer;

        // ------------------------------------------------------------------ ciclo

        void Awake()
        {
            _vitals = GetComponent<CharacterVitals>();

            if (tuning == null)
            {
                Debug.LogError($"{nameof(PlayerMeleeAttacker)} sem {nameof(CombatTuningDef)}. Ataques desligados.", this);
                enabled = false;
                return;
            }

            _pipeline = new DamagePipeline(tuning) { LoggingEnabled = logDamageBreakdown };
            _hitbox = new MeleeHitbox(maxCollidersPerQuery, MaxTargetsPerSwing);
            _results = new IDamageable[MaxTargetsPerSwing];

            // LayerMask.GetMask aloca um vetor de strings a cada chamada, entao ela nunca
            // pode acontecer dentro da janela de dano.
            _resolvedMask = targetMask.value != 0 ? targetMask : GameLayers.PlayerAttackTargets;
        }

        void Update()
        {
            // Com rede, a janela do host anda sozinha entre o pedido de abrir e o de
            // fechar. Sem rede, quem chama TickHitbox e a maquina de estados, e entrar
            // aqui tambem consultaria a fisica duas vezes no mesmo quadro.
            if (IsSpawned && IsServer)
                ResolveTick();
        }

        // ------------------------------------------------------------ IMeleeAttacker

        public void BeginSwing(AttackDef attack)
        {
            if (!CanRequest) return;

            _windowOpen = false;

            if (CanResolve) ResolveBeginSwing(attack);
            else BeginSwingRpc(IndexOf(attack));
        }

        public void OpenHitbox()
        {
            if (!CanRequest) return;

            _windowOpen = true;

            if (CanResolve) ResolveSetWindow(true);
            else SetWindowRpc(true);
        }

        public void CloseHitbox()
        {
            if (!CanRequest) return;

            _windowOpen = false;

            if (CanResolve) ResolveSetWindow(false);
            else SetWindowRpc(false);
        }

        public void CancelSwing()
        {
            if (!CanRequest) return;

            _windowOpen = false;

            if (CanResolve) ResolveCancelSwing();
            else CancelSwingRpc();
        }

        /// <summary>
        /// So faz alguma coisa fora de rede, onde quem pede e quem resolve sao o mesmo
        /// objeto. Com rede, quem consulta a fisica e o <c>Update</c> do host.
        /// </summary>
        public void TickHitbox()
        {
            if (IsSpawned) return;

            ResolveTick();
        }

        // ------------------------------------------------------------------- rede

        // InvokePermission.Owner faz o proprio NGO recusar um pedido que nao venha do
        // dono. Sem isso, um participante poderia mandar o personagem dos outros atacar.

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void BeginSwingRpc(byte attackIndex) => ResolveBeginSwing(FromIndex(attackIndex));

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void SetWindowRpc(bool open) => ResolveSetWindow(open);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void CancelSwingRpc() => ResolveCancelSwing();

        /// <summary>
        /// O catalogo de golpes viaja por indice porque asset nao viaja pela rede. Dois
        /// golpes cabem em um byte com folga; quando as posturas entrarem (tarefa 1.14)
        /// isto vira uma tabela vinda da escola, e nao um <c>if</c> a mais.
        /// </summary>
        byte IndexOf(AttackDef attack)
        {
            if (attack == lightAttack) return 0;
            if (attack == heavyAttack) return 1;

            return NoAttack;
        }

        AttackDef FromIndex(byte index) => index switch
        {
            0 => lightAttack,
            1 => heavyAttack,
            _ => null,
        };

        // -------------------------------------------------------------- resolucao

        void ResolveBeginSwing(AttackDef attack)
        {
            _resolvingAttack = attack;
            _resolvingWindowOpen = false;
            _queriedSinceOpen = false;
            _hitbox?.BeginSwing();
        }

        void ResolveSetWindow(bool open)
        {
            if (_resolvingAttack == null) return;

            if (open)
            {
                _resolvingWindowOpen = true;
                _queriedSinceOpen = false;
                return;
            }

            // Uma janela mais curta que um quadro do host ainda produz exatamente uma
            // consulta. Sem esta linha, o golpe de um cliente cujo abrir e fechar chegam
            // no mesmo quadro nao acertaria nunca, e o sintoma seria "as vezes nao sai".
            if (_resolvingWindowOpen && !_queriedSinceOpen)
                Query();

            _resolvingWindowOpen = false;
        }

        void ResolveCancelSwing()
        {
            _resolvingWindowOpen = false;
            _resolvingAttack = null;
        }

        void ResolveTick()
        {
            if (!_resolvingWindowOpen || _resolvingAttack == null || _hitbox == null) return;

            Query();
        }

        void Query()
        {
            _queriedSinceOpen = true;

            int count = _hitbox.Query(
                transform.position, transform.forward, _resolvingAttack, _resolvedMask, _results);

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
                _resolvingAttack.stance,
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
