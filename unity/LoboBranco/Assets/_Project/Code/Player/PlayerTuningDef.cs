using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// Os numeros de sensacao do jogador que nao pertencem nem a locomocao nem ao dano.
    ///
    /// Um asset para dois campos parece exagero, e nao e: os dois so podem ser julgados
    /// com o jogo rodando, porque o valor certo deles e questao de tato e nao de calculo.
    /// Recompilar entre cada tentativa mataria o ajuste.
    /// </summary>
    [CreateAssetMenu(menuName = "LoboBranco/Player/Tuning", fileName = "PlayerTuning")]
    public sealed class PlayerTuningDef : ScriptableObject
    {
        [Header("Buffer de input (docs/07 secao 4.4)")]
        [Tooltip("Quanto tempo o ultimo input espera por uma janela. Abaixo de 0,1 o combate " +
                 "parece que ignora o clique; acima de 0,3 ele parece que joga sozinho.")]
        [Range(0f, 0.5f)] public float inputBufferSeconds = 0.2f;

        [Header("Postura (docs/03 secao 4)")]
        [Tooltip("Tempo da troca de postura. E o custo que transforma postura em decisao: " +
                 "perto de zero, trocar deixa de ser aposta e o inimigo perde o sentido.")]
        [Range(0f, 1f)] public float stanceSwitchSeconds = 0.25f;

        [Header("Espada (docs/03 secao 3)")]
        [Tooltip("Tempo de guardar uma espada e sacar a outra. Nem a esquiva corta isso: e o " +
                 "custo que faz a escolha entre aco e prata ser um risco e nao uma conveniencia.")]
        [Range(0f, 2f)] public float weaponSwapSeconds = 0.7f;
    }
}
