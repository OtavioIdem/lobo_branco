using System.Collections;
using LoboBranco.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LoboBranco.Tests
{
    /// <summary>
    /// Testes de movimento. Possiveis porque PlayerLocomotion nao conhece o Input System:
    /// o teste escreve direto em MoveInput. Ver docs/07_ARQUITETURA_TECNICA.md secao 9.
    /// </summary>
    public sealed class PlayerLocomotionTests
    {
        GameObject _ground;
        GameObject _player;
        PlayerLocomotion _locomotion;

        [SetUp]
        public void SetUp()
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
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_player);
            Object.DestroyImmediate(_ground);
        }

        [UnityTest]
        public IEnumerator Parado_sem_input_nao_se_move_no_plano()
        {
            Vector3 start = _player.transform.position;

            _locomotion.MoveInput = Vector2.zero;
            yield return WaitSeconds(0.5f);

            Vector3 end = _player.transform.position;
            float planarDrift = new Vector2(end.x - start.x, end.z - start.z).magnitude;

            Assert.Less(planarDrift, 0.01f, "O jogador deslizou sem input.");
        }

        // Estes dois testes verificam DIRECAO, nao distancia. Distancia percorrida depende
        // do framerate, que em batchmode -nographics e erratico, e um teste que depende
        // dele e intermitente. Quanto o jogador anda por segundo e assunto do teste de
        // velocidade, que mede CurrentSpeed e nao deslocamento.

        [UnityTest]
        public IEnumerator Input_para_frente_move_na_direcao_do_yaw_de_referencia()
        {
            _locomotion.ReferenceYaw = 0f;
            _locomotion.MoveInput = Vector2.up;

            yield return WaitSeconds(1.0f);

            AssertMovedTowards(Vector3.forward, "yaw de referencia zero deve mover em +Z");
        }

        [UnityTest]
        public IEnumerator Yaw_de_referencia_de_90_graus_move_em_X_positivo()
        {
            _locomotion.ReferenceYaw = 90f;
            _locomotion.MoveInput = Vector2.up;

            yield return WaitSeconds(1.0f);

            AssertMovedTowards(Vector3.right, "com a camera virada 90 graus, 'para frente' e +X");
        }

        [UnityTest]
        public IEnumerator Input_lateral_move_perpendicular_ao_yaw_de_referencia()
        {
            _locomotion.ReferenceYaw = 0f;
            _locomotion.MoveInput = Vector2.right;

            yield return WaitSeconds(1.0f);

            AssertMovedTowards(Vector3.right, "'D' com camera em zero deve mover em +X");
        }

        [UnityTest]
        public IEnumerator O_personagem_encara_a_direcao_em_que_anda()
        {
            _locomotion.ReferenceYaw = 45f;
            _locomotion.MoveInput = Vector2.up;

            yield return WaitSeconds(1.0f);

            float angle = Vector3.Angle(_player.transform.forward, new Vector3(1f, 0f, 1f).normalized);

            Assert.Less(angle, 5f,
                $"O corpo deveria ter girado para a direcao do movimento. Desvio de {angle:F1} graus.");
        }

        [UnityTest]
        public IEnumerator Sprint_percorre_mais_distancia_que_caminhada()
        {
            _locomotion.ReferenceYaw = 0f;
            _locomotion.MoveInput = Vector2.up;
            _locomotion.SprintHeld = false;
            yield return WaitSeconds(1.5f);
            float walked = _player.transform.position.z;

            _locomotion.Teleport(new Vector3(0f, 0.1f, 0f), 0f);
            yield return null;

            _locomotion.MoveInput = Vector2.up;
            _locomotion.SprintHeld = true;
            yield return WaitSeconds(1.5f);
            float ran = _player.transform.position.z;

            Assert.Greater(ran, walked * 1.5f,
                $"Correndo deveria cobrir bem mais que caminhando. Caminhou {walked:F2} m, correu {ran:F2} m.");
        }

        [UnityTest]
        public IEnumerator Velocidade_de_caminhada_estabiliza_perto_de_dois_metros_por_segundo()
        {
            _locomotion.MoveInput = Vector2.up;
            _locomotion.SprintHeld = false;

            yield return WaitSeconds(1.5f); // tempo de sobra para o SmoothDamp convergir

            Assert.AreEqual(2.0f, _locomotion.CurrentSpeed, 0.15f,
                "Caminhada deve ficar em 2,0 m/s (docs/08 secao 2).");
        }

        [UnityTest]
        public IEnumerator Teleport_reposiciona_e_zera_a_inercia()
        {
            _locomotion.MoveInput = Vector2.up;
            _locomotion.SprintHeld = true;
            yield return WaitSeconds(0.8f);

            Assert.Greater(_locomotion.CurrentSpeed, 1f, "Deveria estar em movimento antes do teleporte.");

            _locomotion.Teleport(new Vector3(10f, 0.1f, -4f), 180f);
            _locomotion.MoveInput = Vector2.zero;
            yield return null;

            Assert.AreEqual(10f, _player.transform.position.x, 0.05f);
            Assert.AreEqual(-4f, _player.transform.position.z, 0.05f);
            Assert.AreEqual(0f, _locomotion.CurrentSpeed, 0.001f, "A inercia deveria ter sido zerada.");
        }

        /// <summary>
        /// Verifica que o deslocamento no plano XZ aponta para a direcao esperada, e que
        /// houve deslocamento de fato. Independente de framerate.
        /// </summary>
        void AssertMovedTowards(Vector3 expectedDirection, string because)
        {
            Vector3 p = _player.transform.position;
            var displacement = new Vector3(p.x, 0f, p.z);

            Assert.Greater(displacement.magnitude, 0.1f,
                $"Nao saiu do lugar, entao nao ha direcao para avaliar ({because}).");

            float angle = Vector3.Angle(displacement.normalized, expectedDirection);

            Assert.Less(angle, 5f,
                $"Direcao errada: {because}. Desvio de {angle:F1} graus, deslocamento {displacement}.");
        }

        static IEnumerator WaitSeconds(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
}
