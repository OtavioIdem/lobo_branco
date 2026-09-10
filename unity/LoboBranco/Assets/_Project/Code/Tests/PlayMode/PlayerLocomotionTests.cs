using System.Collections;
using LoboBranco.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LoboBranco.Tests
{
    /// <summary>
    /// Testes de movimento, determinísticos.
    ///
    /// O componente fica desabilitado e o teste chama <see cref="PlayerLocomotion.Tick"/>
    /// com passos fixos de 1/60. Assim um segundo de simulação é sempre um segundo, e não
    /// o que o framerate errático do batchmode sem gráficos resolveu entregar naquela
    /// execução. A versão anterior destes testes dependia de Time.deltaTime e era
    /// intermitente por causa disso.
    ///
    /// Continuam em PlayMode porque CharacterController.Move precisa do runtime.
    /// </summary>
    public sealed class PlayerLocomotionTests
    {
        GameObject _ground;
        GameObject _player;
        PlayerLocomotion _locomotion;

        const float Step = 1f / 60f;
        const int OneSecond = 60;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _ground.transform.localScale = new Vector3(60f, 1f, 60f);
            _ground.transform.position = new Vector3(0f, -0.5f, 0f);

            _player = new GameObject("Player");
            _player.transform.position = new Vector3(0f, 0.1f, 0f);

            var controller = _player.AddComponent<CharacterController>();
            controller.height = 1.85f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.925f, 0f);
            controller.stepOffset = 0.4f;

            _locomotion = _player.AddComponent<PlayerLocomotion>();

            // Desabilitado para o Update automático não somar passos por cima dos nossos.
            // Tick continua funcionando: ele é público e não depende do componente estar ativo.
            yield return null;
            _locomotion.enabled = false;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_player);
            Object.DestroyImmediate(_ground);
        }

        /// <summary>Simula uma quantidade exata de tempo, em passos fixos.</summary>
        void Simulate(float seconds)
        {
            int steps = Mathf.RoundToInt(seconds / Step);
            for (int i = 0; i < steps; i++)
                _locomotion.Tick(Step);
        }

        // --------------------------------------------------------------- parado

        [Test]
        public void Parado_sem_input_nao_se_move_no_plano()
        {
            _locomotion.MoveInput = Vector2.zero;
            Simulate(0.5f);

            Vector3 p = _player.transform.position;
            float drift = new Vector2(p.x, p.z).magnitude;

            Assert.Less(drift, 0.01f, "O jogador deslizou sem input.");
        }

        // ------------------------------------------------------------- direcao

        [Test]
        public void Input_para_frente_move_na_direcao_do_yaw_de_referencia()
        {
            _locomotion.ReferenceYaw = 0f;
            _locomotion.MoveInput = Vector2.up;
            Simulate(1f);

            AssertMovedTowards(Vector3.forward, "yaw de referência zero deve mover em +Z");
        }

        [Test]
        public void Yaw_de_referencia_de_90_graus_move_em_X_positivo()
        {
            _locomotion.ReferenceYaw = 90f;
            _locomotion.MoveInput = Vector2.up;
            Simulate(1f);

            AssertMovedTowards(Vector3.right, "com a câmera virada 90 graus, 'para frente' é +X");
        }

        [Test]
        public void Input_lateral_move_perpendicular_ao_yaw_de_referencia()
        {
            _locomotion.ReferenceYaw = 0f;
            _locomotion.MoveInput = Vector2.right;
            Simulate(1f);

            AssertMovedTowards(Vector3.right, "'D' com câmera em zero deve mover em +X");
        }

        [Test]
        public void O_personagem_encara_a_direcao_em_que_anda()
        {
            _locomotion.ReferenceYaw = 45f;
            _locomotion.MoveInput = Vector2.up;
            Simulate(1f);

            float angle = Vector3.Angle(_player.transform.forward, new Vector3(1f, 0f, 1f).normalized);

            Assert.Less(angle, 5f,
                $"O corpo deveria ter girado para a direção do movimento. Desvio de {angle:F1} graus.");
        }

        // ----------------------------------------------------------- velocidade

        [Test]
        public void Velocidade_de_caminhada_estabiliza_em_dois_metros_por_segundo()
        {
            _locomotion.MoveInput = Vector2.up;
            _locomotion.SprintHeld = false;
            Simulate(1.5f);

            Assert.AreEqual(2.0f, _locomotion.CurrentSpeed, 0.05f,
                "Caminhada deve ficar em 2,0 m/s (docs/08 seção 2).");
        }

        [Test]
        public void Velocidade_de_corrida_estabiliza_em_cinco_e_meio()
        {
            _locomotion.MoveInput = Vector2.up;
            _locomotion.SprintHeld = true;
            Simulate(1.5f);

            Assert.AreEqual(5.5f, _locomotion.CurrentSpeed, 0.05f,
                "Corrida deve ficar em 5,5 m/s (docs/08 seção 2).");
        }

        [Test]
        public void Um_segundo_de_caminhada_cobre_perto_de_dois_metros()
        {
            _locomotion.ReferenceYaw = 0f;
            _locomotion.MoveInput = Vector2.up;

            Simulate(0.5f);  // deixa a aceleração terminar antes de medir
            Vector3 start = _player.transform.position;
            Simulate(1f);
            Vector3 end = _player.transform.position;

            float distance = new Vector2(end.x - start.x, end.z - start.z).magnitude;

            // Agora que o passo é fixo, dá para afirmar distância sem o teste ficar intermitente.
            Assert.AreEqual(2.0f, distance, 0.15f);
        }

        [Test]
        public void Correr_cobre_mais_distancia_que_caminhar()
        {
            _locomotion.ReferenceYaw = 0f;
            _locomotion.MoveInput = Vector2.up;
            _locomotion.SprintHeld = false;
            Simulate(1.5f);
            float walked = _player.transform.position.z;

            _locomotion.Teleport(new Vector3(0f, 0.1f, 0f), 0f);
            _locomotion.MoveInput = Vector2.up;
            _locomotion.SprintHeld = true;
            Simulate(1.5f);
            float ran = _player.transform.position.z;

            Assert.Greater(ran, walked * 2f,
                $"Correndo deveria cobrir bem mais. Caminhou {walked:F2} m, correu {ran:F2} m.");
        }

        // -------------------------------------------------------------- estado

        [Test]
        public void Teleport_reposiciona_e_zera_a_inercia()
        {
            _locomotion.MoveInput = Vector2.up;
            _locomotion.SprintHeld = true;
            Simulate(0.8f);

            Assert.Greater(_locomotion.CurrentSpeed, 1f, "Deveria estar em movimento antes do teleporte.");

            _locomotion.Teleport(new Vector3(10f, 0.1f, -4f), 180f);
            _locomotion.MoveInput = Vector2.zero;

            Assert.AreEqual(10f, _player.transform.position.x, 0.05f);
            Assert.AreEqual(-4f, _player.transform.position.z, 0.05f);
            Assert.AreEqual(0f, _locomotion.CurrentSpeed, 0.001f, "A inércia deveria ter sido zerada.");
        }

        [Test]
        public void Delta_zero_ou_negativo_nao_faz_nada()
        {
            _locomotion.MoveInput = Vector2.up;
            Vector3 before = _player.transform.position;

            _locomotion.Tick(0f);
            _locomotion.Tick(-0.5f);

            Assert.AreEqual(before, _player.transform.position);
        }

        // ----------------------------------------------------------- auxiliares

        void AssertMovedTowards(Vector3 expectedDirection, string because)
        {
            Vector3 p = _player.transform.position;
            var displacement = new Vector3(p.x, 0f, p.z);

            Assert.Greater(displacement.magnitude, 0.5f,
                $"Não saiu do lugar, então não há direção para avaliar ({because}).");

            float angle = Vector3.Angle(displacement.normalized, expectedDirection);

            Assert.Less(angle, 5f,
                $"Direção errada: {because}. Desvio de {angle:F1} graus, deslocamento {displacement}.");
        }
    }
}
