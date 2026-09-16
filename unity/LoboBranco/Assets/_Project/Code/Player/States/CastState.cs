using LoboBranco.Combat;
using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// Uma habilidade, do inicio da conjuracao ao fim da recuperacao (tarefa 1.32).
    ///
    /// Como o <see cref="AttackState"/>, o estado nao sabe qual habilidade roda: ele recebe
    /// uma vaga, pergunta o asset a quem conjura e obedece os tempos escritos nele. Um sinal
    /// do Grifo e um do Lobo passam por aqui sem nenhum <c>if</c> de escola (regra 7 do
    /// CLAUDE.md).
    ///
    /// Comprometido: enquanto roda, so a esquiva entra (docs/03 secao 1). O compromisso tambem
    /// segura a rede. O dono so pede de novo quando o estado acaba, e ate la a recarga que o
    /// host armou ja chegou replicada (tech/adr/0011).
    ///
    /// O estado nao cobra nada e nao conta recarga. Ele diz quando comecou, quando o efeito
    /// sai e quando foi cortado; quem cobra, arma e decide e o host, do outro lado do contrato.
    /// </summary>
    public sealed class CastState : PlayerStateBase
    {
        public override PlayerStateId Id => PlayerStateId.CastSign;

        public override bool IsCommitted => true;

        AbilityDef _ability;
        float _elapsed;
        bool _triggered;

        // Verdadeiro quando nao ha mais nada para cortar do lado de quem resolve: o efeito ja
        // saiu, o host ja recusou, ou nunca houve pedido.
        bool _settled;

        // ---------------------------------------------------------------- leitura

        /// <summary>Habilidade em execucao. O painel de debug mostra isto.</summary>
        public AbilityDef CurrentAbility => _ability;

        public float Elapsed => _elapsed;

        /// <summary>Verdadeiro depois do instante do efeito.</summary>
        public bool Triggered => _triggered;

        // ------------------------------------------------------------------ ciclo

        public override void OnEnter(PlayerStateContext context)
        {
            int slot = context.PendingAbilitySlot;
            context.PendingAbilitySlot = PlayerStateContext.NoAbilitySlot;
            context.CastRefused = false;

            _ability = context.Abilities?.AbilityAt(slot);
            _elapsed = 0f;
            _triggered = false;
            _settled = _ability == null;

            if (context.Locomotion != null)
            {
                // Conjurar para o bruxo, como golpear. As maos ocupadas sao o custo.
                context.Locomotion.MoveInput = Vector2.zero;
                context.Locomotion.SprintHeld = false;
            }

            if (_ability != null)
                context.Abilities.BeginCast(slot);
        }

        public override void OnTick(PlayerStateContext context, float deltaTime)
        {
            // Sem asset nao ha o que conjurar. Sair aqui e nao no OnEnter pelo mesmo motivo do
            // golpe: trocar de estado dentro do OnEnter reentra na maquina no meio da transicao.
            if (_ability == null)
            {
                Finish(context);
                return;
            }

            // O host recusou. Ele ja esqueceu este pedido, entao cortar nao manda nada, e o
            // efeito nao sai nem se o tempo de conjurar tiver passado na tela do dono.
            if (context.CastRefused)
            {
                _settled = true;
                Finish(context);
                return;
            }

            _elapsed += deltaTime;

            if (!_triggered && _elapsed >= _ability.castTime)
            {
                _triggered = true;
                _settled = true;
                context.Abilities.TriggerCast();
            }

            if (_elapsed >= _ability.TotalDuration)
                Finish(context);
        }

        public override void OnExit(PlayerStateContext context)
        {
            // Cortado antes do efeito, pela esquiva ou pelo mundo. Vigor e recarga ficam pagos,
            // como o vigor do golpe cortado: devolver faria da esquiva um jeito de ensaiar sinal
            // de graca.
            if (!_settled)
                context.Abilities?.CancelCast();

            _ability = null;
            _settled = true;
            context.CastRefused = false;
        }

        /// <summary>Terminar nao e ser interrompido, entao a saida e forcada. Ver <see cref="AttackState"/>.</summary>
        static void Finish(PlayerStateContext context)
            => context.Machine.ForceChangeState(PlayerStateId.Locomotion);
    }
}
