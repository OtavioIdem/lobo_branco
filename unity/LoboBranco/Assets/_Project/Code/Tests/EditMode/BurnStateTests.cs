using LoboBranco.Combat;
using NUnit.Framework;

namespace LoboBranco.Tests
{
    /// <summary>
    /// A Queimadura como regra pura (tarefa 1.18d): 4 por segundo por 5 s, em tiques, e reaplicar
    /// sem somar. O relogio e passado a mao, entao nenhum teste espera.
    ///
    /// O defeito que importa e o de soma: dois bruxos com fogo no mesmo alvo nao podem dobrar a
    /// queimadura, e o tique final nao pode sumir por arredondamento.
    /// </summary>
    public sealed class BurnStateTests
    {
        BurnState _queima;

        [SetUp]
        public void SetUp() => _queima = default;

        int TiquesAte(double agora) => _queima.ConsumeTicks(agora);

        [Test]
        public void Queimadura_do_documento_da_cinco_tiques_de_quatro()
        {
            Assert.IsTrue(_queima.Ignite(10d, 4f, 5f, 1f));

            Assert.AreEqual(4f, _queima.DamagePerTick);
            Assert.AreEqual(5, TiquesAte(100d), "docs/03 secao 8: 4 por segundo por 5 s sao 20 de dano.");
        }

        [Test]
        public void Primeiro_tique_vem_um_intervalo_depois_de_acender()
        {
            _queima.Ignite(10d, 4f, 5f, 1f);

            Assert.AreEqual(0, TiquesAte(10.5d), "O dano do instante e o do proprio sinal.");
            Assert.AreEqual(1, TiquesAte(11d));
        }

        [Test]
        public void Quadro_longo_entrega_todos_os_tiques_vencidos()
        {
            _queima.Ignite(0d, 4f, 5f, 1f);

            Assert.AreEqual(3, TiquesAte(3.2d), "Queimar nao pode depender de framerate.");
            Assert.AreEqual(2, TiquesAte(9d));
        }

        [Test]
        public void Intervalo_somado_cinco_vezes_nao_perde_o_ultimo_tique()
        {
            _queima.Ignite(0.1d, 4f, 0.5f, 0.1f);

            Assert.AreEqual(5, TiquesAte(10d));
        }

        /// <summary>
        /// O tique agendado para o instante exato em que alguem pergunta tem que vencer. Quem
        /// pergunta calcula <c>T+2</c> de uma vez, e o compasso chegou la somando <c>T+1+1</c>: em
        /// ponto flutuante os dois podem diferir no ultimo bit. Sem folga, a queimadura tira 4 em vez
        /// de 8 dependendo de que horas a partida comecou, e o teste passa ou falha por sorte.
        /// </summary>
        [Test]
        public void Tique_no_instante_exato_vence_com_relogio_quebrado()
        {
            const double Relogio = 12.3456789d;
            _queima.Ignite(Relogio, 4f, 5f, 1f);

            Assert.AreEqual(2, TiquesAte(Relogio + 2d));
        }

        [Test]
        public void Acaba_no_fim_da_duracao()
        {
            _queima.Ignite(0d, 4f, 5f, 1f);

            Assert.IsTrue(_queima.IsBurning(4.9d));
            Assert.IsFalse(_queima.IsBurning(5d));
        }

        [Test]
        public void Mesma_queimadura_de_novo_renova_sem_somar_e_sem_reiniciar_o_compasso()
        {
            _queima.Ignite(0d, 4f, 5f, 1f);
            Assert.AreEqual(2, TiquesAte(2.5d));

            Assert.IsTrue(_queima.Ignite(2.5d, 4f, 5f, 1f), "Renovar a mesma forca estende o fim.");

            Assert.AreEqual(4f, _queima.DamagePerTick, "Dois bruxos com fogo nao dobram a queimadura.");
            Assert.AreEqual(1, TiquesAte(3d), "O compasso segue no segundo cheio.");
            Assert.AreEqual(4, TiquesAte(100d), "Tiques em 4, 5, 6 e 7, e o fim renovado em 7,5.");
        }

        [Test]
        public void Queimadura_mais_fraca_nao_entra_enquanto_a_forte_queima()
        {
            _queima.Ignite(0d, 6f, 5f, 1f);

            Assert.IsFalse(_queima.Ignite(1d, 4f, 5f, 1f));
            Assert.AreEqual(6f, _queima.DamagePerTick);
        }

        [Test]
        public void Queimadura_mais_forte_substitui_a_fraca()
        {
            _queima.Ignite(0d, 4f, 5f, 1f);

            Assert.IsTrue(_queima.Ignite(1d, 6f, 2f, 1f));
            Assert.AreEqual(6f, _queima.DamagePerTick);
            Assert.IsFalse(_queima.IsBurning(3d), "A forte manda, inclusive na duracao: o resto da fraca se perde.");
        }

        [Test]
        public void Queimadura_apagada_acende_de_novo_com_compasso_novo()
        {
            _queima.Ignite(0d, 4f, 5f, 1f);
            TiquesAte(10d);

            Assert.IsTrue(_queima.Ignite(20d, 4f, 5f, 1f));
            Assert.AreEqual(0, TiquesAte(20.5d));
            Assert.AreEqual(1, TiquesAte(21d));
        }

        [Test]
        public void Numeros_zerados_nao_acendem()
        {
            Assert.IsFalse(_queima.Ignite(0d, 0f, 5f, 1f));
            Assert.IsFalse(_queima.Ignite(0d, 4f, 0f, 1f));
            Assert.IsFalse(_queima.Ignite(0d, 4f, 5f, 0f));
            Assert.AreEqual(0, TiquesAte(100d));
        }

        [Test]
        public void Apagar_encerra_os_tiques()
        {
            _queima.Ignite(0d, 4f, 5f, 1f);
            _queima.Clear();

            Assert.IsFalse(_queima.IsBurning(1d));
            Assert.AreEqual(0, TiquesAte(100d));
        }
    }
}
