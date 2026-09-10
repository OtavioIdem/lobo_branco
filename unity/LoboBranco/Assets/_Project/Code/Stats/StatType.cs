namespace LoboBranco.Stats
{
    /// <summary>
    /// Todo valor numerico que poderia ser alterado por pocao, talento, mutagenio ou
    /// equipamento. Se algo pode receber modificador, ele e um StatType.
    ///
    /// Sobre os nomes: o jogo exibe "Vigor" para o recurso gasto em sinais e esquiva, e
    /// "Vigor" tambem era o nome do atributo no original. Em codigo isso colidiria, entao
    /// o atributo e <see cref="Endurance"/> e o recurso e <see cref="MaxStamina"/>.
    /// Os nomes exibidos vem de localizacao (tech/adr/0005).
    /// </summary>
    public enum StatType
    {
        // ---------------------------------------------------- atributos primarios
        // docs/02 secao 5. Cada um governa uma decisao de jogo, nao so um numero.

        /// <summary>Dano da postura Forte, poder de aparo, capacidade de carga.</summary>
        Strength = 0,

        /// <summary>Dano da postura Rapida, janela de esquiva, chance de critico.</summary>
        Dexterity = 1,

        /// <summary>Vigor maximo e regeneracao, resistencia a atordoamento.</summary>
        Endurance = 2,

        /// <summary>Intensidade de sinais, limite de toxicidade, qualidade de deducao.</summary>
        Intelligence = 3,

        // ------------------------------------------------------------- recursos

        MaxVitality = 10,
        MaxStamina = 11,
        MaxToxicity = 12,
        StaminaRegen = 13,
        StaminaRegenInCombat = 14,

        // ------------------------------------------------------------- ofensivo

        /// <summary>Bonus plano somado ao dano base da arma no estagio 1 do pipeline.</summary>
        AttackDamage = 20,

        /// <summary>
        /// Multiplicador do estagio 8. Base 1.0; pocoes e talentos somam PercentAdd,
        /// entao a pocao Trovao vira um modificador de +0.30 e o Get devolve 1.30.
        /// </summary>
        DamageMultiplier = 21,

        CritChance = 22,
        SignIntensity = 23,

        // ------------------------------------------------------------- defensivo

        /// <summary>Subtracao plana no estagio 10, nao percentual (docs/03 secao 9).</summary>
        Armor = 30,

        StaggerResistance = 31,

        // -------------------------------------------------- movimento e utilidade

        MoveSpeed = 40,
        CarryWeight = 41,
    }
}
