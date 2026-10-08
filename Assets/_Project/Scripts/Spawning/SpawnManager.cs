using System.Collections.Generic;
using UnityEngine;
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

        [Header("Object Pools")]
        [SerializeField] private ObjectPool<FoodPickup> foodPool;
        [SerializeField] private ObjectPool<GoldFoodPickup> goldFoodPool;
        [SerializeField] private ObjectPool<SharkEnemy> sharkPool;
        [SerializeField] private ObjectPool<RockObstacle> rockPool;
        [SerializeField] private ObjectPool<LogObstacle> logPool;
        [SerializeField] private ObjectPool<HookTrap> hookPool;

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
            ObjectPool<HookTrap> hooks)
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
        }

        private void OnEnable()
        {
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnContinueUsed += HandleContinueUsed;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnContinueUsed -= HandleContinueUsed;
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
                if (e.Type != EntityType.Food && e.Type != EntityType.GoldFood && e.yPosition < 180f)
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
                float diff = Mathf.Clamp01(currentDistance / 5000f);
                SpawnPattern(currentStage, diff);

                float minInt = spawnConfig != null ? spawnConfig.minSpawnInterval : 150f;
                float maxInt = spawnConfig != null ? spawnConfig.maxSpawnInterval : 230f;
                float reduction = diff * (spawnConfig != null ? spawnConfig.intervalReductionPerDifficulty : 50f);
                nextSpawnDistance = currentDistance + Mathf.Max(85f, Random.Range(minInt, maxInt) - reduction);
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

        private void SpawnPattern(int stage, float difficulty)
        {
            if (foodPool == null) return;

            float rw = gameConfig != null ? gameConfig.riverHalfWidth : 150f;
            float startY = 340f;
            float r = Random.value;
            float side = Random.value < 0.5f ? -1f : 1f;

            if (r < 0.30f) // Pattern 0: Curved Food Line (+ rare Gold Food)
            {
                float x = Random.Range(-rw + 32f, rw - 32f);
                int n = Random.Range(3, 6);
                for (int i = 0; i < n; i++)
                {
                    var food = foodPool.Get();
                    food.OnSpawn(new Vector2(x + Mathf.Sin(i * 0.9f) * 18f, startY + i * 34f));
                    activeEntities.Add(food);
                }

                float goldChance = (spawnConfig != null ? spawnConfig.goldFoodBaseChance : 0.07f) + difficulty * 0.05f;
                if (goldFoodPool != null && Random.value < goldChance)
                {
                    var gold = goldFoodPool.Get();
                    gold.OnSpawn(new Vector2(Random.Range(-rw + 32f, rw - 32f), startY + n * 34f + 40f));
                    activeEntities.Add(gold);
                }
            }
            else if (r < 0.50f && rockPool != null) // Pattern 1: Rocks with safe food path on opposite side
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
            }
            else if (r < 0.66f && logPool != null) // Pattern 2: Bank Log with safe passage on opposite side
            {
                float len = rw * 2f * Random.Range(0.42f, 0.56f + difficulty * 0.08f);
                float x0 = side < 0f ? -rw - 10f : rw + 10f - len;
                var log = logPool.Get();
                log.width = len;
                log.OnSpawn(new Vector2(x0 + len * 0.5f, startY));
                activeEntities.Add(log);

                var food = foodPool.Get();
                food.OnSpawn(new Vector2(side < 0f ? rw - 38f : -rw + 38f, startY));
                activeEntities.Add(food);
            }
            else if ((r < 0.84f || (difficulty > 0.5f && r < 0.90f)) && sharkPool != null) // Pattern 3: Tracking Shark
            {
                var shark = sharkPool.Get();
                shark.Initialize(playerTransform, spawnConfig);
                int largeStageReq = spawnConfig != null ? spawnConfig.largeSharksAfterStage : 2;
                shark.isLarge = stage >= largeStageReq && Random.value < 0.42f;
                shark.OnSpawn(new Vector2(Random.Range(-rw + 40f, rw - 40f), startY + 40f));
                activeEntities.Add(shark);
            }
            else if (hookPool != null) // Pattern 4: Swaying Hook Trap + real food
            {
                var hook = hookPool.Get();
                hook.OnSpawn(new Vector2(Random.Range(-rw + 40f, rw - 40f), startY));
                activeEntities.Add(hook);

                var food = foodPool.Get();
                food.OnSpawn(new Vector2(Random.Range(-rw + 32f, rw - 32f), startY + 80f));
                activeEntities.Add(food);
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
            }
        }
    }
}
