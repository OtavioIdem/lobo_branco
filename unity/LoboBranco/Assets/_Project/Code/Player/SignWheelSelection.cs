using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// A escolha do sinal (tarefa 1.18g): qual vaga esta selecionada, e como a roda muda isso.
    ///
    /// Classe pura, sem input e sem camera, pela regra 4 do CLAUDE.md: ela recebe uma direcao por
    /// metodo e nao sabe se veio de mouse, de analogico ou de teste. E o que permite verificar a
    /// conta de angulo sem simular teclado.
    ///
    /// <b>Autoridade: ninguem.</b> Escolher sinal e local do dono, como escolher postura: nada disso
    /// viaja. O que viaja e a vaga, uma vez, quando a conjuracao e pedida (tech/adr/0011).
    ///
    /// A roda <b>nunca desacelera o tempo</b> (tech/adr/0010). Ela nao mexe em tempo nenhum: o mundo
    /// corre enquanto o bruxo escolhe, e escolher no meio da luta e uma decisao com risco.
    /// </summary>
    public sealed class SignWheelSelection
    {
        /// <summary>
        /// Quanto o ponteiro precisa andar para trocar a vaga. Abaixo disto, abrir a roda e soltar
        /// sem mirar mantem o sinal que ja estava, que e o que evita trocar de sinal sem querer.
        /// </summary>
        public float PointerDeadZone { get; set; } = 0.35f;

        int _slotCount;

        /// <summary>Quantas vagas a escola usa. Mudar isto nunca deixa a selecao fora da faixa.</summary>
        public int SlotCount
        {
            get => _slotCount;
            set
            {
                _slotCount = Mathf.Max(0, value);
                Selected = _slotCount == 0 ? 0 : Mathf.Clamp(Selected, 0, _slotCount - 1);
            }
        }

        /// <summary>A vaga que o botao de sinal usa.</summary>
        public int Selected { get; private set; }

        /// <summary>A roda esta aberta.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>Para onde o ponteiro aponta agora, de 0 a 1. Zero quer dizer centro.</summary>
        public Vector2 Pointer { get; private set; }

        /// <summary>Abre a roda. O ponteiro comeca no centro, entao a vaga so muda se alguem mirar.</summary>
        public void Open()
        {
            IsOpen = true;
            Pointer = Vector2.zero;
        }

        /// <summary>Fecha a roda. A vaga escolhida fica; fechar nao conjura nada.</summary>
        public void Close()
        {
            IsOpen = false;
            Pointer = Vector2.zero;
        }

        /// <summary>
        /// Move o ponteiro. Fora da zona morta, a vaga passa a ser a do angulo apontado.
        /// Ignorado com a roda fechada: mexer o mouse na luta nao pode trocar de sinal.
        /// </summary>
        public void Point(Vector2 delta)
        {
            if (!IsOpen || _slotCount == 0) return;

            Vector2 pointer = Pointer + delta;

            // Preso ao circulo: mouse anda em pixels e analogico anda de -1 a 1, e o que importa
            // para a escolha e a direcao. Sem isto, um movimento de mouse deixaria o ponteiro a
            // dezenas de unidades do centro e voltar ao centro levaria o mesmo caminho de volta.
            if (pointer.sqrMagnitude > 1f) pointer = pointer.normalized;

            Pointer = pointer;

            if (pointer.magnitude < PointerDeadZone) return;

            Selected = SlotAt(pointer);
        }

        /// <summary>
        /// Escolhe pela tecla direta. Devolve falso quando a vaga nao existe na escola, e nesse caso
        /// nada muda: a tecla de um sinal que a escola nao tem nao pode apagar a selecao.
        /// </summary>
        public bool SelectDirect(int slot)
        {
            if (slot < 0 || slot >= _slotCount) return false;

            Selected = slot;
            return true;
        }

        /// <summary>
        /// A vaga sob uma direcao. A vaga 0 fica em cima e as outras seguem no sentido horario, que
        /// e a ordem em que a roda e desenhada.
        /// </summary>
        public int SlotAt(Vector2 direction)
        {
            if (_slotCount <= 0) return 0;
            if (direction.sqrMagnitude < 0.000001f) return Selected;

            // Atan2(x, y), e nao (y, x): o zero fica em cima, e nao a direita.
            float degrees = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            if (degrees < 0f) degrees += 360f;

            float perSlot = 360f / _slotCount;
            int slot = Mathf.RoundToInt(degrees / perSlot) % _slotCount;

            return slot;
        }

        /// <summary>O angulo do centro de uma vaga, em graus, com zero em cima.</summary>
        public float AngleOf(int slot) => _slotCount <= 0 ? 0f : slot * (360f / _slotCount);
    }
}
