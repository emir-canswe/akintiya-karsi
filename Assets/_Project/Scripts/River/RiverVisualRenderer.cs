using UnityEngine;
using Upstream.Camera;
using Upstream.Core;
using Upstream.Data;

namespace Upstream.River
{
    /// <summary>
    /// Renders the bright cartoon river: deep-to-light water gradient, scrolling sunlight caustics, glints and
    /// bubbles, grassy banks with a sandy, foamy shore, bank decorations (trees, flower bushes, mushrooms,
    /// stones, lily pads) and pooled sparkle/bubble bursts. Water colors blend between regions.
    /// </summary>
    public class RiverVisualRenderer : MonoBehaviour
    {
        [SerializeField] private RiverController riverController;
        [SerializeField] private GameConfig gameConfig;

        private const float TileH = 128f; // world height of one grass/shore/caustics tile
        private const float BankWidth = 220f;

        private SpriteRenderer waterDeep;
        private SpriteRenderer waterGlow;
        private SpriteRenderer causticsNear;
        private SpriteRenderer causticsFar;
        private Transform leftGrass, rightGrass, leftShore, rightShore;

        private Transform[] glints;
        private Transform[] bubbles;
        private Transform[] bankDecos;
        private float[] bankDecoX;

        private struct BurstParticle
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public Vector2 velocity;
            public float life;
            public float maxLife;
            public float size;
            public float spin;
            public Color baseColor;
            public bool active;
        }

