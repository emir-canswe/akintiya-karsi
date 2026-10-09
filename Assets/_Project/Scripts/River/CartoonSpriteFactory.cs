using UnityEngine;

namespace Upstream.River
{
    /// <summary>
    /// Bright, kid-friendly cartoon art painted at runtime (no image files): every shape is described by a
    /// signed distance function, which gives all sprites the same crisp dark outline and smooth edges.
    /// Tiling textures (water light, grass, shore) are periodic so they scroll without seams.
    /// </summary>
    public static class CartoonSpriteFactory
    {
        // Signed distance in pixels: negative inside the shape.
        private delegate float Sdf(Vector2 p);
        private delegate Color Fill(Vector2 p, float d);

        private static Sprite shrimp, star, rock, log, hook, heart, bubble, glint, sparkle;
        private static Sprite caustics, grassTile, shoreTile, waterGlow;
        private static Sprite tree, flowerBush, mushroom, stone, lilyPad;
        private static Sprite barFill, homeIcon, playIcon, pauseIcon, wave, closeIcon, dragIcon, bigWave, foamStrip;

        public static readonly Color GrassColor = Hex("#6CCB4A");

        // ------------------------------------------------------------------ helpers

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }

        private static float Frac(float v) => v - Mathf.Floor(v);

        /// <summary>
        /// GLSL-style smoothstep: 0 below <paramref name="edge0"/>, 1 above <paramref name="edge1"/>, smooth between.
        /// (Unity's Mathf.SmoothStep interpolates between two values instead, which is not what masks need.)
        /// </summary>
        private static float Smooth(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }
        private static float Hash(float x, float y) => Frac(Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.547f);

        private static float SdCircle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

        // Good-enough ellipse distance for anti-aliasing and outlines.
        private static float SdEllipse(Vector2 p, Vector2 c, Vector2 r)
        {
            Vector2 q = new Vector2((p.x - c.x) / r.x, (p.y - c.y) / r.y);
            return (q.magnitude - 1f) * Mathf.Min(r.x, r.y);
        }

        private static float SdBox(Vector2 p, Vector2 c, Vector2 half, float radius)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + new Vector2(radius, radius);
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        private static Vector2 Rotate(Vector2 p, Vector2 pivot, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad, cs = Mathf.Cos(r), sn = Mathf.Sin(r);
            Vector2 d = p - pivot;
            return pivot + new Vector2(d.x * cs - d.y * sn, d.x * sn + d.y * cs);
        }

        private static float SmoothMin(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }

        /// <summary>Paints an outlined shape: fill inside, outline band at the edge, anti-aliased rim.</summary>
        private static Texture2D Paint(int w, int h, Sdf sdf, Fill fill, Color outline, float outlineWidth)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = sdf(p);
                    if (d > 1f) { px[y * w + x] = Color.clear; continue; }

