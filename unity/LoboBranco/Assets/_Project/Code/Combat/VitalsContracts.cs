namespace LoboBranco.Combat
{
    /// <summary>
    /// O que uma acao precisa perguntar antes de comecar: da o vigor?
    ///
    /// So leitura, e de proposito. Quem gasta e o host, pelo
    /// <see cref="CharacterVitals.TrySpendStamina"/>; quem pergunta e o dono, para saber
    /// se vale pedir. Se este contrato tivesse um metodo de gastar, um estado do jogador
    /// poderia cobrar vigor na maquina errada, e o sintoma seria o companheiro perdendo
    /// vigor que o host nunca tirou.
    ///
    /// Interface, e nao a classe, porque a maquina de estados e verificada em EditMode com
    /// dublês: o teste de uma acao cara precisa poder dizer "tem 3 de vigor" sem cena,
    /// sem rede e sem folha de atributos.
    /// </summary>
    public interface IStaminaSource
    {
        float CurrentStamina { get; }

        float MaxStamina { get; }

        bool CanAfford(float cost);
    }
}
