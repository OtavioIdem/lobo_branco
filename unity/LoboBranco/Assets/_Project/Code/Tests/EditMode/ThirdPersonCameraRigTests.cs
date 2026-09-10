using LoboBranco.CameraSystem;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// Testes do pivo de camera. Rodam em EditMode porque o rig e matematica pura:
    /// ApplyLook acumula e limita angulos, e PlanarForward deriva do yaw. Nada disso
    /// precisa do loop de jogo, e testar em EditMode e instantaneo.
    ///
    /// As asserçoes evitam depender dos valores de sensibilidade, que sao de
    /// balanceamento e vao mudar. O que e testado e o contrato: yaw circula,
    /// pitch e limitado, e PlanarForward concorda com o yaw.
    /// </summary>
    public sealed class ThirdPersonCameraRigTests
    {
        GameObject _pivot;
        ThirdPersonCameraRig _rig;

        // Espelham os limites declarados em ThirdPersonCameraRig.
        const float PitchMax = 70f;
        const float PitchMin = -35f;

        [SetUp]
        public void SetUp()
        {
            _pivot = new GameObject("CameraPivot");
            _rig = _pivot.AddComponent<ThirdPersonCameraRig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_pivot);
        }

        [Test]
        public void Pitch_nao_passa_do_limite_inferior_olhando_para_baixo()
        {
            _rig.ApplyLook(new Vector2(0f, -10_000f));

            Assert.AreEqual(PitchMax, _rig.Pitch, 0.001f,
                "Olhar para baixo sem parar deveria travar no limite, nao virar a camera de cabeca para baixo.");
        }

        [Test]
        public void Pitch_nao_passa_do_limite_superior_olhando_para_cima()
        {
            _rig.ApplyLook(new Vector2(0f, 10_000f));

            Assert.AreEqual(PitchMin, _rig.Pitch, 0.001f,
                "Olhar para cima sem parar deveria travar no limite.");
        }

        [Test]
        public void Mouse_para_cima_olha_para_cima_por_padrao()
        {
            float before = _rig.Pitch;

            _rig.ApplyLook(new Vector2(0f, 1f));

            Assert.Less(_rig.Pitch, before,
                "Sem inversao, delta positivo em Y deve reduzir o pitch, que e olhar para cima.");
        }

        [Test]
        public void Yaw_circula_dentro_de_zero_a_360()
        {
            for (int i = 0; i < 50; i++)
                _rig.ApplyLook(new Vector2(100f, 0f));

            Assert.GreaterOrEqual(_rig.Yaw, 0f);
            Assert.Less(_rig.Yaw, 360f, "Yaw acumulado deveria ter sido normalizado.");
        }

        [Test]
        public void Yaw_negativo_tambem_circula_para_dentro_da_faixa()
        {
            _rig.ApplyLook(new Vector2(-100f, 0f));

            Assert.GreaterOrEqual(_rig.Yaw, 0f, "Girar para a esquerda no inicio nao deveria produzir yaw negativo.");
            Assert.Less(_rig.Yaw, 360f);
        }

        [Test]
        public void PlanarForward_e_horizontal_e_normalizado()
        {
            _rig.ApplyLook(new Vector2(37f, -21f));

            Vector3 forward = _rig.PlanarForward;

            Assert.AreEqual(0f, forward.y, 0.0001f, "PlanarForward nao deve ter componente vertical.");
            Assert.AreEqual(1f, forward.magnitude, 0.0001f, "PlanarForward deve estar normalizado.");
        }

        [Test]
        public void PlanarForward_concorda_com_o_yaw()
        {
            _rig.ApplyLook(new Vector2(13f, 0f));

            Vector3 expected = Quaternion.Euler(0f, _rig.Yaw, 0f) * Vector3.forward;
            float angle = Vector3.Angle(_rig.PlanarForward, expected);

            Assert.Less(angle, 0.01f,
                "PlanarForward e o yaw precisam concordar: o movimento do jogador depende dos dois.");
        }

        [Test]
        public void AlignBehindTarget_adota_o_yaw_do_alvo()
        {
            var target = new GameObject("Target");
            target.transform.rotation = Quaternion.Euler(0f, 123f, 0f);
            _rig.FollowTarget = target.transform;

            _rig.AlignBehindTarget();

            Assert.AreEqual(123f, _rig.Yaw, 0.01f);

            Object.DestroyImmediate(target);
        }

        [Test]
        public void Sem_alvo_os_metodos_de_posicionamento_nao_lancam()
        {
            _rig.FollowTarget = null;

            Assert.DoesNotThrow(() => _rig.SnapToTarget());
            Assert.DoesNotThrow(() => _rig.AlignBehindTarget());
            Assert.DoesNotThrow(() => _rig.ApplyLook(Vector2.one));
        }

        [Test]
        public void SnapToTarget_posiciona_o_pivo_acima_do_alvo()
        {
            var target = new GameObject("Target");
            target.transform.position = new Vector3(5f, 0f, -3f);
            _rig.FollowTarget = target.transform;

            _rig.SnapToTarget();

            Vector3 p = _pivot.transform.position;

            Assert.AreEqual(5f, p.x, 0.001f);
            Assert.AreEqual(-3f, p.z, 0.001f);
            Assert.Greater(p.y, 1f, "O pivo deveria estar na altura dos olhos, nao no chao.");

            Object.DestroyImmediate(target);
        }
    }
}
