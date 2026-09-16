using Unity.Netcode;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// O escudo do docs/03 secao 8 (tarefa 1.18e), guardado como o instante em que ele cai e o
    /// quanto devolve ao quebrar. E ao mesmo tempo a regra e o formato de rede, como o
    /// <see cref="ControlState"/>, e pelo mesmo motivo: o host escreve uma vez por conjuracao e por
    /// quebra, e cada maquina pergunta "o escudo esta de pe?" contra o proprio relogio do servidor.
    ///
    /// <b>Ele guarda o quanto devolve, e nao so a duracao.</b> A intensidade de quem conjurou entra
    /// na conta uma vez, na hora de erguer; se ficasse de fora, quem quebra o escudo precisaria
    /// perguntar a folha de atributos de quem o ergueu, e ela pode ter mudado no meio.
    ///
    /// Erguer de novo substitui, e nunca soma: dois escudos nao absorvem dois golpes. O escudo ja
    /// custa vigor e recarga, e o compromisso que o documento pede esta ai.
    /// </summary>
    public struct WardState : INetworkSerializeByMemcpy
    {
        public double Until;

        /// <summary>Fracao do dano absorvido que volta para quem bateu. 0,3 e os 30% do documento.</summary>
        public float ReflectFraction;

        public bool IsUp(double now) => now < Until;

        /// <summary>Segundos ate o escudo cair sozinho, nunca negativo.</summary>
        public float RemainingAt(double now) => now < Until ? (float)(Until - now) : 0f;

        /// <summary>
        /// Ergue. Devolve verdadeiro so quando mudou alguma coisa: um escudo novo mais curto e mais
        /// fraco que o de pe nao vale a mensagem de rede.
        /// </summary>
        public bool Raise(double now, float seconds, float reflectFraction)
        {
            if (seconds <= 0f) return false;

            double until = now + seconds;
            reflectFraction = Mathf.Max(0f, reflectFraction);

            if (IsUp(now) && until <= Until && reflectFraction <= ReflectFraction) return false;

            Until = until;
            ReflectFraction = reflectFraction;
            return true;
        }

        /// <summary>
        /// Quebra o escudo e diz quanto devolver. Falso quando nao havia escudo de pe, e ai o dano
        /// segue o caminho normal.
        /// </summary>
        public bool TryBreak(double now, float absorbed, out float reflected)
        {
            reflected = 0f;

            if (!IsUp(now)) return false;

            reflected = Mathf.Max(0f, absorbed) * ReflectFraction;
            this = default;
            return true;
        }

        /// <summary>Cai agora. A morte leva o escudo junto.</summary>
        public void Clear() => this = default;

        /// <summary>Muda o relogio de referencia sem mudar quanto falta. Ver <see cref="ControlState.Rebase"/>.</summary>
        public void Rebase(double delta) => Until += delta;
    }
}
