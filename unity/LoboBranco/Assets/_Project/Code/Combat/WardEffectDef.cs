using System.Collections.Generic;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Um efeito de sinal que ergue o escudo do docs/03 secao 8 (tarefa 1.18e): absorve um golpe por
    /// 8 s e devolve 30% do dano ao quebrar.
    ///
    /// A intensidade escala <b>o que volta</b>, e nao a duracao. O escudo absorve um golpe inteiro,
    /// seja ele de 8 ou de 80, entao nao ha "escudo mais forte": a unica potencia que existe nele e o
    /// troco. Uma duracao que crescesse com a Inteligencia tambem faria o sinal deixar de ser
    /// decisao de quando erguer, que e o compromisso que o documento pede.
    ///
    /// Quem aplica e o host, e o alvo e quem conjurou: a area deste sinal e a forma <c>Self</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "LoboBranco/Combat/Sign Effect/Ward", fileName = "SignEffect_Ward_")]
    public sealed class WardEffectDef : SignEffectDef
    {
        [Tooltip("Quanto tempo o escudo espera por um golpe. docs/03 secao 8: 8 s.")]
        [Min(0f)] public float seconds = 8f;

        [Tooltip("Fracao do dano absorvido que volta para quem bateu. docs/03 secao 8: 0,3.")]
        [Min(0f)] public float reflectFraction = 0.3f;

        public override void Apply(in SignCast cast, CharacterVitals target)
        {
            if (target == null || !target.TryGetComponent(out WardStatus ward)) return;

            ward.Raise(seconds, cast.Scale(reflectFraction));
        }

        public override void CollectProblems(List<string> problems)
        {
            if (problems == null) return;

            if (seconds <= 0f)
                problems.Add("ergue um escudo que dura zero segundos");
        }
    }
}
