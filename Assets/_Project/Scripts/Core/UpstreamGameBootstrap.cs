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
        private AudioManager audioManagerRef;
        private TMP_Text muteLabel;

        private void Awake()
        {
            if (initialized && FindObjectsByType<UpstreamGameBootstrap>(FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject);
                return;
            }
            initialized = true;
            Application.targetFrameRate = 60;

            BuildEntireGame();

            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            AudioManager.OnSettingsChanged += RefreshMuteLabel;
        }

        private void OnDestroy()
        {
            if (initialized) initialized = false;
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
            AudioManager.OnSettingsChanged -= RefreshMuteLabel;
        }

        private void HandleLanguageChanged()
        {
            shownStage = -1; // force the stage badge to re-render in the new language
            RefreshMuteLabel();
        }

        private void RefreshMuteLabel()
        {
            if (muteLabel == null) return;
            bool muted = audioManagerRef != null ? audioManagerRef.IsMuted : !SaveSystem.Load().settingsSoundOn;
            muteLabel.text = LocalizationManager.Get(muted ? "sound_short_off" : "sound_short_on");
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
            mainCam.backgroundColor = new Color(0.12f, 0.56f, 0.69f, 1f);
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

            FoodPickup foodPrefab = CreateEntityTemplate<FoodPickup>("FoodTemplate", templatesRoot, WatercolorSpriteFactory.GetShrimpSprite(), 5);
            GoldFoodPickup goldPrefab = CreateEntityTemplate<GoldFoodPickup>("GoldFoodTemplate", templatesRoot, WatercolorSpriteFactory.GetGoldFoodSprite(), 6);
            SharkEnemy sharkPrefab = CreateEntityTemplate<SharkEnemy>("SharkTemplate", templatesRoot, WatercolorSpriteFactory.GetSharkSprite(), 7);
            RockObstacle rockPrefab = CreateEntityTemplate<RockObstacle>("RockTemplate", templatesRoot, WatercolorSpriteFactory.GetRockSprite(), 4);
            LogObstacle logPrefab = CreateEntityTemplate<LogObstacle>("LogTemplate", templatesRoot, WatercolorSpriteFactory.GetLogSprite(), 4);
            HookTrap hookPrefab = CreateHookTemplate("HookTemplate", templatesRoot);
            CoinPickup coinPrefab = CreateEntityTemplate<CoinPickup>("CoinTemplate", templatesRoot, WatercolorSpriteFactory.GetCoinSprite(), 6);

            var foodPool = CreatePool("FoodPool", foodPrefab, 24, poolsRoot);
            var goldPool = CreatePool("GoldFoodPool", goldPrefab, 6, poolsRoot);
            var sharkPool = CreatePool("SharkPool", sharkPrefab, 8, poolsRoot);
            var rockPool = CreatePool("RockPool", rockPrefab, 10, poolsRoot);
            var logPool = CreatePool("LogPool", logPrefab, 8, poolsRoot);
            var hookPool = CreatePool("HookPool", hookPrefab, 8, poolsRoot);
            var coinPool = CreatePool("CoinPool", coinPrefab, 24, poolsRoot);

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
                coinPool);

            ToastUI toastUI = BuildUI(turboSystem, scoreSystem, audioManager, adsManager.Ads);

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

        private static T CreateEntityTemplate<T>(string name, Transform parent, Sprite sprite, int sortingOrder) where T : EntityBase
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            return go.AddComponent<T>();
        }

        private static HookTrap CreateHookTemplate(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WatercolorSpriteFactory.GetHookSprite();
            sr.sortingOrder = 6;

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = 1.6f;
            lr.endWidth = 1.6f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(1f, 1f, 1f, 0.72f);
            lr.endColor = new Color(1f, 1f, 1f, 0.72f);
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

        private ToastUI BuildUI(TurboSystem turboSystem, ScoreSystem scoreSystem, AudioManager audioManager, IAdsService adsService)
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
            Sprite heartSpr = WatercolorSpriteFactory.GetHeartSprite();

            // --- SAFE AREA (HUD, menu, buttons stay clear of notches; dim overlays stay full-screen) ---
            GameObject safeObj = CreateUIRect("SafeArea", canvasObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safeObj.AddComponent<SafeAreaFitter>();
            Transform safeRoot = safeObj.transform;

            // --- HUD ROOT ---
            GameObject hudObj = CreateUIRect("HUD", safeRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            HUDController hud = hudObj.AddComponent<HUDController>();

            // Hearts (Top-Left)
            Image[] heartImages = new Image[4];
            for (int i = 0; i < heartImages.Length; i++)
            {
                GameObject hObj = CreateUIRect($"Heart_{i}", hudObj.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f + i * 26f, -28f), new Vector2(23f, 23f));
                Image img = hObj.AddComponent<Image>();
                img.sprite = heartSpr;
                img.color = new Color(1f, 0.36f, 0.42f, 1f);
                heartImages[i] = img;
            }

            // Distance Text (Top-Center)
            TMP_Text distText = CreateLabel("DistanceText", hudObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(220f, 52f), "0<size=50%>m</size>", 42f, TextAlignmentOptions.Center, FontStyles.Bold);

            // Power Bar (Below Distance)
            GameObject powerBgObj = CreateUIRect("PowerBarBg", hudObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(168f, 13f));
            Image powerBgImg = powerBgObj.AddComponent<Image>();
            powerBgImg.sprite = whiteSpr;
            powerBgImg.color = new Color(0f, 0.12f, 0.18f, 0.55f);

            GameObject powerFillObj = CreateUIRect("PowerBarFill", powerBgObj.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-4f, -4f));
            Image powerFillImg = powerFillObj.AddComponent<Image>();
            powerFillImg.sprite = whiteSpr;
            powerFillImg.color = new Color(0.49f, 0.94f, 0.78f, 1f);
            powerFillImg.type = Image.Type.Filled;
            powerFillImg.fillMethod = Image.FillMethod.Horizontal;
            powerFillImg.fillAmount = 0f;

            // Growth Stage Indicator (Below Power Bar)
            GameObject stageBgObj = CreateUIRect("StageBarBg", hudObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(148f, 8f));
            Image stageBgImg = stageBgObj.AddComponent<Image>();
            stageBgImg.sprite = whiteSpr;
            stageBgImg.color = new Color(0f, 0.12f, 0.18f, 0.40f);

            GameObject stageFillObj = CreateUIRect("StageBarFill", stageBgObj.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-2f, -2f));
            stageProgressFill = stageFillObj.AddComponent<Image>();
            stageProgressFill.sprite = whiteSpr;
            stageProgressFill.color = new Color(1f, 0.84f, 0.35f, 0.9f);
            stageProgressFill.type = Image.Type.Filled;
            stageProgressFill.fillMethod = Image.FillMethod.Horizontal;
            stageProgressFill.fillAmount = 0f;

            stageBadgeText = CreateLabel("StageBadgeText", hudObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(240f, 20f), LocalizationManager.Get("stage_badge_0"), 12f, TextAlignmentOptions.Center, FontStyles.Bold);
            stageBadgeText.color = new Color(1f, 0.92f, 0.75f, 0.9f);

            // Mute Button (Top-Right)
            GameObject muteObj = CreateUIRect("MuteButton", safeRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(40f, 40f));
            Image muteBg = muteObj.AddComponent<Image>();
            muteBg.sprite = whiteSpr;
            muteBg.color = new Color(1f, 1f, 1f, 0.18f);
            Button muteBtn = muteObj.AddComponent<Button>();
            audioManagerRef = audioManager;
            muteLabel = CreateLabel("MuteLabel", muteObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "", 10f, TextAlignmentOptions.Center, FontStyles.Bold);
            RefreshMuteLabel();
            // Label updates through AudioManager.OnSettingsChanged (also when changed in Settings).
            muteBtn.onClick.AddListener(audioManager.ToggleMute);

            // --- TOAST UI ---
            GameObject toastObj = CreateUIRect("Toast", safeRoot, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(320f, 54f));
            CanvasGroup toastCg = toastObj.AddComponent<CanvasGroup>();
            TMP_Text toastText = CreateLabel("ToastLabel", toastObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "", 30f, TextAlignmentOptions.Center, FontStyles.Bold);
            ToastUI toastUI = toastObj.AddComponent<ToastUI>();
            // Wire ToastUI fields via reflection so SerializeFields are populated before first Show
            SetPrivateField(toastUI, "label", toastText);
            SetPrivateField(toastUI, "canvasGroup", toastCg);

            hud.Configure(distText, heartImages, powerFillImg, powerBgObj, null, toastUI, turboSystem);

            // Coins picked up this run (under the hearts)
            GameObject hudCoinObj = CreateUIRect("CoinIcon", hudObj.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -56f), new Vector2(17f, 17f));
            Image hudCoinImg = hudCoinObj.AddComponent<Image>();
            hudCoinImg.sprite = WatercolorSpriteFactory.GetCoinSprite();
            TMP_Text hudCoinText = CreateLabel("CoinText", hudObj.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(82f, -56f), new Vector2(100f, 22f), "0", 16f, TextAlignmentOptions.Left, FontStyles.Bold);
            hudCoinText.color = new Color(1f, 0.84f, 0.35f, 1f);
            hud.SetCoinCounter(hudCoinText);
            hudObj.SetActive(false);

            // --- MAIN MENU OVERLAY ---
            // Full-screen dim over the river (edge to edge, under the safe area); it also swallows taps
            // so only the menu's buttons do anything. Shown and hidden together with the menu.
            GameObject menuDimObj = CreateUIRect("MainMenuDim", canvasObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            menuDimObj.transform.SetSiblingIndex(safeObj.transform.GetSiblingIndex());
            Image menuDim = menuDimObj.AddComponent<Image>();
            menuDim.sprite = whiteSpr;
            menuDim.color = new Color(0f, 0.10f, 0.16f, 0.45f);

            GameObject menuObj = CreateUIRect("MainMenu", safeRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            MainMenuScreen mainMenu = menuObj.AddComponent<MainMenuScreen>();
            // Keep the sound toggle above the menu so it stays tappable.
            muteObj.transform.SetAsLastSibling();

            TMP_Text titleText = CreateLabel("Title", menuObj.transform, new Vector2(0.5f, 0.74f), new Vector2(0.5f, 0.74f), Vector2.zero, new Vector2(320f, 120f), "", 48f, TextAlignmentOptions.Center, FontStyles.Bold);
            Localize(titleText, "title");
            TMP_Text howText = CreateLabel("HowTo", menuObj.transform, new Vector2(0.5f, 0.57f), new Vector2(0.5f, 0.57f), Vector2.zero, new Vector2(300f, 80f), "", 15f, TextAlignmentOptions.Center, FontStyles.Normal);
            Localize(howText, "how_to");
            howText.color = new Color(0.95f, 0.98f, 1f, 0.85f);
            Sprite roundSpr = WatercolorSpriteFactory.GetRoundedRectSprite();
            Button playBtn = CreateTextButton("PlayButton", menuObj.transform, new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(240f, 64f), "play_btn", roundSpr, new Color(1f, 0.54f, 0.36f, 1f), 28f, new Color(0.17f, 0.05f, 0f, 1f));
            TMP_Text bestStartText = CreateLabel("BestStart", menuObj.transform, new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(300f, 48f), "", 15f, TextAlignmentOptions.Center, FontStyles.Normal);
            bestStartText.color = new Color(0.95f, 0.98f, 1f, 0.78f);

            mainMenu.Configure(titleText, howText, playBtn, bestStartText, menuDimObj);

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
            overDim.color = new Color(0f, 0.10f, 0.16f, 0.48f);
            CanvasGroup overCg = overObj.AddComponent<CanvasGroup>();

            GameObject cardObj = CreateUIRect("Card", overObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(310f, 410f));
            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = roundSpr;
            cardBg.type = Image.Type.Sliced;
            cardBg.color = new Color(0.03f, 0.16f, 0.23f, 0.92f);

            TMP_Text overHeader = CreateLabel("OverHeader", cardObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(260f, 28f), "", 16f, TextAlignmentOptions.Center, FontStyles.Bold);
            Localize(overHeader, "game_over_title");
            overHeader.color = new Color(0.95f, 0.98f, 1f, 0.76f);

            TMP_Text finalScoreText = CreateLabel("FinalScore", cardObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(260f, 74f), "0<size=45%>m</size>", 62f, TextAlignmentOptions.Center, FontStyles.Bold);

            GameObject badgeObj = CreateUIRect("NewBestBadge", cardObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -142f), new Vector2(130f, 26f));
            Image badgeBg = badgeObj.AddComponent<Image>();
            badgeBg.sprite = roundSpr;
            badgeBg.type = Image.Type.Sliced;
            badgeBg.color = new Color(1f, 0.54f, 0.36f, 1f);
            TMP_Text badgeLbl = CreateLabel("BadgeLabel", badgeObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "", 13f, TextAlignmentOptions.Center, FontStyles.Bold);
            badgeLbl.color = new Color(0.17f, 0.05f, 0f, 1f);
            Localize(badgeLbl, "new_best");

            TMP_Text bestText = CreateLabel("BestScore", cardObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -174f), new Vector2(240f, 26f), "", 15f, TextAlignmentOptions.Center, FontStyles.Normal);

            // "You now start as YOUNG!" (only shown when a run unlocks a bigger starting fish)
            TMP_Text unlockNote = CreateLabel("UnlockNote", cardObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(280f, 24f), "", 12f, TextAlignmentOptions.Center, FontStyles.Bold);
            unlockNote.color = new Color(1f, 0.84f, 0.35f, 1f);

            // "+42 coins" earned this run
            GameObject earnedIcon = CreateUIRect("CoinsIcon", cardObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-62f, -232f), new Vector2(22f, 22f));
            earnedIcon.AddComponent<Image>().sprite = WatercolorSpriteFactory.GetCoinSprite();
            TMP_Text coinsEarned = CreateLabel("CoinsEarned", cardObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(14f, -232f), new Vector2(140f, 28f), "", 20f, TextAlignmentOptions.Left, FontStyles.Bold);
            coinsEarned.color = new Color(1f, 0.84f, 0.35f, 1f);

            // Play Again Button
            GameObject againBtnObj = CreateUIRect("PlayAgainBtn", cardObj.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 148f), new Vector2(266f, 48f));
            Image againBg = againBtnObj.AddComponent<Image>();
            againBg.sprite = roundSpr;
            againBg.type = Image.Type.Sliced;
            againBg.color = new Color(1f, 0.54f, 0.36f, 1f);
            Button againBtn = againBtnObj.AddComponent<Button>();
            TMP_Text againLbl = CreateLabel("AgainLabel", againBtnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "", 18f, TextAlignmentOptions.Center, FontStyles.Bold);
            againLbl.color = new Color(0.17f, 0.05f, 0f, 1f);
            Localize(againLbl, "play_again");

            // Continue Button
            GameObject contBtnObj = CreateUIRect("ContinueBtn", cardObj.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 94f), new Vector2(266f, 44f));
            Image contBg = contBtnObj.AddComponent<Image>();
            contBg.sprite = roundSpr;
            contBg.type = Image.Type.Sliced;
            contBg.color = new Color(1f, 1f, 1f, 0.15f);
            Button contBtn = contBtnObj.AddComponent<Button>();
            TMP_Text contLabel = CreateLabel("ContLabel", contBtnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "", 16f, TextAlignmentOptions.Center, FontStyles.Bold);

            GameOverScreen gameOverScreen = overObj.AddComponent<GameOverScreen>();
            gameOverScreen.Configure(finalScoreText, bestText, badgeObj, againBtn, contBtn, contLabel, unlockNote, overCg, scoreSystem, adsService);
            Button menuBtn = CreateTextButton("MenuBtn", cardObj.transform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(266f, 40f), "menu_btn", roundSpr, new Color(1f, 1f, 1f, 0.08f), 15f, new Color(1f, 1f, 1f, 0.85f));
            gameOverScreen.SetCoinsAndMenu(coinsEarned, menuBtn);
            overObj.SetActive(false);

            // --- PAUSE OVERLAY (app backgrounded / back button) ---
            GameObject pauseObj = CreateUIRect("PauseOverlay", canvasObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image pauseDim = pauseObj.AddComponent<Image>();
            pauseDim.sprite = whiteSpr;
            pauseDim.color = new Color(0f, 0.10f, 0.16f, 0.55f);
            pauseObj.AddComponent<Button>().transition = Selectable.Transition.None;
            TMP_Text pauseLabel = CreateLabel("PauseLabel", pauseObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 120f), "", 34f, TextAlignmentOptions.Center, FontStyles.Bold);
            Localize(pauseLabel, "paused");
            pauseObj.AddComponent<PauseOverlay>();

            // --- FISH SHOP (full screen, opened from the menu) ---
            FishShopScreen shop = BuildShopPanel(canvasObj.transform, whiteSpr);

            // --- SETTINGS PANEL (on top of everything; opened from menu and pause) ---
            SettingsScreen settings = BuildSettingsPanel(canvasObj.transform, whiteSpr);

            // Menu buttons under Play: FISH (shop) and SETTINGS. Children of the menu: they hide with it.
            Button shopBtn = CreateTextButton("ShopButton", menuObj.transform, new Vector2(0.5f, 0.42f), new Vector2(-62f, -76f), new Vector2(116f, 50f), "shop_btn", roundSpr, new Color(1f, 0.80f, 0.30f, 1f), 15f, new Color(0.17f, 0.05f, 0f, 1f));
            ShiftLabelRight(shopBtn, 22f);
            AddButtonIcon(shopBtn, FishSkinFactory.GetBody(FishCatalog.Get(FishCatalog.StarterId)), new Vector2(-38f, 0f), new Vector2(14f, 21f));
            shopBtn.onClick.AddListener(shop.Open);

            Button menuSettingsBtn = CreateTextButton("SettingsButton", menuObj.transform, new Vector2(0.5f, 0.42f), new Vector2(62f, -76f), new Vector2(116f, 50f), "settings_title", roundSpr, new Color(1f, 1f, 1f, 0.16f), 15f, Color.white);
            ShiftLabelRight(menuSettingsBtn, 22f);
            AddButtonIcon(menuSettingsBtn, WatercolorSpriteFactory.GetGearSprite(), new Vector2(-38f, 0f), new Vector2(18f, 18f));
            menuSettingsBtn.onClick.AddListener(settings.Open);

            Button pauseSettingsBtn = CreateTextButton("SettingsButton", pauseObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(200f, 46f), "settings_title", roundSpr, new Color(1f, 1f, 1f, 0.18f), 16f, Color.white);
            AddButtonIcon(pauseSettingsBtn, WatercolorSpriteFactory.GetGearSprite(), new Vector2(-62f, 0f), new Vector2(20f, 20f));
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
            dim.color = new Color(0f, 0.06f, 0.10f, 0.72f); // also swallows taps meant for screens below

            GameObject card = CreateUIRect("Card", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 330f));
            Image cardBg = card.AddComponent<Image>();
            cardBg.sprite = roundSpr;
            cardBg.type = Image.Type.Sliced;
            cardBg.color = new Color(0.04f, 0.18f, 0.26f, 0.97f);

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
            Color cardColor = new Color(0.05f, 0.20f, 0.29f, 1f);
            Color gold = new Color(1f, 0.84f, 0.35f, 1f);

            GameObject root = CreateUIRect("FishShop", canvasRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image bg = root.AddComponent<Image>();
            bg.sprite = whiteSpr;
            bg.color = new Color(0.02f, 0.11f, 0.17f, 0.97f); // opaque enough to read; swallows taps

            GameObject content = CreateUIRect("Content", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            content.AddComponent<SafeAreaFitter>();
            Transform c = content.transform;

            // Header: title (left) + coin balance (right)
            TMP_Text title = CreateLabel("Title", c, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(116f, -30f), new Vector2(200f, 30f), "", 20f, TextAlignmentOptions.Left, FontStyles.Bold);
            Localize(title, "shop_title");

            GameObject balanceObj = CreateUIRect("Balance", c, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-62f, -30f), new Vector2(104f, 32f));
            Image balanceBg = balanceObj.AddComponent<Image>();
            balanceBg.sprite = roundSpr;
            balanceBg.type = Image.Type.Sliced;
            balanceBg.color = new Color(0f, 0f, 0f, 0.3f);
            GameObject balanceIcon = CreateUIRect("Icon", balanceObj.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(20f, 20f));
            balanceIcon.AddComponent<Image>().sprite = coinSpr;
            TMP_Text balanceText = CreateLabel("Amount", balanceObj.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(64f, 0f), new Vector2(68f, 26f), "0", 16f, TextAlignmentOptions.Left, FontStyles.Bold);
            balanceText.color = gold;

            // Detail card: big animated preview on the left, info + action on the right
            GameObject detail = CreateUIRect("Detail", c, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(308f, 170f));
            Image detailBg = detail.AddComponent<Image>();
            detailBg.sprite = roundSpr;
            detailBg.type = Image.Type.Sliced;
            detailBg.color = cardColor;

            FishPreview bigPreview = FishPreview.Create(detail.transform, new Vector2(-82f, 0f), 150f, true);
            TMP_Text nameText = CreateLabel("Name", detail.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(62f, 50f), new Vector2(172f, 28f), "", 18f, TextAlignmentOptions.Center, FontStyles.Bold);
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 12f;
            nameText.fontSizeMax = 18f;
            TMP_Text rarityText = CreateLabel("Rarity", detail.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(62f, 26f), new Vector2(172f, 20f), "", 13f, TextAlignmentOptions.Center, FontStyles.Bold);
            TMP_Text perkText = CreateLabel("Perk", detail.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(62f, -4f), new Vector2(172f, 36f), "", 13f, TextAlignmentOptions.Center, FontStyles.Normal);
            perkText.color = new Color(1f, 1f, 1f, 0.82f);
            Button action = CreateTextButton("ShopAction", detail.transform, new Vector2(0.5f, 0.5f), new Vector2(62f, -54f), new Vector2(172f, 44f), null, roundSpr, Color.white, 15f, Color.white);

            // Scrollable grid of all fish
            GameObject scrollObj = CreateUIRect("Grid", c, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform scrollRt = (RectTransform)scrollObj.transform;
            scrollRt.offsetMin = new Vector2(8f, 74f);
            scrollRt.offsetMax = new Vector2(-8f, -246f);
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
            grid.cellSize = new Vector2(96f, 124f);
            grid.spacing = new Vector2(8f, 8f);
            grid.padding = new RectOffset(0, 0, 4, 12);
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
                GameObject cardObj = new GameObject($"Fish_{fish.id}", typeof(RectTransform));
                cardObj.transform.SetParent(gridObj.transform, false);
                Image frame = cardObj.AddComponent<Image>();
                frame.sprite = roundSpr;
                frame.type = Image.Type.Sliced;
                frame.pixelsPerUnitMultiplier = 2f; // 12-unit corners
                Button btn = cardObj.AddComponent<Button>();

                GameObject inner = CreateUIRect("Inner", cardObj.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-6f, -6f));
                Image innerImg = inner.AddComponent<Image>();
                innerImg.sprite = roundSpr;
                innerImg.type = Image.Type.Sliced;
                innerImg.pixelsPerUnitMultiplier = 2.4f;
                innerImg.color = cardColor;
                innerImg.raycastTarget = false;

                TMP_Text cardName = CreateLabel("Name", inner.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -11f), new Vector2(84f, 16f), "", 10f, TextAlignmentOptions.Center, FontStyles.Bold);
                cardName.enableAutoSizing = true;
                cardName.fontSizeMin = 7f;
                cardName.fontSizeMax = 10f;
                FishPreview preview = FishPreview.Create(inner.transform, new Vector2(0f, 1f), 72f, false);
                preview.SetFish(fish);

                GameObject coinIcon = CreateUIRect("Coin", inner.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-24f, 13f), new Vector2(14f, 14f));
                Image coinImg = coinIcon.AddComponent<Image>();
                coinImg.sprite = coinSpr;
                coinImg.raycastTarget = false;
                TMP_Text price = CreateLabel("Price", inner.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(6f, 13f), new Vector2(76f, 18f), "", 13f, TextAlignmentOptions.Center, FontStyles.Bold);

                cards[i] = new FishShopScreen.Card { fish = fish, button = btn, frame = frame, preview = preview, priceText = price, coinIcon = coinIcon, nameText = cardName };
            }

            Button close = CreateTextButton("ShopClose", c, new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(308f, 48f), "close_btn", roundSpr, new Color(1f, 0.54f, 0.36f, 1f), 18f, new Color(0.17f, 0.05f, 0f, 1f));

            FishShopScreen screen = root.AddComponent<FishShopScreen>();
            screen.Configure(bigPreview, nameText, rarityText, perkText, action, balanceText, cards, close);
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
            bg.color = new Color(1f, 1f, 1f, 0.08f);
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
