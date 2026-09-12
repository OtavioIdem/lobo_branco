using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace LoboBranco.AI
{
    /// <summary>
    /// Se o alvo esta perto o bastante para o golpe conectar.
    ///
    /// O alcance nao e um numero deste no: ele e o <c>reach</c> do proprio
    /// <c>AttackDef</c>, o mesmo que a hitbox consulta. Um numero a parte aqui poderia
    /// discordar da hitbox, e o sintoma seria a criatura parando e batendo no vazio para
    /// sempre, ou colando no bruxo antes de bater.
    ///
    /// A folga existe porque os dois se mexem: comecar o golpe exatamente no limite faz
    /// o alvo sair do alcance durante a anticipacao, e todo golpe erraria por um passo.
    /// </summary>
    [System.Serializable, GeneratePropertyBag]
    [Condition(
        name: "Ao Alcance do Golpe",
        description: "Compara a distancia ate o alvo com o alcance do golpe da especie.",
        story: "[Agent] esta ao alcance do golpe",
        category: "Conditions/Lobo Branco",
        id: "9533364cd55c46daaadd11eca82c05c9")]
    public partial class IsInStrikeRangeCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        [Tooltip("Fracao do alcance do golpe que conta como 'perto o bastante'.")]
        [SerializeReference] public BlackboardVariable<float> RangeFactor = new BlackboardVariable<float>(0.85f);

        public override bool IsTrue()
        {
            var enemy = EnemyNodeUtility.Resolve<EnemyAgent>(Agent);
            var attacker = EnemyNodeUtility.Resolve<EnemyMeleeAttacker>(Agent);

            if (enemy == null || attacker == null) return false;

            float reach = attacker.Reach;
            if (reach <= 0f) return false;

            float factor = RangeFactor != null ? RangeFactor.Value : 0.85f;

            return enemy.DistanceToTarget <= reach * factor;
        }
    }
}
