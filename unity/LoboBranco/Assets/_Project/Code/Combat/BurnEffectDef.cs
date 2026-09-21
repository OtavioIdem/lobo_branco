using System.Collections.Generic;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Um efeito de sinal que acende a Queimadura (tarefa 1.18d): 4 por segundo por 5 s, do docs/03
    /// secao 8. Quem conta os tiques e o <see cref="BurnStatus"/> do alvo, no host.
    ///
    /// A intensidade escala o dano por segundo, e nao a duracao. Escalar os dois elevaria a
    /// intensidade ao quadrado, e a Queimadura sairia desproporcional a todos os outros efeitos.
    /// </summary>
    [CreateAssetMenu(menuName = "LoboBranco/Combat/Sign Effect/Burn", fileName = "SignEffect_Burn_")]
    public sealed class BurnEffectDef : SignEffectDef
    {
        [Tooltip("Dano por segundo com a intensidade de referencia. docs/03 secao 8: 4.")]
        [Min(0f)] public float damagePerSecond = 4f;

        [Tooltip("Quanto tempo queima. docs/03 secao 8: 5 s.")]
        [Min(0f)] public float seconds = 5f;

        [Tooltip("De quanto em quanto tempo o dano sai. Nao e balanceamento: o total nao muda.")]
        [Min(0.05f)] public float tickSeconds = 1f;

        /// <summary>Uma criatura sem <see cref="BurnStatus"/> nao queima, e o efeito a ignora sem erro.</summary>
        public override void Apply(in SignCast cast, CharacterVitals target)
        {
            if (target == null || !target.TryGetComponent(out BurnStatus burn)) return;

            burn.Ignite(cast.Scale(damagePerSecond), seconds, tickSeconds, cast.Caster);
        }

        public override void CollectProblems(List<string> problems)
        {
            if (problems == null) return;

            if (damagePerSecond <= 0f || seconds <= 0f)
                problems.Add("queima com dano ou duracao zero");
            else if (tickSeconds > seconds)
                problems.Add("tem um tique mais longo que a queimadura, e nunca fere");
        }
    }
}
