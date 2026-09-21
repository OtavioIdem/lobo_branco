using LoboBranco.AI;
using NUnit.Framework;

namespace LoboBranco.Tests
{
    /// <summary>
    /// Quem tem a vez de golpear (docs/07 secao 6).
    ///
    /// O que estes testes protegem nao e uma conta, e a legibilidade do combate. Um token
    /// que vaza deixa uma vaga bloqueada para sempre, e o sintoma e um encontro em que as
    /// criaturas param de atacar no meio da luta, sem nada no Console. Por isso quase
    /// metade deles e sobre devolver, e nao sobre conceder.
    ///
    /// Classe pura com identificadores inteiros: nenhuma cena, nenhum GameObject.
    /// </summary>
    public sealed class AttackTokenPoolTests
    {
        AttackTokenPool _tokens;

        const ulong Bruxo = 100;
        const ulong OutroBruxo = 200;

        const ulong Barghest1 = 1;
        const ulong Barghest2 = 2;
        const ulong Barghest3 = 3;

        [SetUp]
        public void SetUp()
        {
            _tokens = new AttackTokenPool { MaxPerTarget = 2 };
        }

        // -------------------------------------------------------------- conceder

        [Test]
        public void Os_dois_primeiros_recebem_a_vez()
        {
            Assert.IsTrue(_tokens.TryAcquire(Barghest1, Bruxo));
            Assert.IsTrue(_tokens.TryAcquire(Barghest2, Bruxo));
            Assert.AreEqual(2, _tokens.HoldersOf(Bruxo));
        }

        /// <summary>
        /// A regra inteira em uma linha. Sem ela, cinco barghests golpeiam no mesmo
        /// instante e nao ha esquiva que resolva cinco golpes de uma vez.
        /// </summary>
        [Test]
        public void O_terceiro_e_recusado()
        {
            _tokens.TryAcquire(Barghest1, Bruxo);
            _tokens.TryAcquire(Barghest2, Bruxo);

            Assert.IsFalse(_tokens.TryAcquire(Barghest3, Bruxo));
            Assert.AreEqual(2, _tokens.HoldersOf(Bruxo));
        }

        /// <summary>
        /// O teto e por alvo, e nao por encontro. Com teto global, o segundo jogador de
        /// uma sessao cooperativa nunca seria atacado.
        /// </summary>
        [Test]
        public void O_teto_vale_por_alvo_e_nao_pelo_encontro()
        {
            _tokens.TryAcquire(Barghest1, Bruxo);
            _tokens.TryAcquire(Barghest2, Bruxo);

            Assert.IsTrue(_tokens.TryAcquire(Barghest3, OutroBruxo),
                "o teto de um bruxo nao pode bloquear o outro");

            Assert.AreEqual(2, _tokens.HoldersOf(Bruxo));
            Assert.AreEqual(1, _tokens.HoldersOf(OutroBruxo));
        }

        [Test]
        public void Pedir_de_novo_pelo_mesmo_alvo_nao_conta_duas_vezes()
        {
            _tokens.TryAcquire(Barghest1, Bruxo);

            Assert.IsTrue(_tokens.TryAcquire(Barghest1, Bruxo));
            Assert.AreEqual(1, _tokens.HoldersOf(Bruxo));
        }

        /// <summary>
        /// Trocar de alvo tem que devolver o token antigo. Sem isso, uma criatura que muda
        /// de bruxo no meio da luta vaza uma permissao e a vaga do primeiro fica presa.
        /// </summary>
        [Test]
        public void Trocar_de_alvo_devolve_a_vaga_do_alvo_anterior()
        {
            _tokens.TryAcquire(Barghest1, Bruxo);
            _tokens.TryAcquire(Barghest1, OutroBruxo);

            Assert.AreEqual(0, _tokens.HoldersOf(Bruxo));
            Assert.AreEqual(1, _tokens.HoldersOf(OutroBruxo));
            Assert.AreEqual(OutroBruxo, _tokens.TargetOf(Barghest1));
        }

        [Test]
        public void Teto_menor_que_um_e_tratado_como_um()
        {
            _tokens.MaxPerTarget = 0;

            Assert.AreEqual(1, _tokens.MaxPerTarget);
            Assert.IsTrue(_tokens.TryAcquire(Barghest1, Bruxo));
            Assert.IsFalse(_tokens.TryAcquire(Barghest2, Bruxo));
        }

