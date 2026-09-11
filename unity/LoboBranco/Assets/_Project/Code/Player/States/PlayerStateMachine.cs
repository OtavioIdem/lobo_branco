using System;
using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// A maquina de estados do jogador, escrita a mao (docs/07 secao 4.4).
    ///
    /// Sem plugin e sem cadeia de <c>if</c> em <c>Update</c>. A diferenca pratica nao e
    /// estetica: com estados como objetos, a pergunta "o que pode cancelar um ataque?"
    /// tem uma resposta unica em <see cref="PlayerStateRules"/>, e existe teste para ela.
    /// Com <c>if</c> espalhado, a resposta e "depende de qual linha rodou primeiro".
    ///
    /// Custo por frame: uma indexacao de vetor e uma chamada virtual. Zero alocacao
    /// depois do registro dos estados, que acontece uma vez no Awake.
    /// </summary>
    public sealed class PlayerStateMachine
    {
        readonly IPlayerState[] _states;
        readonly PlayerStateContext _context;

        // Um bit por estado, para avisar uma unica vez sobre um estado nao registrado.
        // Sem isto, uma tecla de esquiva antes da tarefa 1.10 existir enche o console
        // com o mesmo aviso sessenta vezes por segundo.
        int _warned;

        public IPlayerState Current { get; private set; }
        public PlayerStateId CurrentId { get; private set; }

        /// <summary>Segundos desde a ultima troca. Zerado em toda transicao.</summary>
        public float TimeInState { get; private set; }

        /// <summary>Argumentos: estado anterior, estado novo.</summary>
        public event Action<PlayerStateId, PlayerStateId> StateChanged;

        public PlayerStateMachine(PlayerStateContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.Machine = this;

            _states = new IPlayerState[Enum.GetValues(typeof(PlayerStateId)).Length];
        }

        // ---------------------------------------------------------------- registro

        public void Register(IPlayerState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            int index = (int)state.Id;
            if (index < 0 || index >= _states.Length)
                throw new ArgumentOutOfRangeException(nameof(state), $"Id fora da faixa: {state.Id}");

            _states[index] = state;
        }

        public bool Has(PlayerStateId id)
        {
            int index = (int)id;
            return index >= 0 && index < _states.Length && _states[index] != null;
        }

        /// <summary>Entra no estado inicial. Nao passa pela regra de transicao: nao ha de onde sair.</summary>
        public void Start(PlayerStateId id)
        {
            if (!Has(id))
                throw new InvalidOperationException($"Estado inicial {id} nao registrado.");

            Current = _states[(int)id];
            CurrentId = id;
            TimeInState = 0f;
            Current.OnEnter(_context);
        }

        // -------------------------------------------------------------- transicao

        /// <summary>
        /// Se a transicao seria aceita agora. Nao muda nada, e e o que a UI e o painel de
        /// debug devem consultar em vez de tentar a troca para descobrir.
        /// </summary>
        public bool CanEnter(PlayerStateId next)
        {
            if (!Has(next)) return false;
            if (Current == null) return true;
            if (next == CurrentId) return false;

            return Current.CanTransitionTo(next);
        }

        /// <summary>
        /// Tenta trocar de estado respeitando a regra de ouro. Devolve falso quando a
        /// transicao foi recusada, e recusa e o caso normal: e assim que um ataque em
        /// andamento ignora o segundo clique.
        /// </summary>
        public bool TryChangeState(PlayerStateId next)
        {
            if (!Has(next))
            {
                WarnMissing(next);
                return false;
            }

            if (!CanEnter(next)) return false;

            Switch(next);
            return true;
        }

        /// <summary>
        /// Troca ignorando <see cref="IPlayerState.CanTransitionTo"/>.
        ///
        /// Existe para dois casos, e so eles: o estado que terminou sozinho e volta para a
        /// locomocao (terminar nao e ser interrompido, entao a regra de ouro nao se
        /// aplica), e uma imposicao do jogo como morte ou dialogo. Usar isto para atender
        /// um input e furar a regra de ouro por um caminho lateral.
        /// </summary>
        public void ForceChangeState(PlayerStateId next)
        {
            if (!Has(next))
            {
                WarnMissing(next);
                return;
            }

            if (next == CurrentId)
            {
                // Reentrada explicita: sair e entrar de novo, para o estado se reiniciar.
                Current.OnExit(_context);
                TimeInState = 0f;
                Current.OnEnter(_context);
                return;
            }

            Switch(next);
        }

        void Switch(PlayerStateId next)
        {
            PlayerStateId previous = CurrentId;

            Current?.OnExit(_context);

            Current = _states[(int)next];
            CurrentId = next;
            TimeInState = 0f;

            Current.OnEnter(_context);
            StateChanged?.Invoke(previous, next);
        }

        // ------------------------------------------------------------------- tick

        public void Tick(float deltaTime)
        {
            if (Current == null || deltaTime <= 0f) return;

            TimeInState += deltaTime;
            Current.OnTick(_context, deltaTime);
        }

        void WarnMissing(PlayerStateId id)
        {
            int bit = 1 << (int)id;
            if ((_warned & bit) != 0) return;

            _warned |= bit;
            Debug.LogWarning($"[FSM] Estado {id} ainda nao foi implementado. Transicao ignorada.");
        }
    }
}
