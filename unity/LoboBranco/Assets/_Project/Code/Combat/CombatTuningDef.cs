using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Todos os multiplicadores do pipeline de dano, em um asset. Esta e a superficie de
    /// balanceamento do combate inteiro (docs/03 secoes 3, 4, 6 e 9).
    ///
    /// Desde a tarefa 1.16 ele carrega tambem os tempos do Vigor, que nao sao
    /// multiplicadores mas sao numeros de combate, valem para bruxo e para monstro, e
    /// precisam ser alcancaveis do modulo `Combat`. Separar em um segundo asset criaria
    /// dois lugares para procurar o mesmo tipo de numero.
    ///
    /// Antes de mexer em qualquer valor daqui, leia a skill `balancear-combate`: os
    /// estagios sao multiplicativos, e a razao de 5,3 vezes entre jogador preparado e
    /// despreparado e o pilar 2 escrito em matematica. Ha teste que falha se ela mudar.
    /// </summary>
    [CreateAssetMenu(menuName = "LoboBranco/Combat/Tuning", fileName = "CombatTuning")]
    public sealed class CombatTuningDef : ScriptableObject
    {
        [Header("Estagio 2 — postura (docs/03 secao 4)")]
        public float strongMultiplier = 1.45f;
        public float fastMultiplier = 0.75f;
        public float groupMultiplier = 0.90f;

        [Header("Estagio 3 — afinidade de postura")]
        [Tooltip("Postura casa com o arquetipo do alvo.")]
        public float affinityMatch = 1.3f;

        [Tooltip("Postura errada. Nunca zere: o jogador precisa poder errar e sentir, nao bater em parede.")]
        public float affinityMismatch = 0.8f;

        [Header("Estagio 4 — material da lamina (docs/03 secao 3)")]
        public float steelVsHumanoid = 1.0f;
        public float steelVsMonster = 0.35f;
        public float silverVsHumanoid = 0.5f;
        public float silverVsMonster = 1.0f;

        [Header("Estagio 5 — Fluxo (docs/03 secao 6)")]
        [Tooltip("Janela no fim de cada golpe para encadear o proximo. docs/03 secao 6.")]
        public float flowWindowSeconds = 0.22f;

        [Tooltip("Indexado pelo numero de elos da corrente. Acima do tamanho do vetor, usa o ultimo.")]
        public float[] flowBonusByChain = { 1.00f, 1.00f, 1.10f, 1.20f, 1.30f, 1.35f };

        [Tooltip("A partir de quantos elos o vigor fica mais barato. docs/03 secao 6: tres.")]
        [Min(1)] public int flowStaminaDiscountChain = 3;

        [Tooltip("Desconto no custo de vigor a partir dali. 0,20 e os 20 por cento do documento.")]
        [Range(0f, 1f)] public float flowStaminaDiscount = 0.20f;

        [Header("Vigor (docs/03 secao 7)")]
        [Tooltip("Silencio de regeneracao depois de cada gasto. E o que faz gastar ser escolha.")]
        [Min(0f)] public float staminaRegenDelay = 1.5f;

        [Tooltip("Quanto tempo depois de gastar ou apanhar ainda se conta como em combate. " +
                 "Heuristica: quem vai saber isso de verdade e o coordenador de encontro da tarefa 1.22.")]
        [Min(0f)] public float combatMemorySeconds = 5f;

        [Header("Adrenalina (docs/03 secao 7)")]
        [Tooltip("Cargas maximas. Tres, e elas nao regeneram sozinhas.")]
        [Min(0)] public int adrenalineMaxCharges = 3;

        [Tooltip("A partir de quantos elos de Fluxo nasce a primeira carga. docs/03 secao 6: cinco.")]
        [Min(1)] public int adrenalineFlowLinksForFirstCharge = 5;

        [Tooltip("Dali para frente, uma carga a cada tantos elos.")]
        [Min(1)] public int adrenalineFlowLinksPerCharge = 2;

        [Tooltip("Custo do segundo suspiro, em cargas.")]
        [Min(0)] public int secondWindCost = 2;

        [Tooltip("Quanto do vigor maximo o segundo suspiro devolve. 0,40 e os 40 por cento do documento.")]
        [Range(0f, 1f)] public float secondWindStaminaFraction = 0.40f;

        [Header("Estagio 6 — oleo de lamina")]
        public float oilMatchMultiplier = 1.5f;

        [Header("Estagio 7 — conhecimento do bestiario (docs/02 secao 7)")]
        [Tooltip("Concedido quando a secao Vulnerabilidades esta destravada. Pesquisar e uma mecanica de dano.")]
        public float bestiaryKnowledgeMultiplier = 1.25f;

        [Header("Estagio 9 — critico")]
        public float criticalMultiplier = 2.0f;

        [Header("Estagio 10 — armadura")]
        [Tooltip("Piso de dano depois da subtracao de armadura. Sem piso, um alvo muito blindado vira imune.")]
        public float minimumDamage = 1.0f;

        public float StanceMultiplier(Stance stance)
        {
            switch (stance)
            {
                case Stance.Strong: return strongMultiplier;
                case Stance.Fast: return fastMultiplier;
                case Stance.Group: return groupMultiplier;
                default: return 1f;
            }
        }

        public float AffinityMultiplier(Stance stance, StanceArchetype archetype)
            => stance == archetype.PreferredStance() ? affinityMatch : affinityMismatch;

        public float MaterialMultiplier(WeaponMaterial material, CreatureClass target)
        {
            bool humanoid = target.IsHumanoid();

            if (material == WeaponMaterial.Steel)
                return humanoid ? steelVsHumanoid : steelVsMonster;

            return humanoid ? silverVsHumanoid : silverVsMonster;
        }

        /// <summary>
        /// Custo de vigor do golpe, ja com o desconto da corrente de Fluxo
        /// (docs/03 secao 6). Um lugar so, porque o dono consulta para saber se pode pedir
        /// e o host consulta para cobrar: contas diferentes nos dois lados dariam um golpe
        /// que sai na tela de quem bate e nao sai na de quem apanha.
        /// </summary>
        public float StaminaCost(float baseCost, int flowChain)
            => flowChain >= flowStaminaDiscountChain ? baseCost * (1f - flowStaminaDiscount) : baseCost;

        public float FlowMultiplier(int chain)
        {
            if (flowBonusByChain == null || flowBonusByChain.Length == 0) return 1f;
            if (chain < 0) chain = 0;

            return chain >= flowBonusByChain.Length
                ? flowBonusByChain[flowBonusByChain.Length - 1]
                : flowBonusByChain[chain];
        }

        /// <summary>
        /// Instancia com os valores padrao, sem precisar de asset em disco.
        /// Existe para teste; o jogo usa o asset.
        /// </summary>
        public static CombatTuningDef CreateDefault() => CreateInstance<CombatTuningDef>();
    }
}
