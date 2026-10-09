using UnityEngine;

namespace Upstream.River
{
    /// <summary>
    /// Small procedural UI sprites (white quad, rounded rect, circle, glow, coin, gear) generated at runtime.
    /// Gameplay art lives in <see cref="CartoonSpriteFactory"/> and <see cref="FishSkinFactory"/>.
    /// </summary>
    public static class WatercolorSpriteFactory
    {
        private static Sprite whiteSprite;
        private static Sprite roundedRectSprite;
        private static Sprite circleSprite;
        private static Sprite gearSprite;
        private static Sprite coinSprite;
        private static Sprite circleGlowSprite;

        public static Sprite GetWhiteSprite()
        {
            if (whiteSprite != null) return whiteSprite;
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] cols = new Color[16];
            for (int i = 0; i < 16; i++) cols[i] = Color.white;
            tex.SetPixels(cols);
            tex.Apply();
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return whiteSprite;
        }

        /// <summary>
        /// 9-sliced rounded rectangle for UI (use Image.Type.Sliced). Corner radius is 24 canvas units at
        /// pixelsPerUnitMultiplier 1; raise the multiplier for smaller corners. Too-small rects become pills.
        /// </summary>
        public static Sprite GetRoundedRectSprite()
        {
            if (roundedRectSprite != null) return roundedRectSprite;
            const int sz = 64;
            const float radius = 24f;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    // Distance outside the inner rect (rounded corners), antialiased over one pixel.
                    float px = x + 0.5f, py = y + 0.5f;
                    float qx = Mathf.Max(radius - px, px - (sz - radius), 0f);
                    float qy = Mathf.Max(radius - py, py - (sz - radius), 0f);
                    float a = Mathf.Clamp01(radius - Mathf.Sqrt(qx * qx + qy * qy) + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            roundedRectSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100f, 0,
                                              SpriteMeshType.FullRect, new Vector4(28f, 28f, 28f, 28f));
            return roundedRectSprite;
        }

        /// <summary>Solid antialiased circle (toggle knobs, round buttons).</summary>
        public static Sprite GetCircleSprite()
        {
            if (circleSprite != null) return circleSprite;
            const int sz = 64;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            float r = sz * 0.5f;
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d)));
                }
            }
            tex.Apply();
            circleSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100f);
            return circleSprite;
        }

        /// <summary>Gold coin: rim, face with a fish-scale emboss and a shine; used in the river and as the UI coin icon.</summary>
        public static Sprite GetCoinSprite()
        {
            if (coinSprite != null) return coinSprite;
            const int sz = 48;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color rim = new Color(0.86f, 0.55f, 0.08f, 1f);
            Color face = new Color(1f, 0.80f, 0.22f, 1f);
            Color emboss = new Color(0.93f, 0.66f, 0.12f, 1f);
            float r = sz * 0.5f;
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float dx = x + 0.5f - r, dy = y + 0.5f - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(r - d);
                    if (a <= 0f) { tex.SetPixel(x, y, Color.clear); continue; }

                    Color c = d > r - 1.8f ? new Color(0.58f, 0.30f, 0.02f, 1f) : (d > r - 5f ? rim : face); // dark cartoon outline
                    // Embossed inner ring
                    if (Mathf.Abs(d - (r - 10f)) < 1.2f) c = emboss;
                    // Shine: light upper-left
                    float shine = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(dx, dy), new Vector2(-6f, 7f)) / 14f);
                    c = Color.Lerp(c, Color.white, shine * 0.55f);
                    c.a = a;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            // 48 px over 1.6 ppu = 30 world units: about a shrimp's size.
            coinSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 1.6f);
            return coinSprite;
        }

        /// <summary>Gear (cog) icon for the settings button.</summary>
        public static Sprite GetGearSprite()
        {
            if (gearSprite != null) return gearSprite;
            const int sz = 64;
            const int teeth = 8;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            float c = sz * 0.5f;
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float dx = x + 0.5f - c, dy = y + 0.5f - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    // Square-ish teeth: outer radius jumps between body (22) and tooth tip (30).
                    float wave = Mathf.Cos(angle * teeth);
                    float outer = Mathf.Lerp(22f, 30f, Mathf.Clamp01((wave + 0.15f) * 3f));
                    float a = Mathf.Clamp01(outer - d) * Mathf.Clamp01(d - 9f); // hole in the middle
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            gearSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100f);
            return gearSprite;
        }

        public static Sprite GetCircleGlowSprite()
        {
            if (circleGlowSprite != null) return circleGlowSprite;
            int sz = 64;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float dx = (x - sz * 0.5f) / (sz * 0.5f);
                    float dy = (y - sz * 0.5f) / (sz * 0.5f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            circleGlowSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 1f);
            return circleGlowSprite;
        }

    }
}
