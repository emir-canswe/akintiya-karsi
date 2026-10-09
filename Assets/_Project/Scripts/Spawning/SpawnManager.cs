using System.Collections.Generic;
using UnityEngine;
using Upstream.Camera;
using Upstream.Core;
using Upstream.Data;
using Upstream.Entities;
using Upstream.Mechanics;
using Upstream.River;

namespace Upstream.Spawning
{
    /// <summary>
    /// Manages the spawning and lifecycle of all river entities using Object Pools.
    /// Every pattern guarantees a passable safe gap marked by food.
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        [SerializeField] private SpawnConfig spawnConfig;
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private RiverController riverController;
        [SerializeField] private GrowthSystem growthSystem;
        [SerializeField] private DifficultyScaler difficultyScaler;

        // Object pools (plain C# objects, wired by the bootstrap via Configure)
        private ObjectPool<FoodPickup> foodPool;
        private ObjectPool<GoldFoodPickup> goldFoodPool;
        private ObjectPool<SharkEnemy> sharkPool;
        private ObjectPool<RockObstacle> rockPool;
        private ObjectPool<LogObstacle> logPool;
        private ObjectPool<HookTrap> hookPool;
        private ObjectPool<CoinPickup> coinPool;
        private ObjectPool<WaveObstacle> wavePool;

        private float nextSpawnDistance = 120f;
        private readonly List<EntityBase> activeEntities = new List<EntityBase>(64);

        public IReadOnlyList<EntityBase> ActiveEntities => activeEntities;

        public void Configure(
            SpawnConfig sConfig,
            GameConfig gConfig,
            Transform player,
            RiverController river,
            GrowthSystem growth,
            ObjectPool<FoodPickup> foods,
            ObjectPool<GoldFoodPickup> goldFoods,
            ObjectPool<SharkEnemy> sharks,
            ObjectPool<RockObstacle> rocks,
            ObjectPool<LogObstacle> logs,
            ObjectPool<HookTrap> hooks,
            DifficultyScaler difficulty = null,
            ObjectPool<CoinPickup> coins = null,
            ObjectPool<WaveObstacle> waves = null)
        {
            spawnConfig = sConfig;
            gameConfig = gConfig;
            playerTransform = player;
            riverController = river;
            growthSystem = growth;
            foodPool = foods;
            goldFoodPool = goldFoods;
            sharkPool = sharks;
            rockPool = rocks;
            logPool = logs;
            hookPool = hooks;
            difficultyScaler = difficulty;
            coinPool = coins;
            wavePool = waves;
        }

        private void OnEnable()
        {
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnContinueUsed += HandleContinueUsed;
            GameEvents.OnReturnToMenu += ClearAllEntities;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnContinueUsed -= HandleContinueUsed;
            GameEvents.OnReturnToMenu -= ClearAllEntities;
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;
            GameState state = GameManager.Instance.CurrentState;
            if (state != GameState.Playing && state != GameState.Menu && state != GameState.Dying)
                return;

            float flowSpeed = riverController != null ? riverController.FlowSpeed : 150f;
            int stage = growthSystem != null ? growthSystem.CurrentStage : 0;

            if (state == GameState.Playing)
            {
                Tick(Time.deltaTime, GameManager.Instance.CurrentDistance, stage, flowSpeed);
            }
            else
            {
                // Scroll existing entities during menu/dying
                for (int i = activeEntities.Count - 1; i >= 0; i--)
                {
                    var entity = activeEntities[i];
                    entity.Tick(Time.deltaTime, flowSpeed);
                    if (!entity.isActive)
                    {
                        ReturnToPool(entity);
                        activeEntities.RemoveAt(i);
                    }
                }
            }
        }

        private void HandleGameStart()
        {
            ClearAllEntities();
            nextSpawnDistance = 90f;
        }

        private void HandleContinueUsed()
        {
            // Clear nearby hazards on continue so player isn't immediately hit
            for (int i = activeEntities.Count - 1; i >= 0; i--)
            {
                var e = activeEntities[i];
                if (e.Type != EntityType.Food && e.Type != EntityType.GoldFood && e.Type != EntityType.Coin && e.yPosition < 180f)
                {
                    ReturnToPool(e);
                    activeEntities.RemoveAt(i);
                }
            }
        }

        public void ClearAllEntities()
        {
            for (int i = activeEntities.Count - 1; i >= 0; i--)
            {
                ReturnToPool(activeEntities[i]);
            }
            activeEntities.Clear();
        }

