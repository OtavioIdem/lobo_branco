using LoboBranco.Combat;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// Os numeros de sensacao do docs/03 secao 11, lidos do asset.
    ///
    /// Os dois primeiros testes repetem os numeros do documento de proposito: se alguem
    /// afinar o asset sem atualizar o documento, o teste avisa que os dois discordam.
    /// </summary>
    public sealed class HitFeedbackDefTests
    {
        HitFeedbackDef _sensacao;

        [SetUp]
        public void SetUp() => _sensacao = ScriptableObject.CreateInstance<HitFeedbackDef>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_sensacao);

        [Test]
        public void Hitstop_forte_e_leve_sao_os_do_documento()
        {
            Assert.AreEqual(0.08f, _sensacao.HitstopFor(Stance.Strong), 0.0001f);
            Assert.AreEqual(0.04f, _sensacao.HitstopFor(Stance.Fast), 0.0001f);
        }

        /// <summary>
        /// Grupo acerta varios alvos, e congelar como o Forte com quatro alvos parece
        /// travamento. O documento nao da o numero, entao o teste so garante a ordem.
        /// </summary>
        [Test]
        public void Hitstop_de_grupo_fica_entre_o_leve_e_o_forte()
        {
            float grupo = _sensacao.HitstopFor(Stance.Group);

            Assert.Greater(grupo, _sensacao.HitstopFor(Stance.Fast));
            Assert.Less(grupo, _sensacao.HitstopFor(Stance.Strong));
        }

        [Test]
        public void O_tremor_cresce_com_o_dano()
        {
            Assert.Less(_sensacao.ShakeFor(5f), _sensacao.ShakeFor(15f));
        }

        /// <summary>Sem teto, um golpe enorme sacode a tela ate ninguem ver o telegrafo seguinte.</summary>
        [Test]
        public void O_tremor_tem_teto()
        {
            Assert.AreEqual(_sensacao.shakeMax, _sensacao.ShakeFor(10000f), 0.0001f);
        }

        [Test]
        public void Dano_nenhum_nao_treme()
        {
            Assert.AreEqual(0f, _sensacao.ShakeFor(0f));
            Assert.AreEqual(0f, _sensacao.ShakeFor(-3f));
        }
    }
}
