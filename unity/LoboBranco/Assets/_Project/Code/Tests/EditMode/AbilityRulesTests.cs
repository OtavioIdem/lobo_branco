using System.Collections.Generic;
using LoboBranco.Combat;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// A pergunta "esta habilidade pode sair agora?" (tarefa 1.32), e o que torna uma habilidade
    /// usavel.
    ///
    /// A regra e a mesma funcao no dono e no host (tech/adr/0011). Se ela mudar de um jeito que
    /// ninguem confere, o sintoma e em rede e intermitente: um sinal que sai na tela de quem
    /// conjura e e recusado pelo host.
    /// </summary>
    public sealed class AbilityRulesTests
    {
        AbilityDef _sinal;
        readonly List<string> _problemas = new List<string>();

        // Os numeros do abridor do docs/03 secao 8.
        const float Custo = 30f;
        const float Recarga = 4f;

        [SetUp]
        public void SetUp()
        {
            _sinal = ScriptableObject.CreateInstance<AbilityDef>();
            _sinal.staminaCost = Custo;
            _sinal.cooldownSeconds = Recarga;
            _sinal.castTime = 0.3f;
            _sinal.recovery = 0.4f;

            _problemas.Clear();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_sinal);

        // ------------------------------------------------------------- regra

        [Test]
        public void Vaga_vazia_e_recusada_antes_de_tudo()
        {
            Assert.AreEqual(AbilityRefusal.NoAbility, AbilityRules.Check(null, 5f, new VigorFalso(0f)));
        }

        /// <summary>"Ainda nao voltou" se resolve esperando. Dizer "falta vigor" antes faria o jogador economizar a toa.</summary>
        [Test]
        public void Recarga_e_recusada_antes_do_vigor()
        {
            Assert.AreEqual(AbilityRefusal.OnCooldown, AbilityRules.Check(_sinal, 2f, new VigorFalso(0f)));
        }

        [Test]
        public void Sem_vigor_e_recusada()
        {
            Assert.AreEqual(AbilityRefusal.NoStamina, AbilityRules.Check(_sinal, 0f, new VigorFalso(Custo - 1f)));
        }

        [Test]
        public void Vigor_exato_basta()
        {
            Assert.AreEqual(AbilityRefusal.None, AbilityRules.Check(_sinal, 0f, new VigorFalso(Custo)));
        }

        [Test]
        public void Sem_fonte_de_vigor_o_vigor_nao_cobra()
        {
            Assert.AreEqual(AbilityRefusal.None, AbilityRules.Check(_sinal, 0f, null));
        }

        /// <summary>Sem folga, e o que o dono usa: qualquer resto de recarga segura o pedido.</summary>
        [Test]
        public void Sem_folga_qualquer_resto_de_recarga_recusa()
        {
            Assert.AreEqual(AbilityRefusal.OnCooldown, AbilityRules.Check(_sinal, 0.001f, null));
        }

        /// <summary>
        /// A folga e do host, para o pedido que saiu quando o relogio estimado do dono disse
        /// que a recarga tinha voltado. Ela aceita o quase pronto e so ele.
        /// </summary>
        [Test]
        public void Folga_do_host_aceita_so_a_recarga_quase_pronta()
        {
            Assert.AreEqual(AbilityRefusal.None, AbilityRules.Check(_sinal, 0.05f, null, 0.1f));
            Assert.AreEqual(AbilityRefusal.OnCooldown, AbilityRules.Check(_sinal, 0.15f, null, 0.1f));
        }

        [Test]
        public void Folga_negativa_nao_vira_rigor_extra()
        {
            Assert.AreEqual(AbilityRefusal.None, AbilityRules.Check(_sinal, 0f, null, -1f));
        }

        // ------------------------------------------------------------ asset

        [Test]
        public void Habilidade_com_os_numeros_do_documento_esta_pronta()
        {
            _sinal.CollectProblems(_problemas);

            CollectionAssert.IsEmpty(_problemas);
        }

        /// <summary>O compromisso e o que impede o dono de pedir de novo antes da resposta do host.</summary>
        [Test]
        public void Habilidade_de_duracao_zero_e_problema()
        {
            _sinal.castTime = 0f;
            _sinal.recovery = 0f;

            _sinal.CollectProblems(_problemas);

            Assert.AreEqual(1, _problemas.Count);
            StringAssert.Contains("zero segundos", _problemas[0]);
        }

        [Test]
        public void Habilidade_sem_custo_e_sem_recarga_e_problema()
        {
            _sinal.staminaCost = 0f;
            _sinal.cooldownSeconds = 0f;

            _sinal.CollectProblems(_problemas);

            Assert.AreEqual(1, _problemas.Count);
            StringAssert.Contains("recarga", _problemas[0]);
        }

        /// <summary>Uma habilidade que so espera, sem cobrar vigor, e uma escolha de design valida.</summary>
        [Test]
        public void Habilidade_so_com_recarga_e_valida()
        {
            _sinal.staminaCost = 0f;

            _sinal.CollectProblems(_problemas);

            CollectionAssert.IsEmpty(_problemas);
        }

        // ------------------------------------------------------------ dublês

        sealed class VigorFalso : IStaminaSource
        {
            readonly float _disponivel;

            public VigorFalso(float disponivel) => _disponivel = disponivel;

            public float CurrentStamina => _disponivel;

            public float MaxStamina => 100f;

            public bool CanAfford(float cost) => cost <= _disponivel;
        }
    }
}
