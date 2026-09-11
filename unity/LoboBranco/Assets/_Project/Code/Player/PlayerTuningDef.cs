using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// Os numeros de sensacao do jogador que nao pertencem nem a locomocao nem ao dano.
    ///
    /// Um asset para um campo so parece exagero, e nao e: a janela de buffer e o numero
    /// que mais precisa ser ajustado com o jogo rodando, porque o valor certo dela e uma
    /// questao de tato e nao de calculo. Recompilar entre cada tentativa mataria o ajuste.
    /// </summary>
    [CreateAssetMenu(menuName = "LoboBranco/Player/Tuning", fileName = "PlayerTuning")]
    public sealed class PlayerTuningDef : ScriptableObject
    {
        [Header("Buffer de input (docs/07 secao 4.4)")]
        [Tooltip("Quanto tempo o ultimo input espera por uma janela. Abaixo de 0,1 o combate " +
                 "parece que ignora o clique; acima de 0,3 ele parece que joga sozinho.")]
        [Range(0f, 0.5f)] public float inputBufferSeconds = 0.2f;
    }
}
