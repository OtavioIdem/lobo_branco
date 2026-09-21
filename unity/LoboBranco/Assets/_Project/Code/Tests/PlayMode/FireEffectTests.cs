using System.Collections.Generic;
using System.Reflection;
using LoboBranco.Combat;
using LoboBranco.Stats;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// O fogo aplicado numa criatura de verdade (tarefa 1.18d): o dano do instante pelo pipeline, a
    /// Queimadura em tiques, e a carga de adrenalina de quem mata. Sem rede, o proprio objeto resolve.
    ///
    /// Os tiques sao dados pelo relogio que o teste passa ao <see cref="BurnStatus.Step"/>, e nenhum
    /// teste espera um segundo de verdade.
    /// </summary>
    public sealed class FireEffectTests
    {
        static readonly FieldInfo ProfileField =
            typeof(DamageReceiver).GetField("profile", BindingFlags.Instance | BindingFlags.NonPublic);

        DamageEffectDef _dano;
        BurnEffectDef _queima;
        AbilityDef _fogo;
        CharacterVitals _bruxo;
        readonly List<Object> _criados = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            var tuning = CombatTuningDef.CreateDefault();
            _criados.Add(tuning);

            _dano = ScriptableObject.CreateInstance<DamageEffectDef>();
            _dano.tuning = tuning;
            _queima = ScriptableObject.CreateInstance<BurnEffectDef>();
            _fogo = ScriptableObject.CreateInstance<AbilityDef>();
            _criados.Add(_dano);
            _criados.Add(_queima);
            _criados.Add(_fogo);

            // docs/03 secao 12: o bruxo de nivel 1 bate 12. O fogo sai 0,8 disso.
            var espada = ScriptableObject.CreateInstance<MeleeWeaponDef>();
            espada.baseDamage = 12f;
            espada.material = WeaponMaterial.Steel;
            _criados.Add(espada);

            var go = new GameObject("Bruxo");
            _criados.Add(go);
            _bruxo = go.AddComponent<CharacterVitals>();
            _bruxo.Stats.SetBase(StatType.MaxVitality, 100f);
            _bruxo.RestoreToFull();
            go.AddComponent<ConjuradorDeFogoFalso>().Espada = espada;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _criados.Count; i++)
                if (_criados[i] != null)
                    Object.DestroyImmediate(_criados[i]);

            _criados.Clear();
        }

        CharacterVitals CriarAlvo(float vida, float armadura = 0f, float resistenciaAFogo = 1f)
        {
            var go = new GameObject("Alvo");
            _criados.Add(go);

            var vitals = go.AddComponent<CharacterVitals>();
            vitals.Stats.SetBase(StatType.MaxVitality, vida);
            vitals.Stats.SetBase(StatType.Armor, armadura);
            vitals.RestoreToFull();

            var perfil = ScriptableObject.CreateInstance<MonsterDef>();
            perfil.resistances = new[] { new MonsterDef.Resistance { type = DamageType.Fire, multiplier = resistenciaAFogo } };
            _criados.Add(perfil);
            ProfileField.SetValue(go.AddComponent<DamageReceiver>(), perfil);

            go.AddComponent<BurnStatus>();
            return vitals;
        }

        SignCast Conjuracao(float intensidade = 1f)
            => new SignCast(_bruxo, _fogo, Vector3.zero, Vector3.forward, intensidade);

        // --------------------------------------------------------------- instante

        [Test]
        public void Fogo_tira_oito_decimos_da_espada_menos_a_armadura()
        {
            CharacterVitals alvo = CriarAlvo(55f, armadura: 2f);

            _dano.Apply(Conjuracao(), alvo);

            Assert.AreEqual(55f - (12f * 0.8f - 2f), alvo.CurrentVitality, 0.001f,
                "docs/03 secao 8: 0,8x de 12, menos os 2 de armadura do barghest.");
        }

        [Test]
        public void Aco_na_mao_nao_enfraquece_o_fogo_contra_monstro()
        {
            CharacterVitals alvo = CriarAlvo(55f);

            _dano.Apply(Conjuracao(), alvo);

            Assert.AreEqual(55f - 9.6f, alvo.CurrentVitality, 0.001f,
                "Com o material na conta, o fogo sairia 0,35x contra o barghest.");
        }

        [Test]
        public void Intensidade_multiplica_o_dano_do_instante()
        {
            CharacterVitals alvo = CriarAlvo(55f);

            _dano.Apply(Conjuracao(intensidade: 1.5f), alvo);

            Assert.AreEqual(55f - 14.4f, alvo.CurrentVitality, 0.001f);
        }

        [Test]
        public void Fogo_que_mata_rende_uma_carga_a_quem_conjurou()
        {
            CharacterVitals alvo = CriarAlvo(5f);

            _dano.Apply(Conjuracao(), alvo);

            Assert.IsTrue(alvo.IsDown);
            Assert.AreEqual(1, _bruxo.CurrentAdrenaline, "Morte causada rende uma carga (docs/03 secao 7).");
        }

        // -------------------------------------------------------------- queimadura

        [Test]
        public void Queimadura_tira_quatro_por_tique_ignorando_a_armadura()
        {
            CharacterVitals alvo = CriarAlvo(55f, armadura: 12f);
            var burn = alvo.GetComponent<BurnStatus>();

            _queima.Apply(Conjuracao(), alvo);
            double inicio = Time.timeAsDouble;

            burn.Step(inicio + 1.0);
            Assert.AreEqual(51f, alvo.CurrentVitality, 0.001f,
                "Contra 12 de armadura, subtrair deixaria 1 por tique: o fogo seria pior contra quem deveria abrir.");

            burn.Step(inicio + 10.0);
            Assert.AreEqual(35f, alvo.CurrentVitality, 0.001f, "Cinco tiques de 4: 20 no total.");
            Assert.IsFalse(burn.IsBurning);
        }

        [Test]
        public void Queimadura_passa_pela_resistencia_a_fogo()
        {
            CharacterVitals alvo = CriarAlvo(55f, resistenciaAFogo: 1.5f);
            var burn = alvo.GetComponent<BurnStatus>();

            _queima.Apply(Conjuracao(), alvo);
            burn.Step(Time.timeAsDouble + 1.0);

            Assert.AreEqual(49f, alvo.CurrentVitality, 0.001f);
        }

        [Test]
        public void Queimadura_que_mata_rende_carga_e_apaga()
        {
            CharacterVitals alvo = CriarAlvo(6f);
            var burn = alvo.GetComponent<BurnStatus>();

            _queima.Apply(Conjuracao(), alvo);
            burn.Step(Time.timeAsDouble + 2.0);

            Assert.IsTrue(alvo.IsDown,
                $"vida {alvo.CurrentVitality}, queimando {burn.IsBurning}, tique de {burn.DamagePerTick}");
            Assert.IsFalse(burn.IsBurning, "A morte leva a queimadura junto.");
            Assert.AreEqual(1, _bruxo.CurrentAdrenaline);
        }

        [Test]
        public void Criatura_abatida_nao_acende()
        {
            CharacterVitals alvo = CriarAlvo(5f);
            alvo.ApplyDamage(10f);

            _queima.Apply(Conjuracao(), alvo);

            Assert.IsFalse(alvo.GetComponent<BurnStatus>().IsBurning);
        }

        [Test]
        public void Criatura_sem_queimadura_ignora_o_efeito_sem_erro()
        {
            var go = new GameObject("Sem_Queimadura");
            _criados.Add(go);
            var vitals = go.AddComponent<CharacterVitals>();

            Assert.DoesNotThrow(() => _queima.Apply(Conjuracao(), vitals));
        }
    }
}
