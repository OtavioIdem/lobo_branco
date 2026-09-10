using System.Collections.Generic;
using LoboBranco.Combat;
using LoboBranco.Stats;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// Os dois cenarios de referencia do docs/03 secao 9 e a razao de 5,3 vezes entre eles.
    ///
    /// Essa razao e o pilar 2 escrito em matematica: quem faz o trabalho de bruxo vence,
    /// quem improvisa apanha. Se este arquivo comecar a falhar, ou o balanceamento mudou
    /// de proposito e os numeros aqui precisam ser atualizados com justificativa, ou
    /// alguem quebrou o motor do jogo sem perceber.
    /// </summary>
    public sealed class DamagePipelineTests
    {
        CombatTuningDef _tuning;
        DamagePipeline _pipeline;
        FakeAttacker _attacker;

        // Base 1.0 faz o dano final ser o proprio multiplicador acumulado, o que torna
        // a asserção legivel: o numero do teste e o numero do documento.
        const float UnitWeaponDamage = 1f;

        [SetUp]
        public void SetUp()
        {
            _tuning = CombatTuningDef.CreateDefault();
            _pipeline = new DamagePipeline(_tuning);
            _attacker = new FakeAttacker();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        // =============================================================
        // Os dois cenarios canonicos
        // =============================================================

        [Test]
        public void Jogador_preparado_contra_besta_multiplica_por_2_19()
        {
            var barghest = new FakeTarget
            {
                CreatureClass = CreatureClass.Beast,
                Archetype = StanceArchetype.Agile,
                VulnerableToOil = OilClass.Beast,
            };

            _attacker.KnownBestiary.Add(CreatureClass.Beast);

            var request = new DamageRequest(
                _attacker, barghest, UnitWeaponDamage, DamageType.Slash,
                Stance.Fast,                 // 0,75
                WeaponMaterial.Silver,       // 1,00 contra monstro
                OilClass.Beast,              // 1,50
                flowChain: 3);               // 1,20  e afinidade certa da 1,30, bestiario 1,25

            DamageResult result = _pipeline.Resolve(request);

            Assert.AreEqual(2.19f, result.TotalMultiplier, 0.01f,
                "Preparado deveria bater 2,19 vezes o dano base (docs/03 secao 9).");
        }

        [Test]
        public void Jogador_despreparado_contra_besta_multiplica_por_0_41()
        {
            var barghest = new FakeTarget
            {
                CreatureClass = CreatureClass.Beast,
                Archetype = StanceArchetype.Agile,
                VulnerableToOil = OilClass.Beast,
            };

            var request = new DamageRequest(
                _attacker, barghest, UnitWeaponDamage, DamageType.Slash,
                Stance.Strong,               // 1,45, mas afinidade errada da 0,80
                WeaponMaterial.Steel);       // 0,35 contra monstro

            DamageResult result = _pipeline.Resolve(request);

            Assert.AreEqual(0.41f, result.TotalMultiplier, 0.01f,
                "Despreparado deveria bater 0,41 vezes o dano base (docs/03 secao 9).");
        }

        [Test]
        public void Preparo_vale_cinco_vezes_mais_que_improviso()
        {
            var barghest = new FakeTarget
            {
                CreatureClass = CreatureClass.Beast,
                Archetype = StanceArchetype.Agile,
                VulnerableToOil = OilClass.Beast,
            };

            var unprepared = new DamageRequest(
                _attacker, barghest, UnitWeaponDamage, DamageType.Slash,
                Stance.Strong, WeaponMaterial.Steel);
            float low = _pipeline.Resolve(unprepared).TotalMultiplier;

            _attacker.KnownBestiary.Add(CreatureClass.Beast);
            var prepared = new DamageRequest(
                _attacker, barghest, UnitWeaponDamage, DamageType.Slash,
                Stance.Fast, WeaponMaterial.Silver, OilClass.Beast, flowChain: 3);
            float high = _pipeline.Resolve(prepared).TotalMultiplier;

            float ratio = high / low;

            // Este e o numero mais importante do jogo. Ver a skill balancear-combate.
            Assert.AreEqual(5.3f, ratio, 0.15f,
                $"A razao preparado/despreparado saiu {ratio:0.00}. Ela e o pilar 2 " +
                "expresso em matematica; se ela cair, preparacao virou decoracao.");
        }

        // =============================================================
        // Estagios isolados
        // =============================================================

        [Test]
        public void O_pipeline_tem_onze_estagios()
        {
            Assert.AreEqual(11, _pipeline.StageCount);
        }

        [Test]
        public void Aco_contra_monstro_e_punido_e_prata_contra_humano_tambem()
        {
            var monster = new FakeTarget { CreatureClass = CreatureClass.Beast, Archetype = StanceArchetype.Agile };
            var human = new FakeTarget { CreatureClass = CreatureClass.Humanoid, Archetype = StanceArchetype.Agile };

            float steelOnMonster = Multiplier(monster, Stance.Fast, WeaponMaterial.Steel);
            float silverOnMonster = Multiplier(monster, Stance.Fast, WeaponMaterial.Silver);
            float steelOnHuman = Multiplier(human, Stance.Fast, WeaponMaterial.Steel);
            float silverOnHuman = Multiplier(human, Stance.Fast, WeaponMaterial.Silver);

            Assert.Less(steelOnMonster, silverOnMonster, "Prata deveria vencer aco contra monstro.");
            Assert.Less(silverOnHuman, steelOnHuman, "Aco deveria vencer prata contra humano.");
        }

        [Test]
        public void Postura_certa_vale_mais_que_postura_errada()
        {
            var heavy = new FakeTarget { CreatureClass = CreatureClass.Necrophage, Archetype = StanceArchetype.Heavy };

            float right = Multiplier(heavy, Stance.Strong, WeaponMaterial.Silver);
            float wrong = Multiplier(heavy, Stance.Group, WeaponMaterial.Silver);

            Assert.Greater(right, wrong);
        }

        [Test]
        public void Oleo_errado_nao_da_bonus_nenhum()
        {
            var necrophage = new FakeTarget
            {
                CreatureClass = CreatureClass.Necrophage,
                Archetype = StanceArchetype.Heavy,
                VulnerableToOil = OilClass.Necrophage,
            };

            float rightOil = Multiplier(necrophage, Stance.Strong, WeaponMaterial.Silver, OilClass.Necrophage);
            float wrongOil = Multiplier(necrophage, Stance.Strong, WeaponMaterial.Silver, OilClass.Beast);
            float noOil = Multiplier(necrophage, Stance.Strong, WeaponMaterial.Silver);

            Assert.AreEqual(noOil, wrongOil, 0.001f, "Comprar o oleo errado nao deveria ajudar em nada.");
            Assert.Greater(rightOil, noOil);
        }

        [Test]
        public void Fluxo_sobe_o_dano_ate_o_teto_e_para()
        {
            var target = new FakeTarget { CreatureClass = CreatureClass.Beast, Archetype = StanceArchetype.Agile };

            float chain0 = Multiplier(target, Stance.Fast, WeaponMaterial.Silver, OilClass.None, 0);
            float chain3 = Multiplier(target, Stance.Fast, WeaponMaterial.Silver, OilClass.None, 3);
            float chain5 = Multiplier(target, Stance.Fast, WeaponMaterial.Silver, OilClass.None, 5);
            float chain50 = Multiplier(target, Stance.Fast, WeaponMaterial.Silver, OilClass.None, 50);

            Assert.Greater(chain3, chain0);
            Assert.Greater(chain5, chain3);
            Assert.AreEqual(chain5, chain50, 0.001f, "Acima do teto o Fluxo deveria parar de crescer.");
        }

        [Test]
        public void Ignorar_o_Fluxo_nao_pune_o_jogador()
        {
            var target = new FakeTarget { CreatureClass = CreatureClass.Beast, Archetype = StanceArchetype.Agile };

            // Corrente zerada tem que valer exatamente 1,0, nunca menos: o Fluxo e bonus
            // opcional, e transformar ele em requisito restauraria o defeito D1.
            float chain0 = Multiplier(target, Stance.Fast, WeaponMaterial.Silver, OilClass.None, 0);
            float chain1 = Multiplier(target, Stance.Fast, WeaponMaterial.Silver, OilClass.None, 1);

            Assert.AreEqual(chain0, chain1, 0.001f);
        }

        [Test]
        public void Pesquisar_o_bestiario_aumenta_o_dano()
        {
            var target = new FakeTarget { CreatureClass = CreatureClass.Specter, Archetype = StanceArchetype.Agile };

            float unknown = Multiplier(target, Stance.Fast, WeaponMaterial.Silver);
            _attacker.KnownBestiary.Add(CreatureClass.Specter);
            float known = Multiplier(target, Stance.Fast, WeaponMaterial.Silver);

            Assert.AreEqual(_tuning.bestiaryKnowledgeMultiplier, known / unknown, 0.001f,
                "Ler o bestiario e uma mecanica de dano, nao so informacao.");
        }

        [Test]
        public void Buff_de_pocao_entra_pela_folha_de_atributos()
        {
            var target = new FakeTarget { CreatureClass = CreatureClass.Beast, Archetype = StanceArchetype.Agile };
            var thunderbolt = new object();

            float before = Multiplier(target, Stance.Fast, WeaponMaterial.Silver);

            // A pocao Trovao: mais 30 por cento de dano (docs/05 secao 4).
            _attacker.Stats.AddModifier(
                StatModifier.PercentAdd(StatType.DamageMultiplier, 0.30f, thunderbolt));

            float during = Multiplier(target, Stance.Fast, WeaponMaterial.Silver);

            _attacker.Stats.RemoveAllFromSource(thunderbolt);
            float after = Multiplier(target, Stance.Fast, WeaponMaterial.Silver);

            Assert.AreEqual(1.30f, during / before, 0.001f);
            Assert.AreEqual(before, after, 0.001f, "O bonus deveria ter sumido junto com a pocao.");
        }

        // =============================================================
        // Armadura e resistencia
        // =============================================================

        [Test]
        public void Armadura_e_subtracao_plana_e_nao_percentual()
        {
            var armored = new FakeTarget { CreatureClass = CreatureClass.Humanoid, Archetype = StanceArchetype.Heavy };
            armored.Stats.SetBase(StatType.Armor, 10f);

            var request = new DamageRequest(
                _attacker, armored, weaponDamage: 50f, DamageType.Slash,
                Stance.Strong, WeaponMaterial.Steel);

            DamageResult result = _pipeline.Resolve(request);

            // 50 * 1,45 * 1,30 * 1,00 = 94,25, menos 10 de armadura = 84,25
            Assert.AreEqual(84.25f, result.Amount, 0.01f);
        }

        [Test]
        public void Alvo_muito_blindado_ainda_recebe_o_piso_de_dano()
        {
            var tank = new FakeTarget { CreatureClass = CreatureClass.Construct, Archetype = StanceArchetype.Heavy };
            tank.Stats.SetBase(StatType.Armor, 9999f);

            var request = new DamageRequest(
                _attacker, tank, weaponDamage: 12f, DamageType.Slash,
                Stance.Strong, WeaponMaterial.Silver);

            DamageResult result = _pipeline.Resolve(request);

            // Sem piso, armadura alta viraria imunidade e a luta ficaria impossivel
            // em vez de dificil.
            Assert.AreEqual(_tuning.minimumDamage, result.Amount, 0.001f);
        }

        [Test]
        public void Resistencia_e_fraqueza_ao_tipo_de_dano_valem()
        {
            var fireproof = new FakeTarget { CreatureClass = CreatureClass.Beast, Archetype = StanceArchetype.Agile };
            fireproof.Resistances[DamageType.Fire] = 0.25f;
            fireproof.Resistances[DamageType.Frost] = 2.0f;

            float slash = Amount(fireproof, DamageType.Slash);
            float fire = Amount(fireproof, DamageType.Fire);
            float frost = Amount(fireproof, DamageType.Frost);

            Assert.AreEqual(slash * 0.25f, fire, 0.01f);
            Assert.AreEqual(slash * 2.0f, frost, 0.01f);
        }

        // =============================================================
        // Diagnostico
        // =============================================================

        [Test]
        public void Com_log_ligado_da_para_ver_cada_multiplicador_aplicado()
        {
            var target = new FakeTarget { CreatureClass = CreatureClass.Beast, Archetype = StanceArchetype.Agile };
            _pipeline.LoggingEnabled = true;

            _pipeline.Resolve(new DamageRequest(
                _attacker, target, 12f, DamageType.Slash, Stance.Strong, WeaponMaterial.Steel));

            string log = _pipeline.LastContext.DescribeLog();

            // Sem isto, um erro de balanceamento e invisivel: o numero final parece
            // plausivel e ninguem descobre qual estagio errou.
            Assert.IsNotEmpty(_pipeline.LastContext.Log);
            StringAssert.Contains("postura", log);
            StringAssert.Contains("afinidade", log);
        }

        [Test]
        public void Com_log_desligado_nada_e_registrado()
        {
            var target = new FakeTarget { CreatureClass = CreatureClass.Beast, Archetype = StanceArchetype.Agile };
            _pipeline.LoggingEnabled = false;

            _pipeline.Resolve(new DamageRequest(
                _attacker, target, 12f, DamageType.Slash, Stance.Fast, WeaponMaterial.Silver));

            Assert.IsEmpty(_pipeline.LastContext.Log,
                "Formatar string aloca; em combate o log fica desligado (regra 5 do CLAUDE.md).");
        }

        [Test]
        public void Deal_entrega_o_dano_ao_alvo()
        {
            var target = new FakeTarget { CreatureClass = CreatureClass.Beast, Archetype = StanceArchetype.Agile };

            DamageResult result = _pipeline.Deal(new DamageRequest(
                _attacker, target, 20f, DamageType.Slash, Stance.Fast, WeaponMaterial.Silver));

            Assert.AreEqual(1, target.HitsTaken);
            Assert.AreEqual(result.Amount, target.TotalDamageTaken, 0.001f);
        }

        // =============================================================
        // Auxiliares
        // =============================================================

        float Multiplier(FakeTarget target, Stance stance, WeaponMaterial material,
                         OilClass oil = OilClass.None, int flowChain = 0)
            => _pipeline.Resolve(new DamageRequest(
                _attacker, target, UnitWeaponDamage, DamageType.Slash,
                stance, material, oil, flowChain)).TotalMultiplier;

        float Amount(FakeTarget target, DamageType type)
            => _pipeline.Resolve(new DamageRequest(
                _attacker, target, 20f, type, Stance.Fast, WeaponMaterial.Silver)).Amount;

        sealed class FakeAttacker : IDamageDealer
        {
            public StatSheet Stats { get; } = new StatSheet();
            public readonly HashSet<CreatureClass> KnownBestiary = new HashSet<CreatureClass>();

            public FakeAttacker()
            {
                // Base neutra: o estagio 8 multiplica por este valor.
                Stats.SetBase(StatType.DamageMultiplier, 1f);
            }

            public bool HasBestiaryKnowledge(CreatureClass creatureClass)
                => KnownBestiary.Contains(creatureClass);
        }

        sealed class FakeTarget : IDamageable
        {
            public StatSheet Stats { get; } = new StatSheet();
            public CreatureClass CreatureClass { get; set; } = CreatureClass.Beast;
            public StanceArchetype Archetype { get; set; } = StanceArchetype.Agile;
            public OilClass VulnerableToOil { get; set; } = OilClass.None;

            public readonly Dictionary<DamageType, float> Resistances = new Dictionary<DamageType, float>();

            public int HitsTaken { get; private set; }
            public float TotalDamageTaken { get; private set; }

            public float GetResistance(DamageType type)
                => Resistances.TryGetValue(type, out float value) ? value : 1f;

            public void ApplyDamage(in DamageResult result)
            {
                HitsTaken++;
                TotalDamageTaken += result.Amount;
            }
        }
    }
}
