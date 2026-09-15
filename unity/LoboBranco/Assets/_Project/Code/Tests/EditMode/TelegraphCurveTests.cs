using LoboBranco.Combat;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// O telegrafo como funcao do tempo (docs/03 secao 10).
    ///
    /// O que estes testes protegem e o contrato entre o que o jogador ve e o que o golpe
    /// faz: o aviso tem que acabar exatamente quando a hitbox abre. Se o aviso terminar
    /// antes, o jogador esquiva cedo; se terminar depois, ele apanha ainda sendo avisado.
    /// Nos dois casos ele aprende o tempo errado, e nada disso aparece no Console.
    ///
    /// Dois deles sao sobre rede, e existem porque a curva e o que o cliente calcula a
    /// partir do carimbo de tempo do host.
    /// </summary>
    public sealed class TelegraphCurveTests
    {
        AttackDef _golpe;

        // Golpe de 1,0 s com janela de 0,6 a 0,8 s e 0,4 s de recuperacao.
        const float AvisoInicial = 0.35f;

        [SetUp]
        public void SetUp()
        {
            _golpe = CriarAtaque(abre: 0.6f, fecha: 0.8f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_golpe);

        static AttackDef CriarAtaque(float abre, float fecha)
        {
            var attack = ScriptableObject.CreateInstance<AttackDef>();
            attack.strikeTime = 1.0f;
            attack.recovery = 0.4f;
            attack.hitboxOpenAt = abre;
            attack.hitboxCloseAt = fecha;

            return attack;
        }

        TelegraphSample Em(float segundos) => TelegraphCurve.Evaluate(_golpe, segundos, AvisoInicial);

        // ------------------------------------------------------------- anticipacao

        [Test]
        public void Sem_golpe_o_telegrafo_fica_apagado()
        {
            TelegraphSample amostra = TelegraphCurve.Evaluate(null, 0.3f, AvisoInicial);

            Assert.IsTrue(amostra.Finished);
            Assert.AreEqual(0f, amostra.Warning);
            Assert.AreEqual(0f, amostra.Flash);
        }

        /// <summary>
        /// Um aviso que nasce apagado so fica perceptivel no meio da anticipacao, e a metade
        /// que sobra e curta demais para reagir.
        /// </summary>
        [Test]
        public void O_aviso_aparece_desde_o_primeiro_quadro()
        {
            TelegraphSample amostra = Em(0f);

            Assert.AreEqual(AttackPhase.Anticipation, amostra.Phase);
            Assert.AreEqual(AvisoInicial, amostra.Warning, 0.0001f);
        }

        /// <summary>Um aviso que enche diz quando o golpe vem, e nao so que ele vem.</summary>
        [Test]
        public void O_aviso_cresce_ate_a_janela_abrir()
        {
            float cedo = Em(0.1f).Warning;
            float meio = Em(0.3f).Warning;
            float tarde = Em(0.5f).Warning;

            Assert.Less(cedo, meio);
            Assert.Less(meio, tarde);
        }

        [Test]
        public void O_aviso_esta_quase_cheio_no_ultimo_instante_da_anticipacao()
        {
            Assert.Greater(Em(0.59f).Warning, 0.95f);
        }

        [Test]
        public void O_aviso_nao_acende_o_golpe_antes_da_hora()
        {
            Assert.AreEqual(0f, Em(0.59f).Flash);
        }

        // -------------------------------------------------------- janela e depois

        /// <summary>O aviso acaba no instante exato em que a hitbox passa a acertar.</summary>
        [Test]
        public void A_janela_abre_junto_com_a_hitbox()
        {
            TelegraphSample amostra = Em(_golpe.HitboxOpenTime);

            Assert.AreEqual(AttackPhase.Active, amostra.Phase);
            Assert.AreEqual(1f, amostra.Flash);
        }

        /// <summary>
        /// Apagar aos poucos e a unica pista de que a criatura esta comprometida, e
        /// comprometida e quando se contra-ataca.
        /// </summary>
        [Test]
        public void A_recuperacao_apaga_aos_poucos()
        {
            TelegraphSample logoDepois = Em(0.9f);
            TelegraphSample quaseNoFim = Em(1.3f);

            Assert.AreEqual(AttackPhase.Recovery, logoDepois.Phase);
            Assert.AreEqual(0f, logoDepois.Warning);
            Assert.Greater(logoDepois.Flash, quaseNoFim.Flash);
            Assert.Greater(quaseNoFim.Flash, 0f);
        }

        [Test]
        public void Depois_do_fim_nao_sobra_nada()
        {
            TelegraphSample amostra = Em(_golpe.TotalDuration + 0.01f);

            Assert.IsTrue(amostra.Finished);
            Assert.AreEqual(0f, amostra.Warning);
            Assert.AreEqual(0f, amostra.Flash);
        }

        /// <summary>
        /// Janela de duracao zero nao pode deixar a curva presa no aviso. O golpe acerta em
        /// um quadro, e o telegrafo tem que sair da anticipacao nesse mesmo quadro.
        /// </summary>
        [Test]
        public void Janela_instantanea_nao_trava_no_aviso()
        {
            var instantaneo = CriarAtaque(abre: 0.6f, fecha: 0.6f);

            TelegraphSample amostra = TelegraphCurve.Evaluate(instantaneo, 0.6f, AvisoInicial);

            Assert.AreNotEqual(AttackPhase.Anticipation, amostra.Phase);
            Assert.AreEqual(1f, amostra.Flash, 0.0001f);

            Object.DestroyImmediate(instantaneo);
        }

        [Test]
        public void Aviso_inicial_fora_da_faixa_e_limitado()
        {
            TelegraphSample amostra = TelegraphCurve.Evaluate(_golpe, 0f, 2f);

            Assert.LessOrEqual(amostra.Warning, 1f);
        }

        // ------------------------------------------------------------------- rede

        /// <summary>
        /// A estimativa de tempo do servidor no cliente pode ficar um pouco atras do carimbo
        /// do host. Tratar isso como "antes do golpe" apagaria o aviso justamente em quem ja
        /// recebe tudo com atraso.
        /// </summary>
        [Test]
        public void Relogio_do_cliente_atrasado_nao_apaga_o_aviso()
        {
            TelegraphSample amostra = Em(-0.05f);

            Assert.IsFalse(amostra.Finished);
            Assert.AreEqual(AttackPhase.Anticipation, amostra.Phase);
            Assert.AreEqual(AvisoInicial, amostra.Warning, 0.0001f);
        }

        /// <summary>
        /// Um cliente com ping recebe o inicio do golpe tarde, e o aviso dele ja comeca
        /// alto. E o preco de o aviso terminar junto com a hitbox do host: a alternativa,
        /// recomecar do zero, faria o aviso do cliente acabar depois de o golpe ter acertado.
        /// </summary>
        [Test]
        public void Cliente_que_recebe_o_golpe_tarde_ve_o_aviso_ja_adiantado()
        {
            const float IdaEVolta = 0.1f;

            Assert.Greater(Em(IdaEVolta).Warning, AvisoInicial);
        }
    }
}
