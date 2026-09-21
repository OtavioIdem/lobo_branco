using System.Collections.Generic;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Uma habilidade com custo e recarga, descrita inteiramente como dado (tarefa 1.32). E a
    /// infraestrutura dos sinais da tarefa 1.18 e das escolas do docs/13 secao 5: um sinal do
    /// Grifo e um do Lobo sao dois assets deste tipo, e nenhum <c>if</c> de escola.
    ///
    /// O asset diz quanto custa, quanto demora para voltar e quanto tempo o bruxo fica preso
    /// nele. Desde a tarefa 1.18b ele diz tambem onde alcanca e o que faz: a area e os efeitos,
    /// que o <see cref="SignResolver"/> aplica no host no instante do efeito. A variante de uma
    /// escola nao mora aqui, e sim na escola, porque o mesmo sinal e de todas elas.
    ///
    /// A linha do tempo tem duas fases, e nao as tres do golpe: conjurar ate o efeito, e
    /// recuperar depois dele. Um sinal nao abre uma janela de dano por um intervalo; ele
    /// acontece num instante, e esse instante e o que o host precisa saber.
    /// </summary>
    [CreateAssetMenu(menuName = "LoboBranco/Combat/Ability", fileName = "Ability_")]
    public sealed class AbilityDef : ScriptableObject
    {
        [Header("Identidade")]
        [Tooltip("Nome exibido. Vem de asset e nunca de codigo (tech/adr/0005).")]
        public string displayName = "Habilidade";

        [Header("Custo e recarga (docs/03 secao 8)")]
        [Tooltip("Vigor cobrado pelo host no inicio da conjuracao. Nao volta se a esquiva cortar.")]
        [Min(0f)] public float staminaCost = 30f;

        [Tooltip("Segundos ate poder usar de novo, contados do inicio da conjuracao.")]
        [Min(0f)] public float cooldownSeconds = 4f;

        [Header("Linha do tempo, em segundos")]
        [Tooltip("Do inicio ao efeito. O docs/03 nao da este numero: ver a nota da tarefa 1.32 no docs/12.")]
        [Min(0f)] public float castTime = 0.3f;

        [Tooltip("Do efeito ao fim. Durante ela nada cancela, exceto a esquiva (docs/03 secao 1).")]
        [Min(0f)] public float recovery = 0.4f;

        [Header("Efeito (tarefa 1.18)")]
        [Tooltip("Onde o sinal alcanca. A intensidade nunca muda a area.")]
        public SignArea area;

        [Tooltip("O que acontece com cada criatura dentro da area, na ordem. A variante da escola vem depois.")]
        public SignEffectDef[] effects;

        /// <summary>Conjuracao mais recuperacao. E quanto tempo o bruxo fica comprometido.</summary>
        public float TotalDuration => castTime + recovery;

        /// <summary>
        /// Escreve em <paramref name="problems"/> o que impede esta habilidade de ser usada. Nada
        /// escrito quer dizer que ela esta pronta.
        ///
        /// A duracao zero e problema por causa da rede, e nao de design: o compromisso do estado
        /// de sinal e o que impede o dono de pedir de novo antes de a recarga do host chegar
        /// replicada. Sem ele, todo segundo pedido vira uma recusa do host.
        /// </summary>
        public void CollectProblems(List<string> problems)
        {
            if (problems == null) return;

            if (TotalDuration <= 0f)
                problems.Add("dura zero segundos, e nada impede o dono de pedir de novo antes de o host responder");

            if (staminaCost <= 0f && cooldownSeconds <= 0f)
                problems.Add("nao custa vigor e nao tem recarga, e habilidade de graca vira clique");

            int before = problems.Count;
            area.CollectProblems(problems);

            for (int k = before; k < problems.Count; k++)
                problems[k] = $"a area {problems[k]}";

            if (effects != null && effects.Length > 0 && !area.HasArea)
                problems.Add("tem efeito e nao tem area, e o efeito nao alcanca ninguem");

            SignEffectDef.CollectListProblems(effects, "o efeito", problems);
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
