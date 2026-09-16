using System.Collections.Generic;
using LoboBranco.Combat;
using LoboBranco.Stats;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// O sinal que deixa a armadilha no chao (tarefa 1.18f): uma por conjuracao, onde o bruxo esta, e
    /// com o tempo de campo escalado pela intensidade.
    ///
    /// Sem rede: o objeto nasce local, que e o caminho da sandbox solo (risco X8 do doc 13).
    /// </summary>
    public sealed class TrapEffectTests
    {
        TrapEffectDef _efeito;
        AbilityDef _sinal;
        CharacterVitals _bruxo;
        SignTrap _molde;

        readonly List<Object> _criados = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            var molde = new GameObject("Molde_Armadilha");
            molde.SetActive(false);
            _criados.Add(molde);
            _molde = molde.AddComponent<SignTrap>();

            _efeito = ScriptableObject.CreateInstance<TrapEffectDef>();
            _efeito.trapPrefab = molde;
            _criados.Add(_efeito);

            _sinal = ScriptableObject.CreateInstance<AbilityDef>();
            _sinal.area = new SignArea { shape = SignAreaShape.Self };
            _sinal.effects = new SignEffectDef[] { _efeito };
            _criados.Add(_sinal);

            var bruxo = new GameObject("Bruxo");
            bruxo.transform.position = new Vector3(3f, 0f, 5f);
            _criados.Add(bruxo);
            _bruxo = bruxo.AddComponent<CharacterVitals>();
            _bruxo.Stats.SetBase(StatType.MaxVitality, 100f);
            _bruxo.RestoreToFull();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (SignTrap trap in Todas())
                if (trap != _molde && trap != null)
                    Object.DestroyImmediate(trap.gameObject);

            for (int i = 0; i < _criados.Count; i++)
                if (_criados[i] != null)
                    Object.DestroyImmediate(_criados[i]);

            _criados.Clear();
        }

        SignCast Conjuracao(float intensidade = 1f)
            => new SignCast(_bruxo, _sinal, _bruxo.transform.position, Vector3.forward, intensidade);

        /// <summary>
        /// A armadilha que nasceu, ou nula. O molde fica desligado de proposito: ligado, ele nunca
        /// foi armado, e o primeiro <c>Update</c> dele o destruiria. A copia nasce desligada junto, e
        /// por isso a busca precisa pedir os inativos.
        /// </summary>
        SignTrap Nascida()
        {
            foreach (SignTrap trap in Todas())
                if (trap != _molde)
                    return trap;

            return null;
        }

        static SignTrap[] Todas()
            => Object.FindObjectsByType<SignTrap>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        [Test]
        public void Armadilha_nasce_onde_o_bruxo_esta()
        {
            _efeito.Apply(Conjuracao(), _bruxo);

            SignTrap nascida = Nascida();
            Assert.IsNotNull(nascida);
            Assert.AreEqual(_bruxo.transform.position, nascida.transform.position);
            Assert.IsTrue(nascida.IsActive);
        }

        [Test]
        public void Armadilha_dura_os_doze_segundos_do_documento()
        {
            _efeito.Apply(Conjuracao(), _bruxo);

            Assert.AreEqual(12f, Nascida().Remaining, 0.1f, "docs/03 secao 8: 12 s.");
        }

        [Test]
        public void Intensidade_estica_o_tempo_de_campo()
        {
            _efeito.Apply(Conjuracao(intensidade: 2f), _bruxo);

            Assert.AreEqual(24f, Nascida().Remaining, 0.1f,
                "A intensidade escala o tempo, e nao os 60% de lentidao.");
        }

        [Test]
        public void Alvo_que_nao_e_quem_conjurou_nao_larga_armadilha()
        {
            var outro = new GameObject("Outro");
            _criados.Add(outro);
            var vitals = outro.AddComponent<CharacterVitals>();

            _efeito.Apply(Conjuracao(), vitals);

            Assert.IsNull(Nascida(), "Uma area de cone largaria uma armadilha por criatura atingida.");
        }

        [Test]
        public void Efeito_sem_prefab_nao_quebra()
        {
            _efeito.trapPrefab = null;

            Assert.DoesNotThrow(() => _efeito.Apply(Conjuracao(), _bruxo));
            Assert.IsNull(Nascida());
        }
    }
}
