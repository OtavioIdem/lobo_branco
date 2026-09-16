using System.Collections.Generic;

namespace LoboBranco.AI
{
    /// <summary>
    /// Quem tem permissao para golpear agora (docs/07 secao 6).
    ///
    /// Sem isto, cinco barghests desferem o golpe no mesmo instante e o combate fica
    /// injusto e ilegivel ao mesmo tempo: nao ha esquiva que resolva cinco golpes
    /// simultaneos, e nao ha como ler cinco telegrafos de uma vez. Limitar quantos atacam
    /// ao mesmo tempo e o truque mais importante de IA de combate, e quase todo jogo de
    /// acao bom faz alguma versao dele.
    ///
    /// <b>O limite e por alvo, e nao por encontro.</b> O docs/07 secao 6 escreve "um
    /// coordenador por encontro concede no maximo 2 tokens", e isso foi pensado para um
    /// jogador. Em coop de quatro, um teto global de dois faria um encontro de oito
    /// criaturas ter seis paradas assistindo, e o segundo, o terceiro e o quarto jogador
    /// nunca seriam atacados. Por alvo, cada bruxo enfrenta no maximo dois de cada vez, e
    /// um grupo grande continua sendo um grupo grande.
    ///
    /// Classe pura, com identificadores no lugar de referencias: ela nao precisa
    /// saber o que e uma criatura nem o que e um jogador, e por isso a regra inteira e
    /// verificavel em EditMode sem cena e sem fisica.
    ///
    /// Autoridade: de quem decide, e quem decide e o host (ADR 0008). Duas maquinas
    /// concedendo tokens dariam permissoes diferentes, e o cliente veria uma criatura
    /// atacar enquanto o host acha que ela esta esperando.
    /// </summary>
    public sealed class AttackTokenPool
    {
        readonly Dictionary<ulong, ulong> _targetOf = new Dictionary<ulong, ulong>(16);
        readonly Dictionary<ulong, ulong> _holdersPerTarget = new Dictionary<ulong, ulong>(8);

        int _maxPerTarget = 2;

        /// <summary>Quantos podem golpear o mesmo alvo ao mesmo tempo. Nunca menos que um.</summary>
        public int MaxPerTarget
        {
            get => _maxPerTarget;
            set => _maxPerTarget = value < 1 ? 1 : value;
        }

        /// <summary>Quantos tokens estao concedidos, somando todos os alvos.</summary>
        public int IssuedCount => _targetOf.Count;

        // ---------------------------------------------------------------- consulta

        /// <summary>Quantos estao golpeando este alvo agora.</summary>
        public int HoldersOf(ulong targetId)
            => _holdersPerTarget.TryGetValue(targetId, out ulong count) ? (int)count : 0;

        /// <summary>Se este atacante tem permissao agora.</summary>
        public bool Holds(ulong attackerId) => _targetOf.ContainsKey(attackerId);

        /// <summary>O alvo pelo qual o token foi concedido, ou zero se nao ha token.</summary>
        public ulong TargetOf(ulong attackerId)
            => _targetOf.TryGetValue(attackerId, out ulong target) ? target : 0UL;

        // ---------------------------------------------------------------- comandos

        /// <summary>
        /// Pede permissao para golpear um alvo. Devolve falso quando o alvo ja tem o teto
        /// de atacantes, e recusar e o caso normal: e assim que a terceira criatura espera
        /// em vez de somar o golpe dela ao dos outros dois.
        ///
        /// Pedir de novo pelo mesmo alvo com um token na mao devolve verdadeiro sem
        /// contar duas vezes. Trocar de alvo devolve o token antigo antes de pedir o novo,
        /// senao uma criatura que muda de bruxo no meio da luta vazaria uma permissao e o
        /// alvo antigo ficaria bloqueado para sempre.
        /// </summary>
        public bool TryAcquire(ulong attackerId, ulong targetId)
        {
            if (_targetOf.TryGetValue(attackerId, out ulong current))
            {
                if (current == targetId) return true;

                Release(attackerId);
            }

            if (HoldersOf(targetId) >= _maxPerTarget) return false;

            _targetOf[attackerId] = targetId;
            _holdersPerTarget[targetId] = (ulong)(HoldersOf(targetId) + 1);

            return true;
        }

        /// <summary>
        /// Devolve o token. Devolve falso quando nao havia nenhum, o que e comum: quem
        /// encerra um golpe devolve sem perguntar se chegou a pegar.
        /// </summary>
        public bool Release(ulong attackerId)
        {
            if (!_targetOf.TryGetValue(attackerId, out ulong targetId)) return false;

            _targetOf.Remove(attackerId);

            int remaining = HoldersOf(targetId) - 1;

            // Apagar a entrada em vez de deixar zero: sem isto o dicionario cresce com um
            // alvo por jogador que ja saiu da sessao, e nunca encolhe.
            if (remaining <= 0) _holdersPerTarget.Remove(targetId);
            else _holdersPerTarget[targetId] = (ulong)remaining;

            return true;
        }

        /// <summary>
        /// Devolve todos os tokens concedidos por este alvo. Chamado quando o alvo cai ou
        /// sai da sessao: sem isso, as criaturas que estavam batendo nele continuariam
        /// contando contra um alvo que nao existe mais.
        /// </summary>
        public void ReleaseAllFor(ulong targetId)
        {
            if (!_holdersPerTarget.ContainsKey(targetId)) return;

            // Coletar antes de remover: mexer no dicionario durante a propria iteracao
            // lanca excecao. A lista e curta por natureza, no maximo MaxPerTarget.
            var toRelease = new List<ulong>(_maxPerTarget);

            foreach (KeyValuePair<ulong, ulong> pair in _targetOf)
                if (pair.Value == targetId)
                    toRelease.Add(pair.Key);

            for (int i = 0; i < toRelease.Count; i++)
                _targetOf.Remove(toRelease[i]);

            _holdersPerTarget.Remove(targetId);
        }

        public void Clear()
        {
            _targetOf.Clear();
            _holdersPerTarget.Clear();
        }
    }
}
