using System.Collections.Generic;
using UnityEngine;
using Upstream.Data;

namespace Upstream.River
{
    /// <summary>
    /// Paints each <see cref="FishDef"/> into body / tail / fin sprites at runtime (no image files), cached per fish.
    /// Same layout as the original trout so the player renderer can swap skins freely: the fish points up (+Y),
    /// eyes near the top, tail pivot at its top edge.
    /// </summary>
    public static class FishSkinFactory
    {
        private const float PixelsPerUnit = 1.8f; // matches WatercolorSpriteFactory's fish sprites

        private static readonly Dictionary<int, Sprite> Bodies = new Dictionary<int, Sprite>();
        private static readonly Dictionary<int, Sprite> Tails = new Dictionary<int, Sprite>();
        private static readonly Dictionary<int, Sprite> Fins = new Dictionary<int, Sprite>();

        private static readonly Vector2[] SpotPositions =
        {
            new Vector2(-0.38f, -0.05f), new Vector2(0.35f, -0.18f), new Vector2(-0.22f, -0.32f), new Vector2(0.25f, 0.12f),
            new Vector2(0.05f, -0.55f), new Vector2(-0.05f, 0.25f),
        };

        public static Sprite GetBody(FishDef fish)
        {
            if (Bodies.TryGetValue(fish.id, out Sprite cached) && cached != null) return cached;
            Sprite s = PaintBody(fish);
            Bodies[fish.id] = s;
            return s;
        }

        public static Sprite GetTail(FishDef fish)
        {
            if (Tails.TryGetValue(fish.id, out Sprite cached) && cached != null) return cached;
            Sprite s = PaintTail(fish);
            Tails[fish.id] = s;
            return s;
        }

        public static Sprite GetFin(FishDef fish)
        {
            if (Fins.TryGetValue(fish.id, out Sprite cached) && cached != null) return cached;
            Sprite s = PaintFin(fish);
            Fins[fish.id] = s;
            return s;
        }

        private static Texture2D NewTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        private static float Frac(float v) => v - Mathf.Floor(v);

        // Cheap deterministic hash in [0,1) for star fields.
        private static float Hash(int x, int y) => Frac(Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.547f);

        private static Sprite PaintBody(FishDef f)
        {
            const int w = 64, h = 96;
            Texture2D tex = NewTexture(w, h);
            Color eyeBlack = new Color(0.06f, 0.13f, 0.17f, 1f);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - w * 0.5f) / (w * 0.34f);
                    float ny = (y - h * 0.5f) / (h * 0.44f);
                    float d = nx * nx + ny * ny;
                    if (d > 1f) { pixels[y * w + x] = Color.clear; continue; }

                    float grain = 0.95f + 0.05f * Mathf.PerlinNoise(x * 0.2f + f.id * 3.1f, y * 0.2f);
                    Color c = f.pattern == FishPattern.Rainbow
                        ? Color.HSVToRGB(Frac(0.62f - ny * 0.42f), 0.62f, 1f)
                        : f.body;
                    c *= grain;

                    // Belly
                    float bx = nx / 0.52f, by = (ny + 0.08f) / 0.65f;
                    float bd = bx * bx + by * by;
                    if (bd < 1f) c = Color.Lerp(c, f.pattern == FishPattern.Rainbow ? Color.white : f.belly, (1f - bd) * 0.85f);

                    c = ApplyPattern(f, c, nx, ny, x, y);

                    // Eyes near the top (+ny)
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 p = new Vector2(nx, ny);
                        if (Vector2.Distance(p, new Vector2(side * 0.58f, 0.60f)) < 0.11f) c = eyeBlack;
                        else if (Vector2.Distance(p, new Vector2(side * 0.54f, 0.56f)) < 0.19f) c = Color.white;
                    }

