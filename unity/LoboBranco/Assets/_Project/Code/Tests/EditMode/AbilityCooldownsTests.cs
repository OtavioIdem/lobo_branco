using LoboBranco.Combat;
using NUnit.Framework;

namespace LoboBranco.Tests
{
    /// <summary>
    /// A recarga guardada como instante (tarefa 1.32, tech/adr/0011).
    ///
    /// O que quebra em silencio aqui e a troca de relogio. Uma recarga que sobrevive errada a
    /// mudanca do relogio local para o do servidor nao gera erro nenhum: o sinal so para de
    /// sair por alguns minutos, e ninguem liga isso a ter aberto uma sessao.
    /// </summary>
    public sealed class AbilityCooldownsTests
    {
        const int Vagas = 5;

        [Test]
        public void Vaga_nunca_usada_esta_pronta()
        {
            var recarga = new AbilityCooldowns(Vagas);

            Assert.IsTrue(recarga.IsReady(0, 0d));
            Assert.IsTrue(recarga.IsReady(4, 1234.5d));
            Assert.AreEqual(0f, recarga.Remaining(0, 0d));
        }

        [Test]
        public void Recarga_armada_so_volta_no_instante_certo()
        {
            var recarga = new AbilityCooldowns(Vagas);
            recarga.Start(0, 10d, 4f);

            Assert.IsFalse(recarga.IsReady(0, 13.99d));
            Assert.IsTrue(recarga.IsReady(0, 14d));
        }

        [Test]
        public void Quanto_falta_anda_com_o_relogio_e_nunca_fica_negativo()
        {
            var recarga = new AbilityCooldowns(Vagas);
            recarga.Start(0, 10d, 4f);

            Assert.AreEqual(3f, recarga.Remaining(0, 11d), 1e-5f);
            Assert.AreEqual(0f, recarga.Remaining(0, 20d));
        }

        [Test]
        public void Recarga_de_uma_vaga_nao_mexe_nas_outras()
        {
            var recarga = new AbilityCooldowns(Vagas);
            recarga.Start(2, 10d, 4f);

            Assert.IsTrue(recarga.IsReady(0, 11d));
            Assert.IsTrue(recarga.IsReady(1, 11d));
            Assert.IsFalse(recarga.IsReady(2, 11d));
        }

        [Test]
        public void Recarga_nova_substitui_a_anterior()
        {
            var recarga = new AbilityCooldowns(Vagas);
            recarga.Start(0, 0d, 4f);
            recarga.Start(0, 1d, 1f);

            Assert.AreEqual(2d, recarga.ReadyAt(0), 1e-9d);
        }

        [Test]
        public void Recarga_negativa_nao_adianta_o_relogio()
        {
            var recarga = new AbilityCooldowns(Vagas);
            recarga.Start(0, 10d, -3f);

            Assert.AreEqual(10d, recarga.ReadyAt(0), 1e-9d);
            Assert.IsTrue(recarga.IsReady(0, 10d));
        }

        /// <summary>Uma vaga que nao existe e uma habilidade que a escola nao tem. "Pronta" mandaria o pedido ao host.</summary>
        [Test]
        public void Vaga_fora_da_faixa_nunca_esta_pronta_e_nao_arma()
        {
            var recarga = new AbilityCooldowns(Vagas);

            Assert.IsFalse(recarga.IsReady(-1, 100d));
            Assert.IsFalse(recarga.IsReady(Vagas, 100d));

            Assert.DoesNotThrow(() => recarga.Start(Vagas + 2, 0d, 4f));
            Assert.DoesNotThrow(() => recarga.Overwrite(-1, 50d));
        }

        /// <summary>
        /// A sessao comeca com o personagem ja existindo: o relogio local estava em 100 e o do
        /// servidor esta em 5. O que faltava continua faltando, e o que estava pronto continua.
        /// </summary>
        [Test]
        public void Trocar_de_relogio_preserva_quanto_falta()
        {
            var recarga = new AbilityCooldowns(Vagas);
            recarga.Start(0, 100d, 4f);

            recarga.Rebase(5d - 100d);

            Assert.AreEqual(4f, recarga.Remaining(0, 5d), 1e-5f);
            Assert.IsTrue(recarga.IsReady(1, 5d), "Vaga que estava pronta nao pode ganhar recarga na troca.");
        }

        [Test]
        public void Limpar_deixa_todas_as_vagas_prontas()
        {
            var recarga = new AbilityCooldowns(Vagas);
            recarga.Start(0, 10d, 4f);
            recarga.Start(3, 10d, 8f);

            recarga.Clear();

            Assert.IsTrue(recarga.IsReady(0, 10d));
            Assert.IsTrue(recarga.IsReady(3, 10d));
        }

        // -------------------------------------------------------------- rede

        /// <summary>O que o host escreve e o que o dono le tem que ser o mesmo numero, vaga por vaga.</summary>
        [Test]
        public void Copia_replicada_leva_e_traz_todas_as_vagas()
        {
            var host = new AbilityCooldowns(AbilityReadyTimes.Capacity);
            for (int i = 0; i < AbilityReadyTimes.Capacity; i++)
                host.Start(i, 100d, i + 1f);

            var dono = new AbilityCooldowns(AbilityReadyTimes.Capacity);
            AbilityReadyTimes.From(host).CopyTo(dono);

            for (int i = 0; i < AbilityReadyTimes.Capacity; i++)
                Assert.AreEqual(host.ReadyAt(i), dono.ReadyAt(i), 1e-9d, $"Vaga {i}.");
        }

        [Test]
        public void Copia_replicada_ignora_vaga_fora_da_faixa()
        {
            var times = new AbilityReadyTimes();
            times[AbilityReadyTimes.Capacity] = 99d;

            Assert.AreEqual(0d, times[AbilityReadyTimes.Capacity]);
            Assert.AreEqual(0d, times[-1]);
        }
    }
}
