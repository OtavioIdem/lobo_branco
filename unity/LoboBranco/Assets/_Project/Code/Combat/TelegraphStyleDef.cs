using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Como um telegrafo aparece: cor do aviso, cor do golpe, e quanto o corpo se encolhe.
    ///
    /// Os <em>tempos</em> nao moram aqui, e isso e deliberado: eles sao os do
    /// <see cref="AttackDef"/>, os mesmos que abrem a hitbox. Este asset so decide a
    /// aparencia, entao trocar a cor nunca muda quando o golpe acerta.
    ///
    /// Enquanto nao houver Animator (tech/adr/0007), o encolher do corpo e a pose de
    /// anticipacao que o docs/08 secao 3 pede: em greybox, a capsula que se abaixa antes de
    /// saltar e a unica silhueta de ataque que existe.
    /// </summary>
    [CreateAssetMenu(menuName = "LoboBranco/Combat/Telegraph Style", fileName = "TelegraphStyle_")]
    public sealed class TelegraphStyleDef : ScriptableObject
    {
        [Header("Anticipacao")]
        [Tooltip("Cor do aviso. Nunca vermelho: vermelho e o tell de Unblockable (docs/03 secao 5), " +
                 "e um aviso comum vermelho ensinaria ao jogador a resposta errada.")]
        public Color warningColor = new Color(1f, 0.72f, 0.18f);

        [Tooltip("Intensidade do aviso no primeiro quadro. Comecar apagado desperdica metade da anticipacao.")]
        [Range(0f, 1f)] public float startWarning = 0.35f;

        [Tooltip("Quanto o corpo se abaixa no fim da anticipacao, como fracao da altura.")]
        [Range(0f, 0.5f)] public float crouch = 0.15f;

        [Header("Janela de dano")]
        [Tooltip("Cor enquanto o golpe acerta. Apaga durante a recuperacao.")]
        public Color strikeColor = new Color(1f, 0.97f, 0.88f);
    }
}
