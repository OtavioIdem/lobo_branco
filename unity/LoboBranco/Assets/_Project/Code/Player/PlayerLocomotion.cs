using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// Movimento do jogador. Deliberadamente sem nenhuma referencia ao Input System
    /// nem a camera: recebe <see cref="MoveInput"/> e <see cref="ReferenceYaw"/> de fora.
    ///
    /// Isso e o que permite testar movimento sem simular teclado, e e o que vai permitir
    /// que a FSM (doc 07 secao 4.4) assuma o controle depois: um estado de esquiva
    /// simplesmente escreve em MoveInput.
    ///
    /// Escalas e velocidades vem de docs/08_PIPELINE_ARTE_E_AUDIO.md secao 2 e sao
    /// fixas para o projeto inteiro. Mudar aqui muda o tamanho de todos os cenarios.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerLocomotion : MonoBehaviour
    {
        [Header("Velocidade (m/s)")]
        [SerializeField] float walkSpeed = 2.0f;
        [SerializeField] float runSpeed = 5.5f;

        [Tooltip("Segundos para atingir a velocidade alvo. Baixo demais parece robotico, alto demais parece patinar.")]
        [SerializeField] float speedSmoothTime = 0.12f;

        [Header("Rotacao")]
        [Tooltip("Segundos para completar o giro na direcao do movimento.")]
        [SerializeField] float turnSmoothTime = 0.10f;

        [Header("Gravidade")]
        [SerializeField] float gravity = -19.62f;

        [Tooltip("Forca constante contra o chao. Sem isso o CharacterController perde contato em rampas e descidas.")]
        [SerializeField] float groundedStick = -2f;

        // ---------------------------------------------------------------- entradas

        /// <summary>Eixo de movimento bruto, de -1 a 1 em cada componente.</summary>
        public Vector2 MoveInput { get; set; }

        /// <summary>Verdadeiro para usar <see cref="runSpeed"/> em vez de <see cref="walkSpeed"/>.</summary>
        public bool SprintHeld { get; set; }

        /// <summary>
        /// Yaw da referencia de movimento, em graus. Normalmente o yaw da camera, para
        /// que "W" signifique "para onde eu estou olhando".
        /// </summary>
        public float ReferenceYaw { get; set; }

        // ---------------------------------------------------------------- leitura

        /// <summary>Velocidade horizontal atual, em m/s. A animacao vai ler isto.</summary>
        public float CurrentSpeed => _currentSpeed;

        /// <summary>Fracao de 0 a 1 entre parado e corrida. Util para blend tree.</summary>
        public float NormalizedSpeed => runSpeed > 0f ? _currentSpeed / runSpeed : 0f;

        public bool IsGrounded => _controller.isGrounded;

        // ---------------------------------------------------------------- interno

        CharacterController _controller;
        float _currentSpeed;
        float _speedSmoothVelocity;
        float _turnSmoothVelocity;
        float _verticalVelocity;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Um passo de simulacao com delta explicito.
        ///
        /// O tempo entra por parametro pelo mesmo motivo que input e camera entram por
        /// propriedade: <c>Time.deltaTime</c> e estado ambiente, e depender dele torna o
        /// comportamento impossivel de reproduzir. Com o delta explicito, um teste roda
        /// 60 passos de 1/60 e obtem exatamente um segundo de movimento, toda vez.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            Vector2 input = Vector2.ClampMagnitude(MoveInput, 1f);
            float targetSpeed = (SprintHeld ? runSpeed : walkSpeed) * input.magnitude;

            _currentSpeed = Mathf.SmoothDamp(
                _currentSpeed, targetSpeed, ref _speedSmoothVelocity, speedSmoothTime, Mathf.Infinity, deltaTime);

            Vector3 planarDirection = Vector3.zero;

            if (input.sqrMagnitude > 0.0001f)
            {
                // Angulo do input somado ao yaw da referencia: e o que faz o movimento
                // ser relativo a camera e nao ao mundo.
                float targetAngle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg + ReferenceYaw;

                float smoothedAngle = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity,
                    turnSmoothTime, Mathf.Infinity, deltaTime);

                transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);
                planarDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            }

            ApplyGravity(deltaTime);

            Vector3 velocity = planarDirection * _currentSpeed + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * deltaTime);
        }

        void ApplyGravity(float deltaTime)
        {
            if (_controller.isGrounded && _verticalVelocity <= 0f)
                _verticalVelocity = groundedStick;
            else
                _verticalVelocity += gravity * deltaTime;
        }

        /// <summary>Zera a inercia. Chamar em teleporte, morte e troca de cena.</summary>
        public void ResetMotion()
        {
            _currentSpeed = 0f;
            _speedSmoothVelocity = 0f;
            _turnSmoothVelocity = 0f;
            _verticalVelocity = 0f;
        }

        /// <summary>Teleporta sem que o CharacterController lute contra a colisao.</summary>
        public void Teleport(Vector3 position, float yaw)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            _controller.enabled = true;
            ResetMotion();
        }
    }
}
