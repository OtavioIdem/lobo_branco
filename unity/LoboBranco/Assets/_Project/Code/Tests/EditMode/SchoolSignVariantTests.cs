using System.Collections.Generic;
using LoboBranco.Combat;
using LoboBranco.Player;
using LoboBranco.Stats;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// O gancho de variante da escola (tarefa 1.18b, docs/13 secao 5): cada escola e especializada
    /// num sinal e soma efeitos so dela a ele.
    ///
    /// O erro que importa e o silencioso. Uma especializada que nao esta em vaga nenhuma, ou uma
    /// variante sem especializada, existe no Inspector e nunca sai em jogo.
    /// </summary>
    public sealed class SchoolSignVariantTests
    {
        SchoolDef _escola;
        AbilityDef _abridor;
        AbilityDef _fogo;
        readonly List<Object> _criados = new List<Object>();
        readonly List<string> _problemas = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _abridor = Criar<AbilityDef>("Sinal_Abridor");
            _fogo = Criar<AbilityDef>("Sinal_Fogo");

            _escola = Criar<SchoolDef>("Escola");
            _escola.statBlock = Criar<StatBlockDef>("Atributos");
            _escola.favoredStance = Stance.Fast;
            _escola.strongAttack = CriarAtaque(Stance.Strong);
            _escola.fastAttack = CriarAtaque(Stance.Fast);
            _escola.groupAttack = CriarAtaque(Stance.Group);
            _escola.abilities = new[] { _abridor, _fogo };

            _problemas.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _criados.Count; i++)
                Object.DestroyImmediate(_criados[i]);

            _criados.Clear();
        }

        T Criar<T>(string nome) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = nome;
            _criados.Add(asset);

            return asset;
        }

        AttackDef CriarAtaque(Stance stance)
        {
            var attack = Criar<AttackDef>($"Ataque_{stance}");
            attack.stance = stance;

            return attack;
        }

        EfeitoDeSinalMudo CriarEfeito(string problema = null)
        {
            var efeito = Criar<EfeitoDeSinalMudo>("Variante");
            efeito.Problema = problema;

            return efeito;
        }

        [Test]
        public void Variante_so_sai_para_o_sinal_especializado()
        {
            var variante = new SignEffectDef[] { CriarEfeito() };
            _escola.specializedAbility = _fogo;
            _escola.variantEffects = variante;

            Assert.AreSame(variante, _escola.VariantEffectsFor(_fogo));
            Assert.IsNull(_escola.VariantEffectsFor(_abridor),
                "O abridor desta escola e o de todo mundo.");
            Assert.IsNull(_escola.VariantEffectsFor(null));
        }

        [Test]
        public void Escola_sem_especializacao_nao_tem_variante_e_esta_pronta()
        {
            _escola.CollectProblems(_problemas);

            Assert.IsEmpty(_problemas, string.Join("; ", _problemas));
            Assert.IsNull(_escola.VariantEffectsFor(_abridor));
        }

        [Test]
        public void Especializada_sem_efeito_de_variante_esta_pronta()
        {
            _escola.specializedAbility = _abridor;

            _escola.CollectProblems(_problemas);

            Assert.IsEmpty(_problemas,
                "E o Lobo hoje: especializado no abridor, com a variante ainda por desenhar.");
        }

        [Test]
        public void Especializada_fora_das_vagas_e_problema()
        {
            _escola.abilities = new[] { _abridor };
            _escola.specializedAbility = _fogo;

            _escola.CollectProblems(_problemas);

            Assert.AreEqual(1, _problemas.Count);
            StringAssert.Contains("nao esta em nenhuma vaga", _problemas[0]);
        }

        [Test]
        public void Variante_sem_especializada_e_problema()
        {
            _escola.variantEffects = new SignEffectDef[] { CriarEfeito() };

            _escola.CollectProblems(_problemas);

            Assert.AreEqual(1, _problemas.Count);
            StringAssert.Contains("nunca sai", _problemas[0]);
        }

        [Test]
        public void Problema_do_efeito_de_variante_aparece_na_escola()
        {
            _escola.specializedAbility = _fogo;
            _escola.variantEffects = new SignEffectDef[] { CriarEfeito("queima por zero segundos"), null };

            _escola.CollectProblems(_problemas);

            Assert.AreEqual(2, _problemas.Count, string.Join("; ", _problemas));
            StringAssert.Contains("queima por zero segundos", _problemas[0]);
            StringAssert.Contains("efeito de variante 1 esta vazio", _problemas[1]);
        }
    }
}
