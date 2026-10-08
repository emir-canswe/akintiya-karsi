using UnityEngine;

namespace Upstream.River
{
    /// <summary>
    /// Generates stylized watercolor-inspired 2D Sprites in memory at runtime.
    /// Zero external PNG dependencies required.
    /// </summary>
    public static class WatercolorSpriteFactory
    {
        private static Sprite whiteSprite;
        private static Sprite circleGlowSprite;
        private static Sprite fishBodySprite;
        private static Sprite fishTailSprite;
        private static Sprite fishFinSprite;
        private static Sprite sharkSprite;
        private static Sprite rockSprite;
        private static Sprite logSprite;
        private static Sprite hookSprite;
        private static Sprite shrimpSprite;
        private static Sprite goldFoodSprite;
        private static Sprite heartSprite;
        private static Sprite reedSprite;
        private static Sprite bushSprite;

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

        public static Sprite GetFishBodySprite()
        {
            if (fishBodySprite != null) return fishBodySprite;
            int w = 64, h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            Color bodyCol = new Color(1f, 0.54f, 0.36f, 1f);     // #ff8a5c
            Color bellyCol = new Color(1f, 0.82f, 0.72f, 1f);    // #ffd2b8
            Color spotCol = new Color(0.31f, 0.12f, 0.08f, 0.42f);
            Color eyeBlack = new Color(0.06f, 0.13f, 0.17f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - w * 0.5f) / (w * 0.34f);
                    float ny = (y - h * 0.5f) / (h * 0.44f);
                    float d = nx * nx + ny * ny;

                    if (d > 1f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float edgeAA = Mathf.Clamp01((1f - d) * 8f);
                    float paperGrain = 0.95f + 0.05f * Mathf.PerlinNoise(x * 0.2f, y * 0.2f);
                    Color c = bodyCol * paperGrain;

                    // Belly ellipse
                    float bx = nx / 0.52f;
                    float by = (ny + 0.08f) / 0.65f;
                    if (bx * bx + by * by < 1f)
                    {
                        float bt = Mathf.Clamp01(1f - (bx * bx + by * by));
                        c = Color.Lerp(c, bellyCol, bt * 0.85f);
                    }

                    // Trout spots
                    Vector2[] spots =
                    {
                        new Vector2(-0.38f, -0.05f),
                        new Vector2(0.35f, -0.18f),
                        new Vector2(-0.22f, -0.32f),
                        new Vector2(0.25f, 0.12f)
                    };
                    for (int s = 0; s < spots.Length; s++)
                    {
                        if (Vector2.Distance(new Vector2(nx, ny), spots[s]) < 0.11f)
                        {
                            c = Color.Lerp(c, spotCol, 0.45f);
                        }
                    }

                    // Eyes near top (+ny)
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 eyeWhiteCenter = new Vector2(side * 0.54f, 0.56f);
                        Vector2 pupilCenter = new Vector2(side * 0.58f, 0.60f);
                        if (Vector2.Distance(new Vector2(nx, ny), pupilCenter) < 0.11f)
                        {
                            c = eyeBlack;
                        }
                        else if (Vector2.Distance(new Vector2(nx, ny), eyeWhiteCenter) < 0.19f)
                        {
                            c = Color.white;
                        }
                    }

                    c.a = edgeAA;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            fishBodySprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1.8f);
            return fishBodySprite;
        }

