using System.Collections.Generic;
using LoboBranco.Combat;
using LoboBranco.Stats;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// A vida de um personagem e o caminho sem rede dela.
    ///
    /// Em PlayMode porque <c>Awake</c> so roda com o jogo rodando, e e nele que a folha
    /// de atributos nasce. O que esta aqui e o lado solo: o outro lado, o replicado, so
    /// existe com dois processos conversando e por isso e verificado na sandbox, com o
    /// painel F2 aberto, e nao em teste automatizado (doc 13 secao 11).
    ///
    /// O que estes testes protegem e a regra da ADR 0008: sem rede, o proprio objeto tem
    /// autoridade. Se isso quebrar, a Sandbox_Combate para de ser jogavel sozinha e o
    /// combate passa a exigir um host para ser testado, que e o risco X8 do doc 13.
    /// </summary>
    public sealed class CharacterVitalsTests
    {
        readonly List<GameObject> _criados = new List<GameObject>();

        const float VidaMaxima = 100f;

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _criados.Count; i++)
                if (_criados[i] != null)
                    Object.Destroy(_criados[i]);

            _criados.Clear();
        }

        /// <summary>
        /// Sem asset de atributos, a vida maxima seria zero e todo teste comecaria com o
        /// alvo abatido. Escrever a base direto na folha evita depender de um asset em
        /// disco para verificar uma regra que e de codigo.
        /// </summary>
        CharacterVitals CriarVitals(string nome = "Alvo")
        {
            var go = new GameObject(nome);
            _criados.Add(go);

            var vitals = go.AddComponent<CharacterVitals>();
            vitals.Stats.SetBase(StatType.MaxVitality, VidaMaxima);
            vitals.RestoreToFull();

            return vitals;
        }

        [Test]
        public void Sem_rede_o_proprio_objeto_resolve_o_proprio_dano()
        {
            CharacterVitals vitals = CriarVitals();

            Assert.IsTrue(vitals.CanResolve, "Sem NetworkManager, quem manda na vida e o proprio objeto.");
        }

        [Test]
        public void Dano_tira_vida_e_avisa_quem_escuta()
        {
            CharacterVitals vitals = CriarVitals();

            float anterior = -1f;
            float atual = -1f;
            vitals.VitalityChanged += (before, after) =>
            {
                anterior = before;
                atual = after;
            };

            vitals.ApplyDamage(30f);

            Assert.AreEqual(70f, vitals.CurrentVitality, 0.001f);
            Assert.AreEqual(VidaMaxima, anterior, 0.001f);
            Assert.AreEqual(70f, atual, 0.001f);
        }

        [Test]
        public void Vida_nao_passa_de_zero_e_o_excedente_nao_conta()
        {
            CharacterVitals vitals = CriarVitals();

            float tirado = vitals.ApplyDamage(VidaMaxima * 10f);

            Assert.AreEqual(0f, vitals.CurrentVitality, 0.001f);
            Assert.IsTrue(vitals.IsDown);
            Assert.AreEqual(VidaMaxima, tirado, 0.001f, "O golpe que mata tira so o que restava.");
        }

        [Test]
        public void Quem_ja_caiu_nao_leva_mais_dano()
        {
            CharacterVitals vitals = CriarVitals();
            vitals.ApplyDamage(VidaMaxima);

            float tirado = vitals.ApplyDamage(10f);

            Assert.AreEqual(0f, tirado, 0.001f);
        }

        [Test]
        public void Restaurar_devolve_a_vida_cheia()
        {
            CharacterVitals vitals = CriarVitals();
            vitals.ApplyDamage(VidaMaxima);

            vitals.RestoreToFull();

            Assert.AreEqual(VidaMaxima, vitals.CurrentVitality, 0.001f);
            Assert.IsFalse(vitals.IsDown);
        }

        /// <summary>
        /// O alvo de sandbox nao guarda mais vida propria. Se ele voltar a guardar, o
        /// host e o cliente passam a contar vidas diferentes para a mesma capsula.
        /// </summary>
        [Test]
        public void Alvo_de_sandbox_tira_vida_do_componente_replicado()
        {
            var go = new GameObject("Capsula");
            _criados.Add(go);

            var dummy = go.AddComponent<CombatDummy>();
            CharacterVitals vitals = go.GetComponent<CharacterVitals>();

            Assert.IsNotNull(vitals, "CombatDummy exige CharacterVitals ao lado.");

            vitals.Stats.SetBase(StatType.MaxVitality, VidaMaxima);
            vitals.RestoreToFull();

            DamageResult recebido = default;
            dummy.Damaged += (_, result) => recebido = result;

            dummy.ApplyDamage(new DamageResult(25f, DamageType.Slash, wasCritical: false, totalMultiplier: 1f));

            Assert.AreEqual(75f, vitals.CurrentVitality, 0.001f);
            Assert.AreEqual(75f, dummy.CurrentVitality, 0.001f);
            Assert.AreEqual(25f, recebido.Amount, 0.001f);
        }
    }
}
