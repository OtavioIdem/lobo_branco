using System.Collections;
using System.Collections.Generic;
using LoboBranco.Combat;
using LoboBranco.Core;
using LoboBranco.Stats;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LoboBranco.Tests
{
    /// <summary>
    /// A armadilha do sinal de campo (tarefa 1.18f): quem esta dentro fica lento, quem esta fora
    /// nao, e ela some na hora marcada. Sem rede, que e o caminho em que o proprio objeto resolve.
    ///
    /// O relogio e passado a mao ao <see cref="SignTrap.Step"/>, entao nenhum teste espera 12 s.
    ///
    /// O defeito silencioso aqui e a lentidao que nao sai: se a armadilha aplicasse os 12 s de uma
    /// vez, a criatura sairia de dentro dela e continuaria arrastando o pe ate o fim, e nada no
    /// Console diria isso.
    /// </summary>
    public sealed class SignTrapTests
    {
        readonly List<Object> _criados = new List<Object>();

        SignTrap _armadilha;

        [SetUp]
        public void SetUp() => _armadilha = CriarArmadilha(Vector3.zero);

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _criados.Count; i++)
                if (_criados[i] != null)
                    Object.DestroyImmediate(_criados[i]);

            _criados.Clear();
        }

        /// <summary>
        /// A armadilha nasce desarmada, e assim ela fica: sem prazo, ela nao varre e nao some. Foi
        /// este teste que mostrou que "sem prazo" estava sendo lido como "prazo vencido", e a
        /// armadilha se destruia no primeiro quadro.
        /// </summary>
        SignTrap CriarArmadilha(Vector3 posicao)
        {
            var go = new GameObject("Armadilha");
            go.transform.position = posicao;
            _criados.Add(go);

            return go.AddComponent<SignTrap>();
        }

        /// <summary>Uma criatura que pode ser lentificada, na layer que a armadilha procura.</summary>
        ControlStatus CriarCriatura(Vector3 posicao)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Criatura";
            go.transform.position = posicao;
            go.layer = GameLayers.IndexOf(GameLayers.Enemy);
            _criados.Add(go);

            var vitals = go.AddComponent<CharacterVitals>();
            vitals.Stats.SetBase(StatType.MaxVitality, 55f);
            vitals.Stats.SetBase(StatType.MoveSpeed, 1f);
            vitals.RestoreToFull();

            var status = go.AddComponent<ControlStatus>();
            Physics.SyncTransforms();

            return status;
        }

        void Varrer() => _armadilha.Step(Time.timeAsDouble, deltaTime: 1f);

        [UnityTest]
        public IEnumerator Armadilha_nunca_armada_nao_some_e_nao_varre()
        {
            ControlStatus dentro = CriarCriatura(new Vector3(0f, 1f, 2f));
            yield return null;

            Varrer();

            Assert.IsFalse(_armadilha == null, "Sem prazo nao e prazo vencido.");
            Assert.AreEqual(0f, dentro.SlowFraction);
        }

        [UnityTest]
        public IEnumerator Criatura_dentro_do_raio_fica_lenta()
        {
            _armadilha.Arm(12f);
            ControlStatus dentro = CriarCriatura(new Vector3(0f, 1f, 2f));
            yield return null;

            Varrer();

            Assert.AreEqual(0.6f, dentro.SlowFraction, 0.001f, "docs/03 secao 8: lentidao de 60%.");
        }

        [UnityTest]
        public IEnumerator Criatura_fora_do_raio_nao_fica_lenta()
        {
            _armadilha.Arm(12f);
            ControlStatus fora = CriarCriatura(new Vector3(0f, 1f, 7f));
            yield return null;

            Varrer();

            Assert.AreEqual(0f, fora.SlowFraction, "O raio e de 4 m (docs/03 secao 8).");
        }

        [UnityTest]
        public IEnumerator Lentidao_dura_pouco_alem_do_pulso_para_quem_sai_voltar_a_correr()
        {
            _armadilha.Arm(12f);
            ControlStatus dentro = CriarCriatura(new Vector3(0f, 1f, 2f));
            yield return null;

            Varrer();

            Assert.Less(dentro.SlowRemaining, 1f,
                "A armadilha reaplica em pulsos: aplicar os 12 s de uma vez faria a lentidao seguir " +
                "a criatura para fora dela.");
            Assert.Greater(dentro.SlowRemaining, 0f);
        }

        [UnityTest]
        public IEnumerator Lentidao_e_renovada_enquanto_a_criatura_fica_dentro()
        {
            _armadilha.Arm(12f);
            ControlStatus dentro = CriarCriatura(new Vector3(0f, 1f, 2f));
            yield return null;

            Varrer();
            float primeira = dentro.SlowRemaining;

            yield return null;
            Varrer();

            Assert.GreaterOrEqual(dentro.SlowRemaining, primeira - 0.05f, "O segundo pulso renova.");
            Assert.AreEqual(0.6f, dentro.SlowFraction, 0.001f);
        }

        [UnityTest]
        public IEnumerator Armadilha_some_na_hora_marcada()
        {
            _armadilha.Arm(12f);
            GameObject objeto = _armadilha.gameObject;

            Assert.IsTrue(_armadilha.IsActive);
            Assert.AreEqual(12f, _armadilha.Remaining, 0.1f);

            _armadilha.Step(Time.timeAsDouble + 13d, deltaTime: 1f);
            yield return null;

            Assert.IsTrue(objeto == null, "Uma armadilha que nao some vira chao lento para sempre.");
        }

        [UnityTest]
        public IEnumerator Armadilha_vencida_nao_lentifica_mais_ninguem()
        {
            ControlStatus dentro = CriarCriatura(new Vector3(0f, 1f, 2f));
            yield return null;

            // Armar e passar do fim no mesmo quadro: com um quadro entre as duas linhas, o Update da
            // propria armadilha varreria antes de vencer, e o teste mediria o cenario errado.
            _armadilha.Arm(0.5f);
            _armadilha.Step(Time.timeAsDouble + 1d, deltaTime: 1f);

            Assert.AreEqual(0f, dentro.SlowFraction);
        }
    }
}
