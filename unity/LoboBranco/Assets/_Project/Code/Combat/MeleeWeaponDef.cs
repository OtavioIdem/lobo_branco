using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// O minimo que o pipeline de dano precisa saber sobre a lamina que desferiu o golpe:
    /// dano cru, material e tipo de dano. Numeros de referencia em docs/03 secao 12
    /// (jogador nivel 1 bate 12 de base).
    ///
    /// Provisorio de proposito. A hierarquia de <c>ItemDef</c> com durabilidade, slot de
    /// oleo e modificadores entra no M2 (tarefas 2.1 e 2.4), e absorve este tipo. Ele
    /// existe agora porque o alternativo seria o numero 12 dentro de um MonoBehaviour, e
    /// isso e a regra 1 do CLAUDE.md quebrada logo no primeiro golpe.
    /// </summary>
    [CreateAssetMenu(menuName = "LoboBranco/Combat/Melee Weapon", fileName = "Weapon_")]
    public sealed class MeleeWeaponDef : ScriptableObject
    {
        [Tooltip("Estagio 4 do pipeline. Aco em monstro e 0,35x, e essa e a decisao binaria " +
                 "de maior impacto do combate (docs/03 secao 3).")]
        public WeaponMaterial material = WeaponMaterial.Steel;

        [Tooltip("Estagio 11: a resistencia do alvo e consultada por este tipo.")]
        public DamageType damageType = DamageType.Slash;

        [Tooltip("Estagio 1: dano cru, antes de qualquer multiplicador.")]
        [Min(0f)] public float baseDamage = 12f;
    }
}
