#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Upstream.Core;
using Upstream.Data;

namespace Upstream.EditorTools
{
    /// <summary>
    /// Automatically creates the default ScriptableObject assets and Game.unity scene on first load,
    /// and provides 1-click menu tools for scene setup and Android APK/AAB builds.
    /// </summary>
    [InitializeOnLoad]
    public static class UpstreamSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string ConfigFolder = "Assets/_Project/ScriptableObjects";

        static UpstreamSceneBuilder()
        {
            EditorApplication.delayCall += AutoInitializeProject;
        }

        private static void AutoInitializeProject()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;

            EnsureConfigAssets();

            if (!File.Exists(ScenePath))
            {
                BuildAndSaveGameScene();
            }
        }

        [MenuItem("Upstream/1. Prepare & Open Game Scene", priority = 10)]
        public static void BuildAndSaveGameScene()
        {
            EnsureConfigAssets();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<UnityEngine.Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 300f;
            cam.backgroundColor = new Color(0.12f, 0.56f, 0.69f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camObj.transform.position = new Vector3(0f, 0f, -10f);
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<Upstream.Camera.GameCamera>();

            GameObject bootObj = new GameObject("UpstreamGameBootstrap");
            var bootstrap = bootObj.AddComponent<UpstreamGameBootstrap>();
            AssignConfigAssets(bootstrap);

            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Upstream] Game.unity ve ScriptableObject ayarları otomatik oluşturuldu! Üstteki PLAY (▶) tuşuna basıp hemen oynayabilirsin.");
        }

        [MenuItem("Upstream/2. Create/Refresh ScriptableObject Configs", priority = 20)]
        public static void EnsureConfigAssets()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
                AssetDatabase.CreateFolder("Assets", "_Project");
            if (!AssetDatabase.IsValidFolder(ConfigFolder))
                AssetDatabase.CreateFolder("Assets/_Project", "ScriptableObjects");

            CreateAssetIfMissing<GameConfig>($"{ConfigFolder}/GameConfig.asset");
            CreateAssetIfMissing<GrowthConfig>($"{ConfigFolder}/GrowthConfig.asset");
            CreateAssetIfMissing<SpawnConfig>($"{ConfigFolder}/SpawnConfig.asset");
            CreateAssetIfMissing<DifficultyConfig>($"{ConfigFolder}/DifficultyConfig.asset");
        }

        /// <summary>
        /// Links the config assets to the bootstrap so Inspector tweaks are used at runtime
        /// (otherwise the bootstrap falls back to fresh default instances).
        /// </summary>
        private static void AssignConfigAssets(UpstreamGameBootstrap bootstrap)
        {
            var so = new SerializedObject(bootstrap);
            so.FindProperty("gameConfigAsset").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameConfig>($"{ConfigFolder}/GameConfig.asset");
            so.FindProperty("growthConfigAsset").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GrowthConfig>($"{ConfigFolder}/GrowthConfig.asset");
            so.FindProperty("spawnConfigAsset").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SpawnConfig>($"{ConfigFolder}/SpawnConfig.asset");
            so.FindProperty("difficultyConfigAsset").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DifficultyConfig>($"{ConfigFolder}/DifficultyConfig.asset");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateAssetIfMissing<T>(string path) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) == null)
            {
                T asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
        }

        [MenuItem("Upstream/3. Build Android APK (Telefon Testi)", priority = 30)]
        public static void BuildAndroidApk()
        {
            BuildAndroid(appBundle: false);
        }

        [MenuItem("Upstream/4. Build Android AAB (Google Play Store)", priority = 31)]
        public static void BuildAndroidAab()
        {
            BuildAndroid(appBundle: true);
        }

        /// <summary>
        /// Command-line entry point (exits with 0 on success, 1 on failure):
        /// Unity.exe -batchmode -quit -projectPath . -executeMethod Upstream.EditorTools.UpstreamSceneBuilder.BuildAndroidApkCli
        /// </summary>
        public static void BuildAndroidApkCli()
        {
            bool ok = BuildAndroid(appBundle: false);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static bool BuildAndroid(bool appBundle)
        {
            if (!File.Exists(ScenePath)) BuildAndSaveGameScene();
            Directory.CreateDirectory("Builds");

            EditorUserBuildSettings.buildAppBundle = appBundle;
            PlayerSettings.companyName = "EmirCan";
            // Launcher label. Store titles: "Blue'nun Nehri: Akıntıya Karşı" / "Blue's River: Swim & Grow".
            PlayerSettings.productName = "Blue’s River"; // typographic apostrophe: a plain one needs escaping in Android strings.xml
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.emircan.upstream");
            // Same settings for test APKs and store AABs, so what you test on the phone is what ships.
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            ReleaseSettings.Apply();
            // Test APKs may be debug-signed; a Play Store AAB must be signed with the upload key.
            if (!ReleaseSettings.ConfigureSigning(required: appBundle)) return false;

            string output = appBundle ? "Builds/Upstream.aab" : "Builds/Upstream.apk";
            BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(new[] { ScenePath }, output, BuildTarget.Android, BuildOptions.None);
            }
            finally
            {
                ReleaseSettings.ClearSigningSecrets();
            }

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[Upstream] Build FAILED ({report.summary.result}), {report.summary.totalErrors} error(s).");
                return false;
            }

            Debug.Log($"[Upstream] Build OK: {output} ({report.summary.totalSize / (1024f * 1024f):0.0} MB)");
            return true;
        }
    }
}
#endif
