using LoboBranco.Combat;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// O hitstop visto pela linha do tempo do golpe (tech/adr/0010).
    ///
    /// Em coop o hitstop nao pode ser <c>Time.timeScale</c>, entao ele e um golpe que fica
    /// parado por alguns centesimos de segundo, nas duas pontas da rede. O que estes testes
    /// protegem e a conta que faz as duas pontas concordarem: o golpe tem que durar
    /// exatamente o tempo dele mais o tempo segurado, nem um passo a mais. Se durar mais no
    /// dono do que no host, a janela de Fluxo de 0,22 s encolhe so para quem bateu, e o
    /// sintoma e "o encadeamento as vezes nao pega", sem nada no Console.
    /// </summary>
    public sealed class AttackTimelineHoldTests
    {
        AttackTimeline _linha;
        AtacanteFalso _atacante;
        AttackDef _golpe;

        const float Step = 1f / 60f;
        const float Hitstop = 0.08f;

        [SetUp]
        public void SetUp()
        {
            _golpe = ScriptableObject.CreateInstance<AttackDef>();
            _golpe.strikeTime = 0.5f;
            _golpe.recovery = 0.2f;
            _golpe.hitboxOpenAt = 0.6f;
            _golpe.hitboxCloseAt = 0.9f;

            _atacante = new AtacanteFalso();
            _linha = new AttackTimeline();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_golpe);

        [Test]
        public void Segurar_congela_o_golpe()
        {
            _linha.Begin(_golpe, _atacante);
            _linha.Tick(0.1f);

            _linha.Hold(Hitstop);
            _linha.Tick(0.05f);

            Assert.AreEqual(0.1f, _linha.Elapsed, 0.0001f);
            Assert.IsTrue(_linha.Running);
        }

        /// <summary>
        /// A sobra do passo avanca o golpe. Sem isto, cada hitstop arredondaria para cima em
        /// um quadro inteiro, e a duracao passaria a depender da taxa de quadros de quem joga.
        /// </summary>
        [Test]
        public void A_sobra_do_passo_avanca_o_golpe()
        {
            _linha.Begin(_golpe, _atacante);
            _linha.Hold(0.02f);
            _linha.Tick(0.05f);

            Assert.AreEqual(0.03f, _linha.Elapsed, 0.0001f);
            Assert.AreEqual(0f, _linha.HoldRemaining, 0.0001f);
        }

        /// <summary>A conta que faz host e dono concordarem sobre quando o golpe acaba.</summary>
        [Test]
        public void O_golpe_dura_o_tempo_dele_mais_o_tempo_segurado()
        {
            _linha.Begin(_golpe, _atacante);

            float simulado = 0f;
            bool segurou = false;

            while (_linha.Tick(Step))
            {
                simulado += Step;

                if (!segurou && _linha.HitboxOpen)
                {
                    _linha.Hold(Hitstop);
                    segurou = true;
                }

                Assert.Less(simulado, 5f, "a linha do tempo nao terminou");
            }

            simulado += Step;

            Assert.IsTrue(segurou);
            Assert.AreEqual(_golpe.TotalDuration + Hitstop, simulado, Step * 1.5f);
        }

        /// <summary>
        /// Um hitstop por golpe, e nao por alvo. A postura Grupo acerta ate quatro, e somar
        /// quatro congelamentos transformaria o golpe mais util contra bando no mais lento.
        /// </summary>
        [Test]
        public void Segurar_de_novo_nao_soma()
        {
            _linha.Begin(_golpe, _atacante);

            _linha.Hold(Hitstop);
            _linha.Hold(Hitstop);
            Assert.AreEqual(Hitstop, _linha.HoldRemaining, 0.0001f);

            _linha.Hold(0.02f);
            Assert.AreEqual(Hitstop, _linha.HoldRemaining, 0.0001f, "um pedido menor nao encolhe o maior");
        }

        /// <summary>
        /// Congelado, o golpe nao consulta a fisica. O alvo ja foi atingido, e um segundo
        /// alvo que entrasse no arco durante o congelamento seria acertado por um golpe que,
        /// na tela, esta parado.
        /// </summary>
        [Test]
        public void Congelado_o_golpe_nao_consulta_a_hitbox()
        {
            _linha.Begin(_golpe, _atacante);
            _linha.Tick(_golpe.HitboxOpenTime + 0.01f);

            int antes = _atacante.Consultas;
            _linha.Hold(Hitstop);
            _linha.Tick(0.03f);

            Assert.AreEqual(antes, _atacante.Consultas);
        }

        [Test]
        public void Segurar_sem_golpe_nao_faz_nada()
        {
            _linha.Hold(Hitstop);

            Assert.AreEqual(0f, _linha.HoldRemaining);
            Assert.IsFalse(_linha.Tick(Step));
        }

        [Test]
        public void Golpe_novo_nao_herda_o_congelamento_do_anterior()
        {
            _linha.Begin(_golpe, _atacante);
            _linha.Hold(Hitstop);
            _linha.End();

            _linha.Begin(_golpe, _atacante);

            Assert.AreEqual(0f, _linha.HoldRemaining);
        }

        sealed class AtacanteFalso : IMeleeAttacker
        {
            public int Consultas { get; private set; }

            public void BeginSwing(AttackDef attack) { }

            public void OpenHitbox() { }

            public void CloseHitbox() { }

            public void TickHitbox() => Consultas++;

            public void CancelSwing() { }
        }
    }
}
