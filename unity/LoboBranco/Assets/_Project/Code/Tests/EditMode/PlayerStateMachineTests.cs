using System.Collections.Generic;
using System.Text.RegularExpressions;
using LoboBranco.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LoboBranco.Tests
{
    /// <summary>
    /// As transicoes da FSM do jogador, e principalmente a regra de ouro do docs/03
    /// secao 1: nenhum input cancela uma animacao ja iniciada, exceto a esquiva.
    ///
    /// Essa regra e a origem de todo o peso tatico do combate, e ela e exatamente o tipo
    /// de coisa que se perde em silencio: basta alguem trocar um <c>TryChangeState</c> por
    /// um <c>ForceChangeState</c> para atacar virar cancelavel, e nenhum erro aparece.
    /// O que se ve e o combate ficando "mais responsivo" e o jogo ficando raso.
    /// </summary>
    public sealed class PlayerStateMachineTests
    {
        PlayerStateContext _context;
        PlayerStateMachine _machine;

        [SetUp]
        public void SetUp()
        {
            _context = new PlayerStateContext();
            _machine = new PlayerStateMachine(_context);
        }

        /// <summary>Estado de mentira que registra as chamadas recebidas.</summary>
        sealed class EstadoEspiao : PlayerStateBase
        {
            readonly PlayerStateId _id;
            readonly bool _committed;

            public EstadoEspiao(PlayerStateId id, bool committed = false)
            {
                _id = id;
                _committed = committed;
            }

            public override PlayerStateId Id => _id;
            public override bool IsCommitted => _committed;

            public int Entradas { get; private set; }
            public int Saidas { get; private set; }
            public int Ticks { get; private set; }
            public float TempoRecebido { get; private set; }

            public override void OnEnter(PlayerStateContext context) => Entradas++;
            public override void OnExit(PlayerStateContext context) => Saidas++;

            public override void OnTick(PlayerStateContext context, float deltaTime)
            {
                Ticks++;
                TempoRecebido += deltaTime;
            }
        }

        EstadoEspiao Registrar(PlayerStateId id, bool committed = false)
        {
            var estado = new EstadoEspiao(id, committed);
            _machine.Register(estado);
            return estado;
        }

        // ------------------------------------------------------------- registro

        [Test]
        public void Construtor_escreve_a_maquina_no_contexto()
        {
            Assert.AreSame(_machine, _context.Machine,
                "Os estados alcancam a maquina pelo contexto; sem isso nenhum deles consegue transicionar.");
        }

        [Test]
        public void Start_entra_no_estado_inicial()
        {
            var locomocao = Registrar(PlayerStateId.Locomotion);
            _machine.Start(PlayerStateId.Locomotion);

            Assert.AreEqual(PlayerStateId.Locomotion, _machine.CurrentId);
            Assert.AreEqual(1, locomocao.Entradas);
        }

        [Test]
        public void Estado_nao_registrado_nao_recebe_transicao()
        {
            Registrar(PlayerStateId.Locomotion);
            _machine.Start(PlayerStateId.Locomotion);

            // Esquiva e a tarefa 1.10 e ainda nao existe. A maquina avisa uma vez, e o
            // aviso e o que impede a transicao de sumir sem sintoma durante o M1.
            LogAssert.Expect(LogType.Warning, new Regex("Dodge"));

            Assert.IsFalse(_machine.Has(PlayerStateId.Dodge));
            Assert.IsFalse(_machine.TryChangeState(PlayerStateId.Dodge));
            Assert.AreEqual(PlayerStateId.Locomotion, _machine.CurrentId);
        }

        // ------------------------------------------------------------ transicao

        [Test]
        public void Transicao_permitida_troca_de_estado_e_avisa_na_ordem_certa()
        {
            var locomocao = Registrar(PlayerStateId.Locomotion);
            var ataque = Registrar(PlayerStateId.Attack, committed: true);

            var trocas = new List<(PlayerStateId, PlayerStateId)>();
            _machine.StateChanged += (de, para) => trocas.Add((de, para));

            _machine.Start(PlayerStateId.Locomotion);

            Assert.IsTrue(_machine.TryChangeState(PlayerStateId.Attack));
            Assert.AreEqual(PlayerStateId.Attack, _machine.CurrentId);
            Assert.AreEqual(1, locomocao.Saidas);
            Assert.AreEqual(1, ataque.Entradas);

            Assert.AreEqual(1, trocas.Count);
            Assert.AreEqual((PlayerStateId.Locomotion, PlayerStateId.Attack), trocas[0]);
        }

        [Test]
        public void Transicao_para_o_proprio_estado_e_recusada()
        {
            var locomocao = Registrar(PlayerStateId.Locomotion);
            _machine.Start(PlayerStateId.Locomotion);

            Assert.IsFalse(_machine.TryChangeState(PlayerStateId.Locomotion));
            Assert.AreEqual(1, locomocao.Entradas, "Nao pode reentrar por acidente.");
        }

        [Test]
        public void TimeInState_zera_a_cada_troca()
        {
            Registrar(PlayerStateId.Locomotion);
            Registrar(PlayerStateId.Attack, committed: true);

            _machine.Start(PlayerStateId.Locomotion);
            _machine.Tick(0.5f);
            Assert.AreEqual(0.5f, _machine.TimeInState, 0.0001f);

            _machine.TryChangeState(PlayerStateId.Attack);
            Assert.AreEqual(0f, _machine.TimeInState, 0.0001f);
        }

        // -------------------------------------------------------- regra de ouro

        [Test]
        public void Acao_comprometida_recusa_outro_ataque()
        {
            Registrar(PlayerStateId.Locomotion);
            Registrar(PlayerStateId.Attack, committed: true);

            _machine.Start(PlayerStateId.Locomotion);
            _machine.TryChangeState(PlayerStateId.Attack);

            Assert.IsFalse(_machine.CanEnter(PlayerStateId.Attack));
            Assert.AreEqual(PlayerStateId.Attack, _machine.CurrentId,
                "Um golpe iniciado nao pode ser abortado por outro golpe (docs/03 secao 1).");
        }

        [Test]
        public void Acao_comprometida_recusa_sinal_item_e_interacao()
        {
            Registrar(PlayerStateId.Locomotion);
            Registrar(PlayerStateId.Attack, committed: true);
            Registrar(PlayerStateId.CastSign, committed: true);
            Registrar(PlayerStateId.UseItem, committed: true);
            Registrar(PlayerStateId.Interact);

            _machine.Start(PlayerStateId.Locomotion);
            _machine.TryChangeState(PlayerStateId.Attack);

            Assert.IsFalse(_machine.TryChangeState(PlayerStateId.CastSign));
            Assert.IsFalse(_machine.TryChangeState(PlayerStateId.UseItem));
            Assert.IsFalse(_machine.TryChangeState(PlayerStateId.Interact));
            Assert.AreEqual(PlayerStateId.Attack, _machine.CurrentId);
        }

        [Test]
        public void Acao_comprometida_aceita_esquiva_e_rolamento()
        {
            Registrar(PlayerStateId.Locomotion);
            Registrar(PlayerStateId.Attack, committed: true);
            Registrar(PlayerStateId.Dodge, committed: true);

            _machine.Start(PlayerStateId.Locomotion);
            _machine.TryChangeState(PlayerStateId.Attack);

            Assert.IsTrue(_machine.TryChangeState(PlayerStateId.Dodge),
                "A esquiva e a unica excecao que o docs/03 secao 1 concede ao jogador.");
            Assert.AreEqual(PlayerStateId.Dodge, _machine.CurrentId);
        }

        [Test]
        public void Acao_comprometida_aceita_imposicao_do_mundo()
        {
            Registrar(PlayerStateId.Locomotion);
            Registrar(PlayerStateId.Attack, committed: true);
            Registrar(PlayerStateId.Stagger, committed: true);
            Registrar(PlayerStateId.Death, committed: true);
            Registrar(PlayerStateId.DialogueLocked, committed: true);

            _machine.Start(PlayerStateId.Locomotion);
            _machine.TryChangeState(PlayerStateId.Attack);

            // Atordoamento, morte e dialogo nao sao input: sao o mundo agindo. Se nao
            // atravessassem, o jogador ficaria preso na propria animacao de ataque.
            Assert.IsTrue(_machine.CanEnter(PlayerStateId.Stagger));
            Assert.IsTrue(_machine.CanEnter(PlayerStateId.Death));
            Assert.IsTrue(_machine.CanEnter(PlayerStateId.DialogueLocked));
        }

        [Test]
        public void Estado_livre_aceita_qualquer_transicao_registrada()
        {
            Registrar(PlayerStateId.Locomotion);
            Registrar(PlayerStateId.Attack, committed: true);
            Registrar(PlayerStateId.CastSign, committed: true);

            _machine.Start(PlayerStateId.Locomotion);

            Assert.IsTrue(_machine.CanEnter(PlayerStateId.Attack));
            Assert.IsTrue(_machine.CanEnter(PlayerStateId.CastSign));
        }

        [Test]
        public void Regra_de_ouro_esta_em_um_lugar_so()
        {
            // O teste guarda a lista fechada. Alguem que quiser tornar outra coisa
            // cancelavel tem que passar por aqui, e a mudanca aparece na revisao.
            Assert.IsTrue(PlayerStateRules.CancelsCommittedAction(PlayerStateId.Dodge));
            Assert.IsTrue(PlayerStateRules.CancelsCommittedAction(PlayerStateId.Roll));
            Assert.IsTrue(PlayerStateRules.CancelsCommittedAction(PlayerStateId.Stagger));
            Assert.IsTrue(PlayerStateRules.CancelsCommittedAction(PlayerStateId.Death));
            Assert.IsTrue(PlayerStateRules.CancelsCommittedAction(PlayerStateId.DialogueLocked));

            Assert.IsFalse(PlayerStateRules.CancelsCommittedAction(PlayerStateId.Attack));
            Assert.IsFalse(PlayerStateRules.CancelsCommittedAction(PlayerStateId.Parry));
            Assert.IsFalse(PlayerStateRules.CancelsCommittedAction(PlayerStateId.Riposte));
            Assert.IsFalse(PlayerStateRules.CancelsCommittedAction(PlayerStateId.CastSign));
            Assert.IsFalse(PlayerStateRules.CancelsCommittedAction(PlayerStateId.UseItem));
            Assert.IsFalse(PlayerStateRules.CancelsCommittedAction(PlayerStateId.Interact));
            Assert.IsFalse(PlayerStateRules.CancelsCommittedAction(PlayerStateId.Locomotion));
        }

        // -------------------------------------------------------------- forcado

        [Test]
        public void ForceChangeState_atravessa_a_regra_de_ouro()
        {
            Registrar(PlayerStateId.Locomotion);
            Registrar(PlayerStateId.Attack, committed: true);

            _machine.Start(PlayerStateId.Locomotion);
            _machine.TryChangeState(PlayerStateId.Attack);

            // E assim que um golpe que terminou volta para a locomocao: terminar sozinho
            // nao e ser interrompido.
            _machine.ForceChangeState(PlayerStateId.Locomotion);

            Assert.AreEqual(PlayerStateId.Locomotion, _machine.CurrentId);
        }

        [Test]
        public void ForceChangeState_para_o_mesmo_estado_reinicia_ele()
        {
            var locomocao = Registrar(PlayerStateId.Locomotion);
            _machine.Start(PlayerStateId.Locomotion);

            _machine.ForceChangeState(PlayerStateId.Locomotion);

            Assert.AreEqual(2, locomocao.Entradas);
            Assert.AreEqual(1, locomocao.Saidas);
        }

        // ----------------------------------------------------------------- tick

        [Test]
        public void Tick_repassa_o_delta_recebido_para_o_estado()
        {
            var locomocao = Registrar(PlayerStateId.Locomotion);
            _machine.Start(PlayerStateId.Locomotion);

            for (int i = 0; i < 60; i++)
                _machine.Tick(1f / 60f);

            Assert.AreEqual(60, locomocao.Ticks);
            Assert.AreEqual(1f, locomocao.TempoRecebido, 0.001f,
                "Sessenta passos de 1/60 tem que somar exatamente um segundo, em toda execucao.");
        }

        [Test]
        public void Tick_com_delta_zero_nao_avanca_nada()
        {
            var locomocao = Registrar(PlayerStateId.Locomotion);
            _machine.Start(PlayerStateId.Locomotion);

            _machine.Tick(0f);

            Assert.AreEqual(0, locomocao.Ticks);
            Assert.AreEqual(0f, _machine.TimeInState, 0.0001f);
        }
    }
}
