using System;
using LoboBranco.Stats;
using Unity.Netcode;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Autoridade: o host decide, todos veem (ADR 0008). So quem resolve atordoa, derruba e
    /// lentifica, porque so ele resolve o sinal que causa isso. O estado chega nas outras
    /// maquinas como instantes no relogio do servidor, e cada uma calcula sozinha se ainda vale.
    ///
    /// Ele mora em componente, e nao no grafo de behavior tree, pela ADR 0009: um no so roda
    /// enquanto o galho dele esta ativo, e uma criatura atordoada so no ramo de combate seguiria
    /// andando no ramo de patrulha. Quem obedece o controle sao o <c>EnemyAgent</c>, que para de
    /// andar e de girar, e o <c>EnemyMeleeAttacker</c>, que corta o golpe. Por isso o controle
    /// funciona antes de o grafo do barghest existir.
    ///
    /// Mora no modulo de combate, e nao no de IA, porque o bruxo tambem vai ser atordoado quando
    /// a tarefa 1.10 der a ele o estado de cambalear. O problema e um so, como a vida.
    ///
    /// A lentidao vira modificador de <c>MoveSpeed</c> na folha de atributos, em todas as
    /// maquinas e pelo mesmo estado replicado. Assim a folha do cliente nao diverge da do host, e
    /// pocao e talento que mexerem em velocidade no M2 somam com ela sem caso especial.
    ///
    /// Sem rede, este componente e o proprio host e o relogio e o local (risco X8 do doc 13).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterVitals))]
    public sealed class ControlStatus : NetworkBehaviour
    {
        // Server como permissao faz o NGO recusar controle escrito por cliente, como a vida.
        readonly NetworkVariable<ControlState> _replicated = new NetworkVariable<ControlState>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        // O estado enquanto nao ha rede, e o espelho do replicado depois: uma sessao que cai
        // continua com o controle que tinha.
        ControlState _solo;

        CharacterVitals _vitals;
        float _appliedSlow;
        bool _warnedMissingSpeed;

        // ---------------------------------------------------------------- leitura

        /// <summary>Quem escreve: o host, ou eu mesmo sem rede.</summary>
        public bool CanResolve => !IsSpawned || IsServer;

        double Now => IsSpawned ? NetworkManager.ServerTime.Time : Time.timeAsDouble;

        ControlState State => IsSpawned ? _replicated.Value : _solo;

        /// <summary>Atordoada ou derrubada. Nao anda, nao gira e nao golpeia.</summary>
        public bool IsIncapacitated => State.IsIncapacitated(Now);

        public ControlKind Incapacitation => State.Incapacitation(Now);

        public float IncapacitatedRemaining => State.IncapacitatedRemaining(Now);

        /// <summary>Fracao da velocidade tirada agora, de 0 a 1.</summary>
        public float SlowFraction => State.SlowAt(Now);

        /// <summary>
        /// Segundos ate a lentidao passar. A armadilha da tarefa 1.18f reaplica em pulsos curtos, e
        /// e por aqui que se ve que quem saiu de dentro dela volta a correr sozinho.
        /// </summary>
        public float SlowRemaining => State.SlowRemainingAt(Now);

        /// <summary>
        /// O estado mudou, em todas as maquinas. O fim natural nao dispara nada: cada maquina
        /// chega nele sozinha pelo relogio, e quem precisa saber pergunta.
        /// </summary>
        public event Action Changed;

        // ------------------------------------------------------------- autoridade

        /// <summary>Atordoa. Devolve verdadeiro so quando isso mudou alguma coisa. So o host chama.</summary>
        public bool ApplyStun(float seconds)
        {
            if (!CanWrite(nameof(ApplyStun))) return false;

            ControlState state = State;
            return state.Stun(Now, seconds) && Write(state);
        }

        /// <summary>Derruba. Devolve verdadeiro so quando isso mudou alguma coisa. So o host chama.</summary>
        public bool ApplyKnockdown(float seconds)
        {
            if (!CanWrite(nameof(ApplyKnockdown))) return false;

            ControlState state = State;
            return state.KnockDown(Now, seconds) && Write(state);
        }

        /// <summary>Lentifica. A mais forte manda. So o host chama.</summary>
        public bool ApplySlow(float fraction, float seconds)
        {
            if (!CanWrite(nameof(ApplySlow))) return false;

            WarnIfSpeedCannotSlow();

            ControlState state = State;
            return state.Slow(Now, fraction, seconds) && Write(state);
        }

        /// <summary>Encerra tudo agora. So o host chama.</summary>
        public void ClearAll()
        {
            if (!CanResolve) return;

            ControlState state = State;
            state.Clear();
            Write(state);
        }

        bool CanWrite(string action)
        {
            if (!CanResolve)
            {
                Debug.LogError($"{name}: cliente tentou {action}. Controle e do host (ADR 0008).", this);
                return false;
            }

            // Quem ja caiu nao tem controle a perder, e guardar um atordoamento nela faria a
            // capsula de sandbox levantar atordoada.
            return _vitals == null || !_vitals.IsDown;
        }

        bool Write(ControlState state)
        {
            if (IsSpawned)
            {
                // O evento sai do OnValueChanged, que dispara aqui e nas outras maquinas.
                _replicated.Value = state;
                return true;
            }

            _solo = state;
            Changed?.Invoke();
            return true;
        }

        // ------------------------------------------------------------------ ciclo

        void Awake()
        {
            _vitals = GetComponent<CharacterVitals>();
        }

        void OnEnable()
        {
            if (_vitals != null) _vitals.VitalityChanged += OnVitalityChanged;
        }

        void OnDisable()
        {
            if (_vitals != null) _vitals.VitalityChanged -= OnVitalityChanged;
        }

        public override void OnNetworkSpawn()
        {
            _replicated.OnValueChanged += OnReplicatedChanged;

            if (!IsServer) return;

            // O relogio de referencia deixa de ser o local e passa a ser o do servidor.
            _solo.Rebase(NetworkManager.ServerTime.Time - Time.timeAsDouble);
            _replicated.Value = _solo;
        }

        public override void OnNetworkDespawn()
        {
            _replicated.OnValueChanged -= OnReplicatedChanged;
        }

        /// <summary>
        /// Roda em todas as maquinas. A lentidao so e reescrita na folha quando a fracao muda, e
        /// o fim dela e percebido aqui, sem mensagem: a folha fica suja uma vez por mudanca, e nao
        /// uma por quadro.
        /// </summary>
        void Update()
        {
            StatSheet stats = _vitals != null ? _vitals.Stats : null;
            if (stats == null) return;

            float slow = SlowFraction;
            if (Mathf.Approximately(slow, _appliedSlow)) return;

            stats.RemoveAllFromSource(this);

            if (slow > 0f)
                stats.AddModifier(StatModifier.PercentMult(StatType.MoveSpeed, 1f - slow, this));

            _appliedSlow = slow;
        }

        // ---------------------------------------------------------------- interno

        void OnReplicatedChanged(ControlState previous, ControlState current)
        {
            _solo = current;
            Changed?.Invoke();
        }

        void OnVitalityChanged(float before, float after)
        {
            if (after <= 0f && CanResolve) ClearAll();
        }

        /// <summary>
        /// Um multiplicador sobre zero continua zero. Uma criatura cujo bloco de atributos nao
        /// lista <c>MoveSpeed</c> anda na velocidade da especie pelo neutro do <c>EnemyAgent</c>,
        /// e ignoraria toda lentidao sem erro nenhum. Avisar uma vez e o que liga o sintoma
        /// ("Yrden nao pega no barghest") a causa, que e um campo faltando num asset.
        /// </summary>
        void WarnIfSpeedCannotSlow()
        {
            if (_warnedMissingSpeed || _vitals == null || _vitals.Stats == null) return;
            if (_vitals.Stats.GetBase(StatType.MoveSpeed) > 0f) return;

            _warnedMissingSpeed = true;
            Debug.LogWarning($"{name}: bloco de atributos sem MoveSpeed. A lentidao nao tem sobre o que multiplicar.", this);
        }

#if UNITY_EDITOR
        // Para conferir as travas do inimigo no editor antes de existir sinal que as dispare.

        [ContextMenu("Debug/Atordoar 1,5 s")]
        void DebugStun() => ApplyStun(1.5f);

        [ContextMenu("Debug/Derrubar 2 s")]
        void DebugKnockdown() => ApplyKnockdown(2f);

        [ContextMenu("Debug/Lentificar 60% por 4 s")]
        void DebugSlow() => ApplySlow(0.6f, 4f);
#endif
    }
}
