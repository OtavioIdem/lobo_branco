namespace LoboBranco.Combat
{
    /// <summary>
    /// A Corrente de Fluxo do docs/03 secao 6: encadear golpes dentro da janela de 0,22 s
    /// que se abre no fim de cada golpe aumenta o dano do proximo, ate o teto de 35%.
    ///
    /// A regra que define o sistema nao e o bonus, e a ausencia de castigo: atacar fora da
    /// janela nao pune ninguem, so reinicia a corrente. Quem ignora Fluxo continua jogando
    /// o jogo, e quem domina se sente esgrimista. Por isso nao existe nada aqui que tire
    /// dano, gaste recurso ou trave estado: a unica coisa que esta classe faz e contar.
    ///
    /// Classe pura, com o tempo entrando por parametro, pelo mesmo motivo do resto do
    /// combate: a linha do tempo de uma corrente de cinco elos e verificavel em EditMode,
    /// em milissegundos, sem cena e sem framerate.
    ///
    /// Autoridade: de quem conta. Em rede, quem conta e o host, porque a corrente muda o
    /// dano e dano e do host (ADR 0008). O host mede o intervalo entre dois pedidos do
    /// mesmo jogador, e nao o relogio dele: a latencia atrasa os dois pedidos junto, entao
    /// o intervalo entre eles sobrevive ao ping.
    ///
    /// Os elos sao o numero que o custo reduzido de vigor (tarefa 1.16) e a adrenalina a
    /// cada dois elos (tarefa 1.17) vao ler quando existirem.
    /// </summary>
    public sealed class FlowChain
    {
        readonly float _windowSeconds;

        float _windowRemaining;

        public FlowChain(float windowSeconds)
        {
            _windowSeconds = windowSeconds;
        }

        /// <summary>Elos da corrente, contando o golpe corrente. Zero fora de combate.</summary>
        public int Links { get; private set; }

        /// <summary>Verdadeiro enquanto da para encadear. E isto que o brilho da lamina mostra (tarefa 1.13).</summary>
        public bool WindowOpen => _windowRemaining > 0f;

        public float WindowRemaining => _windowRemaining > 0f ? _windowRemaining : 0f;

        /// <summary>
        /// Um golpe novo comecou. Devolve os elos ja com este golpe incluso, que e o
        /// numero que vai no <see cref="DamageRequest"/>.
        ///
        /// A janela fecha aqui: durante o golpe nao ha o que encadear, e deixar ela aberta
        /// faria um golpe longo manter a corrente sozinho.
        /// </summary>
        public int Begin()
        {
            Links = WindowOpen ? Links + 1 : 1;
            _windowRemaining = 0f;

            return Links;
        }

        /// <summary>O golpe terminou inteiro. E aqui que a janela se abre.</summary>
        public void Open()
        {
            _windowRemaining = _windowSeconds;
        }

        /// <summary>
        /// Envelhece a janela. Quando ela expira a corrente zera, e e so isso que
        /// acontece: nao ha penalidade nenhuma por ter deixado expirar.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_windowRemaining <= 0f) return;

            _windowRemaining -= deltaTime;

            if (_windowRemaining <= 0f)
            {
                _windowRemaining = 0f;
                Links = 0;
            }
        }

        /// <summary>
        /// Corta a corrente. Um golpe interrompido no meio nunca chega ao fim, entao a
        /// janela dele nunca abriria e o proximo golpe ja recomecaria do primeiro elo.
        /// Isto existe para o numero mostrado concordar com isso na mesma hora, em vez de
        /// ficar parado no valor velho ate o jogador atacar de novo.
        /// </summary>
        public void Break()
        {
            Links = 0;
            _windowRemaining = 0f;
        }
    }
}
