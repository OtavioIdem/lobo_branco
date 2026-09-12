namespace LoboBranco.Combat
{
    /// <summary>Onde um golpe esta na propria linha do tempo.</summary>
    public enum AttackPhase
    {
        /// <summary>A lamina esta subindo e nao acerta ninguem. E o telegrafo do docs/03 secao 10.</summary>
        Anticipation = 0,

        /// <summary>Janela de dano aberta.</summary>
        Active = 1,

        /// <summary>O compromisso continua, mas o golpe nao acerta mais.</summary>
        Recovery = 2,
    }

    /// <summary>
    /// A linha do tempo de um golpe corpo a corpo, do inicio da anticipacao ao fim da
    /// recuperacao, dirigida pelos tempos do <see cref="AttackDef"/>.
    ///
    /// Existe como classe propria porque o bruxo e o monstro desferem o mesmo golpe. Ate
    /// aqui esta contagem morava dentro do <c>AttackState</c>, e copiar as quinze linhas
    /// dela para a IA da tarefa 1.21 criaria duas linhas do tempo que envelheceriam
    /// separadas: o dia em que o Animator entrar (tech/adr/0007), so uma das duas mudaria,
    /// e o sintoma seria "o inimigo acerta antes da animacao" sem erro nenhum no Console.
    ///
    /// Classe pura, sem MonoBehaviour e com o tempo entrando por parametro, pelo mesmo
    /// motivo do resto do combate: a janela de dano e verificavel em EditMode, em passos
    /// fixos, sem cena e sem framerate.
    ///
    /// Autoridade: nenhuma. Ela so conta o tempo e avisa quem empunha a arma. Quem decide
    /// se o golpe acontece e quem chama, e quem resolve o dano e sempre o host (ADR 0008).
    /// </summary>
    public sealed class AttackTimeline
    {
        AttackDef _attack;
        IMeleeAttacker _attacker;
        float _elapsed;
        bool _opened;
        bool _closed;

        // ---------------------------------------------------------------- leitura

        /// <summary>Golpe em execucao, ou nulo entre golpes.</summary>
        public AttackDef Attack => _attack;

        /// <summary>Segundos desde o inicio do golpe.</summary>
        public float Elapsed => _elapsed;

        /// <summary>Verdadeiro enquanto o golpe ainda nao chegou ao fim da recuperacao.</summary>
        public bool Running => _attack != null && _elapsed < _attack.TotalDuration;

        /// <summary>Verdadeiro entre a abertura e o fechamento da janela de dano.</summary>
        public bool HitboxOpen => _opened && !_closed;

        /// <summary>
        /// Fase corrente. Sem golpe em execucao vale <see cref="AttackPhase.Recovery"/>,
        /// que e o estado em que ninguem esta comprometido nem acertando nada.
        /// </summary>
        public AttackPhase CurrentPhase
        {
            get
            {
                if (_attack == null) return AttackPhase.Recovery;
                if (_elapsed < _attack.HitboxOpenTime) return AttackPhase.Anticipation;
                if (_elapsed < _attack.HitboxCloseTime) return AttackPhase.Active;

                return AttackPhase.Recovery;
            }
        }

        // ------------------------------------------------------------------ ciclo

        /// <summary>
        /// Comeca um golpe. Golpe nulo e aceito de proposito: quem chamou fica com um
        /// <see cref="Running"/> falso no primeiro <see cref="Tick"/> e devolve o controle,
        /// em vez de travar esperando uma linha do tempo que nao existe.
        /// </summary>
        public void Begin(AttackDef attack, IMeleeAttacker attacker)
        {
            _attack = attack;
            _attacker = attacker;
            _elapsed = 0f;
            _opened = false;
            _closed = false;

            if (_attack != null)
                _attacker?.BeginSwing(_attack);
        }

        /// <summary>
        /// Envelhece o golpe. Devolve falso quando ele terminou, e terminar inclui o caso
        /// de nunca ter comecado.
        /// </summary>
        public bool Tick(float deltaTime)
        {
            if (_attack == null) return false;

            _elapsed += deltaTime;

            if (!_attack.hitboxFromAnimationEvent)
                DriveHitboxByTime();
            else if (HitboxOpen)
                _attacker?.TickHitbox();

            return _elapsed < _attack.TotalDuration;
        }

        /// <summary>
        /// Encerra o golpe corrente e limpa o estado. Fecha a janela se ela ficou aberta,
        /// e avisa o cancelamento quando o golpe foi cortado antes do fim.
        ///
        /// A esquiva pode cortar um golpe com a janela aberta (docs/03 secao 2). Sem o
        /// fechamento daqui a hitbox ficaria ligada dentro do estado seguinte, e o
        /// personagem acertaria enquanto esquiva.
        /// </summary>
        public void End()
        {
            if (HitboxOpen)
            {
                _closed = true;
                _attacker?.CloseHitbox();
            }

            if (_attack != null && _elapsed < _attack.TotalDuration)
                _attacker?.CancelSwing();

            _attack = null;
            _attacker = null;
        }

        // ---------------------------------------------------------------- interno

        /// <summary>
        /// A janela por tempo decorrido. Quando houver Animator, o clipe chama
        /// <c>OpenHitbox</c> e <c>CloseHitbox</c> e este metodo deixa de ser chamado:
        /// e o que <see cref="AttackDef.hitboxFromAnimationEvent"/> liga (tech/adr/0007).
        /// </summary>
        void DriveHitboxByTime()
        {
            if (!_opened && _elapsed >= _attack.HitboxOpenTime)
            {
                _opened = true;
                _attacker?.OpenHitbox();
            }

            if (!HitboxOpen) return;

            // A consulta acontece antes do fechamento de proposito: uma janela mais curta
            // que um frame ainda produz exatamente uma consulta, em vez de nenhuma.
            _attacker?.TickHitbox();

            if (_elapsed >= _attack.HitboxCloseTime)
            {
                _closed = true;
                _attacker?.CloseHitbox();
            }
        }
    }
}
