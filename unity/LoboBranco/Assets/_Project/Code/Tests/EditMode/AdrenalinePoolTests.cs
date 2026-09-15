using LoboBranco.Combat;
using NUnit.Framework;

namespace LoboBranco.Tests
{
    /// <summary>
    /// As tres cargas de Adrenalina do docs/03 secao 7, e o ganho por corrente de Fluxo
    /// alta do docs/03 secao 6.
    ///
    /// O que estes testes guardam e a diferenca entre Adrenalina e Vigor: Vigor volta
    /// sozinho, Adrenalina nao. Se algum dia ela regenerar por tempo, gastar uma carga
    /// deixa de ser gastar uma luta que ja aconteceu, e o recurso vira outro recurso.
    /// </summary>
    public sealed class AdrenalinePoolTests
    {
        const int Maximo = 3;
        const int PrimeiroElo = 5;
        const int EloPorCarga = 2;

        static AdrenalinePool Pool() => new AdrenalinePool(Maximo, PrimeiroElo, EloPorCarga);

        [Test]
        public void Comeca_vazia()
        {
            Assert.AreEqual(0, Pool().Charges);
        }

        [Test]
        public void Ganhar_enche_de_uma_em_uma_ate_o_teto()
        {
            AdrenalinePool pool = Pool();

            for (int i = 0; i < Maximo; i++)
                Assert.IsTrue(pool.Gain(), $"A carga {i + 1} deveria entrar.");

            Assert.AreEqual(Maximo, pool.Charges);
            Assert.IsFalse(pool.Gain(), "Cheia, o excedente nao fica guardado.");
            Assert.AreEqual(Maximo, pool.Charges);
        }

        [Test]
        public void Gastar_tira_as_cargas_do_custo()
        {
            AdrenalinePool pool = Pool();
            pool.Gain(3);

            Assert.IsTrue(pool.TrySpend(2), "Segundo suspiro custa duas.");
            Assert.AreEqual(1, pool.Charges);
        }

        /// <summary>Meia finalizacao nao existe: ou paga inteiro, ou nao acontece.</summary>
        [Test]
        public void Sem_cargas_suficientes_nao_gasta_nada()
        {
            AdrenalinePool pool = Pool();
            pool.Gain();

            Assert.IsFalse(pool.TrySpend(2));
            Assert.AreEqual(1, pool.Charges);
        }

        /// <summary>
        /// A partir do quinto elo, uma carga a cada dois elos: 5, 7 e 9. E o que liga o
        /// recurso a maestria, porque encadear cinco golpes nao acontece por acaso.
        /// </summary>
        [Test]
        public void Corrente_alta_gera_carga_a_cada_dois_elos()
        {
            AdrenalinePool pool = Pool();

            Assert.IsFalse(pool.NoteFlowLinks(4), "Quatro elos ainda nao rendem nada.");
            Assert.IsTrue(pool.NoteFlowLinks(5), "O quinto elo rende a primeira.");
            Assert.IsFalse(pool.NoteFlowLinks(6), "O sexto nao, porque e a cada dois.");
            Assert.IsTrue(pool.NoteFlowLinks(7));
            Assert.IsFalse(pool.NoteFlowLinks(8));
            Assert.IsTrue(pool.NoteFlowLinks(9));

            Assert.AreEqual(Maximo, pool.Charges);
        }

        [Test]
        public void Corrente_alta_com_a_adrenalina_cheia_nao_estoura_o_teto()
        {
            AdrenalinePool pool = Pool();
            pool.Gain(3);

            Assert.IsFalse(pool.NoteFlowLinks(5));
            Assert.AreEqual(Maximo, pool.Charges);
        }

        [Test]
        public void Cair_leva_a_adrenalina_junto()
        {
            AdrenalinePool pool = Pool();
            pool.Gain(3);

            pool.Clear();

            Assert.AreEqual(0, pool.Charges);
        }
    }
}
