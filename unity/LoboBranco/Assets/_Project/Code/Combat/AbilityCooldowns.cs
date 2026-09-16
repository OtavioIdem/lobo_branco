using System;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// A recarga de cada vaga de habilidade, guardada como o instante em que ela volta, e nao
    /// como quanto falta.
    ///
    /// Instante, e nao contagem, por causa da rede (tech/adr/0011). Uma contagem regressiva
    /// muda todo quadro, entao ou ela viaja todo quadro, ou cada maquina conta a sua e elas
    /// divergem. Um instante muda uma vez por conjuracao: o host manda o numero quando a
    /// recarga comeca, e cada maquina calcula quanto falta pelo proprio relogio do servidor. E
    /// o mesmo arranjo do telegrafo da tarefa 1.23.
    ///
    /// Classe pura, com o relogio por parametro, como o <see cref="StaminaPool"/>: a recarga
    /// inteira e verificavel em EditMode sem esperar segundo nenhum.
    ///
    /// Autoridade: de quem resolve. Em rede e o host, e o dono guarda aqui a copia replicada.
    /// </summary>
    public sealed class AbilityCooldowns
    {
        readonly double[] _readyAt;

        public AbilityCooldowns(int slots)
        {
            _readyAt = new double[Mathf.Max(0, slots)];
        }

        public int SlotCount => _readyAt.Length;

        public bool IsValid(int slot) => slot >= 0 && slot < _readyAt.Length;

        /// <summary>Instante em que a vaga volta. Zero numa vaga que nunca foi usada.</summary>
        public double ReadyAt(int slot) => IsValid(slot) ? _readyAt[slot] : 0d;

        /// <summary>
        /// Vaga fora da faixa nunca esta pronta. Quem pergunta por ela pediu uma habilidade que a
        /// escola nao tem, e "pronta" faria esse pedido chegar ao host.
        /// </summary>
        public bool IsReady(int slot, double now) => IsValid(slot) && now >= _readyAt[slot];

        /// <summary>Segundos ate a vaga voltar, nunca negativo.</summary>
        public float Remaining(int slot, double now)
            => IsValid(slot) ? (float)Math.Max(0d, _readyAt[slot] - now) : 0f;

        /// <summary>Arma a recarga a partir de <paramref name="now"/>. Recarga nova substitui a anterior.</summary>
        public void Start(int slot, double now, float seconds)
        {
            if (!IsValid(slot)) return;

            _readyAt[slot] = now + Mathf.Max(0f, seconds);
        }

        /// <summary>Usado quando a copia replicada manda.</summary>
        public void Overwrite(int slot, double readyAt)
        {
            if (!IsValid(slot)) return;

            _readyAt[slot] = readyAt;
        }

        /// <summary>
        /// Muda o relogio de referencia sem mudar quanto falta. Chamado quando uma sessao comeca
        /// com o personagem ja existindo: o relogio local e o do servidor partem de zeros
        /// diferentes, e sem isto uma recarga armada antes da sessao duraria minutos.
        /// </summary>
        public void Rebase(double delta)
        {
            for (int i = 0; i < _readyAt.Length; i++)
                _readyAt[i] += delta;
        }

        /// <summary>Todas as vagas prontas.</summary>
        public void Clear() => Array.Clear(_readyAt, 0, _readyAt.Length);
    }
}
