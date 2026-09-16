using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>O telegrafo de um golpe em um instante: quanto avisar e quanto acender.</summary>
    public readonly struct TelegraphSample
    {
        public readonly AttackPhase Phase;

        /// <summary>Intensidade do aviso, de 0 a 1. Cresce durante a anticipacao.</summary>
        public readonly float Warning;

        /// <summary>Intensidade do golpe, de 0 a 1. Acende na janela de dano e apaga na recuperacao.</summary>
        public readonly float Flash;

        /// <summary>Verdadeiro quando o golpe acabou, ou nunca existiu.</summary>
        public readonly bool Finished;

        public TelegraphSample(AttackPhase phase, float warning, float flash, bool finished)
        {
            Phase = phase;
            Warning = warning;
            Flash = flash;
            Finished = finished;
        }

        public static readonly TelegraphSample Idle = new TelegraphSample(AttackPhase.Recovery, 0f, 0f, true);
    }

    /// <summary>
    /// O telegrafo de um golpe como funcao do tempo decorrido (docs/03 secao 10).
    ///
    /// A regra que importa e a do aviso: ele <b>cresce</b> ate a janela abrir, em vez de
    /// apenas ligar. Um aviso que so liga diz <em>que</em> o golpe vem; um aviso que enche
    /// diz <em>quando</em>, e quando e a informacao de que a esquiva da tarefa 1.10 precisa.
    /// Ele tambem comeca ja visivel, e nao do zero: um aviso que nasce apagado so vira algo
    /// perceptivel no meio da anticipacao, e a metade que sobra e curta demais para reagir.
    ///
    /// Os instantes saem do mesmo <see cref="AttackDef"/> que dirige a hitbox, e isso e o
    /// que garante que o aviso acaba exatamente quando o golpe passa a acertar. Um tempo de
    /// telegrafo em asset separado poderia discordar da hitbox, e o sintoma seria o
    /// jogador aprendendo o tempo errado.
    ///
    /// Classe pura e sem estado, com o tempo por parametro. E o que deixa o host e o
    /// cliente calcularem o mesmo aviso a partir de um unico instante de inicio, sem que a
    /// curva precise viajar pela rede.
    /// </summary>
    public static class TelegraphCurve
    {
        /// <param name="elapsed">
        /// Segundos desde o inicio do golpe, no relogio do servidor. Negativo e aceito: a
        /// estimativa de tempo do cliente pode ficar um pouco atras do carimbo do host.
        /// </param>
        /// <param name="startWarning">Intensidade do aviso no primeiro quadro, de 0 a 1.</param>
        public static TelegraphSample Evaluate(AttackDef attack, float elapsed, float startWarning)
        {
            if (attack == null) return TelegraphSample.Idle;

            // Relogio atrasado conta como inicio do golpe, e nao como antes dele. Tratar
            // como antes apagaria o aviso justamente no cliente, que ja recebe tudo tarde.
            if (elapsed < 0f) elapsed = 0f;

            if (elapsed >= attack.TotalDuration) return TelegraphSample.Idle;

            float open = attack.HitboxOpenTime;
            float close = attack.HitboxCloseTime;

            if (elapsed < open)
            {
                float progress = elapsed / open;
                float warning = Mathf.Lerp(Mathf.Clamp01(startWarning), 1f, progress);

                return new TelegraphSample(AttackPhase.Anticipation, warning, 0f, false);
            }

            if (elapsed < close)
                return new TelegraphSample(AttackPhase.Active, 1f, 1f, false);

            // O golpe apaga durante a recuperacao em vez de sumir de uma vez. E a unica pista
            // de que a criatura esta comprometida, e comprometida e quando se contra-ataca.
            float recovery = attack.TotalDuration - close;
            float fade = recovery > 0f ? 1f - (elapsed - close) / recovery : 0f;

            return new TelegraphSample(AttackPhase.Recovery, 0f, Mathf.Clamp01(fade), false);
        }
    }
}
