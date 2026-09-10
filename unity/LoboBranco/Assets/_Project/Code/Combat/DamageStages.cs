using LoboBranco.Stats;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Um passo do calculo de dano. Cada estagio faz uma coisa e e testavel sozinho.
    ///
    /// Onze objetos pequenos em vez de uma funcao de duzentas linhas: a ordem vira dado,
    /// da para inserir um estagio novo sem tocar nos outros, e cada um registra a propria
    /// contribuicao no log.
    /// </summary>
    public interface IDamageStage
    {
        void Apply(DamageContext ctx, CombatTuningDef tuning);
    }

    // =====================================================================
    // Os onze estagios, na ordem do docs/03 secao 9. A ordem importa: trocar
    // dois estagios muda o resultado, porque armadura e subtracao plana.
    // =====================================================================

    /// <summary>1. Dano cru da arma mais o bonus plano de atributos e talentos.</summary>
    public sealed class WeaponBaseStage : IDamageStage
    {
        public void Apply(DamageContext ctx, CombatTuningDef tuning)
        {
            float bonus = ctx.Attacker?.Stats?.Get(StatType.AttackDamage) ?? 0f;

            if (bonus != 0f)
                ctx.ApplyFlat(bonus, "bonus plano de ataque");
            else
                ctx.Note("dano base da arma");
        }
    }

    /// <summary>2. Postura: Forte bate mais e mais devagar, Rapida o contrario.</summary>
    public sealed class StanceMultiplierStage : IDamageStage
    {
        public void Apply(DamageContext ctx, CombatTuningDef tuning)
            => ctx.ApplyMultiplier(tuning.StanceMultiplier(ctx.Stance), $"postura {ctx.Stance}");
    }

    /// <summary>
    /// 3. Afinidade: a postura casa com o arquetipo do alvo, ou nao.
    /// E a decisao mais frequente do combate, e o que se herda do original.
    /// </summary>
    public sealed class StanceAffinityStage : IDamageStage
    {
        public void Apply(DamageContext ctx, CombatTuningDef tuning)
        {
            if (ctx.Target == null) return;

            StanceArchetype archetype = ctx.Target.Archetype;
            float multiplier = tuning.AffinityMultiplier(ctx.Stance, archetype);
            bool matched = ctx.Stance == archetype.PreferredStance();

            ctx.ApplyMultiplier(multiplier, matched
                ? $"afinidade certa contra {archetype}"
                : $"afinidade errada contra {archetype}, ideal seria {archetype.PreferredStance()}");
        }
    }

    /// <summary>4. Aco contra prata. A decisao binaria de maior impacto do combate.</summary>
    public sealed class WeaponMaterialStage : IDamageStage
    {
        public void Apply(DamageContext ctx, CombatTuningDef tuning)
        {
            if (ctx.Target == null) return;

            CreatureClass creatureClass = ctx.Target.CreatureClass;
            float multiplier = tuning.MaterialMultiplier(ctx.Material, creatureClass);

            ctx.ApplyMultiplier(multiplier, $"{ctx.Material} contra {creatureClass}");
        }
    }

    /// <summary>5. Fluxo: bonus por encadear golpes no ritmo. Opcional por design.</summary>
    public sealed class FlowChainStage : IDamageStage
    {
        public void Apply(DamageContext ctx, CombatTuningDef tuning)
        {
            float multiplier = tuning.FlowMultiplier(ctx.FlowChain);
            if (multiplier == 1f) return;

            ctx.ApplyMultiplier(multiplier, $"Fluxo {ctx.FlowChain} elos");
        }
    }

    /// <summary>6. Oleo de lamina, se a classe casar com a vulnerabilidade do alvo.</summary>
    public sealed class BladeOilStage : IDamageStage
    {
        public void Apply(DamageContext ctx, CombatTuningDef tuning)
        {
            if (ctx.Target == null || ctx.AppliedOil == OilClass.None) return;
            if (ctx.AppliedOil != ctx.Target.VulnerableToOil)
            {
                ctx.Note($"oleo {ctx.AppliedOil} nao serve contra este alvo");
                return;
            }

            ctx.ApplyMultiplier(tuning.oilMatchMultiplier, $"oleo {ctx.AppliedOil}");
        }
    }

    /// <summary>
    /// 7. Conhecimento do bestiario. Ler o bestiario e literalmente uma mecanica de dano,
    /// e e o que faz o pilar 1 existir mecanicamente e nao so na ficcao.
    /// </summary>
    public sealed class BestiaryKnowledgeStage : IDamageStage
    {
        public void Apply(DamageContext ctx, CombatTuningDef tuning)
        {
            if (ctx.Attacker == null || ctx.Target == null) return;
            if (!ctx.Attacker.HasBestiaryKnowledge(ctx.Target.CreatureClass)) return;

            ctx.ApplyMultiplier(tuning.bestiaryKnowledgeMultiplier, "bestiario pesquisado");
        }
    }

    /// <summary>8. Buffs de pocao e talento, agregados pela folha de atributos.</summary>
    public sealed class BuffStage : IDamageStage
    {
        public void Apply(DamageContext ctx, CombatTuningDef tuning)
        {
            StatSheet stats = ctx.Attacker?.Stats;
            if (stats == null) return;

            float multiplier = stats.Get(StatType.DamageMultiplier);
            if (multiplier == 1f || multiplier == 0f) return;

            ctx.ApplyMultiplier(multiplier, "buffs ativos");
        }
    }

    /// <summary>9. Critico.</summary>
    public sealed class CriticalHitStage : IDamageStage
    {
        public void Apply(DamageContext ctx, CombatTuningDef tuning)
        {
            if (!ctx.IsCritical) return;

            ctx.ApplyMultiplier(tuning.criticalMultiplier, "critico");
        }
    }

    /// <summary>
    /// 10. Armadura, por subtracao plana com piso.
    ///
    /// Plana e nao percentual de proposito: assim a armadura pune desproporcionalmente a
    /// postura Rapida, que bate fraco e rapido, e e isso que da funcao a postura Forte
    /// contra alvos blindados. "Consertar" isso removeria uma decisao do combate.
    /// </summary>
    public sealed class ArmorStage : IDamageStage
    {
        public void Apply(DamageContext ctx, CombatTuningDef tuning)
        {
            if (ctx.Target?.Stats == null) return;

            float armor = ctx.Target.Stats.Get(StatType.Armor);
            if (armor <= 0f) return;

            float before = ctx.Amount;
            float after = Mathf.Max(tuning.minimumDamage, before - armor);

            ctx.ApplyFlat(after - before, $"armadura {armor:0.#}");
        }
    }

    /// <summary>11. Resistencia do alvo ao tipo de dano.</summary>
    public sealed class ResistanceStage : IDamageStage
    {
        public void Apply(DamageContext ctx, CombatTuningDef tuning)
        {
            if (ctx.Target == null) return;

            float resistance = ctx.Target.GetResistance(ctx.Type);
            if (resistance == 1f) return;

            ctx.Amount = Mathf.Max(tuning.minimumDamage, ctx.Amount * resistance);

            if (ctx.LoggingEnabled)
                ctx.Note($"resistencia a {ctx.Type}: x{resistance:0.##}");
        }
    }
}
