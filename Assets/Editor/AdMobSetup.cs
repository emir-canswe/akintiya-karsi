using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Upstream.EditorTools
{
    /// <summary>
    /// One-shot wiring of the Google Mobile Ads plugin: writes the AdMob app ids into the plugin's settings
    /// asset and turns on USE_ADMOB for Android. Defaults are Google's official TEST app ids; pass real ones
    /// through the UPSTREAM_ADMOB_ANDROID_APP_ID environment variable before the production build.
    /// </summary>
    public static class AdMobSetup
    {
        private const string TestAndroidAppId = "ca-app-pub-3940256099942544~3347511713";
        private const string TestIosAppId = "ca-app-pub-3940256099942544~1458002511";
        private const string Define = "USE_ADMOB";

        [MenuItem("Upstream/7. Setup AdMob (app id + USE_ADMOB)", priority = 50)]
        public static void Setup()
        {
            string androidId = Environment.GetEnvironmentVariable("UPSTREAM_ADMOB_ANDROID_APP_ID");
            if (string.IsNullOrEmpty(androidId)) androidId = TestAndroidAppId;

            Type settingsType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .FirstOrDefault(t => t.FullName == "GoogleMobileAds.Editor.GoogleMobileAdsSettings");
            if (settingsType == null)
                throw new InvalidOperationException("Google Mobile Ads plugin is not installed.");

            var load = settingsType.GetMethod("LoadInstance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            var instance = (ScriptableObject)load.Invoke(null, null);
            settingsType.GetProperty("GoogleMobileAdsAndroidAppId").SetValue(instance, androidId);
            settingsType.GetProperty("GoogleMobileAdsIOSAppId").SetValue(instance, TestIosAppId);
            EditorUtility.SetDirty(instance);

            var target = NamedBuildTarget.Android;
            string defines = PlayerSettings.GetScriptingDefineSymbols(target);
            if (!defines.Split(';').Contains(Define))
                PlayerSettings.SetScriptingDefineSymbols(target, string.IsNullOrEmpty(defines) ? Define : defines + ";" + Define);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Upstream] AdMob set up: android app id {androidId}, {Define} on Android.");
        }

        public static void SetupCli()
        {
            try { Setup(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogError(e); EditorApplication.Exit(1); }
        }
    }
}
