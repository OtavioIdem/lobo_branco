using System.IO;
using System.Linq;
using LoboBranco.CameraSystem;
using LoboBranco.Core;
using LoboBranco.Player;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LoboBranco.EditorTools
{
    /// <summary>
    /// Monta a hierarquia da cena de sandbox por codigo, em vez de a mao.
    /// Motivo: cena montada a mao nao e revisavel em diff e nao e reproduzivel.
    /// Rodar de novo reconstroi tudo do zero.
    /// </summary>
    public static class SandboxSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/Sandbox_Combate.unity";
        const string ControlsPath = "Assets/_Project/Settings/PlayerControls.inputactions";

        // Escalas fixas do projeto (docs/08_PIPELINE_ARTE_E_AUDIO.md secao 2).
        const float PlayerHeight = 1.85f;
        const float PlayerRadius = 0.3f;
        const float StepOffset = 0.4f;
        const float CameraDistance = 4.5f;

        [MenuItem("Lobo Branco/Setup/5. Montar sandbox de combate")]
        public static void BuildSandbox()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            DestroyIfPresent("Player");
            DestroyIfPresent("CameraPivot");
            DestroyIfPresent("CM_Exploration");

            var player = CreatePlayer();
            var pivot = CreateCameraPivot(player.transform);
            CreateVirtualCamera(pivot.transform);
            EnsureBrainOnMainCamera();
            WirePlayer(player, pivot.GetComponent<ThirdPersonCameraRig>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log("[Sandbox] Hierarquia montada e cena salva.");
        }

        // ------------------------------------------------------------- jogador

        static GameObject CreatePlayer()
        {
            var player = new GameObject("Player")
            {
                layer = LayerMask.NameToLayer("Player"),
            };
            player.transform.position = new Vector3(0f, 0.1f, -8f);

            var controller = player.AddComponent<CharacterController>();
            controller.height = PlayerHeight;
            controller.radius = PlayerRadius;
            controller.center = new Vector3(0f, PlayerHeight * 0.5f, 0f);
            controller.stepOffset = StepOffset;
            controller.slopeLimit = 50f;
            controller.skinWidth = 0.02f;
            controller.minMoveDistance = 0f;

            player.AddComponent<PlayerInputReader>();
            player.AddComponent<PlayerLocomotion>();
            player.AddComponent<PlayerBrain>();
            player.AddComponent<PlayerDebugOverlay>();

            // Corpo greybox: capsula da altura certa, sem collider proprio
            // (quem colide e o CharacterController).
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body_Greybox";
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0f, PlayerHeight * 0.5f, 0f);
            body.transform.localScale = new Vector3(
                PlayerRadius * 2f, PlayerHeight * 0.5f, PlayerRadius * 2f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.layer = player.layer;

            // Indicador de frente. Sem isso e impossivel julgar a rotacao em greybox.
            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Facing_Greybox";
            nose.transform.SetParent(player.transform, false);
            nose.transform.localPosition = new Vector3(0f, PlayerHeight * 0.8f, PlayerRadius + 0.1f);
            nose.transform.localScale = new Vector3(0.12f, 0.12f, 0.3f);
            Object.DestroyImmediate(nose.GetComponent<Collider>());
            nose.layer = player.layer;

            return player;
        }

        static void WirePlayer(GameObject player, ThirdPersonCameraRig rig)
        {
            var controls = AssetDatabase.LoadAssetAtPath<Object>(ControlsPath);
            if (controls == null)
                Debug.LogError($"[Sandbox] Nao achei {ControlsPath}.");

            var reader = new SerializedObject(player.GetComponent<PlayerInputReader>());
            reader.FindProperty("actions").objectReferenceValue = controls;
            reader.ApplyModifiedPropertiesWithoutUndo();

            var brain = new SerializedObject(player.GetComponent<PlayerBrain>());
            brain.FindProperty("cameraRig").objectReferenceValue = rig;
            brain.ApplyModifiedPropertiesWithoutUndo();

            var overlay = new SerializedObject(player.GetComponent<PlayerDebugOverlay>());
            overlay.FindProperty("locomotion").objectReferenceValue = player.GetComponent<PlayerLocomotion>();
            overlay.FindProperty("input").objectReferenceValue = player.GetComponent<PlayerInputReader>();
            overlay.FindProperty("cameraRig").objectReferenceValue = rig;
            overlay.ApplyModifiedPropertiesWithoutUndo();
        }

        // -------------------------------------------------------------- camera

        static GameObject CreateCameraPivot(Transform followTarget)
        {
            var pivot = new GameObject("CameraPivot");
            var rig = pivot.AddComponent<ThirdPersonCameraRig>();

            var so = new SerializedObject(rig);
            so.FindProperty("followTarget").objectReferenceValue = followTarget;
            so.ApplyModifiedPropertiesWithoutUndo();

            return pivot;
        }

        static void CreateVirtualCamera(Transform pivot)
        {
            var go = new GameObject("CM_Exploration");
            var cam = go.AddComponent<CinemachineCamera>();

            cam.Target = new CameraTarget
            {
                TrackingTarget = pivot,
            };
            cam.Lens.FieldOfView = 55f;

            // Corpo: fica atras do pivo e herda a rotacao dele, com amortecimento.
            var follow = go.AddComponent<CinemachineFollow>();
            follow.FollowOffset = new Vector3(0f, 0f, -CameraDistance);
            follow.TrackerSettings.BindingMode = BindingMode.LockToTargetNoRoll;
            follow.TrackerSettings.PositionDamping = new Vector3(0.25f, 0.25f, 0.25f);

            // Mira: aponta direto para o pivo, sem lag de composicao.
            go.AddComponent<CinemachineHardLookAt>();

            // Evita atravessar parede. Ver docs/07 secao 7.
            // AvoidObstacles e uma struct: ler, alterar, escrever de volta.
            var deoccluder = go.AddComponent<CinemachineDeoccluder>();
            deoccluder.CollideAgainst = GameLayers.CameraBlockers;

            var avoidance = deoccluder.AvoidObstacles;
            avoidance.Enabled = true;
            avoidance.CameraRadius = 0.25f;
            deoccluder.AvoidObstacles = avoidance;

            go.transform.position = pivot.position - pivot.forward * CameraDistance;
        }

        static void EnsureBrainOnMainCamera()
        {
            var mainCamera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .FirstOrDefault(c => c.CompareTag("MainCamera"));

            if (mainCamera == null)
            {
                Debug.LogError("[Sandbox] Nenhuma camera com a tag MainCamera na cena.");
                return;
            }

            if (mainCamera.GetComponent<CinemachineBrain>() == null)
                mainCamera.gameObject.AddComponent<CinemachineBrain>();

            // O Brain assume o controle; qualquer transform manual seria sobrescrito.
            mainCamera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        // --------------------------------------------------------------- util

        static void DestroyIfPresent(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null)
                Object.DestroyImmediate(existing);
        }
    }
}
