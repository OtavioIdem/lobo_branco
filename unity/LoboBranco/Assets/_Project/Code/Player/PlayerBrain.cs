using LoboBranco.CameraSystem;
using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// Dono da maquina de estados do jogador (docs/07 secao 4.4) e o unico componente que
    /// conhece input, movimento e camera ao mesmo tempo.
    ///
    /// A divisao de trabalho por frame:
    /// a camera e escrita direto na locomocao, porque yaw de camera e referencia e nao
    /// comando; o input vira campo do contexto e evento de botao vira entrada no buffer;
    /// e a partir dai quem decide o que acontece e o estado atual, nunca este componente.
    /// Nenhum <c>if</c> de combate mora aqui, e isso e o ponto da FSM.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerLocomotion))]
    public sealed class PlayerBrain : MonoBehaviour
    {
        [SerializeField] ThirdPersonCameraRig cameraRig;

        [Header("Dados")]
        [Tooltip("Janela do buffer de input. Sem asset, cai no padrao de 0,2 s do docs/07.")]
        [SerializeField] PlayerTuningDef tuning;

        [Header("Cursor")]
        [Tooltip("Trava e esconde o cursor durante o jogo. Desligue para depurar UI.")]
        [SerializeField] bool lockCursor = true;

        PlayerInputReader _input;
        PlayerLocomotion _locomotion;
        PlayerMeleeAttacker _attacker;

        PlayerStateContext _context;
        PlayerStateMachine _machine;
        InputBuffer _buffer;

        // ---------------------------------------------------------------- leitura

        /// <summary>Para o painel de debug. Ninguem de fora deve provocar transicao por aqui.</summary>
        public PlayerStateMachine Machine => _machine;

        public InputBuffer Buffer => _buffer;

        // ------------------------------------------------------------------ ciclo

        void Awake()
        {
            _input = GetComponent<PlayerInputReader>();
            _locomotion = GetComponent<PlayerLocomotion>();
            _attacker = GetComponent<PlayerMeleeAttacker>();

            ResolveCameraRig();
            BuildStateMachine();
        }

        void BuildStateMachine()
        {
            _buffer = new InputBuffer
            {
                Window = tuning != null ? tuning.inputBufferSeconds : 0.2f,
            };

            _context = new PlayerStateContext
            {
                Locomotion = _locomotion,
                Attacker = _attacker,
                Buffer = _buffer,
                LightAttack = _attacker != null ? _attacker.LightAttack : null,
                HeavyAttack = _attacker != null ? _attacker.HeavyAttack : null,
            };

            // O construtor da maquina escreve a si mesma no contexto.
            _machine = new PlayerStateMachine(_context);

            _machine.Register(new LocomotionState());
            _machine.Register(new AttackState());

            // Esquiva, rolamento, aparo e riposte sao as tarefas 1.10 e 1.11; sinais sao
            // a 1.18. Ate la a maquina avisa uma vez e ignora a transicao.

            _machine.Start(PlayerStateId.Locomotion);
        }

        void ResolveCameraRig()
        {
            if (cameraRig == null)
            {
                cameraRig = FindAnyObjectByType<ThirdPersonCameraRig>();

                if (cameraRig == null)
                    Debug.LogError($"{nameof(PlayerBrain)} nao encontrou um {nameof(ThirdPersonCameraRig)} na cena.", this);
            }

            if (cameraRig != null && cameraRig.FollowTarget == null)
                cameraRig.FollowTarget = transform;
        }

        void OnEnable()
        {
            _input.AttackLightPressed += OnAttackLight;
            _input.AttackHeavyPressed += OnAttackHeavy;
            _input.DodgePressed += OnDodge;
            _input.CastSignPressed += OnCastSign;
            _input.InteractPressed += OnInteract;
        }

        void OnDisable()
        {
            _input.AttackLightPressed -= OnAttackLight;
            _input.AttackHeavyPressed -= OnAttackHeavy;
            _input.DodgePressed -= OnDodge;
            _input.CastSignPressed -= OnCastSign;
            _input.InteractPressed -= OnInteract;

            // Sem isto, um ataque guardado sai sozinho quando o controle volta.
            _buffer?.Clear();
        }

        void Start()
        {
            if (cameraRig != null)
                cameraRig.AlignBehindTarget();

            SetCursorLocked(lockCursor);
        }

        void Update()
        {
            float deltaTime = Time.deltaTime;

            if (cameraRig != null)
            {
                cameraRig.ApplyLook(_input.Look);

                // Yaw de camera e referencia de direcao, nao comando: por isso ele nao
                // passa pelos estados. Durante um ataque o movimento e zero de qualquer
                // forma, entao escrever aqui nao fura o compromisso do golpe.
                _locomotion.ReferenceYaw = cameraRig.Yaw;
                _context.ReferenceYaw = cameraRig.Yaw;
            }

            _context.MoveInput = _input.Move;
            _context.SprintHeld = _input.SprintHeld;
            _context.ParryHeld = _input.ParryHeld;

            _machine.Tick(deltaTime);

            // Envelhecer o buffer depois da maquina: assim um input que chegou neste frame
            // e avaliado com a janela inteira, em vez de nascer ja com um frame gasto.
            _buffer.Tick(deltaTime);
        }

        // -------------------------------------------------------------- handlers

        void OnAttackLight() => _buffer.Push(BufferedAction.AttackLight);
        void OnAttackHeavy() => _buffer.Push(BufferedAction.AttackHeavy);
        void OnDodge() => _buffer.Push(BufferedAction.Dodge);
        void OnCastSign() => _buffer.Push(BufferedAction.CastSign);
        void OnInteract() => _buffer.Push(BufferedAction.Interact);

        void OnApplicationFocus(bool hasFocus)
        {
            // Sem isso, alt-tab devolve o foco com o cursor solto e a camera para de girar.
            if (lockCursor)
                SetCursorLocked(hasFocus);
        }

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
