using LoboBranco.CameraSystem;
using UnityEngine.InputSystem;
using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// Painel de debug da cena de sandbox (docs/11_SETUP_AMBIENTE.md secao 6).
    /// Usa IMGUI de proposito: e feio, e zero esforco, e nao concorre com a UI de verdade
    /// que sera feita em UI Toolkit. Nao vai para o jogo final.
    ///
    /// Conforme os sistemas de vitalidade, vigor, toxicidade e postura entrarem
    /// (M1 e M2), cada um ganha uma linha aqui.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerDebugOverlay : MonoBehaviour
    {
        [SerializeField] PlayerLocomotion locomotion;
        [SerializeField] ThirdPersonCameraRig cameraRig;
        [SerializeField] PlayerInputReader input;

        [Tooltip("F1 alterna o painel em tempo de execucao.")]
        [SerializeField] bool visible = true;

        GUIStyle _style;
        float _fps;

        void Awake()
        {
            if (locomotion == null) locomotion = GetComponent<PlayerLocomotion>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (cameraRig == null) cameraRig = FindAnyObjectByType<ThirdPersonCameraRig>();
        }

        void Update()
        {
            // Media exponencial: um contador cru de 1/deltaTime tremula demais para ser lido.
            _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.1f);

            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
                visible = !visible;
        }

        void OnGUI()
        {
            if (!visible) return;

            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = Color.white },
            };

            GUILayout.BeginArea(new Rect(12f, 12f, 340f, 260f), GUI.skin.box);
            GUILayout.Label("SANDBOX  —  F1 esconde", _style);
            GUILayout.Space(4f);

            GUILayout.Label($"FPS               {_fps,6:F0}", _style);

            if (locomotion != null)
            {
                GUILayout.Label($"Velocidade        {locomotion.CurrentSpeed,6:F2} m/s", _style);
                GUILayout.Label($"No chao           {locomotion.IsGrounded,6}", _style);
                GUILayout.Label($"Posicao           {locomotion.transform.position.x,6:F1}, {locomotion.transform.position.z,5:F1}", _style);
            }

            if (cameraRig != null)
            {
                GUILayout.Label($"Camera yaw        {cameraRig.Yaw,6:F0} deg", _style);
                GUILayout.Label($"Camera pitch      {cameraRig.Pitch,6:F0} deg", _style);
            }

            if (input != null)
            {
                GUILayout.Label($"Move              {input.Move.x,6:F2}, {input.Move.y,5:F2}", _style);
                GUILayout.Label($"Sprint            {input.SprintHeld,6}", _style);
            }

            GUILayout.Space(4f);
            GUILayout.Label("WASD mover | mouse camera | Shift correr", _style);

            GUILayout.EndArea();
        }
    }
}