        public void Tick(float dt, float currentDistance, int currentStage, float flowSpeed)
        {
            if (currentDistance >= nextSpawnDistance)
            {
                float diff = difficultyScaler != null
                    ? difficultyScaler.GetDifficulty(currentDistance)
                    : DifficultyScaler.CalculateDifficulty(currentDistance, DifficultyScaler.DefaultRampDistance);
                SpawnPattern(currentStage, diff, Meters(currentDistance));

                // Spaced in time, not distance: as the river speeds up, patterns get further apart in meters
                // so the player always has the same moment to react.
                float easy = spawnConfig != null ? spawnConfig.spawnGapSecondsEasy : 1.7f;
                float hard = spawnConfig != null ? spawnConfig.spawnGapSecondsHard : 1.0f;
                float gap = SpawnGapSeconds(diff, easy, hard) * Random.Range(0.85f, 1.15f);
                nextSpawnDistance = currentDistance + Mathf.Max(85f, gap * flowSpeed);
            }

            for (int i = activeEntities.Count - 1; i >= 0; i--)
            {
                var entity = activeEntities[i];
                entity.Tick(dt, flowSpeed);

                if (!entity.isActive)
                {
                    ReturnToPool(entity);
                    activeEntities.RemoveAt(i);
                }
            }
        }

        public enum PatternType { FoodLine, Rocks, Log, Shark, Hook }

        // ---- Difficulty curve (pure, unit-tested) ----

        /// <summary>Seconds between two spawn patterns: <paramref name="easy"/> at difficulty 0 down to <paramref name="hard"/> at 1.</summary>
        public static float SpawnGapSeconds(float difficulty, float easy = 1.7f, float hard = 1.0f) =>
            Mathf.Lerp(easy, hard, Mathf.Clamp01(difficulty));

        /// <summary>Meters at which each predator first shows up: the line-up changes the further you swim.</summary>
        public const float PiranhaFromMeters = 250f;
        public const float PikeFromMeters = 600f;
        public const float CatfishFromMeters = 1000f;
        /// <summary>Piranhas come in pairs, then in threes from here on.</summary>
        public const float PiranhaTrioFromMeters = 800f;

        /// <summary>Most predators allowed on screen at once: 2 at the start, one more every 600 m, at most 5.</summary>
        public static int MaxPredatorsFor(float meters) => Mathf.Min(5, 2 + Mathf.FloorToInt(Mathf.Max(0f, meters) / 600f));

        public static int PiranhaSchoolSize(float meters) => meters >= PiranhaTrioFromMeters ? 3 : 2;

        /// <summary>
        /// Which predator shows up at this distance. <paramref name="r"/> is uniform in [0,1).
        /// Only sharks at first; piranhas, then pikes, then catfish join, and sharks get rarer once pikes arrive.
        /// </summary>
        public static PredatorKind PickPredator(float meters, float r)
        {
            float shark = meters >= PikeFromMeters ? 0.6f : 1f;
            float piranha = meters >= PiranhaFromMeters ? 0.8f : 0f;
            float pike = meters >= PikeFromMeters ? 0.8f : 0f;
            float catfish = meters >= CatfishFromMeters ? 0.6f : 0f;
            float t = Mathf.Clamp01(r) * (shark + piranha + pike + catfish);
            if ((t -= shark) < 0f) return PredatorKind.Shark;
            if ((t -= piranha) < 0f) return PredatorKind.Piranha;
            if ((t -= pike) < 0f) return PredatorKind.Pike;
            return catfish > 0f ? PredatorKind.Catfish : PredatorKind.Shark;
        }

        private float Meters(float distance)
        {
            float divisor = gameConfig != null && gameConfig.distanceToMetersDivisor > 0f ? gameConfig.distanceToMetersDivisor : 10f;
            return distance / divisor;
        }

        /// <summary>Predators currently swimming (not knocked away).</summary>
        private int ActivePredators()
        {
            int n = 0;
            for (int i = 0; i < activeEntities.Count; i++)
            {
                var e = activeEntities[i];
                if (e.Type == EntityType.Shark && e.isActive && !e.isFlying) n++;
            }
            return n;
        }

        // Scratch buffer for weights: avoids allocating every spawn.
        private readonly float[] weightBuffer = new float[5];

        /// <summary>
        /// Pure weighted pick. <paramref name="r"/> is a uniform random value in [0,1).
        /// Weights are relative and don't need to sum to 1. Falls back to FoodLine if all are zero.
        /// </summary>
        public static PatternType PickPattern(float r, float[] weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Length; i++) total += Mathf.Max(0f, weights[i]);
            if (total <= 0f) return PatternType.FoodLine;

