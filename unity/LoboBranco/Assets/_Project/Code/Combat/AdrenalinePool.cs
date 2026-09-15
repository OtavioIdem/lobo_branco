using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// As tres cargas de Adrenalina do docs/03 secao 7. Ganha com riposte, com corrente de
    /// Fluxo alta e com mortes; gasta em finalizacao, sinal reforcado e segundo suspiro.
    ///
    /// A diferenca para o Vigor e o que define o recurso: Vigor volta sozinho e Adrenalina
    /// nao. Ela so entra quando o bruxo faz alguma coisa bem feita, entao gastar uma carga
    /// e gastar uma luta que ja aconteceu. Por isso ela nao regenera, nao vaza e nao tem
    /// atraso: ou esta la, ou nao esta.
    ///
    /// Classe pura, com os numeros vindos de fora, como o <see cref="StaminaPool"/> e o
    /// <see cref="FlowChain"/>.
    ///
    /// Autoridade: o host (doc 13 secao 6). Quem conta corrente, morte e riposte e ele.
    /// </summary>
    public sealed class AdrenalinePool
    {
        readonly int _flowLinksForFirstCharge;
        readonly int _flowLinksPerCharge;

        public AdrenalinePool(int maxCharges, int flowLinksForFirstCharge, int flowLinksPerCharge)
        {
            MaxCharges = Mathf.Max(0, maxCharges);
            _flowLinksForFirstCharge = Mathf.Max(1, flowLinksForFirstCharge);
            _flowLinksPerCharge = Mathf.Max(1, flowLinksPerCharge);
        }

        public int MaxCharges { get; }

        public int Charges { get; private set; }

        public bool IsFull => Charges >= MaxCharges;

        public bool CanAfford(int cost) => cost <= 0 || Charges >= cost;

        /// <summary>Devolve falso quando ja estava cheia. O excedente nao fica guardado.</summary>
        public bool Gain(int charges = 1)
        {
            if (charges <= 0 || IsFull) return false;

            Charges = Mathf.Min(MaxCharges, Charges + charges);
            return true;
        }

        /// <summary>
        /// Cobra as cargas. Ou paga inteiro, ou nao acontece: meia finalizacao nao existe.
        /// </summary>
        public bool TrySpend(int cost)
        {
            if (!CanAfford(cost)) return false;

            Charges -= Mathf.Max(0, cost);
            return true;
        }

        /// <summary>
        /// Chamado a cada golpe encadeado, com o total de elos da corrente. A partir do
        /// quinto elo, a cada dois elos nasce uma carga (docs/03 secao 6).
        ///
        /// Contar por elo, e nao por tempo, e o que liga o recurso a maestria: quem
        /// encadeia cinco golpes fez por merecer, e quem mistura golpe e pausa nao.
        /// </summary>
        public bool NoteFlowLinks(int links)
        {
            if (links < _flowLinksForFirstCharge) return false;

            return (links - _flowLinksForFirstCharge) % _flowLinksPerCharge == 0 && Gain();
        }

        /// <summary>Zera. A morte leva a adrenalina junto.</summary>
        public void Clear() => Charges = 0;
    }
}
