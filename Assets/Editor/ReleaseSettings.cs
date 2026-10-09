#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Upstream.EditorTools
{
    /// <summary>
    /// Release configuration in one place: version, app icon (generated procedurally so it can be
    /// regenerated/tweaked from code), splash colors and Android signing.
    ///
    /// Releasing a new version: bump <see cref="Version"/> AND <see cref="VersionCode"/> (Google Play
    /// rejects an upload whose versionCode isn't higher than the previous one).
    /// </summary>
    public static class ReleaseSettings
    {
        public const string Version = "0.1.0";
        public const int VersionCode = 1;

        private const string IconFolder = "Assets/_Project/Art/UI";
        private const string IconPath = IconFolder + "/AppIcon.png";
        private const string IconBackgroundPath = IconFolder + "/AppIcon_Background.png";
        private const string IconForegroundPath = IconFolder + "/AppIcon_Foreground.png";

        // Brand colors (same as the game: river teal + koi orange)
        private static readonly Color WaterTop = new Color(0.13f, 0.62f, 0.75f);
        private static readonly Color WaterBottom = new Color(0.04f, 0.33f, 0.45f);
        private static readonly Color FishColor = new Color(1f, 0.54f, 0.36f);
        private static readonly Color FishBelly = new Color(1f, 0.82f, 0.72f);

        [MenuItem("Upstream/5. Apply Release Settings (version, icon, splash)", priority = 40)]
        public static void Apply()
        {
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.Android.bundleVersionCode = VersionCode;
            PlayerSettings.iOS.buildNumber = VersionCode.ToString();

            // Unity Personal must show the Unity splash; at least make it on-brand.
            PlayerSettings.SplashScreen.backgroundColor = WaterBottom;
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;

            GenerateIconsIfMissing();
            ApplyIcons();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Upstream] Release settings applied: v{Version} ({VersionCode})");
        }

        [MenuItem("Upstream/6. Regenerate App Icon", priority = 41)]
        public static void RegenerateIcons()
        {
            GenerateIcons();
            ApplyIcons();
        }

        /// <summary>
        /// Configures release signing from environment variables (never from files in the repo):
        /// UPSTREAM_KEYSTORE_PATH, UPSTREAM_KEYSTORE_PASS, UPSTREAM_KEY_ALIAS, UPSTREAM_KEY_PASS.
        /// Returns false if signing is required (AAB) but not configured.
        /// </summary>
        public static bool ConfigureSigning(bool required)
        {
            string path = Environment.GetEnvironmentVariable("UPSTREAM_KEYSTORE_PATH");
            string storePass = Environment.GetEnvironmentVariable("UPSTREAM_KEYSTORE_PASS");
            string alias = Environment.GetEnvironmentVariable("UPSTREAM_KEY_ALIAS");
            string keyPass = Environment.GetEnvironmentVariable("UPSTREAM_KEY_PASS");

            bool configured = !string.IsNullOrEmpty(path) && File.Exists(path) &&
                              !string.IsNullOrEmpty(storePass) && !string.IsNullOrEmpty(alias);

            if (!configured)
            {
                PlayerSettings.Android.useCustomKeystore = false; // debug-signed: fine for phone testing
                if (required)
                {
                    Debug.LogError("[Upstream] A Play Store AAB must be signed with your upload key. " +
                                   "Set UPSTREAM_KEYSTORE_PATH / UPSTREAM_KEYSTORE_PASS / UPSTREAM_KEY_ALIAS / UPSTREAM_KEY_PASS " +
                                   "(see docs/KEYSTORE.md).");
                    return false;
                }
                return true;
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = path;
            PlayerSettings.Android.keystorePass = storePass;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = string.IsNullOrEmpty(keyPass) ? storePass : keyPass;
            return true;
        }

        /// <summary>Passwords are held only in memory for the build; clear them so they're never saved to ProjectSettings.</summary>
        public static void ClearSigningSecrets()
        {
            PlayerSettings.Android.keystorePass = "";
            PlayerSettings.Android.keyaliasPass = "";
        }

        // ---------------------------------------------------------------- icons

        private static void GenerateIconsIfMissing()
        {
            if (!File.Exists(IconPath) || !File.Exists(IconBackgroundPath) || !File.Exists(IconForegroundPath))
            {
                GenerateIcons();
            }
        }

        private static void GenerateIcons()
        {
            Directory.CreateDirectory(IconFolder);
            const int size = 1024;

            // Legacy / store icon: water + fish, full bleed (launchers apply their own mask).
            WritePng(IconPath, size, (u, v) => Over(Water(u, v), Fish(u, v, 0.78f)));
            // Adaptive icon layers: Android may crop to the central ~66%, so the fish is drawn smaller.
            WritePng(IconBackgroundPath, size, Water);
            WritePng(IconForegroundPath, size, (u, v) => Fish(u, v, 0.56f));

            AssetDatabase.Refresh();
            foreach (string path in new[] { IconPath, IconBackgroundPath, IconForegroundPath })
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 1024;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        private static void ApplyIcons()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null)
            {
                Debug.LogWarning("[Upstream] App icon not found; skipping icon setup.");
                return;
            }

            // Default icon: used for every platform/size unless overridden.
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

#if UNITY_ANDROID
            var background = AssetDatabase.LoadAssetAtPath<Texture2D>(IconBackgroundPath);
            var foreground = AssetDatabase.LoadAssetAtPath<Texture2D>(IconForegroundPath);
            if (background != null && foreground != null)
            {
                var adaptive = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive);
                foreach (var platformIcon in adaptive) platformIcon.SetTextures(background, foreground);
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive, adaptive);
                // Round/Legacy kinds are deprecated in Unity 6; older launchers fall back to the default icon above.
            }
