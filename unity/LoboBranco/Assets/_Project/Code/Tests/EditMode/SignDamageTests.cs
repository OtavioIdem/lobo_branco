using System.Collections.Generic;
using LoboBranco.Combat;
using LoboBranco.Stats;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// O dano de um sinal pelo pipeline (tarefa 1.18d): so os estagios que valem para qualquer dano.
    ///
    /// O que este arquivo protege e a separacao. Um sinal que passasse pela postura e pelo material
    /// sairia 0,35 vezes contra monstro com aco na mao, e o fogo seria inutil justamente contra o que
    /// ele deveria queimar. E o contrario: os onze estagios do golpe continuam intocados, e a razao
    /// de 5,3 vezes do DamagePipelineTests nao sabe que sinal existe.
    /// </summary>
    public sealed class SignDamageTests
    {
        CombatTuningDef _tuning;
        DamagePipeline _pipeline;
        readonly List<Object> _criados = new List<Object>();
        readonly List<string> _problemas = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _tuning = CombatTuningDef.CreateDefault();
            _pipeline = new DamagePipeline(_tuning, DamagePipeline.CreateSignStages());
            _criados.Add(_tuning);
            _problemas.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _criados.Count; i++)
                Object.DestroyImmediate(_criados[i]);

            _criados.Clear();
        }

        T Criar<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _criados.Add(asset);
            return asset;
        }

        static DamageRequest Fogo(IDamageDealer quem, IDamageable alvo, float dano)
            => new DamageRequest(quem, alvo, dano, DamageType.Fire, Stance.Strong, WeaponMaterial.Steel,
                OilClass.Beast, flowChain: 5, isCritical: true);

        // ------------------------------------------------------------------ estagios

        [Test]
        public void Sinal_ignora_postura_material_fluxo_oleo_e_critico()
        {
            var barghest = new AlvoDeFogo { Class = CreatureClass.Beast, Oil = OilClass.Beast };

            // O pedido traz tudo que o golpe traria, de proposito: postura Forte, aco em monstro,
            // oleo que casa, Fluxo 5 e critico. Nada disso pode mudar o dano de um sinal.
            DamageResult result = _pipeline.Resolve(Fogo(new QuemConjura(), barghest, 9.6f));

            Assert.AreEqual(9.6f, result.Amount, 0.001f);
            Assert.AreEqual(1f, result.TotalMultiplier, 0.001f);
        }

        [Test]
        public void Sinal_passa_pela_armadura_e_pela_resistencia_a_fogo()
        {
            var alvo = new AlvoDeFogo { FireResistance = 1.5f };
            alvo.Stats.SetBase(StatType.Armor, 2f);

            DamageResult result = _pipeline.Resolve(Fogo(new QuemConjura(), alvo, 9.6f));

            Assert.AreEqual((9.6f - 2f) * 1.5f, result.Amount, 0.001f,
                "A fraqueza a fogo e onde o sinal ganha sentido (docs/03 secao 8).");
        }

        [Test]
        public void Sinal_aceita_bestiario_e_pocao()
        {
            var quem = new QuemConjura();
            quem.Known.Add(CreatureClass.Beast);
            quem.Stats.SetBase(StatType.DamageMultiplier, 1.3f);

            DamageResult result = _pipeline.Resolve(Fogo(quem, new AlvoDeFogo { Class = CreatureClass.Beast }, 10f));

            Assert.AreEqual(10f * 1.25f * 1.3f, result.Amount, 0.001f,
                "Pesquisar e mecanica de dano para qualquer dano, e nao so para a espada.");
        }

        [Test]
        public void Estagios_de_sinal_sao_um_subconjunto_na_ordem_canonica()
        {
            IDamageStage[] golpe = DamagePipeline.CreateDefaultStages();
            IDamageStage[] sinal = DamagePipeline.CreateSignStages();

            int cursor = -1;
            foreach (IDamageStage estagio in sinal)
            {
                int posicao = System.Array.FindIndex(golpe, s => s.GetType() == estagio.GetType());

                Assert.Greater(posicao, cursor,
                    $"{estagio.GetType().Name} fora da ordem: armadura e subtracao, e a ordem muda o resultado.");
                cursor = posicao;
            }
        }

        // ------------------------------------------------------------- dado dos efeitos

        [Test]
        public void Padroes_do_fogo_sao_os_do_documento()
        {
            var dano = Criar<DamageEffectDef>();
            var queima = Criar<BurnEffectDef>();

            Assert.AreEqual(0.8f, dano.weaponDamageMultiplier, "docs/03 secao 8: dano 0,8x.");
            Assert.AreEqual(DamageType.Fire, dano.damageType);
            Assert.AreEqual(4f, queima.damagePerSecond, "docs/03 secao 8: 4 por segundo.");
            Assert.AreEqual(5f, queima.seconds, "docs/03 secao 8: por 5 s.");
        }

        [Test]
        public void Dano_sem_multiplicadores_do_combate_e_problema()
        {
            var dano = Criar<DamageEffectDef>();

            dano.CollectProblems(_problemas);

            Assert.AreEqual(1, _problemas.Count);
            StringAssert.Contains("nao fere", _problemas[0]);
        }

        [Test]
        public void Dano_com_multiplicadores_esta_pronto()
        {
            var dano = Criar<DamageEffectDef>();
            dano.tuning = _tuning;

            dano.CollectProblems(_problemas);

            Assert.IsEmpty(_problemas);
        }

        [Test]
        public void Queimadura_com_tique_maior_que_a_duracao_e_problema()
        {
            var queima = Criar<BurnEffectDef>();
            queima.tickSeconds = 6f;

            queima.CollectProblems(_problemas);

            Assert.AreEqual(1, _problemas.Count);
            StringAssert.Contains("nunca fere", _problemas[0]);
        }

        [Test]
        public void Queimadura_padrao_esta_pronta()
        {
            Criar<BurnEffectDef>().CollectProblems(_problemas);

            Assert.IsEmpty(_problemas);
        }

        // --------------------------------------------------------------------- duble

        sealed class QuemConjura : IDamageDealer
        {
            public readonly HashSet<CreatureClass> Known = new HashSet<CreatureClass>();
            public StatSheet Stats { get; } = new StatSheet();
            public bool HasBestiaryKnowledge(CreatureClass creatureClass) => Known.Contains(creatureClass);
        }

        sealed class AlvoDeFogo : IDamageable
        {
            public CreatureClass Class = CreatureClass.Beast;
            public OilClass Oil = OilClass.None;
            public float FireResistance = 1f;

            public StatSheet Stats { get; } = new StatSheet();
            public CreatureClass CreatureClass => Class;
            public StanceArchetype Archetype => StanceArchetype.Heavy;
            public OilClass VulnerableToOil => Oil;
            public float GetResistance(DamageType type) => type == DamageType.Fire ? FireResistance : 1f;
            public bool IsDown => false;
            public void ApplyDamage(in DamageResult result) { }
        }
    }
}
