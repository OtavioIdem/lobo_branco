using LoboBranco.CameraSystem;
using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// Liga input, movimento e camera. E o unico componente que conhece os tres.
    ///
    /// Hoje ele so repassa valores. Quando a FSM do doc 07 secao 4.4 entrar, e aqui que
    /// ela vive, e o repasse passa a ser condicionado ao estado atual: um estado de
    /// ataque, por exemplo, para de escrever em MoveInput.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerLocomotion))]
    public sealed class PlayerBrain : MonoBehaviour
    {
        [SerializeField] ThirdPersonCameraRig cameraRig;

        [Header("Cursor")]
        [Tooltip("Trava e esconde o cursor durante o jogo. Desligue para depurar UI.")]
        [SerializeField] bool lockCursor = true;

        PlayerInputReader _input;
        PlayerLocomotion _locomotion;

        void Awake()
        {
            _input = GetComponent<PlayerInputReader>();
            _locomotion = GetComponent<PlayerLocomotion>();

            if (cameraRig == null)
            {
                cameraRig = FindAnyObjectByType<ThirdPersonCameraRig>();

                if (cameraRig == null)
                    Debug.LogError($"{nameof(PlayerBrain)} nao encontrou um {nameof(ThirdPersonCameraRig)} na cena.", this);
            }

            if (cameraRig != null && cameraRig.FollowTarget == null)
                cameraRig.FollowTarget = transform;
        }

        void Start()
        {
            if (cameraRig != null)
                cameraRig.AlignBehindTarget();

            SetCursorLocked(lockCursor);
        }

        void Update()
        {
            if (cameraRig != null)
            {
                cameraRig.ApplyLook(_input.Look);
                _locomotion.ReferenceYaw = cameraRig.Yaw;
            }

            _locomotion.MoveInput = _input.Move;
            _locomotion.SprintHeld = _input.SprintHeld;
        }

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