        // -------------------------------------------------------------- devolver

        [Test]
        public void Devolver_abre_vaga_para_quem_esperava()
        {
            _tokens.TryAcquire(Barghest1, Bruxo);
            _tokens.TryAcquire(Barghest2, Bruxo);

            Assert.IsTrue(_tokens.Release(Barghest1));
            Assert.IsTrue(_tokens.TryAcquire(Barghest3, Bruxo));
            Assert.AreEqual(2, _tokens.HoldersOf(Bruxo));
        }

        /// <summary>
        /// Devolver sem ter pegado e comum: quem encerra um golpe devolve sem perguntar se
        /// chegou a receber a vez.
        /// </summary>
        [Test]
        public void Devolver_sem_ter_pegado_nao_quebra_nada()
        {
            Assert.IsFalse(_tokens.Release(Barghest1));
            Assert.AreEqual(0, _tokens.HoldersOf(Bruxo));
        }

        [Test]
        public void Devolver_duas_vezes_nao_abre_vaga_a_mais()
        {
            _tokens.TryAcquire(Barghest1, Bruxo);
            _tokens.Release(Barghest1);
            _tokens.Release(Barghest1);

            _tokens.TryAcquire(Barghest2, Bruxo);
            _tokens.TryAcquire(Barghest3, Bruxo);

            Assert.AreEqual(2, _tokens.HoldersOf(Bruxo));
            Assert.IsFalse(_tokens.TryAcquire(Barghest1, Bruxo));
        }

        /// <summary>
        /// O alvo caiu ou saiu da sessao. Sem isto, as criaturas que batiam nele
        /// continuariam contando contra um alvo que nao existe mais.
        /// </summary>
        [Test]
        public void Alvo_que_cai_devolve_todas_as_vagas_dele()
        {
            _tokens.TryAcquire(Barghest1, Bruxo);
            _tokens.TryAcquire(Barghest2, Bruxo);
            _tokens.TryAcquire(Barghest3, OutroBruxo);

            _tokens.ReleaseAllFor(Bruxo);

            Assert.AreEqual(0, _tokens.HoldersOf(Bruxo));
            Assert.IsFalse(_tokens.Holds(Barghest1));
            Assert.IsFalse(_tokens.Holds(Barghest2));

            Assert.IsTrue(_tokens.Holds(Barghest3), "o outro bruxo nao podia ser afetado");
        }

        [Test]
        public void Alvo_sem_atacante_nenhum_aceita_ser_liberado()
        {
            Assert.DoesNotThrow(() => _tokens.ReleaseAllFor(Bruxo));
            Assert.AreEqual(0, _tokens.IssuedCount);
        }

        [Test]
        public void Limpar_devolve_tudo()
        {
            _tokens.TryAcquire(Barghest1, Bruxo);
            _tokens.TryAcquire(Barghest2, OutroBruxo);

            _tokens.Clear();

            Assert.AreEqual(0, _tokens.IssuedCount);
            Assert.AreEqual(0, _tokens.HoldersOf(Bruxo));
            Assert.AreEqual(0, _tokens.HoldersOf(OutroBruxo));
        }

        /// <summary>
        /// O ciclo real de um encontro longo: bater, devolver, esperar, bater de novo. Se
        /// alguma vaga vazasse, o terceiro pedido da ultima rodada seria recusado.
        /// </summary>
        [Test]
        public void Rodizio_longo_nao_vaza_vaga()
        {
            for (int rodada = 0; rodada < 20; rodada++)
            {
                Assert.IsTrue(_tokens.TryAcquire(Barghest1, Bruxo));
                Assert.IsTrue(_tokens.TryAcquire(Barghest2, Bruxo));
                Assert.IsFalse(_tokens.TryAcquire(Barghest3, Bruxo));

                _tokens.Release(Barghest1);
                _tokens.Release(Barghest2);
            }

            Assert.AreEqual(0, _tokens.IssuedCount);
            Assert.IsTrue(_tokens.TryAcquire(Barghest3, Bruxo));
        }
    }
}
