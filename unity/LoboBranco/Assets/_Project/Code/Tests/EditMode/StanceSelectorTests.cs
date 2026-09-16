using LoboBranco.Combat;
using LoboBranco.Player;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// A troca de postura do docs/03 secao 4.
    ///
    /// O que estes testes protegem nao e a roda, e o custo: a postura nova so vale depois
    /// de 0,25 s. Se a troca virar instantanea, escolher postura deixa de ser aposta e a
    /// camada 2 do combate desaparece sem ninguem notar, porque nada quebra.
    /// </summary>
    public sealed class StanceSelectorTests
    {
        const float Troca = 0.25f;
        const float Passo = 1f / 60f;

        static StanceSelector Seletor(Stance inicial = Stance.Fast)
            => new StanceSelector(Troca, inicial);

        static void Esperar(StanceSelector seletor, float segundos)
        {
            // Arredondar, e nao truncar: 0,25 dividido por 1/60 da 14,999... em float, e
            // truncar comeria um quadro e faria a troca nunca terminar no teste.
            int passos = Mathf.RoundToInt(segundos / Passo);

            for (int i = 0; i < passos; i++)
                seletor.Tick(Passo);
        }

        [Test]
        public void Comeca_na_postura_pedida()
        {
            StanceSelector seletor = Seletor(Stance.Strong);

            Assert.AreEqual(Stance.Strong, seletor.Current);
            Assert.IsFalse(seletor.IsSwitching);
        }

        [Test]
        public void A_postura_nova_so_vale_quando_a_troca_termina()
        {
            StanceSelector seletor = Seletor(Stance.Fast);

            seletor.Request(Stance.Strong);

            Assert.AreEqual(Stance.Fast, seletor.Current, "Durante a troca, quem vale e a postura velha.");
            Assert.AreEqual(Stance.Strong, seletor.Pending);

            Esperar(seletor, Troca - Passo * 2f);
            Assert.AreEqual(Stance.Fast, seletor.Current, "Faltando dois quadros, ainda e a velha.");

            Esperar(seletor, Passo * 3f);
            Assert.AreEqual(Stance.Strong, seletor.Current);
            Assert.IsFalse(seletor.IsSwitching);
        }

        [Test]
        public void Pedir_a_postura_que_ja_vale_nao_faz_nada()
        {
            StanceSelector seletor = Seletor(Stance.Fast);

            Assert.IsFalse(seletor.Request(Stance.Fast));
            Assert.IsFalse(seletor.IsSwitching);
        }

        [Test]
        public void A_roda_anda_nas_tres_posturas_e_volta_ao_comeco()
        {
            StanceSelector seletor = Seletor(Stance.Strong);

            seletor.Cycle(1);
            Esperar(seletor, Troca + Passo);
            Assert.AreEqual(Stance.Fast, seletor.Current);

            seletor.Cycle(1);
            Esperar(seletor, Troca + Passo);
            Assert.AreEqual(Stance.Group, seletor.Current);

            seletor.Cycle(1);
            Esperar(seletor, Troca + Passo);
            Assert.AreEqual(Stance.Strong, seletor.Current, "Depois da ultima, volta para a primeira.");
        }

        [Test]
        public void A_roda_anda_para_tras()
        {
            StanceSelector seletor = Seletor(Stance.Strong);

            seletor.Cycle(-1);
            Esperar(seletor, Troca + Passo);

            Assert.AreEqual(Stance.Group, seletor.Current);
        }

        /// <summary>
        /// Duas voltas rapidas da roda andam dois passos. Contar a partir da postura que
        /// ja esta a caminho e o que faz a roda responder como a mao espera.
        /// </summary>
        [Test]
        public void Duas_voltas_seguidas_andam_dois_passos()
        {
            StanceSelector seletor = Seletor(Stance.Strong);

            seletor.Cycle(1);
            seletor.Cycle(1);
            Esperar(seletor, Troca + Passo);

            Assert.AreEqual(Stance.Group, seletor.Current);
        }

        [Test]
        public void Desistir_no_meio_mantem_a_postura_atual()
        {
            StanceSelector seletor = Seletor(Stance.Fast);

            seletor.Request(Stance.Group);
            Esperar(seletor, Troca * 0.5f);
            seletor.Cancel();
            Esperar(seletor, Troca + Passo);

            Assert.AreEqual(Stance.Fast, seletor.Current);
            Assert.AreEqual(Stance.Fast, seletor.Pending);
        }

        /// <summary>
        /// A regra de quem pode trocar mora no <see cref="PlayerStateRules"/>, junto da
        /// regra de ouro, e nao espalhada. Trocar durante um golpe e o unico caso proibido
        /// pelo docs/03 secao 4.
        /// </summary>
        [Test]
        public void Nao_se_troca_de_postura_no_meio_de_uma_acao_comprometida()
        {
            Assert.IsFalse(PlayerStateRules.CanSwitchStance(committed: true));
            Assert.IsTrue(PlayerStateRules.CanSwitchStance(committed: false));
        }
    }
}
