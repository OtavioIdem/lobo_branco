using LoboBranco.Combat;

namespace LoboBranco.Player
{
    /// <summary>
    /// Qual das tres posturas esta valendo, e os 0,25 s que a troca leva
    /// (docs/03 secao 4).
    ///
    /// A postura e a camada 2 do combate: a decisao que se toma por inimigo, e nao por
    /// segundo. O que faz dela uma decisao de verdade e o custo de mudar de ideia, entao a
    /// troca nao e instantanea e a postura nova so passa a valer quando o tempo acaba.
    /// Ate la o bruxo continua lutando na postura velha, o que e diferente de ficar
    /// desarmado: trocar de postura no meio da luta e uma aposta, nao uma punicao.
    ///
    /// Classe pura, com o tempo por parametro, pelo mesmo motivo do resto do combate: a
    /// troca e verificavel em EditMode sem cena, sem input e sem framerate.
    ///
    /// Autoridade: o dono. A postura e consequencia do input dele, como a maquina de
    /// estados (ADR 0008). O host descobre qual postura foi usada pelo proprio golpe que
    /// chega no pedido, porque cada <see cref="AttackDef"/> ja declara a sua: nao existe
    /// mensagem de postura, e por isso nao existe como as duas discordarem.
    /// </summary>
    public sealed class StanceSelector
    {
        const int StanceCount = 3;

        readonly float _switchSeconds;

        float _remaining;

        public StanceSelector(float switchSeconds, Stance initial = Stance.Fast)
        {
            _switchSeconds = switchSeconds;
            Current = initial;
            Pending = initial;
        }

        /// <summary>A postura que vale agora. E ela que escolhe o golpe.</summary>
        public Stance Current { get; private set; }

        /// <summary>Para onde a troca esta indo. Igual a <see cref="Current"/> quando nao ha troca.</summary>
        public Stance Pending { get; private set; }

        public bool IsSwitching => _remaining > 0f;

        /// <summary>Quanto falta da troca, em segundos. Zero quando nao ha troca.</summary>
        public float Remaining => _remaining > 0f ? _remaining : 0f;

        /// <summary>
        /// Pede uma postura especifica. Devolve falso quando nao ha o que fazer, que e o
        /// caso de pedir a postura que ja esta valendo ou a que ja esta a caminho.
        /// </summary>
        public bool Request(Stance next)
        {
            if (next == Pending) return false;

            Pending = next;
            _remaining = _switchSeconds;

            return true;
        }

        /// <summary>
        /// Anda na roda de posturas. Direcao positiva avanca, negativa volta.
        ///
        /// Anda a partir da postura que ja esta a caminho, e nao da atual: duas voltas
        /// rapidas da roda do mouse andam dois passos, que e o que a mao espera.
        /// </summary>
        public bool Cycle(int direction)
        {
            if (direction == 0) return false;

            int step = direction > 0 ? 1 : -1;
            int index = ((int)Pending + step + StanceCount) % StanceCount;

            return Request((Stance)index);
        }

        /// <summary>Conta o tempo da troca. No fim dele, a postura nova passa a valer.</summary>
        public void Tick(float deltaTime)
        {
            if (_remaining <= 0f) return;

            _remaining -= deltaTime;

            if (_remaining > 0f) return;

            _remaining = 0f;
            Current = Pending;
        }

        /// <summary>
        /// Desiste da troca em andamento e fica na postura atual. Existe para o dia em que
        /// algo do mundo interromper o bruxo no meio da troca (atordoamento, tarefa 1.23).
        /// </summary>
        public void Cancel()
        {
            _remaining = 0f;
            Pending = Current;
        }
    }
}
