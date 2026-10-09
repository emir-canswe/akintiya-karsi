using UnityEngine;
using Upstream.Camera;
using Upstream.Core;
using Upstream.Data;

namespace Upstream.River
{
    /// <summary>
    /// Renders the scrolling watercolor river, animated current streaks, sandy/grassy riverbanks,
    /// deterministic bank decorations (reeds, stones, bushes), and pooled splash particles.
    /// Smoothly blends colors across regions (Mountain Creek -> Wide Lake -> Rapids).
    /// </summary>
    public class RiverVisualRenderer : MonoBehaviour
    {
        [SerializeField] private RiverController riverController;
        [SerializeField] private GameConfig gameConfig;

        private SpriteRenderer waterCenterRenderer;
        private SpriteRenderer waterLeftRenderer;
        private SpriteRenderer waterRightRenderer;
        private SpriteRenderer leftSandRenderer;
        private SpriteRenderer rightSandRenderer;
        private SpriteRenderer leftGrassRenderer;
        private SpriteRenderer rightGrassRenderer;

        private Transform[] currentStreaks;
        private Transform[] bankDecos;
        private int[] bankDecoSides;
        private int[] bankDecoIndices;

        // Simple pooled burst particles
        private struct BurstParticle
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public Vector2 velocity;
            public float life;
            public float maxLife;
            public Color baseColor;
            public bool active;
        }

        private BurstParticle[] particles;

        public void Configure(RiverController controller, GameConfig config)
        {
            riverController = controller;
            gameConfig = config;
        }

        private void Awake()
        {
            if (riverController == null) riverController = GetComponent<RiverController>();
            BuildRiverVisuals();
            BuildParticlePool();
        }

        private void OnEnable()
        {
            GameEvents.OnFoodEaten += HandleFoodEaten;
            GameEvents.OnPlayerHurt += HandlePlayerHurt;
            GameEvents.OnSharkEaten += HandleSharkEaten;
            GameEvents.OnGrowthStageChanged += HandleStageUp;
        }

        private void OnDisable()
        {
            GameEvents.OnFoodEaten -= HandleFoodEaten;
            GameEvents.OnPlayerHurt -= HandlePlayerHurt;
            GameEvents.OnSharkEaten -= HandleSharkEaten;
            GameEvents.OnGrowthStageChanged -= HandleStageUp;
        }