        public static Sprite GetFishTailSprite()
        {
            if (fishTailSprite != null) return fishTailSprite;
            int w = 48, h = 40;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color tailCol = new Color(1f, 0.50f, 0.32f, 1f);

            for (int y = 0; y < h; y++)
            {
                float t = 1f - (float)y / h; // 0 at top pivot, 1 at bottom tip
                float halfSpan = Mathf.Lerp(2f, w * 0.44f, t);
                float notch = t > 0.72f ? (t - 0.72f) * 28f : 0f;

                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - w * 0.5f);
                    if (dx <= halfSpan && dx >= notch * 0.5f)
                    {
                        float aa = Mathf.Clamp01((halfSpan - dx) * 1.5f);
                        tex.SetPixel(x, y, new Color(tailCol.r, tailCol.g, tailCol.b, aa));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            // Pivot at top center so it attaches to the rear of the fish
            fishTailSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.95f), 1.8f);
            return fishTailSprite;
        }

        public static Sprite GetFishFinSprite()
        {
            if (fishFinSprite != null) return fishFinSprite;
            int w = 32, h = 20;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color finCol = new Color(1f, 0.82f, 0.72f, 0.92f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - w * 0.5f) / (w * 0.45f);
                    float ny = (y - h * 0.5f) / (h * 0.42f);
                    float d = nx * nx + ny * ny;
                    if (d <= 1f)
                    {
                        tex.SetPixel(x, y, new Color(finCol.r, finCol.g, finCol.b, Mathf.Clamp01((1f - d) * 5f)));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            fishFinSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1.8f);
            return fishFinSprite;
        }

        public static Sprite GetSharkSprite()
        {
            if (sharkSprite != null) return sharkSprite;
            int w = 96, h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            Color bodyCol = new Color(0.50f, 0.60f, 0.67f, 1f);  // #7f9aab
            Color darkCol = new Color(0.36f, 0.46f, 0.53f, 1f);  // #5d7686
            Color snoutCol = new Color(0.79f, 0.84f, 0.87f, 1f); // #c9d6de

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float cx = (x - w * 0.5f);
                    float cy = (y - h * 0.46f); // Center of body

                    Color pixel = Color.clear;

                    // Pectoral fins
                    float finY = cy + 8f;
                    if (finY > -14f && finY < 16f)
                    {
                        float finSpan = Mathf.Lerp(40f, 14f, Mathf.Abs(finY) / 16f);
                        if (Mathf.Abs(cx) <= finSpan && Mathf.Abs(cx) > 10f)
                        {
                            pixel = new Color(0.44f, 0.54f, 0.61f, 1f);
                        }
                    }

                    // Top tail (at high y, since shark swims downward toward -Y)
                    if (y > h * 0.74f)
                    {
                        float tt = (y - h * 0.74f) / (h * 0.24f);
                        float span = Mathf.Lerp(4f, 20f, tt);
                        float notch = tt > 0.55f ? (tt - 0.55f) * 26f : 0f;
                        if (Mathf.Abs(cx) <= span && Mathf.Abs(cx) >= notch)
                        {
                            pixel = darkCol;
                        }
                    }

                    // Main body ellipse
                    float bx = cx / 19f;
                    float by = cy / 44f;
                    if (bx * bx + by * by <= 1f)
                    {
                        float grain = 0.94f + 0.06f * Mathf.PerlinNoise(x * 0.15f, y * 0.15f);
                        pixel = bodyCol * grain;
                        pixel.a = 1f;

                        // Snout near bottom (-cy)
                        float sx = cx / 10f;
                        float sy = (cy + 32f) / 11f;
                        if (sx * sx + sy * sy <= 1f && cy < -28f)
                        {
                            pixel = snoutCol;
                        }

                        // Dorsal fin in middle
                        if (cy > -8f && cy < 16f && Mathf.Abs(cx) < Mathf.Lerp(5f, 1f, (cy + 8f) / 24f))
                        {
                            pixel = darkCol;
                        }

                        // Eyes near bottom
                        for (int side = -1; side <= 1; side += 2)
                        {
                            if (Vector2.Distance(new Vector2(cx, cy), new Vector2(side * 10.5f, -26f)) < 3.2f)
                            {
                                pixel = new Color(0.08f, 0.08f, 0.08f, 1f);
                            }
                        }
                    }

                    tex.SetPixel(x, y, pixel);
                }
            }
            tex.Apply();
            sharkSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.46f), 1.15f);
            return sharkSprite;
        }

        public static Sprite GetRockSprite()
        {
            if (rockSprite != null) return rockSprite;
            int sz = 80;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            Color baseCol = new Color(0.42f, 0.44f, 0.47f, 1f);
            Color hiCol = new Color(0.55f, 0.57f, 0.61f, 1f);
            Color mossCol = new Color(0.27f, 0.55f, 0.27f, 0.75f);

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float dx = x - sz * 0.5f;
                    float dy = y - sz * 0.5f;
                    float angle = Mathf.Atan2(dy, dx);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float radius = 26f * (0.85f + 0.15f * Mathf.Sin(angle * 3f + 1.2f) * Mathf.Cos(angle * 2f));

                    // Outer foam ring
                    if (dist > radius + 3f && dist < radius + 7.5f)
                    {
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.45f));
                        continue;
                    }

                    if (dist > radius)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    Color c = baseCol;
                    // Top-left highlight
                    if (Vector2.Distance(new Vector2(dx, dy), new Vector2(-6f, 6f)) < 13f)
                    {
                        c = hiCol;
                    }
                    // Bottom-right moss
                    if (Vector2.Distance(new Vector2(dx, dy), new Vector2(8f, -8f)) < 10f)
                    {
                        c = Color.Lerp(c, mossCol, 0.65f);
                    }

                    float aa = Mathf.Clamp01((radius - dist) * 1.5f);
                    c.a = aa;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            rockSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 1.15f);
            return rockSprite;
        }

        public static Sprite GetLogSprite()
        {
            if (logSprite != null) return logSprite;
            int w = 128, h = 28;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            Color barkCol = new Color(0.48f, 0.31f, 0.17f, 1f);
            Color darkBark = new Color(0.36f, 0.23f, 0.12f, 1f);
            Color endRing = new Color(0.79f, 0.60f, 0.38f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dy = Mathf.Abs(y - h * 0.5f);
                    if (dy > 11f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    Color c = barkCol;
                    if (((x + y * 2) % 19) < 3 && y > 6 && y < h - 6)
                    {
                        c = darkBark;
                    }

                    // Wood cut ends
                    if (x < 9 || x > w - 10)
                    {
                        c = endRing;
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            logSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1.05f);
            return logSprite;
        }

        public static Sprite GetHookSprite()
        {
            if (hookSprite != null) return hookSprite;
            int sz = 40;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            Color metal = new Color(0.85f, 0.87f, 0.89f, 1f);
            Color bait = new Color(0.91f, 0.54f, 0.63f, 1f);

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float dx = x - sz * 0.5f;
                    float dy = y - sz * 0.5f;
                    Color c = Color.clear;

                    // Vertical shaft
                    if (Mathf.Abs(dx) < 2.2f && dy > -6f && dy < 14f)
                    {
                        c = metal;
                    }

                    // J-curve at bottom
                    float curveDist = Vector2.Distance(new Vector2(dx, dy), new Vector2(-5f, -6f));
                    if (dy <= -4f && curveDist > 4f && curveDist < 7.5f)
                    {
                        c = metal;
                    }

                    // Pink bait oval
                    float bx = (dx - 1f) / 5.5f;
                    float by = dy / 9.5f;
                    if (bx * bx + by * by <= 1f)
                    {
                        c = bait;
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            hookSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 1.15f);
            return hookSprite;
        }

        public static Sprite GetShrimpSprite()
        {
            if (shrimpSprite != null) return shrimpSprite;
            int sz = 40;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            Color glow = new Color(0.49f, 0.94f, 0.78f, 0.30f);
            Color shrimp = new Color(1f, 0.62f, 0.69f, 1f);

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float dx = x - sz * 0.5f;
                    float dy = y - sz * 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    Color c = Color.clear;
                    if (dist < 16f)
                    {
                        c = glow;
                        c.a = Mathf.Clamp01(1f - dist / 16f) * 0.35f;
                    }

                    // Curved shrimp body ring
                    float angle = Mathf.Atan2(dy, dx);
                    if (dist > 4.2f && dist < 9.5f && angle > -0.4f && angle < 3.6f)
                    {
                        c = shrimp;
                    }
                    // Head dot
                    if (Vector2.Distance(new Vector2(dx, dy), new Vector2(6f, -2.5f)) < 3.5f)
                    {
                        c = shrimp;
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            shrimpSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 1.15f);
            return shrimpSprite;
        }

        public static Sprite GetGoldFoodSprite()
        {
            if (goldFoodSprite != null) return goldFoodSprite;
            int sz = 44;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            Color halo = new Color(1f, 0.84f, 0.35f, 0.35f);
            Color gold = new Color(1f, 0.84f, 0.35f, 1f);
            Color cross = new Color(0.54f, 0.35f, 0f, 1f);

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float dx = x - sz * 0.5f;
                    float dy = y - sz * 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    Color c = Color.clear;
                    if (dist < 19f)
                    {
                        c = halo;
                        c.a = Mathf.Clamp01(1f - dist / 19f) * 0.42f;
                    }
                    if (dist < 11.5f)
                    {
                        c = gold;
                        if ((Mathf.Abs(dx) < 2f && Mathf.Abs(dy) < 6.5f) ||
                            (Mathf.Abs(dy) < 2f && Mathf.Abs(dx) < 6.5f))
                        {
                            c = cross;
                        }
                    }
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            goldFoodSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 1.15f);
            return goldFoodSprite;
        }

        public static Sprite GetHeartSprite()
        {
            if (heartSprite != null) return heartSprite;
            int sz = 48;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float nx = (x - sz * 0.5f) / (sz * 0.38f);
                    float ny = (y - sz * 0.42f) / (sz * 0.38f);
                    // Classic implicit heart equation: (x^2 + y^2 - 1)^3 - x^2 * y^3 <= 0
                    float a = nx * nx + ny * ny - 1f;
                    float eq = a * a * a - nx * nx * ny * ny * ny;
                    if (eq <= 0f)
                    {
                        float aa = Mathf.Clamp01(-eq * 6f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, aa));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            heartSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 1f);
            return heartSprite;
        }

        public static Sprite GetBushSprite()
        {
            if (bushSprite != null) return bushSprite;
            int sz = 40;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color c1 = new Color(0.25f, 0.54f, 0.27f, 1f);
            Color c2 = new Color(0.32f, 0.64f, 0.34f, 1f);

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    Vector2 p = new Vector2(x - sz * 0.5f, y - sz * 0.5f);
                    bool inside = Vector2.Distance(p, new Vector2(-5f, 0f)) < 9f ||
                                  Vector2.Distance(p, new Vector2(6f, 2f)) < 10f ||
                                  Vector2.Distance(p, new Vector2(0f, -5f)) < 8.5f;
                    if (inside)
                    {
                        tex.SetPixel(x, y, p.y > 1f ? c2 : c1);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            bushSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 1f);
            return bushSprite;
        }

        public static Sprite GetReedSprite()
        {
            if (reedSprite != null) return reedSprite;
            int sz = 36;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color stem = new Color(0.25f, 0.49f, 0.24f, 1f);
            Color cattail = new Color(0.48f, 0.32f, 0.19f, 1f);

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float dx = x - sz * 0.5f;
                    float dy = y - sz * 0.25f;
                    Color c = Color.clear;

                    if (dy >= 0f && dy < 22f)
                    {
                        if (Mathf.Abs(dx) < 1.5f ||
                            Mathf.Abs(dx - dy * 0.25f) < 1.3f ||
                            Mathf.Abs(dx + dy * 0.25f) < 1.3f)
                        {
                            c = stem;
                        }
                    }

                    if (Vector2.Distance(new Vector2(dx * 1.8f, dy - 18f), Vector2.zero) < 5.5f)
                    {
                        c = cattail;
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            reedSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 1f);
            return reedSprite;
        }
    }
}
