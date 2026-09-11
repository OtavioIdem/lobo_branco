using System.Collections.Generic;
using LoboBranco.Combat;
using LoboBranco.Player;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// A linha do tempo de um golpe: anticipacao, janela de dano, recuperacao, e o
    /// compromisso que dura tudo isso (docs/03 secoes 1 e 4).
    ///
    /// Roda em EditMode com dublês no lugar da locomocao e da arma, e com passos fixos de
    /// 1/60. Nenhum GameObject, nenhuma fisica, nenhum Animator: e a razao de o contexto
    /// da FSM falar com interfaces em vez de MonoBehaviours.
    ///
    /// O que estes testes protegem e o numero que ninguem confere: se a janela abrir cedo
    /// demais, o golpe acerta antes de a lamina sair, e o sintoma e "o combate parece
    /// errado", nunca um erro no Console.
    /// </summary>
    public sealed class AttackStateTests
    {
        PlayerStateContext _context;
        PlayerStateMachine _machine;
        LocomotionState _locomotionState;
        AttackState _attackState;
        AtacanteFalso _atacante;
        LocomocaoFalsa _locomocao;
        InputBuffer _buffer;

        AttackDef _leve;
        AttackDef _forte;

        const float Step = 1f / 60f;

        // Os numeros do docs/03 secao 4, repetidos aqui de proposito: se o asset mudar sem
        // que o documento mude, o teste avisa.
        const float LeveGolpe = 0.25f;
        const float LeveRecuperacao = 0.15f;
        const float ForteGolpe = 0.55f;
        const float ForteRecuperacao = 0.45f;

        [SetUp]
        public void SetUp()
        {
            _leve = CriarAtaque(Stance.Fast, LeveGolpe, LeveRecuperacao, 0.55f, 0.95f);
            _forte = CriarAtaque(Stance.Strong, ForteGolpe, ForteRecuperacao, 0.72f, 0.96f);

            _atacante = new AtacanteFalso();
            _locomocao = new LocomocaoFalsa();
            _buffer = new InputBuffer { Window = 0.2f };

            _context = new PlayerStateContext
            {
                Locomotion = _locomocao,
                Attacker = _atacante,
                Buffer = _buffer,
                LightAttack = _leve,
                HeavyAttack = _forte,
            };

            _machine = new PlayerStateMachine(_context);
            _locomotionState = new LocomotionState();
            _attackState = new AttackState();

            _machine.Register(_locomotionState);
            _machine.Register(_attackState);
            _machine.Start(PlayerStateId.Locomotion);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_leve);
            Object.DestroyImmediate(_forte);
        }

        static AttackDef CriarAtaque(Stance stance, float golpe, float recuperacao, float abre, float fecha)
        {
            var attack = ScriptableObject.CreateInstance<AttackDef>();
            attack.stance = stance;
            attack.strikeTime = golpe;
            attack.recovery = recuperacao;
            attack.hitboxOpenAt = abre;
            attack.hitboxCloseAt = fecha;
            attack.maxTargets = 1;
            attack.arcDegrees = 110f;
            attack.reach = 2.2f;
            attack.radius = 0.55f;

            return attack;
        }

        /// <summary>
        /// Um passo de jogo, na mesma ordem do <c>PlayerBrain.Update</c>: a maquina anda,
        /// e so depois o buffer envelhece. Trocar essa ordem muda quando um input guardado
        /// expira, e e por isso que o teste imita o loop real em vez de inventar um.
        /// </summary>
        void Avancar(float segundos)
        {
            int passos = Mathf.RoundToInt(segundos / Step);

            for (int i = 0; i < passos; i++)
            {
                _machine.Tick(Step);
                _buffer.Tick(Step);
            }
        }

        void ComecarGolpe(AttackDef attack)
        {
            _context.PendingAttack = attack;
            _machine.TryChangeState(PlayerStateId.Attack);
        }

        // ------------------------------------------------------------ dublês

        sealed class LocomocaoFalsa : ILocomotionDriver
        {
            public Vector2 MoveInput { get; set; }
            public bool SprintHeld { get; set; }
            public float ReferenceYaw { get; set; }
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

        // -------------------------------------------------------- compromisso

        [Test]
        public void Entrar_no_golpe_zera_o_movimento()
        {
            _locomocao.MoveInput = Vector2.one;
            _locomocao.SprintHeld = true;

            ComecarGolpe(_leve);

            Assert.AreEqual(Vector2.zero, _locomocao.MoveInput,
                "O compromisso do golpe e o que da peso a decisao de atacar.");
            Assert.IsFalse(_locomocao.SprintHeld);
        }

        [Test]
        public void Movimento_continua_zerado_durante_a_recuperacao()
        {
            ComecarGolpe(_forte);
            _context.MoveInput = Vector2.one;

            Avancar(ForteGolpe + ForteRecuperacao * 0.5f);

            Assert.AreEqual(Vector2.zero, _locomocao.MoveInput,
                "A recuperacao tambem e compromisso: e ela que transforma clicar em decidir.");
        }

        [Test]
        public void Golpe_iniciado_recusa_outro_golpe()
        {
            ComecarGolpe(_leve);
            Avancar(0.05f);

            _context.PendingAttack = _forte;
            Assert.IsFalse(_machine.TryChangeState(PlayerStateId.Attack));
            Assert.AreEqual(1, _atacante.Golpes.Count, "O segundo clique nao pode comecar um golpe novo.");
        }

        // --------------------------------------------------------- linha do tempo

        [Test]
        public void Janela_de_dano_fica_fechada_durante_a_anticipacao()
        {
            ComecarGolpe(_leve);
            Avancar(_leve.HitboxOpenTime - 0.02f);

            Assert.AreEqual(0, _atacante.Aberturas, "Nao existe golpe sem anticipacao (docs/03 secao 11).");
            Assert.AreEqual(0, _atacante.Consultas);
            Assert.AreEqual(AttackState.Phase.Anticipation, _attackState.CurrentPhase);
        }

        [Test]
        public void Janela_de_dano_abre_no_tempo_escrito_no_asset()
        {
            ComecarGolpe(_leve);
            Avancar(_leve.HitboxOpenTime + 0.01f);

            Assert.AreEqual(1, _atacante.Aberturas);
            Assert.IsTrue(_atacante.JanelaAberta);
            Assert.IsTrue(_attackState.HitboxOpen);
        }

        [Test]
        public void Janela_de_dano_fecha_no_fim_da_janela_e_nao_no_fim_do_golpe()
        {
            ComecarGolpe(_leve);
            Avancar(_leve.HitboxCloseTime + 0.01f);

            Assert.AreEqual(1, _atacante.Fechamentos);
            Assert.IsFalse(_atacante.JanelaAberta);
            Assert.AreEqual(AttackState.Phase.Recovery, _attackState.CurrentPhase);
        }

        [Test]
        public void Janela_aberta_consulta_uma_vez_por_passo()
        {
            ComecarGolpe(_leve);
            Avancar(_leve.TotalDuration);

            float duracaoDaJanela = _leve.HitboxCloseTime - _leve.HitboxOpenTime;
            int passosEsperados = Mathf.RoundToInt(duracaoDaJanela / Step);

            // Mais ou menos um passo por causa de onde a janela cai dentro do quadro.
            Assert.That(_atacante.Consultas, Is.InRange(passosEsperados - 1, passosEsperados + 1),
                "Uma consulta por passo enquanto aberta; a lista de ja-atingidos e que evita o golpe duplo.");
        }

        [Test]
        public void Golpe_com_janela_menor_que_um_quadro_ainda_consulta_uma_vez()
        {
            // Janela de 2,5 ms: menor que qualquer passo de simulacao realista.
            var relampago = CriarAtaque(Stance.Fast, 0.25f, 0.15f, 0.5f, 0.51f);

            _context.PendingAttack = relampago;
            _machine.TryChangeState(PlayerStateId.Attack);
            Avancar(relampago.TotalDuration);

            Assert.AreEqual(1, _atacante.Aberturas);
            Assert.AreEqual(1, _atacante.Fechamentos);
            Assert.GreaterOrEqual(_atacante.Consultas, 1,
                "Uma janela curta nao pode virar um golpe que nunca acerta nada.");

            Object.DestroyImmediate(relampago);
        }

        [Test]
        public void Golpe_leve_termina_em_golpe_mais_recuperacao()
        {
            ComecarGolpe(_leve);

            Avancar(LeveGolpe + LeveRecuperacao - 0.02f);
            Assert.AreEqual(PlayerStateId.Attack, _machine.CurrentId, "Ainda dentro da recuperacao.");

            Avancar(0.04f);
            Assert.AreEqual(PlayerStateId.Locomotion, _machine.CurrentId);
        }

        [Test]
        public void Golpe_forte_dura_mais_que_o_leve_pelos_numeros_do_documento()
        {
            Assert.AreEqual(0.40f, _leve.TotalDuration, 0.0001f, "docs/03 secao 4: 0,25 + 0,15.");
            Assert.AreEqual(1.00f, _forte.TotalDuration, 0.0001f, "docs/03 secao 4: 0,55 + 0,45.");

            ComecarGolpe(_forte);
            Avancar(_leve.TotalDuration + 0.02f);

            Assert.AreEqual(PlayerStateId.Attack, _machine.CurrentId,
                "O golpe forte nao pode terminar no tempo do leve.");
        }

        [Test]
        public void Postura_do_asset_chega_ao_estado()
        {
            ComecarGolpe(_forte);

            Assert.AreEqual(Stance.Strong, _attackState.CurrentAttack.stance,
                "E a postura que alimenta o estagio 2 do pipeline de dano.");
        }

        // ------------------------------------------------------- interrupcao

        [Test]
        public void Sair_do_golpe_com_a_janela_aberta_fecha_a_hitbox()
        {
            // Esquiva ainda nao existe, entao a saida forcada faz o papel dela: o que
            // importa aqui e que ninguem sai de um ataque com a hitbox ligada.
            ComecarGolpe(_leve);
            Avancar(_leve.HitboxOpenTime + 0.01f);
            Assert.IsTrue(_atacante.JanelaAberta);

            _machine.ForceChangeState(PlayerStateId.Locomotion);

            Assert.IsFalse(_atacante.JanelaAberta, "Hitbox ligada dentro do estado seguinte acerta durante a esquiva.");
            Assert.AreEqual(1, _atacante.Cancelamentos);
        }

        [Test]
        public void Golpe_que_termina_sozinho_nao_conta_como_cancelado()
        {
            ComecarGolpe(_leve);
            Avancar(_leve.TotalDuration + 0.02f);

            Assert.AreEqual(0, _atacante.Cancelamentos,
                "Terminar nao e ser interrompido; confundir os dois vira animacao cortada no meio.");
        }

        // ------------------------------------------------ integracao com o buffer

        [Test]
        public void Ataque_apertado_durante_a_anticipacao_e_engolido_pela_janela_do_buffer()
        {
            ComecarGolpe(_forte);

            // 1,0 s de golpe forte contra 0,2 s de buffer: apertar no comeco nao alcanca o fim.
            _buffer.Push(BufferedAction.AttackLight);
            Avancar(_forte.TotalDuration + 0.02f);

            Assert.AreEqual(PlayerStateId.Locomotion, _machine.CurrentId);
            Assert.AreEqual(1, _atacante.Golpes.Count);
        }

        [Test]
        public void Ataque_apertado_no_fim_da_recuperacao_sai_assim_que_o_golpe_termina()
        {
            ComecarGolpe(_leve);
            Avancar(_leve.TotalDuration - 0.1f);

            // Dentro da janela de 0,2 s: e exatamente o caso que o buffer existe para salvar.
            _buffer.Push(BufferedAction.AttackLight);

            Avancar(0.15f);

            Assert.AreEqual(PlayerStateId.Attack, _machine.CurrentId,
                "O input guardado tem que virar o golpe seguinte sem o jogador apertar de novo.");
            Assert.AreEqual(2, _atacante.Golpes.Count);
            Assert.IsFalse(_buffer.HasPending, "O buffer so pode ser consumido uma vez.");
        }

        [Test]
        public void Ataque_pesado_guardado_vira_golpe_pesado()
        {
            ComecarGolpe(_leve);
            Avancar(_leve.TotalDuration - 0.05f);

            _buffer.Push(BufferedAction.AttackHeavy);
            Avancar(0.1f);

            Assert.AreEqual(2, _atacante.Golpes.Count);
            Assert.AreSame(_forte, _atacante.Golpes[1]);
        }

        [Test]
        public void Esquiva_guardada_nao_vira_ataque()
        {
            _buffer.Push(BufferedAction.Dodge);
            Avancar(0.05f);

            Assert.AreEqual(PlayerStateId.Locomotion, _machine.CurrentId);
            Assert.AreEqual(0, _atacante.Golpes.Count,
                "Ate a tarefa 1.10 existir, a esquiva guardada expira sem virar outra coisa.");
        }

        // --------------------------------------------------------- locomocao

        [Test]
        public void Locomocao_repassa_o_input_para_a_locomocao()
        {
            _context.MoveInput = new Vector2(0.5f, -1f);
            _context.SprintHeld = true;

            _machine.Tick(Step);

            Assert.AreEqual(new Vector2(0.5f, -1f), _locomocao.MoveInput);
            Assert.IsTrue(_locomocao.SprintHeld);
        }

        [Test]
        public void Golpe_sem_asset_devolve_o_controle_em_vez_de_travar()
        {
            _context.PendingAttack = null;
            _machine.ForceChangeState(PlayerStateId.Attack);

            _machine.Tick(Step);

            Assert.AreEqual(PlayerStateId.Locomotion, _machine.CurrentId);
        }
    }
}
