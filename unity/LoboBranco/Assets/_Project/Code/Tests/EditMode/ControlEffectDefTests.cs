using System.Collections.Generic;
using LoboBranco.Combat;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// O controle do abridor como dado (tarefa 1.18c): uma resposta por porte, com os numeros do
    /// docs/03 secao 8 nos padroes do asset.
    /// </summary>
    public sealed class ControlEffectDefTests
    {
        ControlEffectDef _efeito;
        readonly List<string> _problemas = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _efeito = ScriptableObject.CreateInstance<ControlEffectDef>();
            _problemas.Clear();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_efeito);

        [Test]
        public void Padroes_derrubam_leves_atordoam_medios_e_nao_movem_pesados()
        {
            Assert.AreEqual(ControlKind.KnockedDown, _efeito.light.control, "docs/03 secao 8: derruba leves.");
            Assert.AreEqual(ControlKind.Stunned, _efeito.medium.control, "docs/03 secao 8: atordoa medios.");
            Assert.AreEqual(1.5f, _efeito.medium.seconds, "docs/03 secao 8: por 1,5 s.");
            Assert.IsFalse(_efeito.heavy.DoesSomething, "docs/03 secao 11: pesados nem se movem.");
        }

        [Test]
        public void Cada_porte_devolve_a_propria_resposta()
        {
            Assert.AreEqual(_efeito.light.control, _efeito.ResponseFor(BodyWeight.Light).control);
            Assert.AreEqual(_efeito.medium.control, _efeito.ResponseFor(BodyWeight.Medium).control);
            Assert.AreEqual(_efeito.heavy.control, _efeito.ResponseFor(BodyWeight.Heavy).control);
        }

        [Test]
        public void Padroes_nao_tem_problema()
        {
            _efeito.CollectProblems(_problemas);

            Assert.IsEmpty(_problemas, string.Join("; ", _problemas));
        }

        [Test]
        public void Controle_por_zero_segundos_e_problema()
        {
            _efeito.light.seconds = 0f;

            _efeito.CollectProblems(_problemas);

            Assert.AreEqual(1, _problemas.Count);
            StringAssert.Contains("derruba o porte leve por zero segundos", _problemas[0]);
        }

        [Test]
        public void Efeito_que_nao_controla_porte_nenhum_e_problema()
        {
            _efeito.light = default;
            _efeito.medium = default;

            _efeito.CollectProblems(_problemas);

            Assert.AreEqual(1, _problemas.Count);
            StringAssert.Contains("nao controla porte nenhum", _problemas[0]);
        }
    }
}
