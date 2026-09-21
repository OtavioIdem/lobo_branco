using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>A forma da area de um sinal.</summary>
    public enum SignAreaShape : byte
    {
        /// <summary>A frente de quem conjura, como o abridor e o fogo do docs/03 secao 8.</summary>
        Cone = 0,

        /// <summary>Em volta de quem conjura, inclusive pelas costas.</summary>
        Radius = 1,

        /// <summary>
        /// Quem conjura, e mais ninguem (tarefa 1.18e). O escudo do docs/03 secao 8 nao procura
        /// alvo: ele e o unico sinal cujo alvo ja e conhecido antes de qualquer consulta, e por isso
        /// nao custa fisica nenhuma.
        /// </summary>
        Self = 2,
    }

    /// <summary>
    /// Onde um sinal alcanca, em dado (tarefa 1.18b). E so geometria: quem esta dentro e decidido
    /// aqui, e o que acontece com quem esta dentro e dos efeitos.
    ///
    /// <b>A intensidade nunca muda a area.</b> A area e o que o jogador aprende a mirar, e o
    /// companheiro aprende a ler: um cone que cresce com a Inteligencia faria o mesmo sinal
    /// alcancar distancias diferentes nas maos de duas escolas, e nenhum dos dois saberia onde
    /// ficar. A intensidade muda o quanto, e a area fica igual para todo mundo.
    ///
    /// A altura nao entra na conta. A sandbox e plana e o capitulo I e quase todo chao; quando uma
    /// criatura em cima de um muro precisar ficar de fora, e aqui que a regra entra.
    /// </summary>
    [Serializable]
    public struct SignArea
    {
        [Tooltip("Cone a frente, ou raio em volta.")]
        public SignAreaShape shape;

        [Tooltip("Alcance em metros. Zero quer dizer que a habilidade nao tem area.")]
        [Min(0f)] public float range;

        [Tooltip("Abertura total do cone, em graus. Ignorado no raio.")]
        [Range(0f, 360f)] public float coneAngleDegrees;

        /// <summary>
        /// A habilidade alcanca alguem. Em quem conjura isso vale sempre, porque o alvo nao depende
        /// de distancia; nas outras formas, um alcance de zero nao alcanca ninguem.
        /// </summary>
        public bool HasArea => shape == SignAreaShape.Self || range > 0f;

        /// <summary>
        /// Se um ponto esta dentro da abertura. Nao confere o alcance: quem ja filtrou por
        /// distancia foi a consulta de fisica, contra a superficie do colisor, e conferir de novo
        /// contra o centro cortaria a criatura grande que so encosta na borda.
        /// </summary>
        /// <param name="planarForward">Frente de quem conjura, ja achatada e normalizada.</param>
        public bool IsWithinAngle(Vector3 origin, Vector3 planarForward, Vector3 point)
        {
            if (shape != SignAreaShape.Cone) return true;

            Vector3 toPoint = point - origin;
            toPoint.y = 0f;

            // Colado em quem conjura nao tem direcao. Deixar de fora quem esta dentro do bruxo
            // seria o sinal errar justamente o inimigo mais perto.
            if (toPoint.sqrMagnitude < 0.0001f) return true;

            return Vector3.Angle(planarForward, toPoint) <= coneAngleDegrees * 0.5f;
        }

        /// <summary>Escreve o que impede esta area de funcionar. Nada escrito quer dizer que ela esta pronta.</summary>
        public void CollectProblems(List<string> problems)
        {
            if (problems == null || !HasArea) return;

            if (shape == SignAreaShape.Cone && coneAngleDegrees <= 0f)
                problems.Add("e um cone de abertura zero, e nao alcanca ninguem");
        }
    }
}
