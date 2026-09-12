using LoboBranco.Combat;
using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// O estado neutro: anda, corre e e o unico lugar de onde uma acao de combate comeca.
    ///
    /// Concentrar aqui a leitura do buffer tem uma consequencia boa: "posso atacar agora?"
    /// deixa de ser uma pergunta espalhada e vira "estou em Locomotion?". Um ataque que
    /// termina volta para ca, e o golpe guardado sai no frame seguinte sem que o estado
    /// de ataque precise conhecer o proximo ataque.
    /// </summary>
    public sealed class LocomotionState : PlayerStateBase
    {
        public override PlayerStateId Id => PlayerStateId.Locomotion;

        /// <summary>Locomocao nunca esta comprometida: tudo cancela andar.</summary>
        public override bool IsCommitted => false;

        public override void OnTick(PlayerStateContext context, float deltaTime)
        {
            if (context.Locomotion != null)
            {
                context.Locomotion.MoveInput = context.MoveInput;
                context.Locomotion.SprintHeld = context.SprintHeld;
            }

            TryStartBufferedAction(context);
        }

        public override void OnExit(PlayerStateContext context)
        {
            // Sair da locomocao sem zerar deixaria a ultima direcao gravada, e o jogador
            // continuaria deslizando durante a acao seguinte.
            if (context.Locomotion == null) return;

            context.Locomotion.MoveInput = Vector2.zero;
            context.Locomotion.SprintHeld = false;
        }

        static void TryStartBufferedAction(PlayerStateContext context)
        {
            InputBuffer buffer = context.Buffer;
            if (buffer == null || !buffer.HasPending) return;

            switch (buffer.Pending)
            {
                // Os dois botoes de ataque dao no mesmo golpe, o da postura corrente
                // (docs/03 secao 4). A tabela do documento tem uma linha por postura e
                // nao uma por botao, e a camada 2 do combate so existe porque e a postura
                // que decide o golpe. O que vai distinguir os dois botoes dentro de uma
                // mesma postura e uma pergunta de design ainda em aberto.
                case BufferedAction.AttackLight:
                case BufferedAction.AttackHeavy:
                    TryAttack(context, context.CurrentAttack, buffer.Pending);
                    break;

                // Esquiva, aparo e sinal sao as tarefas 1.10, 1.11 e 1.18. Ate la o
                // buffer guarda o input e ele expira sozinho, sem virar transicao.
            }
        }

        static void TryAttack(PlayerStateContext context, AttackDef attack, BufferedAction action)
        {
            if (attack == null) return;

            // Sem vigor o golpe nao comeca, e o input continua guardado: dentro da janela
            // de 0,2 s do buffer, o golpe sai sozinho assim que o vigor voltar. E por isso
            // que faltar vigor parece atraso e nao parece input perdido (docs/03 secao 7).
            if (context.Vitals != null && !context.Vitals.CanAfford(context.AttackStaminaCost)) return;

            // Consumir so depois de saber que a transicao aconteceu: se o ataque for
            // recusado, o input continua guardado e tenta de novo no frame seguinte,
            // que e exatamente o que o jogador espera de um buffer.
            context.PendingAttack = attack;

            if (context.Machine.TryChangeState(PlayerStateId.Attack))
                context.Buffer.TryConsume(action);
            else
                context.PendingAttack = null;
        }
    }
}