#endif
        }

        private static void WritePng(string path, int size, Func<float, float, Color> shader)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            // 2x2 supersampling for smooth edges
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color acc = Color.clear;
                    for (int sy = 0; sy < 2; sy++)
                    for (int sx = 0; sx < 2; sx++)
                    {
                        float u = (x + 0.25f + sx * 0.5f) / size;
                        float v = (y + 0.25f + sy * 0.5f) / size;
                        acc += shader(u, v);
                    }
                    pixels[y * size + x] = acc * 0.25f;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        // u, v in 0..1, v = 0 at the bottom.
        private static Color Water(float u, float v)
        {
            Color c = Color.Lerp(WaterBottom, WaterTop, v);
            // soft light ripples drifting downstream
            float ripple = Mathf.Sin((v * 9f + Mathf.Sin(u * 6.283f) * 0.35f) * 6.283f);
            c = Color.Lerp(c, Color.white, Mathf.Clamp01(ripple - 0.86f) * 0.9f);
            // vignette
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.55f));
            c *= Mathf.Lerp(1.05f, 0.82f, Mathf.Clamp01(d * 1.3f));
            c.a = 1f;
            return c;
        }

        /// <summary>Top-down koi swimming upward. <paramref name="scale"/> = fish length as fraction of icon.</summary>
        private static Color Fish(float u, float v, float scale)
        {
            // local coords: fish centered, head up
            float x = (u - 0.5f) / scale;
            float y = (v - 0.47f) / scale;
            float aa = 0.004f / scale;

            // body: ellipse that tapers toward the tail (wide head, slim tail stem)
            float taper = Mathf.Lerp(0.42f, 1f, Mathf.Clamp01((y + 0.22f) / 0.5f));
            float body = Ellipse(x / taper, y - 0.06f, 0.15f, 0.36f) * taper;
            // tail: two lobes fanning out from the tail stem, slightly swishing
            float tailL = Ellipse(Rotate(x + 0.045f, y + 0.34f, -0.5f), 0.055f, 0.15f);
            float tailR = Ellipse(Rotate(x - 0.045f, y + 0.34f, 0.5f), 0.055f, 0.15f);
            // pectoral fins
            float finL = Ellipse(Rotate(x + 0.15f, y - 0.1f, 0.9f), 0.045f, 0.11f);
            float finR = Ellipse(Rotate(x - 0.15f, y - 0.1f, -0.9f), 0.045f, 0.11f);

            float shape = Mathf.Min(body, Mathf.Min(Mathf.Min(tailL, tailR), Mathf.Min(finL, finR)));
            float alpha = Mathf.Clamp01(0.5f - shape / aa);
            if (alpha <= 0f)
            {
                // soft drop shadow on the water
                float shadow = Ellipse(x - 0.03f, y - 0.05f, 0.21f, 0.42f);
                float sa = Mathf.Clamp01(-shadow * 4f + 0.35f) * 0.35f;
                return new Color(0f, 0.08f, 0.12f, sa);
            }

            Color c = FishColor;
            // belly highlight along the spine
            float spine = Mathf.Clamp01(1f - Mathf.Abs(x) / 0.09f) * Mathf.Clamp01(1f - Mathf.Abs(y - 0.12f) / 0.3f);
            c = Color.Lerp(c, FishBelly, spine * 0.55f);
            // fins/tail a bit more transparent-looking
            if (body > 0f) c = Color.Lerp(c, FishBelly, 0.35f);
            // koi patches (white), only on the body
            if (body <= 0f)
            {
                float patch1 = Vector2.Distance(new Vector2(x, y), new Vector2(0.04f, 0.14f)) - 0.075f;
                float patch2 = Vector2.Distance(new Vector2(x, y), new Vector2(-0.035f, -0.06f)) - 0.06f;
                float patch = Mathf.Min(patch1, patch2);
                c = Color.Lerp(c, new Color(1f, 0.97f, 0.93f), Mathf.Clamp01(0.5f - patch / aa));
            }
            // eyes
            float eyeL = Vector2.Distance(new Vector2(x, y), new Vector2(-0.075f, 0.33f));
            float eyeR = Vector2.Distance(new Vector2(x, y), new Vector2(0.075f, 0.33f));
            if (Mathf.Min(eyeL, eyeR) < 0.028f) c = new Color(0.1f, 0.06f, 0.05f);
            c.a = alpha;
            return c;
        }

        private static float Ellipse(float x, float y, float rx, float ry)
        {
            // approximate signed distance (negative inside)
            float k = Mathf.Sqrt((x * x) / (rx * rx) + (y * y) / (ry * ry));
            return (k - 1f) * Mathf.Min(rx, ry);
        }

        private static float Ellipse(Vector2 p, float rx, float ry) => Ellipse(p.x, p.y, rx, ry);

        private static Vector2 Rotate(float x, float y, float angle)
        {
            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            return new Vector2(x * c - y * s, x * s + y * c);
        }

        private static Color Over(Color dst, Color src)
        {
            float a = src.a;
            Color c = src * a + dst * (1f - a);
            c.a = 1f;
            return c;
        }
    }
}
#endif
