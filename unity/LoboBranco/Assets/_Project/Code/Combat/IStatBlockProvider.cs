using LoboBranco.Stats;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Quem decide o bloco de atributos de um personagem, quando nao e o proprio
    /// <see cref="CharacterVitals"/>.
    ///
    /// Existe por causa do grafo de asmdef (regra 2 do CLAUDE.md). A escola do bruxo mora no
    /// modulo do jogador, que fica acima do combate, e o combate nao pode apontar para cima.
    /// Com esta interface, o <see cref="CharacterVitals"/> pergunta "alguem ao meu lado decide
    /// meus atributos?" sem saber o que e uma escola, e a criatura, que nao tem escola nenhuma,
    /// continua usando o proprio campo.
    /// </summary>
    public interface IStatBlockProvider
    {
        /// <summary>O bloco de atributos a usar, ou nulo para cair no campo do proprio componente.</summary>
        StatBlockDef StatBlock { get; }
    }
}
