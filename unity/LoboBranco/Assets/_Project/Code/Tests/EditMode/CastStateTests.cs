using System.Collections.Generic;
using LoboBranco.Combat;
using LoboBranco.Player;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// A linha do tempo de uma conjuracao: pedir, o instante do efeito, a recuperacao, e o
    /// compromisso que dura tudo isso (tarefa 1.32).
    ///
    /// Roda em EditMode com um conjurador de mentira, sem rede e sem host, pela mesma razao do
    /// <c>AttackStateTests</c>. O que estes testes protegem e o que so aparece em rede: um
    /// efeito que sai duas vezes, um corte que manda mensagem sobre uma conjuracao que o host ja
    /// recusou, e uma recusa velha que corta o sinal seguinte.
    /// </summary>
    public sealed class CastStateTests
    {
        PlayerStateContext _context;
        PlayerStateMachine _machine;
        ConjuradorFalso _conjurador;
        LocomocaoFalsa _locomocao;
        InputBuffer _buffer;

        AbilityDef _sinal;
        AttackDef _golpe;

        const float Step = 1f / 60f;

        const float Conjurar = 0.3f;
        const float Recuperar = 0.4f;

        [SetUp]
        public void SetUp()
        {
            _sinal = ScriptableObject.CreateInstance<AbilityDef>();
            _sinal.staminaCost = 30f;
            _sinal.cooldownSeconds = 4f;
            _sinal.castTime = Conjurar;
            _sinal.recovery = Recuperar;

            _golpe = ScriptableObject.CreateInstance<AttackDef>();

            _conjurador = new ConjuradorFalso();
            _conjurador.Vagas[0] = _sinal;

            _locomocao = new LocomocaoFalsa();
            _buffer = new InputBuffer { Window = 0.2f };

            _context = new PlayerStateContext
            {
                Locomotion = _locomocao,
                Buffer = _buffer,
                Abilities = _conjurador,
                CurrentAttack = _golpe,
            };

            _machine = new PlayerStateMachine(_context);
            _machine.Register(new LocomotionState());
            _machine.Register(new AttackState());
            _machine.Register(new CastState());
            _machine.Start(PlayerStateId.Locomotion);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_sinal);
            Object.DestroyImmediate(_golpe);
        }

        /// <summary>Um passo de jogo, na ordem do <c>PlayerBrain.Update</c>: a maquina anda, depois o buffer envelhece.</summary>
        void Avancar(float segundos)
        {
            int passos = Mathf.RoundToInt(segundos / Step);

            for (int i = 0; i < passos; i++)
            {
                _machine.Tick(Step);
                _buffer.Tick(Step);
            }
        }

        void ComecarSinal(int vaga = 0)
        {
            _context.PendingAbilitySlot = vaga;
            _machine.TryChangeState(PlayerStateId.CastSign);
        }

        // ------------------------------------------------------------ entrada

        [Test]
        public void Sinal_apertado_na_locomocao_conjura_a_vaga_selecionada()
        {
            _conjurador.Vagas[2] = _sinal;
            _context.SelectedAbilitySlot = 2;

            _buffer.Push(BufferedAction.CastSign);
            Avancar(Step);

            Assert.AreEqual(PlayerStateId.CastSign, _machine.CurrentId);
            CollectionAssert.AreEqual(new[] { 2 }, _conjurador.Inicios);
            Assert.IsFalse(_buffer.HasPending, "O input que virou conjuracao sai do buffer.");
        }

        /// <summary>Apertar um pouco antes de a recarga voltar nao pode ser apertar em vao.</summary>
        [Test]
        public void Sinal_em_recarga_espera_no_buffer_em_vez_de_sumir()
        {
            _conjurador.Recusa = AbilityRefusal.OnCooldown;

            _buffer.Push(BufferedAction.CastSign);
            Avancar(0.05f);

            Assert.AreEqual(PlayerStateId.Locomotion, _machine.CurrentId);
            Assert.IsTrue(_buffer.HasPending);
            CollectionAssert.IsEmpty(_conjurador.Inicios, "Recusado pela regra, o pedido nem sai.");

            _conjurador.Recusa = AbilityRefusal.None;
            Avancar(0.05f);

            Assert.AreEqual(PlayerStateId.CastSign, _machine.CurrentId);
        }

        [Test]
        public void Vaga_vazia_nao_conjura()
        {
            _context.SelectedAbilitySlot = 3;

            _buffer.Push(BufferedAction.CastSign);
            Avancar(0.05f);

            Assert.AreEqual(PlayerStateId.Locomotion, _machine.CurrentId);
            CollectionAssert.IsEmpty(_conjurador.Inicios);
        }

        [Test]
        public void Conjurar_zera_o_movimento()
        {
            _locomocao.MoveInput = Vector2.one;
            _locomocao.SprintHeld = true;

            ComecarSinal();

            Assert.AreEqual(Vector2.zero, _locomocao.MoveInput);
            Assert.IsFalse(_locomocao.SprintHeld);
        }

        // ---------------------------------------------------- linha do tempo

        [Test]
        public void Efeito_sai_uma_vez_no_tempo_de_conjurar_do_asset()
        {
            ComecarSinal();

            Avancar(Conjurar - 2f * Step);
            Assert.AreEqual(0, _conjurador.Efeitos, "Antes do tempo de conjurar, nada sai.");

            Avancar(4f * Step);
            Assert.AreEqual(1, _conjurador.Efeitos);

            Avancar(Recuperar - 4f * Step);
            Assert.AreEqual(1, _conjurador.Efeitos, "A recuperacao nao dispara o efeito de novo.");
        }

        [Test]
        public void Sinal_termina_em_conjurar_mais_recuperar()
        {
            ComecarSinal();

            Avancar(Conjurar + Recuperar - 2f * Step);
            Assert.AreEqual(PlayerStateId.CastSign, _machine.CurrentId);

            Avancar(4f * Step);
            Assert.AreEqual(PlayerStateId.Locomotion, _machine.CurrentId);
        }

        [Test]
        public void Sinal_em_andamento_recusa_golpe()
        {
            ComecarSinal();
            Avancar(0.1f);

            Assert.IsFalse(_machine.CanEnter(PlayerStateId.Attack), "Conjurar e acao comprometida (docs/03 secao 1).");
        }

        [Test]
        public void Sinal_que_termina_sozinho_nao_conta_como_cortado()
        {
            ComecarSinal();
            Avancar(Conjurar + Recuperar + 2f * Step);

            Assert.AreEqual(0, _conjurador.Cancelamentos);
        }

        // ------------------------------------------------------------- cortes

        [Test]
        public void Corte_antes_do_efeito_avisa_o_conjurador()
        {
            ComecarSinal();
            Avancar(0.1f);

            _machine.ForceChangeState(PlayerStateId.Locomotion);

            Assert.AreEqual(1, _conjurador.Cancelamentos);
            Assert.AreEqual(0, _conjurador.Efeitos);
        }

        /// <summary>Depois do efeito nao ha o que cortar do lado do host, e uma mensagem a mais seria sobre nada.</summary>
        [Test]
        public void Corte_depois_do_efeito_nao_avisa_ninguem()
        {
            ComecarSinal();
            Avancar(Conjurar + 3f * Step);

            _machine.ForceChangeState(PlayerStateId.Locomotion);

            Assert.AreEqual(1, _conjurador.Efeitos);
            Assert.AreEqual(0, _conjurador.Cancelamentos);
        }

        // ----------------------------------------------------- recusa do host

        [Test]
        public void Recusa_do_host_encerra_a_conjuracao_sem_efeito_e_sem_corte()
        {
            ComecarSinal();
            Avancar(0.1f);

            _context.CastRefused = true;
            Avancar(Step);

            Assert.AreEqual(PlayerStateId.Locomotion, _machine.CurrentId);
            Assert.AreEqual(0, _conjurador.Efeitos);
            Assert.AreEqual(0, _conjurador.Cancelamentos, "O host ja esqueceu o pedido que recusou.");
            Assert.IsFalse(_context.CastRefused);
        }

        /// <summary>A recusa chega depois de o tempo de conjurar do dono passar: o efeito ja pedido morre no host, e a recuperacao acaba aqui.</summary>
        [Test]
        public void Recusa_que_chega_na_recuperacao_encerra_o_sinal()
        {
            ComecarSinal();
            Avancar(Conjurar + 3f * Step);

            _context.CastRefused = true;
            Avancar(Step);

            Assert.AreEqual(PlayerStateId.Locomotion, _machine.CurrentId);
            Assert.AreEqual(0, _conjurador.Cancelamentos);
        }

        [Test]
        public void Recusa_que_sobrou_nao_corta_o_sinal_seguinte()
        {
            _context.CastRefused = true;

            ComecarSinal();
            Avancar(0.1f);

            Assert.AreEqual(PlayerStateId.CastSign, _machine.CurrentId);
        }

        // ------------------------------------------------------------ dublês

        sealed class LocomocaoFalsa : ILocomotionDriver
        {
            public Vector2 MoveInput { get; set; }
            public bool SprintHeld { get; set; }
            public float ReferenceYaw { get; set; }
        }

        sealed class ConjuradorFalso : IAbilityCaster
        {
            public readonly AbilityDef[] Vagas = new AbilityDef[AbilityReadyTimes.Capacity];
            public readonly List<int> Inicios = new List<int>();

            public AbilityRefusal Recusa;
            public int Efeitos { get; private set; }
            public int Cancelamentos { get; private set; }

            public AbilityDef AbilityAt(int slot) => slot >= 0 && slot < Vagas.Length ? Vagas[slot] : null;

            public AbilityRefusal CanCast(int slot) => AbilityAt(slot) == null ? AbilityRefusal.NoAbility : Recusa;

            public void BeginCast(int slot) => Inicios.Add(slot);

            public void TriggerCast() => Efeitos++;

            public void CancelCast() => Cancelamentos++;
        }
    }
}
