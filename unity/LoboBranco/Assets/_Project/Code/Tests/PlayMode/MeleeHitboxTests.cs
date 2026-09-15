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
    /// A janela de dano: alcance, arco, e a lista de ja-atingidos que impede o mesmo alvo
    /// de levar o mesmo golpe duas vezes (docs/07 secao 4.5).
    ///
    /// Em PlayMode porque a consulta e de fisica de verdade. O que nao esta aqui e tempo:
    /// nenhum teste espera frames nem mede duracao, porque o framerate em batchmode e
    /// erratico. A linha do tempo do golpe e verificada em EditMode, com passos fixos.
    ///
    /// O golpe duplo e exatamente um bug silencioso: ele nao lanca erro, so faz o inimigo
    /// morrer com metade dos golpes, e ninguem descobre pelo Console.
    /// </summary>
    /// <summary>Alvo minimo: existe so para ser encontrado pela consulta.</summary>
    public sealed class AlvoFalso : MonoBehaviour, IDamageable
    {
        public readonly List<float> Recebidos = new List<float>();

        public StatSheet Stats { get; } = new StatSheet();
        public CreatureClass CreatureClass => Combat.CreatureClass.Beast;
        public StanceArchetype Archetype => StanceArchetype.Agile;
        public OilClass VulnerableToOil => OilClass.Beast;

        public float GetResistance(DamageType type) => 1f;
        public bool IsDown => false;
        public void ApplyDamage(in DamageResult result) => Recebidos.Add(result.Amount);
    }

    public sealed class MeleeHitboxTests
    {
        MeleeHitbox _hitbox;
        AttackDef _ataque;
        IDamageable[] _resultados;
        readonly List<GameObject> _criados = new List<GameObject>();

        LayerMask _todasAsLayers;

        [SetUp]
        public void SetUp()
        {
            _hitbox = new MeleeHitbox();
            _resultados = new IDamageable[8];
            _todasAsLayers = Physics.AllLayers;

            _ataque = ScriptableObject.CreateInstance<AttackDef>();
            _ataque.strikeTime = 0.25f;
            _ataque.recovery = 0.15f;
            _ataque.maxTargets = 1;
            _ataque.arcDegrees = 110f;
            _ataque.reach = 2.2f;
            _ataque.radius = 0.55f;
            _ataque.heightOffset = 1.1f;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _criados.Count; i++)
                if (_criados[i] != null)
                    Object.DestroyImmediate(_criados[i]);

            _criados.Clear();
            Object.DestroyImmediate(_ataque);
        }

        AlvoFalso CriarAlvo(Vector3 posicao)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"Alvo_{_criados.Count}";
            go.transform.position = posicao;

            var alvo = go.AddComponent<AlvoFalso>();
            _criados.Add(go);

            // Sem isto a fisica ainda enxerga o colisor na posicao de origem.
            Physics.SyncTransforms();

            return alvo;
        }

        int Golpear(Vector3 origem, Vector3 frente)
            => _hitbox.Query(origem, frente, _ataque, _todasAsLayers, _resultados);

        // ---------------------------------------------------------------- alcance

        [UnityTest]
        public IEnumerator Alvo_a_frente_e_dentro_do_alcance_e_atingido()
        {
            AlvoFalso alvo = CriarAlvo(new Vector3(0f, 1f, 2f));
            yield return null;

            _hitbox.BeginSwing();
            int atingidos = Golpear(Vector3.zero, Vector3.forward);

            Assert.AreEqual(1, atingidos);
            Assert.AreSame(alvo, _resultados[0]);
        }

        [UnityTest]
        public IEnumerator Alvo_alem_do_alcance_nao_e_atingido()
        {
            CriarAlvo(new Vector3(0f, 1f, 6f));
            yield return null;

            _hitbox.BeginSwing();

            Assert.AreEqual(0, Golpear(Vector3.zero, Vector3.forward),
                "Alcance de 2,2 m nao pode alcancar 6 m.");
        }

        [UnityTest]
        public IEnumerator Alvo_atras_nao_e_atingido()
        {
            // Colado nas costas: perto o bastante para a capsula de consulta encostar
            // nele, para que quem recuse o acerto seja o arco e nao o alcance.
            CriarAlvo(new Vector3(0f, 1f, -0.2f));
            yield return null;

            _hitbox.BeginSwing();

            Assert.AreEqual(0, Golpear(Vector3.zero, Vector3.forward),
                "O filtro de arco e o que separa golpe a frente de golpe em volta.");
        }

        [UnityTest]
        public IEnumerator Arco_de_180_graus_alcanca_o_flanco_que_o_arco_estreito_perde()
        {
            // A 72 graus da frente: dentro da capsula larga, fora do arco de 110.
            CriarAlvo(new Vector3(1.5f, 1f, 0.5f));
            _ataque.radius = 1.2f;
            yield return null;

            _hitbox.BeginSwing();
            Assert.AreEqual(0, Golpear(Vector3.zero, Vector3.forward),
                "Com 110 graus, um alvo quase a 90 graus fica de fora.");

            _ataque.arcDegrees = 180f;
            _hitbox.BeginSwing();

            Assert.AreEqual(1, Golpear(Vector3.zero, Vector3.forward),
                "Com 180 graus, o mesmo alvo entra: e o que da funcao a postura Grupo.");
        }

        // ------------------------------------------------------- ja-atingidos

        [UnityTest]
        public IEnumerator Mesmo_alvo_nao_e_atingido_duas_vezes_no_mesmo_golpe()
        {
            CriarAlvo(new Vector3(0f, 1f, 2f));
            yield return null;

            _hitbox.BeginSwing();

            Assert.AreEqual(1, Golpear(Vector3.zero, Vector3.forward));

            // A janela fica aberta por varios quadros e consulta em todos eles.
            for (int i = 0; i < 5; i++)
                Assert.AreEqual(0, Golpear(Vector3.zero, Vector3.forward),
                    "A partir da segunda consulta, o alvo ja esta na lista de atingidos.");

            Assert.AreEqual(1, _hitbox.HitCount);
        }

        [UnityTest]
        public IEnumerator Golpe_novo_libera_o_alvo_para_ser_atingido_de_novo()
        {
            CriarAlvo(new Vector3(0f, 1f, 2f));
            yield return null;

            _hitbox.BeginSwing();
            Assert.AreEqual(1, Golpear(Vector3.zero, Vector3.forward));
            Assert.AreEqual(0, Golpear(Vector3.zero, Vector3.forward));

            _hitbox.BeginSwing();

            Assert.AreEqual(1, Golpear(Vector3.zero, Vector3.forward),
                "Sem limpar a lista, o segundo golpe nunca acertaria o mesmo inimigo.");
        }

        // ------------------------------------------------------------- teto de alvos

        [UnityTest]
        public IEnumerator Teto_de_alvos_do_asset_limita_o_golpe_inteiro()
        {
            CriarAlvo(new Vector3(-1.0f, 1f, 1.6f));
            CriarAlvo(new Vector3(0f, 1f, 1.8f));
            CriarAlvo(new Vector3(1.0f, 1f, 1.6f));
            yield return null;

            _ataque.arcDegrees = 180f;
            _ataque.maxTargets = 2;
            _ataque.radius = 0.9f;
            _ataque.reach = 2.6f;

            _hitbox.BeginSwing();
            int primeira = Golpear(Vector3.zero, Vector3.forward);
            int segunda = Golpear(Vector3.zero, Vector3.forward);

            Assert.AreEqual(2, primeira + segunda,
                "Duas posturas acertam ate quatro; esta esta limitada a dois pelo asset.");
            Assert.AreEqual(2, _hitbox.HitCount);
        }

        [UnityTest]
        public IEnumerator Golpe_em_grupo_alcanca_varios_alvos_de_uma_vez()
        {
            CriarAlvo(new Vector3(-1.0f, 1f, 1.6f));
            CriarAlvo(new Vector3(0f, 1f, 1.8f));
            CriarAlvo(new Vector3(1.0f, 1f, 1.6f));
            yield return null;

            _ataque.arcDegrees = 180f;
            _ataque.maxTargets = 4;
            _ataque.radius = 0.9f;
            _ataque.reach = 2.6f;

            _hitbox.BeginSwing();

            Assert.AreEqual(3, Golpear(Vector3.zero, Vector3.forward),
                "Postura Grupo acerta ate 4 em arco de 180 graus (docs/03 secao 4).");
        }

        // ------------------------------------------------------------- robustez

        [UnityTest]
        public IEnumerator Objeto_sem_IDamageable_e_ignorado()
        {
            var parede = GameObject.CreatePrimitive(PrimitiveType.Cube);
            parede.transform.position = new Vector3(0f, 1f, 1.5f);
            _criados.Add(parede);
            Physics.SyncTransforms();
            yield return null;

            _hitbox.BeginSwing();

            Assert.AreEqual(0, Golpear(Vector3.zero, Vector3.forward),
                "Cenario nao leva dano de espada.");
        }

        [UnityTest]
        public IEnumerator Direcao_nula_nao_produz_consulta()
        {
            CriarAlvo(new Vector3(0f, 1f, 2f));
            yield return null;

            _hitbox.BeginSwing();

            Assert.AreEqual(0, Golpear(Vector3.zero, Vector3.zero),
                "Sem direcao nao ha frente, e um golpe sem frente acertaria tudo em volta.");
        }

        [UnityTest]
        public IEnumerator Alvo_em_filho_do_colisor_e_encontrado_pela_hierarquia()
        {
            var raiz = new GameObject("Inimigo");
            var alvo = raiz.AddComponent<AlvoFalso>();
            _criados.Add(raiz);

            var corpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            corpo.transform.SetParent(raiz.transform, false);
            raiz.transform.position = new Vector3(0f, 1f, 2f);
            Physics.SyncTransforms();
            yield return null;

            _hitbox.BeginSwing();

            Assert.AreEqual(1, Golpear(Vector3.zero, Vector3.forward));
            Assert.AreSame(alvo, _resultados[0],
                "O colisor atingido raramente e o objeto que leva dano.");
        }
    }
}
