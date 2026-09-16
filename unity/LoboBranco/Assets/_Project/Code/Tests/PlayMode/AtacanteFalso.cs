using LoboBranco.Combat;
using LoboBranco.Stats;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// Quem bate, nos testes do escudo: um <see cref="IDamageDealer"/> em componente, para o troco
    /// dos 30% ter para onde voltar. O atacante de verdade e do modulo da IA e traz consigo hitbox,
    /// linha do tempo e especie.
    /// </summary>
    public sealed class AtacanteFalso : MonoBehaviour, IDamageDealer
    {
        public StatSheet Stats { get; } = new StatSheet();

        public bool HasBestiaryKnowledge(CreatureClass creatureClass) => false;
    }
}
