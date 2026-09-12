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
    public sealed class CharacterVitals : NetworkBehaviour
    {
        [Header("Dados")]
        [Tooltip("Valores base da criatura. docs/03 secao 12.")]
        [SerializeField] StatBlockDef statBlock;

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

        // ------------------------------------------------------------------ ciclo

        void Awake()
        {
            if (statBlock == null)
                Debug.LogWarning($"{name}: {nameof(CharacterVitals)} sem {nameof(StatBlockDef)}. Vida nasce zerada.", this);

            _stats = statBlock != null ? statBlock.CreateSheet() : new StatSheet();
            _soloVitality = MaxVitality;
        }

        public override void OnNetworkSpawn()
        {
            _replicatedVitality.OnValueChanged += OnReplicatedVitalityChanged;

            // O host semeia o valor inicial. O cliente recebe o estado no proprio spawn,
            // entao um jogador que entra no meio da luta ve a vida como ela esta.
            if (IsServer)
                _replicatedVitality.Value = _soloVitality;
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

            float before = CurrentVitality;
            Write(Mathf.Max(0f, before - amount));

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
