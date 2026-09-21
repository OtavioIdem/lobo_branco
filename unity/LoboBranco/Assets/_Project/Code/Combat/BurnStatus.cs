using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Autoridade: o host (ADR 0008, tech/adr/0012). So quem resolve acende e conta os tiques, e o
    /// dano de cada tique chega nas outras maquinas pela vida replicada, como o de um golpe.
    ///
    /// <b>O estado da queimadura nao viaja.</b> Hoje ninguem alem do host tem o que fazer com ele:
    /// a vida ja mostra o dano, e o fogo visivel e arte do M4. Quando o fogo tiver efeito visual,
    /// ou quando a regeneracao da Besta precisar saber que o Igni a interrompe (docs/03 secao 10),
    /// ele vira instantes replicados como o <see cref="ControlState"/>. Replicar agora seria mandar
    /// o que nenhuma maquina le.
    ///
    /// O relogio e o local, e nao o do servidor, pelo mesmo motivo: quem conta e so o host, e nenhum
    /// instante daqui sai desta maquina.
    ///
    /// O tique passa pela resistencia a fogo e nao pela armadura. A armadura e subtracao plana
    /// com piso de 1, e contra ela 4 por tique viraria 2 no barghest e 1 no alghoul de 12 de
    /// armadura: a queimadura seria o pior efeito justamente contra quem o fogo deveria abrir.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterVitals))]
    public sealed class BurnStatus : MonoBehaviour
    {
        CharacterVitals _vitals;
        IDamageable _damageable;
        BurnState _state;

        // Quem acendeu por ultimo. Morte pela queimadura e morte causada por ele (docs/03 secao 7).
        CharacterVitals _source;

        double Now => Time.timeAsDouble;

        /// <summary>Queimando agora. So vale no host.</summary>
        public bool IsBurning => _state.IsBurning(Now);

        /// <summary>Dano de cada tique, antes da resistencia. Zero sem queimadura.</summary>
        public float DamagePerTick => IsBurning ? _state.DamagePerTick : 0f;

        /// <summary>Segundos ate apagar, nunca negativo.</summary>
        public float Remaining => IsBurning ? (float)(_state.Until - Now) : 0f;

        void Awake()
        {
            _vitals = GetComponent<CharacterVitals>();
            _damageable = GetComponent<IDamageable>();
        }

        /// <summary>
        /// Acende, ou renova pela regra do <see cref="BurnState"/>. So o host chama. Devolve
        /// verdadeiro quando isso mudou alguma coisa.
        /// </summary>
        public bool Ignite(float damagePerSecond, float seconds, float tickSeconds, CharacterVitals source)
        {
            if (!_vitals.CanResolve)
            {
                Debug.LogError($"{name}: cliente tentou acender queimadura. Dano e do host (ADR 0008).", this);
                return false;
            }

            if (_vitals.IsDown) return false;

            if (!_state.Ignite(Now, damagePerSecond, seconds, tickSeconds)) return false;

            _source = source;
            return true;
        }

        void Update()
        {
            if (_state.Until <= 0d) return;

            Step(Now);
        }

        /// <summary>
        /// Aplica os tiques vencidos ate <paramref name="now"/>, no relogio desta maquina. O
        /// <c>Update</c> chama com a hora; o teste chama com a hora que quer, para nao esperar.
        /// </summary>
        public void Step(double now)
        {
            if (!_vitals.CanResolve) return;

            int ticks = _state.ConsumeTicks(now);
            if (ticks > 0) Burn(ticks);

            if (!_state.IsBurning(now)) Extinguish();
        }

        void Burn(int ticks)
        {
            if (_vitals.IsDown)
            {
                Extinguish();
                return;
            }

            float resistance = _damageable != null ? _damageable.GetResistance(DamageType.Fire) : 1f;
            float amount = _state.DamagePerTick * ticks * resistance;
            if (amount <= 0f) return;

            if (_damageable != null)
                _damageable.ApplyDamage(new DamageResult(amount, DamageType.Fire, false, resistance));
            else
                _vitals.ApplyDamage(amount);

            if (!_vitals.IsDown) return;

            if (_source != null && _source != _vitals) _source.GainAdrenaline();
            Extinguish();
        }

        void Extinguish()
        {
            _state.Clear();
            _source = null;
        }
    }
}