        private void BuildRiverVisuals()
        {
            float rw = gameConfig != null ? gameConfig.riverHalfWidth : 150f;
            Sprite white = WatercolorSpriteFactory.GetWhiteSprite();
            // Tall enough for the fitted camera view (taller on narrow phones) plus shake/zoom margin.
            float quadH = GameCamera.ViewHalfHeight * 2f + 150f;

            // Water background layers
            waterLeftRenderer = CreateQuad("WaterLeft", new Vector3(-rw * 0.5f, 0f, 5f), new Vector3(rw, quadH, 1f), new Color(0.11f, 0.49f, 0.60f), -20, white);
            waterRightRenderer = CreateQuad("WaterRight", new Vector3(rw * 0.5f, 0f, 5f), new Vector3(rw, quadH, 1f), new Color(0.11f, 0.49f, 0.60f), -20, white);
            waterCenterRenderer = CreateQuad("WaterCenter", new Vector3(0f, 0f, 4.9f), new Vector3(rw * 1.15f, quadH, 1f), new Color(0.16f, 0.64f, 0.77f), -19, white);

            // Sand & Grass riverbanks
            float bankWidth = 220f;
            leftSandRenderer = CreateQuad("LeftSand", new Vector3(-rw - bankWidth * 0.5f, 0f, 2f), new Vector3(bankWidth, quadH, 1f), new Color(0.89f, 0.79f, 0.56f), -5, white);
            rightSandRenderer = CreateQuad("RightSand", new Vector3(rw + bankWidth * 0.5f, 0f, 2f), new Vector3(bankWidth, quadH, 1f), new Color(0.89f, 0.79f, 0.56f), -5, white);

            leftGrassRenderer = CreateQuad("LeftGrass", new Vector3(-rw - 16f - bankWidth * 0.5f, 0f, 1.9f), new Vector3(bankWidth, quadH, 1f), new Color(0.35f, 0.64f, 0.35f), -4, white);
            rightGrassRenderer = CreateQuad("RightGrass", new Vector3(rw + 16f + bankWidth * 0.5f, 0f, 1.9f), new Vector3(bankWidth, quadH, 1f), new Color(0.35f, 0.64f, 0.35f), -4, white);

            // 18 Scrolling current streaks
            currentStreaks = new Transform[18];
            for (int i = 0; i < currentStreaks.Length; i++)
            {
                float h = 24f + (i % 3) * 8f;
                var sr = CreateQuad($"Streak_{i}", Vector3.zero, new Vector3(2.4f, h, 1f), new Color(1f, 1f, 1f, 0.16f), -15, white);
                currentStreaks[i] = sr.transform;
            }

            // Bank decorations (16 per side = 32 total)
            int perSide = 16;
            bankDecos = new Transform[perSide * 2];
            bankDecoSides = new int[perSide * 2];
            bankDecoIndices = new int[perSide * 2];

            Sprite reedSpr = WatercolorSpriteFactory.GetReedSprite();
            Sprite bushSpr = WatercolorSpriteFactory.GetBushSprite();
            Sprite rockSpr = WatercolorSpriteFactory.GetRockSprite();

            for (int s = 0; s < 2; s++)
            {
                int side = s == 0 ? -1 : 1;
                for (int i = 0; i < perSide; i++)
                {
                    int idx = s * perSide + i;
                    bankDecoSides[idx] = side;
                    bankDecoIndices[idx] = i;

                    GameObject decoObj = new GameObject($"BankDeco_{side}_{i}");
                    decoObj.transform.SetParent(transform, false);
                    var sr = decoObj.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = -2;

                    float r = Hash01(i, side, 1);
                    if (r < 0.45f) sr.sprite = reedSpr;
                    else if (r < 0.75f)
                    {
                        sr.sprite = rockSpr;
                        decoObj.transform.localScale = Vector3.one * 0.38f;
                    }
                    else sr.sprite = bushSpr;

                    bankDecos[idx] = decoObj.transform;
                }
            }
        }

        private void BuildParticlePool()
        {
            particles = new BurstParticle[48];
            Sprite glow = WatercolorSpriteFactory.GetCircleGlowSprite();
            for (int i = 0; i < particles.Length; i++)
            {
                GameObject pObj = new GameObject($"Particle_{i}");
                pObj.transform.SetParent(transform, false);
                pObj.SetActive(false);
                var sr = pObj.AddComponent<SpriteRenderer>();
                sr.sprite = glow;
                sr.sortingOrder = 15;
                particles[i] = new BurstParticle
                {
                    transform = pObj.transform,
                    renderer = sr,
                    active = false
                };
            }
        }

        public void SpawnBurst(Vector2 pos, Color col, int count = 8, float speed = 85f, float size = 0.22f)
        {
            int spawned = 0;
            for (int i = 0; i < particles.Length && spawned < count; i++)
            {
                if (!particles[i].active)
                {
                    particles[i].active = true;
                    particles[i].life = 0.5f;
                    particles[i].maxLife = 0.5f;
                    particles[i].baseColor = col;
                    particles[i].velocity = Random.insideUnitCircle * speed;
                    particles[i].transform.position = new Vector3(pos.x, pos.y, 0f);
                    particles[i].transform.localScale = Vector3.one * size;
                    particles[i].renderer.color = col;
                    particles[i].transform.gameObject.SetActive(true);
                    spawned++;
                }
            }
        }

        private void HandleFoodEaten(bool isGold)
        {
            var player = FindFirstObjectByType<Player.PlayerController>();
            Vector2 pos = player != null ? player.Position + new Vector2(0f, 18f) : new Vector2(0f, -120f);
            Color c = isGold ? new Color(1f, 0.84f, 0.35f, 1f) : new Color(0.49f, 0.94f, 0.78f, 1f);
            SpawnBurst(pos, c, 7, 75f, 0.20f);
        }

