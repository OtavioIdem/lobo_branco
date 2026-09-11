using LoboBranco.Combat;
using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// Um golpe corpo a corpo, do inicio da anticipacao ao fim da recuperacao.
    ///
    /// O estado nao sabe se o golpe e leve ou forte: ele recebe um <see cref="AttackDef"/>
    /// e obedece a linha do tempo escrita no asset. E o que faz a tabela do docs/03
    /// secao 4 ser ajustavel com o jogo rodando, e o que vai permitir que a troca de
    /// postura da tarefa 1.14 seja uma troca de asset e nao um estado novo.
    ///
    /// O golpe e comprometido: enquanto ele roda, nada alem da esquiva entra
    /// (docs/03 secao 1, aplicada em <see cref="PlayerStateRules"/>).
    /// </summary>
    public sealed class AttackState : PlayerStateBase
    {
        public enum Phase
        {
            /// <summary>Anticipacao: a lamina esta subindo e nao acerta ninguem.</summary>
            Anticipation = 0,

            /// <summary>Janela de dano aberta.</summary>
            Active = 1,

            /// <summary>Recuperacao: o compromisso continua, mas o golpe nao acerta mais.</summary>
            Recovery = 2,
        }

        public override PlayerStateId Id => PlayerStateId.Attack;

        public override bool IsCommitted => true;

        AttackDef _attack;
        float _elapsed;
        bool _opened;
        bool _closed;

        // ---------------------------------------------------------------- leitura

        /// <summary>Golpe em execucao. O painel de debug mostra isto.</summary>
        public AttackDef CurrentAttack => _attack;

        public float Elapsed => _elapsed;

        public Phase CurrentPhase
        {
            get
            {
                if (_attack == null) return Phase.Recovery;
                if (_elapsed < _attack.HitboxOpenTime) return Phase.Anticipation;
                if (_elapsed < _attack.HitboxCloseTime) return Phase.Active;

                return Phase.Recovery;
            }
        }

        /// <summary>Verdadeiro entre a abertura e o fechamento da janela de dano.</summary>
        public bool HitboxOpen => _opened && !_closed;

        // ------------------------------------------------------------------ ciclo

        public override void OnEnter(PlayerStateContext context)
        {
            _attack = context.PendingAttack;
            context.PendingAttack = null;

            _elapsed = 0f;
            _opened = false;
            _closed = false;

            if (context.Locomotion != null)
            {
                // O compromisso do golpe e o que da peso a decisao de atacar: enquanto ele
                // roda, o jogador nao anda nem corre.
                context.Locomotion.MoveInput = Vector2.zero;
                context.Locomotion.SprintHeld = false;
            }

            if (_attack != null)
                context.Attacker?.BeginSwing(_attack);
        }

        public override void OnTick(PlayerStateContext context, float deltaTime)
        {
            // Sem asset de ataque nao ha o que executar. Sai no proprio tick e nao no
            // OnEnter, porque trocar de estado de dentro do OnEnter e reentrar na maquina
            // no meio de uma transicao.
            if (_attack == null)
            {
                Finish(context);
                return;
            }

            _elapsed += deltaTime;

            if (!_attack.hitboxFromAnimationEvent)
                DriveHitboxByTime(context);
            else if (HitboxOpen)
                context.Attacker?.TickHitbox();

            if (_elapsed >= _attack.TotalDuration)
                Finish(context);
        }

        public override void OnExit(PlayerStateContext context)
        {
            // A esquiva pode cortar o golpe com a janela aberta. Sem fechar aqui, a hitbox
            // ficaria ligada dentro do estado seguinte e o jogador acertaria enquanto esquiva.
            if (HitboxOpen)
            {
                _closed = true;
                context.Attacker?.CloseHitbox();
            }

            bool interrupted = _attack != null && _elapsed < _attack.TotalDuration;
            if (interrupted)
                context.Attacker?.CancelSwing();

            _attack = null;
        }

        // ---------------------------------------------------------------- interno

        /// <summary>
        /// A janela por tempo decorrido. Quando houver Animator, o clipe chama
        /// <c>OpenHitbox</c> e <c>CloseHitbox</c> e este metodo deixa de ser chamado:
        /// e o que <c>AttackDef.hitboxFromAnimationEvent</c> liga (tech/adr/0007).
        /// </summary>
        void DriveHitboxByTime(PlayerStateContext context)
        {
            if (!_opened && _elapsed >= _attack.HitboxOpenTime)
            {
                _opened = true;
                context.Attacker?.OpenHitbox();
            }

            if (!HitboxOpen) return;

            // A consulta acontece antes do fechamento de proposito: uma janela mais curta
            // que um frame ainda produz exatamente uma consulta, em vez de nenhuma.
            context.Attacker?.TickHitbox();

            if (_elapsed >= _attack.HitboxCloseTime)
            {
                _closed = true;
                context.Attacker?.CloseHitbox();
            }
        }

        /// <summary>
        /// Terminar nao e ser interrompido, entao a regra de ouro nao se aplica e a saida
        /// e forcada. Se fosse <c>TryChangeState</c>, o proprio <c>IsCommitted</c> deste
        /// estado recusaria a volta para a locomocao e o jogador ficaria preso no golpe.
        /// </summary>
        void Finish(PlayerStateContext context)
        {
            context.Machine.ForceChangeState(PlayerStateId.Locomotion);
        }
    }
}
