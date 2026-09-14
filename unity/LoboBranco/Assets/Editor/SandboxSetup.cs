using System.IO;
using System.Linq;
using System.Reflection;
using LoboBranco.AI;
using LoboBranco.CameraSystem;
using LoboBranco.Combat;
using LoboBranco.Core;
using LoboBranco.Net;
using LoboBranco.Player;
using LoboBranco.Stats;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

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
        const string EnemyMonsterPath = "Assets/_Project/Data/Monsters/Monster_Barghest.asset";
        const string SteelSwordPath = "Assets/_Project/Data/Combat/Weapons/Weapon_SteelSword.asset";
        const string SilverSwordPath = "Assets/_Project/Data/Combat/Weapons/Weapon_SilverSword.asset";
        const string LightAttackPath = "Assets/_Project/Data/Combat/Attacks/Attack_Light.asset";
        const string HeavyAttackPath = "Assets/_Project/Data/Combat/Attacks/Attack_Heavy.asset";
        const string GroupAttackPath = "Assets/_Project/Data/Combat/Attacks/Attack_Group.asset";
        const string PlayerTuningPath = "Assets/_Project/Data/Player/PlayerTuning.asset";
        const string EnemyMaterialPath = "Assets/_Project/Art/Materials/M_Greybox_Enemy.mat";

        const string PlayerPrefabPath = "Assets/_Project/Prefabs/Characters/Player.prefab";
        const string EnemyPrefabPath = "Assets/_Project/Prefabs/Characters/Enemy_Barghest.prefab";

        const string WitcherProfilePath = "Assets/_Project/Data/Monsters/Combatant_Witcher.asset";
        const string NavMeshDataPath = "Assets/_Project/Scenes/Sandbox_Combate_NavMesh.asset";

        const string EnemyRoot = "Enemies";
        const string HunterRoot = "Cacadores";
        const string NavMeshRoot = "NavMesh";
        const string NetworkRoot = "NetworkManager";

        // Escalas fixas do projeto (docs/08_PIPELINE_ARTE_E_AUDIO.md secao 2).
        const float PlayerHeight = 1.85f;
        const float PlayerRadius = 0.3f;
        const float StepOffset = 0.4f;
        const float CameraDistance = 4.5f;

        // O barghest e uma besta baixa e larga, e nao um humanoide: a altura menor que a
        // do bruxo e o que faz o golpe dele parecer vir de baixo.
        const float HunterHeight = 1.4f;
        const float HunterRadius = 0.4f;

        [MenuItem("Lobo Branco/Setup/5. Montar sandbox de combate")]
        public static void BuildSandbox()
        {
            // Os prefabs primeiro: o NetworkManager da cena precisa apontar para eles.
            GameObject prefab = BuildPlayerPrefab();
            GameObject enemyPrefab = BuildEnemyPrefab();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            DestroyIfPresent("Player");
            DestroyIfPresent("CameraPivot");
            DestroyIfPresent("CM_Exploration");
            DestroyIfPresent(EnemyRoot);
            DestroyIfPresent(HunterRoot);
            DestroyIfPresent(NavMeshRoot);
            DestroyIfPresent(NetworkRoot);

            // O jogador nao mora mais na cena: quem o cria e o host, um por conexao
            // (doc 13 secao 6). O pivo de camera fica, porque ele e local por natureza,
            // e o dono se prende a ele quando nasce.
            var pivot = CreateCameraPivot(null);
            CreateVirtualCamera(pivot.transform);
            EnsureBrainOnMainCamera();
            CreateNetworkManager(prefab);
            CreateEnemies();

            // A malha de navegacao antes dos cacadores: um NavMeshAgent que nasce fora da
            // malha reclama no Console a cada quadro, e a definicao de pronto do docs/00
            // pede cinco minutos sem uma linha de erro.
            BakeNavMesh();
            CreateHunters(enemyPrefab);

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

            // O coordenador de encontro mora aqui, e nao em um objeto proprio, porque ele
            // e do host como o resto deste objeto: no cliente ninguem pede token, porque
            // no cliente ninguem decide atacar (docs/07 secao 6).
            var coordinator = go.AddComponent<EncounterCoordinator>();

            var so = new SerializedObject(coordinator);
            so.FindProperty("tuning").objectReferenceValue = Require<CombatTuningDef>(TuningPath);
            so.ApplyModifiedPropertiesWithoutUndo();

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

            // A porta pela qual o bruxo apanha. Sem ela o golpe do inimigo da tarefa 1.21
            // atravessa o jogador sem tirar nada, e nada aparece no Console.
            player.AddComponent<DamageReceiver>();

            player.AddComponent<PlayerMeleeAttacker>();

            // Nao tem nada para ligar: as habilidades sao da escola, e o conjurador pergunta a ela.
            player.AddComponent<PlayerAbilityCaster>();

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
            overlay.FindProperty("caster").objectReferenceValue = player.GetComponent<PlayerAbilityCaster>();
            overlay.ApplyModifiedPropertiesWithoutUndo();

            WireVitals(player.GetComponent<CharacterVitals>(), PlayerStatsPath);
            WireProfile(player.GetComponent<DamageReceiver>(), WitcherProfilePath);
            WireAttacker(player.GetComponent<PlayerMeleeAttacker>());
            WireHitFeedback(player);
            WireSchool(player);
        }

        /// <summary>
        /// A escola do bruxo (tarefa 1.31). Dela saem os atributos, os tres golpes e a postura
        /// inicial, e por isso nem o atacante nem a folha de atributos tem golpe ou bloco de
        /// atributos proprio ligado aqui: eles perguntam a escola em tempo de execucao.
        ///
        /// Todos os jogadores nascem Lobo por enquanto. Escolher a escola na sala e a tarefa
        /// 1.34, e ela transforma isto em um indice replicado decidido antes do spawn.
        /// </summary>
        static void WireSchool(GameObject player)
        {
            const string WolfSchoolPath = "Assets/_Project/Data/Player/School_Wolf.asset";

            var school = new SerializedObject(player.AddComponent<PlayerSchool>());
            school.FindProperty("school").objectReferenceValue = Require<SchoolDef>(WolfSchoolPath);
            school.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// A sensacao do golpe conectado (tarefa 1.25). O mesmo asset vai para dois lugares:
        /// o atacante le o hitstop dele no host, e o componente de sensacao le o tremor e o
        /// soco na tela do dono (tech/adr/0010).
        /// </summary>
        static void WireHitFeedback(GameObject player)
        {
            const string HitFeedbackPath = "Assets/_Project/Data/Combat/HitFeedback_Default.asset";

            var feedbackDef = Require<HitFeedbackDef>(HitFeedbackPath);

            // Uniforme e nao dissipando: o impulso so existe na maquina de quem bateu, entao
            // a distancia ate a camera nao significa nada, e dissipar faria a forca do tremor
            // depender de onde a camera esta. A duracao nao e gravada aqui: ela e lida do
            // asset em tempo de execucao, para afinar sem remontar o jogador.
            var impulse = player.AddComponent<CinemachineImpulseSource>();
            impulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            impulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;

            var hitFeedback = new SerializedObject(player.AddComponent<PlayerHitFeedback>());
            hitFeedback.FindProperty("feedback").objectReferenceValue = feedbackDef;
            hitFeedback.ApplyModifiedPropertiesWithoutUndo();

            var attacker = new SerializedObject(player.GetComponent<PlayerMeleeAttacker>());
            attacker.FindProperty("feedback").objectReferenceValue = feedbackDef;
            attacker.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Liga o perfil de combate de quem recebe golpe. Sem perfil o alvo vira besta
        /// agil, e no bruxo isso significa aco valendo 0,35x contra ele: metade dos
        /// inimigos do capitulo mal arranharia o jogador (docs/03 secao 3).
        /// </summary>
        static void WireProfile(DamageReceiver receiver, string profilePath)
        {
            if (receiver == null) return;

            var so = new SerializedObject(receiver);
            so.FindProperty("profile").objectReferenceValue = Require<MonsterDef>(profilePath);
            so.ApplyModifiedPropertiesWithoutUndo();
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

            // Os golpes nao sao ligados aqui desde a tarefa 1.31: eles sao da escola, e o
            // atacante pergunta a ela. Ver WireSchool.

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

            // O alvo de sandbox nao recebe mais golpe por conta propria: quem recebe e o
            // DamageReceiver, que o RequireComponent traz junto com o CharacterVitals.
            enemy.AddComponent<CombatDummy>();

            // A especie e o bloco de atributos apontam para os mesmos numeros: o
            // MonsterDef descreve o barghest e referencia o StatBlock dele.
            WireProfile(enemy.GetComponent<DamageReceiver>(), EnemyMonsterPath);
            WireVitals(enemy.GetComponent<CharacterVitals>(), EnemyStatsPath);
        }

        // --------------------------------------------------------------- cacadores

        /// <summary>
        /// O inimigo de verdade da tarefa 1.21, em prefab. Ele nao e o alvo de sandbox com
        /// IA por cima: o alvo pisca, tomba e levanta sozinho, e nada disso e comportamento
        /// de criatura. Sao dois objetos diferentes que reusam a mesma vida, o mesmo perfil
        /// de dano e a mesma especie.
        ///
        /// Autoridade de posicao no servidor, e nao no dono como no jogador: quem simula o
        /// monstro e o host, e o cliente so assiste (ADR 0008).
        ///
        /// O grafo de behavior tree fica <b>vazio</b> aqui, e isso e limitacao de
        /// ferramenta e nao esquecimento: o asset de grafo do <c>com.unity.behavior</c> e
        /// authoring do editor grafico e o tipo dele e interno ao pacote, entao nao ha como
        /// monta-lo por script. Os nos, esses sim, estao todos escritos em
        /// <c>Assets/_Project/Code/AI/Nodes</c>. Falta arrasta-los uma vez e apontar o
        /// grafo resultante neste prefab.
        /// </summary>
        [MenuItem("Lobo Branco/Setup/8. Montar prefab de inimigo")]
        public static GameObject BuildEnemyPrefab()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(EnemyPrefabPath) ?? string.Empty);

            GameObject enemy = CreateHunterBody();

            enemy.AddComponent<NetworkObject>();

            var netTransform = enemy.AddComponent<NetworkTransform>();
            netTransform.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            netTransform.SyncScaleX = false;
            netTransform.SyncScaleY = false;
            netTransform.SyncScaleZ = false;

            WireHunter(enemy);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(enemy, EnemyPrefabPath);
            Object.DestroyImmediate(enemy);

            EnsureNetworkPrefabHash(saved);

            Debug.Log($"[Sandbox] Prefab de inimigo gravado em {EnemyPrefabPath}.");
            return saved;
        }

        static GameObject CreateHunterBody()
        {
            var enemy = new GameObject("Enemy_Barghest")
            {
                layer = LayerMask.NameToLayer(GameLayers.Enemy),
            };

            // O colisor vive na raiz porque e por ele que a hitbox do bruxo acha o
            // IDamageable, e a busca sobe a hierarquia a partir do colisor atingido.
            var capsule = enemy.AddComponent<CapsuleCollider>();
            capsule.height = HunterHeight;
            capsule.radius = HunterRadius;
            capsule.center = new Vector3(0f, HunterHeight * 0.5f, 0f);

            var nav = enemy.AddComponent<NavMeshAgent>();
            nav.radius = HunterRadius;
            nav.height = HunterHeight;
            nav.speed = 3.5f;               // sobrescrito pelo MonsterDef em tempo de execucao
            nav.acceleration = 12f;
            nav.angularSpeed = 480f;
            nav.stoppingDistance = 0f;
            nav.autoBraking = true;

            enemy.AddComponent<CharacterVitals>();
            enemy.AddComponent<DamageReceiver>();
            enemy.AddComponent<EnemyMeleeAttacker>();
            enemy.AddComponent<EnemyAgent>();

            // O grafo so roda em quem e dono, e o dono de um monstro e o host. E a mesma
            // regra da ADR 0008 cobrada pelo proprio pacote, sem um if nosso.
            var graphAgent = enemy.AddComponent<Unity.Behavior.BehaviorGraphAgent>();
            graphAgent.NetcodeRunOnlyOnOwner = true;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body_Greybox";
            body.transform.SetParent(enemy.transform, false);
            body.transform.localPosition = new Vector3(0f, HunterHeight * 0.5f, 0f);
            body.transform.localScale = new Vector3(
                HunterRadius * 2f, HunterHeight * 0.5f, HunterRadius * 2f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.layer = enemy.layer;

            var renderer = body.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = LoadOrCreateEnemyMaterial();

            // Indicador de frente. Em greybox e a unica forma de ver para onde a criatura
            // esta virada, e para onde ela esta virada e metade do que o telegrafo comunica.
            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Facing_Greybox";
            nose.transform.SetParent(enemy.transform, false);
            nose.transform.localPosition = new Vector3(0f, HunterHeight * 0.7f, HunterRadius + 0.1f);
            nose.transform.localScale = new Vector3(0.12f, 0.12f, 0.3f);
            Object.DestroyImmediate(nose.GetComponent<Collider>());
            nose.layer = enemy.layer;

            return enemy;
        }

        static void WireHunter(GameObject enemy)
        {
            WireVitals(enemy.GetComponent<CharacterVitals>(), EnemyStatsPath);
            WireProfile(enemy.GetComponent<DamageReceiver>(), EnemyMonsterPath);

            var attacker = new SerializedObject(enemy.GetComponent<EnemyMeleeAttacker>());
            attacker.FindProperty("monster").objectReferenceValue = Require<MonsterDef>(EnemyMonsterPath);
            attacker.FindProperty("tuning").objectReferenceValue = Require<CombatTuningDef>(TuningPath);

            // Zero significaria "use o padrao do GameLayers", mas gravar a mascara
            // explicita deixa visivel no Inspector em quem a criatura acerta.
            attacker.FindProperty("targetMask").intValue = GameLayers.EnemyAttackTargets;
            attacker.ApplyModifiedPropertiesWithoutUndo();

            var agent = new SerializedObject(enemy.GetComponent<EnemyAgent>());
            agent.FindProperty("monster").objectReferenceValue = Require<MonsterDef>(EnemyMonsterPath);
            agent.FindProperty("targetMask").intValue = GameLayers.EnemyAttackTargets;
            agent.FindProperty("sightBlockers").intValue = GameLayers.Walkable;
            agent.ApplyModifiedPropertiesWithoutUndo();

            // O telegrafo e um NetworkBehaviour, e por isso so pode entrar depois do
            // NetworkObject. Ele pisca e abaixa o corpo greybox, e nunca a raiz: a raiz tem
            // colisor e posicao em rede, e encolhe-la encolheria a hitbox junto.
            const string TelegraphStylePath = "Assets/_Project/Data/Combat/TelegraphStyle_Default.asset";

            var telegraph = new SerializedObject(enemy.AddComponent<EnemyTelegraph>());
            telegraph.FindProperty("style").objectReferenceValue = Require<TelegraphStyleDef>(TelegraphStylePath);
            telegraph.FindProperty("body").objectReferenceValue = enemy.transform.Find("Body_Greybox");
            telegraph.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Dois cacadores, nos flancos e atras dos alvos parados. Ficam longe o bastante
        /// para o jogador nascer fora do alcance de visao deles: a criatura tem que ser
        /// vista chegando, e nao ja estar em cima de quem entrou na sessao.
        /// </summary>
        static void CreateHunters(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError($"[Sandbox] Nao achei {EnemyPrefabPath}. Nenhum cacador na cena.");
                return;
            }

            var root = new GameObject(HunterRoot);

            CreateHunter(prefab, root.transform, "Enemy_Cacador_A", new Vector3(-6f, 0f, 6f));
            CreateHunter(prefab, root.transform, "Enemy_Cacador_B", new Vector3(6f, 0f, 6f));
        }

        static void CreateHunter(GameObject prefab, Transform parent, string name, Vector3 position)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.position = position;

            // Viradas para o centro da arena, que e para onde o jogador nasce.
            Vector3 toCenter = -position;
            toCenter.y = 0f;
            if (toCenter.sqrMagnitude > 0.0001f)
                instance.transform.rotation = Quaternion.LookRotation(toCenter, Vector3.up);
        }

        // ---------------------------------------------------------------- navmesh

        /// <summary>
        /// Assa a malha de navegacao sobre o chao da sandbox e grava o resultado como
        /// asset ao lado da cena.
        ///
        /// Assar por script e nao pelo botao da janela de navegacao e a mesma regra do
        /// resto deste arquivo: o que e montado a mao nao e reproduzivel e nao aparece em
        /// diff. A malha cobre so a layer Environment, porque assar sobre o proprio
        /// jogador e os inimigos produziria buracos moveis na malha.
        /// </summary>
        static void BakeNavMesh()
        {
            var go = new GameObject(NavMeshRoot);

            var surface = go.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = GameLayers.Walkable;
            surface.agentTypeID = 0;

            surface.BuildNavMesh();

            if (surface.navMeshData == null)
            {
                Debug.LogError("[Sandbox] A malha de navegacao saiu vazia. O chao esta na layer Environment?");
                return;
            }

            var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshDataPath);
            if (existing != null) AssetDatabase.DeleteAsset(NavMeshDataPath);

            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshDataPath);
            Debug.Log($"[Sandbox] Malha de navegacao gravada em {NavMeshDataPath}.");
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

            // Sem ouvinte, o tremor do Impulse e gerado e nunca chega a tela (docs/07 secao
            // 7). Em espaco de camera para o tremor ser sempre na tela, e nao no mundo: um
            // tremor no eixo do mundo some quando a camera olha na direcao dele.
            var listener = go.AddComponent<CinemachineImpulseListener>();
            listener.Gain = 1f;
            listener.UseCameraSpace = true;

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
