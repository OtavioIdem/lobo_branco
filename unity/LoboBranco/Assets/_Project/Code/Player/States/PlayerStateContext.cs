using LoboBranco.Combat;
using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// Tudo que um estado precisa alcancar, em um objeto so, criado uma vez.
    ///
    /// Os alvos sao interfaces e nao MonoBehaviours: e o que permite montar a maquina
    /// inteira em um teste de EditMode com dublês, sem GameObject, sem fisica e sem
    /// Animator. E a mesma razao pela qual <c>PlayerLocomotion</c> recebe direcao por
    /// propriedade em vez de ler o teclado (regra 4 do CLAUDE.md).
    ///
    /// Nenhum estado le input diretamente: quem le e o <see cref="PlayerBrain"/>, que
    /// escreve os campos continuos aqui uma vez por frame.
    /// </summary>
    public sealed class PlayerStateContext
    {
        // ------------------------------------------------------------- colaboradores

        public PlayerStateMachine Machine;

        /// <summary>Para onde os estados escrevem movimento. Nulo e valido: o estado so nao move.</summary>
        public ILocomotionDriver Locomotion;

        /// <summary>Quem empunha a arma. Nulo e valido: o golpe roda sem causar dano.</summary>
        public IMeleeAttacker Attacker;

        public InputBuffer Buffer;

        // ------------------------------------------------------- entradas continuas

        /// <summary>Eixo de movimento bruto, escrito pelo componente de ligacao a cada frame.</summary>
        public Vector2 MoveInput;

        public bool SprintHeld;

        /// <summary>Aparo segurado. Consumido pela tarefa 1.11.</summary>
        public bool ParryHeld;

        /// <summary>
        /// Yaw da camera. Os estados nao o usam para andar (quem escreve isso na locomocao
        /// e o <see cref="PlayerBrain"/>), mas a esquiva da tarefa 1.10 precisa dele para
        /// saber o que "para tras" significa quando nao ha input de direcao.
        /// </summary>
        public float ReferenceYaw;

        // --------------------------------------------------------------- ataques

        /// <summary>Golpe escolhido por quem pediu a transicao, consumido pelo estado de ataque.</summary>
        public AttackDef PendingAttack;

        /// <summary>
        /// O golpe da postura que esta valendo agora (docs/03 secao 4). Quem escreve e o
        /// <see cref="PlayerBrain"/>, uma vez por frame, a partir do
        /// <see cref="StanceSelector"/>.
        ///
        /// E um campo so, e nao um por botao, porque a postura e que decide o golpe: se o
        /// botao direito desse um golpe Forte com a postura Rapida valendo, escolher
        /// postura nao seria decisao nenhuma e a camada 2 do combate morreria.
        /// </summary>
        public AttackDef CurrentAttack;

        /// <summary>Postura corrente, para os estados que precisam dela sem olhar o golpe.</summary>
        public Stance CurrentStance;

        // --------------------------------------------------------------- espadas

        /// <summary>Quem carrega as duas espadas. Nulo e valido: o estado de troca nao troca nada.</summary>
        public IWeaponHolder Weapons;

        // ----------------------------------------------------------------- vigor

        /// <summary>
        /// So para perguntar se da. Nulo e valido, e significa "vigor ainda nao cobra
        /// nada", que e como o jogo estava ate a tarefa 1.16.
        /// </summary>
        public IStaminaSource Vitals;

        /// <summary>
        /// Custo de vigor do golpe corrente, ja com o desconto de Fluxo. Escrito pelo
        /// <see cref="PlayerBrain"/> uma vez por frame, junto da postura, porque ele muda
        /// quando a corrente muda e nao so quando a postura muda.
        /// </summary>
        public float AttackStaminaCost;

        /// <summary>
        /// Espada pedida por quem iniciou a troca, consumida no fim dela. Nula quando nao
        /// ha troca em andamento, o que e diferente de "trocar para a que ja esta na mao".
        /// </summary>
        public WeaponMaterial? PendingWeapon;
    }
}
