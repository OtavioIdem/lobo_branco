using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Um golpe corpo a corpo, descrito inteiramente como dado: quanto tempo dura cada
    /// fase, quando a janela de dano abre, que forma ela tem e quantos alvos aceita.
    /// Os tempos e os custos sao os do docs/03 secao 4.
    ///
    /// Por que linha do tempo em dado e nao evento de animacao: o jogador ainda e uma
    /// capsula greybox sem Animator (animacoes de Mixamo entram no M4, tarefa 4.5), entao
    /// nao existe animacao onde pendurar um evento. A janela e dirigida por tempo
    /// decorrido, e <see cref="hitboxFromAnimationEvent"/> desliga isso quando a animacao
    /// existir: ai quem abre e fecha e o evento, como manda o docs/07 secao 4.5.
    /// Ver tech/adr/0007.
    ///
    /// A janela de dano e gravada como fracao de <see cref="strikeTime"/>, e nao em
    /// segundos, de proposito: fracao e exatamente o "normalized time" de um clipe de
    /// animacao, entao o dia da migracao e uma troca de fonte do numero, nao um
    /// reajuste de todos os assets.
    /// </summary>
    [CreateAssetMenu(menuName = "LoboBranco/Combat/Attack", fileName = "Attack_")]
    public sealed class AttackDef : ScriptableObject
    {
        [Header("Postura (docs/03 secao 4)")]
        [Tooltip("Alimenta o estagio 2 do pipeline de dano. Forte 1,45 / Rapida 0,75 / Grupo 0,90.")]
        public Stance stance = Stance.Fast;

        [Header("Linha do tempo, em segundos")]
        [Tooltip("Tempo do golpe: anticipacao mais contato. Coluna 'Tempo do golpe' do docs/03 secao 4.")]
        [Min(0.01f)] public float strikeTime = 0.25f;

        [Tooltip("Recuperacao. Durante ela nada cancela o golpe, exceto a esquiva (docs/03 secao 2).")]
        [Min(0f)] public float recovery = 0.15f;

        [Header("Janela de dano, como fracao do tempo do golpe")]
        [Tooltip("0 abre no inicio, 1 abre no fim. O que sobra antes disso e a anticipacao.")]
        [Range(0f, 1f)] public float hitboxOpenAt = 0.55f;

        [Range(0f, 1f)] public float hitboxCloseAt = 0.95f;

        [Tooltip("Ligar quando houver Animator: os eventos do clipe passam a abrir e fechar, " +
                 "e os dois campos acima viram apenas documentacao.")]
        public bool hitboxFromAnimationEvent;

        [Header("Custo e alvos (docs/03 secoes 4 e 7)")]
        [Tooltip("Vigor gasto. Ainda nao e cobrado: o recurso entra na tarefa 1.16.")]
        [Min(0f)] public float staminaCost = 4f;

        [Tooltip("Teto de alvos do golpe inteiro, nao por consulta. Grupo aceita 4.")]
        [Min(1)] public int maxTargets = 1;

        [Tooltip("Abertura total do arco a frente do personagem. Grupo usa 180.")]
        [Range(10f, 360f)] public float arcDegrees = 110f;

        [Header("Geometria da janela de dano, em metros")]
        [Tooltip("Alcance a partir do centro do personagem.")]
        [Min(0.1f)] public float reach = 2.2f;

        [Tooltip("Raio da capsula de consulta. Largo demais acerta o que passou longe.")]
        [Min(0.05f)] public float radius = 0.55f;

        [Tooltip("Altura da janela acima da base do personagem. 1,1 fica na altura do torso.")]
        public float heightOffset = 1.1f;

        /// <summary>Golpe mais recuperacao. E quanto tempo o jogador fica comprometido.</summary>
        public float TotalDuration => strikeTime + recovery;

        /// <summary>Instante, em segundos desde o inicio do golpe, em que a janela abre.</summary>
        public float HitboxOpenTime => strikeTime * hitboxOpenAt;

        /// <summary>Instante em que a janela fecha. Nunca antes de abrir.</summary>
        public float HitboxCloseTime => strikeTime * Mathf.Max(hitboxCloseAt, hitboxOpenAt);

#if UNITY_EDITOR
        void OnValidate()
        {
            // Fechar antes de abrir nao quebra nada (HitboxCloseTime corrige), mas produz
            // uma janela de duracao zero, e o sintoma e "meu ataque nao acerta nada".
            if (hitboxCloseAt < hitboxOpenAt)
                Debug.LogWarning($"{name}: a janela de dano fecha antes de abrir.", this);
        }
#endif
    }
}
