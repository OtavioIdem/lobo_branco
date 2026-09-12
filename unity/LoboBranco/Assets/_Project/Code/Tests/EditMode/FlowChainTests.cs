using LoboBranco.Combat;
using NUnit.Framework;

namespace LoboBranco.Tests
{
    /// <summary>
    /// A Corrente de Fluxo do docs/03 secao 6.
    ///
    /// O teste que mais importa aqui e o de perder a corrente: o sistema inteiro se
    /// justifica por nao punir quem atacou tarde, entao a corrente reiniciar em um elo, e
    /// nao em zero nem em coisa pior, e a regra de design escrita como assercao.
    ///
    /// Passos fixos de 1/60 em vez de tempo real, pelo mesmo motivo dos outros testes de
    /// linha do tempo: framerate em batchmode e erratico e nao pode decidir resultado.
    /// </summary>
    public sealed class FlowChainTests
    {
        const float Janela = 0.22f;
        const float Passo = 1f / 60f;

        static FlowChain Corrente() => new FlowChain(Janela);

        /// <summary>Faz o tempo passar em passos fixos, como o loop de jogo faria.</summary>
        static void Esperar(FlowChain corrente, float segundos)
        {
            int passos = (int)(segundos / Passo);

            for (int i = 0; i < passos; i++)
                corrente.Tick(Passo);
        }

        [Test]
        public void O_primeiro_golpe_vale_um_elo()
        {
            FlowChain corrente = Corrente();

            Assert.AreEqual(1, corrente.Begin());
        }

        [Test]
        public void Encadear_dentro_da_janela_soma_elo()
        {
            FlowChain corrente = Corrente();

            corrente.Begin();
            corrente.Open();
            Esperar(corrente, Janela * 0.5f);

            Assert.AreEqual(2, corrente.Begin());
        }

        [Test]
        public void Cinco_golpes_seguidos_dao_cinco_elos()
        {
            FlowChain corrente = Corrente();

            corrente.Begin();

            for (int i = 0; i < 4; i++)
            {
                corrente.Open();
                Esperar(corrente, Janela * 0.5f);
                corrente.Begin();
            }

            Assert.AreEqual(5, corrente.Links);
        }

        /// <summary>
        /// A regra central: atacar tarde nao pune, so recomeca. Se algum dia isto virar
        /// zero, ou negativo, ou qualquer castigo, o pilar do docs/03 secao 6 foi perdido.
        /// </summary>
        [Test]
        public void Atacar_fora_da_janela_reinicia_em_um_elo_e_nao_pune()
        {
            FlowChain corrente = Corrente();

            corrente.Begin();
            corrente.Begin();
            corrente.Open();
            Esperar(corrente, Janela * 2f);

            Assert.AreEqual(0, corrente.Links, "A corrente expira sozinha.");
            Assert.AreEqual(1, corrente.Begin(), "O golpe atrasado comeca uma corrente nova.");
        }

        [Test]
        public void A_janela_fica_fechada_durante_o_golpe()
        {
            FlowChain corrente = Corrente();

            corrente.Open();
            corrente.Begin();

            Assert.IsFalse(corrente.WindowOpen, "Durante o golpe nao ha o que encadear.");
        }

        [Test]
        public void A_janela_so_abre_quando_o_golpe_termina()
        {
            FlowChain corrente = Corrente();

            corrente.Begin();
            Esperar(corrente, 1f);

            Assert.IsFalse(corrente.WindowOpen);

            corrente.Open();

            Assert.IsTrue(corrente.WindowOpen);
        }

        [Test]
        public void Golpe_interrompido_corta_a_corrente()
        {
            FlowChain corrente = Corrente();

            corrente.Begin();
            corrente.Open();
            corrente.Begin();
            corrente.Break();

            Assert.AreEqual(0, corrente.Links);
            Assert.IsFalse(corrente.WindowOpen);
            Assert.AreEqual(1, corrente.Begin());
        }

        [Test]
        public void A_janela_dura_o_que_o_asset_manda()
        {
            FlowChain corrente = Corrente();

            corrente.Begin();
            corrente.Open();
            Esperar(corrente, Janela - Passo * 2f);

            Assert.IsTrue(corrente.WindowOpen, "Faltando dois quadros, ainda encadeia.");

            Esperar(corrente, Passo * 3f);

            Assert.IsFalse(corrente.WindowOpen, "Passada a janela, nao encadeia mais.");
        }
    }
}