                    Color c;
                    if (outlineWidth <= 0f) c = fill(p, d);
                    else if (d > -outlineWidth) c = outline;
                    else
                    {
                        Color f = fill(p, d);
                        c = Color.Lerp(f, outline, Mathf.Clamp01(d + outlineWidth + 1f)); // soft inner edge
                    }
                    c.a *= Mathf.Clamp01(0.5f - d);
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Sprite ToSprite(Texture2D tex, Vector2 pivot, float ppu) =>
            Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, ppu);

        /// <summary>Light from the top-left: 1 at the lit side, 0 at the shaded side.</summary>
        private static float Light(Vector2 p, Vector2 center, float radius)
        {
            Vector2 d = (p - center) / Mathf.Max(radius, 1f);
            return Mathf.Clamp01(0.5f + (-d.x + d.y) * 0.5f);
        }

        private static Color Shine(Color c, Vector2 p, Vector2 spot, float radius, float strength)
        {
            float s = Mathf.Clamp01(1f - (p - spot).magnitude / radius);
            return Color.Lerp(c, Color.white, s * s * strength);
        }

        // ------------------------------------------------------------------ pickups

        /// <summary>Curled coral shrimp with an eye, segments and antennae, on a soft light halo.</summary>
        public static Sprite GetShrimp()
        {
            if (shrimp != null) return shrimp;
            const int s = 44;
            Vector2 c = new Vector2(22f, 21f);
            Color body = Hex("#FF8A65"), belly = Hex("#FFC6A8"), seg = Hex("#E8603F"), outline = Hex("#9C2F1E");

            // Body: circles along a C-shaped arc, thick at the head, thin at the tail.
            const int n = 9;
            var centers = new Vector2[n];
            var radii = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float a = Mathf.Lerp(-35f, 215f, t) * Mathf.Deg2Rad;
                centers[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 9f;
                radii[i] = Mathf.Lerp(6.8f, 3.2f, t);
            }
            Vector2 head = centers[0];
            Vector2 tailTip = centers[n - 1] + new Vector2(-3f, -4f);

            Sdf sdf = p =>
            {
                float d = 99f;
                for (int i = 0; i < n; i++) d = SmoothMin(d, SdCircle(p, centers[i], radii[i]), 1.5f);
                d = Mathf.Min(d, SdEllipse(p, tailTip, new Vector2(4.5f, 2.6f)));
                return d;
            };
            Fill fill = (p, d) =>
            {
                // Segment bands: nearest arc index
                int best = 0; float bd = 99f;
                for (int i = 0; i < n; i++) { float dd = (p - centers[i]).magnitude; if (dd < bd) { bd = dd; best = i; } }
                Color col = Color.Lerp(body, belly, Mathf.Clamp01(-(d + 2f) / 4f) * 0.35f); // lighter toward the middle
                if (best % 2 == 1 && bd > radii[best] * 0.55f) col = Color.Lerp(col, seg, 0.45f);
                // Eye
                Vector2 eye = head + new Vector2(1.5f, 2.2f);
                float e = (p - eye).magnitude;
                if (e < 2.4f) col = e < 0.9f && p.x < eye.x ? Color.white : Hex("#1B1B2F");
                return Shine(col, p, head + new Vector2(-2f, 3f), 4f, 0.6f);
            };

            Texture2D tex = Paint(s, s, sdf, fill, outline, 1.6f);
            // Halo + antennae painted over the clear background
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    Color cur = tex.GetPixel(x, y);
                    // Antennae: two thin arcs from the head
                    for (int k = -1; k <= 1; k += 2)
                    {
                        Vector2 a0 = head + new Vector2(3f, 2f * k);
                        Vector2 dir = new Vector2(1f, 0.55f * k).normalized;
                        float t = Vector2.Dot(p - a0, dir);
                        if (t > 0f && t < 11f)
                        {
                            Vector2 onLine = a0 + dir * t + new Vector2(0f, t * t * 0.03f * k);
                            if ((p - onLine).magnitude < 0.75f && cur.a < 0.5f) cur = outline;
                        }
                    }
                    if (cur.a < 0.05f)
                    {
                        float g = Mathf.Clamp01(1f - (p - c).magnitude / 21f);
                        cur = new Color(1f, 1f, 0.85f, g * g * 0.45f);
                    }
                    tex.SetPixel(x, y, cur);
                }
            }
            tex.Apply();
            shrimp = ToSprite(tex, new Vector2(0.5f, 0.5f), 1.15f);
            return shrimp;
        }

        /// <summary>Smiling golden star with a warm glow (heals / power boost).</summary>
        public static Sprite GetStar()
        {
            if (star != null) return star;
            const int s = 52;
            Vector2 c = new Vector2(26f, 25f);
            Color top = Hex("#FFF06A"), bottom = Hex("#FFB300"), outline = Hex("#B85C00");

            Sdf sdf = p =>
            {
                // Rounded 5-point star: radius modulated by angle.
                Vector2 d = p - c;
                float a = Mathf.Atan2(d.x, d.y); // 0 = up
                float k = Mathf.Cos(5f * a);
                float r = Mathf.Lerp(9.5f, 17f, Mathf.SmoothStep(0f, 1f, (k + 1f) * 0.5f));
                return (d.magnitude - r) * 0.85f;
            };
            Fill fill = (p, d) =>
            {
                Color col = Color.Lerp(bottom, top, Mathf.Clamp01((p.y - c.y + 14f) / 28f));
                // Face: eyes + smile
                for (int k = -1; k <= 1; k += 2)
                {
                    Vector2 eye = c + new Vector2(4.2f * k, 2.5f);
                    if (SdEllipse(p, eye, new Vector2(1.6f, 2.4f)) < 0f) col = Hex("#5A2A00");
                    if ((p - (eye + new Vector2(-0.5f, 0.9f))).magnitude < 0.7f) col = Color.white;
                }
                Vector2 m = p - (c + new Vector2(0f, -1.5f));
                if (m.y < 0f && Mathf.Abs(m.magnitude - 3.6f) < 0.8f) col = Hex("#5A2A00");
                // Rosy cheeks
                for (int k = -1; k <= 1; k += 2)
                    if ((p - (c + new Vector2(7f * k, -1.5f))).magnitude < 1.8f) col = Color.Lerp(col, Hex("#FF8A65"), 0.6f);
                return Shine(col, p, c + new Vector2(-6f, 7f), 6f, 0.7f);
            };

            Texture2D tex = Paint(s, s, sdf, fill, outline, 1.8f);
            AddGlow(tex, c, 25f, new Color(1f, 0.9f, 0.4f, 1f), 0.55f);
            star = ToSprite(tex, new Vector2(0.5f, 0.5f), 1.15f);
            return star;
        }

        private static void AddGlow(Texture2D tex, Vector2 c, float radius, Color col, float strength)
        {
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    Color cur = tex.GetPixel(x, y);
                    if (cur.a > 0.95f) continue;
                    float g = Mathf.Clamp01(1f - (new Vector2(x + 0.5f, y + 0.5f) - c).magnitude / radius);
                    Color glow = new Color(col.r, col.g, col.b, g * g * strength);
                    // "under" blend: existing pixel stays on top of the glow
                    float a = cur.a + glow.a * (1f - cur.a);
                    Color rgb = a > 0f ? (cur * cur.a + glow * glow.a * (1f - cur.a)) / a : Color.clear;
                    rgb.a = a;
                    tex.SetPixel(x, y, rgb);
                }
            }
            tex.Apply();
        }

        // ------------------------------------------------------------------ obstacles

        /// <summary>Chunky boulder with a mossy cap, light from the top-left and a wavy foam ring.</summary>
        public static Sprite GetRock()
        {
            if (rock != null) return rock;
            const int s = 80;
            Vector2 c = new Vector2(40f, 40f);
            Color lit = Hex("#C9CBE3"), mid = Hex("#9A9CBC"), dark = Hex("#6C6D93"), outline = Hex("#34325A");
            Color moss = Hex("#7BD448"), mossLit = Hex("#B4F07A");

            float Radius(float ang) => 25f * (0.88f + 0.12f * Mathf.Sin(ang * 3f + 1.2f) * Mathf.Cos(ang * 2f));
            Sdf sdf = p =>
            {
                Vector2 d = p - c;
                return d.magnitude - Radius(Mathf.Atan2(d.y, d.x));
            };
            Fill fill = (p, d) =>
            {
                float l = Light(p, c, 26f);
                Color col = l > 0.62f ? lit : (l > 0.36f ? mid : dark);
                col = Color.Lerp(col, mid, 0.25f);
                // Moss cap on top
                float n = Mathf.PerlinNoise(p.x * 0.12f + 3f, p.y * 0.12f + 9f);
                if (p.y - c.y + n * 10f > 9f)
                {
                    col = Hash(Mathf.Floor(p.x / 3f), Mathf.Floor(p.y / 3f)) > 0.8f ? mossLit : moss;
                }
                // A couple of cracks
                if (Mathf.Abs((p.x - c.x) * 0.6f + (p.y - c.y) + 8f) < 0.7f && p.x > c.x && d < -4f) col = dark;
                return Shine(col, p, c + new Vector2(-9f, 6f), 8f, 0.45f);
            };
            Texture2D tex = Paint(s, s, sdf, fill, outline, 2.2f);

            // Wavy foam ring around the rock (behind it).
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    Color cur = tex.GetPixel(x, y);
                    if (cur.a > 0.9f) continue;
                    Vector2 d = new Vector2(x + 0.5f, y + 0.5f) - c;
                    float ang = Mathf.Atan2(d.y, d.x);
                    float ring = d.magnitude - (Radius(ang) + 6f + Mathf.Sin(ang * 9f) * 1.5f);
                    float a = Mathf.Clamp01(1.6f - Mathf.Abs(ring)) * 0.8f;
                    if (a > cur.a) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            rock = ToSprite(tex, new Vector2(0.5f, 0.5f), 1.15f);
            return rock;
        }

        /// <summary>Round log with bark grooves and cut ends showing tree rings. 9-sliced so the ends never stretch.</summary>
        public static Sprite GetLog()
        {
            if (log != null) return log;
            const int w = 128, h = 30;
            Vector2 c = new Vector2(w * 0.5f, h * 0.5f);
            Color bark = Hex("#B8743B"), barkLit = Hex("#D99A58"), groove = Hex("#7E4A22"), outline = Hex("#4E2A12");
            Color cut = Hex("#F5D9A0"), ring = Hex("#C99657");

            Sdf sdf = p => SdBox(p, c, new Vector2(62f, 12.5f), 12.5f);
            Fill fill = (p, d) =>
            {
                float endDist = Mathf.Min(p.x - 2f, w - 2f - p.x);
                if (endDist < 11f)
                {
                    // Cut end: rings around the end's center
                    Vector2 ec = new Vector2(p.x < c.x ? 13f : w - 13f, c.y);
                    float r = SdEllipse(p, ec, new Vector2(8.5f, 11f));
                    if (r < 0f)
                    {
                        float rr = (p - ec).magnitude;
                        return Mathf.Abs(Frac(rr / 3.2f) - 0.5f) < 0.16f ? ring : cut;
                    }
                }
                float v = (p.y - c.y) / 12.5f; // -1..1 across the log
                Color col = v > 0.25f ? barkLit : bark;
                // Bark grooves running along the log
                float g = Mathf.Abs(Frac(p.y / 5.5f + Mathf.Sin(p.x * 0.11f) * 0.15f) - 0.5f);
                if (g < 0.09f && Mathf.Abs(v) < 0.8f) col = groove;
                return col;
            };
            Texture2D tex = Paint(w, h, sdf, fill, outline, 2f);
            log = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1.1f, 0, SpriteMeshType.FullRect,
                                new Vector4(26f, 0f, 26f, 0f));
            return log;
        }

        /// <summary>
        /// Rolling wave band: deep blue at the back, lighter toward the front, a scalloped white foam crest
        /// facing downstream (the bottom). 9-sliced so it can stretch across any width.
        /// </summary>
        public static Sprite GetWave()
        {
            if (wave != null) return wave;
            const int w = 128, h = 40;
            Vector2 c = new Vector2(w * 0.5f, 21f);
            Color back = Hex("#2F6FE0"), front = Hex("#5FC8FF"), foam = Color.white, outline = Hex("#163C8C");
            Sdf sdf = p =>
            {
                float band = SdBox(p, c, new Vector2(62f, 13f), 13f);
                // Foam scallops bulging out of the bottom edge
                float scallop = 99f;
                for (int i = 0; i < 9; i++) scallop = Mathf.Min(scallop, SdCircle(p, new Vector2(10f + i * 13.5f, 9f), 6.5f));
                return SmoothMin(band, scallop, 3f);
            };
            Fill fill = (p, d) =>
            {
                float t = Mathf.Clamp01((p.y - 8f) / 26f); // 0 front (bottom) .. 1 back (top)
                Color col = Color.Lerp(front, back, t);
                // Curl lines across the wave
                float curl = Mathf.Abs(Frac((p.x + Mathf.Sin(p.y * 0.35f) * 4f) / 16f) - 0.5f);
                if (curl < 0.05f && p.y > 16f) col = Color.Lerp(col, Color.white, 0.35f);
                // Foam crest
                col = Color.Lerp(col, foam, Mathf.Clamp01((14f - p.y) / 4f));
                return Shine(col, p, new Vector2(p.x, 28f), 4f, 0.25f);
            };
            Texture2D tex = Paint(w, h, sdf, fill, outline, 2f);
            wave = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1.1f, 0, SpriteMeshType.FullRect,
                                 new Vector4(24f, 0f, 24f, 0f));
            return wave;
        }

        /// <summary>
        /// Big river-wide wave, tiling sideways: a scalloped foam crest at the front (bottom, facing downstream),
        /// a darker curl just behind it and a sunlit back that fades into the river.
        /// </summary>
        public static Sprite GetBigWave()
        {
            if (bigWave != null) return bigWave;
            const int w = 128, h = 80;
            Color lip = Hex("#1F5CC4"), face = Hex("#3C8FEA"), back = Hex("#7FD6FF"), outline = Hex("#163C8C");
            Color foamShade = Hex("#CDEFFF");
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    // Everything repeats every 16/32/128 px so the texture tiles without seams.
                    float foamTop = 25f + 2.5f * Mathf.Sin(p.x / w * Mathf.PI * 2f * 4f);
                    float scallop = SdCircle(new Vector2(Frac(p.x / 16f) * 16f, p.y), new Vector2(8f, 14f), 8.2f);
                    float bubble = SdCircle(new Vector2(Frac((p.x + 8f) / 32f) * 32f, p.y), new Vector2(16f, 8.5f), 4.6f);
                    float foamBand = Mathf.Max(14f - p.y, p.y - foamTop);
                    float foam = Mathf.Min(Mathf.Min(scallop, bubble), foamBand);

                    Color c;
                    if (foam < 0f)
                    {
                        // Foam: white, a touch of blue at the bottom of each puff, dark rim underneath.
                        c = Color.Lerp(foamShade, Color.white, Mathf.Clamp01((p.y - 6f) / 10f));
                        if (foam > -2.2f && p.y < foamTop - 3f) c = Color.Lerp(c, outline, Mathf.Clamp01(foam + 2.2f));
                        c.a = 1f;
                    }
                    else if (p.y > foamTop - 1f)
                    {
                        // Water body: dark curl right behind the crest, lighter toward the back, fading out.
                        float t = Mathf.Clamp01((p.y - foamTop) / (h - foamTop));
                        c = t < 0.25f ? Color.Lerp(lip, face, t / 0.25f) : Color.Lerp(face, back, (t - 0.25f) / 0.75f);
                        // Curving streaks of light along the wave
                        float streak = Mathf.Abs(Frac((p.x + Mathf.Sin(p.y * 0.14f) * 9f) / 32f) - 0.5f);
                        if (streak < 0.035f && t > 0.12f && t < 0.7f) c = Color.Lerp(c, Color.white, 0.45f * (1f - t));
                        c.a = 0.95f * (1f - Smooth(0.45f, 1f, t)) * Mathf.Clamp01(p.y - foamTop + 1f);
                    }
                    else
                    {
                        // Anti-aliased rim just outside the foam
                        c = outline;
                        c.a = Mathf.Clamp01(1f - foam) * (p.y < foamTop ? 1f : 0f);
                    }
                    px[y * w + x] = c;
                }
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
            tex.SetPixels(px);
            tex.Apply();
            bigWave = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1.1f, 0, SpriteMeshType.FullRect);
            return bigWave;
        }

        /// <summary>Strip of churning foam puffs (tiles sideways); laid over a big wave's crest and swayed.</summary>
        public static Sprite GetFoamStrip()
        {
            if (foamStrip != null) return foamStrip;
            const int w = 64, h = 24;
            Color shade = Hex("#D8F3FF");
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = SdCircle(new Vector2(Frac(p.x / 16f) * 16f, p.y), new Vector2(8f, 12f), 6.4f);
                    d = Mathf.Min(d, SdCircle(new Vector2(Frac((p.x + 8f) / 32f) * 32f, p.y), new Vector2(16f, 15f), 4.2f));
                    d = Mathf.Min(d, SdCircle(p, new Vector2(40f, 6f), 2.6f));
                    d = Mathf.Min(d, SdCircle(p, new Vector2(12f, 5f), 2f));
                    Color c = Color.Lerp(shade, Color.white, Mathf.Clamp01((p.y - 6f) / 10f));
                    c.a = Mathf.Clamp01(0.5f - d);
                    px[y * w + x] = c;
                }
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
            tex.SetPixels(px);
            tex.Apply();
            foamStrip = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1.1f, 0, SpriteMeshType.FullRect);
            return foamStrip;
        }

        // ------------------------------------------------------------------ predators

        /// <summary>Look of one predator species, painted top-down with the nose pointing down.</summary>
        private class PredatorSpec
        {
            public float nose, tailEnd;          // body extent below/above the pivot (texture px)
            public float halfWidth;              // widest half-width (px)
            public float a = 0.5f, b = 0.7f;     // profile exponents: halfWidth ∝ t^a (1-t)^b
            public Color spine, back, side, belly, finDark, finLit, outline, gill;
            public float finT = 0.42f, finOut = 21f, finLen = 15f, finThick = 5f, finAngle = 32f;
            public float eyeT = 0.17f, eyeSize = 1f;
            public bool dorsal, gills, teeth, whiskers, spots, blotches, brows;
            public float tailAttach;             // px above the pivot where the tail joins
            public float tailScale = 1f;
            public bool roundTail;
        }

        private static readonly System.Collections.Generic.Dictionary<Data.PredatorKind, Sprite> predatorBodies =
            new System.Collections.Generic.Dictionary<Data.PredatorKind, Sprite>();
        private static readonly System.Collections.Generic.Dictionary<Data.PredatorKind, Sprite> predatorTails =
            new System.Collections.Generic.Dictionary<Data.PredatorKind, Sprite>();

        private static PredatorSpec Spec(Data.PredatorKind kind)
        {
            switch (kind)
            {
                case Data.PredatorKind.Pike:
                    // Long, slim ambush hunter: olive-green with pale bean spots and a duck-bill snout
                    return new PredatorSpec
                    {
                        nose = 54f, tailEnd = 46f, halfWidth = 9.5f, a = 0.62f, b = 0.5f,
                        spine = Hex("#3E5A2A"), back = Hex("#5F8A3A"), side = Hex("#9CBF5C"), belly = Hex("#E8F0C0"),
                        finDark = Hex("#6E7F2C"), finLit = Hex("#B9C760"), outline = Hex("#1E2E12"), gill = Hex("#2A3D1A"),
                        finT = 0.55f, finOut = 12f, finLen = 9f, finThick = 3.5f, finAngle = 30f,
                        eyeT = 0.13f, eyeSize = 0.9f, spots = true, gills = true, tailAttach = 40f, tailScale = 0.8f,
                    };
                case Data.PredatorKind.Piranha:
                    // Small, round, red-bellied and toothy (comes in schools)
                    return new PredatorSpec
                    {
                        nose = 22f, tailEnd = 18f, halfWidth = 15f, a = 0.45f, b = 0.5f,
                        spine = Hex("#3B4150"), back = Hex("#6A5560"), side = Hex("#D9524A"), belly = Hex("#FF6A4D"),
                        finDark = Hex("#B5322B"), finLit = Hex("#E8604F"), outline = Hex("#1A1C24"), gill = Hex("#2B2F3A"),
                        finT = 0.45f, finOut = 15f, finLen = 7f, finThick = 3.5f, finAngle = 28f,
                        eyeT = 0.28f, eyeSize = 1.15f, teeth = true, brows = true, tailAttach = 13f, tailScale = 0.6f,
                    };
                case Data.PredatorKind.Catfish:
                    // Big, wide, slow bottom-dweller with whiskers and mottled skin
                    return new PredatorSpec
                    {
                        nose = 46f, tailEnd = 36f, halfWidth = 24f, a = 0.32f, b = 0.9f,
                        spine = Hex("#4A3B2A"), back = Hex("#6E5A3E"), side = Hex("#9C8460"), belly = Hex("#E2D3B0"),
                        finDark = Hex("#5A4A33"), finLit = Hex("#8C7550"), outline = Hex("#24190E"), gill = Hex("#33281C"),
                        finT = 0.33f, finOut = 26f, finLen = 15f, finThick = 6f, finAngle = 25f,
                        eyeT = 0.13f, eyeSize = 0.75f, whiskers = true, blotches = true, tailAttach = 27f, tailScale = 1.05f, roundTail = true,
                    };
                default:
                    // Shark: slate-blue back over a pale belly rim, gill slits, shaded dorsal fin
                    return new PredatorSpec
                    {
                        nose = 47f, tailEnd = 36f, halfWidth = 18.5f,
                        spine = Hex("#34557F"), back = Hex("#4C74A6"), side = Hex("#7FA4D3"), belly = Hex("#DCEBFA"),
                        finDark = Hex("#2E4C74"), finLit = Hex("#5B82B6"), outline = Hex("#122746"), gill = Hex("#22395C"),
                        dorsal = true, gills = true, tailAttach = 30f,
                    };
            }
        }

        /// <summary>World-unit distance from a predator's pivot to where its tail attaches.</summary>
        public static float PredatorTailAttachY(Data.PredatorKind kind) => Spec(kind).tailAttach / 1.15f;

        /// <summary>Predator body seen from above, nose down. The tail is separate (<see cref="GetPredatorTail"/>).</summary>
        public static Sprite GetPredatorBody(Data.PredatorKind kind)
        {
            if (predatorBodies.TryGetValue(kind, out Sprite cached) && cached != null) return cached;
            PredatorSpec sp = Spec(kind);
            const int w = 96, h = 128;
            Vector2 c = new Vector2(48f, h * 0.46f);
            float noseY = c.y - sp.nose, tailY = c.y + sp.tailEnd;
            float norm = Mathf.Pow(sp.a / (sp.a + sp.b), sp.a) * Mathf.Pow(sp.b / (sp.a + sp.b), sp.b);

            float HalfWidth(float t)
            {
                t = Mathf.Clamp01(t);
                return sp.halfWidth * Mathf.Pow(t, sp.a) * Mathf.Pow(1f - t, sp.b) / norm;
            }
            float BodyY(float t) => Mathf.Lerp(noseY, tailY, t);
            float Body(Vector2 p)
            {
                float t = (p.y - noseY) / (tailY - noseY);
                if (t < 0f) return (p - new Vector2(c.x, noseY)).magnitude;
                if (t > 1f) return (p - new Vector2(c.x, tailY)).magnitude;
                return Mathf.Abs(p.x - c.x) - HalfWidth(t);
            }
            Vector2 finR = new Vector2(c.x + sp.finOut, BodyY(sp.finT) + sp.finLen * 0.45f);
            Vector2 finL = new Vector2(c.x - sp.finOut, finR.y);
            float Fins(Vector2 p)
            {
                float r = SdEllipse(Rotate(p, finR, -sp.finAngle), finR, new Vector2(sp.finLen, sp.finThick));
                float l = SdEllipse(Rotate(p, finL, sp.finAngle), finL, new Vector2(sp.finLen, sp.finThick));
                return Mathf.Min(r, l);
            }
            // Catfish whiskers: thin curved barbels sweeping out from the snout
            float Whiskers(Vector2 p)
            {
                if (!sp.whiskers) return 99f;
                float d = 99f;
                for (int k = -1; k <= 1; k += 2)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        Vector2 root = new Vector2(c.x + k * (4f + j * 5f), noseY + 3f + j * 4f);
                        Vector2 q = p - root;
                        float along = q.x * k; // outward distance
                        if (along < 0f || along > 30f - j * 8f) continue;
                        float curveY = -along * 0.45f + along * along * 0.012f; // droops forward, then curls
                        d = Mathf.Min(d, Mathf.Abs(q.y - curveY) - 1.4f + along * 0.02f);
                    }
                }
                return d;
            }

            Sdf sdf = p => Mathf.Min(SmoothMin(Body(p), Fins(p), 4f), Whiskers(p));
            Fill fill = (p, d) =>
            {
                if (Whiskers(p) < 0f && Body(p) > 0f) return sp.finDark;
                float t = Mathf.Clamp01((p.y - noseY) / (tailY - noseY));
                float hw = Mathf.Max(HalfWidth(t), 0.01f);
                float across = Mathf.Abs(p.x - c.x) / hw;

                Color col;
                if (Body(p) > 0f)
                {
                    col = Color.Lerp(sp.finDark, sp.finLit, Mathf.Clamp01((p.y - (finR.y - 6f)) / 12f));
                }
                else
                {
                    // Countershading: dark spine -> lighter flanks -> belly color at the rim
                    col = Color.Lerp(sp.spine, sp.back, Smooth(0f, 0.45f, across));
                    col = Color.Lerp(col, sp.side, Smooth(0.45f, 0.85f, across));
                    col = Color.Lerp(col, sp.belly, Smooth(0.84f, 1f, across) * 0.85f);
                    col = Color.Lerp(col, sp.side, Mathf.Clamp01((0.18f - t) / 0.18f) * 0.5f);

                    if (sp.spots)
                    {
                        // Pale bean-shaped spots in rows along the flanks
                        float gx = (p.x - c.x) / 4.2f, gy = (p.y - noseY) / 7f;
                        float cellX = Mathf.Floor(gx), cellY = Mathf.Floor(gy);
                        Vector2 inCell = new Vector2(Frac(gx) - 0.5f, Frac(gy) - 0.5f);
                        if (across > 0.3f && t > 0.2f && t < 0.95f && Hash(cellX, cellY) > 0.45f &&
                            (inCell.x * inCell.x * 4f + inCell.y * inCell.y * 9f) < 0.5f)
                            col = Color.Lerp(col, sp.belly, 0.7f);
                    }
                    if (sp.blotches)
                    {
                        float n = Mathf.PerlinNoise(p.x * 0.18f + 7f, p.y * 0.18f + 3f);
                        if (n > 0.58f && across < 0.85f) col = Color.Lerp(col, sp.spine, 0.55f);
                    }
                    if (sp.dorsal)
                    {
                        float dt = (t - 0.46f) / 0.26f;
                        if (dt > 0f && dt < 1f)
                        {
                            float half = 5f * (1f - dt);
                            float dx = p.x - c.x;
                            if (Mathf.Abs(dx) < half) col = dx < -half * 0.35f ? sp.finLit : sp.finDark;
                        }
                    }
                    if (sp.gills)
                    {
                        for (int g = 0; g < 3; g++)
                        {
                            float gt = (sp.dorsal ? 0.28f : 0.22f) + g * 0.045f;
                            float gy = BodyY(gt);
                            float gx = HalfWidth(gt) * 0.72f;
                            for (int k = -1; k <= 1; k += 2)
                            {
                                Vector2 q = p - new Vector2(c.x + gx * k, gy);
                                if (Mathf.Abs(q.y - q.x * q.x * 0.04f) < 0.7f && Mathf.Abs(q.x) < Mathf.Min(3.2f, gx * 0.5f)) col = sp.gill;
                            }
                        }
                    }
                    if (sp.teeth)
                    {
                        // White zigzag teeth along the snout
                        Vector2 m = p - new Vector2(c.x, noseY + 5f);
                        if (m.y < 2.5f && m.y > -1.5f && Mathf.Abs(m.x) < 9f &&
                            Mathf.Abs(Frac(m.x / 3f) - 0.5f) * 3f < (2.5f - m.y) * 0.45f)
                            col = Color.white;
                    }

                    // Eyes with a glint (and angry brows for piranhas)
                    for (int k = -1; k <= 1; k += 2)
                    {
                        Vector2 eye = new Vector2(c.x + HalfWidth(sp.eyeT) * 0.62f * k, BodyY(sp.eyeT));
                        float e = SdEllipse(p, eye, new Vector2(2.6f, 3.1f) * sp.eyeSize);
                        if (e < 0f) col = Hex("#0B1426");
                        if ((p - (eye + new Vector2(-0.8f * k, 0.9f) * sp.eyeSize)).magnitude < 0.75f * sp.eyeSize) col = Color.white;
                        if (sp.brows)
                        {
                            Vector2 b0 = eye + new Vector2(-4f * k, 5f), b1 = eye + new Vector2(3.5f * k, 3f);
                            Vector2 ab = b1 - b0;
                            float bt = Mathf.Clamp01(Vector2.Dot(p - b0, ab) / ab.sqrMagnitude);
                            if ((p - (b0 + ab * bt)).magnitude < 1.1f) col = sp.outline;
                        }
                        else if (e > 0f && e < 1.4f && p.y > eye.y) col = Color.Lerp(col, sp.spine, 0.5f);
                    }
                }
                return Shine(col, p, new Vector2(c.x - sp.halfWidth * 0.3f, c.y + 10f), sp.halfWidth * 0.6f, 0.25f);
            };
            Sprite s = ToSprite(Paint(w, h, sdf, fill, sp.outline, 2.1f), new Vector2(0.5f, 0.46f), 1.15f);
            predatorBodies[kind] = s;
            return s;
        }

        /// <summary>Predator tail pointing up, pivot at its base so it can beat on its own.</summary>
        public static Sprite GetPredatorTail(Data.PredatorKind kind)
        {
            if (predatorTails.TryGetValue(kind, out Sprite cached) && cached != null) return cached;
            PredatorSpec sp = Spec(kind);
            const int w = 52, h = 48;
            Vector2 b = new Vector2(26f, 2f);
            float k = sp.tailScale;
            Sdf sdf = p =>
            {
                Vector2 q = b + (p - b) / k; // scale the shape around its base
                float stalk = SdEllipse(q, b + new Vector2(0f, 12f), new Vector2(5.5f, 13f));
                if (sp.roundTail) return SmoothMin(stalk, SdEllipse(q, b + new Vector2(0f, 26f), new Vector2(13f, 12f)), 6f) * k;
                Vector2 lc = b + new Vector2(-7f, 24f), rc = b + new Vector2(7f, 24f);
                float l = SdEllipse(Rotate(q, lc, 38f), lc, new Vector2(5.5f, 15f));
                float r = SdEllipse(Rotate(q, rc, -38f), rc, new Vector2(5.5f, 15f));
                return SmoothMin(stalk, SmoothMin(l, r, 6f), 9f) * k;
            };
            Fill fill = (p, d) => p.x < b.x ? sp.finLit : sp.finDark;
            Sprite s = ToSprite(Paint(w, h, sdf, fill, sp.outline, 2.1f), new Vector2(0.5f, b.y / h), 1.15f);
            predatorTails[kind] = s;
            return s;
        }

        /// <summary>Shiny hook with a wiggly pink worm (the bait that hides the danger).</summary>
        public static Sprite GetHook()
        {
            if (hook != null) return hook;
            const int w = 40, h = 48;
            Vector2 c = new Vector2(20f, 24f);
            Color metal = Hex("#DDE6F0"), metalDark = Hex("#8E9BB0"), worm = Hex("#FF7FAE"), wormSeg = Hex("#E0558C");
            Color outline = Hex("#2B2F45");

            float Shaft(Vector2 p) => SdBox(p, c + new Vector2(2f, 6f), new Vector2(2f, 14f), 2f);
            float Curve(Vector2 p)
            {
                Vector2 cc = c + new Vector2(-4f, -8f);
                float ring = Mathf.Abs((p - cc).magnitude - 6f) - 2f;
                return p.y > cc.y + 1f && p.x < cc.x + 5f ? Mathf.Max(ring, p.y - (cc.y + 1f)) : ring;
            }
            float Eye(Vector2 p) => Mathf.Abs((p - (c + new Vector2(2f, 21f))).magnitude - 3f) - 1.3f;
            float Worm(Vector2 p)
            {
                Vector2 q = p - (c + new Vector2(2f, 2f));
                float wx = Mathf.Sin(q.y * 0.45f) * 4f;
                return SdBox(new Vector2(q.x - wx, q.y), Vector2.zero, new Vector2(3.6f, 9f), 3.6f);
            }

            Sdf sdf = p => Mathf.Min(Mathf.Min(Shaft(p), Curve(p)), Mathf.Min(Eye(p), Worm(p)));
            Fill fill = (p, d) =>
            {
                if (Worm(p) < 0f)
                {
                    Vector2 q = p - (c + new Vector2(2f, 2f));
                    Color col = Mathf.Abs(Frac(q.y / 3.2f) - 0.5f) < 0.12f ? wormSeg : worm;
                    return Shine(col, p, c + new Vector2(0f, 6f), 3f, 0.5f);
                }
                return p.x < c.x + 1f ? metal : metalDark;
            };
            hook = ToSprite(Paint(w, h, sdf, fill, outline, 1.4f), new Vector2(0.5f, 0.5f), 1.15f);
            return hook;
        }

        // ------------------------------------------------------------------ HUD / FX

        /// <summary>White heart with a dark outline and shine; tinted by the HUD (full / empty).</summary>
        public static Sprite GetHeart()
        {
            if (heart != null) return heart;
            const int s = 48;
            const float scale = 34f; // heart height ≈ 1.1 * scale
            Sdf sdf = p =>
            {
                Vector2 q = new Vector2(Mathf.Abs(p.x - s * 0.5f), p.y - 5f) / scale;
                float d;
                if (q.y + q.x > 1f) d = (q - new Vector2(0.25f, 0.75f)).magnitude - 0.35355f;
                else
                {
                    float m = Mathf.Max(q.x + q.y, 0f) * 0.5f;
                    float a = (q - new Vector2(0f, 1f)).sqrMagnitude;
                    float b = (q - new Vector2(m, m)).sqrMagnitude;
                    d = Mathf.Sqrt(Mathf.Min(a, b)) * Mathf.Sign(q.x - q.y);
                }
                return d * scale;
            };
            Fill fill = (p, d) => Color.white;
            Texture2D tex = Paint(s, s, sdf, fill, new Color(0.45f, 0.30f, 0.35f, 1f), 2.6f);
            // Highlight blob (stays white after tint, reads as gloss)
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    if (SdEllipse(Rotate(p, new Vector2(16f, 29f), 35f), new Vector2(16f, 29f), new Vector2(4.5f, 2.6f)) < 0f)
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 1f));
                }
            tex.Apply();
            heart = ToSprite(tex, new Vector2(0.5f, 0.5f), 1f);
            return heart;
        }

        /// <summary>White house icon (home / main menu) with a dark outline.</summary>
        public static Sprite GetHomeIcon()
        {
            if (homeIcon != null) return homeIcon;
            const int s = 48;
            Sdf sdf = p =>
            {
                float walls = SdBox(p, new Vector2(24f, 16f), new Vector2(12f, 10f), 2f);
                // Roof: triangle via two half-planes and a floor
                Vector2 q = new Vector2(Mathf.Abs(p.x - 24f), p.y - 24f);
                float roof = Mathf.Max((q.x * 0.8f + q.y * 0.6f) - 12f, -q.y);
                float door = SdBox(p, new Vector2(24f, 11f), new Vector2(3.5f, 5.5f), 1.5f);
                return Mathf.Max(Mathf.Min(walls, roof) - 0.5f, -door);
            };
            homeIcon = ToSprite(Paint(s, s, sdf, (p, d) => Color.white, Hex("#1B2A5C"), 2f), new Vector2(0.5f, 0.5f), 1f);
            return homeIcon;
        }

        /// <summary>White double-headed horizontal arrow ("drag left/right") with a dark outline.</summary>
        public static Sprite GetDragIcon()
        {
            if (dragIcon != null) return dragIcon;
            const int s = 48;
            Sdf sdf = p =>
            {
                Vector2 q = new Vector2(Mathf.Abs(p.x - 24f), p.y - 24f); // mirrored: one arrow head serves both sides
                float shaft = SdBox(q, new Vector2(0f, 0f), new Vector2(12f, 3.2f), 1.5f);
                // Arrow head: triangle pointing outward (+x), tip at x=21
                Vector2 h = q - new Vector2(21f, 0f);
                float head = Mathf.Max(Mathf.Max(h.x * 0.8f + Mathf.Abs(h.y) * 0.6f, -h.x - 9f), Mathf.Abs(h.y) - 9f) - 0.5f;
                return Mathf.Min(shaft, head);
            };
            dragIcon = ToSprite(Paint(s, s, sdf, (p, d) => Color.white, Hex("#1B2A5C"), 2f), new Vector2(0.5f, 0.5f), 1f);
            return dragIcon;
        }

        /// <summary>White "close" cross with rounded arms and a dark outline.</summary>
        public static Sprite GetCloseIcon()
        {
            if (closeIcon != null) return closeIcon;
            const int s = 40;
            Sdf sdf = p =>
            {
                Vector2 q = p - new Vector2(20f, 20f);
                float a = SdBox(Rotate(q, Vector2.zero, 45f), Vector2.zero, new Vector2(13f, 3.5f), 3.5f);
                float b = SdBox(Rotate(q, Vector2.zero, -45f), Vector2.zero, new Vector2(13f, 3.5f), 3.5f);
                return Mathf.Min(a, b);
            };
            closeIcon = ToSprite(Paint(s, s, sdf, (p, d) => Color.white, Hex("#1B2A5C"), 2f), new Vector2(0.5f, 0.5f), 1f);
            return closeIcon;
        }

        /// <summary>White "pause" icon: two rounded bars with a dark outline.</summary>
        public static Sprite GetPauseIcon()
        {
            if (pauseIcon != null) return pauseIcon;
            const int s = 40;
            Sdf sdf = p => Mathf.Min(SdBox(p, new Vector2(14f, 20f), new Vector2(4.5f, 12f), 2.5f),
                                     SdBox(p, new Vector2(26f, 20f), new Vector2(4.5f, 12f), 2.5f));
            pauseIcon = ToSprite(Paint(s, s, sdf, (p, d) => Color.white, Hex("#1B2A5C"), 2f), new Vector2(0.5f, 0.5f), 1f);
            return pauseIcon;
        }

        /// <summary>White rounded "play" triangle with a dark outline.</summary>
        public static Sprite GetPlayIcon()
        {
            if (playIcon != null) return playIcon;
            const int s = 40;
            Sdf sdf = p =>
            {
                Vector2 q = p - new Vector2(18f, 20f);
                // Triangle pointing right: left edge + two slanted edges, slightly rounded
                float left = -q.x - 10f;
                float top = (q.x * 0.5f + q.y * 0.866f) - 6.5f;
                float bottom = (q.x * 0.5f - q.y * 0.866f) - 6.5f;
                return Mathf.Max(left, Mathf.Max(top, bottom)) - 2f;
            };
            playIcon = ToSprite(Paint(s, s, sdf, (p, d) => Color.white, Hex("#1B2A5C"), 2f), new Vector2(0.5f, 0.5f), 1f);
            return playIcon;
        }

        /// <summary>Glossy capsule for progress bars (Filled image: the left end stays round). White, tint via Image.</summary>
        public static Sprite GetBarFill()
        {
            if (barFill != null) return barFill;
            const int w = 160, h = 12;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = SdBox(p, new Vector2(w * 0.5f, h * 0.5f), new Vector2(w * 0.5f, h * 0.5f), h * 0.5f);
                    float gloss = y > h * 0.55f ? 1f : 0.82f; // lighter upper half
                    tex.SetPixel(x, y, new Color(gloss, gloss, gloss, Mathf.Clamp01(0.5f - d)));
                }
            tex.Apply();
            barFill = ToSprite(tex, new Vector2(0.5f, 0.5f), 1f);
            return barFill;
        }

        /// <summary>Soap-bubble ring with a highlight.</summary>
        public static Sprite GetBubble()
        {
            if (bubble != null) return bubble;
            const int s = 24;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            Vector2 c = new Vector2(12f, 12f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = (p - c).magnitude;
                    float rim = Mathf.Clamp01(1.4f - Mathf.Abs(d - 9.5f)) * 0.9f;
                    float inner = d < 9.5f ? 0.12f : 0f;
                    float hi = (p - (c + new Vector2(-3.5f, 3.5f))).magnitude < 2.2f ? 0.95f : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Max(rim, Mathf.Max(inner, hi))));
                }
            tex.Apply();
            bubble = ToSprite(tex, new Vector2(0.5f, 0.5f), 1f);
            return bubble;
        }

        /// <summary>Soft rounded streak of light on the water surface.</summary>
        public static Sprite GetGlint()
        {
            if (glint != null) return glint;
            const int w = 8, h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = SdEllipse(new Vector2(x + 0.5f, y + 0.5f), new Vector2(4f, 16f), new Vector2(3.2f, 15f));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(-d * 0.6f)));
                }
            tex.Apply();
            glint = ToSprite(tex, new Vector2(0.5f, 0.5f), 1f);
            return glint;
        }

        /// <summary>4-point twinkle used by pickup bursts.</summary>
        public static Sprite GetSparkle()
        {
            if (sparkle != null) return sparkle;
            const int s = 32;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            Vector2 c = new Vector2(16f, 16f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    Vector2 d = new Vector2(x + 0.5f, y + 0.5f) - c;
                    float ax = Mathf.Abs(d.x), ay = Mathf.Abs(d.y);
                    float star4 = Mathf.Clamp01(1.5f - (Mathf.Sqrt(ax) + Mathf.Sqrt(ay) - 3.6f) * 1.4f);
                    float core = Mathf.Clamp01(1f - d.magnitude / 6f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Max(star4, core))));
                }
            tex.Apply();
            sparkle = ToSprite(tex, new Vector2(0.5f, 0.5f), 1f);
            return sparkle;
        }

        // ------------------------------------------------------------------ river & banks (tiling)

        /// <summary>Sunlight network on the riverbed (white, tint/alpha via the renderer). Seamless 128x128.</summary>
        public static Sprite GetCaustics()
        {
            if (caustics != null) return caustics;
            const int s = 128;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
            const float tau = Mathf.PI * 2f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float u = x / (float)s, v = y / (float)s;
                    // Integer frequencies keep it periodic (seamless when tiled).
                    float a = Mathf.Sin(tau * (2f * u + 0.35f * Mathf.Sin(tau * 3f * v)));
                    float b = Mathf.Sin(tau * (2f * v + 0.35f * Mathf.Sin(tau * 2f * u)));
                    float cc = Mathf.Sin(tau * (u + v + 0.25f * Mathf.Sin(tau * 2f * (u - v))) * 3f);
                    float line = Mathf.Min(Mathf.Abs(a * b), Mathf.Abs(cc) * 0.8f);
                    float alpha = Mathf.Clamp01(1f - line * 5.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            tex.Apply();
            caustics = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
            return caustics;
        }

        /// <summary>Horizontal light band for the river's middle (alpha 1 in the center, 0 at the edges).</summary>
        public static Sprite GetWaterGlow()
        {
            if (waterGlow != null) return waterGlow;
            const int w = 64, h = 4;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float t = Mathf.Abs((x + 0.5f) / w - 0.5f) * 2f; // 0 center .. 1 edge
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, 1f - Smooth(0.15f, 1f, t)));
                }
            tex.Apply();
            waterGlow = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
            return waterGlow;
        }

        /// <summary>Lush grass with lighter/darker tufts and tiny flowers. Seamless; 128x128 world units at 2x resolution.</summary>
        public static Sprite GetGrassTile()
        {
            if (grassTile != null) return grassTile;
            const int R = 2;            // texels per world unit
            const int s = 128 * R;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
            const float tau = Mathf.PI * 2f;
            Color baseC = GrassColor, darkC = Hex("#4FAE3A"), lightC = Hex("#8BE05E");
            Color[] petals = { Hex("#FFE66D"), Hex("#FF8FB8"), Color.white, Hex("#FFB347") };
            const float cell = 16f;
            var px = new Color[s * s];

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float wx = (x + 0.5f) / R, wy = (y + 0.5f) / R; // world units inside the tile
                    float u = wx / 128f, v = wy / 128f;
                    float n = Mathf.Sin(tau * (u * 2f + Mathf.Sin(tau * v * 3f) * 0.2f)) * Mathf.Sin(tau * (v * 3f + Mathf.Sin(tau * u * 2f) * 0.2f));
                    // Soft patches of lighter and darker grass
                    Color col = Color.Lerp(baseC, lightC, Smooth(0.3f, 0.6f, n));
                    col = Color.Lerp(col, darkC, Smooth(-0.4f, -0.7f, n));

                    // Grass tufts: a little three-blade "v", one per cell at a hashed spot
                    int cx = (int)(wx / cell), cy = (int)(wy / cell);
                    Vector2 tuft = new Vector2(cx * cell + 3f + Hash(cx, cy) * 10f, cy * cell + 3f + Hash(cy, cx) * 10f);
                    Vector2 d = new Vector2(wx, wy) - tuft;
                    if (d.y > 0f && d.y < 4.5f)
                    {
                        float blade = Mathf.Min(Mathf.Abs(d.x - d.y * 0.6f), Mathf.Min(Mathf.Abs(d.x + d.y * 0.6f), Mathf.Abs(d.x)));
                        col = Color.Lerp(col, darkC, Mathf.Clamp01(1.2f - blade * 1.6f));
                    }

                    // Tiny flowers in some cells
                    float fh = Hash(cx + 17f, cy + 5f);
                    if (fh > 0.72f)
                    {
                        Vector2 fc = new Vector2(cx * cell + 8f, cy * cell + 8f) + new Vector2(Hash(cx, cy + 9f) - 0.5f, Hash(cx + 9f, cy) - 0.5f) * 6f;
                        Vector2 fd = new Vector2(wx, wy) - fc;
                        float r = fd.magnitude;
                        float petalEdge = 2.4f + Mathf.Cos(Mathf.Atan2(fd.y, fd.x) * 5f) * 0.9f;
                        col = Color.Lerp(col, petals[(int)(fh * 37f) % petals.Length], Mathf.Clamp01((petalEdge - r) * 2f));
                        col = Color.Lerp(col, Hex("#FFD23F"), Mathf.Clamp01((1.3f - r) * 2f));
                    }
                    px[y * s + x] = col;
                }
            tex.SetPixels(px);
            tex.Apply();
            grassTile = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), R, 0, SpriteMeshType.FullRect);
            return grassTile;
        }

        /// <summary>
        /// Shore strip for the LEFT bank (flip X for the right): grass edge, golden sand with pebbles, wet sand,
        /// and a wavy white foam line where it meets the water. Seamless vertically. 40 x 128 world units, 2x resolution.
        /// </summary>
        public static Sprite GetShoreTile()
        {
            if (shoreTile != null) return shoreTile;
            const int R = 2;
            const int w = 40 * R, h = 128 * R;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
            const float tau = Mathf.PI * 2f;
            Color sand = Hex("#FFD98A"), sandShade = Hex("#F7C873"), wet = Hex("#E0AE5C"), pebble = Hex("#FFF1D6"), grassEdge = Hex("#4FAE3A");
            var px = new Color[w * h];

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float wx = (x + 0.5f) / R, wy = (y + 0.5f) / R;
                    float v = wy / 128f;
                    // Edges wobble with whole-number frequencies so the tile repeats seamlessly.
                    float grassLine = 9f + Mathf.Sin(tau * v * 3f) * 2.5f + Mathf.Sin(tau * v * 7f) * 1f;
                    float foamLine = 33f + Mathf.Sin(tau * v * 4f + 1f) * 2f + Mathf.Sin(tau * v * 9f) * 0.8f;

                    // Sand with soft darker ripples (periodic in y)
                    float ripple = Mathf.Sin(tau * (v * 10f) + wx * 0.35f) * 0.5f + 0.5f;
                    Color col = Color.Lerp(sand, sandShade, ripple * 0.35f);

                    // Pebbles: one per 8-unit cell where the hash says so
                    float pcx = Mathf.Floor(wx / 8f), pcy = Mathf.Floor(wy / 8f);
                    if (Hash(pcx + 3f, pcy) > 0.8f)
                    {
                        Vector2 pc = new Vector2(pcx * 8f + 4f, pcy * 8f + 4f);
                        float pd = (new Vector2(wx, wy) - pc).magnitude;
                        col = Color.Lerp(col, pebble, Mathf.Clamp01((1.6f - pd) * 2f));
                    }

                    // Wet sand near the water, then foam, then foam fading into the water
                    col = Color.Lerp(col, wet, Smooth(foamLine - 6f, foamLine - 1f, wx));
                    float foam = Mathf.Clamp01((wx - foamLine) * 2f) * Mathf.Clamp01((foamLine + 2.8f - wx) * 2f);
                    col = Color.Lerp(col, Color.white, foam);
                    if (wx > foamLine + 2.8f)
                    {
                        float a = Mathf.Clamp01(1f - (wx - foamLine - 2.8f) / 4.5f) * 0.55f;
                        col = new Color(1f, 1f, 1f, a);
                    }

                    // Grass rim on the land side, transparent beyond it (the grass tile shows through)
                    float rim = Mathf.Clamp01((grassLine - wx) * 2f);
                    col = Color.Lerp(col, grassEdge, rim);
                    col.a *= Mathf.Clamp01((wx - (grassLine - 2f)) * 2f);

                    px[y * w + x] = col;
                }
            tex.SetPixels(px);
            tex.Apply();
            shoreTile = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), R, 0, SpriteMeshType.FullRect);
            return shoreTile;
        }

        // ------------------------------------------------------------------ bank decorations

        /// <summary>Round tree canopy seen from above: layered greens, highlight, a few red apples.</summary>
        public static Sprite GetTree()
        {
            if (tree != null) return tree;
            const int s = 72;
            Vector2 c = new Vector2(36f, 36f);
            Color dark = Hex("#2F8F3A"), mid = Hex("#45B04A"), lit = Hex("#7CDB5E"), outline = Hex("#1D5A24");
            Vector2[] lobes = { new Vector2(0f, 0f), new Vector2(-14f, 6f), new Vector2(13f, 8f), new Vector2(-9f, -13f), new Vector2(11f, -12f), new Vector2(1f, 16f) };
            float[] lr = { 19f, 13f, 13f, 12f, 12f, 12f };
            Sdf sdf = p =>
            {
                float d = 99f;
                for (int i = 0; i < lobes.Length; i++) d = SmoothMin(d, SdCircle(p, c + lobes[i], lr[i]), 3f);
                return d;
            };
            Fill fill = (p, d) =>
            {
                float l = Light(p, c, 30f);
                Color col = l > 0.62f ? lit : (l > 0.38f ? mid : dark);
                // inner leaf clumps
                for (int i = 1; i < lobes.Length; i++)
                    if (Mathf.Abs(SdCircle(p, c + lobes[i] * 0.55f, lr[i] * 0.55f)) < 0.8f && l < 0.7f) col = dark;
                // apples
                Vector2[] apples = { new Vector2(-8f, 4f), new Vector2(9f, -4f), new Vector2(3f, 10f) };
                foreach (var a in apples)
                {
                    float ad = SdCircle(p, c + a, 3f);
                    if (ad < 0f) col = ad > -0.9f ? Hex("#8E1B1B") : Hex("#FF4D4D");
                    if ((p - (c + a + new Vector2(-1f, 1f))).magnitude < 0.8f) col = Color.white;
                }
                return col;
            };
            Texture2D tex = Paint(s, s, sdf, fill, outline, 2.2f);
            tree = ToSprite(tex, new Vector2(0.5f, 0.5f), 1f);
            return tree;
        }

        /// <summary>Little bush covered in flowers (colors vary by tint-free palette in the texture).</summary>
        public static Sprite GetFlowerBush()
        {
            if (flowerBush != null) return flowerBush;
            const int s = 44;
            Vector2 c = new Vector2(22f, 22f);
            Color leaf = Hex("#3FA34D"), leafLit = Hex("#6CCF5A"), outline = Hex("#1D5A24");
            Color[] petals = { Hex("#FF6FA8"), Hex("#FFD23F"), Hex("#B57CFF"), Hex("#FF8C42") };
            Vector2[] lobes = { new Vector2(-7f, 0f), new Vector2(7f, 2f), new Vector2(0f, -7f), new Vector2(0f, 8f) };
            Sdf sdf = p =>
            {
                float d = 99f;
                foreach (var l in lobes) d = SmoothMin(d, SdCircle(p, c + l, 10f), 3f);
                return d;
            };
            Vector2[] flowers = { new Vector2(-8f, 3f), new Vector2(7f, 6f), new Vector2(1f, -8f), new Vector2(9f, -5f), new Vector2(-4f, 11f) };
            Fill fill = (p, d) =>
            {
                Color col = Light(p, c, 18f) > 0.55f ? leafLit : leaf;
                for (int i = 0; i < flowers.Length; i++)
                {
                    Vector2 fd = p - (c + flowers[i]);
                    float ang = Mathf.Atan2(fd.y, fd.x);
                    float r = fd.magnitude;
                    if (r < 1.4f) col = Hex("#FFF4B0");
                    else if (r < 3.6f + Mathf.Cos(ang * 5f) * 1.2f) col = petals[i % petals.Length];
                }
                return col;
            };
            flowerBush = ToSprite(Paint(s, s, sdf, fill, outline, 1.8f), new Vector2(0.5f, 0.5f), 1f);
            return flowerBush;
        }

        /// <summary>Red toadstool with white dots, seen from above.</summary>
        public static Sprite GetMushroom()
        {
            if (mushroom != null) return mushroom;
            const int s = 32;
            Vector2 c = new Vector2(16f, 16f);
            Color cap = Hex("#FF4D4D"), capLit = Hex("#FF8080"), outline = Hex("#7A1414");
            Sdf sdf = p => SdCircle(p, c, 12f);
            Fill fill = (p, d) =>
            {
                Color col = Light(p, c, 12f) > 0.6f ? capLit : cap;
                Vector2[] dots = { new Vector2(-5f, 4f), new Vector2(4f, 6f), new Vector2(5f, -4f), new Vector2(-3f, -6f), new Vector2(0f, 0f) };
                foreach (var dd in dots) if (SdCircle(p, c + dd, 2.2f) < 0f) col = Color.white;
                return col;
            };
            mushroom = ToSprite(Paint(s, s, sdf, fill, outline, 1.6f), new Vector2(0.5f, 0.5f), 1f);
            return mushroom;
        }

        /// <summary>Smooth river stone (warm or cool tint chosen by the renderer).</summary>
        public static Sprite GetStone()
        {
            if (stone != null) return stone;
            const int s = 28;
            Vector2 c = new Vector2(14f, 14f);
            Sdf sdf = p => SdEllipse(p, c, new Vector2(11f, 8.5f));
            Fill fill = (p, d) => Shine(Light(p, c, 11f) > 0.55f ? new Color(0.95f, 0.95f, 0.98f) : new Color(0.75f, 0.76f, 0.84f), p, c + new Vector2(-4f, 3f), 4f, 0.7f);
            stone = ToSprite(Paint(s, s, sdf, fill, new Color(0.25f, 0.24f, 0.36f), 1.5f), new Vector2(0.5f, 0.5f), 1f);
            return stone;
        }

        /// <summary>Lily pad with a notch and a pink water-lily flower.</summary>
        public static Sprite GetLilyPad()
        {
            if (lilyPad != null) return lilyPad;
            const int s = 44;
            Vector2 c = new Vector2(22f, 22f);
            Color pad = Hex("#5CC95C"), padLit = Hex("#8BE07A"), vein = Hex("#3E9E45"), outline = Hex("#256B2C");
            Sdf sdf = p =>
            {
                float d = SdCircle(p, c, 17f);
                Vector2 q = p - c;
                float ang = Mathf.Atan2(q.y, q.x);
                // notch wedge toward the lower right
                if (Mathf.Abs(Mathf.DeltaAngle(ang * Mathf.Rad2Deg, -40f)) < 16f && q.magnitude > 2f) d = Mathf.Max(d, 2f);
                return d;
            };
            Fill fill = (p, d) =>
            {
                Vector2 q = p - c;
                Color col = Light(p, c, 17f) > 0.55f ? padLit : pad;
                float ang = Mathf.Atan2(q.y, q.x) * Mathf.Rad2Deg;
                if (Mathf.Abs(Frac(ang / 45f) - 0.5f) > 0.46f && q.magnitude > 3f) col = vein;
                // Flower
                Vector2 fc = c + new Vector2(-4f, 4f);
                Vector2 fd = p - fc;
                float r = fd.magnitude, fa = Mathf.Atan2(fd.y, fd.x);
                if (r < 2f) col = Hex("#FFE066");
                else if (r < 5.5f + Mathf.Cos(fa * 6f) * 1.6f) col = Mathf.Cos(fa * 6f) > 0.3f ? Hex("#FFC2DA") : Hex("#FF7FB0");
                return col;
            };
            lilyPad = ToSprite(Paint(s, s, sdf, fill, outline, 1.6f), new Vector2(0.5f, 0.5f), 1f);
            return lilyPad;
        }
    }
}
