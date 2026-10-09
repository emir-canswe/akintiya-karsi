using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;
using Upstream.Ads;
using Upstream.Audio;
using Upstream.Camera;
using Upstream.Data;
using Upstream.Entities;
using Upstream.Mechanics;
using Upstream.Player;
using Upstream.River;
using Upstream.Spawning;
using Upstream.UI;

namespace Upstream.Core
{
    /// <summary>
    /// Automatically builds and wires the entire Upstream game at runtime if not already present in the scene.
    /// Ensures pressing Play in ANY scene immediately launches the complete playable game.
    /// </summary>
    public class UpstreamGameBootstrap : MonoBehaviour
    {
        private static bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            if (FindFirstObjectByType<UpstreamGameBootstrap>() != null) return;
            GameObject bootObj = new GameObject("UpstreamGameBootstrap");
            bootObj.AddComponent<UpstreamGameBootstrap>();
        }

        // Assign the assets from Assets/_Project/ScriptableObjects here (the scene builder does this
        // automatically) so balance tweaks in the Inspector actually reach the game. Missing ones fall
        // back to code defaults.
        [Header("Balance Configs")]
        [SerializeField] private GameConfig gameConfigAsset;
        [SerializeField] private GrowthConfig growthConfigAsset;
        [SerializeField] private SpawnConfig spawnConfigAsset;
        [SerializeField] private DifficultyConfig difficultyConfigAsset;

        private const int StageCount = 5;

        private GrowthSystem playerGrowth;
        private TMP_Text stageBadgeText;
        private Image stageProgressFill;
        private int shownStage = -1;

        private void Awake()
        {
            if (initialized && FindObjectsByType<UpstreamGameBootstrap>(FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject);
                return;
            }
            initialized = true;
            Application.targetFrameRate = 60;
            // Collisions are computed by CollisionManager; Unity's physics would only simulate the idle player collider.
            Physics2D.simulationMode = SimulationMode2D.Script;
            Physics.simulationMode = SimulationMode.Script;

            BuildEntireGame();

            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        }

        private void OnDestroy()
        {
            if (initialized) initialized = false;
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged()
        {
            shownStage = -1; // force the stage badge to re-render in the new language
        }

        private void Update()
        {
            // Keep Growth Stage HUD indicator updated
            if (playerGrowth != null && stageBadgeText != null)
            {
                int s = Mathf.Clamp(playerGrowth.CurrentStage, 0, StageCount - 1);
                if (s != shownStage)
                {
                    // Only touch the text when the stage changes: setting TMP text every frame rebuilds the mesh.
                    shownStage = s;
                    stageBadgeText.text = LocalizationManager.Get($"stage_badge_{s}");
                }
                if (stageProgressFill != null)
                {
                    stageProgressFill.fillAmount = playerGrowth.StageProgress;
                }
            }
        }

        private void BuildEntireGame()
        {
            // 1. Configs
            GameConfig gameConfig = gameConfigAsset != null ? gameConfigAsset : ScriptableObject.CreateInstance<GameConfig>();
            GrowthConfig growthConfig = growthConfigAsset != null ? growthConfigAsset : ScriptableObject.CreateInstance<GrowthConfig>();
            SpawnConfig spawnConfig = spawnConfigAsset != null ? spawnConfigAsset : ScriptableObject.CreateInstance<SpawnConfig>();
            DifficultyConfig diffConfig = difficultyConfigAsset != null ? difficultyConfigAsset : ScriptableObject.CreateInstance<DifficultyConfig>();

            // 2. Camera (Orthographic size 300 -> height 600, width ~360 on 9:16 portrait)
            UnityEngine.Camera mainCam = UnityEngine.Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<UnityEngine.Camera>();
                camObj.AddComponent<AudioListener>();
            }
            mainCam.orthographic = true;
            mainCam.orthographicSize = GameCamera.DesignHalfHeight;
            mainCam.backgroundColor = new Color(0.42f, 0.80f, 0.29f, 1f); // grass green behind everything
            mainCam.clearFlags = CameraClearFlags.SolidColor;

            GameCamera gameCamera = mainCam.GetComponent<GameCamera>();
            if (gameCamera == null) gameCamera = mainCam.gameObject.AddComponent<GameCamera>();
            gameCamera.SetBasePosition(new Vector3(0f, 0f, -10f));
            // Must happen before the river/spawner are built: they size themselves from the view height.
            gameCamera.FitToRiver(gameConfig.riverHalfWidth);

            // 3. Core Systems
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                GameObject gmObj = new GameObject("GameManager");
                gameManager = gmObj.AddComponent<GameManager>();
            }
            gameManager.Config = gameConfig;
            gameManager.GrowthConfig = growthConfig;

            ScoreSystem scoreSystem = gameManager.gameObject.AddComponent<ScoreSystem>();
            scoreSystem.Config = gameConfig;

            DifficultyScaler diffScaler = gameManager.gameObject.AddComponent<DifficultyScaler>();
            diffScaler.Config = diffConfig;
            // Ads: AdMob on device builds with USE_ADMOB, MockAdsService otherwise. Consent (UMP) first.
            AdsManager adsManager = gameManager.gameObject.AddComponent<AdsManager>();

            // 4. Audio Manager + Procedural Synth Clips
            AudioManager audioManager = FindFirstObjectByType<AudioManager>();
            if (audioManager == null)
            {
                GameObject audioObj = new GameObject("AudioManager");
                audioManager = audioObj.AddComponent<AudioManager>();
            }
            ProceduralAudioFactory.PopulateAudioManager(audioManager);

            // 5. River + Watercolor Visuals
            GameObject riverObj = new GameObject("River");
            RiverController riverController = riverObj.AddComponent<RiverController>();
            riverController.Config = gameConfig;
            riverController.DiffConfig = diffConfig;
            RiverVisualRenderer riverVisuals = riverObj.AddComponent<RiverVisualRenderer>();
            riverVisuals.Configure(riverController, gameConfig);

            // 6. Player Fish
            GameObject playerObj = new GameObject("PlayerFish");
            CircleCollider2D playerCol = playerObj.AddComponent<CircleCollider2D>();
            playerCol.radius = gameConfig.playerRadius;
            playerCol.isTrigger = true;

            GrowthSystem growthSystem = playerObj.AddComponent<GrowthSystem>();
            growthSystem.Config = growthConfig;
            playerGrowth = growthSystem;

            HealthSystem healthSystem = playerObj.AddComponent<HealthSystem>();
            healthSystem.Config = gameConfig;

            TurboSystem turboSystem = playerObj.AddComponent<TurboSystem>();
            turboSystem.Config = gameConfig;

            PlayerController playerController = playerObj.AddComponent<PlayerController>();
            playerController.Config = gameConfig;

            playerObj.AddComponent<PlayerFishRenderer>();

            // 7. Entity Prefab Templates & Object Pools
            Transform templatesRoot = new GameObject("EntityTemplates").transform;
            templatesRoot.SetParent(transform, false);
            templatesRoot.gameObject.SetActive(false);

            Transform poolsRoot = new GameObject("ObjectPools").transform;
            poolsRoot.SetParent(transform, false);

