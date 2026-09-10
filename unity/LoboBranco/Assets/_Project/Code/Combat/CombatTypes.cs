namespace LoboBranco.Combat
{
    /// <summary>
    /// Postura de combate. Herdada do original e mantida de proposito: escolher a postura
    /// certa por inimigo e o motor de decisao segundo a segundo do combate (docs/03 secao 4).
    /// </summary>
    public enum Stance
    {
        /// <summary>Dano alto, lento. Contra alvos blindados ou lentos.</summary>
        Strong = 0,

        /// <summary>Dano baixo, cadencia alta. Contra alvos ageis.</summary>
        Fast = 1,

        /// <summary>Area em arco. Contra tres ou mais alvos.</summary>
        Group = 2,
    }

    /// <summary>
    /// Material da lamina. A decisao binaria mais impactante do combate: usar a espada
    /// errada corta o dano drasticamente (docs/03 secao 3).
    /// </summary>
    public enum WeaponMaterial
    {
        Steel = 0,
        Silver = 1,
    }

    /// <summary>
    /// Como o alvo se comporta em combate. Determina qual postura tem afinidade com ele,
    /// e e por isso que um monstro precisa declarar o seu.
    /// </summary>
    public enum StanceArchetype
    {
        /// <summary>Blindado, pesado, lento. Afinidade com Forte.</summary>
        Heavy = 0,

        /// <summary>Agil, esquivo. Afinidade com Rapida.</summary>
        Agile = 1,

        /// <summary>Aparece em bando. Afinidade com Grupo.</summary>
        Swarm = 2,
    }

    /// <summary>
    /// Classe da criatura. Decide aco contra prata e qual oleo funciona.
    /// Os nomes sao genericos de proposito: o nome exibido vem de asset (tech/adr/0005).
    /// </summary>
    public enum CreatureClass
    {
        /// <summary>Humanos e humanoides. Unico grupo em que o aco e melhor que a prata.</summary>
        Humanoid = 0,

        Beast = 1,
        Necrophage = 2,
        Specter = 3,
        Insectoid = 4,
        Cursed = 5,
        Construct = 6,
    }

    /// <summary>
    /// Classe de oleo de lamina. Casada contra a vulnerabilidade declarada no alvo, em
    /// vez de mapeada por codigo a partir da classe: isso deixa um monstro especifico ser
    /// excecao sem tocar em codigo.
    /// </summary>
    public enum OilClass
    {
        None = 0,
        Beast = 1,
        Necrophage = 2,
        Specter = 3,
        Insectoid = 4,
        Cursed = 5,
    }

    public enum DamageType
    {
        Slash = 0,
        Pierce = 1,
        Blunt = 2,
        Fire = 3,
        Frost = 4,
        Poison = 5,
    }

    public static class CombatTypeExtensions
    {
        /// <summary>Qual postura tem afinidade com este arquetipo (docs/03 secao 4).</summary>
        public static Stance PreferredStance(this StanceArchetype archetype)
        {
            switch (archetype)
            {
                case StanceArchetype.Heavy: return Stance.Strong;
                case StanceArchetype.Agile: return Stance.Fast;
                case StanceArchetype.Swarm: return Stance.Group;
                default: return Stance.Fast;
            }
        }

        public static bool IsHumanoid(this CreatureClass creatureClass)
            => creatureClass == CreatureClass.Humanoid;
    }
}
