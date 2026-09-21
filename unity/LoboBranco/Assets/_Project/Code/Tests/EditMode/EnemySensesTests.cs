using LoboBranco.AI;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// O cone de visao, o circulo do ouvido e a memoria de quem caca (docs/07 secao 6).
    ///
    /// Roda em EditMode com posicoes escritas a mao e a linha de visao entrando por
    /// parametro. Nenhum GameObject, nenhum colisor, nenhuma fisica: e a razao de a
    /// percepcao ser uma classe pura em vez de um metodo dentro do componente.
    ///
    /// O que estes testes protegem e o comportamento que o jogador percebe como
    /// inteligencia ou como burrice, e que nunca aparece como erro no Console: contornar
    /// um grupo tem que funcionar, e quebrar a linha de visao nao pode desligar o inimigo
    /// na hora.
    /// </summary>
    public sealed class EnemySensesTests
    {
        EnemySenses _sentidos;

        static readonly Vector3 Origem = Vector3.zero;
        static readonly Vector3 Frente = Vector3.forward;

        const float Alcance = 18f;
        const float MeioAngulo = 60f;
        const float Ouvido = 6f;
        const float Memoria = 4f;

        [SetUp]
        public void SetUp()
        {
            _sentidos = new EnemySenses
            {
                SightRange = Alcance,
                SightHalfAngle = MeioAngulo,
                HearingRange = Ouvido,
                LoseTargetAfter = Memoria,
            };
        }

        // ------------------------------------------------------------- geometria

        [Test]
        public void Alvo_a_frente_e_dentro_do_alcance_e_percebido()
        {
            Assert.IsTrue(_sentidos.Perceives(Origem, Frente, new Vector3(0f, 0f, 12f), lineOfSightClear: true));
        }

        [Test]
        public void Alvo_alem_do_alcance_de_visao_nao_e_percebido()
        {
            Assert.IsFalse(_sentidos.Perceives(Origem, Frente, new Vector3(0f, 0f, Alcance + 1f), lineOfSightClear: true));
        }

        /// <summary>
        /// Contornar um grupo tem que funcionar. Sem esta regra, o cone nao significa nada
        /// e a posicao do jogador deixa de ser uma decisao.
        /// </summary>
        [Test]
        public void Alvo_fora_do_cone_e_longe_do_ouvido_nao_e_percebido()
        {
            var atras = new Vector3(0f, 0f, -12f);

            Assert.IsFalse(_sentidos.Perceives(Origem, Frente, atras, lineOfSightClear: true));
        }

        /// <summary>
        /// E a regra que impede o contorno de virar invisibilidade: colar pelas costas
        /// desperta, mesmo com a criatura olhando para o outro lado.
        /// </summary>
        [Test]
        public void Alvo_atras_mas_dentro_do_ouvido_e_percebido()
        {
            var colado = new Vector3(0f, 0f, -(Ouvido - 1f));

            Assert.IsTrue(_sentidos.Perceives(Origem, Frente, colado, lineOfSightClear: true));
        }

        [Test]
        public void Alvo_no_limite_do_cone_ainda_e_percebido()
        {
            // Exatamente 60 graus a direita, a 12 m: dentro do alcance e na borda do cone.
            Vector3 naBorda = Quaternion.Euler(0f, MeioAngulo, 0f) * Frente * 12f;

            Assert.IsTrue(_sentidos.Perceives(Origem, Frente, naBorda, lineOfSightClear: true));
        }

        [Test]
        public void Alvo_logo_alem_da_borda_do_cone_nao_e_percebido()
        {
            Vector3 foraDaBorda = Quaternion.Euler(0f, MeioAngulo + 5f, 0f) * Frente * 12f;

            Assert.IsFalse(_sentidos.Perceives(Origem, Frente, foraDaBorda, lineOfSightClear: true));
        }

        [Test]
        public void Parede_corta_a_visao()
        {
            Assert.IsFalse(_sentidos.Perceives(Origem, Frente, new Vector3(0f, 0f, 12f), lineOfSightClear: false));
        }

        /// <summary>
        /// Ouvir atravessa parede. Sem isso, um obstaculo fino entre os dois deixaria a
        /// criatura surda a alguem a um metro dela.
        /// </summary>
        [Test]
        public void Parede_nao_corta_o_ouvido()
        {
            var perto = new Vector3(0f, 0f, Ouvido - 1f);

            Assert.IsTrue(_sentidos.Perceives(Origem, Frente, perto, lineOfSightClear: false));
        }

        /// <summary>
        /// Alvo em cima do proprio nariz nao tem direcao, e a conta de angulo nao vale.
        /// Sem este caso, a criatura ficaria cega a quem a esta abracando.
        /// </summary>
        [Test]
        public void Alvo_na_mesma_posicao_e_percebido()
        {
            Assert.IsTrue(_sentidos.Perceives(Origem, Frente, Origem, lineOfSightClear: true));
        }

        /// <summary>
        /// A percepcao e medida no plano. Um alvo acima ou abaixo nao pode escapar do cone
        /// por altura, senao uma rampa viraria camuflagem.
        /// </summary>
        [Test]
        public void Altura_nao_entra_na_conta()
        {
            var acima = new Vector3(0f, 5f, 12f);

            Assert.IsTrue(_sentidos.Perceives(Origem, Frente, acima, lineOfSightClear: true));
        }

        [Test]
        public void Ouvido_zerado_desliga_a_percepcao_pelas_costas()
        {
            _sentidos.HearingRange = 0f;

            Assert.IsFalse(_sentidos.Perceives(Origem, Frente, new Vector3(0f, 0f, -2f), lineOfSightClear: true));
        }

        // --------------------------------------------------------------- memoria

        [Test]
        public void Perceber_liga_a_caca()
        {
            Assert.IsTrue(_sentidos.Tick(0.1f, perceivedNow: true));
            Assert.IsTrue(_sentidos.Aware);
            Assert.AreEqual(0f, _sentidos.TimeSincePerceived, 0.0001f);
        }

        /// <summary>
        /// Perder de vista nao e esquecer. E o que impede o pior comportamento de IA que
        /// existe: o inimigo que desiste no instante em que voce quebra a linha de visao.
        /// </summary>
        [Test]
        public void Perder_de_vista_nao_encerra_a_caca_na_hora()
        {
            _sentidos.Tick(0.1f, perceivedNow: true);

            Assert.IsTrue(_sentidos.Tick(Memoria - 0.5f, perceivedNow: false));
            Assert.IsTrue(_sentidos.Aware);
        }

        [Test]
        public void A_caca_termina_quando_a_memoria_expira()
        {
            _sentidos.Tick(0.1f, perceivedNow: true);

            Assert.IsFalse(_sentidos.Tick(Memoria, perceivedNow: false));
            Assert.IsFalse(_sentidos.Aware);
        }

        /// <summary>
        /// Reaparecer no meio da memoria zera a contagem. Sem isto, um alvo que entra e
        /// sai de vista seria esquecido no meio da perseguicao.
        /// </summary>
        [Test]
        public void Perceber_de_novo_reinicia_a_memoria()
        {
            _sentidos.Tick(0.1f, perceivedNow: true);
            _sentidos.Tick(Memoria - 0.2f, perceivedNow: false);
            _sentidos.Tick(0.1f, perceivedNow: true);

            Assert.IsTrue(_sentidos.Tick(Memoria - 0.2f, perceivedNow: false));
            Assert.IsTrue(_sentidos.Aware);
        }

        [Test]
        public void Sem_nunca_ter_percebido_a_memoria_nao_conta()
        {
            Assert.IsFalse(_sentidos.Tick(10f, perceivedNow: false));
            Assert.AreEqual(0f, _sentidos.TimeSincePerceived, 0.0001f);
        }

        /// <summary>
        /// Esquecer na hora existe para quando o alvo deixa de ser alvo: ele caiu, saiu da
        /// sessao, ou a propria criatura caiu.
        /// </summary>
        [Test]
        public void Esquecer_encerra_a_caca_sem_esperar_a_memoria()
        {
            _sentidos.Tick(0.1f, perceivedNow: true);
            _sentidos.Forget();

            Assert.IsFalse(_sentidos.Aware);
            Assert.AreEqual(0f, _sentidos.TimeSincePerceived, 0.0001f);
        }
    }
}
