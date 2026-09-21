using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// O Vigor do docs/03 secao 7: 100 de base, regenera 18/s fora de combate, 6/s em
    /// combate, e nada durante 1,5 s depois de cada gasto.
    ///
    /// O atraso de 1,5 s e o coracao do sistema. Sem ele o recurso vira um contador que
    /// sempre volta, e gastar deixa de ser escolha; com ele, cada gasto abre uma janela em
    /// que o bruxo esta sem rede de protecao. Sinal e defesa saem do mesmo bolso, e e esse
    /// o dilema que o documento chama de central.
    ///
    /// Classe pura, com o tempo por parametro, como o <see cref="FlowChain"/> e o
    /// <c>StanceSelector</c>: a curva de regeneracao inteira e verificavel em EditMode.
    ///
    /// Autoridade: de quem conta. Em rede isso e o host, porque o Vigor decide se uma
    /// acao acontece (doc 13 secao 6). O dono le a copia replicada para saber se pode
    /// pedir, e quem cobra e o host.
    /// </summary>
    public sealed class StaminaPool
    {
        readonly float _regenDelaySeconds;
        readonly float _combatMemorySeconds;

        float _regenBlockedFor;
        float _combatMemory;

        public StaminaPool(float regenDelaySeconds, float combatMemorySeconds)
        {
            _regenDelaySeconds = regenDelaySeconds;
            _combatMemorySeconds = combatMemorySeconds;
        }

        /// <summary>Teto, vindo do <c>StatSheet</c>. Mudar isto nao enche o que ja foi gasto.</summary>
        public float Max { get; private set; }

        public float Current { get; private set; }

        /// <summary>Regeneracao fora de combate, por segundo.</summary>
        public float RegenPerSecond { get; set; }

        /// <summary>Regeneracao em combate, por segundo. Bem menor, e e ela que aperta.</summary>
        public float CombatRegenPerSecond { get; set; }

        /// <summary>
        /// Verdadeiro por alguns segundos depois de gastar ou de apanhar. E uma heuristica
        /// ate o coordenador de encontro da tarefa 1.22 existir: e ele quem vai saber de
        /// verdade quando a luta comecou e acabou.
        /// </summary>
        public bool InCombat => _combatMemory > 0f;

        /// <summary>Verdadeiro durante os 1,5 s de silencio depois de um gasto.</summary>
        public bool RegenBlocked => _regenBlockedFor > 0f;

        public float Fraction => Max > 0f ? Current / Max : 0f;

        /// <summary>Define o teto e enche. Chamado quando a folha de atributos nasce.</summary>
        public void Reset(float max)
        {
            Max = Mathf.Max(0f, max);
            Current = Max;
            _regenBlockedFor = 0f;
            _combatMemory = 0f;
        }

        public bool CanAfford(float cost) => cost <= 0f || Current >= cost;

        /// <summary>
        /// Cobra o custo. Devolve falso e nao cobra nada quando falta vigor: meio gasto
        /// seria um bruxo que comeca o golpe e nao termina.
        /// </summary>
        public bool TrySpend(float cost)
        {
            if (!CanAfford(cost)) return false;

            if (cost > 0f)
            {
                Current -= cost;
                _regenBlockedFor = _regenDelaySeconds;
            }

            NoteCombat();
            return true;
        }

        /// <summary>Lembra que houve luta. Apanhar tambem conta, e nao so gastar.</summary>
        public void NoteCombat() => _combatMemory = _combatMemorySeconds;

        public void Tick(float deltaTime)
        {
            if (_combatMemory > 0f) _combatMemory -= deltaTime;

            if (_regenBlockedFor > 0f)
            {
                _regenBlockedFor -= deltaTime;
                return;
            }

            if (Current >= Max) return;

            float rate = InCombat ? CombatRegenPerSecond : RegenPerSecond;
            Current = Mathf.Min(Max, Current + rate * deltaTime);
        }

        /// <summary>Devolve vigor. E o que o segundo suspiro da tarefa 1.17 vai chamar.</summary>
        public void Restore(float amount)
        {
            if (amount <= 0f) return;

            Current = Mathf.Min(Max, Current + amount);
        }

        /// <summary>Usado quando a copia replicada manda. Ver <c>CharacterVitals</c>.</summary>
        public void Overwrite(float value) => Current = Mathf.Clamp(value, 0f, Max);
    }
}
