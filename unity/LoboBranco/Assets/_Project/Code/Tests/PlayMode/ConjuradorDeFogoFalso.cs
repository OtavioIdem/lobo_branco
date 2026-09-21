using LoboBranco.Combat;
using LoboBranco.Stats;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// Quem conjura o fogo nos testes: uma espada na mao e a folha de atributos da vida ao lado. O
    /// atacante de verdade e do modulo do jogador e exige tuning, escola e rede para ficar de pe; o
    /// efeito so pergunta estes dois contratos.
    /// </summary>
    public sealed class ConjuradorDeFogoFalso : MonoBehaviour, IDamageDealer, IWeaponHolder
    {
        public MeleeWeaponDef Espada;

        public StatSheet Stats => GetComponent<CharacterVitals>().Stats;
        public bool HasBestiaryKnowledge(CreatureClass creatureClass) => false;

        public WeaponMaterial EquippedMaterial => Espada != null ? Espada.material : WeaponMaterial.Steel;
        public MeleeWeaponDef EquippedWeapon => Espada;
        public bool Has(WeaponMaterial material) => Espada != null && Espada.material == material;
        public void Equip(WeaponMaterial material) { }
    }
}
