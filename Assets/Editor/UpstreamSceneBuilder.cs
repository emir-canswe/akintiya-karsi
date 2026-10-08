#if UNITY_EDITOR
using System.IO;
using UnityEditor;
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
            bootObj.AddComponent<UpstreamGameBootstrap>();

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
            if (!File.Exists(ScenePath)) BuildAndSaveGameScene();
            Directory.CreateDirectory("Builds");
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.companyName = "EmirCan";
            PlayerSettings.productName = "Upstream";
            PlayerSettings.SetApplicationIdentifier( UnityEditor.Build.NamedBuildTarget.Android, "com.emircan.upstream");

            BuildPipeline.BuildPlayer(new[] { ScenePath }, "Builds/Upstream.apk", BuildTarget.Android, BuildOptions.None);
            Debug.Log("[Upstream] APK oluşturuldu: Builds/Upstream.apk");
        }

        [MenuItem("Upstream/4. Build Android AAB (Google Play Store)", priority = 31)]
        public static void BuildAndroidAab()
        {
            if (!File.Exists(ScenePath)) BuildAndSaveGameScene();
            Directory.CreateDirectory("Builds");
            EditorUserBuildSettings.buildAppBundle = true;
            PlayerSettings.companyName = "EmirCan";
            PlayerSettings.productName = "Upstream";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.emircan.upstream");
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            BuildPipeline.BuildPlayer(new[] { ScenePath }, "Builds/Upstream.aab", BuildTarget.Android, BuildOptions.None);
            Debug.Log("[Upstream] AAB oluşturuldu: Builds/Upstream.aab");
        }
    }
}
#endif
