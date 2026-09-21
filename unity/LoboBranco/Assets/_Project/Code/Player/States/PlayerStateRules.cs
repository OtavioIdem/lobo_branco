namespace LoboBranco.Player
{
    /// <summary>
    /// A regra de ouro do combate, em um lugar so.
    ///
    /// docs/03 secao 1: <em>nenhum input cancela uma animacao ja iniciada, exceto a
    /// esquiva, e a esquiva custa vigor.</em> Todo o peso tatico do combate nasce dessa
    /// unica frase: se um ataque pudesse ser abortado sem custo, comprometer-se com um
    /// golpe deixaria de ser uma decisao e o combate viraria clique.
    ///
    /// Ela vive aqui, e nao espalhada pelos estados, porque uma regra copiada em doze
    /// arquivos e uma regra que vai divergir em doze arquivos. Quem quiser saber o que o
    /// jogo permite cancelar le esta classe, e o teste que protege a regra tambem.
    /// </summary>
    public static class PlayerStateRules
    {
        /// <summary>
        /// Os unicos destinos que atravessam uma acao comprometida.
        ///
        /// <see cref="PlayerStateId.Dodge"/> e <see cref="PlayerStateId.Roll"/> sao a
        /// excecao que o documento concede ao jogador, e sao pagas com vigor.
        /// Os outros tres nao sao input: sao o mundo agindo sobre o jogador. Um golpe
        /// que atordoa, a morte e o inicio de um dialogo tem que poder interromper
        /// qualquer coisa, senao o personagem fica preso na propria animacao.
        /// </summary>
        public static bool CancelsCommittedAction(PlayerStateId next)
        {
            switch (next)
            {
                case PlayerStateId.Dodge:
                case PlayerStateId.Roll:
                case PlayerStateId.Stagger:
                case PlayerStateId.Death:
                case PlayerStateId.DialogueLocked:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Verdadeiro se um estado pode ceder o lugar agora.
        /// Estado nao comprometido aceita qualquer transicao.
        /// </summary>
        public static bool CanInterrupt(bool committed, PlayerStateId next)
            => !committed || CancelsCommittedAction(next);

        /// <summary>
        /// Trocar de postura pode durante o deslocamento e nao pode durante um golpe
        /// (docs/03 secao 4). A pergunta e a mesma de sempre: o que esta rodando agora e
        /// uma acao comprometida?
        ///
        /// Mora aqui junto com a regra de ouro para nao virar um segundo lugar onde se
        /// decide o que interrompe o que.
        /// </summary>
        public static bool CanSwitchStance(bool committed) => !committed;
    }
}
