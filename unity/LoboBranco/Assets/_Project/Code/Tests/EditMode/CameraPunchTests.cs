using LoboBranco.CameraSystem;
using NUnit.Framework;

namespace LoboBranco.Tests
{
    /// <summary>
    /// O soco de camera do docs/03 secao 11: desloca no impacto e volta desacelerando.
    ///
    /// O que estes testes protegem e o que o jogador sente e ninguem mede. Um soco que
    /// acumula deixa a camera torta depois de uma luta longa; um soco que chega atrasado um
    /// quadro deixa de parecer impacto e passa a parecer atraso de rede.
    /// </summary>
    public sealed class CameraPunchTests
    {
        CameraPunch _soco;

        const float Graus = 2f;
        const float Recuperacao = 0.2f;

        [SetUp]
        public void SetUp() => _soco = new CameraPunch();

        [Test]
        public void Sem_soco_nao_ha_deslocamento()
        {
            _soco.Tick(0.1f);

            Assert.AreEqual(0f, _soco.Offset);
        }

        /// <summary>O deslocamento acontece no proprio quadro do impacto, antes de qualquer passo.</summary>
        [Test]
        public void O_soco_desloca_na_hora()
        {
            _soco.Kick(Graus, Recuperacao);

            Assert.AreEqual(Graus, _soco.Offset, 0.0001f);
        }

        [Test]
        public void A_camera_volta_ao_lugar_no_tempo_de_recuperacao()
        {
            _soco.Kick(Graus, Recuperacao);
            _soco.Tick(Recuperacao);

            Assert.AreEqual(0f, _soco.Offset);
        }

        [Test]
        public void A_volta_nunca_passa_do_lugar_nem_recua()
        {
            _soco.Kick(Graus, Recuperacao);

            float anterior = _soco.Offset;

            for (int i = 0; i < 12; i++)
            {
                _soco.Tick(Recuperacao / 10f);

                Assert.LessOrEqual(_soco.Offset, anterior);
                Assert.GreaterOrEqual(_soco.Offset, 0f);

                anterior = _soco.Offset;
            }
        }

        /// <summary>
        /// Um golpe leve logo depois de um forte nao pode encolher a sensacao do forte. Sem
        /// esta regra, encadear golpes deixaria a camera cada vez menos reativa.
        /// </summary>
        [Test]
        public void Soco_menor_no_meio_de_um_maior_nao_encolhe_o_maior()
        {
            _soco.Kick(Graus, Recuperacao);
            _soco.Tick(0.05f);

            float antes = _soco.Offset;
            _soco.Kick(0.1f, Recuperacao);

            Assert.AreEqual(antes, _soco.Offset, 0.0001f);
        }

        [Test]
        public void Soco_maior_substitui_o_que_sobrou_do_menor()
        {
            _soco.Kick(0.5f, Recuperacao);
            _soco.Tick(0.05f);
            _soco.Kick(Graus, Recuperacao);

            Assert.AreEqual(Graus, _soco.Offset, 0.0001f);
        }

        /// <summary>Dez golpes seguidos nao podem deixar a camera vinte graus mais baixa.</summary>
        [Test]
        public void Socos_seguidos_nao_acumulam()
        {
            for (int i = 0; i < 10; i++)
            {
                _soco.Kick(Graus, Recuperacao);
                _soco.Tick(0.01f);
            }

            Assert.LessOrEqual(_soco.Offset, Graus);
        }

        [Test]
        public void Recuperacao_zero_nao_divide_por_zero()
        {
            _soco.Kick(Graus, 0f);
            _soco.Tick(0.001f);

            Assert.AreEqual(0f, _soco.Offset);
            Assert.IsFalse(float.IsNaN(_soco.Offset));
        }
    }
}
