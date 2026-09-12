using LoboBranco.Combat;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// A especie em asset (docs/07 secao 4.1).
    ///
    /// O teste que importa e o do tipo nao listado: resistencia e uma tabela esparsa, e
    /// esquecer o neutro faria toda criatura ser imune a tudo que ninguem listou. O
    /// sintoma disso e "meu Igni nao tira vida", e ele nao aponta para aqui.
    /// </summary>
    public sealed class MonsterDefTests
    {
        MonsterDef _barghest;

        [SetUp]
        public void SetUp()
        {
            _barghest = ScriptableObject.CreateInstance<MonsterDef>();
            _barghest.creatureClass = CreatureClass.Beast;
            _barghest.archetype = StanceArchetype.Agile;
            _barghest.vulnerableToOil = OilClass.Beast;
        }

        [TearDown]
        public void TearDown()
        {
            if (_barghest != null) Object.DestroyImmediate(_barghest);
        }

        [Test]
        public void Tipo_nao_listado_e_neutro()
        {
            Assert.AreEqual(1f, _barghest.GetResistance(DamageType.Slash), 0.001f);
            Assert.AreEqual(1f, _barghest.GetResistance(DamageType.Fire), 0.001f);
        }

        [Test]
        public void Tipo_listado_devolve_o_que_o_asset_diz()
        {
            _barghest.resistances = new[]
            {
                new MonsterDef.Resistance { type = DamageType.Fire, multiplier = 1.5f },
                new MonsterDef.Resistance { type = DamageType.Frost, multiplier = 0.5f },
            };

            Assert.AreEqual(1.5f, _barghest.GetResistance(DamageType.Fire), 0.001f, "Fraqueza.");
            Assert.AreEqual(0.5f, _barghest.GetResistance(DamageType.Frost), 0.001f, "Resistencia.");
            Assert.AreEqual(1f, _barghest.GetResistance(DamageType.Slash), 0.001f, "O resto segue neutro.");
        }

        /// <summary>
        /// A especie e o que o estagio 3 e o 4 do pipeline consultam. Se o arquetipo ou a
        /// classe sumirem do asset, a postura certa e a espada certa param de significar
        /// alguma coisa, e o combate fica uniforme sem nenhum erro aparecer.
        /// </summary>
        [Test]
        public void A_especie_diz_qual_postura_e_qual_oleo_casam()
        {
            Assert.AreEqual(Stance.Fast, _barghest.archetype.PreferredStance());
            Assert.AreEqual(OilClass.Beast, _barghest.vulnerableToOil);
            Assert.IsFalse(_barghest.creatureClass.IsHumanoid(), "Besta nao e humanoide: prata ganha do aco.");
        }
    }
}
