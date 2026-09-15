using LoboBranco.Combat;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// O Vigor do docs/03 secao 7: 100 de base, 18/s fora de combate, 6/s em combate, e
    /// nada durante 1,5 s depois de cada gasto.
    ///
    /// O teste que importa e o do atraso. Sem os 1,5 s de silencio o recurso vira um
    /// contador que sempre volta, e gastar deixa de ser escolha: sinal e defesa saem do
    /// mesmo bolso, e e a janela sem rede de protecao que torna o bolso apertado.
    /// </summary>
    public sealed class StaminaPoolTests
    {
        const float Atraso = 1.5f;
        const float MemoriaDeCombate = 5f;
        const float Maximo = 100f;
        const float ForaDeCombate = 18f;
        const float EmCombate = 6f;
        const float Passo = 1f / 60f;

        static StaminaPool Vigor()
        {
            var pool = new StaminaPool(Atraso, MemoriaDeCombate)
            {
                RegenPerSecond = ForaDeCombate,
                CombatRegenPerSecond = EmCombate,
            };

            pool.Reset(Maximo);
            return pool;
        }

        static void Esperar(StaminaPool pool, float segundos)
        {
            int passos = Mathf.RoundToInt(segundos / Passo);

            for (int i = 0; i < passos; i++)
                pool.Tick(Passo);
        }

        [Test]
        public void Comeca_cheio()
        {
            StaminaPool pool = Vigor();

            Assert.AreEqual(Maximo, pool.Current, 0.001f);
            Assert.AreEqual(1f, pool.Fraction, 0.001f);
        }

        [Test]
        public void Gastar_tira_o_custo()
        {
            StaminaPool pool = Vigor();

            Assert.IsTrue(pool.TrySpend(8f));
            Assert.AreEqual(92f, pool.Current, 0.001f);
        }

        /// <summary>
        /// Meio gasto seria um bruxo que comeca o golpe e nao termina. Ou paga inteiro,
        /// ou nao acontece.
        /// </summary>
        [Test]
        public void Sem_vigor_nao_gasta_nem_um_pouco()
        {
            StaminaPool pool = Vigor();
            pool.TrySpend(95f);

            Assert.IsFalse(pool.TrySpend(8f));
            Assert.AreEqual(5f, pool.Current, 0.001f);
        }

        [Test]
        public void Depois_de_gastar_nao_regenera_por_um_segundo_e_meio()
        {
            StaminaPool pool = Vigor();
            pool.TrySpend(50f);

            Esperar(pool, Atraso - Passo * 2f);

            Assert.AreEqual(50f, pool.Current, 0.001f, "Dentro do atraso, o vigor nao anda.");

            Esperar(pool, Passo * 4f);

            Assert.Greater(pool.Current, 50f, "Passado o atraso, ele volta a subir.");
        }

        /// <summary>
        /// Seis por segundo contra dezoito: a diferenca e o que faz uma luta longa pesar.
        /// </summary>
        [Test]
        public void Em_combate_regenera_menos_que_descansando()
        {
            // Gasto grande de proposito: medir perto do teto mediria a parede e nao a taxa.
            StaminaPool emCombate = Vigor();
            emCombate.TrySpend(90f);
            Esperar(emCombate, Atraso);

            float antes = emCombate.Current;
            Esperar(emCombate, 1f);
            float ganhoEmCombate = emCombate.Current - antes;

            StaminaPool descansando = Vigor();
            descansando.TrySpend(90f);
            Esperar(descansando, Atraso + MemoriaDeCombate);

            float antesDescansando = descansando.Current;
            Esperar(descansando, 1f);
            float ganhoDescansando = descansando.Current - antesDescansando;

            Assert.AreEqual(EmCombate, ganhoEmCombate, 0.5f);
            Assert.AreEqual(ForaDeCombate, ganhoDescansando, 0.5f);
        }

        [Test]
        public void Apanhar_conta_como_combate()
        {
            StaminaPool pool = Vigor();
            pool.TrySpend(50f);
            Esperar(pool, Atraso + MemoriaDeCombate);

            Assert.IsFalse(pool.InCombat, "Passou tempo demais sem nada acontecer.");

            pool.NoteCombat();

            Assert.IsTrue(pool.InCombat, "Levar um golpe e luta tanto quanto dar um.");
        }

        [Test]
        public void A_regeneracao_para_no_teto()
        {
            StaminaPool pool = Vigor();
            pool.TrySpend(5f);

            Esperar(pool, 30f);

            Assert.AreEqual(Maximo, pool.Current, 0.001f);
        }

        [Test]
        public void Sem_vigor_maximo_nada_e_pago()
        {
            var pool = new StaminaPool(Atraso, MemoriaDeCombate);
            pool.Reset(0f);

            Assert.IsTrue(pool.CanAfford(0f), "Acao de graca acontece mesmo sem vigor nenhum.");
            Assert.IsFalse(pool.CanAfford(1f));
        }
    }
}