            FoodPickup foodPrefab = CreateEntityTemplate<FoodPickup>("FoodTemplate", templatesRoot, CartoonSpriteFactory.GetShrimp(), 5);
            GoldFoodPickup goldPrefab = CreateEntityTemplate<GoldFoodPickup>("GoldFoodTemplate", templatesRoot, CartoonSpriteFactory.GetStar(), 6);
            SharkEnemy sharkPrefab = CreateEntityTemplate<SharkEnemy>("SharkTemplate", templatesRoot, CartoonSpriteFactory.GetPredatorBody(PredatorKind.Shark), 7, new Vector2(6f, -9f));
            RockObstacle rockPrefab = CreateEntityTemplate<RockObstacle>("RockTemplate", templatesRoot, CartoonSpriteFactory.GetRock(), 4, new Vector2(5f, -7f));
            LogObstacle logPrefab = CreateEntityTemplate<LogObstacle>("LogTemplate", templatesRoot, CartoonSpriteFactory.GetLog(), 4, new Vector2(5f, -7f));
            HookTrap hookPrefab = CreateHookTemplate("HookTemplate", templatesRoot);
            CoinPickup coinPrefab = CreateEntityTemplate<CoinPickup>("CoinTemplate", templatesRoot, WatercolorSpriteFactory.GetCoinSprite(), 6);
            WaveObstacle wavePrefab = CreateEntityTemplate<WaveObstacle>("WaveTemplate", templatesRoot, CartoonSpriteFactory.GetWave(), 4, castShadow: false);

            var foodPool = CreatePool("FoodPool", foodPrefab, 24, poolsRoot);
            var goldPool = CreatePool("GoldFoodPool", goldPrefab, 6, poolsRoot);
            var sharkPool = CreatePool("SharkPool", sharkPrefab, 12, poolsRoot); // piranhas come in threes
            var rockPool = CreatePool("RockPool", rockPrefab, 10, poolsRoot);
            var logPool = CreatePool("LogPool", logPrefab, 8, poolsRoot);
            var hookPool = CreatePool("HookPool", hookPrefab, 8, poolsRoot);
            var coinPool = CreatePool("CoinPool", coinPrefab, 24, poolsRoot);
            var wavePool = CreatePool("WavePool", wavePrefab, 6, poolsRoot); // a big-wave set is 3 on its own

            // 8. SpawnManager & UI & CollisionManager
            GameObject spawnerObj = new GameObject("SpawnManager");
            SpawnManager spawnManager = spawnerObj.AddComponent<SpawnManager>();
            spawnManager.Configure(
                spawnConfig,
                gameConfig,
                playerObj.transform,
                riverController,
                growthSystem,
                foodPool,
                goldPool,
                sharkPool,
                rockPool,
                logPool,
                hookPool,
                diffScaler,
                coinPool,
                wavePool);

            ToastUI toastUI = BuildUI(turboSystem, scoreSystem, adsManager.Ads);

