using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace LoboBranco.AI
{
    /// <summary>
    /// Se a criatura esta atordoada ou derrubada (tarefa 1.18a). E o galho "[Atordoado?] ->
    /// aguardar" da arvore do docs/07 secao 6.
    ///
    /// O galho nao e o que segura a criatura: quem segura sao o <c>EnemyAgent</c> e o
    /// <c>EnemyMeleeAttacker</c>, que recusam andar, girar e golpear enquanto o controle vale,
    /// em qualquer galho (ADR 0009). O galho existe para a arvore nao ficar pedindo a vez de
    /// golpear a cada quadro de uma criatura que nao pode golpear, e para o grafo mostrar a quem
    /// le que atordoar e um estado, e nao uma falha de ataque.
    /// </summary>
    [System.Serializable, GeneratePropertyBag]
    [Condition(
        name: "Sem Controle",
        description: "Verdadeiro enquanto a criatura esta atordoada ou derrubada.",
        story: "[Agent] esta sem controle",
        category: "Conditions/Lobo Branco",
        id: "3f1c7e9a52b84d0e9c6a1b7d2e4f8a60")]
    public partial class IsIncapacitatedCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        public override bool IsTrue()
        {
            var enemy = EnemyNodeUtility.Resolve<EnemyAgent>(Agent);

            return enemy != null && enemy.IsIncapacitated;
        }
    }
}
