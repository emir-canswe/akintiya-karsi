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
        // Painted at 2x the old 64x96 so the fish stay crisp on a phone; world size is unchanged (PPU scales with it).
        private const int Scale = 2;
        private const float PixelsPerUnit = 1.8f * Scale;

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

        private static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        /// <summary>k above 1 pushes a color away from gray (livelier), below 1 pulls it toward gray.</summary>
        private static Color Saturate(Color c, float k)
        {
            float l = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
            return new Color(Mathf.Clamp01(l + (c.r - l) * k), Mathf.Clamp01(l + (c.g - l) * k), Mathf.Clamp01(l + (c.b - l) * k), c.a);
        }

        private static Color Tint(Color c, Color toward, float t) => Color.Lerp(c, toward, Mathf.Clamp01(t));

        /// <summary>Cartoon outline: a deep, slightly navy shade of the fish's own color.</summary>
        private static Color OutlineFor(FishDef f)
        {
            Color baseCol = f.pattern == FishPattern.Rainbow ? new Color(0.45f, 0.3f, 0.7f) : f.body;
            Color c = Color.Lerp(baseCol * 0.38f, new Color(0.08f, 0.10f, 0.25f), 0.35f);
            c.a = 1f;
            return c;
        }

        private static readonly Vector3 LightDir = new Vector3(-0.5f, 0.6f, 0.62f).normalized;

        private static Sprite PaintBody(FishDef f)
        {
            const int w = 64 * Scale, h = 96 * Scale;
            Texture2D tex = NewTexture(w, h);
            Color outlineCol = OutlineFor(f);
            bool rainbow = f.pattern == FishPattern.Rainbow;
            Color[] pixels = new Color[w * h];
            float outlineW = 2.1f * Scale;
            float radiusPx = w * 0.34f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float ny = (y - h * 0.5f) / (h * 0.44f);
                    // Fish shape: full width at the head, tapering toward the tail
                    float taper = Mathf.Lerp(1f, 0.62f, Smooth(0.25f, -1f, ny));
                    float nx = (x - w * 0.5f) / (w * 0.34f * taper);
                    float d = nx * nx + ny * ny;
                    float r = Mathf.Sqrt(d);
                    float edgePx = (1f - r) * radiusPx * taper; // distance to the silhouette, in pixels
                    if (edgePx < -0.5f) { pixels[y * w + x] = Color.clear; continue; }

                    Color c = rainbow ? Color.HSVToRGB(Frac(0.62f - ny * 0.42f), 0.62f, 1f) : Saturate(f.body, 1.3f);

                    // Belly: lighter underside that fades smoothly into the back
                    float bx = nx / 0.55f, by = (ny + 0.08f) / 0.68f;
                    float bd = Mathf.Sqrt(bx * bx + by * by);
                    Color belly = rainbow ? Color.white : Saturate(f.belly, 1.1f);
                    c = Color.Lerp(c, belly, Smooth(1f, 0.15f, bd) * 0.9f);

                    c = ApplyPattern(f, c, nx, ny, x, y);

                    // Fine scales: faint arcs over the whole body so it reads as a fish, not a blob
                    if (ny < 0.5f && f.pattern != FishPattern.Scales && f.pattern != FishPattern.Facets)
                    {
                        float sv = ny * 8.5f, su = nx * 5.2f;
                        float row = Mathf.Floor(sv);
                        float sx = Frac(su + (Mathf.Abs(row) % 2f) * 0.5f) - 0.5f;
                        float sy = Frac(sv) - 0.2f;
                        float arc = 1f - Smooth(0f, 0.09f, Mathf.Abs(Mathf.Sqrt(sx * sx + sy * sy) - 0.5f));
                        c *= 1f - 0.08f * arc * (1f - 0.4f * r);
                    }

                    // Volume: a lit dome, bright top-left, shaded bottom-right
                    float z = Mathf.Sqrt(Mathf.Max(0f, 1f - d));
                    float lambert = Mathf.Max(0f, Vector3.Dot(new Vector3(nx, ny, z).normalized, LightDir));
                    c = new Color(c.r * (0.66f + 0.52f * lambert), c.g * (0.66f + 0.52f * lambert), c.b * (0.68f + 0.5f * lambert), 1f);
                    c *= 1f - 0.12f * Smooth(-0.55f, -1f, ny); // tail end sits in shade

                    // Cool rim light on the shaded edge (reflected river light)
                    float rim = Smooth(0.62f, 0.95f, r) * Smooth(-0.1f, 0.7f, nx * 0.7f - ny * 0.55f);
                    c = Tint(c, new Color(0.65f, 0.95f, 1f, 1f), rim * 0.5f);

                    // Glossy highlights: a long soft streak and a crisp spot
                    float hx = (nx + 0.34f) / 0.2f, hy = (ny - 0.18f) / 0.42f;
                    float streak = Mathf.Exp(-(hx * hx + hy * hy));
                    c = Tint(c, Color.white, streak * 0.7f);
                    float sx2 = (nx + 0.40f) / 0.07f, sy2 = (ny - 0.45f) / 0.1f;
                    c = Tint(c, Color.white, Mathf.Exp(-(sx2 * sx2 + sy2 * sy2)) * 0.95f);
                    float gx = (nx - 0.36f) / 0.06f, gy = (ny + 0.38f) / 0.08f;
                    c = Tint(c, Color.white, Mathf.Exp(-(gx * gx + gy * gy)) * 0.45f);

                    // Big shiny cartoon eyes near the top (+ny)
                    for (int side = -1; side <= 1; side += 2)
                        c = PaintEye(c, nx, ny, side, outlineCol);

                    // Smooth dark outline band along the silhouette
                    c = Color.Lerp(c, outlineCol, Smooth(outlineW + 1.2f, outlineW - 1.2f, edgePx));

                    c.a = Mathf.Clamp01(edgePx + 0.5f) * f.alpha;
                    pixels[y * w + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        /// <summary>One eye: shaded white, colored iris, deep pupil, two sparkles, heavier ring on the lid side.</summary>
        private static Color PaintEye(Color c, float nx, float ny, int side, Color outlineCol)
        {
            const float radius = 0.31f;
            Vector2 center = new Vector2(side * 0.47f, 0.54f);
            Vector2 u = (new Vector2(nx, ny) - center) / radius; // eye space: unit circle
            float e = u.magnitude;
            if (e > 1.06f) return c;

            // Sclera, slightly shaded under the lid
            Color eye = Color.Lerp(Color.white, new Color(0.78f, 0.86f, 0.95f, 1f), Smooth(0.1f, 1f, u.y * 0.8f + e * 0.3f));

            Vector2 irisC = new Vector2(side * 0.10f, 0.06f);
            float ir = (u - irisC).magnitude;
            if (ir < 0.68f)
            {
                float t = ir / 0.68f;
                Color irisLight = new Color(0.30f, 0.62f, 0.98f, 1f), irisDark = new Color(0.04f, 0.12f, 0.36f, 1f);
                eye = Color.Lerp(irisLight, irisDark, Smooth(0.15f, 1f, t) * 0.8f + (u.y < irisC.y ? 0.1f : 0f));
                if (ir < 0.38f) eye = new Color(0.02f, 0.05f, 0.10f, 1f); // pupil
            }
            // sparkles
            float s1 = (u - new Vector2(irisC.x - 0.25f, irisC.y + 0.30f)).magnitude;
            if (s1 < 0.24f) eye = Color.Lerp(eye, Color.white, Smooth(0.24f, 0.14f, s1));
            float s2 = (u - new Vector2(irisC.x + 0.26f, irisC.y - 0.28f)).magnitude;
            if (s2 < 0.11f) eye = Color.Lerp(eye, Color.white, Smooth(0.11f, 0.06f, s2) * 0.9f);

            // rim of the eye: dark ring, heavier on top like a lid
            float ring = 0.86f - 0.06f * Mathf.Clamp01(u.y);
            eye = Color.Lerp(eye, outlineCol, Smooth(ring, ring + 0.1f, e));
            return Color.Lerp(c, eye, 1f - Smooth(0.98f, 1.06f, e));
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
            const int w = 48 * Scale, h = 40 * Scale;
            Texture2D tex = NewTexture(w, h);
            Color[] pixels = new Color[w * h];
            Color outlineCol = OutlineFor(f);
            bool rainbow = f.pattern == FishPattern.Rainbow;
            float outlineW = 1.5f * Scale;
            for (int y = 0; y < h; y++)
            {
                float t = 1f - (float)y / h; // 0 at the top pivot, 1 at the tip
                float halfSpan = Mathf.Lerp(2f * Scale, w * 0.44f, t);
                float notchHalf = t > 0.72f ? (t - 0.72f) * 14f * Scale : 0f;
                Color baseCol = rainbow ? Color.HSVToRGB(Frac(0.15f + t * 0.6f), 0.6f, 1f) : Saturate(f.tail, 1.18f);
                Color tipCol = Tint(Saturate(f.tail, 1.3f), Color.white, 0.28f);

                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - w * 0.5f);
                    if (dx > halfSpan + 0.5f || dx < notchHalf - 0.5f) { pixels[y * w + x] = Color.clear; continue; }

                    float edge = Mathf.Min(halfSpan - dx, y + 0.5f);
                    if (notchHalf > 0f) edge = Mathf.Min(edge, dx - notchHalf);

                    float span01 = dx / Mathf.Max(halfSpan, 1f);
                    // Color runs from the body (deep, saturated) out to a sunlit, lighter tip
                    Color c = Color.Lerp(baseCol * 0.82f, tipCol, Smooth(0.15f, 1f, t));
                    // Soft fin rays fanning out, bright between them
                    float ray = Mathf.Abs(Frac(span01 * 3.5f) - 0.5f);
                    c *= 1f - 0.16f * (1f - Smooth(0f, 0.12f, ray));
                    // Light sheen down the middle
                    c = Tint(c, Color.white, 0.22f * (1f - span01) * t);

                    c = Color.Lerp(c, outlineCol, Smooth(outlineW + 1f, outlineW - 1f, edge));
                    c.a = Mathf.Clamp01(edge + 0.5f) * f.alpha * Mathf.Lerp(1f, 0.88f, t);
                    pixels[y * w + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.95f), PixelsPerUnit);
        }

        private static Sprite PaintFin(FishDef f)
        {
            const int w = 32 * Scale, h = 20 * Scale;
            Texture2D tex = NewTexture(w, h);
            Color[] pixels = new Color[w * h];
            Color outlineCol = OutlineFor(f);
            bool rainbow = f.pattern == FishPattern.Rainbow;
            float radiusPx = Mathf.Min(w * 0.45f, h * 0.42f);
            float outlineW = 1.4f * Scale;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - w * 0.5f) / (w * 0.45f);
                    float ny = (y - h * 0.5f) / (h * 0.42f);
                    float d = nx * nx + ny * ny;
                    float r = Mathf.Sqrt(d);
                    float edge = (1f - r) * radiusPx;
                    if (edge < -0.5f) { pixels[y * w + x] = Color.clear; continue; }

                    Color c = rainbow ? Color.HSVToRGB(Frac(0.5f + nx * 0.2f), 0.45f, 1f) : Saturate(f.fin, 1.2f);
                    // Lighter toward the outer tip, with a few fine rays and a gloss
                    c = Tint(c, Color.white, 0.25f * Smooth(0.1f, 1f, Mathf.Abs(nx)));
                    float ray = Mathf.Abs(Frac((Mathf.Atan2(ny, nx) / Mathf.PI) * 5f) - 0.5f);
                    c *= 1f - 0.14f * (1f - Smooth(0f, 0.12f, ray));
                    c = Tint(c, Color.white, 0.4f * Mathf.Exp(-((nx * nx) / 0.5f + ((ny - 0.35f) * (ny - 0.35f)) / 0.08f)));

                    c = Color.Lerp(c, outlineCol, Smooth(outlineW + 1f, outlineW - 1f, edge));
                    c.a = Mathf.Clamp01(edge + 0.5f) * f.alpha * 0.96f;
                    pixels[y * w + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }
    }
}
