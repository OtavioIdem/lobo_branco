using System.Collections.Generic;
using LoboBranco.Combat;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// A linha do tempo de um golpe, agora compartilhada entre o bruxo e o monstro.
    ///
    /// Os testes do <c>AttackState</c> ja verificam esta contagem pelo lado do jogador.
    /// Estes verificam pelo lado de baixo, e e por isso que valem a pena: a partir da
    /// tarefa 1.21 a mesma classe dirige a janela de dano de quem ataca voce, e um erro
    /// aqui passa a errar dos dois lados do combate ao mesmo tempo.
    ///
    /// Passo fixo de 1/60, dublê no lugar da arma, nenhum GameObject e nenhum Animator.
    /// </summary>
    public sealed class AttackTimelineTests
    {
        AttackTimeline _linha;
        AtacanteFalso _atacante;
        AttackDef _golpe;

        const float Step = 1f / 60f;
        const float TempoDoGolpe = 0.5f;
        const float Recuperacao = 0.2f;

        [SetUp]
        public void SetUp()
        {
            _golpe = CriarAtaque(TempoDoGolpe, Recuperacao, abre: 0.6f, fecha: 0.9f);
            _atacante = new AtacanteFalso();
            _linha = new AttackTimeline();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_golpe);

        static AttackDef CriarAtaque(float golpe, float recuperacao, float abre, float fecha)
        {
            var attack = ScriptableObject.CreateInstance<AttackDef>();
            attack.stance = Stance.Fast;
            attack.strikeTime = golpe;
            attack.recovery = recuperacao;
            attack.hitboxOpenAt = abre;
            attack.hitboxCloseAt = fecha;
            attack.maxTargets = 1;
            attack.reach = 2f;
            attack.radius = 0.5f;

            return attack;
        }

        /// <summary>Avanca em passos fixos e devolve se a linha do tempo ainda esta rodando.</summary>
        bool Avancar(float segundos)
        {
            int passos = Mathf.RoundToInt(segundos / Step);
            bool rodando = true;

            for (int i = 0; i < passos; i++)
                rodando = _linha.Tick(Step);

            return rodando;
        }

        // ----------------------------------------------------------- janela de dano

        [Test]
        public void Comecar_avisa_quem_empunha_a_arma()
        {
            _linha.Begin(_golpe, _atacante);

            Assert.AreEqual(1, _atacante.Golpes.Count);
            Assert.AreSame(_golpe, _atacante.Golpes[0]);
        }

        [Test]
        public void Janela_fica_fechada_durante_a_anticipacao()
        {
            _linha.Begin(_golpe, _atacante);
            Avancar(_golpe.HitboxOpenTime - 0.05f);

            Assert.AreEqual(AttackPhase.Anticipation, _linha.CurrentPhase);
            Assert.IsFalse(_linha.HitboxOpen);
            Assert.AreEqual(0, _atacante.Aberturas);
        }

        [Test]
        public void Janela_abre_no_tempo_escrito_no_asset()
        {
            _linha.Begin(_golpe, _atacante);
            Avancar(_golpe.HitboxOpenTime + 0.02f);

            Assert.AreEqual(1, _atacante.Aberturas);
            Assert.IsTrue(_linha.HitboxOpen);
        }

        /// <summary>
        /// A janela fecha no fim dela, e nao no fim do golpe. Se fechasse no fim, a
        /// recuperacao inteira acertaria, e o compromisso deixaria de ter custo.
        /// </summary>
        [Test]
        public void Janela_fecha_antes_do_fim_do_golpe()
        {
            _linha.Begin(_golpe, _atacante);
            Avancar(_golpe.HitboxCloseTime + 0.02f);

            Assert.AreEqual(1, _atacante.Fechamentos);
            Assert.IsFalse(_linha.HitboxOpen);
            Assert.AreEqual(AttackPhase.Recovery, _linha.CurrentPhase);
        }

        [Test]
        public void Janela_aberta_consulta_uma_vez_por_passo()
        {
            _linha.Begin(_golpe, _atacante);
            Avancar(_golpe.HitboxOpenTime + 0.001f);

            int antes = _atacante.Consultas;
            _linha.Tick(Step);

            Assert.AreEqual(antes + 1, _atacante.Consultas);
        }

        /// <summary>
        /// Uma janela mais curta que um quadro ainda produz exatamente uma consulta. Sem
        /// isto, o golpe rapido do monstro as vezes atravessaria o bruxo sem tirar nada, e
        /// o sintoma nao apareceria no Console.
        /// </summary>
        [Test]
        public void Janela_menor_que_um_quadro_ainda_consulta_uma_vez()
        {
            var instantaneo = CriarAtaque(0.5f, 0.2f, abre: 0.6f, fecha: 0.6f);

            _linha.Begin(instantaneo, _atacante);
            Avancar(instantaneo.HitboxOpenTime + 0.02f);

            Assert.AreEqual(1, _atacante.Aberturas);
            Assert.AreEqual(1, _atacante.Fechamentos);
            Assert.GreaterOrEqual(_atacante.Consultas, 1);

            Object.DestroyImmediate(instantaneo);
        }

        // ------------------------------------------------------------------ fim

        [Test]
        public void Golpe_termina_em_tempo_de_golpe_mais_recuperacao()
        {
            _linha.Begin(_golpe, _atacante);

            Assert.IsTrue(Avancar(_golpe.TotalDuration - 0.05f), "terminou antes da hora");
            Assert.IsFalse(Avancar(0.1f), "nao terminou no fim da recuperacao");
        }

        [Test]
        public void Golpe_que_termina_sozinho_nao_conta_como_cancelado()
        {
            _linha.Begin(_golpe, _atacante);
            Avancar(_golpe.TotalDuration + 0.05f);
            _linha.End();

            Assert.AreEqual(0, _atacante.Cancelamentos);
        }

        /// <summary>
        /// Cortar o golpe com a janela aberta tem que fechar a hitbox. Sem isto ela ficaria
        /// ligada dentro do estado seguinte, e o personagem acertaria enquanto esquiva.
        /// </summary>
        [Test]
        public void Encerrar_com_a_janela_aberta_fecha_a_hitbox_e_cancela()
        {
            _linha.Begin(_golpe, _atacante);
            Avancar(_golpe.HitboxOpenTime + 0.02f);

            Assert.IsTrue(_linha.HitboxOpen);

            _linha.End();

            Assert.AreEqual(1, _atacante.Fechamentos);
            Assert.AreEqual(1, _atacante.Cancelamentos);
            Assert.IsFalse(_atacante.JanelaAberta);
        }

        [Test]
        public void Sem_golpe_a_linha_do_tempo_nao_roda()
        {
            _linha.Begin(null, _atacante);

            Assert.IsFalse(_linha.Running);
            Assert.IsFalse(_linha.Tick(Step));
            Assert.AreEqual(0, _atacante.Golpes.Count);
        }

        [Test]
        public void Encerrar_limpa_o_golpe_corrente()
        {
            _linha.Begin(_golpe, _atacante);
            _linha.End();

            Assert.IsNull(_linha.Attack);
            Assert.IsFalse(_linha.Running);
        }

        sealed class AtacanteFalso : IMeleeAttacker
        {
            public readonly List<AttackDef> Golpes = new List<AttackDef>();
            public int Aberturas { get; private set; }
            public int Fechamentos { get; private set; }
            public int Consultas { get; private set; }
            public int Cancelamentos { get; private set; }
            public bool JanelaAberta { get; private set; }

            public void BeginSwing(AttackDef attack) => Golpes.Add(attack);

            public void OpenHitbox()
            {
                Aberturas++;
                JanelaAberta = true;
            }

            public void CloseHitbox()
            {
                Fechamentos++;
                JanelaAberta = false;
            }

            public void TickHitbox() => Consultas++;

            public void CancelSwing()
            {
                Cancelamentos++;
                JanelaAberta = false;
            }
        }
    }
}
