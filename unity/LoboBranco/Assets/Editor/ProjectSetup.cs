using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LoboBranco.EditorTools
{
    /// <summary>
    /// Setup unico do projeto. Roda em batchmode via -executeMethod, ou pelo menu.
    /// Cada metodo e idempotente: rodar duas vezes nao quebra nada.
    /// Referencia: docs/11_SETUP_AMBIENTE.md secao 4.
    /// </summary>
    public static class ProjectSetup
    {
        // ---------------------------------------------------------------- pacotes

        static readonly string[] PackagesToAdd =
        {
            "com.unity.cinemachine",
            "com.unity.behavior",
            "com.unity.addressables",
            "com.unity.localization",
            "com.unity.splines",
            "com.unity.probuilder",
            "com.unity.nuget.newtonsoft-json",

            // Rede. A lista e a da ADR 0008; transport vem junto com NGO, mas fica declarado
            // porque a versao dele importa e dependencia implicita nao aparece no manifest.
            "com.unity.netcode.gameobjects",
            "com.unity.transport",
            "com.unity.services.core",
            "com.unity.services.authentication",
            "com.unity.services.relay",
            "com.unity.multiplayer.playmode",
        };

        static readonly string[] PackagesToRemove =
        {
            "com.unity.visualscripting",        // nao usamos; custa tempo de compilacao
            "com.unity.collab-proxy",           // versionamento e Git
            "com.unity.purchasing",             // sem compra no app; ainda cria Assets/Resources/BillingMode.json
            "com.unity.xr.legacyinputhelpers",  // sem XR, e depende do Input Manager antigo
        };

        [MenuItem("Lobo Branco/Setup/1. Instalar pacotes")]
        public static void InstallPackages()
        {
            var installed = ListInstalled();
            var add = PackagesToAdd.Where(p => !installed.Contains(p)).ToArray();
            var remove = PackagesToRemove.Where(p => installed.Contains(p)).ToArray();

            if (add.Length == 0 && remove.Length == 0)
            {
                Debug.Log("[Setup] Pacotes ja estao como esperado.");
                return;
            }

            Debug.Log($"[Setup] Adicionando: {string.Join(", ", add)}");
            Debug.Log($"[Setup] Removendo: {string.Join(", ", remove)}");

            var request = Client.AddAndRemove(add, remove);
            WaitFor(request, "AddAndRemove", 900);

            if (request.Status == StatusCode.Failure)
                Debug.LogError($"[Setup] Falha ao resolver pacotes: {request.Error?.message}");
            else
                Debug.Log("[Setup] Pacotes resolvidos.");
        }

        [MenuItem("Lobo Branco/Setup/2. Instalar Ink")]
        public static void InstallInk()
        {
            const string ink = "com.inkle.ink-unity-integration";
            if (ListInstalled().Contains(ink))
            {
                Debug.Log("[Setup] Ink ja instalado.");
                return;
            }

            var request = Client.Add("https://github.com/inkle/ink-unity-integration.git#upm");
            WaitFor(request, "Add Ink", 600);

            if (request.Status == StatusCode.Failure)
                Debug.LogError($"[Setup] Falha ao instalar Ink: {request.Error?.message}");
            else
                Debug.Log("[Setup] Ink instalado.");
        }

        static HashSet<string> ListInstalled()
        {
            var list = Client.List(offlineMode: true, includeIndirectDependencies: false);
            WaitFor(list, "List", 300);
            return list.Status == StatusCode.Success
                ? new HashSet<string>(list.Result.Select(p => p.name))
                : new HashSet<string>();
        }

        static void WaitFor(Request request, string label, int timeoutSeconds)
        {
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (!request.IsCompleted)
            {
                if (DateTime.UtcNow > deadline)
                {
                    Debug.LogError($"[Setup] Timeout em {label} apos {timeoutSeconds}s.");
                    return;
                }
                Thread.Sleep(200);
            }
        }

        // ------------------------------------------------------- editor externo

        [MenuItem("Lobo Branco/Setup/0. Usar VS Code como editor")]
        public static void UseVsCode()
        {
            string[] candidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs", "Microsoft VS Code", "Code.exe"),
                @"C:\Program Files\Microsoft VS Code\Code.exe",
            };

            string path = candidates.FirstOrDefault(File.Exists);

            if (path == null)
            {
                Debug.LogError("[Setup] Code.exe nao encontrado nos caminhos conhecidos.");
                return;
            }

            EditorPrefs.SetString("kScriptsDefaultApp", path);

            // Gera .csproj e .sln para o C# Dev Kit ler; sem isso o IntelliSense fica cego.
            EditorPrefs.SetBool("kExternalEditorSupportsUnityProj", true);
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();

            Debug.Log($"[Setup] Editor externo definido: {path}");
        }

        // ------------------------------------------------- matriz de colisao

        /// <summary>
        /// Pares que NAO devem colidir. Hitboxes sao consultadas por OverlapCapsule
        /// (doc 07 secao 4.5), entao elas nao precisam colidir com nada fisicamente.
        /// </summary>
        static readonly (string a, string b)[] IgnoredPairs =
        {
            ("PlayerHitbox", "Player"),
            ("PlayerHitbox", "NPC"),
            ("PlayerHitbox", "Environment"),
            ("PlayerHitbox", "Interactable"),
            ("PlayerHitbox", "Clue"),
            ("PlayerHitbox", "Projectile"),
            ("PlayerHitbox", "PlayerHitbox"),
            ("PlayerHitbox", "EnemyHitbox"),
            ("PlayerHitbox", "Water"),

            ("EnemyHitbox", "Enemy"),
            ("EnemyHitbox", "NPC"),
            ("EnemyHitbox", "Environment"),
            ("EnemyHitbox", "Interactable"),
            ("EnemyHitbox", "Clue"),
            ("EnemyHitbox", "Projectile"),
            ("EnemyHitbox", "EnemyHitbox"),
            ("EnemyHitbox", "Water"),

            ("Clue", "Player"),
            ("Clue", "Enemy"),
            ("Clue", "NPC"),
            ("Clue", "Environment"),
            ("Clue", "Interactable"),
            ("Clue", "Projectile"),
            ("Clue", "Clue"),
            ("Clue", "Water"),

            ("Interactable", "Interactable"),
            ("Interactable", "Projectile"),
            ("Interactable", "Water"),

            ("IgnoreCamera", "Player"),
            ("IgnoreCamera", "Enemy"),
            ("IgnoreCamera", "NPC"),
            ("IgnoreCamera", "Projectile"),
            ("IgnoreCamera", "Clue"),
            ("IgnoreCamera", "IgnoreCamera"),

            ("Projectile", "Projectile"),
            ("Projectile", "Water"),
        };

        [MenuItem("Lobo Branco/Setup/3. Configurar matriz de colisao")]
        public static void ConfigureCollisionMatrix()
        {
            int applied = 0, skipped = 0;

            foreach (var (a, b) in IgnoredPairs)
            {
                int la = LayerMask.NameToLayer(a);
                int lb = LayerMask.NameToLayer(b);

                if (la < 0 || lb < 0)
                {
                    Debug.LogWarning($"[Setup] Layer inexistente no par ({a}, {b}). Pulado.");
                    skipped++;
                    continue;
                }

                Physics.IgnoreLayerCollision(la, lb, true);
                applied++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Setup] Matriz de colisao: {applied} pares ignorados, {skipped} pulados.");
        }

        // ------------------------------------------------------------ cenas

        static readonly string[] SceneNames =
        {
            "Boot",
            "MainMenu",
            "Sandbox_Combate",
            "Zone_Vilarejo",
            "Zone_Floresta",
            "Zone_Cripta",
        };

        const string ScenesFolder = "Assets/_Project/Scenes";

        [MenuItem("Lobo Branco/Setup/4. Criar cenas")]
        public static void CreateScenes()
        {
            Directory.CreateDirectory(ScenesFolder);

            var buildScenes = new List<EditorBuildSettingsScene>();

            foreach (var name in SceneNames)
            {
                string path = $"{ScenesFolder}/{name}.unity";

                if (!File.Exists(path))
                {
                    var setup = name == "Boot" || name == "MainMenu"
                        ? NewSceneSetup.EmptyScene
                        : NewSceneSetup.DefaultGameObjects;

                    var scene = EditorSceneManager.NewScene(setup, NewSceneMode.Single);

                    if (name == "Sandbox_Combate")
                        BuildSandbox(scene);

                    EditorSceneManager.SaveScene(scene, path);
                    Debug.Log($"[Setup] Cena criada: {path}");
                }

                buildScenes.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();

            // A SampleScene do template nao serve para nada aqui.
            const string sample = "Assets/Scenes/SampleScene.unity";
            if (File.Exists(sample))
            {
                AssetDatabase.DeleteAsset("Assets/Scenes");
                Debug.Log("[Setup] SampleScene do template removida.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Setup] {SceneNames.Length} cenas registradas no Build Settings.");
        }

        /// <summary>
        /// Sandbox de combate do doc 11 secao 6: chao de 50x50 e nada mais.
        /// E onde 60% do desenvolvimento vai acontecer.
        /// </summary>
        static void BuildSandbox(Scene scene)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5f, 1f, 5f); // plane = 10u, entao 50x50
            ground.layer = LayerMask.NameToLayer("Environment");

            var marker = new GameObject("SpawnPoint_Player");
            marker.transform.position = new Vector3(0f, 0f, -8f);

            var enemies = new GameObject("SpawnPoints_Enemies");
            for (int i = 0; i < 4; i++)
            {
                var p = new GameObject($"EnemySpawn_{i}");
                p.transform.SetParent(enemies.transform);
                float angle = i * Mathf.PI * 0.5f;
                p.transform.position = new Vector3(Mathf.Cos(angle) * 6f, 0f, Mathf.Sin(angle) * 6f);
            }
        }

        // ------------------------------------------------------------ tudo

        public static void RunAll()
        {
            InstallPackages();
            ConfigureCollisionMatrix();
            CreateScenes();
        }

        /// <summary>
        /// Etapas que precisam rodar depois que os pacotes ja foram resolvidos e compilados.
        /// Chamada em uma segunda sessao de batchmode.
        /// </summary>
        public static void PostPackageSetup()
        {
            ConfigureCollisionMatrix();
            CreateScenes();
        }

        // ------------------------------------------------------------ build

        /// <summary>
        /// Build de verificacao para Windows x64, backend Mono (rapido, para desenvolvimento).
        /// Doc 11 secao 7: o checklist de saida de M0 exige que o .exe gere e rode.
        /// </summary>
        [MenuItem("Lobo Branco/Build/Windows x64 (Mono)")]
        public static void BuildWindows()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[Build] Nenhuma cena no Build Settings.");
                return;
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "../Builds/Windows/LoboBranco.exe",
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[Build] Resultado: {summary.result} | " +
                      $"{summary.totalSize / (1024 * 1024)} MB | " +
                      $"{summary.totalTime.TotalSeconds:F0}s | " +
                      $"{summary.totalErrors} erros, {summary.totalWarnings} avisos");

            if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}
