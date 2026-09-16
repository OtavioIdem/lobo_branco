namespace LoboBranco.Combat
{
    /// <summary>
    /// Quem carrega as duas espadas e sabe qual esta na mao.
    ///
    /// Existe separado do <see cref="IMeleeAttacker"/> porque sao duas perguntas
    /// diferentes: uma e sobre desferir um golpe, a outra e sobre o que esta empunhado. O
    /// estado que troca de espada nao precisa saber abrir janela de dano, e o teste dele
    /// nao deveria ter que fingir que sabe.
    ///
    /// Autoridade: o dono escolhe, o host precisa saber. O material e o estagio 4 do
    /// pipeline, o multiplicador de maior impacto do combate (docs/03 secao 3), entao
    /// quem resolve o golpe tem que estar empunhando a mesma espada que o dono acha que
    /// esta empunhando.
    /// </summary>
    public interface IWeaponHolder
    {
        /// <summary>Material da espada na mao. Decide o estagio 4 do pipeline.</summary>
        WeaponMaterial EquippedMaterial { get; }

        /// <summary>A espada na mao, ou nulo quando nenhuma esta configurada.</summary>
        MeleeWeaponDef EquippedWeapon { get; }

        /// <summary>Falso quando a espada daquele material nao existe no equipamento.</summary>
        bool Has(WeaponMaterial material);

        /// <summary>
        /// Troca a espada na mao. Chamado no <em>fim</em> da troca, e nao no comeco: os
        /// 0,7 s do docs/03 secao 3 sao o tempo de guardar uma e sacar a outra, e o dano
        /// novo so vale quando a lamina nova esta na mao.
        /// </summary>
        void Equip(WeaponMaterial material);
    }
}
