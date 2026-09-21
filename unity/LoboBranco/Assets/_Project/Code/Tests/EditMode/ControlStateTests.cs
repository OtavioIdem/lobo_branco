using LoboBranco.Combat;
using NUnit.Framework;

namespace LoboBranco.Tests
{
    /// <summary>
    /// Atordoar, derrubar e lentificar (tarefa 1.18a).
    ///
    /// O que quebra em silencio aqui e a reaplicacao. Se dois Aards somassem, dois bruxos em coop
    /// deixariam uma criatura atordoada para sempre, e ninguem ligaria o inimigo que nunca ataca
    /// a uma regra de soma. Se uma lentidao fraca substituisse a forte, o Yrden de um jogador
    /// seria anulado pela poeira do outro.
    /// </summary>
    public sealed class ControlStateTests
    {
        [Test]
        public void Sem_controle_nada_esta_ativo()
        {
            var estado = new ControlState();

            Assert.IsFalse(estado.IsIncapacitated(0d));
            Assert.AreEqual(ControlKind.None, estado.Incapacitation(0d));
            Assert.AreEqual(0f, estado.SlowAt(0d));
            Assert.AreEqual(0f, estado.IncapacitatedRemaining(0d));
        }

        [Test]
        public void Atordoamento_vale_ate_o_instante_e_para_de_valer_nele()
        {
            var estado = new ControlState();
            estado.Stun(10d, 1.5f);

            Assert.IsTrue(estado.IsStunned(11.49d));
            Assert.IsFalse(estado.IsStunned(11.5d));
        }

        /// <summary>Dois Aards seguidos nao podem somar: a criatura voltaria a agir so depois dos dois.</summary>
        [Test]
        public void Atordoar_de_novo_nao_soma()
        {
            var estado = new ControlState();
            estado.Stun(10d, 1.5f);
            estado.Stun(10.5d, 1.5f);

            Assert.AreEqual(12d, estado.StunnedUntil, 1e-9d);
        }

        [Test]
        public void Atordoamento_mais_curto_que_o_atual_nao_muda_nada()
        {
            var estado = new ControlState();
            estado.Stun(10d, 3f);

            Assert.IsFalse(estado.Stun(10.5d, 1f), "Nada mudou, e o host nao escreve na rede.");
            Assert.AreEqual(13d, estado.StunnedUntil, 1e-9d);
        }

        [Test]
        public void Tempo_zero_ou_negativo_nao_aplica()
        {
            var estado = new ControlState();

            Assert.IsFalse(estado.Stun(10d, 0f));
            Assert.IsFalse(estado.KnockDown(10d, -1f));
            Assert.IsFalse(estado.Slow(10d, 0.6f, 0f));
        }

        [Test]
        public void Derrubada_e_atordoamento_contam_separados_e_vale_o_maior()
        {
            var estado = new ControlState();
            estado.Stun(10d, 1.5f);
            estado.KnockDown(10d, 3f);

            Assert.AreEqual(3f, estado.IncapacitatedRemaining(10d), 1e-5f);
            Assert.IsTrue(estado.IsIncapacitated(12d), "O atordoamento acabou, e a derrubada continua.");
            Assert.IsFalse(estado.IsIncapacitated(13d));
        }

        [Test]
        public void Derrubada_vence_atordoamento_no_tipo()
        {
            var estado = new ControlState();
            estado.Stun(10d, 5f);
            estado.KnockDown(10d, 1f);

            Assert.AreEqual(ControlKind.KnockedDown, estado.Incapacitation(10.5d));
            Assert.AreEqual(ControlKind.Stunned, estado.Incapacitation(12d));
        }

        // ----------------------------------------------------------- lentidao

        [Test]
        public void Lentidao_mais_forte_substitui_a_mais_fraca()
        {
            var estado = new ControlState();
            estado.Slow(10d, 0.3f, 10f);

            Assert.IsTrue(estado.Slow(11d, 0.6f, 2f));
            Assert.AreEqual(0.6f, estado.SlowAt(12d), 1e-5f);
        }

        [Test]
        public void Lentidao_mais_fraca_nao_entra_enquanto_a_forte_vale()
        {
            var estado = new ControlState();
            estado.Slow(10d, 0.6f, 4f);

            Assert.IsFalse(estado.Slow(11d, 0.3f, 20f));
            Assert.AreEqual(0.6f, estado.SlowAt(12d), 1e-5f);
        }

        [Test]
        public void Lentidao_mais_fraca_entra_depois_que_a_forte_acaba()
        {
            var estado = new ControlState();
            estado.Slow(10d, 0.6f, 1f);

            Assert.IsTrue(estado.Slow(12d, 0.3f, 5f));
            Assert.AreEqual(0.3f, estado.SlowAt(13d), 1e-5f);
        }

        [Test]
        public void A_mesma_lentidao_de_novo_renova_o_fim()
        {
            var estado = new ControlState();
            estado.Slow(10d, 0.6f, 4f);

            Assert.IsTrue(estado.Slow(13d, 0.6f, 4f));
            Assert.AreEqual(17d, estado.SlowedUntil, 1e-9d);
        }

        [Test]
        public void Lentidao_acima_de_um_vira_parada_total_e_nao_velocidade_negativa()
        {
            var estado = new ControlState();
            estado.Slow(10d, 1.8f, 4f);

            Assert.AreEqual(1f, estado.SlowAt(11d));
        }

        [Test]
        public void Lentidao_acaba_no_instante()
        {
            var estado = new ControlState();
            estado.Slow(10d, 0.6f, 4f);

            Assert.AreEqual(0f, estado.SlowAt(14d));
        }

        // ------------------------------------------------------------ relogio

        [Test]
        public void Trocar_de_relogio_preserva_quanto_falta()
        {
            var estado = new ControlState();
            estado.Stun(100d, 1.5f);
            estado.Slow(100d, 0.6f, 4f);

            estado.Rebase(5d - 100d);

            Assert.AreEqual(1.5f, estado.IncapacitatedRemaining(5d), 1e-5f);
            Assert.AreEqual(0.6f, estado.SlowAt(8.9d), 1e-5f);
            Assert.AreEqual(0f, estado.SlowAt(9d));
        }

        [Test]
        public void Limpar_encerra_tudo()
        {
            var estado = new ControlState();
            estado.Stun(10d, 5f);
            estado.KnockDown(10d, 5f);
            estado.Slow(10d, 0.6f, 5f);

            estado.Clear();

            Assert.IsFalse(estado.IsIncapacitated(10d));
            Assert.AreEqual(0f, estado.SlowAt(10d));
        }
    }
}
