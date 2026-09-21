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
    public sealed class PlayerMeleeAttacker : NetworkBehaviour, IMeleeAttacker, IWeaponHolder, IDamageDealer
    {
        [Header("Dados")]
        [SerializeField] CombatTuningDef tuning;

        [Header("Espadas (docs/03 secao 3)")]
        [Tooltip("Aco: 1,0x em humanoide, 0,35x em monstro.")]
        [SerializeField] MeleeWeaponDef steelSword;

        [Tooltip("Prata: 0,5x em humanoide, 1,0x em monstro.")]
        [SerializeField] MeleeWeaponDef silverSword;

        [Tooltip("Com qual espada o bruxo entra na cena.")]
        [SerializeField] WeaponMaterial startingMaterial = WeaponMaterial.Steel;

        // Os golpes por postura moravam aqui ate a tarefa 1.31. Agora sao da escola, e este
        // componente pergunta a ela: um bruxo do Grifo e um do Lobo empunham a mesma espada e
        // desferem golpes diferentes (docs/13 secao 5.1). Achado na primeira vez que precisa,
        // porque o host resolve o golpe do companheiro e a pergunta chega pela rede.
        PlayerSchool _school;

        PlayerSchool School => _school != null ? _school : (_school = GetComponent<PlayerSchool>());

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

        [Header("Sensacao (docs/03 secao 11)")]
        [Tooltip("De onde sai o hitstop por postura. Sem asset, o golpe conecta sem congelar.")]
        [SerializeField] HitFeedbackDef feedback;

        const int MaxTargetsPerSwing = 8;

        /// <summary>Indice que nao corresponde a golpe nenhum. Vai no lugar de null pela rede.</summary>
        const byte NoAttack = byte.MaxValue;

        DamagePipeline _pipeline;
        MeleeHitbox _hitbox;
        IDamageable[] _results;
        CharacterVitals _vitals;
        LayerMask _resolvedMask;
        FlowChain _flow;

        // A corrente de Fluxo e contada por quem resolve, mas quem precisa ver e o dono:
        // e o dono que decide se ataca de novo agora ou espera. Mesmo arranjo da vida em
        // CharacterVitals, e pelo mesmo motivo: sem rede, o campo solo; com rede, a
        // variavel replicada, porque escrever nela antes do spawn gera aviso do NGO.
        readonly NetworkVariable<byte> _replicatedFlowLinks = new NetworkVariable<byte>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        byte _soloFlowLinks;

        // Estado do lado de quem pede. Serve so para o painel de debug do dono.
        bool _windowOpen;

        // Estado do lado de quem resolve. Em uma sessao de quatro, estes campos no host
        // sao a unica verdade sobre quem esta acertando o que.
        AttackDef _resolvingAttack;
        bool _resolvingWindowOpen;
        bool _queriedSinceOpen;

        // Um hitstop por golpe, e nao por alvo (tech/adr/0010). Zerado no inicio de cada
        // golpe, do lado de quem resolve.
        bool _hitstopGranted;

        /// <summary>
        /// O golpe conectou: dano do primeiro alvo e segundos de hitstop que o host somou.
        /// Dispara so no dono, uma vez por golpe. E o que congela o golpe na maquina de estados
        /// e o que dispara tremor e soco de camera na tela de quem bateu.
        /// </summary>
        public event System.Action<float, float> HitConfirmed;

        // Quanto falta do golpe corrente, na contagem de quem resolve. Chegar a zero e o
        // que abre a janela de Fluxo, entao ela abre no fim do golpe inteiro e nao no fim
        // da janela de dano.
        float _resolvingRemaining;

        // A espada na mao. Vale no dono, que escolhe, e no host, que resolve o dano com
        // ela. Nao e replicada para os outros participantes porque ninguem ainda tem o que
        // fazer com essa informacao: quando a lamina tiver modelo e brilho de oleo (M4),
        // ela vira NetworkVariable como a vida e o Fluxo.
        WeaponMaterial _equipped;

        // ---------------------------------------------------------------- leitura

        public StatSheet Stats => _vitals != null ? _vitals.Stats : null;

        /// <summary>
        /// O golpe de uma postura. E por aqui que a postura vira dano: o golpe carrega a
        /// propria postura, entao o estagio 2 e o 3 do pipeline saem do asset e nenhum
        /// <c>if</c> de postura precisa existir em codigo de combate.
        /// </summary>
        public AttackDef AttackFor(Stance stance) => School != null ? School.AttackFor(stance) : null;

        /// <summary>Verdadeiro enquanto a janela de dano esta aberta. O painel de debug mostra isto.</summary>
        public bool HitboxOpen => _windowOpen;

        /// <summary>Quantos alvos distintos o golpe corrente ja acertou. So o host conta.</summary>
        public int HitsThisSwing => _hitbox?.HitCount ?? 0;

        /// <summary>Elos da corrente de Fluxo, contados pelo host e visiveis para todos.</summary>
        public int FlowLinks => IsSpawned ? _replicatedFlowLinks.Value : _soloFlowLinks;

        /// <summary>Verdadeiro enquanto da para encadear. Vale so em quem resolve.</summary>
        public bool FlowWindowOpen => _flow != null && _flow.WindowOpen;

        /// <summary>
        /// Custo de vigor do golpe agora, ja com o desconto de Fluxo (docs/03 secao 6).
        /// O dono consulta para saber se pode pedir, e o host cobra o mesmo numero.
        /// </summary>
        public float StaminaCostFor(AttackDef attack)
            => attack != null && tuning != null ? tuning.StaminaCost(attack.staminaCost, FlowLinks) : 0f;

        /// <summary>Resumo do ultimo golpe conectado. Vazio enquanto <c>logDamageBreakdown</c> estiver desligado.</summary>
        public string LastHitSummary { get; private set; } = string.Empty;

        /// <summary>
        /// Quem pode pedir um golpe: o dono, ou eu mesmo quando nao ha rede. A ordem
        /// importa, porque <c>IsOwner</c> le o NetworkManager e fora de rede nao ha um.
        /// </summary>
        public bool CanRequest => !IsSpawned || IsOwner;

        /// <summary>Quem consulta a fisica e roda o pipeline: o host, ou eu mesmo sem rede.</summary>
        public bool CanResolve => !IsSpawned || IsServer;

        // ------------------------------------------------------------ IWeaponHolder

        public WeaponMaterial EquippedMaterial => _equipped;

        public MeleeWeaponDef EquippedWeapon => WeaponOf(_equipped);

        public bool Has(WeaponMaterial material) => WeaponOf(material) != null;

        /// <summary>
        /// Troca a espada na mao, no fim dos 0,7 s do <see cref="WeaponSwapState"/>.
        ///
        /// O dono avisa o host, porque o material e o estagio 4 do pipeline e quem roda o
        /// pipeline e o host: sem este aviso, o host resolveria o golpe com a espada
        /// errada e o dano sairia 0,35x quando deveria sair 1,0x. Uma mensagem por troca,
        /// e nao uma por golpe, porque trocar de espada custa 0,7 s e golpe nao.
        /// </summary>
        public void Equip(WeaponMaterial material)
        {
            if (!Has(material)) return;

            ApplyEquip(material);

            if (IsSpawned && IsOwner && !IsServer)
                EquipRpc((byte)material);
        }

        void ApplyEquip(WeaponMaterial material) => _equipped = material;

        MeleeWeaponDef WeaponOf(WeaponMaterial material)
            => material == WeaponMaterial.Silver ? silverSword : steelSword;

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
            _flow = new FlowChain(tuning.flowWindowSeconds);

            // Sem a espada inicial configurada, entra a que existir: uma capsula sem
            // espada nenhuma bate zero e o sintoma parece bug de pipeline.
            _equipped = Has(startingMaterial) ? startingMaterial : WeaponMaterial.Steel;

            // LayerMask.GetMask aloca um vetor de strings a cada chamada, entao ela nunca
            // pode acontecer dentro da janela de dano.
            _resolvedMask = targetMask.value != 0 ? targetMask : GameLayers.PlayerAttackTargets;
        }

        void Update()
        {
            if (!CanResolve) return;

            AdvanceFlow(Time.deltaTime);

            // Com rede, a janela de dano do host anda sozinha entre o pedido de abrir e o
            // de fechar. Sem rede, quem chama TickHitbox e a maquina de estados, e
            // consultar aqui tambem bateria na fisica duas vezes no mesmo quadro.
            if (IsSpawned)
                ResolveTick();
        }

        /// <summary>
        /// Conta o golpe corrente ate o fim e so entao abre a janela de encadear. O tempo
        /// sai do proprio <see cref="AttackDef"/>, que quem resolve tambem tem em maos:
        /// assim o host nao precisa de um pedido a mais so para saber que o golpe acabou.
        /// </summary>
        void AdvanceFlow(float deltaTime)
        {
            if (_resolvingRemaining > 0f)
            {
                _resolvingRemaining -= deltaTime;

                if (_resolvingRemaining <= 0f)
                {
                    _resolvingRemaining = 0f;
                    _flow.Open();
                }

                return;
            }

            int before = _flow.Links;
            _flow.Tick(deltaTime);

            if (_flow.Links != before)
                WriteFlowLinks(_flow.Links);
        }

        void WriteFlowLinks(int links)
        {
            var value = (byte)Mathf.Min(links, byte.MaxValue);

            if (IsSpawned) _replicatedFlowLinks.Value = value;
            else _soloFlowLinks = value;
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

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void EquipRpc(byte material) => ApplyEquip((WeaponMaterial)material);

        /// <summary>
        /// O golpe viaja como postura, porque asset nao viaja pela rede e a postura e
        /// exatamente o que distingue um golpe do outro (docs/03 secao 4). O host resolve
        /// o asset do proprio lado: os dois tem o mesmo prefab, entao a mesma postura da
        /// no mesmo golpe, e nao existe como as duas maquinas discordarem de qual foi.
        ///
        /// Desde a tarefa 1.31 a tabela vem do <see cref="SchoolDef"/>, e este metodo continua
        /// sendo uma conversao de postura para asset, sem nenhum <c>if</c> de escola. E por
        /// isso que cada golpe da escola tem que declarar a postura da vaga em que esta: o
        /// dono manda a postura do golpe, e o host busca o asset pela vaga.
        /// </summary>
        byte IndexOf(AttackDef attack) => attack != null ? (byte)attack.stance : NoAttack;

        AttackDef FromIndex(byte index)
            => index <= (byte)Stance.Group ? AttackFor((Stance)index) : null;

        // -------------------------------------------------------------- resolucao

        void ResolveBeginSwing(AttackDef attack)
        {
            _resolvingAttack = attack;
            _resolvingWindowOpen = false;
            _queriedSinceOpen = false;
            _hitstopGranted = false;
            _hitbox?.BeginSwing();

            int links = _flow.Begin();
            WriteFlowLinks(links);

            // Corrente alta rende adrenalina (docs/03 secao 6). Quem conta elos e este
            // lado, entao a carga nasce aqui e vai replicada como o resto.
            if (_vitals != null) _vitals.NoteFlowLinks(links);

            // Cobrar depois do Begin e de proposito: o desconto de Fluxo vale para o golpe
            // que fecha a corrente, e nao para o seguinte (docs/03 secao 6).
            if (_vitals != null)
                _vitals.TrySpendStamina(StaminaCostFor(attack));

            _resolvingRemaining = attack != null ? attack.TotalDuration : 0f;
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
            _resolvingRemaining = 0f;

            _flow.Break();
            WriteFlowLinks(0);
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

            // A espada que conta e a que este lado acha que esta na mao. Em rede, este
            // lado e o host, e e por isso que a troca vira aviso em vez de ficar so no
            // dono: aqui e onde o 0,35x do aco em monstro e decidido.
            MeleeWeaponDef blade = EquippedWeapon;

            var request = new DamageRequest(
                this,
                target,
                blade != null ? blade.baseDamage : 0f,
                blade != null ? blade.damageType : DamageType.Slash,
                _resolvingAttack.stance,
                blade != null ? blade.material : WeaponMaterial.Steel,
                appliedOil,
                _flow.Links,
                isCritical: false); // Critico depende de CritChance, que entra com o equipamento

            // Reavaliado a cada golpe para que o interruptor do Inspector valha durante
            // a sessao, que e quando ele serve para alguma coisa.
            _pipeline.LoggingEnabled = logDamageBreakdown;

            DamageResult result = _pipeline.Deal(request);

            GrantHitstop(result.Amount);

            // Morte causada rende uma carga (docs/03 secao 7). A pergunta e feita depois do
            // golpe: antes dele o alvo estava de pe, senao nao teria sido acertado.
            if (target.IsDown && _vitals != null)
                _vitals.GainAdrenaline();

            if (_pipeline.LoggingEnabled)
                RecordSummary(target, result);
        }

        /// <summary>
        /// Soma o hitstop na contagem de quem resolve e confirma o acerto para o dono
        /// (tech/adr/0010). Uma vez por golpe, e nao por alvo: a postura Grupo acerta ate
        /// quatro, e somar quatro congelamentos faria dela o golpe mais lento do jogo.
        ///
        /// O dono recebe o numero pronto, e nao a postura. Quem somou os segundos a janela
        /// de Fluxo foi este lado, e e esse mesmo numero que o dono tem que segurar.
        /// </summary>
        void GrantHitstop(float damage)
        {
            if (_hitstopGranted || _resolvingAttack == null) return;

            _hitstopGranted = true;

            float hitstop = feedback != null ? feedback.HitstopFor(_resolvingAttack.stance) : 0f;

            // A metade do host da conta. A contagem que abre a janela de Fluxo anda o
            // mesmo tempo que o dono segura, e as duas pontas terminam o golpe juntas.
            _resolvingRemaining += hitstop;

            if (!IsSpawned || IsOwner) ConfirmHit(damage, hitstop);
            else HitConfirmedRpc(damage, hitstop);
        }

        // Server como permissao faz o NGO recusar confirmacao vinda de cliente. Sem isso,
        // um participante poderia congelar o golpe de outro quando quisesse.
        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        void HitConfirmedRpc(float damage, float hitstopSeconds) => ConfirmHit(damage, hitstopSeconds);

        void ConfirmHit(float damage, float hitstopSeconds) => HitConfirmed?.Invoke(damage, hitstopSeconds);

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
            // Os golpes sao da escola desde a tarefa 1.31. Sem escola no prefab, nao ha o que desenhar.
            AttackDef preview = AttackFor(Stance.Fast);
            if (preview == null) preview = AttackFor(Stance.Strong);
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
