using LoboBranco.Stats;

namespace LoboBranco.Combat
{
    /// <summary>Quem desfere o golpe. So o que o pipeline precisa saber.</summary>
    public interface IDamageDealer
    {
        StatSheet Stats { get; }

        /// <summary>
        /// Se a secao Vulnerabilidades do bestiario esta destravada para esta classe.
        /// O conhecimento e do atacante, nao do alvo: e por isso que mora aqui.
        /// </summary>
        bool HasBestiaryKnowledge(CreatureClass creatureClass);
    }

    /// <summary>Quem recebe o golpe.</summary>
    public interface IDamageable
    {
        StatSheet Stats { get; }
        CreatureClass CreatureClass { get; }
        StanceArchetype Archetype { get; }

        /// <summary>Classe de oleo que multiplica o dano contra este alvo. None se nenhuma.</summary>
        OilClass VulnerableToOil { get; }

        /// <summary>Multiplicador por tipo de dano. 1.0 e neutro, abaixo resiste, acima e fraqueza.</summary>
        float GetResistance(DamageType type);

        /// <summary>
        /// Ja caiu. Quem desfere o golpe precisa saber, porque morte causada da adrenalina
        /// (docs/03 secao 7) e porque golpe em quem ja esta no chao nao deveria contar.
        /// </summary>
        bool IsDown { get; }

        void ApplyDamage(in DamageResult result);
    }

    /// <summary>
    /// Tudo que descreve um golpe antes de ser resolvido. Struct para nao alocar: um
    /// combate com quatro inimigos resolve dezenas de golpes por segundo, e a regra 5 do
    /// CLAUDE.md e zero alocacao por frame em combate.
    /// </summary>
    public readonly struct DamageRequest
    {
        public readonly IDamageDealer Attacker;
        public readonly IDamageable Target;

        /// <summary>Dano cru da arma, antes de qualquer multiplicador.</summary>
        public readonly float WeaponDamage;

        public readonly DamageType Type;
        public readonly Stance Stance;
        public readonly WeaponMaterial Material;
        public readonly OilClass AppliedOil;

        /// <summary>Elos da corrente de Fluxo no momento do golpe (docs/03 secao 6).</summary>
        public readonly int FlowChain;

        public readonly bool IsCritical;

        public DamageRequest(
            IDamageDealer attacker,
            IDamageable target,
            float weaponDamage,
            DamageType type,
            Stance stance,
            WeaponMaterial material,
            OilClass appliedOil = OilClass.None,
            int flowChain = 0,
            bool isCritical = false)
        {
            Attacker = attacker;
            Target = target;
            WeaponDamage = weaponDamage;
            Type = type;
            Stance = stance;
            Material = material;
            AppliedOil = appliedOil;
            FlowChain = flowChain;
            IsCritical = isCritical;
        }
    }

    /// <summary>
    /// Quem consegue absorver um dano antes de ele virar vida perdida (tarefa 1.18e).
    ///
    /// Existe como contrato, e nao como referencia ao escudo, porque o <see cref="DamageReceiver"/>
    /// e a porta de todo dano do jogo e nao deveria conhecer sinal nenhum. Pocao de pele de pedra e
    /// armadura que quebra, no M2, entram pela mesma porta.
    /// </summary>
    public interface IDamageAbsorber
    {
        /// <summary>
        /// Engoliu o dano? Verdadeiro quer dizer que o alvo nao perde vida nenhuma desta vez. So
        /// quem resolve o dano pergunta, entao a resposta vale no host.
        /// </summary>
        bool TryAbsorb(in DamageResult result);
    }

    /// <summary>Resultado resolvido de um golpe.</summary>
    public readonly struct DamageResult
    {
        public readonly float Amount;
        public readonly DamageType Type;
        public readonly bool WasCritical;

        /// <summary>
        /// Quem bateu, ou nulo quando o dano nao tem autor alcancavel. O escudo da tarefa 1.18e
        /// precisa dele para devolver os 30%, e sem isto o alvo sabe que apanhou e nao de quem.
        /// </summary>
        public readonly IDamageDealer Attacker;

        /// <summary>
        /// Produto de todos os estagios multiplicativos, antes de armadura e resistencia.
        /// Existe para diagnostico e para os testes de referencia do docs/03 secao 9.
        /// </summary>
        public readonly float TotalMultiplier;

        public DamageResult(
            float amount, DamageType type, bool wasCritical, float totalMultiplier, IDamageDealer attacker = null)
        {
            Amount = amount;
            Type = type;
            WasCritical = wasCritical;
            TotalMultiplier = totalMultiplier;
            Attacker = attacker;
        }
    }
}
