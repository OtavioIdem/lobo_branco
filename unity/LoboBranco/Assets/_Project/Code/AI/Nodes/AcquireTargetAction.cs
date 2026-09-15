using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace LoboBranco.AI
{
    /// <summary>
    /// Poe no quadro negro o alvo que o <see cref="EnemyAgent"/> ja escolheu.
    ///
    /// O no nao procura nada: quem varre a fisica e o <see cref="EnemyAgent"/>, quatro
    /// vezes por segundo, rodando o tempo todo. Isto e deliberado e e a regra de divisao
    /// entre os dois lados: um no so executa enquanto o galho dele esta ativo, e uma
    /// criatura que so procurasse dentro do ramo de patrulha ficaria cega justamente
    /// enquanto persegue ou bate.
    ///
    /// Falha quando nao ha alvo, e falhar aqui e o caminho normal de volta a patrulha.
    /// </summary>
    [System.Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Pegar Alvo",
        description: "Copia o alvo escolhido pelo EnemyAgent para uma variavel do quadro negro.",
        story: "[Agent] pega o alvo em [Target]",
        category: "Action/Lobo Branco",
        id: "7f7f337f73cb463683dadd3f7703083f")]
    public partial class AcquireTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<GameObject> Target;

        protected override Status OnStart()
        {
            var enemy = EnemyNodeUtility.Resolve<EnemyAgent>(Agent);
            if (enemy == null) return Status.Failure;

            GameObject found = enemy.TargetObject;
            if (found == null) return Status.Failure;

            if (Target != null) Target.Value = found;

            return Status.Success;
        }
    }
}
