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

        private static Sprite shrimp, star, rock, log, shark, hook, heart, bubble, glint, sparkle;
        private static Sprite caustics, grassTile, shoreTile, waterGlow;
        private static Sprite tree, flowerBush, mushroom, stone, lilyPad;
        private static Sprite barFill, homeIcon, playIcon, pauseIcon, sharkTail, wave, closeIcon;

        public static readonly Color GrassColor = Hex("#6CCB4A");

        // ------------------------------------------------------------------ helpers

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }

        private static float Frac(float v) => v - Mathf.Floor(v);
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
        /// Cartoon shark seen from above, nose pointing down (it swims toward the player): blue back,
        /// big angry-but-cute eyes, little teeth.
        /// </summary>
        public static Sprite GetShark()
        {
            if (shark != null) return shark;
            const int w = 96, h = 128;
            Vector2 c = new Vector2(48f, h * 0.46f);
            Color back = Hex("#3F72C9"), side = Hex("#78ACF2"), fin = Hex("#3563B5"), outline = Hex("#16305E");

            float Body(Vector2 p) => SdEllipse(p, c, new Vector2(19f, 44f));
            float Fins(Vector2 p)
            {
                float l = SdEllipse(Rotate(p, c + new Vector2(-19f, -4f), -35f), c + new Vector2(-19f, -4f), new Vector2(15f, 5.5f));
                float r = SdEllipse(Rotate(p, c + new Vector2(19f, -4f), 35f), c + new Vector2(19f, -4f), new Vector2(15f, 5.5f));
                return Mathf.Min(l, r);
            }
            // The tail is a separate sprite (GetSharkTail) so it can beat on its own.
            Sdf sdf = p => SmoothMin(Body(p), Fins(p), 3f);
            Fill fill = (p, d) =>
            {
                Color col;
                if (Body(p) > 0f) col = fin;
                else
                {
                    float sideAmt = Mathf.Clamp01(Mathf.Abs(p.x - c.x) / 16f);
                    col = Color.Lerp(back, side, sideAmt * sideAmt);
                    // Dorsal fin ridge
                    if (Mathf.Abs(p.x - c.x) < Mathf.Lerp(4f, 0.8f, Mathf.Clamp01((p.y - c.y + 6f) / 22f)) && p.y > c.y - 6f && p.y < c.y + 16f)
                        col = Hex("#244F99");
                }
                // Eyes near the nose (bottom)
                for (int k = -1; k <= 1; k += 2)
                {
                    Vector2 eye = c + new Vector2(9.5f * k, -26f);
                    float e = SdEllipse(p, eye, new Vector2(5f, 5.5f));
                    if (e < 0f)
                    {
                        col = e > -1.1f ? outline : Color.white;
                        Vector2 pupil = eye + new Vector2(-1.5f * k, -1.6f);
                        if ((p - pupil).magnitude < 2.6f) col = Hex("#10142A");
                        if ((p - (pupil + new Vector2(-0.8f, 0.9f))).magnitude < 0.8f) col = Color.white;
                    }
                    // Angry brow slanting toward the middle
                    Vector2 b0 = eye + new Vector2(-5.5f * k, 6.5f), b1 = eye + new Vector2(4.5f * k, 3.5f);
                    Vector2 ab = b1 - b0;
                    float t = Mathf.Clamp01(Vector2.Dot(p - b0, ab) / ab.sqrMagnitude);
                    if ((p - (b0 + ab * t)).magnitude < 1.3f) col = outline;
                }
                // Mouth with tiny teeth at the nose
                Vector2 m = p - (c + new Vector2(0f, -37f));
                if (Mathf.Abs(m.y + m.x * m.x * 0.02f) < 1.1f && Mathf.Abs(m.x) < 8f) col = outline;
                if (m.y < -0.5f && m.y > -3.5f && Mathf.Abs(m.x) < 7f && Mathf.Abs(Frac(m.x / 3.5f) - 0.5f) * 3.5f < (m.y + 3.5f) * 0.6f)
                    col = Color.white;
                return Shine(col, p, c + new Vector2(-7f, 18f), 9f, 0.35f);
            };
            shark = ToSprite(Paint(w, h, sdf, fill, outline, 2.3f), new Vector2(0.5f, 0.46f), 1.15f);
            return shark;
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

        /// <summary>Distance from the shark sprite's pivot to where the tail attaches (world units).</summary>
        public const float SharkTailAttachY = 30f / 1.15f;

        /// <summary>Shark tail (stalk + forked fin) pointing up, pivot at its base so it can wag.</summary>
        public static Sprite GetSharkTail()
        {
            if (sharkTail != null) return sharkTail;
            const int w = 52, h = 48;
            Vector2 b = new Vector2(26f, 2f); // attachment point
            Color fin = Hex("#3563B5"), finLit = Hex("#4A7CC9"), outline = Hex("#16305E");
            Sdf sdf = p =>
            {
                float stalk = SdEllipse(p, b + new Vector2(0f, 12f), new Vector2(6f, 13f));
                float l = SdEllipse(Rotate(p, b + new Vector2(-8f, 25f), 35f), b + new Vector2(-8f, 25f), new Vector2(5f, 14f));
                float r = SdEllipse(Rotate(p, b + new Vector2(8f, 25f), -35f), b + new Vector2(8f, 25f), new Vector2(5f, 14f));
                return SmoothMin(stalk, Mathf.Min(l, r), 2f);
            };
            Fill fill = (p, d) => p.x < b.x ? finLit : fin;
            sharkTail = ToSprite(Paint(w, h, sdf, fill, outline, 2.3f), new Vector2(0.5f, b.y / h), 1.15f);
            return sharkTail;
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
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, 1f - Mathf.SmoothStep(0.15f, 1f, t)));
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
                    Color col = Color.Lerp(baseC, lightC, Mathf.SmoothStep(0.3f, 0.6f, n));
                    col = Color.Lerp(col, darkC, Mathf.SmoothStep(-0.4f, -0.7f, n));

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
                    col = Color.Lerp(col, wet, Mathf.SmoothStep(foamLine - 6f, foamLine - 1f, wx));
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
