using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LoboBranco.Player
{
    /// <summary>
    /// Unico ponto do jogo que fala com o Input System. Todo o resto le propriedades
    /// e escuta eventos daqui.
    ///
    /// Motivo: quando a FSM do jogador existir (doc 07 secao 4.4) ela precisa de um
    /// buffer de input de 0,2 s. Com a leitura centralizada, o buffer entra em um lugar
    /// so. Espalhar chamadas de InputAction pelo codigo torna isso impossivel.
    ///
    /// Acoes definidas em Assets/_Project/Settings/PlayerControls.inputactions,
    /// conforme docs/02_GDD.md secao 4.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] InputActionAsset actions;

        const string MapName = "Player";

        // Abaixo disto o eixo e ruido ou volta do direcional ao centro, e nao um pedido.
        const float DeadZone = 0.5f;

        InputActionMap _map;

        InputAction _move;
        InputAction _look;
        InputAction _sprint;
        InputAction _attackLight;
        InputAction _attackHeavy;
        InputAction _dodge;
        InputAction _parry;
        InputAction _castSign;
        InputAction _interact;
        InputAction _witcherSenses;
        InputAction _cycleStance;
        InputAction _switchSteel;
        InputAction _switchSilver;

        // ---------------------------------------------------------------- estado

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool ParryHeld { get; private set; }

        // ---------------------------------------------------------------- eventos

        public event Action AttackLightPressed;
        public event Action AttackHeavyPressed;
        public event Action DodgePressed;
        public event Action CastSignPressed;
        public event Action InteractPressed;
        public event Action WitcherSensesPressed;

        /// <summary>Roda de postura. Positivo avanca, negativo volta (docs/02 secao 4).</summary>
        public event Action<int> StanceCycled;

        /// <summary>Pedido de espada de aco. Quem decide se da e a maquina de estados.</summary>
        public event Action SwitchSteelPressed;

        /// <summary>Pedido de espada de prata.</summary>
        public event Action SwitchSilverPressed;

        // ---------------------------------------------------------------- ciclo

        void Awake()
        {
            if (actions == null)
            {
                Debug.LogError($"{nameof(PlayerInputReader)} sem InputActionAsset atribuido.", this);
                enabled = false;
                return;
            }

            _map = actions.FindActionMap(MapName, throwIfNotFound: true);

            _move = _map.FindAction("Move", true);
            _look = _map.FindAction("Look", true);
            _sprint = _map.FindAction("Sprint", true);
            _attackLight = _map.FindAction("AttackLight", true);
            _attackHeavy = _map.FindAction("AttackHeavy", true);
            _dodge = _map.FindAction("Dodge", true);
            _parry = _map.FindAction("Parry", true);
            _castSign = _map.FindAction("CastSign", true);
            _interact = _map.FindAction("Interact", true);
            _witcherSenses = _map.FindAction("WitcherSenses", true);
            _cycleStance = _map.FindAction("CycleStance", true);
            _switchSteel = _map.FindAction("SwitchSteel", true);
            _switchSilver = _map.FindAction("SwitchSilver", true);
        }

        void OnEnable()
        {
            if (_map == null) return;

            _attackLight.performed += OnAttackLight;
            _attackHeavy.performed += OnAttackHeavy;
            _dodge.performed += OnDodge;
            _castSign.performed += OnCastSign;
            _interact.performed += OnInteract;
            _witcherSenses.performed += OnWitcherSenses;
            _cycleStance.performed += OnCycleStance;
            _switchSteel.performed += OnSwitchSteel;
            _switchSilver.performed += OnSwitchSilver;

            _map.Enable();
        }

        void OnDisable()
        {
            if (_map == null) return;

            _attackLight.performed -= OnAttackLight;
            _attackHeavy.performed -= OnAttackHeavy;
            _dodge.performed -= OnDodge;
            _castSign.performed -= OnCastSign;
            _interact.performed -= OnInteract;
            _witcherSenses.performed -= OnWitcherSenses;
            _cycleStance.performed -= OnCycleStance;
            _switchSteel.performed -= OnSwitchSteel;
            _switchSilver.performed -= OnSwitchSilver;

            _map.Disable();

            Move = Vector2.zero;
            Look = Vector2.zero;
            SprintHeld = false;
            ParryHeld = false;
        }

        void Update()
        {
            Move = _move.ReadValue<Vector2>();
            Look = _look.ReadValue<Vector2>();
            SprintHeld = _sprint.IsPressed();
            ParryHeld = _parry.IsPressed();
        }

        // ---------------------------------------------------------------- handlers

        void OnAttackLight(InputAction.CallbackContext _) => AttackLightPressed?.Invoke();
        void OnAttackHeavy(InputAction.CallbackContext _) => AttackHeavyPressed?.Invoke();
        void OnDodge(InputAction.CallbackContext _) => DodgePressed?.Invoke();
        void OnCastSign(InputAction.CallbackContext _) => CastSignPressed?.Invoke();
        void OnInteract(InputAction.CallbackContext _) => InteractPressed?.Invoke();
        void OnWitcherSenses(InputAction.CallbackContext _) => WitcherSensesPressed?.Invoke();
        void OnSwitchSteel(InputAction.CallbackContext _) => SwitchSteelPressed?.Invoke();
        void OnSwitchSilver(InputAction.CallbackContext _) => SwitchSilverPressed?.Invoke();

        /// <summary>
        /// A acao e um eixo, e nao um botao, porque roda de mouse e direcional sao eixos.
        /// O valor bruto da roda vem em degraus grandes no Windows, entao o que interessa
        /// e o sinal e nao a magnitude. A zona morta existe para o retorno do direcional
        /// ao centro nao contar como uma troca a mais.
        /// </summary>
        void OnCycleStance(InputAction.CallbackContext context)
        {
            float axis = context.ReadValue<float>();

            if (axis > DeadZone) StanceCycled?.Invoke(1);
            else if (axis < -DeadZone) StanceCycled?.Invoke(-1);
        }
    }
}
