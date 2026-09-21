using LoboBranco.Combat;
using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// Guardar uma espada e sacar a outra: 0,7 s em que o bruxo nao ataca, nao anda e nao
    /// esquiva (docs/03 secao 3).
    ///
    /// E o estado mais rigido do jogo, e de proposito. A camada 1 do combate e uma decisao
    /// binaria de altissimo impacto, porque aco em monstro da 0,35x: se trocar fosse
    /// barato, nao haveria decisao nenhuma, bastaria trocar sempre que conveniente. Numa
    /// luta mista, de bandidos com um cao amaldicoado no meio, e esse custo que transforma
    /// "qual espada saco agora" em um risco de verdade.
    ///
    /// Por isso ele e a excecao a excecao: a regra de ouro do docs/03 secao 1 deixa a
    /// esquiva cancelar qualquer coisa, e a secao 3 declara esta troca nao-cancelavel.
    /// Quem manda e a secao 3, e a razao esta em <see cref="CanTransitionTo"/>.
    ///
    /// A espada nova so entra em vigor no fim: trocar e um compromisso inteiro, nao meio.
    /// </summary>
    public sealed class WeaponSwapState : PlayerStateBase
    {
        readonly float _duration;

        float _elapsed;
        bool _equipped;

        public WeaponSwapState(float duration)
        {
            _duration = duration;
        }

        public override PlayerStateId Id => PlayerStateId.SwapWeapon;

        public override bool IsCommitted => true;

        /// <summary>Quanto falta da troca. O painel de debug mostra isto.</summary>
        public float Remaining => Mathf.Max(0f, _duration - _elapsed);

        public override void OnEnter(PlayerStateContext context)
        {
            _elapsed = 0f;
            _equipped = false;

            // Trocar de espada para o bruxo, ao contrario de trocar de postura, que a
            // secao 4 permite em deslocamento. As duas maos ocupadas sao o custo.
            if (context.Locomotion == null) return;

            context.Locomotion.MoveInput = Vector2.zero;
            context.Locomotion.SprintHeld = false;
        }

        public override void OnTick(PlayerStateContext context, float deltaTime)
        {
            _elapsed += deltaTime;

            if (_elapsed < _duration) return;

            Complete(context);
            context.Machine.ForceChangeState(PlayerStateId.Locomotion);
        }

        public override void OnExit(PlayerStateContext context)
        {
            // Atordoamento, morte e dialogo ainda podem tirar o bruxo daqui no meio. Nesse
            // caso a espada nao troca: ele foi interrompido com as duas na mao.
            context.PendingWeapon = null;
        }

        /// <summary>
        /// A unica sobrescrita de transicao do jogo, e ela merece a explicacao: a esquiva
        /// cancela qualquer acao comprometida (docs/03 secao 1), menos esta, que o
        /// docs/03 secao 3 declara nao-cancelavel. Sem isso, esquivar viraria o jeito de
        /// pagar meio preco pela troca, e o custo da camada 1 evaporaria.
        ///
        /// Atordoamento, morte e dialogo continuam passando, porque eles nao sao input do
        /// jogador: sao o mundo agindo sobre ele.
        /// </summary>
        public override bool CanTransitionTo(PlayerStateId next)
        {
            if (next == PlayerStateId.Dodge || next == PlayerStateId.Roll) return false;

            return PlayerStateRules.CanInterrupt(IsCommitted, next);
        }

        void Complete(PlayerStateContext context)
        {
            if (_equipped || context.Weapons == null || context.PendingWeapon == null) return;

            _equipped = true;
            context.Weapons.Equip(context.PendingWeapon.Value);
        }
    }
}
