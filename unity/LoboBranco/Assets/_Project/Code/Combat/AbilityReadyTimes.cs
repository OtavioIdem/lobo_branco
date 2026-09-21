using Unity.Netcode;

namespace LoboBranco.Combat
{
    /// <summary>
    /// A recarga de todas as vagas de um personagem, do jeito que ela viaja pela rede.
    ///
    /// Um struct de tamanho fixo copiado byte a byte, e nao uma <c>NetworkList</c>. A lista
    /// aloca memoria nativa que precisa ser liberada, e manda cada elemento como mudanca
    /// separada; este struct e quarenta bytes que so viajam quando uma conjuracao comeca, e
    /// nao alocam nada (regra 5 do CLAUDE.md).
    ///
    /// Cinco vagas porque sao cinco os sinais do docs/03 secao 8. Uma escola que precise de
    /// mais esbarra no <c>SchoolDef</c>, que avisa no Inspector, antes de esbarrar aqui.
    /// </summary>
    public struct AbilityReadyTimes : INetworkSerializeByMemcpy
    {
        public const int Capacity = 5;

        public double Slot0;
        public double Slot1;
        public double Slot2;
        public double Slot3;
        public double Slot4;

        public double this[int slot]
        {
            get
            {
                switch (slot)
                {
                    case 0: return Slot0;
                    case 1: return Slot1;
                    case 2: return Slot2;
                    case 3: return Slot3;
                    case 4: return Slot4;
                    default: return 0d;
                }
            }
            set
            {
                switch (slot)
                {
                    case 0: Slot0 = value; break;
                    case 1: Slot1 = value; break;
                    case 2: Slot2 = value; break;
                    case 3: Slot3 = value; break;
                    case 4: Slot4 = value; break;
                }
            }
        }

        public static AbilityReadyTimes From(AbilityCooldowns cooldowns)
        {
            var times = new AbilityReadyTimes();
            if (cooldowns == null) return times;

            for (int i = 0; i < Capacity; i++)
                times[i] = cooldowns.ReadyAt(i);

            return times;
        }

        public void CopyTo(AbilityCooldowns cooldowns)
        {
            if (cooldowns == null) return;

            for (int i = 0; i < Capacity; i++)
                cooldowns.Overwrite(i, this[i]);
        }
    }
}
