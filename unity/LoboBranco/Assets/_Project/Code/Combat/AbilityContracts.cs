namespace LoboBranco.Combat
{
    /// <summary>
    /// O que o estado de sinal precisa poder mandar em quem conjura.
    ///
    /// Interface pelo mesmo motivo do <see cref="IMeleeAttacker"/>: com ela, a linha do tempo de
    /// uma conjuracao e verificada em EditMode, sem rede e sem cena, e o estado nunca sabe se
    /// do outro lado ha um host cobrando vigor ou um dublê contando chamadas.
    ///
    /// As habilidades sao pedidas por vaga, e nao por asset, porque e a vaga que viaja pela
    /// rede. O asset e resolvido de cada lado pela escola, que e a mesma nas duas maquinas.
    /// </summary>
    public interface IAbilityCaster
    {
        /// <summary>A habilidade de uma vaga, ou nulo se a vaga estiver vazia.</summary>
        AbilityDef AbilityAt(int slot);

        /// <summary>Se a vaga pode sair agora. Nao muda nada. Ver <see cref="AbilityRules"/>.</summary>
        AbilityRefusal CanCast(int slot);

        /// <summary>Comeca a conjuracao. E aqui que o host cobra o vigor e arma a recarga.</summary>
        void BeginCast(int slot);

        /// <summary>O instante do efeito. Chamado uma vez por conjuracao, no fim do tempo de conjurar.</summary>
        void TriggerCast();

        /// <summary>Corta a conjuracao antes do efeito. O que foi pago fica pago.</summary>
        void CancelCast();
    }
}
