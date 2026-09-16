using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Um efeito de sinal que tira o controle da criatura, com uma resposta por porte (tarefa
    /// 1.18c). O abridor do docs/03 secao 8 e um asset deste tipo: derruba leves, atordoa medios e
    /// nao move pesados.
    ///
    /// Autoridade: o host, como todo efeito de sinal (tech/adr/0012). Quem aplica e o
    /// <see cref="ControlStatus"/>, que ja so aceita escrita de quem resolve e ja replica o
    /// resultado; este asset so decide qual controle e por quanto tempo.
    ///
    /// A resposta e por porte, e nao por limiar de atributo, porque e assim que o documento fala e
    /// e assim que o jogador aprende: "o abridor derruba barghest" e uma regra que cabe numa
    /// entrada de bestiario, e "derruba abaixo de 30 de resistencia" nao cabe.
    ///
    /// A duracao escala com a intensidade, e o tipo de controle nao. Um Grifo de Inteligencia alta
    /// segura o barghest no chao por mais tempo, e nunca derruba o que o documento diz que so
    /// atordoa: promover o porte mudaria a regra que o bestiario ensina.
    /// </summary>
    [CreateAssetMenu(menuName = "LoboBranco/Combat/Sign Effect/Control", fileName = "SignEffect_Control_")]
    public sealed class ControlEffectDef : SignEffectDef
    {
        /// <summary>O que o efeito faz com um porte.</summary>
        [Serializable]
        public struct Response
        {
            [Tooltip("None nao faz nada com este porte.")]
            public ControlKind control;

            [Tooltip("Segundos com a intensidade de referencia. A intensidade de quem conjura multiplica isto.")]
            [Min(0f)] public float seconds;

            public bool DoesSomething => control != ControlKind.None && seconds > 0f;
        }

        [Tooltip("Criaturas leves. docs/03 secao 8: o abridor derruba.")]
        public Response light = new Response { control = ControlKind.KnockedDown, seconds = 2f };

        [Tooltip("Criaturas medias. docs/03 secao 8: o abridor atordoa por 1,5 s.")]
        public Response medium = new Response { control = ControlKind.Stunned, seconds = 1.5f };

        [Tooltip("Criaturas pesadas. docs/03 secao 8 e 11: pesados nem se movem.")]
        public Response heavy;

        public Response ResponseFor(BodyWeight weight) => weight switch
        {
            BodyWeight.Light => light,
            BodyWeight.Heavy => heavy,
            _ => medium,
        };

        /// <summary>
        /// Uma criatura sem <see cref="ControlStatus"/> ignora o efeito sem erro. E o alvo parado
        /// da sandbox, que so pisca e levanta, e nao a criatura de verdade.
        /// </summary>
        public override void Apply(in SignCast cast, CharacterVitals target)
        {
            if (target == null || !target.TryGetComponent(out ControlStatus status)) return;

            BodyWeight weight = target.TryGetComponent(out DamageReceiver receiver)
                ? receiver.BodyWeight
                : BodyWeight.Medium;

            Response response = ResponseFor(weight);
            if (!response.DoesSomething) return;

            float seconds = cast.Scale(response.seconds);

            switch (response.control)
            {
                case ControlKind.KnockedDown:
                    status.ApplyKnockdown(seconds);
                    break;
                case ControlKind.Stunned:
                    status.ApplyStun(seconds);
                    break;
            }
        }

        public override void CollectProblems(List<string> problems)
        {
            if (problems == null) return;

            CheckResponse("leve", light, problems);
            CheckResponse("medio", medium, problems);
            CheckResponse("pesado", heavy, problems);

            if (!light.DoesSomething && !medium.DoesSomething && !heavy.DoesSomething)
                problems.Add("nao controla porte nenhum, e o sinal cobra por nada");
        }

        static void CheckResponse(string label, Response response, List<string> problems)
        {
            if (response.control != ControlKind.None && response.seconds <= 0f)
                problems.Add($"{(response.control == ControlKind.KnockedDown ? "derruba" : "atordoa")} o porte {label} por zero segundos");
        }
    }
}
