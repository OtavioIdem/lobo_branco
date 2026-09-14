using System;
using LoboBranco.Stats;
using Unity.Netcode;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Autoridade: o host (ADR 0008). A vida atual e escrita so por quem resolve dano e
    /// chega nas outras maquinas replicada. Cliente le, nunca escreve.
    ///
    /// E a folha de atributos de um personagem mais o unico numero dela que muda sozinho
    /// durante a luta. Ter as duas coisas no mesmo componente e deliberado: com a folha
    /// em um lugar e a vida em outro, uma pocao poderia subir a vitalidade maxima de uma
    /// copia da folha enquanto o dano le a outra, e o sintoma disso aparece semanas depois.
    ///
    /// Os valores base nao sao replicados, e isso nao e economia: eles vem do mesmo
    /// <see cref="StatBlockDef"/> em todas as maquinas, entao replicar seria mandar pela
    /// rede uma coisa que o outro lado ja tem em disco. O que precisa viajar e so o que
    /// diverge, e hoje isso e a vida. Quando pocoes e talentos entrarem (M2) eles vao
    /// nascer no host e os modificadores passam a viajar tambem.
    ///
    /// Sem rede ligada, este componente e o proprio host. Isso e o risco X8 do doc 13:
    /// a Sandbox_Combate tem que continuar jogavel sozinha, sem NetworkManager nenhum.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterVitals : NetworkBehaviour, IStaminaSource
    {
        [Header("Dados")]
        [Tooltip("Valores base da criatura. docs/03 secao 12.")]
        [SerializeField] StatBlockDef statBlock;

        [Tooltip("Tempos do Vigor. Sem asset, valem os do docs/03 secao 7.")]
        [SerializeField] CombatTuningDef tuning;

        // Server como permissao de escrita e o que faz a regra da ADR 0008 ser cobrada
        // pela biblioteca: um cliente que tentar escrever leva erro do proprio NGO, em
        // vez de divergir em silencio.
        readonly NetworkVariable<float> _replicatedVitality = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        // A vida enquanto nao ha rede. Escrever na NetworkVariable antes do spawn gera
        // aviso do NGO a cada golpe, entao o caminho solo tem o proprio campo e a
        // propriedade abaixo decide de qual dos dois se le.
        float _soloVitality;

        // O Vigor tem a mesma divisao da vida, com uma diferenca: ele muda todo quadro,
        // porque regenera. Por isso quem conta e a classe pura, e a variavel replicada so
        // recebe o valor quando ele andou o bastante para alguem perceber.
        readonly NetworkVariable<float> _replicatedStamina = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        /// <summary>
        /// Meio ponto de vigor ninguem ve, e escrever todo quadro encheria a rede com
        /// diferencas de 0,3. O cheio e o zero sempre passam, porque sao os dois valores
        /// em que a barra muda de significado.
        /// </summary>
        const float StaminaWriteEpsilon = 0.5f;

        // Adrenalina e inteira e pequena, entao ela cabe em um byte e nao precisa de
        // epsilon nenhum: ela muda de um em um, e cada mudanca importa.
        readonly NetworkVariable<byte> _replicatedAdrenaline = new NetworkVariable<byte>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        AdrenalinePool _adrenaline;
        StaminaPool _stamina;
        StatSheet _stats;

        // ---------------------------------------------------------------- leitura

        public StatSheet Stats => _stats;

        public float MaxVitality => _stats?.Get(StatType.MaxVitality) ?? 0f;

        public float CurrentVitality => IsSpawned ? _replicatedVitality.Value : _soloVitality;

        public bool IsDown => CurrentVitality <= 0f;

        /// <summary>
        /// Verdadeiro em quem pode escrever: o host, ou qualquer um quando nao ha rede.
        /// A ordem importa, porque <c>IsServer</c> le o NetworkManager e fora de rede
        /// nao existe NetworkManager para ler.
        /// </summary>
        public bool CanResolve => !IsSpawned || IsServer;

        /// <summary>
        /// Vida anterior e vida atual, em todas as maquinas. E por aqui que piscar,
        /// tombar e levantar acontecem: sao reacao local a um numero que o host mudou,
        /// e nao um RPC a mais (doc 13 secao 6).
        /// </summary>
        public event Action<float, float> VitalityChanged;

        // ------------------------------------------------------------------ vigor

        public float MaxStamina => _stats?.Get(StatType.MaxStamina) ?? 0f;

        public float CurrentStamina => IsSpawned ? _replicatedStamina.Value : _stamina.Current;

        /// <summary>Verdadeiro durante o silencio de 1,5 s depois de um gasto. Vale em quem conta.</summary>
        public bool StaminaRegenBlocked => _stamina.RegenBlocked;

        /// <summary>Heuristica de "esta lutando", ate a tarefa 1.22. Vale em quem conta.</summary>
        public bool InCombat => _stamina.InCombat;

        /// <summary>
        /// Se o vigor da para a acao. Quem pergunta e o dono, antes de pedir, e ele le a
        /// copia replicada: pode estar uma viagem de ida e volta atrasada, e em jogo
        /// cooperativo contra IA essa diferenca nao muda nada (ADR 0008).
        /// </summary>
        public bool CanAfford(float cost) => cost <= 0f || CurrentStamina >= cost;

        /// <summary>
        /// Cobra o vigor. So quem resolve chama, e o host sempre cobra o que tem: recusar
        /// um golpe que ja saiu na tela do dono trocaria um problema invisivel por um bem
        /// visivel. Devolve falso quando faltou, para quem quiser saber.
        /// </summary>
        public bool TrySpendStamina(float cost)
        {
            if (!CanResolve)
            {
                Debug.LogError($"{name}: cliente tentou gastar vigor. Recurso e do host (ADR 0008).", this);
                return false;
            }

            bool paid = _stamina.TrySpend(cost);
            WriteStamina(_stamina.Current, force: true);

            return paid;
        }

        /// <summary>Devolve vigor. Segundo suspiro e pocoes chamam isto.</summary>
        public void RestoreStamina(float amount)
        {
            if (!CanResolve) return;

            _stamina.Restore(amount);
            WriteStamina(_stamina.Current, force: true);
        }

        // ------------------------------------------------------------- adrenalina

        public int CurrentAdrenaline => IsSpawned ? _replicatedAdrenaline.Value : _adrenaline.Charges;

        public int MaxAdrenaline => _adrenaline.MaxCharges;

        public bool CanAffordAdrenaline(int cost) => cost <= 0 || CurrentAdrenaline >= cost;

        /// <summary>Uma carga por morte causada e por riposte (tarefa 1.11). So o host.</summary>
        public bool GainAdrenaline(int charges = 1)
        {
            if (!CanResolve) return false;

            bool gained = _adrenaline.Gain(charges);
            if (gained) WriteAdrenaline();

            return gained;
        }

        /// <summary>
        /// Avisa a corrente de Fluxo do golpe. A partir do quinto elo, a cada dois elos
        /// nasce uma carga (docs/03 secao 6). So o host conta elos, entao so ele ganha.
        /// </summary>
        public bool NoteFlowLinks(int links)
        {
            if (!CanResolve) return false;

            bool gained = _adrenaline.NoteFlowLinks(links);
            if (gained) WriteAdrenaline();

            return gained;
        }

        public bool TrySpendAdrenaline(int cost)
        {
            if (!CanResolve)
            {
                Debug.LogError($"{name}: cliente tentou gastar adrenalina. Recurso e do host (ADR 0008).", this);
                return false;
            }

            if (!_adrenaline.TrySpend(cost)) return false;

            WriteAdrenaline();
            return true;
        }

        /// <summary>
        /// Segundo suspiro: duas cargas viram 40 por cento do vigor maximo, na hora
        /// (docs/03 secao 7). E o unico dos tres gastos que ja da para existir: finalizacao
        /// depende de execucao e sinal reforcado depende dos sinais (tarefas 1.11 e 1.18).
        /// </summary>
        public bool TrySecondWind()
        {
            int cost = tuning != null ? tuning.secondWindCost : 2;
            float fraction = tuning != null ? tuning.secondWindStaminaFraction : 0.4f;

            if (!TrySpendAdrenaline(cost)) return false;

            RestoreStamina(MaxStamina * fraction);
            return true;
        }

        void WriteAdrenaline()
        {
            if (!IsSpawned) return;

            _replicatedAdrenaline.Value = (byte)Mathf.Clamp(_adrenaline.Charges, 0, byte.MaxValue);
        }

        // ------------------------------------------------------------------ ciclo

        void Awake()
        {
            StatBlockDef block = ResolveStatBlock();

            if (block == null)
                Debug.LogWarning($"{name}: {nameof(CharacterVitals)} sem {nameof(StatBlockDef)}. Vida nasce zerada.", this);

            _stats = block != null ? block.CreateSheet() : new StatSheet();
            _soloVitality = MaxVitality;

            _stamina = new StaminaPool(
                tuning != null ? tuning.staminaRegenDelay : 1.5f,
                tuning != null ? tuning.combatMemorySeconds : 5f);

            _adrenaline = new AdrenalinePool(
                tuning != null ? tuning.adrenalineMaxCharges : 3,
                tuning != null ? tuning.adrenalineFlowLinksForFirstCharge : 5,
                tuning != null ? tuning.adrenalineFlowLinksPerCharge : 2);

            RefreshStaminaFromStats();
        }

        /// <summary>
        /// Quem esta ao lado decide primeiro. No bruxo, e a escola (docs/13 secao 5.1); na
        /// criatura nao ha ninguem, e vale o campo deste componente. A pergunta e por
        /// <see cref="IStatBlockProvider"/> porque a escola mora num modulo acima deste, e o
        /// grafo de asmdef so aponta para baixo.
        /// </summary>
        StatBlockDef ResolveStatBlock()
        {
            var provider = GetComponent<IStatBlockProvider>();
            StatBlockDef provided = provider != null ? provider.StatBlock : null;

            return provided != null ? provided : statBlock;
        }

        /// <summary>
        /// Le teto e taxas da folha de atributos. Separado do <c>Awake</c> porque o dia em
        /// que uma pocao mexer em <c>MaxStamina</c>, e so chamar isto de novo.
        /// </summary>
        void RefreshStaminaFromStats()
        {
            _stamina.RegenPerSecond = _stats.Get(StatType.StaminaRegen);
            _stamina.CombatRegenPerSecond = _stats.Get(StatType.StaminaRegenInCombat);
            _stamina.Reset(MaxStamina);
        }

        /// <summary>
        /// So quem resolve conta o vigor. No cliente, a copia replicada e a verdade e o
        /// contador local ficaria inventando uma segunda.
        /// </summary>
        void Update()
        {
            if (!CanResolve) return;

            _stamina.Tick(Time.deltaTime);
            WriteStamina(_stamina.Current, force: false);
        }

        void WriteStamina(float value, bool force)
        {
            if (!IsSpawned) return;

            bool atEdge = value <= 0f || value >= MaxStamina;

            if (!force && !atEdge && Mathf.Abs(value - _replicatedStamina.Value) < StaminaWriteEpsilon)
                return;

            _replicatedStamina.Value = value;
        }

        public override void OnNetworkSpawn()
        {
            _replicatedVitality.OnValueChanged += OnReplicatedVitalityChanged;

            // O host semeia o valor inicial. O cliente recebe o estado no proprio spawn,
            // entao um jogador que entra no meio da luta ve a vida como ela esta.
            if (IsServer)
            {
                _replicatedVitality.Value = _soloVitality;
                _replicatedStamina.Value = _stamina.Current;
                _replicatedAdrenaline.Value = (byte)_adrenaline.Charges;
            }
        }

        public override void OnNetworkDespawn()
        {
            _replicatedVitality.OnValueChanged -= OnReplicatedVitalityChanged;
        }

        // ------------------------------------------------------------- autoridade

        /// <summary>
        /// Tira vida. So o host chama, porque so ele roda o <see cref="DamagePipeline"/>.
        /// Devolve o quanto saiu de fato, que e menos que o pedido quando o golpe mata.
        /// </summary>
        public float ApplyDamage(float amount)
        {
            if (!CanResolve)
            {
                Debug.LogError($"{name}: cliente tentou escrever vida. Dano e do host (ADR 0008).", this);
                return 0f;
            }

            if (amount <= 0f || IsDown) return 0f;

            // Apanhar e luta tanto quanto bater: sem isto, quem so defende regeneraria
            // vigor na taxa de fora de combate no meio da briga.
            _stamina.NoteCombat();

            float before = CurrentVitality;
            Write(Mathf.Max(0f, before - amount));

            // A adrenalina e o que a luta rendeu ate aqui, e cair encerra a conta.
            if (IsDown)
            {
                _adrenaline.Clear();
                WriteAdrenaline();
            }

            return before - CurrentVitality;
        }

        /// <summary>Devolve a vida cheia. So o host chama.</summary>
        public void RestoreToFull()
        {
            if (!CanResolve)
            {
                Debug.LogError($"{name}: cliente tentou curar. Vida e do host (ADR 0008).", this);
                return;
            }

            Write(MaxVitality);

            // Quem volta de pe volta inteiro. Levantar com o vigor de quando caiu faria a
            // capsula de sandbox reviver sem poder fazer nada.
            _stamina.Reset(MaxStamina);
            WriteStamina(_stamina.Current, force: true);
        }

        // ---------------------------------------------------------------- interno

        void Write(float value)
        {
            if (IsSpawned)
            {
                // O evento sai do OnValueChanged, que dispara aqui e nas outras maquinas.
                _replicatedVitality.Value = value;
                return;
            }

            float before = _soloVitality;
            if (Mathf.Approximately(before, value)) return;

            _soloVitality = value;
            VitalityChanged?.Invoke(before, value);
        }

        void OnReplicatedVitalityChanged(float before, float after)
        {
            // Mantido em dia para que uma sessao que cai continue com a vida certa em vez
            // de voltar para a cheia.
            _soloVitality = after;

            VitalityChanged?.Invoke(before, after);
        }
    }
}