                    c.a = Mathf.Clamp01((1f - d) * 8f) * f.alpha;
                    pixels[y * w + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        private static Color ApplyPattern(FishDef f, Color c, float nx, float ny, int x, int y)
        {
            Vector2 p = new Vector2(nx, ny);
            switch (f.pattern)
            {
                case FishPattern.Spots:
                    for (int s = 0; s < SpotPositions.Length; s++)
                    {
                        // The starter trout keeps its original four spots.
                        if (f.id == FishCatalog.StarterId && s >= 4) break;
                        if (Vector2.Distance(p, SpotPositions[s]) < 0.11f) c = Color.Lerp(c, f.accent, 0.5f);
                    }
                    break;

                case FishPattern.Stripes:
                {
                    // Bands across the body, slightly curved like a real perch.
                    float v = ny + nx * nx * 0.18f;
                    float band = Mathf.Abs(Frac(v * 2.3f + 0.15f) - 0.5f);
                    if (band < 0.11f && ny < 0.42f) c = Color.Lerp(c, f.accent, 0.8f);
                    break;
                }

                case FishPattern.Bands:
                {
                    // Clownfish: three white bands with thin dark outlines.
                    float[] centers = { 0.30f, -0.12f, -0.62f };
                    float v = ny - nx * nx * 0.12f;
                    for (int i = 0; i < centers.Length; i++)
                    {
                        float dist = Mathf.Abs(v - centers[i]);
                        float half = i == 2 ? 0.08f : 0.12f;
                        if (dist < half) c = f.accent;
                        else if (dist < half + 0.035f) c = new Color(0.12f, 0.05f, 0.04f, 1f);
                    }
                    break;
                }

                case FishPattern.Scales:
                {
                    // Overlapping scallops; rows are offset by half a scale.
                    float u = nx * 4.2f, vv = ny * 6.0f;
                    float row = Mathf.Floor(vv);
                    float cx = Frac(u + (Mathf.Abs(row) % 2f) * 0.5f) - 0.5f;
                    float cy = Frac(vv) - 0.15f;
                    float r = Mathf.Sqrt(cx * cx + cy * cy);
                    if (Mathf.Abs(r - 0.5f) < 0.08f && ny < 0.42f) c = Color.Lerp(c, f.accent, 0.55f);
                    break;
                }

                case FishPattern.Patches:
                {
                    float n = Mathf.PerlinNoise(nx * 2.4f + 11.3f, ny * 2.4f + 4.7f);
                    if (n > 0.56f && ny < 0.45f) c = Color.Lerp(c, f.accent, Mathf.Clamp01((n - 0.56f) * 14f) * 0.92f);
                    break;
                }

                case FishPattern.Stars:
                {
                    float hsh = Hash(x, y);
                    if (hsh > 0.985f && ny < 0.42f) c = f.accent;
                    else if (hsh > 0.97f && ny < 0.42f) c = Color.Lerp(c, f.accent, 0.45f);
                    break;
                }

                case FishPattern.Cracks:
                {
                    // Glowing lava veins along noise contour lines.
                    float n1 = Mathf.PerlinNoise(nx * 3.1f + 2.1f, ny * 3.1f + 8.4f);
                    float n2 = Mathf.PerlinNoise(nx * 6.3f + 5.7f, ny * 6.3f + 1.9f);
                    float vein = Mathf.Min(Mathf.Abs(n1 - 0.5f), Mathf.Abs(n2 - 0.5f) * 1.6f);
                    if (vein < 0.035f && ny < 0.45f) c = Color.Lerp(f.accent, new Color(1f, 0.9f, 0.4f, 1f), 1f - vein / 0.035f);
                    break;
                }

                case FishPattern.Facets:
                {
                    // Crystal: diamond facets with alternating brightness and bright edges.
                    float a = (nx + ny) * 2.6f, b = (nx - ny) * 2.6f;
                    bool odd = ((int)Mathf.Floor(a) + (int)Mathf.Floor(b)) % 2 == 0;
                    c *= odd ? 1.08f : 0.86f;
                    float edge = Mathf.Min(Mathf.Min(Frac(a), 1f - Frac(a)), Mathf.Min(Frac(b), 1f - Frac(b)));
                    if (edge < 0.06f) c = Color.Lerp(c, f.accent, 0.7f);
                    break;
                }

                case FishPattern.Rainbow:
                {
                    // Pearly shine streak.
                    float streak = Mathf.Abs(nx + 0.35f - ny * 0.25f);
                    if (streak < 0.08f && ny < 0.45f) c = Color.Lerp(c, Color.white, 0.55f);
                    break;
                }
            }
            c.a = 1f;
            return c;
        }

        private static Sprite PaintTail(FishDef f)
        {
            const int w = 48, h = 40;
            Texture2D tex = NewTexture(w, h);
            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = 1f - (float)y / h; // 0 at the top pivot, 1 at the tip
                float halfSpan = Mathf.Lerp(2f, w * 0.44f, t);
                float notch = t > 0.72f ? (t - 0.72f) * 28f : 0f;
                Color col = f.pattern == FishPattern.Rainbow ? Color.HSVToRGB(Frac(0.15f + t * 0.6f), 0.6f, 1f) : f.tail;

                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - w * 0.5f);
                    if (dx <= halfSpan && dx >= notch * 0.5f)
                    {
                        // Fin rays: faint darker lines fanning out.
                        float ray = Mathf.Abs(Frac(dx / Mathf.Max(halfSpan, 1f) * 3f) - 0.5f) < 0.08f ? 0.88f : 1f;
                        Color c = col * ray;
                        c.a = Mathf.Clamp01((halfSpan - dx) * 1.5f) * f.alpha;
                        pixels[y * w + x] = c;
                    }
                    else
                    {
                        pixels[y * w + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.95f), PixelsPerUnit);
        }

        private static Sprite PaintFin(FishDef f)
        {
            const int w = 32, h = 20;
            Texture2D tex = NewTexture(w, h);
            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - w * 0.5f) / (w * 0.45f);
                    float ny = (y - h * 0.5f) / (h * 0.42f);
                    float d = nx * nx + ny * ny;
                    if (d > 1f) { pixels[y * w + x] = Color.clear; continue; }
                    Color c = f.pattern == FishPattern.Rainbow ? Color.HSVToRGB(Frac(0.5f + nx * 0.2f), 0.45f, 1f) : f.fin;
                    c.a = Mathf.Clamp01((1f - d) * 5f) * 0.92f * f.alpha;
                    pixels[y * w + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }
    }
}
