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

        // Splash screen background: deep river teal (icon colors are further down, with the icon art).
        private static readonly Color WaterBottom = new Color(0.04f, 0.33f, 0.45f);

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
            WriteStoreIcon();
        }

        /// <summary>Command line: regenerate the icons and the 512 px Play Store icon, then quit.</summary>
        public static void RegenerateIconsCli()
        {
            RegenerateIcons();
            AssetDatabase.SaveAssets();
            EditorApplication.Exit(0);
        }

        /// <summary>Google Play's "App icon": 512 x 512, 32-bit PNG, full square (Play rounds the corners).</summary>
        public const string StoreIconPath = "docs/store/play-icon-512.png";

        private static void WriteStoreIcon()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StoreIconPath));
            WritePng(StoreIconPath, 512, StoreIcon);
            Debug.Log($"[Upstream] Play Store icon written to {StoreIconPath}");
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

            // Legacy / iOS / store icon: Blue on the warm background, full bleed (stores apply their own mask).
            WritePng(IconPath, size, StoreIcon);
            // Adaptive icon layers: Android may crop to the central ~66%, so Blue is drawn smaller.
            WritePng(IconBackgroundPath, size, Background);
            WritePng(IconForegroundPath, size, (u, v) => BlueFish(u, v, 0.6f));

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

        // ---------------------------------------------------------------- icon art
        // u, v in 0..1, v = 0 at the bottom. Blue (the hero) on a warm sunny background: almost every fish
        // and water game on the store is blue, so a blue fish on orange stands out in a grid of icons.

        private static readonly Color Navy = H("#163C8C");
        private static readonly Color SunCenter = H("#FFD84D");
        private static readonly Color SunEdge = H("#FF8A3D");
        private static readonly Color BlueDark = H("#2C7FD6");
        private static readonly Color BlueLight = H("#66B9FF");
        private static readonly Color BlueSpot = H("#1B4F9C");
        private static readonly Color FinColor = H("#CDEBFF");
        private static readonly Color TailColor = H("#FF6B5E");
        private static readonly Color TailDark = H("#E0474A");
        private static readonly Color Pupil = H("#14182B");

        /// <summary>Head direction: tilted toward the upper right, swimming "up" the icon.</summary>
        private const float FishAngle = 32f * Mathf.Deg2Rad;

        private static Color H(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }

        private static Color StoreIcon(float u, float v) =>
            Over(Over(Background(u, v), FishShadow(u, v, 0.9f)), BlueFish(u, v, 0.9f));

        private static Color Background(float u, float v)
        {
            // Sunny radial gradient
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.56f));
            Color c = Color.Lerp(SunCenter, SunEdge, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(d / 0.75f)));

            // Water ripples spreading out from Blue
            Vector2 q = new Vector2(u - 0.5f, v - 0.5f);
            float r = q.magnitude;
            float ang = Mathf.Atan2(q.y, q.x);
            for (int i = 0; i < 3; i++)
            {
                float ringR = 0.3f + i * 0.1f + 0.006f * Mathf.Sin(ang * 7f + i * 1.7f);
                float ring = Mathf.Clamp01(1f - Mathf.Abs(r - ringR) / 0.009f) * (0.42f - i * 0.1f);
                c = Color.Lerp(c, Color.white, ring);
            }

            // Bubbles rising at the top right
            c = Bubble(c, u, v, new Vector2(0.80f, 0.80f), 0.040f);
            c = Bubble(c, u, v, new Vector2(0.87f, 0.68f), 0.024f);
            c = Bubble(c, u, v, new Vector2(0.72f, 0.89f), 0.017f);
            c.a = 1f;
            return c;
        }

        private static Color Bubble(Color c, float u, float v, Vector2 center, float radius)
        {
            float d = Vector2.Distance(new Vector2(u, v), center);
            if (d > radius * 1.25f) return c;
            float rim = Mathf.Clamp01(1f - Mathf.Abs(d - radius) / (radius * 0.2f));
            float fill = d < radius ? 0.22f : 0f;
            float shine = Vector2.Distance(new Vector2(u, v), center + new Vector2(-0.35f, 0.35f) * radius) < radius * 0.25f ? 0.95f : 0f;
            return Color.Lerp(c, Color.white, Mathf.Max(rim * 0.9f, Mathf.Max(fill, shine)));
        }

        /// <summary>Soft shadow of Blue on the background, offset down-right.</summary>
        private static Color FishShadow(float u, float v, float scale)
        {
            Vector2 p = Rotate((u - 0.5f - 0.025f) / scale, (v - 0.5f + 0.035f) / scale, FishAngle);
            float d = Ellipse(p.x, p.y + 0.02f, 0.2f, 0.42f);
            float a = Mathf.Clamp01(0.5f - d * 6f) * 0.28f;
            return new Color(0.62f, 0.2f, 0.05f, a);
        }

        /// <summary>Blue, top-down like in the game: tapered body, light fins, coral tail, big eyes, thick navy outline.</summary>
        private static Color BlueFish(float u, float v, float scale)
        {
            Vector2 p = Rotate((u - 0.5f) / scale, (v - 0.5f) / scale, FishAngle);
            float x = p.x, y = p.y;
            float aa = 1.2f / (1024f * scale);
            const float outlineW = 0.02f;

            float taper = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01((y + 0.26f) / 0.42f));
            float body = Ellipse(x / taper, y - 0.03f, 0.165f, 0.33f) * taper;
            float tailL = Ellipse(Rotate(x + 0.05f, y + 0.35f, -0.55f), 0.066f, 0.145f);
            float tailR = Ellipse(Rotate(x - 0.05f, y + 0.35f, 0.55f), 0.066f, 0.145f);
            float tail = Mathf.Min(tailL, tailR);
            float finL = Ellipse(Rotate(x + 0.165f, y - 0.06f, 0.95f), 0.05f, 0.11f);
            float finR = Ellipse(Rotate(x - 0.165f, y - 0.06f, -0.95f), 0.05f, 0.11f);
            float fins = Mathf.Min(finL, finR);
            float shape = Mathf.Min(body, Mathf.Min(tail, fins));

            Color c = Color.clear;
            float alpha = Mathf.Clamp01(0.5f - shape / aa);
            if (alpha > 0f)
            {
                if (body < 0f)
                {
                    // Lit from the upper left, a lighter stripe down the back, dark spots, a glossy highlight
                    float light = Mathf.Clamp01(0.5f - x * 2.2f + (y - 0.05f) * 0.9f);
                    c = Color.Lerp(BlueDark, BlueLight, light);
                    float spine = Mathf.Clamp01(1f - Mathf.Abs(x) / 0.07f) * Mathf.Clamp01(1f - Mathf.Abs(y + 0.02f) / 0.24f);
                    c = Color.Lerp(c, FinColor, spine * 0.35f);
                    float spots = Mathf.Min(Mathf.Min(
                        Vector2.Distance(p, new Vector2(0.06f, -0.03f)) - 0.028f,
                        Vector2.Distance(p, new Vector2(-0.065f, -0.1f)) - 0.022f),
                        Vector2.Distance(p, new Vector2(0.035f, -0.18f)) - 0.017f);
                    c = Color.Lerp(c, BlueSpot, Mathf.Clamp01(0.5f - spots / aa) * 0.85f);
                    float gloss = Ellipse(Rotate(x + 0.075f, y - 0.1f, -0.25f), 0.026f, 0.075f);
                    c = Color.Lerp(c, Color.white, Mathf.Clamp01(0.5f - gloss / aa) * 0.55f);
                }
                else if (tail <= fins)
                {
                    c = Color.Lerp(TailDark, TailColor, Mathf.Clamp01((-y - 0.25f) / 0.2f));
                    // fin rays
                    if (Mathf.Abs(Frac(x * 28f) - 0.5f) < 0.08f) c = Color.Lerp(c, TailDark, 0.5f);
                }
                else
                {
                    c = Color.Lerp(FinColor, Color.white, 0.3f);
                }
                // Thick cartoon outline
                c = Color.Lerp(c, Navy, Mathf.Clamp01((shape + outlineW) / aa + 0.5f));
                c.a = alpha;
            }

            // Big eyes on top (they bulge past the body's edge, like in the game)
            c = Over4(c, Eye(p, new Vector2(-0.088f, 0.22f), aa));
            c = Over4(c, Eye(p, new Vector2(0.088f, 0.22f), aa));
            return c;
        }

        private static Color Eye(Vector2 p, Vector2 center, float aa)
        {
            const float r = 0.07f, ring = 0.016f;
            float d = Vector2.Distance(p, center) - r;
            float a = Mathf.Clamp01(0.5f - d / aa);
            if (a <= 0f) return Color.clear;
            Color c = Color.white;
            // Looking ahead (up the river)
            Vector2 pupilC = center + new Vector2(0.008f, 0.022f);
            float pupil = Vector2.Distance(p, pupilC) - 0.04f;
            c = Color.Lerp(c, Pupil, Mathf.Clamp01(0.5f - pupil / aa));
            float shine = Vector2.Distance(p, pupilC + new Vector2(-0.013f, 0.014f)) - 0.014f;
            c = Color.Lerp(c, Color.white, Mathf.Clamp01(0.5f - shine / aa));
            float shine2 = Vector2.Distance(p, pupilC + new Vector2(0.015f, -0.014f)) - 0.006f;
            c = Color.Lerp(c, Color.white, Mathf.Clamp01(0.5f - shine2 / aa));
            c = Color.Lerp(c, Navy, Mathf.Clamp01((d + ring) / aa + 0.5f));
            c.a = a;
            return c;
        }

        private static float Frac(float x) => x - Mathf.Floor(x);

        /// <summary>Alpha-over that keeps transparency (for layers drawn on a transparent foreground).</summary>
        private static Color Over4(Color dst, Color src)
        {
            float a = src.a + dst.a * (1f - src.a);
            if (a <= 0f) return Color.clear;
            Color c = (src * src.a + dst * dst.a * (1f - src.a)) / a;
            c.a = a;
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
