using System.Collections;
using System.Collections.Generic;
using LoboBranco.Combat;
using LoboBranco.Stats;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LoboBranco.Tests
{
    /// <summary>
    /// O instante do efeito de um sinal (tarefa 1.18b): quem esta na area, em que ordem, e os
    /// efeitos da habilidade e da variante aplicados em cada um.
    ///
    /// Em PlayMode porque a consulta e de fisica de verdade, como a do golpe. Nenhum teste mede
    /// tempo: um sinal acontece num instante, e o instante e dado.
    ///
    /// Os defeitos daqui sao todos silenciosos. O sinal que acerta quem conjurou, o que pega o
    /// barghest de longe e deixa o colado, e o que empurra duas vezes a criatura de dois colisores
    /// nao lancam erro nenhum.
    /// </summary>
    public sealed class SignResolverTests
    {
        SignResolver _resolver;
        AbilityDef _sinal;
        EfeitoDeSinalGravador _efeito;
        CharacterVitals _bruxo;

        readonly List<string> _registro = new List<string>();
        readonly List<Object> _criados = new List<Object>();

        static readonly LayerMask TodasAsLayers = Physics.AllLayers;

        [SetUp]
        public void SetUp()
        {
            _resolver = new SignResolver();
            _registro.Clear();

            _sinal = ScriptableObject.CreateInstance<AbilityDef>();
            _sinal.area = new SignArea { shape = SignAreaShape.Cone, range = 6f, coneAngleDegrees = 90f };
            _criados.Add(_sinal);

            _efeito = CriarEfeito("base", 2f);
            _sinal.effects = new SignEffectDef[] { _efeito };

            // Quem conjura tem colisor e vida, e fica no centro da area: e o alvo mais perto que
            // existe, e o sinal nunca pode pega-lo.
            _bruxo = CriarCriatura("Bruxo", Vector3.zero);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _criados.Count; i++)
                if (_criados[i] != null)
                    Object.DestroyImmediate(_criados[i]);

            _criados.Clear();
        }

        EfeitoDeSinalGravador CriarEfeito(string rotulo, float potencia)
        {
            var efeito = ScriptableObject.CreateInstance<EfeitoDeSinalGravador>();
            efeito.Rotulo = rotulo;
            efeito.Potencia = potencia;
            efeito.Registro = _registro;
            _criados.Add(efeito);

            return efeito;
        }

        CharacterVitals CriarCriatura(string nome, Vector3 pes, bool viva = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = nome;
            go.transform.position = pes + Vector3.up;
            _criados.Add(go);

            var vitals = go.AddComponent<CharacterVitals>();

            // Sem vida maxima, a criatura nasce abatida e nenhum sinal a alcanca.
            if (viva)
            {
                vitals.Stats.SetBase(StatType.MaxVitality, 50f);
                vitals.RestoreToFull();
            }

            Physics.SyncTransforms();
            return vitals;
        }

        SignCast Conjuracao(float intensidade = 1f, Vector3? frente = null)
            => new SignCast(_bruxo, _sinal, Vector3.zero, frente ?? Vector3.forward, intensidade);

        int Resolver(float intensidade = 1f, SignEffectDef[] variante = null)
            => _resolver.Resolve(Conjuracao(intensidade), variante, TodasAsLayers);

        // ------------------------------------------------------------------- area

        [UnityTest]
        public IEnumerator Cone_alcanca_quem_esta_a_frente_e_ignora_quem_esta_atras()
        {
            CriarCriatura("Frente", new Vector3(0f, 0f, 3f));
            CriarCriatura("Atras", new Vector3(0f, 0f, -3f));
            yield return null;

            Assert.AreEqual(1, Resolver());
            CollectionAssert.AreEqual(new[] { "base:Frente:2" }, _registro);
        }

        [UnityTest]
        public IEnumerator Criatura_alem_do_alcance_fica_de_fora()
        {
            CriarCriatura("Longe", new Vector3(0f, 0f, 9f));
            yield return null;

            Assert.AreEqual(0, Resolver(), "O cone do abridor tem 6 m, e 9 m esta fora.");
            Assert.IsEmpty(_registro);
        }

        [UnityTest]
        public IEnumerator Raio_alcanca_pelas_costas()
        {
            _sinal.area = new SignArea { shape = SignAreaShape.Radius, range = 4f };
            CriarCriatura("Atras", new Vector3(0f, 0f, -3f));
            yield return null;

            Assert.AreEqual(1, Resolver());
        }

        [UnityTest]
        public IEnumerator Quem_conjura_nunca_e_alvo_do_proprio_sinal()
        {
            _sinal.area = new SignArea { shape = SignAreaShape.Radius, range = 4f };
            yield return null;

            Assert.AreEqual(0, Resolver(), "O bruxo esta no centro do raio e tem vida e colisor.");
        }

        [UnityTest]
        public IEnumerator Criatura_abatida_nao_e_alvo()
        {
            CriarCriatura("Abatida", new Vector3(0f, 0f, 3f), viva: false);
            yield return null;

            Assert.AreEqual(0, Resolver(), "Golpe em quem ja esta no chao nao conta, e sinal tambem nao.");
        }

        [UnityTest]
        public IEnumerator Habilidade_sem_area_nao_procura_ninguem()
        {
            _sinal.area = default;
            CriarCriatura("Frente", new Vector3(0f, 0f, 3f));
            yield return null;

            Assert.AreEqual(0, Resolver());
        }

        [UnityTest]
        public IEnumerator Cone_sem_frente_nao_procura_ninguem()
        {
            CriarCriatura("Frente", new Vector3(0f, 0f, 3f));
            yield return null;

            Assert.AreEqual(0, _resolver.Resolve(Conjuracao(frente: Vector3.zero), null, TodasAsLayers),
                "Um cone sem frente acertaria para qualquer lado.");
        }

        // ------------------------------------------------------------------ ordem

        [UnityTest]
        public IEnumerator Teto_de_alvos_fica_com_os_mais_perto()
        {
            _resolver = new SignResolver(maxColliders: 32, maxTargets: 2);

            // Criados do mais longe para o mais perto, para que a ordem de criacao nao ajude.
            CriarCriatura("Cinco", new Vector3(0f, 0f, 5f));
            CriarCriatura("Quatro", new Vector3(0f, 0f, 4f));
            CriarCriatura("Dois", new Vector3(0f, 0f, 2f));
            yield return null;

            Assert.AreEqual(2, Resolver());
            CollectionAssert.AreEqual(new[] { "base:Dois:2", "base:Quatro:2" }, _registro,
                "Com a matilha acima do teto, o barghest colado no bruxo nao pode ficar de fora.");
        }

        [UnityTest]
        public IEnumerator Criatura_com_dois_colisores_recebe_o_efeito_uma_vez()
        {
            var raiz = new GameObject("Dois_Colisores");
            raiz.transform.position = new Vector3(0f, 1f, 3f);
            _criados.Add(raiz);

            var vitals = raiz.AddComponent<CharacterVitals>();
            vitals.Stats.SetBase(StatType.MaxVitality, 50f);
            vitals.RestoreToFull();

            for (int i = 0; i < 2; i++)
            {
                var corpo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                corpo.transform.SetParent(raiz.transform, false);
                corpo.transform.localPosition = new Vector3(i * 0.4f, 0f, 0f);
            }

            Physics.SyncTransforms();
            yield return null;

            Assert.AreEqual(1, Resolver());
            Assert.AreEqual(1, _registro.Count, "Cabeca e corpo sao a mesma criatura.");
        }

        // ---------------------------------------------------------------- efeitos

        [UnityTest]
        public IEnumerator Efeitos_da_habilidade_saem_antes_da_variante_em_cada_alvo()
        {
            CriarCriatura("Perto", new Vector3(0f, 0f, 2f));
            CriarCriatura("Longe", new Vector3(0f, 0f, 4f));
            yield return null;

            var variante = new SignEffectDef[] { CriarEfeito("variante", 1f) };

            Assert.AreEqual(2, Resolver(variante: variante));
            CollectionAssert.AreEqual(
                new[] { "base:Perto:2", "variante:Perto:1", "base:Longe:2", "variante:Longe:1" },
                _registro,
                "A variante soma ao sinal de todo mundo, e nao troca (docs/13 secao 5).");
        }

        [UnityTest]
        public IEnumerator Intensidade_escala_a_potencia_de_todo_efeito()
        {
            CriarCriatura("Frente", new Vector3(0f, 0f, 3f));
            yield return null;

            var variante = new SignEffectDef[] { CriarEfeito("variante", 1f) };
            Resolver(intensidade: 1.5f, variante: variante);

            CollectionAssert.AreEqual(new[] { "base:Frente:3", "variante:Frente:1.5" }, _registro);
        }

        [UnityTest]
        public IEnumerator Efeito_vazio_na_lista_e_pulado_sem_quebrar()
        {
            _sinal.effects = new SignEffectDef[] { null, _efeito };
            CriarCriatura("Frente", new Vector3(0f, 0f, 3f));
            yield return null;

            Assert.DoesNotThrow(() => Resolver());
            Assert.AreEqual(1, _registro.Count);
        }
    }
}
