namespace LoboBranco.Player
{
    /// <summary>
    /// O contrato de um estado do jogador (docs/07 secao 4.4).
    ///
    /// Quatro metodos, e o tempo entra por parametro em <see cref="OnTick"/> pelo mesmo
    /// motivo que entra em <c>PlayerLocomotion.Tick</c>: <c>Time.deltaTime</c> e estado
    /// ambiente, e um estado que le o relogio sozinho nao pode ser verificado com passos
    /// fixos. Com o delta explicito, o teste roda a linha do tempo de um ataque inteiro
    /// em EditMode, em milissegundos, e o resultado e o mesmo em toda execucao.
    /// </summary>
    public interface IPlayerState
    {
        PlayerStateId Id { get; }

        void OnEnter(PlayerStateContext context);

        void OnTick(PlayerStateContext context, float deltaTime);

        void OnExit(PlayerStateContext context);

        /// <summary>
        /// Se este estado aceita ceder o lugar para <paramref name="next"/> agora.
        /// A implementacao padrao delega para <see cref="PlayerStateRules"/>; sobrescrever
        /// so faz sentido para uma excecao especifica de um estado, e essa excecao merece
        /// um comentario dizendo por que ela existe.
        /// </summary>
        bool CanTransitionTo(PlayerStateId next);
    }

    /// <summary>
    /// Base com o comportamento neutro. Um estado novo declara so o que faz de diferente,
    /// e herda a regra de ouro sem ter que lembrar dela.
    /// </summary>
    public abstract class PlayerStateBase : IPlayerState
    {
        public abstract PlayerStateId Id { get; }

        /// <summary>
        /// Verdadeiro enquanto a acao esta comprometida e nao pode ser abortada.
        /// Locomocao e falso; ataque, esquiva e sinal sao verdadeiros.
        /// </summary>
        public virtual bool IsCommitted => false;

        public virtual void OnEnter(PlayerStateContext context) { }

        public virtual void OnTick(PlayerStateContext context, float deltaTime) { }

        public virtual void OnExit(PlayerStateContext context) { }

        public virtual bool CanTransitionTo(PlayerStateId next)
            => PlayerStateRules.CanInterrupt(IsCommitted, next);
    }
}
