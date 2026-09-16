using System.Collections.Generic;
using System.Text;

namespace LoboBranco.Combat
{
    /// <summary>
    /// O objeto de trabalho que atravessa os onze estagios. Reutilizado pelo
    /// <see cref="DamagePipeline"/> entre golpes, para nao alocar em combate.
    ///
    /// O <see cref="Log"/> e a peca que torna balanceamento depuravel. Sem ele, um
    /// multiplicador errado e invisivel: o numero final parece plausivel e ninguem
    /// descobre qual estagio errou. Com ele, o console mostra
    /// "12 base x 1,45 forte x 0,8 afinidade errada x 0,35 aco em monstro = 4,87".
    ///
    /// Formatar string aloca, entao o log e desligado por padrao e so a cena de sandbox
    /// o liga. Ligado em combate real, ele viola a regra 5 do CLAUDE.md de proposito,
    /// em troca de visibilidade durante a afinacao.
    /// </summary>
    public sealed class DamageContext
    {
        public IDamageDealer Attacker;
        public IDamageable Target;
        public DamageType Type;
        public Stance Stance;
        public WeaponMaterial Material;
        public OilClass AppliedOil;
        public int FlowChain;
        public bool IsCritical;

        /// <summary>Valor corrente, alterado por cada estagio.</summary>
        public float Amount;

        /// <summary>Acumula so os estagios multiplicativos, para o diagnostico e os testes.</summary>
        public float MultiplierSoFar;

        public bool LoggingEnabled;
        public readonly List<string> Log = new List<string>(16);

        public void Begin(in DamageRequest request, bool loggingEnabled)
        {
            Attacker = request.Attacker;
            Target = request.Target;
            Type = request.Type;
            Stance = request.Stance;
            Material = request.Material;
            AppliedOil = request.AppliedOil;
            FlowChain = request.FlowChain;
            IsCritical = request.IsCritical;

            Amount = request.WeaponDamage;
            MultiplierSoFar = 1f;

            LoggingEnabled = loggingEnabled;
            Log.Clear();
        }

        /// <summary>Aplica um multiplicador e registra. Use isto em vez de mexer em Amount direto.</summary>
        public void ApplyMultiplier(float multiplier, string reason)
        {
            Amount *= multiplier;
            MultiplierSoFar *= multiplier;

            if (LoggingEnabled)
                Log.Add($"x {multiplier:0.###}  {reason}  -> {Amount:0.##}");
        }

        /// <summary>Aplica uma alteracao plana e registra. Armadura usa isto.</summary>
        public void ApplyFlat(float delta, string reason)
        {
            Amount += delta;

            if (LoggingEnabled)
                Log.Add($"{delta:+0.##;-0.##}  {reason}  -> {Amount:0.##}");
        }

        public void Note(string reason)
        {
            if (LoggingEnabled)
                Log.Add($"          {reason}  -> {Amount:0.##}");
        }

        /// <summary>Log em uma linha, para o console de debug.</summary>
        public string DescribeLog()
        {
            if (Log.Count == 0) return "(log desligado)";

            var sb = new StringBuilder(256);
            for (int i = 0; i < Log.Count; i++)
                sb.AppendLine(Log[i]);

            return sb.ToString();
        }

        public DamageResult ToResult() => new DamageResult(Amount, Type, IsCritical, MultiplierSoFar, Attacker);
    }
}
