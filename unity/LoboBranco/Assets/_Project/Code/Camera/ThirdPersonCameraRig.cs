using UnityEngine;

namespace LoboBranco.CameraSystem
{
    /// <summary>
    /// Pivo da camera de terceira pessoa. Este componente nao le input: ele recebe
    /// deltas por <see cref="ApplyLook"/>, o que o torna testavel e reutilizavel
    /// (cutscene, camera de dialogo, camera livre de debug usam o mesmo pivo).
    ///
    /// O pivo carrega apenas posicao e rotacao. O enquadramento, o amortecimento e o
    /// desvio de parede sao responsabilidade da CinemachineCamera que rastreia este
    /// transform. Ver docs/07_ARQUITETURA_TECNICA.md secao 7.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThirdPersonCameraRig : MonoBehaviour
    {
        [Header("Alvo")]
        [Tooltip("Transform que o pivo segue, normalmente a raiz do jogador.")]
        [SerializeField] Transform followTarget;

        [Tooltip("Altura do pivo acima da base do alvo. 1.6 fica na altura dos olhos.")]
        [SerializeField] float pivotHeight = 1.6f;

        [Header("Sensibilidade")]
        [SerializeField, Range(0.1f, 10f)] float yawSensitivity = 2.2f;
        [SerializeField, Range(0.1f, 10f)] float pitchSensitivity = 1.8f;
        [SerializeField] bool invertPitch;

        [Header("Limites de pitch")]
        [Tooltip("Olhando para baixo. Positivo em graus, no sistema da Unity.")]
        [SerializeField, Range(0f, 89f)] float pitchMax = 70f;

        [Tooltip("Olhando para cima.")]
        [SerializeField, Range(-89f, 0f)] float pitchMin = -35f;

        float _yaw;
        float _pitch = 12f;

        /// <summary>Rotacao horizontal atual, em graus. O movimento do jogador usa isto.</summary>
        public float Yaw => _yaw;

        /// <summary>Rotacao vertical atual, em graus.</summary>
        public float Pitch => _pitch;

        /// <summary>Direcao horizontal para onde a camera aponta, normalizada e no plano XZ.</summary>
        public Vector3 PlanarForward
        {
            get
            {
                var forward = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
                return forward.normalized;
            }
        }

        public Transform FollowTarget
        {
            get => followTarget;
            set => followTarget = value;
        }

        void Awake()
        {
            if (followTarget == null)
                Debug.LogWarning($"{nameof(ThirdPersonCameraRig)} sem followTarget. O pivo vai ficar parado.", this);

            SnapToTarget();
        }

        /// <summary>
        /// Acumula um delta de olhar. Chame uma vez por frame, com o valor ja escalado
        /// pelo processador do Input System.
        /// </summary>
        public void ApplyLook(Vector2 delta)
        {
            _yaw += delta.x * yawSensitivity;
            _yaw = Mathf.Repeat(_yaw, 360f);

            float pitchDelta = delta.y * pitchSensitivity * (invertPitch ? 1f : -1f);
            _pitch = Mathf.Clamp(_pitch + pitchDelta, pitchMin, pitchMax);
        }

        /// <summary>Coloca o pivo no alvo imediatamente, sem interpolacao. Usar em teleporte e ao carregar cena.</summary>
        public void SnapToTarget()
        {
            if (followTarget == null) return;

            transform.position = followTarget.position + Vector3.up * pivotHeight;
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        /// <summary>Reposiciona a camera atras do alvo, alinhada com a direcao em que ele encara.</summary>
        public void AlignBehindTarget()
        {
            if (followTarget == null) return;

            _yaw = followTarget.eulerAngles.y;
            SnapToTarget();
        }

        // LateUpdate para rodar depois de todo movimento do jogador no frame.
        void LateUpdate()
        {
            if (followTarget == null) return;

            transform.position = followTarget.position + Vector3.up * pivotHeight;
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
    }
}
