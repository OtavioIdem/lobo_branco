using LoboBranco.Combat;
using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// Um golpe corpo a corpo, do inicio da anticipacao ao fim da recuperacao.
    ///
    /// O estado nao sabe se o golpe e leve ou forte: ele recebe um <see cref="AttackDef"/>
    /// e obedece a linha do tempo escrita no asset. E o que faz a tabela do docs/03
    /// secao 4 ser ajustavel com o jogo rodando, e o que permite que a troca de postura
    /// da tarefa 1.14 seja uma troca de asset e nao um estado novo.
    ///
    /// Quem conta o tempo e o <see cref="AttackTimeline"/>, que mora no modulo de combate
    /// porque o monstro da tarefa 1.21 desfere o mesmo golpe pela mesma contagem. O que
    /// sobra aqui e o que e mesmo do jogador: o compromisso, que zera o movimento, e a
    /// volta para a locomocao quando o golpe acaba.
    ///
    /// O golpe e comprometido: enquanto ele roda, nada alem da esquiva entra
    /// (docs/03 secao 1, aplicada em <see cref="PlayerStateRules"/>).
    /// </summary>
    public sealed class AttackState : PlayerStateBase
    {
        public override PlayerStateId Id => PlayerStateId.Attack;

        public override bool IsCommitted => true;

        readonly AttackTimeline _timeline = new AttackTimeline();

        // ---------------------------------------------------------------- leitura

        /// <summary>Golpe em execucao. O painel de debug mostra isto.</summary>
        public AttackDef CurrentAttack => _timeline.Attack;

        public float Elapsed => _timeline.Elapsed;

        public AttackPhase CurrentPhase => _timeline.CurrentPhase;

        /// <summary>Verdadeiro entre a abertura e o fechamento da janela de dano.</summary>
        public bool HitboxOpen => _timeline.HitboxOpen;

        // ------------------------------------------------------------------ ciclo

        public override void OnEnter(PlayerStateContext context)
        {
            AttackDef attack = context.PendingAttack;
            context.PendingAttack = null;

            if (context.Locomotion != null)
            {
                // O compromisso do golpe e o que da peso a decisao de atacar: enquanto ele
                // roda, o jogador nao anda nem corre.
                context.Locomotion.MoveInput = Vector2.zero;
                context.Locomotion.SprintHeld = false;
            }

            _timeline.Begin(attack, context.Attacker);
        }

        public override void OnTick(PlayerStateContext context, float deltaTime)
        {
            // O hitstop pedido pelo host entra antes de o golpe andar, e nao depois: assim o
            // quadro em que ele chega ja e um quadro parado, e a extensao do golpe no dono
            // bate com a que o host somou do lado dele (tech/adr/0010).
            if (context.PendingHitstop > 0f)
            {
                _timeline.Hold(context.PendingHitstop);
                context.PendingHitstop = 0f;
            }

            // Sem asset de ataque nao ha o que executar, e a linha do tempo devolve falso
            // no primeiro passo. Sair aqui e nao no OnEnter e deliberado: trocar de estado
            // de dentro do OnEnter e reentrar na maquina no meio de uma transicao.
            if (!_timeline.Tick(deltaTime))
                Finish(context);
        }

        public override void OnExit(PlayerStateContext context)
        {
            _timeline.End();

            // Um hitstop que sobrou e de um golpe que acabou. Deixado no contexto, ele
            // congelaria o golpe seguinte, que nao acertou nada.
            context.PendingHitstop = 0f;
        }

        // ---------------------------------------------------------------- interno

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
