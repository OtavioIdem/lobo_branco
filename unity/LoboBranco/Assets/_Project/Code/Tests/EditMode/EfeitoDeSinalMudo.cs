using System.Collections.Generic;
using LoboBranco.Combat;

namespace LoboBranco.Tests
{
    /// <summary>
    /// Efeito de sinal que nao faz nada e reclama do que mandarem. Existe porque o tipo base e
    /// abstrato, e as regras de dado da habilidade e da escola precisam de um efeito para conferir.
    /// </summary>
    public sealed class EfeitoDeSinalMudo : SignEffectDef
    {
        public string Problema;

        public override void Apply(in SignCast cast, CharacterVitals target) { }

        public override void CollectProblems(List<string> problems)
        {
            if (!string.IsNullOrEmpty(Problema)) problems.Add(Problema);
        }
    }
}
