using System.Collections.Generic;
using LoboBranco.Combat;
using LoboBranco.Player;
using LoboBranco.Stats;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// A escola como dado (docs/13 secao 5.1).
    ///
    /// O teste que mais importa aqui e o da vaga errada, e ele e sobre rede: o golpe viaja como
    /// postura e o host resolve o asset pela postura. Uma escola com o golpe Forte na vaga
    /// Rapida faria o dono e o host desferirem golpes diferentes, sem erro nenhum no Console.
    /// </summary>
    public sealed class SchoolDefTests
    {
        SchoolDef _escola;
        StatBlockDef _atributos;
        AttackDef _forte;
        AttackDef _rapido;
        AttackDef _grupo;

        readonly List<string> _problemas = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _atributos = ScriptableObject.CreateInstance<StatBlockDef>();
            _forte = CriarAtaque(Stance.Strong);
            _rapido = CriarAtaque(Stance.Fast);
            _grupo = CriarAtaque(Stance.Group);

            _escola = ScriptableObject.CreateInstance<SchoolDef>();
            _escola.statBlock = _atributos;
            _escola.favoredStance = Stance.Fast;
            _escola.strongAttack = _forte;
            _escola.fastAttack = _rapido;
            _escola.groupAttack = _grupo;

            _problemas.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_escola);
            Object.DestroyImmediate(_atributos);
            Object.DestroyImmediate(_forte);
            Object.DestroyImmediate(_rapido);
            Object.DestroyImmediate(_grupo);

            for (int i = 0; i < _habilidades.Count; i++)
                Object.DestroyImmediate(_habilidades[i]);

            _habilidades.Clear();
        }

        readonly List<AbilityDef> _habilidades = new List<AbilityDef>();

        AbilityDef CriarHabilidade(string nome)
        {
            var ability = ScriptableObject.CreateInstance<AbilityDef>();
            ability.name = nome;
            _habilidades.Add(ability);

            return ability;
        }

        static AttackDef CriarAtaque(Stance stance)
        {
            var attack = ScriptableObject.CreateInstance<AttackDef>();
            attack.stance = stance;
            attack.name = $"Ataque_{stance}";

            return attack;
        }

        [Test]
        public void Cada_postura_devolve_o_golpe_da_propria_vaga()
        {
            Assert.AreSame(_forte, _escola.AttackFor(Stance.Strong));
            Assert.AreSame(_rapido, _escola.AttackFor(Stance.Fast));
            Assert.AreSame(_grupo, _escola.AttackFor(Stance.Group));
        }

        [Test]
        public void Escola_completa_nao_tem_problema()
        {
            _escola.CollectProblems(_problemas);

            Assert.IsEmpty(_problemas, string.Join("; ", _problemas));
        }

        /// <summary>
        /// O dono manda "golpe Rapido" pela rede e o host resolve o asset da vaga Rapida. Se a
        /// vaga guarda um golpe que declara outra postura, os dois lados desferem golpes
        /// diferentes.
        /// </summary>
        [Test]
        public void Golpe_na_vaga_de_outra_postura_e_problema()
        {
            _escola.fastAttack = _forte;

            _escola.CollectProblems(_problemas);

            Assert.IsNotEmpty(_problemas);
            StringAssert.Contains("vaga Fast", string.Join("; ", _problemas));
        }

        [Test]
        public void Vaga_vazia_e_problema()
        {
            _escola.groupAttack = null;

            _escola.CollectProblems(_problemas);

            StringAssert.Contains("postura Group", string.Join("; ", _problemas));
        }

        /// <summary>A postura favorecida e a postura com que o bruxo entra na luta. Sem golpe nela, ele entra desarmado.</summary>
        [Test]
        public void Postura_favorecida_sem_golpe_e_problema()
        {
            _escola.favoredStance = Stance.Group;
            _escola.groupAttack = null;

            _escola.CollectProblems(_problemas);

            StringAssert.Contains("favorecida", string.Join("; ", _problemas));
        }

        /// <summary>Sem bloco de atributos, o bruxo nasce com vida zero e cai no primeiro golpe.</summary>
        [Test]
        public void Escola_sem_atributos_e_problema()
        {
            _escola.statBlock = null;

            _escola.CollectProblems(_problemas);

            StringAssert.Contains("atributos", string.Join("; ", _problemas));
        }

        [Test]
        public void Lista_nula_nao_quebra()
        {
            Assert.DoesNotThrow(() => _escola.CollectProblems(null));
        }

        // -------------------------------------------------------- habilidades

        [Test]
        public void Escola_sem_habilidade_nenhuma_e_valida()
        {
            _escola.abilities = new AbilityDef[0];

            _escola.CollectProblems(_problemas);

            Assert.IsEmpty(_problemas, string.Join("; ", _problemas));
            Assert.AreEqual(0, _escola.AbilityCount);
            Assert.IsNull(_escola.AbilityAt(0));
        }

        /// <summary>A vaga e o que viaja pela rede. O host resolve o asset pela vaga, entao cada vaga tem que dar no proprio asset.</summary>
        [Test]
        public void Cada_vaga_devolve_a_propria_habilidade()
        {
            AbilityDef primeira = CriarHabilidade("Primeira");
            AbilityDef segunda = CriarHabilidade("Segunda");
            _escola.abilities = new[] { primeira, segunda };

            Assert.AreSame(primeira, _escola.AbilityAt(0));
            Assert.AreSame(segunda, _escola.AbilityAt(1));
            Assert.IsNull(_escola.AbilityAt(2));
            Assert.IsNull(_escola.AbilityAt(-1));
        }

        [Test]
        public void Vaga_de_habilidade_vazia_e_problema()
        {
            _escola.abilities = new[] { CriarHabilidade("Primeira"), null };

            _escola.CollectProblems(_problemas);

            StringAssert.Contains("vaga de habilidade 1", string.Join("; ", _problemas));
        }

        /// <summary>Cada vaga tem a propria recarga. A mesma habilidade em duas sairia duas vezes pelo preco de uma espera.</summary>
        [Test]
        public void Mesma_habilidade_em_duas_vagas_e_problema()
        {
            AbilityDef sinal = CriarHabilidade("Sinal");
            _escola.abilities = new[] { sinal, sinal };

            _escola.CollectProblems(_problemas);

            StringAssert.Contains("duas recargas", string.Join("; ", _problemas));
        }

        /// <summary>A recarga replicada tem cinco vagas. A sexta nao existe em jogo, e a escola tem que dizer isso antes do playtest.</summary>
        [Test]
        public void Habilidade_alem_da_recarga_replicada_e_problema_e_nao_existe_em_jogo()
        {
            var lista = new AbilityDef[SchoolDef.MaxAbilities + 1];
            for (int i = 0; i < lista.Length; i++)
                lista[i] = CriarHabilidade($"Sinal_{i}");

            _escola.abilities = lista;
            _escola.CollectProblems(_problemas);

            StringAssert.Contains("recarga replicada", string.Join("; ", _problemas));
            Assert.AreEqual(SchoolDef.MaxAbilities, _escola.AbilityCount);
            Assert.IsNull(_escola.AbilityAt(SchoolDef.MaxAbilities));
        }

        [Test]
        public void Problema_da_habilidade_aparece_na_escola()
        {
            AbilityDef quebrada = CriarHabilidade("Quebrada");
            quebrada.castTime = 0f;
            quebrada.recovery = 0f;
            _escola.abilities = new[] { quebrada };

            _escola.CollectProblems(_problemas);

            StringAssert.Contains("'Quebrada' dura zero segundos", string.Join("; ", _problemas));
        }
    }
}
