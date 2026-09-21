using System.Collections.Generic;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Uma conjuracao no instante do efeito: quem, qual sinal, de onde, para onde e com que forca.
    /// Struct para nao alocar, pela regra 5 do CLAUDE.md, como o <see cref="DamageRequest"/>.
    /// </summary>
    public readonly struct SignCast
    {
        /// <summary>Quem conjurou. Nunca e alvo do proprio sinal.</summary>
        public readonly CharacterVitals Caster;

        public readonly AbilityDef Ability;

        /// <summary>Os pes de quem conjura, na copia do host.</summary>
        public readonly Vector3 Origin;

        /// <summary>Para onde quem conjura encara. Achatado no plano na hora da consulta.</summary>
        public readonly Vector3 Forward;

        /// <summary>
        /// A forca do sinal, com 1,0 como o numero do asset. Vem do
        /// <see cref="CombatTuningDef.SignIntensity"/>, e ninguem calcula de novo.
        /// </summary>
        public readonly float Intensity;

        public SignCast(CharacterVitals caster, AbilityDef ability, Vector3 origin, Vector3 forward, float intensity)
        {
            Caster = caster;
            Ability = ability;
            Origin = origin;
            Forward = forward;
            Intensity = intensity;
        }

        /// <summary>Um numero de potencia do asset, com a intensidade aplicada.</summary>
        public float Scale(float potency) => potency * Intensity;
    }

    /// <summary>
    /// Um efeito de sinal, em asset (tarefa 1.18b). O abridor, o fogo, o escudo e a armadilha do
    /// docs/03 secao 8 sao subclasses deste tipo, cada uma com os proprios numeros no Inspector.
    ///
    /// Subclasse de asset, e nao lista polimorfica serializada dentro da habilidade, por dois
    /// motivos. O primeiro e o Inspector: um campo de asset aceita arrastar, e a lista polimorfica
    /// precisa de editor proprio para escolher o tipo. O segundo e a variante de escola: o mesmo
    /// efeito pode estar na habilidade e na variante de outra escola sem ser copiado.
    ///
    /// <b>Autoridade: so o host aplica.</b> Quem chama <see cref="Apply"/> e o
    /// <see cref="SignResolver"/>, e quem chama o resolvedor escuta um evento que so dispara em
    /// quem resolve. O que o efeito muda chega nas outras maquinas pelo estado replicado do alvo,
    /// como controle e vida, e nenhum efeito manda mensagem propria.
    ///
    /// O efeito decide o que a intensidade escala, e a regra e uma so: potencia escala, forma nao.
    /// Duracao, dano e forca passam por <see cref="SignCast.Scale"/>; area, custo e recarga moram
    /// na habilidade e nao escalam nunca.
    /// </summary>
    public abstract class SignEffectDef : ScriptableObject
    {
        /// <summary>
        /// Aplica o efeito numa criatura dentro da area. So o host chama, uma vez por alvo por
        /// conjuracao. O alvo nunca e quem conjurou e nunca esta abatido.
        /// </summary>
        public abstract void Apply(in SignCast cast, CharacterVitals target);

        /// <summary>O que impede este efeito de funcionar. Nada escrito quer dizer que ele esta pronto.</summary>
        public virtual void CollectProblems(List<string> problems) { }

        /// <summary>
        /// Os problemas de uma lista de efeitos, com o nome de cada um na frente. A habilidade e a
        /// variante de escola usam a mesma conferencia.
        /// </summary>
        public static void CollectListProblems(SignEffectDef[] effects, string label, List<string> problems)
        {
            if (effects == null || problems == null) return;

            for (int i = 0; i < effects.Length; i++)
            {
                SignEffectDef effect = effects[i];

                if (effect == null)
                {
                    problems.Add($"{label} {i} esta vazio");
                    continue;
                }

                int before = problems.Count;
                effect.CollectProblems(problems);

                for (int k = before; k < problems.Count; k++)
                    problems[k] = $"o efeito '{effect.name}' {problems[k]}";
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            var problems = new List<string>();
            CollectProblems(problems);

            for (int i = 0; i < problems.Count; i++)
                Debug.LogWarning($"{name}: {problems[i]}.", this);
        }
#endif
    }
}
