using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>Acoes que podem ficar guardadas esperando a hora de sair.</summary>
    public enum BufferedAction
    {
        None = 0,
        AttackLight = 1,
        AttackHeavy = 2,
        Dodge = 3,
        Parry = 4,
        CastSign = 5,
        UseItem = 6,
        Interact = 7,
    }

    /// <summary>
    /// Guarda o ultimo input por uma janela curta, 0,2 s por padrao (docs/07 secao 4.4).
    ///
    /// Por que isso existe: com a regra de ouro do docs/03, um ataque em andamento recusa
    /// todo input. Sem buffer, apertar ataque 50 ms antes da recuperacao acabar joga o
    /// comando no lixo, e o jogador sente que o jogo ignorou o clique dele. O documento e
    /// direto sobre isso: e a causa numero um de "meu combate esta estranho", e nenhum
    /// ajuste de numero conserta.
    ///
    /// Sem <c>Time.deltaTime</c> aqui dentro: o tempo entra por <see cref="Tick"/>, que e
    /// o que faz a expiracao ser verificavel com passos fixos em vez de "rodar e torcer".
    /// </summary>
    public sealed class InputBuffer
    {
        /// <summary>Duracao da janela, em segundos. Vem do <c>PlayerTuningDef</c>.</summary>
        public float Window { get; set; } = 0.2f;

        BufferedAction _pending;
        float _remaining;

        /// <summary>Acao guardada, ou <see cref="BufferedAction.None"/> se nao ha nenhuma valida.</summary>
        public BufferedAction Pending => _remaining > 0f ? _pending : BufferedAction.None;

        /// <summary>Quanto tempo a acao guardada ainda vale.</summary>
        public float Remaining => _remaining;

        public bool HasPending => Pending != BufferedAction.None;

        /// <summary>
        /// Guarda uma acao, substituindo a anterior.
        ///
        /// Substituir e a escolha certa e nao e obvia: uma fila entregaria o ataque antigo
        /// depois que o jogador ja mudou de ideia e apertou esquiva. O ultimo input e o que
        /// o jogador quer agora, e e por isso que ele vence.
        /// </summary>
        public void Push(BufferedAction action)
        {
            if (action == BufferedAction.None)
            {
                Clear();
                return;
            }

            _pending = action;
            _remaining = Window;
        }

        /// <summary>Envelhece a acao guardada. Chamar uma vez por frame.</summary>
        public void Tick(float deltaTime)
        {
            if (_remaining <= 0f || deltaTime <= 0f) return;

            _remaining -= deltaTime;

            if (_remaining <= 0f)
            {
                _remaining = 0f;
                _pending = BufferedAction.None;
            }
        }

        /// <summary>
        /// Consome a acao guardada se ela for exatamente <paramref name="action"/>.
        /// Devolve falso sem consumir nada em qualquer outro caso.
        /// </summary>
        public bool TryConsume(BufferedAction action)
        {
            if (action == BufferedAction.None) return false;
            if (Pending != action) return false;

            Clear();
            return true;
        }

        /// <summary>Consome o que estiver guardado e devolve o que era.</summary>
        public BufferedAction ConsumeAny()
        {
            BufferedAction action = Pending;
            if (action != BufferedAction.None) Clear();

            return action;
        }

        /// <summary>
        /// Esquece o que estava guardado. Chamar ao entrar em dialogo, ao morrer e ao
        /// carregar cena: um ataque guardado que sai sozinho tres segundos depois e um
        /// bug que ninguem consegue reproduzir.
        /// </summary>
        public void Clear()
        {
            _pending = BufferedAction.None;
            _remaining = 0f;
        }

        /// <summary>Fracao de 0 a 1 do que resta da janela. Para o painel de debug.</summary>
        public float NormalizedRemaining => Window > 0f ? Mathf.Clamp01(_remaining / Window) : 0f;
    }
}
