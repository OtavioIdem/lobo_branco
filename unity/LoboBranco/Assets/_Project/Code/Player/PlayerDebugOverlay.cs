using LoboBranco.CameraSystem;
using LoboBranco.Combat;
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
        [SerializeField] PlayerBrain brain;
        [SerializeField] PlayerMeleeAttacker attacker;
        [SerializeField] CharacterVitals vitals;

        [Tooltip("F1 alterna o painel em tempo de execucao.")]
        [SerializeField] bool visible = true;

        GUIStyle _style;
        float _fps;

        void Awake()
        {
            if (locomotion == null) locomotion = GetComponent<PlayerLocomotion>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (brain == null) brain = GetComponent<PlayerBrain>();
            if (attacker == null) attacker = GetComponent<PlayerMeleeAttacker>();
            if (vitals == null) vitals = GetComponent<CharacterVitals>();
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

            GUILayout.BeginArea(new Rect(12f, 12f, 420f, 420f), GUI.skin.box);
            GUILayout.Label("SANDBOX  —  F1 esconde", _style);
            GUILayout.Space(4f);

            GUILayout.Label($"FPS               {_fps,6:F0}", _style);

            // Quem escreve esta vida e o host (ADR 0008). Mostrar de onde ela vem junto
            // com o numero e o que separa "levei dano" de "o host acha que levei".
            if (vitals != null)
                GUILayout.Label(
                    $"Vida              {vitals.CurrentVitality,6:F0} / {vitals.MaxVitality:F0}   " +
                    (vitals.CanResolve ? "[eu resolvo]" : "[o host manda]"),
                    _style);

            DrawStateMachine();

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
            GUILayout.Label("Botao esq. golpe leve | dir. golpe forte", _style);

            if (attacker != null && !string.IsNullOrEmpty(attacker.LastHitSummary))
            {
                GUILayout.Space(4f);
                GUILayout.Label("Ultimo golpe:", _style);
                GUILayout.Label(attacker.LastHitSummary, _style);
            }

            GUILayout.EndArea();
        }

        /// <summary>
        /// Estado, fase do golpe e buffer. Sao os tres numeros que dizem se o combate esta
        /// respondendo: um input que some sem virar transicao aparece aqui como uma acao
        /// guardada que expira sozinha.
        /// </summary>
        void DrawStateMachine()
        {
            PlayerStateMachine machine = brain != null ? brain.Machine : null;
            if (machine == null) return;

            GUILayout.Label($"Estado            {machine.CurrentId,-14} {machine.TimeInState,5:F2}s", _style);

            if (machine.Current is AttackState attackState && attackState.CurrentAttack != null)
            {
                GUILayout.Label(
                    $"Golpe             {attackState.CurrentAttack.name} ({attackState.CurrentAttack.stance})", _style);
                GUILayout.Label(
                    $"Fase              {attackState.CurrentPhase,-14} hitbox {(attackState.HitboxOpen ? "ABERTA" : "fechada")}",
                    _style);
            }

            InputBuffer buffer = brain.Buffer;
            if (buffer != null)
                GUILayout.Label($"Buffer            {buffer.Pending,-14} {buffer.Remaining,5:F2}s", _style);

            if (attacker != null)
                GUILayout.Label($"Alvos no golpe    {attacker.HitsThisSwing,6}", _style);
        }
    }
}
