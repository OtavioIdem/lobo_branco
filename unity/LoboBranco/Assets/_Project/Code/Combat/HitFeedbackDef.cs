using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Os numeros de sensacao de um golpe conectado: quanto ele congela, quanto a camera
    /// treme e quanto ela e empurrada (docs/03 secao 11).
    ///
    /// Moram em asset pela regra 1 do CLAUDE.md, e com mais motivo do que um multiplicador
    /// de dano: o docs/03 diz que setenta por cento da sensacao de bom combate vem desta
    /// secao, entao estes sao os numeros que mais vao ser mexidos com o jogo rodando.
    ///
    /// O hitstop daqui <b>nunca</b> vira <c>Time.timeScale</c>. Em coop a escala de tempo e
    /// global na maquina, e no host ela congelaria a simulacao de todos os jogadores. O
    /// hitstop estende o golpe de quem bateu, pela mesma duracao nas duas pontas da rede
    /// (tech/adr/0010).
    /// </summary>
    [CreateAssetMenu(menuName = "LoboBranco/Combat/Hit Feedback", fileName = "HitFeedback_")]
    public sealed class HitFeedbackDef : ScriptableObject
    {
        [Header("Hitstop, em segundos (docs/03 secao 11)")]
        [Tooltip("Golpe Forte. O documento pede 0,08 s.")]
        [Min(0f)] public float hitstopStrong = 0.08f;

        [Tooltip("Golpe Rapido. O documento pede 0,04 s para o golpe leve.")]
        [Min(0f)] public float hitstopFast = 0.04f;

        [Tooltip("Golpe de Grupo. O documento nao da numero: fica entre os dois porque acerta " +
                 "varios alvos, e congelar muito com varios alvos parece travamento.")]
        [Min(0f)] public float hitstopGroup = 0.05f;

        [Header("Tremor de camera")]
        [Tooltip("Forca do Impulse por ponto de dano. Proporcional, como pede o documento.")]
        [Min(0f)] public float shakePerDamage = 0.02f;

        [Tooltip("Teto da forca. Sem teto, um critico da Besta sacode a tela ate ninguem ver o tell seguinte.")]
        [Min(0f)] public float shakeMax = 0.6f;

        [Tooltip("Quanto o tremor dura, em segundos. Mais longo que o golpe seguinte e o tremor " +
                 "de um golpe passa a atrapalhar a leitura do proximo telegrafo.")]
        [Min(0.01f)] public float shakeDuration = 0.18f;

        [Header("Soco de camera")]
        [Tooltip("Graus de deslocamento no impacto. O documento pede 2.")]
        [Min(0f)] public float punchDegrees = 2f;

        [Tooltip("Segundos para a camera voltar ao lugar.")]
        [Min(0f)] public float punchRecovery = 0.18f;

        /// <summary>Quanto o golpe desta postura congela ao conectar.</summary>
        public float HitstopFor(Stance stance) => stance switch
        {
            Stance.Strong => hitstopStrong,
            Stance.Group => hitstopGroup,
            _ => hitstopFast,
        };

        /// <summary>Forca do tremor para um golpe deste dano. Zero para dano nenhum.</summary>
        public float ShakeFor(float damage)
            => damage <= 0f ? 0f : Mathf.Min(shakeMax, damage * shakePerDamage);
    }
}
