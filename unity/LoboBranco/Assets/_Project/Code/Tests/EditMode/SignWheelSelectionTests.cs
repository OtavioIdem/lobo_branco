using LoboBranco.Player;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// A escolha do sinal (tarefa 1.18g): a conta de angulo da roda, a zona morta e as teclas
    /// diretas. Em EditMode porque a regra nao conhece input nem tela.
    ///
    /// O defeito que importa e trocar de sinal sem querer. Abrir a roda e soltar sem mirar tem que
    /// manter o sinal que ja estava, senao o jogador perde o sinal escolhido no meio da luta e nao
    /// entende por que.
    /// </summary>
    public sealed class SignWheelSelectionTests
    {
        SignWheelSelection _roda;

        [SetUp]
        public void SetUp()
        {
            _roda = new SignWheelSelection { SlotCount = 4 };
        }

        static Vector2 Direcao(float graus) => new Vector2(Mathf.Sin(graus * Mathf.Deg2Rad), Mathf.Cos(graus * Mathf.Deg2Rad));

        // ---------------------------------------------------------------- angulo

        [Test]
        public void Vaga_zero_fica_em_cima_e_as_outras_seguem_no_sentido_horario()
        {
            Assert.AreEqual(0, _roda.SlotAt(Vector2.up));
            Assert.AreEqual(1, _roda.SlotAt(Vector2.right));
            Assert.AreEqual(2, _roda.SlotAt(Vector2.down));
            Assert.AreEqual(3, _roda.SlotAt(Vector2.left));
        }

        [Test]
        public void Cinco_vagas_repartem_o_circulo_em_setenta_e_dois_graus()
        {
            _roda.SlotCount = 5;

            for (int slot = 0; slot < 5; slot++)
            {
                Assert.AreEqual(slot * 72f, _roda.AngleOf(slot), 0.001f);
                Assert.AreEqual(slot, _roda.SlotAt(Direcao(slot * 72f)), $"A vaga {slot} nao esta no proprio angulo.");
            }
        }

        [Test]
        public void Angulo_entre_duas_vagas_cai_na_mais_perto()
        {
            Assert.AreEqual(0, _roda.SlotAt(Direcao(44f)));
            Assert.AreEqual(1, _roda.SlotAt(Direcao(46f)));
        }

        // ---------------------------------------------------------------- ponteiro

        [Test]
        public void Abrir_zera_o_ponteiro_e_mantem_a_vaga()
        {
            _roda.SelectDirect(2);

            _roda.Open();

            Assert.IsTrue(_roda.IsOpen);
            Assert.AreEqual(Vector2.zero, _roda.Pointer);
            Assert.AreEqual(2, _roda.Selected);
        }

        [Test]
        public void Abrir_e_soltar_sem_mirar_mantem_o_sinal()
        {
            _roda.SelectDirect(3);
            _roda.Open();

            _roda.Point(new Vector2(0.1f, 0f));    // dentro da zona morta
            _roda.Close();

            Assert.AreEqual(3, _roda.Selected, "Trocar de sinal sem querer e o pior defeito desta roda.");
            Assert.IsFalse(_roda.IsOpen);
        }

        [Test]
        public void Mirar_alem_da_zona_morta_troca_a_vaga()
        {
            _roda.Open();

            _roda.Point(Vector2.right);

            Assert.AreEqual(1, _roda.Selected);
        }

        [Test]
        public void Ponteiro_fica_preso_ao_circulo()
        {
            _roda.Open();

            _roda.Point(new Vector2(40f, 0f));

            Assert.AreEqual(1f, _roda.Pointer.magnitude, 0.001f,
                "Mouse anda em pixels: sem prender, voltar ao centro levaria o mesmo caminho de volta.");
            Assert.AreEqual(1, _roda.Selected);
        }

        [Test]
        public void Roda_fechada_ignora_o_ponteiro()
        {
            _roda.SelectDirect(2);

            _roda.Point(Vector2.right);

            Assert.AreEqual(2, _roda.Selected, "Mexer o mouse na luta nao pode trocar de sinal.");
            Assert.AreEqual(Vector2.zero, _roda.Pointer);
        }

        // ------------------------------------------------------------ teclas diretas

        [Test]
        public void Tecla_direta_escolhe_a_vaga_com_a_roda_fechada()
        {
            Assert.IsTrue(_roda.SelectDirect(2));

            Assert.AreEqual(2, _roda.Selected);
            Assert.IsFalse(_roda.IsOpen, "A tecla direta nao abre a roda.");
        }

        [Test]
        public void Tecla_de_vaga_que_a_escola_nao_tem_nao_muda_nada()
        {
            _roda.SelectDirect(1);

            Assert.IsFalse(_roda.SelectDirect(4));
            Assert.IsFalse(_roda.SelectDirect(-1));
            Assert.AreEqual(1, _roda.Selected);
        }

        // ------------------------------------------------------------------ vagas

        [Test]
        public void Perder_vagas_nunca_deixa_a_selecao_fora_da_faixa()
        {
            _roda.SelectDirect(3);

            _roda.SlotCount = 2;

            Assert.AreEqual(1, _roda.Selected);
        }

        [Test]
        public void Escola_sem_habilidade_nenhuma_nao_quebra()
        {
            _roda.SlotCount = 0;
            _roda.Open();

            Assert.DoesNotThrow(() => _roda.Point(Vector2.right));
            Assert.AreEqual(0, _roda.Selected);
            Assert.IsFalse(_roda.SelectDirect(0));
        }
    }
}
