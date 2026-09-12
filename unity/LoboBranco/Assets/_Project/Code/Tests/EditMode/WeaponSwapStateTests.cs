using LoboBranco.Combat;
using LoboBranco.Player;
using NUnit.Framework;
using UnityEngine;

namespace LoboBranco.Tests
{
    /// <summary>
    /// A troca de espada do docs/03 secao 3: 0,7 s em que nada acontece e nada cancela.
    ///
    /// O teste que importa aqui e o da esquiva. A regra de ouro do docs/03 secao 1 deixa a
    /// esquiva cortar qualquer acao comprometida, e a secao 3 declara esta troca
    /// nao-cancelavel: e a unica excecao a excecao do jogo. Se ela se perder, esquivar
    /// vira o jeito de pagar meio preco pela troca e a camada 1 do combate, que e a
    /// decisao de maior impacto, deixa de custar.
    /// </summary>
    public sealed class WeaponSwapStateTests
    {
        const float Troca = 0.7f;
        const float Passo = 1f / 60f;

        PlayerStateContext _context;
        PlayerStateMachine _machine;
        WeaponSwapState _troca;
        PorteFalso _espadas;
        LocomocaoFalsa _locomocao;
        InputBuffer _buffer;

        [SetUp]
        public void SetUp()
        {
            _espadas = new PorteFalso();
            _locomocao = new LocomocaoFalsa();
            _buffer = new InputBuffer { Window = 0.2f };

            _context = new PlayerStateContext
            {
                Locomotion = _locomocao,
                Weapons = _espadas,
                Buffer = _buffer,
            };

            _machine = new PlayerStateMachine(_context);
            _troca = new WeaponSwapState(Troca);

            _machine.Register(new LocomotionState());
            _machine.Register(_troca);
            _machine.Start(PlayerStateId.Locomotion);
        }

        void Avancar(float segundos)
        {
            int passos = Mathf.RoundToInt(segundos / Passo);

            for (int i = 0; i < passos; i++)
                _machine.Tick(Passo);
        }

        void ComecarTroca(WeaponMaterial material)
        {
            _context.PendingWeapon = material;
            _machine.TryChangeState(PlayerStateId.SwapWeapon);
        }

        [Test]
        public void A_espada_so_troca_no_fim_dos_sete_decimos()
        {
            ComecarTroca(WeaponMaterial.Silver);

            Avancar(Troca - Passo * 2f);

            Assert.AreEqual(WeaponMaterial.Steel, _espadas.EquippedMaterial,
                "No meio da troca, a espada na mao ainda e a velha.");

            Avancar(Passo * 4f);

            Assert.AreEqual(WeaponMaterial.Silver, _espadas.EquippedMaterial);
            Assert.AreEqual(PlayerStateId.Locomotion, _machine.CurrentId, "Acabou a troca, volta a andar.");
        }

        [Test]
        public void Trocando_o_bruxo_nao_anda_nem_com_a_direcao_segurada()
        {
            // A direcao continua chegando do teclado o tempo todo. O que nao acontece e
            // alguem escrever ela na locomocao: a troca de espada para o bruxo, ao
            // contrario da troca de postura, que o docs/03 secao 4 permite andando.
            _context.MoveInput = Vector2.one;

            ComecarTroca(WeaponMaterial.Silver);
            Avancar(Troca * 0.5f);

            Assert.AreEqual(Vector2.zero, _locomocao.MoveInput);
        }

        /// <summary>
        /// A excecao a excecao. Ver o resumo da classe.
        /// </summary>
        [Test]
        public void Nem_a_esquiva_cancela_a_troca_de_espada()
        {
            ComecarTroca(WeaponMaterial.Silver);

            Assert.IsFalse(_troca.CanTransitionTo(PlayerStateId.Dodge),
                "docs/03 secao 3: a troca de espada nao pode ser cancelada.");
            Assert.IsFalse(_troca.CanTransitionTo(PlayerStateId.Roll));
        }

        [Test]
        public void O_mundo_ainda_interrompe_a_troca()
        {
            ComecarTroca(WeaponMaterial.Silver);

            Assert.IsTrue(_troca.CanTransitionTo(PlayerStateId.Stagger),
                "Atordoamento, morte e dialogo nao sao input do jogador.");
            Assert.IsTrue(_troca.CanTransitionTo(PlayerStateId.Death));
            Assert.IsTrue(_troca.CanTransitionTo(PlayerStateId.DialogueLocked));
        }

        [Test]
        public void Ataque_nao_sai_durante_a_troca()
        {
            ComecarTroca(WeaponMaterial.Silver);

            _buffer.Push(BufferedAction.AttackLight);
            Avancar(Troca * 0.5f);

            Assert.AreEqual(PlayerStateId.SwapWeapon, _machine.CurrentId);
        }

        [Test]
        public void Interromper_no_meio_deixa_a_espada_velha_na_mao()
        {
            ComecarTroca(WeaponMaterial.Silver);
            Avancar(Troca * 0.5f);

            _machine.ForceChangeState(PlayerStateId.Locomotion);
            Avancar(Troca);

            Assert.AreEqual(WeaponMaterial.Steel, _espadas.EquippedMaterial,
                "Interrompido com as duas espadas na mao, fica com a que ja estava.");
        }

        // ------------------------------------------------------------------ dublês

        sealed class PorteFalso : IWeaponHolder
        {
            public WeaponMaterial EquippedMaterial { get; private set; } = WeaponMaterial.Steel;

            public MeleeWeaponDef EquippedWeapon => null;

            public bool Has(WeaponMaterial material) => true;

            public void Equip(WeaponMaterial material) => EquippedMaterial = material;
        }

        sealed class LocomocaoFalsa : ILocomotionDriver
        {
            public Vector2 MoveInput { get; set; }
            public bool SprintHeld { get; set; }
            public float ReferenceYaw { get; set; }
        }
    }
}
