using LoboBranco.CameraSystem;
using Unity.Netcode;
using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// Autoridade: o dono (ADR 0008). Quem simula este personagem e a maquina de quem o
    /// controla, e mais ninguem. O host nao corrige movimento, e o cliente nao declara
    /// dano: quando o golpe conectar, quem resolve e o host (tarefa 1.9f).
    ///
    /// Dono da maquina de estados do jogador (docs/07 secao 4.4) e o unico componente que
    /// conhece input, movimento e camera ao mesmo tempo.
    ///
    /// A divisao de trabalho por frame:
    /// a camera e escrita direto na locomocao, porque yaw de camera e referencia e nao
    /// comando; o input vira campo do contexto e evento de botao vira entrada no buffer;
    /// e a partir dai quem decide o que acontece e o estado atual, nunca este componente.
    /// Nenhum <c>if</c> de combate mora aqui, e isso e o ponto da FSM.
    ///
    /// Sem rede ligada, o jogador local e dono de si mesmo. Isso e deliberado: o risco X8
    /// do doc 13 diz que a rede nao pode virar pre-requisito para testar combate, entao a
    /// Sandbox_Combate continua jogavel sozinha, sem host nenhum.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerLocomotion))]
    public sealed class PlayerBrain : NetworkBehaviour
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
        PlayerDebugOverlay _overlay;

        PlayerStateContext _context;
        PlayerStateMachine _machine;
        InputBuffer _buffer;
        StanceSelector _stance;

        bool _drivesThisCharacter;
        bool _controlDecided;

        // ---------------------------------------------------------------- leitura

        /// <summary>Para o painel de debug. Ninguem de fora deve provocar transicao por aqui.</summary>
        public PlayerStateMachine Machine => _machine;

        public InputBuffer Buffer => _buffer;

        /// <summary>Postura corrente e a troca em andamento. O painel de debug mostra isto.</summary>
        public StanceSelector Stance => _stance;

        /// <summary>Verdadeiro no personagem que esta maquina controla. Falso nos companheiros.</summary>
        public bool DrivesThisCharacter => _drivesThisCharacter;

        // ------------------------------------------------------------------ ciclo

        void Awake()
        {
            _input = GetComponent<PlayerInputReader>();
            _locomotion = GetComponent<PlayerLocomotion>();
            _attacker = GetComponent<PlayerMeleeAttacker>();
            _overlay = GetComponent<PlayerDebugOverlay>();

            BuildStateMachine();
        }

        /// <summary>
        /// Roda depois de <c>OnNetworkSpawn</c> quando existe rede, e e o unico caminho
        /// quando nao existe. Por isso ele so decide se ninguem decidiu antes.
        /// </summary>
        void Start()
        {
            if (!_controlDecided)
                TakeControl();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner) TakeControl();
            else ReleaseControl();
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;

            _buffer?.Clear();
            SetCursorLocked(false);
        }

        // ------------------------------------------------------------ autoridade

        /// <summary>Liga input, movimento, camera e cursor. So no personagem que e meu.</summary>
        void TakeControl()
        {
            _controlDecided = true;
            _drivesThisCharacter = true;

            if (_input != null) _input.enabled = true;
            if (_locomotion != null) _locomotion.enabled = true;
            if (_overlay != null) _overlay.enabled = true;

            ResolveCameraRig();

            if (cameraRig != null)
            {
                cameraRig.FollowTarget = transform;
                cameraRig.AlignBehindTarget();
            }

            SetCursorLocked(lockCursor);
        }

        /// <summary>
        /// Desliga o que so faz sentido no dono. O <see cref="PlayerMeleeAttacker"/>
        /// continua ligado de proposito: no host, e ele quem vai resolver o golpe do
        /// companheiro na tarefa 1.9f, e a FSM parada garante que ele nao age sozinho.
        /// </summary>
        void ReleaseControl()
        {
            _controlDecided = true;
            _drivesThisCharacter = false;

            if (_input != null) _input.enabled = false;
            if (_overlay != null) _overlay.enabled = false;

            // Sem isto, a gravidade do CharacterController briga com a posicao que chega
            // pela rede e o companheiro fica tremendo no chao.
            if (_locomotion != null) _locomotion.enabled = false;
        }

        void BuildStateMachine()
        {
            _buffer = new InputBuffer
            {
                Window = tuning != null ? tuning.inputBufferSeconds : 0.2f,
            };

            _stance = new StanceSelector(tuning != null ? tuning.stanceSwitchSeconds : 0.25f);

            _context = new PlayerStateContext
            {
                Locomotion = _locomotion,
                Attacker = _attacker,
                Buffer = _buffer,
            };

            WriteStanceToContext();

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
                // Prefab de rede nao guarda referencia de cena, entao o dono acha o proprio
                // pivo aqui. Cada instancia do jogo tem um, e so o dono encosta nele.
                cameraRig = FindAnyObjectByType<ThirdPersonCameraRig>();

                if (cameraRig == null)
                    Debug.LogError($"{nameof(PlayerBrain)} nao encontrou um {nameof(ThirdPersonCameraRig)} na cena.", this);
            }
        }

        void OnEnable()
        {
            _input.AttackLightPressed += OnAttackLight;
            _input.AttackHeavyPressed += OnAttackHeavy;
            _input.DodgePressed += OnDodge;
            _input.CastSignPressed += OnCastSign;
            _input.InteractPressed += OnInteract;
            _input.StanceCycled += OnStanceCycled;
        }

        void OnDisable()
        {
            _input.AttackLightPressed -= OnAttackLight;
            _input.AttackHeavyPressed -= OnAttackHeavy;
            _input.DodgePressed -= OnDodge;
            _input.CastSignPressed -= OnCastSign;
            _input.InteractPressed -= OnInteract;
            _input.StanceCycled -= OnStanceCycled;

            // Sem isto, um ataque guardado sai sozinho quando o controle volta.
            _buffer?.Clear();
        }

        void Update()
        {
            // O companheiro e desenhado pela posicao que chega do dono dele. Simular a FSM
            // aqui seria simular o mesmo personagem duas vezes, em duas maquinas.
            if (!_drivesThisCharacter) return;

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

            // A troca de postura corre em paralelo com tudo: ela pode acontecer andando
            // (docs/03 secao 4), e o que ela nao pode e comecar durante um golpe, o que
            // ja foi decidido no momento do input.
            _stance.Tick(deltaTime);
            WriteStanceToContext();

            _machine.Tick(deltaTime);

            // Envelhecer o buffer depois da maquina: assim um input que chegou neste frame
            // e avaliado com a janela inteira, em vez de nascer ja com um frame gasto.
            _buffer.Tick(deltaTime);
        }

        // -------------------------------------------------------------- handlers

        /// <summary>
        /// A roda de postura nao passa pelo buffer de input, e isso e deliberado: o buffer
        /// existe para um input chegar cedo demais e ainda valer, e trocar de postura cedo
        /// demais nao e erro de timing do jogador, e sim tentar trocar no meio de um golpe.
        /// Guardar essa troca faria a postura mudar sozinha depois, que e o oposto de uma
        /// decisao tomada.
        /// </summary>
        void OnStanceCycled(int direction)
        {
            if (!PlayerStateRules.CanSwitchStance(_machine.Current?.IsCommitted ?? false)) return;

            _stance.Cycle(direction);
        }

        void WriteStanceToContext()
        {
            _context.CurrentStance = _stance.Current;
            _context.CurrentAttack = _attacker != null ? _attacker.AttackFor(_stance.Current) : null;
        }

        void OnAttackLight() => _buffer.Push(BufferedAction.AttackLight);
        void OnAttackHeavy() => _buffer.Push(BufferedAction.AttackHeavy);
        void OnDodge() => _buffer.Push(BufferedAction.Dodge);
        void OnCastSign() => _buffer.Push(BufferedAction.CastSign);
        void OnInteract() => _buffer.Push(BufferedAction.Interact);

        void OnApplicationFocus(bool hasFocus)
        {
            // Sem isso, alt-tab devolve o foco com o cursor solto e a camera para de girar.
            if (lockCursor && _drivesThisCharacter)
                SetCursorLocked(hasFocus);
        }

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
