using Unity.Netcode;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>O que tira o controle de uma criatura agora. Derrubada vence atordoada.</summary>
    public enum ControlKind : byte
    {
        None = 0,
        Stunned = 1,
        KnockedDown = 2,
    }

    /// <summary>
    /// Atordoamento, derrubada e lentidao de uma criatura (tarefa 1.18a), guardados como o
    /// instante em que cada um acaba.
    ///
    /// E ao mesmo tempo a regra e o formato de rede, e isso e de proposito. O host escreve um
    /// struct inteiro quando um sinal aplica algo, e cada maquina pergunta "esta atordoada
    /// agora?" contra o proprio relogio do servidor. E o arranjo da recarga de habilidade
    /// (tech/adr/0011) e do telegrafo (tarefa 1.23): nenhuma mensagem por quadro, e o cliente
    /// ve o atordoamento acabar no mesmo instante que o host.
    ///
    /// As regras de reaplicar sao as que nao deixam sinal repetido virar controle eterno por
    /// soma: um atordoamento novo so vale se acabar depois do atual, e nunca soma. Derrubada e
    /// atordoamento contam separados, porque sao sinais diferentes decidindo coisas diferentes, e
    /// a criatura fica sem controle ate o maior dos dois acabar.
    ///
    /// Lentidao tem uma vaga so: a mais forte manda. Uma mais forte e mais curta substitui uma
    /// mais fraca e mais longa, e o resto da fraca se perde. Guardar as duas seria uma lista, e
    /// lista nao cabe num struct copiado byte a byte.
    /// </summary>
    public struct ControlState : INetworkSerializeByMemcpy
    {
        public double StunnedUntil;
        public double KnockedDownUntil;
        public double SlowedUntil;

        /// <summary>Fracao da velocidade que a lentidao tira, de 0 a 1. 0,6 e os 60% do Yrden.</summary>
        public float SlowFraction;

        // ---------------------------------------------------------------- leitura

        public bool IsStunned(double now) => now < StunnedUntil;

        public bool IsKnockedDown(double now) => now < KnockedDownUntil;

        public bool IsIncapacitated(double now) => IsStunned(now) || IsKnockedDown(now);

        public ControlKind Incapacitation(double now)
        {
            if (IsKnockedDown(now)) return ControlKind.KnockedDown;
            if (IsStunned(now)) return ControlKind.Stunned;

            return ControlKind.None;
        }

        /// <summary>Segundos ate a criatura voltar a agir, nunca negativo.</summary>
        public float IncapacitatedRemaining(double now)
        {
            double until = StunnedUntil > KnockedDownUntil ? StunnedUntil : KnockedDownUntil;
            return until > now ? (float)(until - now) : 0f;
        }

        /// <summary>A fracao de velocidade tirada agora. Zero sem lentidao.</summary>
        public float SlowAt(double now) => now < SlowedUntil ? SlowFraction : 0f;

        // --------------------------------------------------------------- escrita

        // As tres devolvem verdadeiro so quando o estado mudou. E o que decide se o host
        // escreve na rede: um segundo Aard sobre uma criatura ja atordoada por mais tempo nao
        // manda mensagem nenhuma.

        public bool Stun(double now, float seconds) => Extend(ref StunnedUntil, now, seconds);

        public bool KnockDown(double now, float seconds) => Extend(ref KnockedDownUntil, now, seconds);

        public bool Slow(double now, float fraction, float seconds)
        {
            fraction = Mathf.Clamp01(fraction);
            if (fraction <= 0f || seconds <= 0f) return false;

            double until = now + seconds;
            float current = SlowAt(now);

            if (fraction > current)
            {
                SlowFraction = fraction;
                SlowedUntil = until;
                return true;
            }

            // A mesma forca de novo renova, e uma mais fraca nao entra enquanto a forte vale.
            if (Mathf.Approximately(fraction, current) && until > SlowedUntil)
            {
                SlowedUntil = until;
                return true;
            }

            return false;
        }

        /// <summary>Tudo acaba agora. A morte leva o controle junto.</summary>
        public void Clear() => this = default;

        /// <summary>Muda o relogio de referencia sem mudar quanto falta. Ver <see cref="AbilityCooldowns.Rebase"/>.</summary>
        public void Rebase(double delta)
        {
            StunnedUntil += delta;
            KnockedDownUntil += delta;
            SlowedUntil += delta;
        }

        static bool Extend(ref double until, double now, float seconds)
        {
            if (seconds <= 0f) return false;

            double next = now + seconds;
            if (next <= until) return false;

            until = next;
            return true;
        }
    }
}
