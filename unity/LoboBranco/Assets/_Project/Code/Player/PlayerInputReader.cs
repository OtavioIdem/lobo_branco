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
    }
}