        private void HandlePlayerHurt(int hp)
        {
            var player = FindFirstObjectByType<Player.PlayerController>();
            Vector2 pos = player != null ? player.Position : new Vector2(0f, -140f);
            SpawnBurst(pos, new Color(1f, 0.36f, 0.42f, 1f), 12, 110f, 0.25f);
        }

        private void HandleSharkEaten()
        {
            var player = FindFirstObjectByType<Player.PlayerController>();
            Vector2 pos = player != null ? player.Position + new Vector2(0f, 24f) : new Vector2(0f, -120f);
            SpawnBurst(pos, new Color(1f, 0.84f, 0.35f, 1f), 18, 160f, 0.34f);
        }

        private void HandleStageUp(int stage)
        {
            if (stage <= 0) return;
            var player = FindFirstObjectByType<Player.PlayerController>();
            Vector2 pos = player != null ? player.Position : new Vector2(0f, -140f);
            SpawnBurst(pos, new Color(0.49f, 0.94f, 0.78f, 1f), 14, 130f, 0.28f);
        }

        private SpriteRenderer CreateQuad(string name, Vector3 pos, Vector3 scale, Color color, int sortOrder, Sprite sprite)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortOrder;
            return sr;
        }

        private static float Hash01(int i, int side, int n)
        {
            float v = Mathf.Sin(i * 127.1f + side * 311.7f + n * 74.7f) * 43758.5453f;
            return v - Mathf.Floor(v);
        }

        private void Update()
        {
            float scroll = riverController != null ? riverController.ScrollOffset : Time.time * 100f;
            float rw = gameConfig != null ? gameConfig.riverHalfWidth : 150f;

            // Smooth region color transition
            if (riverController != null && riverController.ActiveRegion != null)
            {
                RegionData reg = riverController.ActiveRegion;
                waterCenterRenderer.color = Color.Lerp(waterCenterRenderer.color, reg.waterColor1, Time.deltaTime * 2f);
                waterLeftRenderer.color = Color.Lerp(waterLeftRenderer.color, reg.waterColor2, Time.deltaTime * 2f);
                waterRightRenderer.color = Color.Lerp(waterRightRenderer.color, reg.waterColor2, Time.deltaTime * 2f);
            }

            // Animate current streaks scrolling from just above the view to just below it
            float viewH = GameCamera.ViewHalfHeight;
            float spanY = (viewH + 40f) * 2f;
            for (int i = 0; i < currentStreaks.Length; i++)
            {
                float x = -rw + 18f + ((i * 53f) % ((rw - 18f) * 2f));
                float rawY = (i * 97f - scroll * 1.1f) % spanY;
                if (rawY < 0f) rawY += spanY;
                float y = rawY - (viewH + 40f);
                currentStreaks[i].localPosition = new Vector3(x + Mathf.Sin(i + Time.time * 2f) * 3f, y, 0f);
            }

            // Animate bank decorations scrolling downward
            float seg = Mathf.Max(44f, (viewH * 2f + 100f) / 16f);
            float decoSpan = 16 * seg;
            for (int idx = 0; idx < bankDecos.Length; idx++)
            {
                int side = bankDecoSides[idx];
                int i = bankDecoIndices[idx];
                float rawY = (i * seg - scroll) % decoSpan;
                if (rawY < 0f) rawY += decoSpan;
                float y = rawY - (viewH + 50f);
                float off = Hash01(i, side, 2) * 28f;
                float x = side * (rw + 26f + off);
                bankDecos[idx].localPosition = new Vector3(x, y, 0f);
            }

            // Update burst particles
            float dt = Time.deltaTime;
            for (int i = 0; i < particles.Length; i++)
            {
                if (!particles[i].active) continue;
                particles[i].life -= dt;
                if (particles[i].life <= 0f)
                {
                    particles[i].active = false;
                    particles[i].transform.gameObject.SetActive(false);
                    continue;
                }
                Vector3 p = particles[i].transform.position;
                p.x += particles[i].velocity.x * dt;
                p.y += particles[i].velocity.y * dt;
                particles[i].transform.position = p;

                Color c = particles[i].baseColor;
                c.a = Mathf.Clamp01(particles[i].life / particles[i].maxLife);
                particles[i].renderer.color = c;
            }
        }
    }
}