        private BurstParticle[] particles;
        private Player.PlayerController player;
        private float bubbleTimer;

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
            GameEvents.OnCoinCollected += HandleCoinCollected;
            GameEvents.OnWaveHit += HandleWaveHit;
            GameEvents.OnBigWaveHit += HandleBigWaveHit;
            GameEvents.OnShieldBlocked += HandleShieldBlocked;
        }

        private void OnDisable()
        {
            GameEvents.OnFoodEaten -= HandleFoodEaten;
            GameEvents.OnPlayerHurt -= HandlePlayerHurt;
            GameEvents.OnSharkEaten -= HandleSharkEaten;
            GameEvents.OnGrowthStageChanged -= HandleStageUp;
            GameEvents.OnCoinCollected -= HandleCoinCollected;
            GameEvents.OnWaveHit -= HandleWaveHit;
            GameEvents.OnBigWaveHit -= HandleBigWaveHit;
            GameEvents.OnShieldBlocked -= HandleShieldBlocked;
        }

        private float RiverHalfWidth => gameConfig != null ? gameConfig.riverHalfWidth : 150f;

        private void BuildRiverVisuals()
        {
            float rw = RiverHalfWidth;
            // Tall enough for the fitted camera view (taller on narrow phones) plus shake/zoom margin.
            float quadH = GameCamera.ViewHalfHeight * 2f + 150f;
            float tiledH = quadH + TileH * 2f;

            // Water: deep color everywhere, lighter band down the middle, two layers of sunlight.
            waterDeep = CreateRenderer("WaterDeep", new Vector3(0f, 0f, 5f), WatercolorSpriteFactory.GetWhiteSprite(), -20);
            waterDeep.transform.localScale = new Vector3(rw * 2f + 20f, quadH, 1f);
            waterDeep.color = new Color(0.10f, 0.65f, 0.85f);

            waterGlow = CreateRenderer("WaterGlow", new Vector3(0f, 0f, 4.9f), CartoonSpriteFactory.GetWaterGlow(), -19);
            waterGlow.transform.localScale = new Vector3(rw * 2f / 64f, quadH / 4f, 1f);
            waterGlow.color = new Color(0.31f, 0.89f, 0.94f);

            causticsFar = CreateTiled("CausticsFar", CartoonSpriteFactory.GetCaustics(), -18, new Vector2(rw * 2f, tiledH), 0f);
            causticsFar.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
            causticsFar.size = new Vector2(rw * 2f / 1.6f, tiledH / 1.6f + TileH);
            causticsFar.color = new Color(1f, 1f, 1f, 0.10f);
            causticsNear = CreateTiled("CausticsNear", CartoonSpriteFactory.GetCaustics(), -17, new Vector2(rw * 2f, tiledH), 0f);
            causticsNear.color = new Color(1f, 1f, 1f, 0.17f);

            // Banks: tiled grass, then the sand/foam shore strip whose foam line sits on the water's edge.
            Sprite grass = CartoonSpriteFactory.GetGrassTile();
            leftGrass = CreateTiled("LeftGrass", grass, -6, new Vector2(BankWidth, tiledH), -rw - BankWidth * 0.5f - 10f).transform;
            rightGrass = CreateTiled("RightGrass", grass, -6, new Vector2(BankWidth, tiledH), rw + BankWidth * 0.5f + 10f).transform;

            Sprite shore = CartoonSpriteFactory.GetShoreTile();
            // Shore tile is 40 wide with its foam at x≈33: place it so the foam line meets x = ±rw.
            leftShore = CreateTiled("LeftShore", shore, -5, new Vector2(40f, tiledH), -rw - 13f).transform;
            var rs = CreateTiled("RightShore", shore, -5, new Vector2(40f, tiledH), rw + 13f);
            rs.flipX = true;
            rightShore = rs.transform;

            // Surface glints and drifting bubbles
            glints = new Transform[14];
            for (int i = 0; i < glints.Length; i++)
            {
                var sr = CreateRenderer($"Glint_{i}", Vector3.zero, CartoonSpriteFactory.GetGlint(), -15);
                sr.color = new Color(1f, 1f, 1f, 0.28f);
                sr.transform.localScale = new Vector3(0.7f, 0.6f + (i % 3) * 0.25f, 1f);
                glints[i] = sr.transform;
            }
            bubbles = new Transform[8];
            for (int i = 0; i < bubbles.Length; i++)
            {
                var sr = CreateRenderer($"Bubble_{i}", Vector3.zero, CartoonSpriteFactory.GetBubble(), -14);
                sr.color = new Color(1f, 1f, 1f, 0.7f);
                sr.transform.localScale = Vector3.one * (0.35f + Hash01(i, 3, 1) * 0.35f);
                bubbles[i] = sr.transform;
            }

            BuildBankDecorations(rw);
        }

        private void BuildBankDecorations(float rw)
        {
            const int perSide = 16;
            bankDecos = new Transform[perSide * 2];
            bankDecoX = new float[perSide * 2];

            Color[] stoneTints = { new Color(1f, 0.92f, 0.85f), new Color(0.85f, 0.92f, 1f), new Color(0.95f, 0.88f, 1f) };

            for (int s = 0; s < 2; s++)
            {
                int side = s == 0 ? -1 : 1;
                for (int i = 0; i < perSide; i++)
                {
                    int idx = s * perSide + i;
                    var sr = CreateRenderer($"BankDeco_{side}_{i}", Vector3.zero, null, -3);
                    Transform t = sr.transform;
                    float r = Hash01(i, side, 1);
                    float jitter = Hash01(i, side, 2);
                    float x;

                    if (r < 0.22f)
                    {
                        // Tree canopy peeking in from the edge of the screen
                        sr.sprite = CartoonSpriteFactory.GetTree();
                        sr.sortingOrder = -2;
                        t.localScale = Vector3.one * (0.75f + jitter * 0.3f);
                        x = rw + 34f + jitter * 10f;
                    }
                    else if (r < 0.45f)
                    {
                        sr.sprite = CartoonSpriteFactory.GetFlowerBush();
                        t.localScale = Vector3.one * (0.6f + jitter * 0.25f);
                        x = rw + 22f + jitter * 8f;
                    }
                    else if (r < 0.58f)
                    {
                        sr.sprite = CartoonSpriteFactory.GetMushroom();
                        t.localScale = Vector3.one * (0.42f + jitter * 0.15f);
                        x = rw + 16f + jitter * 6f;
                    }
                    else if (r < 0.78f)
                    {
                        // Pebbles on the sand
                        sr.sprite = CartoonSpriteFactory.GetStone();
                        sr.color = stoneTints[i % stoneTints.Length];
                        t.localScale = Vector3.one * (0.4f + jitter * 0.25f);
                        x = rw + 7f + jitter * 7f;
                    }
                    else
                    {
                        // Lily pad floating at the very edge of the water (purely decorative)
                        sr.sprite = CartoonSpriteFactory.GetLilyPad();
                        sr.sortingOrder = -12;
                        t.localScale = Vector3.one * (0.5f + jitter * 0.2f);
                        x = rw - 9f - jitter * 4f;
                    }
                    t.localRotation = Quaternion.Euler(0f, 0f, jitter * 360f);
                    bankDecos[idx] = t;
                    bankDecoX[idx] = side * x;
                }
            }
        }

        private void BuildParticlePool()
        {
            particles = new BurstParticle[64];
            for (int i = 0; i < particles.Length; i++)
            {
                var sr = CreateRenderer($"Particle_{i}", Vector3.zero, CartoonSpriteFactory.GetSparkle(), 15);
                sr.gameObject.SetActive(false);
                particles[i] = new BurstParticle { transform = sr.transform, renderer = sr, active = false };
            }
        }

        /// <summary>Sparkle (or bubble) burst. <paramref name="size"/> is the sprite scale.</summary>
        public void SpawnBurst(Vector2 pos, Color col, int count = 8, float speed = 85f, float size = 0.22f, bool asBubbles = false)
        {
            Sprite sprite = asBubbles ? CartoonSpriteFactory.GetBubble() : CartoonSpriteFactory.GetSparkle();
            int spawned = 0;
            for (int i = 0; i < particles.Length && spawned < count; i++)
            {
                if (particles[i].active) continue;
                ref BurstParticle p = ref particles[i];
                p.active = true;
                p.maxLife = p.life = Random.Range(0.4f, 0.65f);
                p.baseColor = col;
                p.velocity = Random.insideUnitCircle.normalized * speed * Random.Range(0.5f, 1f);
                p.size = size * Random.Range(0.7f, 1.3f) * 3f; // sparkle sprite is ~3x smaller than the old glow
                p.spin = Random.Range(-360f, 360f);
                p.renderer.sprite = sprite;
                p.renderer.color = col;
                p.transform.position = new Vector3(pos.x, pos.y, 0f);
                p.transform.localScale = Vector3.one * p.size;
                p.transform.gameObject.SetActive(true);
                spawned++;
            }
        }

        private Vector2 PlayerPos(Vector2 offset, Vector2 fallback)
        {
            if (player == null) player = FindFirstObjectByType<Player.PlayerController>();
            return player != null ? player.Position + offset : fallback;
        }

        private void HandleFoodEaten(bool isGold)
        {
            Vector2 pos = PlayerPos(new Vector2(0f, 18f), new Vector2(0f, -120f));
            if (isGold) SpawnBurst(pos, new Color(1f, 0.88f, 0.3f, 1f), 12, 120f, 0.30f);
            else SpawnBurst(pos, new Color(1f, 1f, 1f, 0.9f), 6, 70f, 0.22f, asBubbles: true);
        }

        private void HandleCoinCollected(int amount)
        {
            Vector2 pos = PlayerPos(new Vector2(0f, 16f), new Vector2(0f, -120f));
            SpawnBurst(pos, new Color(1f, 0.86f, 0.25f, 1f), 5, 90f, 0.20f);
        }

        private void HandleShieldBlocked(int chargesLeft)
        {
            Vector2 pos = PlayerPos(Vector2.zero, new Vector2(0f, -140f));
            SpawnBurst(pos, new Color(0.6f, 0.95f, 1f, 1f), 14, 150f, 0.3f, asBubbles: true);
        }

        private void HandleWaveHit()
        {
            Vector2 pos = PlayerPos(new Vector2(0f, 14f), new Vector2(0f, -120f));
            SpawnBurst(pos, new Color(1f, 1f, 1f, 0.9f), 10, 110f, 0.26f, asBubbles: true);
        }

        private void HandleBigWaveHit()
        {
            Vector2 pos = PlayerPos(new Vector2(0f, 10f), new Vector2(0f, -120f));
            SpawnBurst(pos, new Color(1f, 1f, 1f, 0.95f), 16, 150f, 0.3f, asBubbles: true);
        }

        private void HandlePlayerHurt(int hp)
        {
            Vector2 pos = PlayerPos(Vector2.zero, new Vector2(0f, -140f));
            SpawnBurst(pos, new Color(1f, 0.35f, 0.45f, 1f), 12, 120f, 0.26f);
        }

        private void HandleSharkEaten()
        {
            Vector2 pos = PlayerPos(new Vector2(0f, 24f), new Vector2(0f, -120f));
            SpawnBurst(pos, new Color(1f, 0.86f, 0.25f, 1f), 14, 170f, 0.34f);
            SpawnBurst(pos, new Color(1f, 0.5f, 0.75f, 1f), 8, 130f, 0.26f);
        }

        private void HandleStageUp(int stage)
        {
            if (stage <= 0) return;
            Vector2 pos = PlayerPos(Vector2.zero, new Vector2(0f, -140f));
            SpawnBurst(pos, new Color(0.45f, 1f, 0.8f, 1f), 10, 140f, 0.28f);
            SpawnBurst(pos, new Color(1f, 0.95f, 0.5f, 1f), 8, 110f, 0.24f);
        }

        private SpriteRenderer CreateRenderer(string name, Vector3 pos, Sprite sprite, int sortOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortOrder;
            return sr;
        }

        private SpriteRenderer CreateTiled(string name, Sprite sprite, int sortOrder, Vector2 size, float x)
        {
            var sr = CreateRenderer(name, new Vector3(x, 0f, 2f), sprite, sortOrder);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = size;
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
            float rw = RiverHalfWidth;
            float viewH = GameCamera.ViewHalfHeight;

            // Region color blend
            if (riverController != null && riverController.ActiveRegion != null)
            {
                RegionData reg = riverController.ActiveRegion;
                waterGlow.color = Color.Lerp(waterGlow.color, reg.waterColor1, Time.deltaTime * 2f);
                waterDeep.color = Color.Lerp(waterDeep.color, reg.waterColor2, Time.deltaTime * 2f);
            }

            // Tiled layers scroll with the river (pattern repeats every tile, so wrap the offset).
            float bankY = -Mathf.Repeat(scroll, TileH);
            SetY(leftGrass, bankY);
            SetY(rightGrass, bankY);
            SetY(leftShore, bankY);
            SetY(rightShore, bankY);
            SetY(causticsNear.transform, -Mathf.Repeat(scroll * 1.05f, TileH));
            SetY(causticsFar.transform, -Mathf.Repeat(scroll * 0.8f + Time.time * 6f, TileH * 1.6f));
            Vector3 farPos = causticsFar.transform.localPosition;
            farPos.x = Mathf.Sin(Time.time * 0.3f) * 10f; // slow sideways shimmer
            causticsFar.transform.localPosition = farPos;

            // Glints: drift with the water
            float spanY = (viewH + 40f) * 2f;
            for (int i = 0; i < glints.Length; i++)
            {
                float x = -rw + 18f + ((i * 53f) % ((rw - 18f) * 2f));
                float y = Mathf.Repeat(i * 97f - scroll * 1.1f, spanY) - (viewH + 40f);
                glints[i].localPosition = new Vector3(x + Mathf.Sin(i + Time.time * 2f) * 3f, y, 0f);
            }

            // Bubbles: rise slowly against the current with a wobble
            for (int i = 0; i < bubbles.Length; i++)
            {
                float x = -rw + 30f + Hash01(i, 1, 4) * (rw * 2f - 60f);
                float y = Mathf.Repeat(i * 151f - scroll * 0.75f, spanY) - (viewH + 40f);
                bubbles[i].localPosition = new Vector3(x + Mathf.Sin(Time.time * 2.4f + i) * 5f, y, 0f);
            }

            // Bank decorations scroll with the banks
            float seg = Mathf.Max(44f, (viewH * 2f + 100f) / 16f);
            float decoSpan = 16 * seg;
            for (int idx = 0; idx < bankDecos.Length; idx++)
            {
                int i = idx % 16;
                float y = Mathf.Repeat(i * seg - scroll + (idx >= 16 ? seg * 0.5f : 0f), decoSpan) - (viewH + 50f);
                bankDecos[idx].localPosition = new Vector3(bankDecoX[idx], y, 0f);
            }

            UpdatePlayerBubbles(scroll);
            UpdateParticles(Time.deltaTime);
        }

        // A little trail of bubbles behind the swimming fish.
        private void UpdatePlayerBubbles(float scroll)
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;
            bubbleTimer -= Time.deltaTime;
            if (bubbleTimer > 0f) return;
            bubbleTimer = Random.Range(0.22f, 0.4f);
            Vector2 pos = PlayerPos(new Vector2(Random.Range(-5f, 5f), -26f), new Vector2(0f, -170f));
            SpawnBurst(pos, new Color(1f, 1f, 1f, 0.75f), 1, 25f, 0.12f, asBubbles: true);
        }

        private void UpdateParticles(float dt)
        {
            float flow = riverController != null ? riverController.FlowSpeed : 150f;
            for (int i = 0; i < particles.Length; i++)
            {
                if (!particles[i].active) continue;
                ref BurstParticle p = ref particles[i];
                p.life -= dt;
                if (p.life <= 0f)
                {
                    p.active = false;
                    p.transform.gameObject.SetActive(false);
                    continue;
                }
                p.velocity *= 1f - 2.5f * dt; // ease out
                Vector3 pos = p.transform.position;
                pos.x += p.velocity.x * dt;
                pos.y += (p.velocity.y - flow * 0.35f) * dt; // carried a bit by the current
                p.transform.position = pos;
                p.transform.Rotate(0f, 0f, p.spin * dt);

                float k = p.life / p.maxLife;
                p.transform.localScale = Vector3.one * p.size * (0.4f + 0.6f * k);
                Color c = p.baseColor;
                c.a *= Mathf.Clamp01(k * 1.5f);
                p.renderer.color = c;
            }
        }

        private static void SetY(Transform t, float y)
        {
            Vector3 p = t.localPosition;
            p.y = y;
            t.localPosition = p;
        }
    }
}
