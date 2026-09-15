using System.Collections.Generic;
using System.Globalization;
using LoboBranco.Combat;

namespace LoboBranco.Tests
{
    /// <summary>
    /// Efeito de sinal que so anota em quem foi aplicado, em que ordem e com que potencia. Aloca
    /// string a cada aplicacao, e tudo bem: e teste, e o que se confere aqui e a ordem.
    /// </summary>
    public sealed class EfeitoDeSinalGravador : SignEffectDef
    {
        public string Rotulo = "efeito";
        public float Potencia = 1f;
        public List<string> Registro;

        // Cultura invariante: numa maquina em pt-BR, 1.5 sairia "1,5" e o teste falharia pela virgula.
        public override void Apply(in SignCast cast, CharacterVitals target)
            => Registro?.Add($"{Rotulo}:{target.name}:{cast.Scale(Potencia).ToString("0.##", CultureInfo.InvariantCulture)}");
    }
}
