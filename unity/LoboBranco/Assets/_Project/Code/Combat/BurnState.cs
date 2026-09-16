using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// A Queimadura de uma criatura (tarefa 1.18d): quanto tira por tique, de quanto em quanto
    /// tempo, e quando acaba. Classe pura com o relogio por parametro, como o
    /// <see cref="ControlState"/>, para a regra inteira ser verificada em EditMode sem esperar.
    ///
    /// <b>Dano por tique, e nao por quadro.</b> O docs/03 secao 8 fala em 4 por segundo, e um dano
    /// por quadro escreveria na vida replicada sessenta vezes por segundo. Um tique por segundo
    /// escreve uma vez, e o total dos 5 s e o mesmo.
    ///
    /// As regras de reaplicar sao as da lentidao: nunca soma, e a mais forte manda. Dois bruxos
    /// com fogo no mesmo barghest nao dobram a queimadura; a mais forte renova a duracao, e uma mais
    /// fraca nao entra enquanto a forte queima. Sem isso, a Queimadura seria o unico efeito do jogo
    /// que cresce com o numero de jogadores sem ninguem precisar se coordenar.
    /// </summary>
    public struct BurnState
    {
        /// <summary>
        /// Folga de relogio na conta dos tiques, dos dois lados da comparacao.
        ///
        /// Somar o intervalo cinco vezes em ponto flutuante pode passar do fim por um fio, e o ultimo
        /// tique sumiria. Pelo mesmo motivo, o tique agendado para o instante exato em que alguem
        /// pergunta tem que vencer: <c>T+1+1</c> e <c>T+2</c> sao o mesmo instante para quem joga e
        /// podem diferir no ultimo bit, e sem a folga a queimadura tira 4 em vez de 8 de vez em
        /// quando, dependendo de que horas a partida comecou.
        /// </summary>
        const double TickEpsilon = 0.0001d;

        public double Until;
        public double NextTickAt;
        public float DamagePerTick;
        public float TickSeconds;

        public bool IsBurning(double now) => now < Until;

        /// <summary>
        /// Acende ou renova. Devolve verdadeiro so quando algo mudou. O primeiro tique vem um
        /// intervalo depois de acender, e nao na hora: o dano do instante e o do proprio sinal.
        /// </summary>
        public bool Ignite(double now, float damagePerSecond, float seconds, float tickSeconds)
        {
            if (damagePerSecond <= 0f || seconds <= 0f || tickSeconds <= 0f) return false;

            float perTick = damagePerSecond * tickSeconds;
            double until = now + seconds;

            if (IsBurning(now))
            {
                if (perTick < DamagePerTick && !Mathf.Approximately(perTick, DamagePerTick)) return false;
                if (Mathf.Approximately(perTick, DamagePerTick) && until <= Until) return false;

                // Renovar ou fortalecer nao reinicia o compasso: quem ja estava queimando continua
                // levando o tique no mesmo ritmo.
                DamagePerTick = perTick;
                Until = until;
                return true;
            }

            DamagePerTick = perTick;
            TickSeconds = tickSeconds;
            Until = until;
            NextTickAt = now + tickSeconds;
            return true;
        }

        /// <summary>
        /// Quantos tiques venceram ate <paramref name="now"/>, e avanca o compasso. Um quadro longo
        /// pode vencer mais de um, e eles saem todos: queimar nao depende de framerate.
        /// </summary>
        public int ConsumeTicks(double now)
        {
            if (TickSeconds <= 0f) return 0;

            int ticks = 0;

            while (NextTickAt <= now + TickEpsilon && NextTickAt <= Until + TickEpsilon)
            {
                ticks++;
                NextTickAt += TickSeconds;
            }

            return ticks;
        }

        /// <summary>Apaga. A morte leva a queimadura junto.</summary>
        public void Clear() => this = default;
    }
}
