using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// A superficie de escrita da locomocao, vista por quem manda nela.
    ///
    /// Existe para que os estados da FSM sejam testaveis em EditMode: um estado que
    /// dependesse de <see cref="PlayerLocomotion"/> exigiria GameObject, CharacterController
    /// e chao para rodar um teste que so quer saber se o ataque zerou o movimento.
    ///
    /// Repare no que nao esta aqui: nao ha <c>Tick</c>. Quem avanca a simulacao continua
    /// sendo o proprio <see cref="PlayerLocomotion"/> no <c>Update</c> dele. Os estados
    /// escrevem intencao, e nao passos, e por isso nao existe risco de o movimento ser
    /// integrado duas vezes no mesmo frame.
    /// </summary>
    public interface ILocomotionDriver
    {
        Vector2 MoveInput { get; set; }
        bool SprintHeld { get; set; }
        float ReferenceYaw { get; set; }
    }
}
