using System.IO;
using System.Linq;
using System.Reflection;
using LoboBranco.CameraSystem;
using LoboBranco.Combat;
using LoboBranco.Core;
using LoboBranco.Net;
using LoboBranco.Player;
using LoboBranco.Stats;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
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

        const string TuningPath = "Assets/_Project/Data/Combat/CombatTuning.asset";
        const string PlayerStatsPath = "Assets/_Project/Data/Stats/StatBlock_Player.asset";
        const string EnemyStatsPath = "Assets/_Project/Data/Stats/StatBlock_Barghest.asset";
        const string SteelSwordPath = "Assets/_Project/Data/Combat/Weapons/Weapon_SteelSword.asset";
        const string SilverSwordPath = "Assets/_Project/Data/Combat/Weapons/Weapon_SilverSword.asset";
        const string LightAttackPath = "Assets/_Project/Data/Combat/Attacks/Attack_Light.asset";
        const string HeavyAttackPath = "Assets/_Project/Data/Combat/Attacks/Attack_Heavy.asset";
        const string GroupAttackPath = "Assets/_Project/Data/Combat/Attacks/Attack_Group.asset";
        const string PlayerTuningPath = "Assets/_Project/Data/Player/PlayerTuning.asset";
        const string EnemyMaterialPath = "Assets/_Project/Art/Materials/M_Greybox_Enemy.mat";

        const string PlayerPrefabPath = "Assets/_Project/Prefabs/Characters/Player.prefab";

        const string EnemyRoot = "Enemies";
        const string NetworkRoot = "NetworkManager";

        // Escalas fixas do projeto (docs/08_PIPELINE_ARTE_E_AUDIO.md secao 2).
        const float PlayerHeight = 1.85f;
        const float PlayerRadius = 0.3f;
        const float StepOffset = 0.4f;
        const float CameraDistance = 4.5f;

        [MenuItem("Lobo Branco/Setup/5. Montar sandbox de combate")]
        public static void BuildSandbox()
        {
            // O prefab primeiro: o NetworkManager da cena precisa apontar para ele.
            GameObject prefab = BuildPlayerPrefab();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            DestroyIfPresent("Player");
            DestroyIfPresent("CameraPivot");
            DestroyIfPresent("CM_Exploration");
            DestroyIfPresent(EnemyRoot);
            DestroyIfPresent(NetworkRoot);

            // O jogador nao mora mais na cena: quem o cria e o host, um por conexao
            // (doc 13 secao 6). O pivo de camera fica, porque ele e local por natureza,
            // e o dono se prende a ele quando nasce.
            var pivot = CreateCameraPivot(null);
            CreateVirtualCamera(pivot.transform);
            EnsureBrainOnMainCamera();
            CreateNetworkManager(prefab);
            CreateEnemies();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log("[Sandbox] Hierarquia montada e cena salva.");
        }

        // ---------------------------------------------------------------- rede

        /// <summary>
        /// Monta o prefab de jogador em rede e o grava em disco. Autoridade de posicao no
        /// dono, conforme a ADR 0008: o <see cref="NetworkTransform"/> sai daqui em modo
        /// <c>Owner</c>, e nao no padrao, que e servidor.
        /// </summary>
        [MenuItem("Lobo Branco/Setup/7. Montar prefab de jogador em rede")]
        public static GameObject BuildPlayerPrefab()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PlayerPrefabPath) ?? string.Empty);

            GameObject player = CreatePlayer();

            player.AddComponent<NetworkObject>();

            var netTransform = player.AddComponent<NetworkTransform>();
            netTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;

            // Escala nunca muda em jogo. Sincronizar custa banda a toa em cada quadro.
            netTransform.SyncScaleX = false;
            netTransform.SyncScaleY = false;
            netTransform.SyncScaleZ = false;

            WirePlayer(player);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            Object.DestroyImmediate(player);

            EnsureNetworkPrefabHash(saved);

            Debug.Log($"[Sandbox] Prefab de jogador gravado em {PlayerPrefabPath}.");
            return saved;
        }

        /// <summary>
        /// O <see cref="NetworkObject"/> calcula o proprio identificador de rede dentro do
        /// <c>OnValidate</c>, a partir do caminho do asset. O objeto temporario que vira o
        /// prefab ainda nao tem caminho, entao o identificador sai zero e fica zero no
        /// arquivo. No editor isso nao aparece, porque abrir o prefab dispara o
        /// <c>OnValidate</c> de novo e conserta em memoria; no build nao ha
        /// <c>OnValidate</c>, e o sintoma e o jogador nao nascer so no executavel, que e o
        /// pior lugar para descobrir qualquer coisa (item 1 da definicao de pronto).
        ///
        /// O metodo e interno ao pacote, entao a chamada e por reflexao. Recalcular a
        /// mesma conta aqui seria pior: ela mudaria de lado quando o pacote mudasse.
        /// </summary>
        static void EnsureNetworkPrefabHash(GameObject prefab)
        {
            var networkObject = prefab != null ? prefab.GetComponent<NetworkObject>() : null;
            if (networkObject == null) return;

            MethodInfo validate = typeof(NetworkObject).GetMethod(
                "OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);

            if (validate == null)
            {
                Debug.LogError("[Sandbox] NetworkObject.OnValidate sumiu do pacote. O prefab pode ficar com hash zero.");
                return;
            }

            validate.Invoke(networkObject, null);

            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Cria o objeto de rede da cena. Ele nao decide nada de jogo: hospeda o
        /// <see cref="NetworkManager"/>, o transporte direto por IP e o painel de debug.
        /// </summary>
        static void CreateNetworkManager(GameObject playerPrefab)
        {
            var go = new GameObject(NetworkRoot);

            var manager = go.AddComponent<NetworkManager>();
            var transport = go.AddComponent<UnityTransport>();

            manager.NetworkConfig ??= new NetworkConfig();
            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.PlayerPrefab = playerPrefab;
            manager.NetworkConfig.ConnectionApproval = true;

            go.AddComponent<NetLauncher>();
            go.AddComponent<NetSpawnRing>();

            // O Relay entra por cima do mesmo transporte do IP direto, e o IP direto
            // continua sendo o caminho do dia a dia (risco X8 do doc 13).
            go.AddComponent<NetRelaySession>();

            go.AddComponent<NetRoomPanel>();
            go.AddComponent<NetDebugHud>();

            if (playerPrefab == null)
                Debug.LogError($"[Sandbox] Nao achei {PlayerPrefabPath}. Nenhum jogador vai nascer.");
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

            // Antes do atacante, e nao depois: a folha de atributos do bruxo mora aqui, e
            // e dela que o pipeline de dano le quando o host resolve o golpe (ADR 0008).
            player.AddComponent<CharacterVitals>();
            player.AddComponent<PlayerMeleeAttacker>();
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

        /// <summary>
        /// Liga os assets do jogador. O pivo de camera fica de fora de proposito: prefab
        /// nao guarda referencia de cena, entao quem acha o pivo e o dono, em tempo de
        /// execucao, e so ele.
        /// </summary>
        static void WirePlayer(GameObject player)
        {
            var controls = AssetDatabase.LoadAssetAtPath<Object>(ControlsPath);
            if (controls == null)
                Debug.LogError($"[Sandbox] Nao achei {ControlsPath}.");

            var reader = new SerializedObject(player.GetComponent<PlayerInputReader>());
            reader.FindProperty("actions").objectReferenceValue = controls;
            reader.ApplyModifiedPropertiesWithoutUndo();

            var brain = new SerializedObject(player.GetComponent<PlayerBrain>());
            brain.FindProperty("tuning").objectReferenceValue = Require<PlayerTuningDef>(PlayerTuningPath);
            brain.ApplyModifiedPropertiesWithoutUndo();

            var overlay = new SerializedObject(player.GetComponent<PlayerDebugOverlay>());
            overlay.FindProperty("locomotion").objectReferenceValue = player.GetComponent<PlayerLocomotion>();
            overlay.FindProperty("input").objectReferenceValue = player.GetComponent<PlayerInputReader>();
            overlay.FindProperty("brain").objectReferenceValue = player.GetComponent<PlayerBrain>();
            overlay.FindProperty("attacker").objectReferenceValue = player.GetComponent<PlayerMeleeAttacker>();
            overlay.FindProperty("vitals").objectReferenceValue = player.GetComponent<CharacterVitals>();
            overlay.ApplyModifiedPropertiesWithoutUndo();

            WireVitals(player.GetComponent<CharacterVitals>(), PlayerStatsPath);
            WireAttacker(player.GetComponent<PlayerMeleeAttacker>());
        }

        /// <summary>
        /// Liga os assets de combate. Sem eles, o componente se desliga sozinho no Awake
        /// e o ataque some sem erro visivel, entao cada ausencia vira um log.
        /// </summary>
        static void WireAttacker(PlayerMeleeAttacker attacker)
        {
            var so = new SerializedObject(attacker);

            so.FindProperty("tuning").objectReferenceValue = Require<CombatTuningDef>(TuningPath);
            so.FindProperty("steelSword").objectReferenceValue = Require<MeleeWeaponDef>(SteelSwordPath);
            so.FindProperty("silverSword").objectReferenceValue = Require<MeleeWeaponDef>(SilverSwordPath);
            // Um golpe por postura, e nao um por botao: e a postura que decide o golpe
            // (docs/03 secao 4). O asset de cada um ja declara a propria postura.
            so.FindProperty("fastAttack").objectReferenceValue = Require<AttackDef>(LightAttackPath);
            so.FindProperty("strongAttack").objectReferenceValue = Require<AttackDef>(HeavyAttackPath);
            so.FindProperty("groupAttack").objectReferenceValue = Require<AttackDef>(GroupAttackPath);

            // Zero significa "use GameLayers.PlayerAttackTargets", mas gravar a mascara
            // explicita aqui torna visivel no Inspector no que o golpe acerta.
            so.FindProperty("targetMask").intValue = GameLayers.PlayerAttackTargets;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Liga o bloco de atributos de quem tem vida. E o mesmo componente no bruxo e na
        /// capsula, e e de proposito: vida replicada e um problema so, resolvido uma vez.
        /// </summary>
        static void WireVitals(CharacterVitals vitals, string statBlockPath)
        {
            var so = new SerializedObject(vitals);
            so.FindProperty("statBlock").objectReferenceValue = Require<StatBlockDef>(statBlockPath);

            // Os tempos do Vigor (docs/03 secao 7) valem para bruxo e para monstro, entao
            // eles vivem no mesmo asset de afinacao do combate.
            so.FindProperty("tuning").objectReferenceValue = Require<CombatTuningDef>(TuningPath);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------- inimigos

        /// <summary>
        /// Tres capsulas vermelhas: uma de frente e duas nos flancos, dentro do arco de
        /// 180 graus da postura Grupo e fora do arco de 110 do golpe leve. E o arranjo
        /// minimo em que da para ver que o filtro de arco existe.
        /// </summary>
        static void CreateEnemies()
        {
            var root = new GameObject(EnemyRoot);

            CreateEnemy(root.transform, "Enemy_Alvo_Frente", new Vector3(0f, 0f, -4.0f));
            CreateEnemy(root.transform, "Enemy_Alvo_Esquerda", new Vector3(-2.2f, 0f, -3.4f));
            CreateEnemy(root.transform, "Enemy_Alvo_Direita", new Vector3(2.2f, 0f, -3.4f));
        }

        static void CreateEnemy(Transform parent, string name, Vector3 position)
        {
            var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = name;
            enemy.transform.SetParent(parent, false);
            enemy.transform.position = position + Vector3.up;
            enemy.layer = LayerMask.NameToLayer(GameLayers.Enemy);

            var renderer = enemy.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = LoadOrCreateEnemyMaterial();

            // Objeto de rede posto na cena, e nao criado pelo host: o alvo ja esta la
            // quando a sessao sobe, e o que ele precisa e que a vida dele seja a mesma nas
            // quatro telas. Sem NetworkObject, cada participante mata a propria capsula.
            enemy.AddComponent<NetworkObject>();

            enemy.AddComponent<CombatDummy>();

            WireVitals(enemy.GetComponent<CharacterVitals>(), EnemyStatsPath);
        }

        static Material LoadOrCreateEnemyMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(EnemyMaterialPath);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("[Sandbox] Shader Lit da URP nao encontrado; capsula fica com o material padrao.");
                return null;
            }

            var material = new Material(shader);
            material.SetColor("_BaseColor", new Color(0.75f, 0.12f, 0.12f));

            AssetDatabase.CreateAsset(material, EnemyMaterialPath);
            return material;
        }

        static T Require<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset == null)
                Debug.LogError($"[Sandbox] Nao achei {path}. Rode 'Lobo Branco/Setup/6. Criar assets de combate' antes.");

            return asset;
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