            CollisionManager collisionManager = spawnerObj.AddComponent<CollisionManager>();
            collisionManager.Configure(
                spawnManager,
                playerObj.transform,
                healthSystem,
                turboSystem,
                growthSystem,
                scoreSystem,
                riverController,
                gameCamera,
                toastUI,
                gameConfig,
                growthConfig);
        }

        private static T CreateEntityTemplate<T>(string name, Transform parent, Sprite sprite, int sortingOrder, Vector2? shadowOffset = null, bool castShadow = true) where T : EntityBase
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            if (castShadow) AddDropShadow(go.transform, sprite, shadowOffset ?? new Vector2(3f, -5f));
            return go.AddComponent<T>();
        }

        /// <summary>Soft silhouette of the same sprite, offset down-right on the water: gives everything depth.</summary>
        private static void AddDropShadow(Transform owner, Sprite sprite, Vector2 offset)
        {
            GameObject shadow = new GameObject("Shadow");
            shadow.transform.SetParent(owner, false);
            shadow.transform.localPosition = new Vector3(offset.x, offset.y, 0.1f);
            var sr = shadow.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0f, 0.12f, 0.28f, 0.28f); // multiplies to a dark, translucent silhouette
            sr.sortingOrder = 2;
        }

        private static HookTrap CreateHookTemplate(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CartoonSpriteFactory.GetHook();
            sr.sortingOrder = 6;
            AddDropShadow(go.transform, sr.sprite, new Vector2(3f, -5f));

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = 1.4f;
            lr.endWidth = 1.4f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            // Dark fishing line reads clearly on the bright water.
            lr.startColor = new Color(0.10f, 0.16f, 0.30f, 0.55f);
            lr.endColor = new Color(0.10f, 0.16f, 0.30f, 0.75f);
            lr.sortingOrder = 5;

            return go.AddComponent<HookTrap>();
        }

        private static ObjectPool<T> CreatePool<T>(string poolName, T prefab, int initialSize, Transform parent) where T : Component
        {
            GameObject poolObj = new GameObject(poolName);
            poolObj.transform.SetParent(parent, false);
            // The GameObject is just a tidy hierarchy parent for pooled instances.
            return new ObjectPool<T>(prefab, initialSize, poolObj.transform);
        }

        private ToastUI BuildUI(TurboSystem turboSystem, ScoreSystem scoreSystem, IAdsService adsService)
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<InputSystemUIInputModule>();
            }

            GameObject canvasObj = new GameObject("GameCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(360f, 640f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            Sprite whiteSpr = WatercolorSpriteFactory.GetWhiteSprite();
            Sprite heartSpr = CartoonSpriteFactory.GetHeart();

            // --- SAFE AREA (HUD, menu, buttons stay clear of notches; dim overlays stay full-screen) ---
            GameObject safeObj = CreateUIRect("SafeArea", canvasObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safeObj.AddComponent<SafeAreaFitter>();
            Transform safeRoot = safeObj.transform;

            // --- HUD ROOT ---
            GameObject hudObj = CreateUIRect("HUD", safeRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            MakeSubCanvas(hudObj, interactive: true); // distance/coins change constantly: rebuild only the HUD
            HUDController hud = hudObj.AddComponent<HUDController>();

            // Health bar (Top-Left): one bar that each hit takes a chunk out of, with a heart at its left end.
            // Spans x 10..106, the same room the old heart row used, so it stays clear of the distance text.
            GameObject healthBgObj = CreateUIRect("HealthBarBg", hudObj.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(70f, -28f), new Vector2(72f, 14f));
            Image healthBgImg = healthBgObj.AddComponent<Image>();
            healthBgImg.sprite = WatercolorSpriteFactory.GetRoundedRectSprite();
            healthBgImg.type = Image.Type.Sliced;
            healthBgImg.color = new Color(0.07f, 0.12f, 0.33f, 0.6f);
            healthBgImg.raycastTarget = false;

            GameObject healthGhostObj = CreateUIRect("HealthBarGhost", healthBgObj.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-4f, -4f));
            Image healthGhostImg = healthGhostObj.AddComponent<Image>();
            healthGhostImg.sprite = CartoonSpriteFactory.GetBarFill();
            healthGhostImg.color = new Color(1f, 0.93f, 0.85f, 0.85f);
            healthGhostImg.type = Image.Type.Filled;
            healthGhostImg.fillMethod = Image.FillMethod.Horizontal;
            healthGhostImg.raycastTarget = false;

            GameObject healthFillObj = CreateUIRect("HealthBarFill", healthBgObj.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-4f, -4f));
            Image healthFillImg = healthFillObj.AddComponent<Image>();
            healthFillImg.sprite = CartoonSpriteFactory.GetBarFill();
            healthFillImg.color = new Color(1f, 0.36f, 0.42f, 1f);
            healthFillImg.type = Image.Type.Filled;
            healthFillImg.fillMethod = Image.FillMethod.Horizontal;
            healthFillImg.raycastTarget = false;

            GameObject heartObj = CreateUIRect("HealthHeart", hudObj.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -28f), new Vector2(25f, 25f));
            Image heartImg = heartObj.AddComponent<Image>();
            heartImg.sprite = heartSpr;
            heartImg.color = new Color(1f, 0.36f, 0.42f, 1f);
            heartImg.raycastTarget = false;

            // Distance Text (Top-Center)
            TMP_Text distText = CreateLabel("DistanceText", hudObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(220f, 52f), "0<size=50%>m</size>", 42f, TextAlignmentOptions.Center, FontStyles.Bold);
            SetOutline(distText, 0.28f);

            // Power Bar (Below Distance)
            GameObject powerBgObj = CreateUIRect("PowerBarBg", hudObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(168f, 13f));
            Image powerBgImg = powerBgObj.AddComponent<Image>();
            powerBgImg.sprite = WatercolorSpriteFactory.GetRoundedRectSprite();
            powerBgImg.type = Image.Type.Sliced;
            powerBgImg.color = new Color(0.07f, 0.12f, 0.33f, 0.6f);

            GameObject powerFillObj = CreateUIRect("PowerBarFill", powerBgObj.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-4f, -4f));
            Image powerFillImg = powerFillObj.AddComponent<Image>();
            powerFillImg.sprite = CartoonSpriteFactory.GetBarFill();
            powerFillImg.color = new Color(0.35f, 1f, 0.62f, 1f);
            powerFillImg.type = Image.Type.Filled;
            powerFillImg.fillMethod = Image.FillMethod.Horizontal;
            powerFillImg.fillAmount = 0f;

            // Growth Stage Indicator (Below Power Bar)
            GameObject stageBgObj = CreateUIRect("StageBarBg", hudObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(148f, 8f));
            Image stageBgImg = stageBgObj.AddComponent<Image>();
            stageBgImg.sprite = WatercolorSpriteFactory.GetRoundedRectSprite();
            stageBgImg.type = Image.Type.Sliced;
            stageBgImg.color = new Color(0.07f, 0.12f, 0.33f, 0.5f);

            GameObject stageFillObj = CreateUIRect("StageBarFill", stageBgObj.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-2f, -2f));
            stageProgressFill = stageFillObj.AddComponent<Image>();
            stageProgressFill.sprite = CartoonSpriteFactory.GetBarFill();
            stageProgressFill.color = new Color(1f, 0.82f, 0.25f, 1f);
            stageProgressFill.type = Image.Type.Filled;
            stageProgressFill.fillMethod = Image.FillMethod.Horizontal;
            stageProgressFill.fillAmount = 0f;

            stageBadgeText = CreateLabel("StageBadgeText", hudObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(240f, 20f), LocalizationManager.Get("stage_badge_0"), 12f, TextAlignmentOptions.Center, FontStyles.Bold);
            stageBadgeText.color = new Color(1f, 0.95f, 0.6f, 1f);
            SetOutline(stageBadgeText, 0.3f);

            // Pause button (top-right, in-game only: part of the HUD). Sound lives in Settings.
            Button pauseBtn = CreateRoundIconButton("PauseButton", hudObj.transform, new Vector2(-28f, -28f), CartoonSpriteFactory.GetPauseIcon(), 24f);
            pauseBtn.onClick.AddListener(() => { if (GameManager.Instance != null) GameManager.Instance.PauseGame(); });

            // --- TOAST UI ---
            GameObject toastObj = CreateUIRect("Toast", safeRoot, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(320f, 54f));
            CanvasGroup toastCg = toastObj.AddComponent<CanvasGroup>();
            TMP_Text toastText = CreateLabel("ToastLabel", toastObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "", 30f, TextAlignmentOptions.Center, FontStyles.Bold);
            SetOutline(toastText, 0.3f);
            ToastUI toastUI = toastObj.AddComponent<ToastUI>();
            // Wire ToastUI fields via reflection so SerializeFields are populated before first Show
            SetPrivateField(toastUI, "label", toastText);
            SetPrivateField(toastUI, "canvasGroup", toastCg);

            hud.Configure(distText, heartImg, healthFillImg, healthGhostImg, powerFillImg, powerBgObj, null, toastUI, turboSystem);

            // Coins picked up this run: a small frosted pill under the health bar. Kept narrow (x 10..74) so it never
            // reaches the power bar, which starts around x = 79 on narrow phones (canvas ~326 wide).
            GameObject hudCoinPill = CreateUIRect("CoinPill", hudObj.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -60f), new Vector2(64f, 26f));
            Image hudCoinBg = hudCoinPill.AddComponent<Image>();
            hudCoinBg.sprite = WatercolorSpriteFactory.GetRoundedRectSprite();
            hudCoinBg.type = Image.Type.Sliced;
            hudCoinBg.color = new Color(1f, 1f, 1f, 0.22f);
            hudCoinBg.raycastTarget = false;
            var hudCoinEdge = hudCoinPill.AddComponent<Outline>();
            hudCoinEdge.effectColor = new Color(1f, 1f, 1f, 0.35f);
            hudCoinEdge.effectDistance = new Vector2(1.5f, -1.5f);
            GameObject hudCoinObj = CreateUIRect("CoinIcon", hudCoinPill.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(20f, 20f));
            Image hudCoinImg = hudCoinObj.AddComponent<Image>();
            hudCoinImg.sprite = WatercolorSpriteFactory.GetCoinSprite();
            hudCoinImg.raycastTarget = false;
            TMP_Text hudCoinText = CreateLabel("CoinText", hudCoinPill.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(44f, 0f), new Vector2(36f, 24f), "0", 17f, TextAlignmentOptions.Left, FontStyles.Bold);
            hudCoinText.color = new Color(1f, 0.86f, 0.3f, 1f);
            hudCoinText.enableAutoSizing = true; // big runs (1000+) shrink to fit instead of spilling out
            hudCoinText.fontSizeMin = 10f;
            hudCoinText.fontSizeMax = 17f;
            SetOutline(hudCoinText, 0.3f);
            hud.SetCoinCounter(hudCoinText, (RectTransform)hudCoinPill.transform);
            hudObj.SetActive(false);

            // --- MAIN MENU OVERLAY ---
            // Full-screen dim over the river (edge to edge, under the safe area); it also swallows taps
            // so only the menu's buttons do anything. Shown and hidden together with the menu.
            GameObject menuDimObj = CreateUIRect("MainMenuDim", canvasObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            menuDimObj.transform.SetSiblingIndex(safeObj.transform.GetSiblingIndex());
            Image menuDim = menuDimObj.AddComponent<Image>();
            menuDim.sprite = whiteSpr;
            menuDim.color = new Color(0.02f, 0.10f, 0.30f, 0.16f); // light blue veil: the colorful river stays visible

            GameObject menuObj = CreateUIRect("MainMenu", safeRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            MainMenuScreen mainMenu = menuObj.AddComponent<MainMenuScreen>();

            // Logo-style title
            TMP_Text titleText = CreateLabel("Title", menuObj.transform, new Vector2(0.5f, 0.80f), new Vector2(0.5f, 0.80f), Vector2.zero, new Vector2(300f, 140f), "", 58f, TextAlignmentOptions.Center, FontStyles.Bold);
            // Each line ("BLUE'S" / "RIVER") stays whole: shrink to fit instead of wrapping mid-word.
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 30f;
            titleText.fontSizeMax = 58f;
            SetOutline(titleText, 0.35f);
            Localize(titleText, "title");

            // Mascot: the equipped fish swimming under the title
            GameObject mascotSlot = CreateUIRect("Mascot", menuObj.transform, new Vector2(0.5f, 0.605f), new Vector2(0.5f, 0.605f), Vector2.zero, new Vector2(100f, 130f));
            FishPreview mascot = FishPreview.Create(mascotSlot.transform, Vector2.zero, 128f, true);

            Sprite roundSpr = WatercolorSpriteFactory.GetRoundedRectSprite();
            Button playBtn = CreateTextButton("PlayButton", menuObj.transform, new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(250f, 72f), "play_btn", roundSpr, new Color(1f, 0.54f, 0.36f, 1f), 32f, new Color(0.17f, 0.05f, 0f, 1f));
            ShiftLabelRight(playBtn, 34f);
            AddButtonIcon(playBtn, CartoonSpriteFactory.GetPlayIcon(), new Vector2(-70f, 0f), new Vector2(34f, 34f));

            TMP_Text bestStartText = CreateLabel("BestStart", menuObj.transform, new Vector2(0.5f, 0.20f), new Vector2(0.5f, 0.20f), Vector2.zero, new Vector2(300f, 48f), "", 16f, TextAlignmentOptions.Center, FontStyles.Bold);
            bestStartText.color = Color.white;
            SetOutline(bestStartText, 0.28f);

            // How to play: four picture tips instead of a paragraph (kids read pictures first)
            BuildTipsPanel(menuObj.transform, roundSpr);

            mainMenu.Configure(titleText, playBtn, bestStartText, menuDimObj);
            mainMenu.SetMascot(mascot);

            // Coin balance (top-left)
            GameObject balanceObj = CreateUIRect("CoinBalance", menuObj.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -28f), new Vector2(108f, 34f));
            Image balanceBg = balanceObj.AddComponent<Image>();
            balanceBg.sprite = roundSpr;
            balanceBg.type = Image.Type.Sliced;
            balanceBg.color = new Color(0f, 0f, 0f, 0.28f);
            balanceBg.raycastTarget = false;
            GameObject balanceIcon = CreateUIRect("Icon", balanceObj.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(22f, 22f));
            Image balanceIconImg = balanceIcon.AddComponent<Image>();
            balanceIconImg.sprite = WatercolorSpriteFactory.GetCoinSprite();
            balanceIconImg.raycastTarget = false;
            TMP_Text balanceText = CreateLabel("Amount", balanceObj.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(68f, 0f), new Vector2(70f, 28f), "0", 17f, TextAlignmentOptions.Left, FontStyles.Bold);
            balanceText.color = new Color(1f, 0.84f, 0.35f, 1f);
            mainMenu.SetCoinBalance(balanceText);

            // --- GAME OVER OVERLAY ---
            GameObject overObj = CreateUIRect("GameOver", canvasObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image overDim = overObj.AddComponent<Image>();
            overDim.sprite = whiteSpr;
            overDim.color = new Color(0.02f, 0.08f, 0.25f, 0.55f);
            CanvasGroup overCg = overObj.AddComponent<CanvasGroup>();

            // Card: GameOverScreen shrinks it when the continue button isn't offered.
            GameObject cardObj = CreateUIRect("Card", overObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(300f, 366f));
            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = roundSpr;
            cardBg.type = Image.Type.Sliced;
            cardBg.color = PanelColor;
            AddPanelBorder(cardBg);

            // Ribbon title straddling the card's top edge
            GameObject ribbon = CreateUIRect("Ribbon", cardObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 2f), new Vector2(236f, 50f));
            Image ribbonImg = ribbon.AddComponent<Image>();
            ribbonImg.sprite = roundSpr;
            ribbonImg.type = Image.Type.Sliced;
            ribbonImg.color = new Color(1f, 0.54f, 0.36f, 1f);
            AddCandyShadow(ribbonImg);
            TMP_Text overHeader = CreateLabel("OverHeader", ribbon.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "", 21f, TextAlignmentOptions.Center, FontStyles.Bold);
            overHeader.color = Color.white;
            SetOutline(overHeader, 0.3f);
            Localize(overHeader, "game_over_title");

            // Distance
            TMP_Text distLabel = CreateLabel("DistanceLabel", cardObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(240f, 20f), "", 13f, TextAlignmentOptions.Center, FontStyles.Bold);
            distLabel.color = new Color(0.78f, 0.9f, 1f, 1f);
            Localize(distLabel, "distance_label");
            TMP_Text finalScoreText = CreateLabel("FinalScore", cardObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(260f, 70f), "0<size=45%>m</size>", 64f, TextAlignmentOptions.Center, FontStyles.Bold);
            SetOutline(finalScoreText, 0.25f);

            // "You now start as YOUNG!" (only shown when a run unlocks a bigger starting fish)
            TMP_Text unlockNote = CreateLabel("UnlockNote", cardObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -134f), new Vector2(280f, 22f), "", 13f, TextAlignmentOptions.Center, FontStyles.Bold);
            unlockNote.color = new Color(1f, 0.88f, 0.35f, 1f);
            SetOutline(unlockNote, 0.25f);

            // Stat pills: best score (star) | coins earned (coin)
            TMP_Text bestText = CreateStatPill("BestPill", cardObj.transform, new Vector2(-72f, -174f), CartoonSpriteFactory.GetStar());
            TMP_Text coinsEarned = CreateStatPill("CoinsPill", cardObj.transform, new Vector2(72f, -174f), WatercolorSpriteFactory.GetCoinSprite());
            coinsEarned.color = new Color(1f, 0.88f, 0.35f, 1f);

            // Next goal: "240 more coins for Redfin" / "You can buy Redfin!"
            TMP_Text goalText = CreateLabel("Goal", cardObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(276f, 22f), "", 13f, TextAlignmentOptions.Center, FontStyles.Bold);
            goalText.color = new Color(0.75f, 1f, 0.8f, 1f);
            goalText.enableAutoSizing = true;
            goalText.fontSizeMin = 9f;
            goalText.fontSizeMax = 13f;
            SetOutline(goalText, 0.25f);

            // "NEW BEST!" sticker hanging off the card's top-right corner
            GameObject badgeObj = CreateUIRect("NewBestBadge", cardObj.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -58f), new Vector2(108f, 32f));
            badgeObj.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
            Image badgeBg = badgeObj.AddComponent<Image>();
            badgeBg.sprite = roundSpr;
            badgeBg.type = Image.Type.Sliced;
            badgeBg.color = new Color(1f, 0.84f, 0.22f, 1f);
            AddCandyShadow(badgeBg);
            TMP_Text badgeLbl = CreateLabel("BadgeLabel", badgeObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "", 14f, TextAlignmentOptions.Center, FontStyles.Bold);
            badgeLbl.color = new Color(0.40f, 0.16f, 0f, 1f);
            Localize(badgeLbl, "new_best");

            // Continue with a rewarded ad (hidden when not offered)
            Button contBtn = CreateTextButton("ContinueBtn", cardObj.transform, new Vector2(0.5f, 0f), new Vector2(0f, 106f), new Vector2(264f, 46f), null, roundSpr, new Color(0.62f, 0.45f, 1f, 1f), 15f, Color.white);
            TMP_Text contLabel = contBtn.GetComponentInChildren<TMP_Text>();
            SetOutline(contLabel, 0.25f);
            ShiftLabelRight(contBtn, 26f);
            AddButtonIcon(contBtn, CartoonSpriteFactory.GetPlayIcon(), new Vector2(-108f, 0f), new Vector2(22f, 22f));

            // Bottom row: round home button + big Play Again
            Button menuBtn = CreateTextButton("MenuBtn", cardObj.transform, new Vector2(0.5f, 0f), new Vector2(-102f, 44f), new Vector2(60f, 56f), null, roundSpr, new Color(1f, 1f, 1f, 0.22f), 12f, Color.white);
            AddButtonIcon(menuBtn, CartoonSpriteFactory.GetHomeIcon(), Vector2.zero, new Vector2(34f, 34f));
            Button againBtn = CreateTextButton("PlayAgainBtn", cardObj.transform, new Vector2(0.5f, 0f), new Vector2(34f, 44f), new Vector2(196f, 56f), "play_again", roundSpr, new Color(1f, 0.54f, 0.36f, 1f), 19f, new Color(0.17f, 0.05f, 0f, 1f));
            ShiftLabelRight(againBtn, 26f);
            AddButtonIcon(againBtn, CartoonSpriteFactory.GetPlayIcon(), new Vector2(-70f, 0f), new Vector2(24f, 24f));

            GameOverScreen gameOverScreen = overObj.AddComponent<GameOverScreen>();
            gameOverScreen.Configure(finalScoreText, bestText, badgeObj, againBtn, contBtn, contLabel, unlockNote, overCg, scoreSystem, adsService);
            gameOverScreen.SetCoinsAndMenu(coinsEarned, menuBtn);
            gameOverScreen.SetGoalText(goalText);
            gameOverScreen.SetCard((RectTransform)cardObj.transform, 366f, 314f);
            overObj.SetActive(false);

            // --- PAUSE OVERLAY (app backgrounded / back button) ---
            GameObject pauseObj = CreateUIRect("PauseOverlay", canvasObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image pauseDim = pauseObj.AddComponent<Image>();
            pauseDim.sprite = whiteSpr;
            pauseDim.color = new Color(0.02f, 0.08f, 0.25f, 0.6f);
            // The dim swallows taps: only the card's buttons do anything.
            GameObject pauseCard = CreateUIRect("Card", pauseObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 292f));
            Image pauseCardBg = pauseCard.AddComponent<Image>();
            pauseCardBg.sprite = roundSpr;
            pauseCardBg.type = Image.Type.Sliced;
            pauseCardBg.color = PanelColor;
            AddPanelBorder(pauseCardBg);
            TMP_Text pauseLabel = CreateLabel("PauseLabel", pauseCard.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(250f, 40f), "", 26f, TextAlignmentOptions.Center, FontStyles.Bold);
            SetOutline(pauseLabel, 0.3f);
            Localize(pauseLabel, "paused");
            Button resumeBtn = CreateTextButton("ResumeBtn", pauseCard.transform, new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(230f, 56f), "resume_btn", roundSpr, new Color(0.36f, 0.86f, 0.45f, 1f), 22f, new Color(0.04f, 0.22f, 0.08f, 1f));
            Button pauseMenuBtn = CreateTextButton("PauseMenuBtn", pauseCard.transform, new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(230f, 46f), "menu_btn", roundSpr, new Color(1f, 1f, 1f, 0.18f), 17f, Color.white);
            pauseObj.AddComponent<PauseOverlay>().Configure(resumeBtn, pauseMenuBtn);

            // --- FISH SHOP (full screen, opened from the menu) ---
            FishShopScreen shop = BuildShopPanel(canvasObj.transform, whiteSpr);

            // --- SETTINGS PANEL (on top of everything; opened from menu and pause) ---
            SettingsScreen settings = BuildSettingsPanel(canvasObj.transform, whiteSpr);

            // Menu: big FISH (shop) button under Play; round settings gear top-right. Children of the menu: they hide with it.
            Button shopBtn = CreateTextButton("ShopButton", menuObj.transform, new Vector2(0.5f, 0.42f), new Vector2(0f, -90f), new Vector2(250f, 56f), "shop_btn", roundSpr, new Color(1f, 0.80f, 0.30f, 1f), 21f, new Color(0.17f, 0.05f, 0f, 1f));
            ShiftLabelRight(shopBtn, 30f);
            AddButtonIcon(shopBtn, FishSkinFactory.GetBody(FishCatalog.Get(FishCatalog.StarterId)), new Vector2(-72f, 0f), new Vector2(20f, 30f));
            shopBtn.onClick.AddListener(shop.Open);

            Button menuSettingsBtn = CreateRoundIconButton("SettingsButton", menuObj.transform, new Vector2(-28f, -28f), WatercolorSpriteFactory.GetGearSprite(), 26f);
            menuSettingsBtn.onClick.AddListener(settings.Open);

            Button pauseSettingsBtn = CreateTextButton("SettingsButton", pauseCard.transform, new Vector2(0.5f, 1f), new Vector2(0f, -178f), new Vector2(230f, 46f), "settings_title", roundSpr, new Color(1f, 1f, 1f, 0.18f), 17f, Color.white);
            AddButtonIcon(pauseSettingsBtn, WatercolorSpriteFactory.GetGearSprite(), new Vector2(-70f, 0f), new Vector2(20f, 20f));
            pauseSettingsBtn.onClick.AddListener(settings.Open);

            pauseObj.SetActive(false);

            return toastUI;
        }

        private SettingsScreen BuildSettingsPanel(Transform canvasRoot, Sprite whiteSpr)
        {
            Sprite roundSpr = WatercolorSpriteFactory.GetRoundedRectSprite();

            GameObject root = CreateUIRect("Settings", canvasRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image dim = root.AddComponent<Image>();
            dim.sprite = whiteSpr;
            dim.color = new Color(0.02f, 0.08f, 0.25f, 0.6f); // also swallows taps meant for screens below

            GameObject card = CreateUIRect("Card", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 330f));
            Image cardBg = card.AddComponent<Image>();
            cardBg.sprite = roundSpr;
            cardBg.type = Image.Type.Sliced;
            cardBg.color = PanelColor;
            AddPanelBorder(cardBg);

            TMP_Text title = CreateLabel("Title", card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(260f, 34f), "", 22f, TextAlignmentOptions.Center, FontStyles.Bold);
            Localize(title, "settings_title");
            GameObject accent = CreateUIRect("TitleAccent", card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(40f, 4f));
            Image accentImg = accent.AddComponent<Image>();
            accentImg.sprite = roundSpr;
            accentImg.type = Image.Type.Sliced;
            accentImg.color = new Color(1f, 0.54f, 0.36f, 1f);

            Button soundRow = CreateSettingsRow("SoundRow", card.transform, -100f, "sound_label", roundSpr, true);
            ToggleSwitch soundSwitch = CreateToggleSwitch(soundRow.transform, roundSpr);
            Button vibrationRow = CreateSettingsRow("VibrationRow", card.transform, -160f, "vib_label", roundSpr, true);
            ToggleSwitch vibrationSwitch = CreateToggleSwitch(vibrationRow.transform, roundSpr);

            // Language row: TR | EN segmented picker on the right.
            Button languageRow = CreateSettingsRow("LanguageRow", card.transform, -220f, "lang_label", roundSpr, false);
            GameObject picker = CreateUIRect("LangPicker", languageRow.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-64f, 0f), new Vector2(108f, 34f));
            Image pickerBg = picker.AddComponent<Image>();
            pickerBg.sprite = roundSpr;
            pickerBg.type = Image.Type.Sliced;
            pickerBg.color = new Color(0f, 0f, 0f, 0.22f);
            Button turkish = CreateTextButton("LangTR", picker.transform, new Vector2(0.5f, 0.5f), new Vector2(-26f, 0f), new Vector2(50f, 28f), null, roundSpr, Color.clear, 14f, Color.white);
            turkish.GetComponentInChildren<TMP_Text>().text = "TR";
            Button english = CreateTextButton("LangEN", picker.transform, new Vector2(0.5f, 0.5f), new Vector2(26f, 0f), new Vector2(50f, 28f), null, roundSpr, Color.clear, 14f, Color.white);
            english.GetComponentInChildren<TMP_Text>().text = "EN";

            Button close = CreateTextButton("Close", card.transform, new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(264f, 48f), "close_btn", roundSpr, new Color(1f, 0.54f, 0.36f, 1f), 18f, new Color(0.17f, 0.05f, 0f, 1f));

            SettingsScreen screen = root.AddComponent<SettingsScreen>();
            screen.Configure(soundRow, soundSwitch, vibrationRow, vibrationSwitch, turkish, english, close, card.GetComponent<RectTransform>());
            root.SetActive(false);
            return screen;
        }

        private static readonly Color PanelColor = new Color(0.17f, 0.32f, 0.78f, 1f);

        /// <summary>"Candy" 3D look: a darker copy of the shape just below it.</summary>
        private static void AddCandyShadow(Image img)
        {
            var shadow = img.gameObject.AddComponent<Shadow>();
            Color c = img.color;
            shadow.effectColor = c.a > 0.5f ? new Color(c.r * 0.6f, c.g * 0.55f, c.b * 0.6f, 1f) : new Color(0.02f, 0.06f, 0.2f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -4f);
        }

        /// <summary>Cartoon panel: thick light border plus a drop shadow.</summary>
        private static void AddPanelBorder(Image img)
        {
            var border = img.gameObject.AddComponent<Outline>();
            border.effectColor = new Color(0.75f, 0.88f, 1f, 1f);
            border.effectDistance = new Vector2(3f, -3f);
            var shadow = img.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.02f, 0.06f, 0.2f, 0.45f);
            shadow.effectDistance = new Vector2(0f, -8f);
        }

        private static readonly System.Collections.Generic.Dictionary<float, Material> OutlineMaterials =
            new System.Collections.Generic.Dictionary<float, Material>();

        /// <summary>
        /// Dark navy text outline: keeps white text readable on the bright water. Built from TMP's own
        /// "Outline" material so the shader's OUTLINE_ON variant ships in the Android build (a keyword set
        /// at runtime on the default material would be stripped and render without an outline).
        /// </summary>
        private static void SetOutline(TMP_Text label, float width)
        {
            if (!OutlineMaterials.TryGetValue(width, out Material mat) || mat == null)
            {
                Material source = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Outline");
                if (source == null) return;
                mat = new Material(source) { name = $"Outline {width}" };
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
                mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(18, 30, 84, 255));
                mat.SetFloat(ShaderUtilities.ID_FaceDilate, width * 0.5f); // keep letters as thick as without outline
                OutlineMaterials[width] = mat;
            }
            label.fontSharedMaterial = mat;
        }

        /// <summary>Rounded pill with an icon on the left and an auto-sized label (game over stats).</summary>
        private static TMP_Text CreateStatPill(string name, Transform parent, Vector2 pos, Sprite icon)
        {
            GameObject pill = CreateUIRect(name, parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, new Vector2(134f, 40f));
            Image bg = pill.AddComponent<Image>();
            bg.sprite = WatercolorSpriteFactory.GetRoundedRectSprite();
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.05f, 0.12f, 0.40f, 0.55f);
            bg.raycastTarget = false;
            GameObject iconObj = CreateUIRect("Icon", pill.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(26f, 26f));
            Image img = iconObj.AddComponent<Image>();
            img.sprite = icon;
            img.raycastTarget = false;
            TMP_Text label = CreateLabel("Label", pill.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(84f, 0f), new Vector2(92f, 30f), "", 15f, TextAlignmentOptions.Left, FontStyles.Bold);
            label.enableAutoSizing = true;
            label.fontSizeMin = 9f;
            label.fontSizeMax = 15f;
            return label;
        }

        /// <summary>
        /// "How to play" card near the bottom of the menu: a titled, see-through card with the four steps in order
        /// (arrows between them) that light up one by one. Deliberately unlike a tab bar: lifted off the bottom
        /// edge, lighter than the buttons, a heading on top and nothing that looks pressable.
        /// </summary>
        private static void BuildTipsPanel(Transform menu, Sprite roundSpr)
        {
            GameObject panel = CreateUIRect("Tips", menu, new Vector2(0.5f, 0.085f), new Vector2(0.5f, 0.085f), Vector2.zero, new Vector2(292f, 74f));
            Image bg = panel.AddComponent<Image>();
            bg.sprite = roundSpr;
            bg.type = Image.Type.Sliced;
            // See-through navy: dark enough for white words and colorful icons to read on the bright river.
            bg.color = new Color(0.04f, 0.12f, 0.36f, 0.62f);
            bg.raycastTarget = false;
            var border = panel.AddComponent<Outline>();
            border.effectColor = new Color(1f, 1f, 1f, 0.3f);
            border.effectDistance = new Vector2(1.5f, -1.5f);

            // Heading tab sitting on the card's top edge
            GameObject header = CreateUIRect("TipsHeader", panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(136f, 22f));
            Image headerBg = header.AddComponent<Image>();
            headerBg.sprite = roundSpr;
            headerBg.type = Image.Type.Sliced;
            headerBg.pixelsPerUnitMultiplier = 2.4f; // pill
            headerBg.color = new Color(0.07f, 0.16f, 0.45f, 0.92f);
            headerBg.raycastTarget = false;
            TMP_Text headerText = CreateLabel("TipsHeaderText", header.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "", 12f, TextAlignmentOptions.Center, FontStyles.Bold);
            headerText.color = new Color(1f, 0.88f, 0.4f, 1f);
            headerText.characterSpacing = 4f;
            Localize(headerText, "tips_title");

            (Sprite icon, Vector2 size, string key)[] tips =
            {
                (CartoonSpriteFactory.GetDragIcon(), new Vector2(28f, 28f), "tip_drag"),
                (CartoonSpriteFactory.GetShrimp(), new Vector2(30f, 30f), "tip_eat"),
                (CartoonSpriteFactory.GetPredatorBody(PredatorKind.Shark), new Vector2(23f, 30f), "tip_shark"),
                (WatercolorSpriteFactory.GetCoinSprite(), new Vector2(24f, 24f), "tip_coins"),
            };
            const float colW = 70f;
            var icons = new RectTransform[tips.Length];
            var labels = new TMP_Text[tips.Length];
            var glows = new Image[tips.Length];
            Sprite glowSpr = WatercolorSpriteFactory.GetCircleGlowSprite();
            Sprite arrow = CartoonSpriteFactory.GetPlayIcon();
            for (int i = 0; i < tips.Length; i++)
            {
                float x = (i - (tips.Length - 1) * 0.5f) * colW;
                // Warm glow behind the step whose turn it is (faded in by TipsAnimator)
                GameObject glowObj = CreateUIRect($"TipGlow_{i}", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 9f), new Vector2(54f, 54f));
                glows[i] = glowObj.AddComponent<Image>();
                glows[i].sprite = glowSpr;
                glows[i].color = new Color(1f, 0.82f, 0.3f, 0f);
                glows[i].raycastTarget = false;

                GameObject iconObj = CreateUIRect($"TipIcon_{i}", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 9f), tips[i].size);
                Image img = iconObj.AddComponent<Image>();
                img.sprite = tips[i].icon;
                img.preserveAspect = true;
                img.raycastTarget = false;
                icons[i] = (RectTransform)iconObj.transform;

                TMP_Text label = CreateLabel($"TipLabel_{i}", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, -21f), new Vector2(colW - 6f, 18f), "", 11f, TextAlignmentOptions.Center, FontStyles.Bold);
                label.enableAutoSizing = true;
                label.fontSizeMin = 8f;
                label.fontSizeMax = 11f;
                SetOutline(label, 0.25f);
                Localize(label, tips[i].key);
                labels[i] = label;

                // Small arrow to the next step: "do this, then this"
                if (i < tips.Length - 1)
                {
                    GameObject arrowObj = CreateUIRect($"TipArrow_{i}", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x + colW * 0.5f, 9f), new Vector2(8f, 8f));
                    Image arrowImg = arrowObj.AddComponent<Image>();
                    arrowImg.sprite = arrow;
                    arrowImg.preserveAspect = true;
                    arrowImg.color = new Color(1f, 0.85f, 0.4f, 0.9f);
                    arrowImg.raycastTarget = false;
                }
            }

            panel.AddComponent<TipsAnimator>().Configure(icons, labels, glows);
        }

        /// <summary>Round blue candy button with a white icon, anchored to the parent's top-right corner.</summary>
        private static Button CreateRoundIconButton(string name, Transform parent, Vector2 anchoredPos, Sprite icon, float iconSize)
        {
            GameObject go = CreateUIRect(name, parent, new Vector2(1f, 1f), new Vector2(1f, 1f), anchoredPos, new Vector2(44f, 44f));
            Image bg = go.AddComponent<Image>();
            bg.sprite = WatercolorSpriteFactory.GetCircleSprite();
            bg.color = new Color(0.17f, 0.32f, 0.78f, 0.92f);
            AddCandyShadow(bg);
            Button btn = go.AddComponent<Button>();
            GameObject iconObj = CreateUIRect("Icon", go.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(iconSize, iconSize));
            Image img = iconObj.AddComponent<Image>();
            img.sprite = icon;
            img.raycastTarget = false;
            return btn;
        }

        /// <summary>Moves a button's label right to make room for an icon on its left.</summary>
        private static void ShiftLabelRight(Button button, float amount)
        {
            var label = button.GetComponentInChildren<TMP_Text>().rectTransform;
            label.offsetMin = new Vector2(amount, label.offsetMin.y);
        }

        private FishShopScreen BuildShopPanel(Transform canvasRoot, Sprite whiteSpr)
        {
            Sprite roundSpr = WatercolorSpriteFactory.GetRoundedRectSprite();
            Sprite coinSpr = WatercolorSpriteFactory.GetCoinSprite();
            Color gold = new Color(1f, 0.86f, 0.3f, 1f);

            // Underwater backdrop: deep blue with drifting sunlight (scrolled by FishShopScreen)
            GameObject root = CreateUIRect("FishShop", canvasRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image bg = root.AddComponent<Image>();
            bg.sprite = whiteSpr;
            bg.color = new Color(0.10f, 0.24f, 0.62f, 1f); // swallows taps meant for the menu below
            GameObject lightObj = CreateUIRect("Sunlight", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RawImage sunlight = lightObj.AddComponent<RawImage>();
            MakeSubCanvas(lightObj, interactive: false); // drifts every frame
            sunlight.texture = CartoonSpriteFactory.GetCaustics().texture;
            sunlight.color = new Color(1f, 1f, 1f, 0.10f);
            sunlight.raycastTarget = false;

            GameObject content = CreateUIRect("Content", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            content.AddComponent<SafeAreaFitter>();
            Transform c = content.transform;

            // Header: round close (left) + coin balance (right)
            GameObject closeObj = CreateUIRect("ShopClose", c, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(44f, 44f));
            Image closeBg = closeObj.AddComponent<Image>();
            closeBg.sprite = WatercolorSpriteFactory.GetCircleSprite();
            closeBg.color = new Color(1f, 0.42f, 0.45f, 1f);
            AddCandyShadow(closeBg);
            Button close = closeObj.AddComponent<Button>();
            GameObject closeIcon = CreateUIRect("Icon", closeObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 24f));
            Image closeIconImg = closeIcon.AddComponent<Image>();
            closeIconImg.sprite = CartoonSpriteFactory.GetCloseIcon();
            closeIconImg.raycastTarget = false;

            GameObject balanceObj = CreateUIRect("Balance", c, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60f, -28f), new Vector2(104f, 36f));
            Image balanceBg = balanceObj.AddComponent<Image>();
            balanceBg.sprite = roundSpr;
            balanceBg.type = Image.Type.Sliced;
            balanceBg.color = new Color(0.04f, 0.10f, 0.35f, 0.7f);
            GameObject balanceIcon = CreateUIRect("Icon", balanceObj.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(19f, 0f), new Vector2(24f, 24f));
            balanceIcon.AddComponent<Image>().sprite = coinSpr;
            TMP_Text balanceText = CreateLabel("Amount", balanceObj.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(64f, 0f), new Vector2(68f, 28f), "0", 17f, TextAlignmentOptions.Left, FontStyles.Bold);
            balanceText.color = gold;
            SetOutline(balanceText, 0.25f);

            GameObject collectionObj = CreateUIRect("Collection", c, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-24f, -28f), new Vector2(96f, 36f));
            Image collectionBg = collectionObj.AddComponent<Image>();
            collectionBg.sprite = roundSpr;
            collectionBg.type = Image.Type.Sliced;
            collectionBg.color = new Color(0.04f, 0.10f, 0.35f, 0.7f);
            GameObject collectionIcon = CreateUIRect("Icon", collectionObj.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(15f, 22f));
            collectionIcon.AddComponent<Image>().sprite = FishSkinFactory.GetBody(FishCatalog.Get(FishCatalog.StarterId));
            TMP_Text collectionText = CreateLabel("Count", collectionObj.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(58f, 0f), new Vector2(64f, 28f), "", 16f, TextAlignmentOptions.Center, FontStyles.Bold);
            SetOutline(collectionText, 0.25f);

            // Display card: the selected fish swims in its own little aquarium
            GameObject detail = CreateUIRect("Detail", c, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(304f, 236f));
            Image detailBg = detail.AddComponent<Image>();
            detailBg.sprite = roundSpr;
            detailBg.type = Image.Type.Sliced;
            detailBg.color = PanelColor;
            AddPanelBorder(detailBg);

            GameObject ribbon = CreateUIRect("Ribbon", detail.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 4f), new Vector2(200f, 40f));
            Image ribbonImg = ribbon.AddComponent<Image>();
            ribbonImg.sprite = roundSpr;
            ribbonImg.type = Image.Type.Sliced;
            ribbonImg.color = new Color(1f, 0.54f, 0.36f, 1f);
            AddCandyShadow(ribbonImg);
            TMP_Text title = CreateLabel("Title", ribbon.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "", 17f, TextAlignmentOptions.Center, FontStyles.Bold);
            SetOutline(title, 0.3f);
            Localize(title, "shop_title");

            GameObject stageObj = CreateUIRect("Aquarium", detail.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(272f, 104f));
            Image stageImg = stageObj.AddComponent<Image>();
            stageImg.sprite = roundSpr;
            stageImg.type = Image.Type.Sliced;
            stageImg.color = new Color(0.45f, 0.82f, 1f, 0.35f);
            stageImg.raycastTarget = false;
            GameObject glowObj = CreateUIRect("Glow", stageObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170f, 120f));
            Image previewGlow = glowObj.AddComponent<Image>();
            previewGlow.sprite = WatercolorSpriteFactory.GetCircleGlowSprite();
            previewGlow.raycastTarget = false;
            FishPreview bigPreview = FishPreview.Create(stageObj.transform, Vector2.zero, 110f, true);
            MakeSubCanvas(bigPreview.gameObject, interactive: false); // swims every frame; the 30 cards stay batched
            bigPreview.transform.localRotation = Quaternion.Euler(0f, 0f, -90f); // swims sideways across the tank

            TMP_Text nameText = CreateLabel("Name", detail.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-40f, -142f), new Vector2(190f, 26f), "", 19f, TextAlignmentOptions.Left, FontStyles.Bold);
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 12f;
            nameText.fontSizeMax = 19f;
            SetOutline(nameText, 0.25f);

            GameObject rarityObj = CreateUIRect("RarityPill", detail.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(98f, -142f), new Vector2(92f, 24f));
            Image rarityPill = rarityObj.AddComponent<Image>();
            rarityPill.sprite = roundSpr;
            rarityPill.type = Image.Type.Sliced;
            rarityPill.raycastTarget = false;
            TMP_Text rarityText = CreateLabel("Rarity", rarityObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "", 12f, TextAlignmentOptions.Center, FontStyles.Bold);
            rarityText.color = Color.white;
            SetOutline(rarityText, 0.25f);

            TMP_Text perkText = CreateLabel("Perk", detail.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(276f, 26f), "", 12f, TextAlignmentOptions.Center, FontStyles.Bold);
            perkText.color = new Color(0.75f, 1f, 0.8f, 1f);
            perkText.enableAutoSizing = true;
            perkText.fontSizeMin = 8f;
            perkText.fontSizeMax = 12f;
            SetOutline(perkText, 0.2f);

            Button action = CreateTextButton("ShopAction", detail.transform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(272f, 46f), null, roundSpr, Color.white, 17f, Color.white);

            // Scrollable grid of all fish
            GameObject scrollObj = CreateUIRect("Grid", c, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform scrollRt = (RectTransform)scrollObj.transform;
            scrollRt.offsetMin = new Vector2(8f, 8f);
            scrollRt.offsetMax = new Vector2(-8f, -318f);
            scrollObj.AddComponent<RectMask2D>();
            Image scrollHit = scrollObj.AddComponent<Image>(); // lets drags between cards scroll too
            scrollHit.color = Color.clear;

            GameObject gridObj = new GameObject("Cards", typeof(RectTransform));
            gridObj.transform.SetParent(scrollObj.transform, false);
            RectTransform gridRt = (RectTransform)gridObj.transform;
            gridRt.anchorMin = new Vector2(0f, 1f);
            gridRt.anchorMax = new Vector2(1f, 1f);
            gridRt.pivot = new Vector2(0.5f, 1f);
            gridRt.sizeDelta = Vector2.zero;
            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(96f, 128f);
            grid.spacing = new Vector2(8f, 10f);
            grid.padding = new RectOffset(0, 0, 6, 16);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            gridObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.content = gridRt;
            scroll.viewport = scrollRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 20f;

            var cards = new FishShopScreen.Card[FishCatalog.Count];
            for (int i = 0; i < FishCatalog.Count; i++)
            {
                FishDef fish = FishCatalog.All[i];
                Color rarity = FishCatalog.RarityColor(fish.rarity);

                GameObject cardObj = new GameObject($"Fish_{fish.id}", typeof(RectTransform));
                cardObj.transform.SetParent(gridObj.transform, false);
                Image frame = cardObj.AddComponent<Image>();
                frame.sprite = roundSpr;
                frame.type = Image.Type.Sliced;
                frame.pixelsPerUnitMultiplier = 2f; // 12-unit corners
                AddCandyShadow(frame);
                Button btn = cardObj.AddComponent<Button>();

                // Inner panel tinted toward the rarity color
                GameObject inner = CreateUIRect("Inner", cardObj.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-8f, -8f));
                Image innerImg = inner.AddComponent<Image>();
                innerImg.sprite = roundSpr;
                innerImg.type = Image.Type.Sliced;
                innerImg.pixelsPerUnitMultiplier = 2.4f;
                innerImg.color = Color.Lerp(new Color(0.12f, 0.24f, 0.62f, 1f), rarity, 0.28f);
                innerImg.raycastTarget = false;

                TMP_Text cardName = CreateLabel("Name", inner.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -11f), new Vector2(82f, 16f), "", 10f, TextAlignmentOptions.Center, FontStyles.Bold);
                cardName.enableAutoSizing = true;
                cardName.fontSizeMin = 7f;
                cardName.fontSizeMax = 10f;
                SetOutline(cardName, 0.25f);
                FishPreview preview = FishPreview.Create(inner.transform, new Vector2(0f, 2f), 70f, false);
                preview.SetFish(fish);

                // Price tag / status chip at the bottom
                GameObject chipObj = CreateUIRect("Chip", inner.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 15f), new Vector2(78f, 22f));
                Image chip = chipObj.AddComponent<Image>();
                chip.sprite = roundSpr;
                chip.type = Image.Type.Sliced;
                chip.raycastTarget = false;
                GameObject coinIcon = CreateUIRect("Coin", chipObj.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(11f, 0f), new Vector2(15f, 15f));
                Image coinImg = coinIcon.AddComponent<Image>();
                coinImg.sprite = coinSpr;
                coinImg.raycastTarget = false;
                TMP_Text price = CreateLabel("Price", chipObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "", 12f, TextAlignmentOptions.Center, FontStyles.Bold);
                SetOutline(price, 0.25f);

                cards[i] = new FishShopScreen.Card { fish = fish, button = btn, frame = frame, preview = preview, priceText = price, coinIcon = coinIcon, nameText = cardName, chip = chip };
            }

            FishShopScreen screen = root.AddComponent<FishShopScreen>();
            screen.Configure(bigPreview, nameText, rarityText, perkText, action, balanceText, cards, close);
            screen.SetDecor(rarityPill, previewGlow, sunlight);
            screen.SetCollectionText(collectionText);
            root.SetActive(false);
            return screen;
        }

        /// <summary>A rounded settings row with its title on the left; the whole row is tappable when <paramref name="tappable"/>.</summary>
        private static Button CreateSettingsRow(string name, Transform card, float y, string labelKey, Sprite roundSpr, bool tappable)
        {
            GameObject row = CreateUIRect(name, card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(264f, 50f));
            Image bg = row.AddComponent<Image>();
            bg.sprite = roundSpr;
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 1.6f; // softer 15-unit corners for the smaller rows
            bg.color = new Color(1f, 1f, 1f, 0.14f);
            Button btn = row.AddComponent<Button>();
            if (!tappable) btn.transition = Selectable.Transition.None;

            TMP_Text label = CreateLabel("Label", row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(84f, 0f), new Vector2(140f, 30f), "", 17f, TextAlignmentOptions.Left, FontStyles.Bold);
            Localize(label, labelKey);
            return btn;
        }

        private static ToggleSwitch CreateToggleSwitch(Transform row, Sprite roundSpr)
        {
            GameObject trackObj = CreateUIRect("Switch", row, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-42f, 0f), new Vector2(52f, 30f));
            Image track = trackObj.AddComponent<Image>();
            track.sprite = roundSpr;
            track.type = Image.Type.Sliced; // 30-unit tall rect with 24-unit corners → pill
            track.raycastTarget = false;

            GameObject knobObj = CreateUIRect("Knob", trackObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 24f));
            Image knob = knobObj.AddComponent<Image>();
            knob.sprite = WatercolorSpriteFactory.GetCircleSprite();
            knob.raycastTarget = false;

            ToggleSwitch sw = trackObj.AddComponent<ToggleSwitch>();
            sw.Configure(track, knobObj.GetComponent<RectTransform>(), 11f);
            return sw;
        }

        private static void AddButtonIcon(Button button, Sprite icon, Vector2 offset, Vector2 size)
        {
            GameObject go = CreateUIRect("Icon", button.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), offset, size);
            Image img = go.AddComponent<Image>();
            img.sprite = icon;
            img.raycastTarget = false;
        }

        private static Button CreateTextButton(string name, Transform parent, Vector2 anchor, Vector2 anchoredPos, Vector2 size,
                                               string localizationKey, Sprite sprite, Color bgColor, float fontSize, Color textColor)
        {
            GameObject go = CreateUIRect(name, parent, anchor, anchor, anchoredPos, size);
            Image bg = go.AddComponent<Image>();
            bg.sprite = sprite;
            if (sprite != null && sprite.border != Vector4.zero) bg.type = Image.Type.Sliced;
            bg.color = bgColor;
            if (bgColor.a > 0.05f) AddCandyShadow(bg);
            Button btn = go.AddComponent<Button>();
            TMP_Text label = CreateLabel("Label", go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "", fontSize, TextAlignmentOptions.Center, FontStyles.Bold);
            label.color = textColor;
            if (!string.IsNullOrEmpty(localizationKey)) Localize(label, localizationKey);
            return btn;
        }

        private static void Localize(TMP_Text label, string key)
        {
            label.gameObject.AddComponent<LocalizedText>().Key = key;
        }

        /// <summary>
        /// Gives a constantly animated part of the UI its own canvas, so its changes don't make Unity rebuild the
        /// whole screen's UI every frame.
        /// </summary>
        private static void MakeSubCanvas(GameObject go, bool interactive)
        {
            Canvas parent = go.GetComponentInParent<Canvas>();
            Canvas sub = go.AddComponent<Canvas>();
            if (parent != null) sub.additionalShaderChannels = parent.additionalShaderChannels;
            if (interactive) go.AddComponent<GraphicRaycaster>();
        }

        private static GameObject CreateUIRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            return go;
        }

        private static TMP_Text CreateLabel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, string text, float fontSize, TextAlignmentOptions align, FontStyles style)
        {
            GameObject go = CreateUIRect(name, parent, anchorMin, anchorMax, anchoredPos, sizeDelta);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.fontStyle = style;
            tmp.color = new Color(0.96f, 0.98f, 1f, 1f);
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(target, value);
        }
    }
}
