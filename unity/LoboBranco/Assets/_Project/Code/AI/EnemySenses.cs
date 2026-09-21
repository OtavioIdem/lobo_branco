using UnityEngine;

namespace LoboBranco.AI
{
    /// <summary>
    /// O que uma criatura percebe e por quanto tempo ela lembra (docs/07 secao 6).
    ///
    /// Sao tres regras, e a terceira e a que faz diferenca em jogo:
    ///
    /// Visao e um cone. Alcance mais meia abertura, medidos no plano: um barghest nao
    /// ve o bruxo pelas costas, e contornar um grupo e uma jogada legitima.
    ///
    /// Audicao e um circulo, e ela nao depende de estar olhando. Sem ela, um inimigo com
    /// cone de 120 graus seria contornavel por qualquer um, sempre, e o combate viraria um
    /// exercicio de andar de lado.
    ///
    /// Perder de vista nao e esquecer. Depois que ela perde o alvo, ela continua cacando
    /// por <see cref="LoseTargetAfter"/> segundos. E o que impede o pior comportamento de
    /// IA que existe, o inimigo que desiste no instante em que voce quebra a linha de
    /// visao e volta a patrulhar com o bruxo a dois metros dele.
    ///
    /// Classe pura, com o tempo entrando por parametro e sem consultar fisica: a linha de
    /// visao chega pronta de quem chamou. E por isso que a matematica do cone e da memoria
    /// e verificavel em EditMode, sem cena, sem colisor e sem framerate.
    ///
    /// Autoridade: de quem decide, e quem decide e o host (ADR 0008). Cliente nenhum roda
    /// isto, porque IA e do host e dois lados percebendo em momentos diferentes dariam
    /// dois inimigos diferentes na mesma sessao.
    /// </summary>
    public sealed class EnemySenses
    {
        /// <summary>Ate onde ela enxerga, em metros.</summary>
        public float SightRange { get; set; } = 18f;

        /// <summary>Metade da abertura do cone, em graus. 60 da um campo de 120.</summary>
        public float SightHalfAngle { get; set; } = 60f;

        /// <summary>Raio em que ela percebe sem ver. Zero desliga o ouvido.</summary>
        public float HearingRange { get; set; } = 6f;

        /// <summary>Segundos de caca depois de perder o alvo de vista.</summary>
        public float LoseTargetAfter { get; set; } = 4f;

        // ---------------------------------------------------------------- leitura

        /// <summary>Verdadeiro enquanto ela esta cacando, vendo o alvo ou nao.</summary>
        public bool Aware { get; private set; }

        /// <summary>Segundos desde a ultima percepcao. Zero enquanto ela percebe.</summary>
        public float TimeSincePerceived { get; private set; }

        // ------------------------------------------------------------- percepcao

        /// <summary>
        /// Se o alvo esta perceptivel agora. Nao mexe em nada: e uma pergunta geometrica.
        /// </summary>
        /// <param name="lineOfSightClear">
        /// Falso quando ha parede no caminho. So vale para a visao: audicao atravessa.
        /// </param>
        public bool Perceives(Vector3 self, Vector3 forward, Vector3 target, bool lineOfSightClear)
        {
            Vector3 toTarget = target - self;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;

            // Ouvir vem antes de ver porque ela nao precisa nem estar virada.
            if (distance <= HearingRange) return true;

            if (distance > SightRange) return false;
            if (!lineOfSightClear) return false;

            // Alvo em cima do proprio nariz nao tem direcao. Estar nessa distancia ja e
            // perceber: recusar aqui faria a criatura ficar cega a quem a esta abracando.
            if (distance < 0.001f) return true;

            Vector3 planar = new Vector3(forward.x, 0f, forward.z);
            if (planar.sqrMagnitude < 0.000001f) return false;

            return Vector3.Angle(planar, toTarget) <= SightHalfAngle;
        }

        /// <summary>
        /// Envelhece a memoria com o que foi percebido neste passo. Devolve
        /// <see cref="Aware"/> ja atualizado, que e a resposta que a arvore consulta.
        /// </summary>
        public bool Tick(float deltaTime, bool perceivedNow)
        {
            if (perceivedNow)
            {
                Aware = true;
                TimeSincePerceived = 0f;
                return true;
            }

            if (!Aware) return false;

            TimeSincePerceived += deltaTime;

            if (TimeSincePerceived >= LoseTargetAfter)
            {
                Aware = false;
                TimeSincePerceived = 0f;
            }

            return Aware;
        }

        /// <summary>
        /// Esquece na hora, sem esperar a memoria expirar. Quem chama e quem sabe que o
        /// alvo deixou de ser alvo: ele caiu, saiu da sessao, ou a criatura caiu.
        /// </summary>
        public void Forget()
        {
            Aware = false;
            TimeSincePerceived = 0f;
        }
    }
}
