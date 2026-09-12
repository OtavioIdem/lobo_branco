using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace LoboBranco.AI
{
    /// <summary>
    /// A pergunta que separa o ramo de patrulha do ramo de combate: ela esta cacando?
    ///
    /// Nao e "esta vendo". Os sentidos guardam o alvo por alguns segundos depois de
    /// perde-lo de vista (<see cref="EnemySenses"/>), entao esta condicao continua
    /// verdadeira enquanto a criatura ainda procura. E isso que impede o comportamento que
    /// mais denuncia IA ruim: o inimigo que volta a patrulhar no instante em que voce
    /// quebra a linha de visao, com voce a dois metros dele.
    /// </summary>
    [System.Serializable, GeneratePropertyBag]
    [Condition(
        name: "Esta Cacando",
        description: "Verdadeiro enquanto a criatura tem alvo, vendo ou lembrando.",
        story: "[Agent] esta cacando alguem",
        category: "Conditions/Lobo Branco",
        id: "8c5509068f6a448fb89802b91ec0765b")]
    public partial class IsHuntingCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        public override bool IsTrue()
        {
            var enemy = EnemyNodeUtility.Resolve<EnemyAgent>(Agent);

            return enemy != null && enemy.IsAware;
        }
    }
}