            float target = Mathf.Clamp01(r) * total;
            float acc = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                float w = Mathf.Max(0f, weights[i]);
                if (w <= 0f) continue;
                acc += w;
                if (target < acc) return (PatternType)i;
            }

            // r == 1 edge case: return the last pattern with a positive weight.
            for (int i = weights.Length - 1; i >= 0; i--)
            {
                if (weights[i] > 0f) return (PatternType)i;
            }
            return PatternType.FoodLine;
        }

        private void FillWeights(float difficulty)
        {
            var c = spawnConfig;
            if (c == null)
            {
                weightBuffer[0] = 0.30f; weightBuffer[1] = 0.20f; weightBuffer[2] = 0.16f;
                weightBuffer[3] = Mathf.Lerp(0.18f, 0.24f, difficulty);
                weightBuffer[4] = Mathf.Lerp(0.16f, 0.10f, difficulty);
                return;
            }
            weightBuffer[0] = Mathf.Lerp(c.diff0FoodLineWeight, c.diff1FoodLineWeight, difficulty);
            weightBuffer[1] = Mathf.Lerp(c.diff0RockWeight, c.diff1RockWeight, difficulty);
            weightBuffer[2] = Mathf.Lerp(c.diff0LogWeight, c.diff1LogWeight, difficulty);
            weightBuffer[3] = Mathf.Lerp(c.diff0SharkWeight, c.diff1SharkWeight, difficulty);
            weightBuffer[4] = Mathf.Lerp(c.diff0HookWeight, c.diff1HookWeight, difficulty);
        }

        private void SpawnPattern(int stage, float difficulty, float meters)
        {
            if (foodPool == null) return;

            float rw = gameConfig != null ? gameConfig.riverHalfWidth : 150f;
            // Spawn just above the visible area (taller phones get a taller view).
            float startY = GameCamera.ViewHalfHeight + 40f;
            float side = Random.value < 0.5f ? -1f : 1f;

            // Now and then a wave rolls down the river instead of the usual pattern.
            if (wavePool != null && Random.value < Mathf.Lerp(0.11f, 0.18f, difficulty))
            {
                SpawnWave(rw, startY, side);
                return;
            }

            FillWeights(difficulty);
            PatternType pattern = PickPattern(Random.value, weightBuffer);

            // Missing pool -> degrade to a food line instead of silently spawning nothing.
            if ((pattern == PatternType.Rocks && rockPool == null) ||
                (pattern == PatternType.Log && logPool == null) ||
                (pattern == PatternType.Shark && sharkPool == null) ||
                (pattern == PatternType.Hook && hookPool == null))
            {
                pattern = PatternType.FoodLine;
            }

            // Never crowd the river: too many predators already swimming -> a breather (food) instead.
            PredatorKind kind = PredatorKind.Shark;
            if (pattern == PatternType.Shark)
            {
                kind = PickPredator(meters, Random.value);
                int count = kind == PredatorKind.Piranha ? PiranhaSchoolSize(meters) : 1;
                if (ActivePredators() + count > MaxPredatorsFor(meters)) pattern = PatternType.FoodLine;
            }

            switch (pattern)
            {
                case PatternType.FoodLine: // Curved Food Line (+ rare Gold Food)
                {
                    float x = Random.Range(-rw + 32f, rw - 32f);
                    int minN = spawnConfig != null ? spawnConfig.foodPerLineMin : 3;
                    int maxN = spawnConfig != null ? spawnConfig.foodPerLineMax : 6;
                    int n = Random.Range(minN, Mathf.Max(minN, maxN) + 1);
                    for (int i = 0; i < n; i++)
                    {
                        var food = foodPool.Get();
                        food.OnSpawn(new Vector2(x + Mathf.Sin(i * 0.9f) * 18f, startY + i * 34f));
                        activeEntities.Add(food);
                    }

                    float goldBase = spawnConfig != null ? spawnConfig.goldFoodBaseChance : 0.07f;
                    float goldInc = spawnConfig != null ? spawnConfig.goldFoodChanceIncrease : 0.05f;
                    float luck = FishShop.Active.LuckMultiplier;
                    if (goldFoodPool != null && Random.value < (goldBase + difficulty * goldInc) * luck)
                    {
                        var gold = goldFoodPool.Get();
                        gold.OnSpawn(new Vector2(Random.Range(-rw + 32f, rw - 32f), startY + n * 34f + 40f));
                        activeEntities.Add(gold);
                    }

                    // Coin arc in the other half of the river: grab the food or the coins, not both.
                    if (Random.value < Mathf.Min(0.9f, 0.45f * luck))
                    {
                        float coinX = x > 0f ? Random.Range(-rw + 34f, -20f) : Random.Range(20f, rw - 34f);
                        SpawnCoinLine(coinX, startY + 10f, Random.Range(4, 7), 30f, 14f);
                    }
                    break;
                }

                case PatternType.Rocks: // Rocks with food on the opposite side
                {
                    float x = side * Random.Range(30f, rw - 32f);
                    var rock1 = rockPool.Get();
                    rock1.OnSpawn(new Vector2(x, startY));
                    activeEntities.Add(rock1);

                    if (Random.value < 0.6f)
                    {
                        var rock2 = rockPool.Get();
                        float x2 = Mathf.Clamp(x - side * Random.Range(50f, 80f), -rw + 28f, rw - 28f);
                        rock2.OnSpawn(new Vector2(x2, startY + 65f));
                        activeEntities.Add(rock2);
                    }

                    var food = foodPool.Get();
                    food.OnSpawn(new Vector2(-side * Random.Range(42f, rw - 40f), startY + 25f));
                    activeEntities.Add(food);

                    // Risky coins hugging the rock line.
                    if (Random.value < 0.5f)
                    {
                        float coinX = Mathf.Clamp(x - side * 36f, -rw + 30f, rw - 30f);
                        SpawnCoinLine(coinX, startY + 110f, 2, 28f, 0f);
                    }
                    break;
                }

                case PatternType.Log: // Bank Log with safe passage on opposite side
                {
                    float minRatio = spawnConfig != null ? spawnConfig.minLogWidthRatio : 0.42f;
                    float maxRatio = spawnConfig != null ? spawnConfig.maxLogWidthRatio : 0.58f;
                    float ratioInc = spawnConfig != null ? spawnConfig.logWidthIncreasePerDifficulty : 0.1f;
                    float ratio = Random.Range(minRatio, maxRatio + difficulty * ratioInc);

                    // Fairness guard: always leave a gap the current (possibly huge) fish fits through.
                    float baseRadius = gameConfig != null ? gameConfig.playerRadius : 14f;
                    float pRadius = baseRadius * (growthSystem != null ? growthSystem.HitboxMultiplier : 1f);
                    float minGap = pRadius * 2f + 36f;
                    float maxLen = Mathf.Max(0f, rw * 2f + 10f - minGap);
                    float len = Mathf.Min(rw * 2f * ratio, maxLen);

                    float x0 = side < 0f ? -rw - 10f : rw + 10f - len;
                    var log = logPool.Get();
                    log.width = len;
                    log.OnSpawn(new Vector2(x0 + len * 0.5f, startY));
                    activeEntities.Add(log);

                    var food = foodPool.Get();
                    food.OnSpawn(new Vector2(side < 0f ? rw - 38f : -rw + 38f, startY));
                    activeEntities.Add(food);

                    // Coins trailing the safe passage (spawned above the screen like everything else).
                    SpawnCoinLine(side < 0f ? rw - 38f : -rw + 38f, startY + 44f, 3, 30f, 0f);
                    break;
                }

                case PatternType.Shark: // A predator: shark, pike, piranha school or catfish
                {
                    float px = Random.Range(-rw + 40f, rw - 40f);
                    if (kind == PredatorKind.Piranha)
                    {
                        // School in a little V: a pair at first, three later on
                        px = Mathf.Clamp(px, -rw + 60f, rw - 60f);
                        SpawnPredator(kind, new Vector2(px - 30f, startY + 72f), stage, difficulty);
                        SpawnPredator(kind, new Vector2(px + 30f, startY + 72f), stage, difficulty);
                        if (PiranhaSchoolSize(meters) >= 3) SpawnPredator(kind, new Vector2(px, startY + 40f), stage, difficulty);
                    }
                    else
                    {
                        SpawnPredator(kind, new Vector2(px, startY + 40f), stage, difficulty);
                    }
                    break;
                }

                case PatternType.Hook: // Swaying Hook Trap + real food
                {
                    var hook = hookPool.Get();
                    float hookX = Random.Range(-rw + 40f, rw - 40f);
                    hook.OnSpawn(new Vector2(hookX, startY));
                    activeEntities.Add(hook);

                    // Keep the real shrimp visibly apart from the hook so eating it is never a coin flip.
                    float foodX = Random.Range(-rw + 32f, rw - 32f);
                    if (Mathf.Abs(foodX - hookX) < 60f)
                    {
                        foodX = hookX + (hookX > 0f ? -1f : 1f) * Random.Range(60f, 110f);
                        foodX = Mathf.Clamp(foodX, -rw + 32f, rw - 32f);
                    }
                    var food = foodPool.Get();
                    food.OnSpawn(new Vector2(foodX, startY + 80f));
                    activeEntities.Add(food);

                    // Tempting coins just beside the hook's swing.
                    if (Random.value < 0.4f)
                    {
                        float coinX = Mathf.Clamp(hookX + (hookX > 0f ? -48f : 48f), -rw + 30f, rw - 30f);
                        SpawnCoinLine(coinX, startY + 20f, 3, 28f, 0f);
                    }
                    break;
                }
            }
        }

        private void SpawnPredator(PredatorKind kind, Vector2 pos, int stage, float difficulty)
        {
            var predator = sharkPool.Get();
            predator.Initialize(playerTransform, spawnConfig);
            predator.kind = kind;
            int largeStageReq = spawnConfig != null ? spawnConfig.largeSharksAfterStage : 3;
            // Catfish are always "large" (only a very big fish can eat them); sharks get big later on.
            predator.isLarge = kind == PredatorKind.Catfish ||
                               (kind == PredatorKind.Shark && stage >= largeStageReq && Random.value < 0.42f);
            predator.difficulty = difficulty;
            predator.OnSpawn(pos);
            activeEntities.Add(predator);
        }

        /// <summary>
        /// A wave covering part of the river: from one bank (gap on the other side) or in the middle
        /// (gaps on both sides). Coins wait in the gap as a reward for dodging it.
        /// </summary>
        private void SpawnWave(float rw, float startY, float side)
        {
            float baseRadius = gameConfig != null ? gameConfig.playerRadius : 14f;
            float pRadius = baseRadius * (growthSystem != null ? growthSystem.HitboxMultiplier : 1f);
            float minGap = pRadius * 2f + 40f; // always passable, even for a huge fish

            var wave = wavePool.Get();
            float len, centerX, gapX;
            if (Random.value < 0.35f)
            {
                len = Mathf.Min(rw * 2f * Random.Range(0.40f, 0.52f), rw * 2f - minGap * 2f);
                centerX = 0f;
                gapX = side * (rw - minGap * 0.5f);
            }
            else
            {
                len = Mathf.Min(rw * 2f * Random.Range(0.50f, 0.68f), rw * 2f + 10f - minGap);
                float x0 = side < 0f ? -rw - 10f : rw + 10f - len;
                centerX = x0 + len * 0.5f;
                gapX = side < 0f ? rw - minGap * 0.5f : -rw + minGap * 0.5f;
            }
            wave.width = len;
            wave.OnSpawn(new Vector2(centerX, startY));
            activeEntities.Add(wave);

            SpawnCoinLine(Mathf.Clamp(gapX, -rw + 26f, rw - 26f), startY - 10f, 2, 26f, 0f);
        }

        /// <summary>Vertical line of coins starting at (x, y) going upstream, with an optional sideways wave.</summary>
        private void SpawnCoinLine(float x, float y, int count, float spacing, float wave)
        {
            if (coinPool == null) return;
            float rw = gameConfig != null ? gameConfig.riverHalfWidth : 150f;
            for (int i = 0; i < count; i++)
            {
                var coin = coinPool.Get();
                float cx = Mathf.Clamp(x + Mathf.Sin(i * 0.8f) * wave, -rw + 24f, rw - 24f);
                coin.OnSpawn(new Vector2(cx, y + i * spacing));
                activeEntities.Add(coin);
            }
        }

        private void ReturnToPool(EntityBase entity)
        {
            if (entity == null) return;
            entity.OnDespawn();
            switch (entity.Type)
            {
                case EntityType.Food: foodPool?.Return((FoodPickup)entity); break;
                case EntityType.GoldFood: goldFoodPool?.Return((GoldFoodPickup)entity); break;
                case EntityType.Shark: sharkPool?.Return((SharkEnemy)entity); break;
                case EntityType.Rock: rockPool?.Return((RockObstacle)entity); break;
                case EntityType.Log: logPool?.Return((LogObstacle)entity); break;
                case EntityType.Hook: hookPool?.Return((HookTrap)entity); break;
                case EntityType.Coin: coinPool?.Return((CoinPickup)entity); break;
                case EntityType.Wave: wavePool?.Return((WaveObstacle)entity); break;
            }
        }
    }
}
