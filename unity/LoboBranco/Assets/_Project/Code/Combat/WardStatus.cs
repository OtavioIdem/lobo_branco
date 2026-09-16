using System;
using Unity.Netcode;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Autoridade: o host decide, todos veem (ADR 0008, tech/adr/0012). Quem ergue o escudo e o
    /// efeito de sinal, que so roda em quem resolve; quem o quebra e o <see cref="DamageReceiver"/>,
    /// que ja recusa dano fora do host. O estado viaja como o instante em que ele cai, no relogio do
    /// servidor, como a recarga e o controle.
    ///
    /// <b>Aqui o estado viaja, e na Queimadura nao.</b> A diferenca nao e de gosto: o escudo e uma
    /// decisao do jogador e ele precisa saber, na propria tela, se ainda esta protegido antes de
    /// entrar no alcance. A queimadura de um barghest ninguem consulta para decidir nada.
    ///
    /// Nao sabe o que e um sinal. E um absorvedor de dano, e a pocao de pele de pedra do M2 entra
    /// pela mesma porta.
    ///
    /// Sem rede, este componente e o proprio host e o relogio e o local (risco X8 do doc 13).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterVitals))]
    public sealed class WardStatus : NetworkBehaviour, IDamageAbsorber
    {
        readonly NetworkVariable<WardState> _replicated = new NetworkVariable<WardState>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        WardState _solo;
        CharacterVitals _vitals;
        DamageReceiver _receiver;

        // ---------------------------------------------------------------- leitura

        /// <summary>Quem ergue e quebra: o host, ou eu mesmo sem rede.</summary>
        public bool CanResolve => !IsSpawned || IsServer;

        double Now => IsSpawned ? NetworkManager.ServerTime.Time : Time.timeAsDouble;

        WardState State => IsSpawned ? _replicated.Value : _solo;

        /// <summary>O escudo esta de pe. Vale em todas as maquinas.</summary>
        public bool IsUp => State.IsUp(Now);

        /// <summary>Segundos ate cair sozinho, nunca negativo.</summary>
        public float Remaining => State.RemainingAt(Now);

        /// <summary>Fracao que voltaria para quem bater agora. Zero sem escudo.</summary>
        public float ReflectFraction => IsUp ? State.ReflectFraction : 0f;

        /// <summary>O escudo subiu ou caiu, em todas as maquinas.</summary>
        public event Action Changed;

        /// <summary>Quebrou por um golpe: quanto foi absorvido e quanto voltou. So em quem resolve.</summary>
        public event Action<float, float> Broken;

        // ------------------------------------------------------------- autoridade

        /// <summary>
        /// Ergue o escudo. So o host chama, e quem chama e o efeito de sinal. Devolve verdadeiro
        /// quando isso mudou alguma coisa.
        /// </summary>
        public bool Raise(float seconds, float reflectFraction)
        {
            if (!CanResolve)
            {
                Debug.LogError($"{name}: cliente tentou erguer escudo. Sinal e do host (ADR 0008).", this);
                return false;
            }

            if (_vitals != null && _vitals.IsDown) return false;

            WardState state = State;
            if (!state.Raise(Now, seconds, reflectFraction)) return false;

            Write(state);
            return true;
        }

        /// <summary>Derruba o escudo sem devolver nada. So o host chama.</summary>
        public void Drop()
        {
            if (!CanResolve || !IsUp) return;

            WardState state = State;
            state.Clear();
            Write(state);
        }

        // -------------------------------------------------------- IDamageAbsorber

        /// <summary>
        /// Engole um golpe inteiro e devolve a fracao para quem bateu. Um golpe, e nao uma
        /// quantidade de dano: o documento diz "absorve 1 golpe", entao o escudo vale o mesmo contra
        /// a garra de 8 e contra a investida de 80. E o que faz dele uma decisao de quando, e nao um
        /// colete de vida extra.
        /// </summary>
        public bool TryAbsorb(in DamageResult result)
        {
            if (!CanResolve || result.Amount <= 0f) return false;

            WardState state = State;
            if (!state.TryBreak(Now, result.Amount, out float reflected)) return false;

            Write(state);
            Reflect(result, reflected);

            Broken?.Invoke(result.Amount, reflected);
            return true;
        }

        /// <summary>
        /// Devolve o dano sem passar pelo pipeline. Ele ja foi resolvido uma vez contra este alvo, e
        /// rodar os estagios de novo aplicaria a armadura de quem bateu a um dano que ja e o que ele
        /// causou. O que volta e uma fracao do que chegou, e nada mais.
        /// </summary>
        void Reflect(in DamageResult result, float amount)
        {
            if (amount <= 0f || result.Attacker == null) return;

            // O atacante chega como contrato, e quem leva dano e o componente ao lado dele. E a
            // mesma subida de hierarquia que a hitbox faz, e nao existe caminho melhor: o pipeline
            // fala de quem bate e de quem apanha como duas coisas diferentes.
            if (result.Attacker is not Component attacker) return;
            if (!attacker.TryGetComponent(out IDamageable target)) target = attacker.GetComponentInParent<IDamageable>();
            if (target == null || target.IsDown) return;

            var mine = GetComponent<IDamageDealer>();
            target.ApplyDamage(new DamageResult(amount, result.Type, wasCritical: false, totalMultiplier: 1f, mine));
        }

        // ------------------------------------------------------------------ ciclo

        void Awake()
        {
            _vitals = GetComponent<CharacterVitals>();
            _receiver = GetComponent<DamageReceiver>();
        }

        void OnEnable()
        {
            if (_vitals != null) _vitals.VitalityChanged += OnVitalityChanged;

            // O escudo se anuncia, em vez de esperar ser procurado: a porta do dano nao tem como
            // achar quem so existir depois dela. Desligado, ele deixa de engolir golpe.
            if (_receiver != null) _receiver.SetAbsorber(this);
        }

        void OnDisable()
        {
            if (_vitals != null) _vitals.VitalityChanged -= OnVitalityChanged;

            if (_receiver != null) _receiver.ClearAbsorber(this);
        }

        public override void OnNetworkSpawn()
        {
            _replicated.OnValueChanged += OnReplicatedChanged;

            if (!IsServer) return;

            _solo.Rebase(NetworkManager.ServerTime.Time - Time.timeAsDouble);
            _replicated.Value = _solo;
        }

        public override void OnNetworkDespawn()
        {
            _replicated.OnValueChanged -= OnReplicatedChanged;
        }

        // ---------------------------------------------------------------- interno

        void Write(WardState state)
        {
            if (IsSpawned)
            {
                // O evento sai do OnValueChanged, que dispara aqui e nas outras maquinas.
                _replicated.Value = state;
                return;
            }

            _solo = state;
            Changed?.Invoke();
        }

        void OnReplicatedChanged(WardState previous, WardState current)
        {
            _solo = current;
            Changed?.Invoke();
        }

        void OnVitalityChanged(float before, float after)
        {
            if (after <= 0f && CanResolve) Drop();
        }
    }
}
