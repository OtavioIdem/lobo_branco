namespace LoboBranco.Stats
{
    /// <summary>
    /// Como um modificador se combina com os outros. A ordem de aplicacao esta em
    /// <see cref="StatSheet"/> e nao depende da ordem em que os modificadores chegaram,
    /// o que e essencial: beber duas pocoes em ordem diferente tem que dar o mesmo numero.
    /// </summary>
    public enum ModifierOp
    {
        /// <summary>Somado ao valor base. <c>Value = 12</c> significa mais 12.</summary>
        Flat = 0,

        /// <summary>
        /// Fracao somada com os outros PercentAdd antes de multiplicar.
        /// <c>Value = 0.30</c> significa mais 30 por cento. Dois de 0.30 dao mais 60, nao mais 69.
        /// </summary>
        PercentAdd = 1,

        /// <summary>
        /// Multiplicador aplicado por fora, um a um. <c>Value = 1.20</c> significa vezes 1,20.
        /// Dois de 1.20 dao 1,44. Use so para efeitos que devem escalar de forma composta.
        /// </summary>
        PercentMult = 2,
    }

    /// <summary>
    /// Uma alteracao de atributo com origem rastreavel.
    ///
    /// O campo <see cref="Source"/> e a razao de esta ser uma struct com identidade e nao
    /// um par de valores: quando a pocao Andorinha vence, o sistema precisa remover
    /// exatamente os modificadores dela e nenhum outro. Sem isso, o bug classico aparece
    /// e o bonus fica para sempre.
    /// </summary>
    public readonly struct StatModifier
    {
        public readonly StatType Stat;
        public readonly ModifierOp Op;
        public readonly float Value;

        /// <summary>
        /// Quem aplicou. Pode ser a instancia da pocao, o no de talento, o item equipado.
        /// Comparado por referencia em <see cref="StatSheet.RemoveAllFromSource"/>.
        /// </summary>
        public readonly object Source;

        public StatModifier(StatType stat, ModifierOp op, float value, object source)
        {
            Stat = stat;
            Op = op;
            Value = value;
            Source = source;
        }

        public static StatModifier Flat(StatType stat, float value, object source)
            => new StatModifier(stat, ModifierOp.Flat, value, source);

        public static StatModifier PercentAdd(StatType stat, float fraction, object source)
            => new StatModifier(stat, ModifierOp.PercentAdd, fraction, source);

        public static StatModifier PercentMult(StatType stat, float multiplier, object source)
            => new StatModifier(stat, ModifierOp.PercentMult, multiplier, source);

        public override string ToString()
            => $"{Stat} {Op} {Value} ({Source?.ToString() ?? "sem origem"})";
    }
}
