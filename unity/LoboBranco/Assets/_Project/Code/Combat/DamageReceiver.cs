using System;
using LoboBranco.Stats;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Autoridade: o host, pelo <see cref="CharacterVitals"/> ao lado (ADR 0008). Quem
    /// recusa um golpe resolvido no lugar errado e este componente, e recusar e melhor do
    /// que tirar vida que o host nunca vai confirmar.
    ///
    /// E a ponte entre o pipeline de dano e um personagem: pega o <c>IDamageable</c> que
    /// o golpe precisa e o responde com a folha de atributos da vida e a classificacao do
    /// perfil de combate. Nada mais.
    ///
    /// Ele existe como componente proprio porque o bruxo e o monstro recebem golpe pela
    /// mesma porta. Ate a tarefa 1.21 esta ponte morava dentro do <see cref="CombatDummy"/>,
    /// que e greybox de sandbox, e o jogador simplesmente nao era atingivel: o inimigo da
    /// tarefa 1.21 podia perseguir e desferir o golpe, e o golpe passava atraves do bruxo
    /// sem tirar nada. Copiar a ponte para o lado do jogador criaria duas implementacoes
    /// do mesmo contrato, e a segunda esqueceria de recusar dano fora do host.
    ///
    /// O perfil e um <see cref="MonsterDef"/> tambem no jogador, e isso e proposital
    /// apesar do nome: o que o pipeline pergunta a um alvo e classe, arquetipo, oleo que
    /// casa e resistencia, e essas quatro perguntas valem igual para um barghest e para um
    /// bruxo. Sem perfil o alvo vira besta agil sem fraqueza, e isso esta errado para o
    /// jogador: com aco valendo 0,35x contra nao-humanoide, um bandido de espada mal
    /// arranharia o bruxo. Por isso a ausencia de perfil e um aviso e nao um silencio.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterVitals))]
    public sealed class DamageReceiver : MonoBehaviour, IDamageable
    {
        [Header("Perfil de combate")]
        [Tooltip("Classe, arquetipo, oleo que casa e resistencias. Vale para monstro e para bruxo.")]
        [SerializeField] MonsterDef profile;

        CharacterVitals _vitals;

        // ---------------------------------------------------------------- leitura

        public StatSheet Stats => _vitals != null ? _vitals.Stats : null;

        public CreatureClass CreatureClass => profile != null ? profile.creatureClass : Combat.CreatureClass.Beast;

        public StanceArchetype Archetype => profile != null ? profile.archetype : StanceArchetype.Agile;

        public OilClass VulnerableToOil => profile != null ? profile.vulnerableToOil : OilClass.None;

        public float CurrentVitality => _vitals != null ? _vitals.CurrentVitality : 0f;

        public float MaxVitality => _vitals != null ? _vitals.MaxVitality : 0f;

        public bool IsDown => _vitals != null && _vitals.IsDown;

        /// <summary>
        /// Dispara a cada golpe recebido, so em quem resolveu o golpe. Quem quiser reagir
        /// em todas as maquinas escuta <see cref="CharacterVitals.VitalityChanged"/>.
        /// </summary>
        public event Action<DamageReceiver, DamageResult> Damaged;

        // ------------------------------------------------------------------ ciclo

        void Awake()
        {
            _vitals = GetComponent<CharacterVitals>();

            if (profile == null)
                Debug.LogWarning($"{name}: {nameof(DamageReceiver)} sem perfil de combate. Vale besta agil sem fraqueza.", this);
        }

        // ---------------------------------------------------------------- IDamageable

        /// <summary>
        /// Multiplicador do estagio 11. Os valores em si ainda sao neutros: quais
        /// criaturas resistem a que e decisao de balanceamento, e ela e a tarefa 1.30.
        /// </summary>
        public float GetResistance(DamageType type) => profile != null ? profile.GetResistance(type) : 1f;

        public void ApplyDamage(in DamageResult result)
        {
            // Chegar aqui sem autoridade significa que alguem rodou o pipeline no lugar
            // errado. Recusar e melhor do que tirar vida que o host nunca vai confirmar.
            if (_vitals == null || !_vitals.CanResolve)
            {
                Debug.LogError($"{name}: dano resolvido fora do host. Ver ADR 0008.", this);
                return;
            }

            if (IsDown) return;

            _vitals.ApplyDamage(result.Amount);

            Damaged?.Invoke(this, result);
        }
    }
}
