using System.Collections.Generic;
using UnityEngine;
using Upstream.Core;
using Upstream.Data;
using Upstream.Entities;

namespace Upstream.Spawning
{
    /// <summary>
    /// Manages the spawning and lifecycle of all river entities using Object Pools.
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        [SerializeField] private SpawnConfig spawnConfig;
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private Transform playerTransform;

        [Header("Object Pools")]
        [SerializeField] private ObjectPool<FoodPickup> foodPool;
        [SerializeField] private ObjectPool<GoldFoodPickup> goldFoodPool;
        [SerializeField] private ObjectPool<SharkEnemy> sharkPool;
        [SerializeField] private ObjectPool<RockObstacle> rockPool;
        [SerializeField] private ObjectPool<LogObstacle> logPool;
        [SerializeField] private ObjectPool<HookTrap> hookPool;

        private float nextSpawnDistance;
        private List<EntityBase> activeEntities = new List<EntityBase>();

        public IReadOnlyList<EntityBase> ActiveEntities => activeEntities;

        private void OnEnable()
        {
            GameEvents.OnGameStart += HandleGameStart;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
        }

        private void HandleGameStart()
        {
            foreach (var entity in activeEntities)
            {
                ReturnToPool(entity);
            }
            activeEntities.Clear();
            nextSpawnDistance = GameManager.Instance.CurrentDistance + 50f;
        }

        /// <summary>
        /// Updates the spawning logic based on distance traveled.
        /// </summary>
        public void Tick(float dt, float currentDistance, int currentStage, float flowSpeed)
        {
            if (currentDistance >= nextSpawnDistance)
            {
                SpawnPattern(currentStage);
                // Difficulty reduces interval
                float reduction = currentStage * (spawnConfig != null ? spawnConfig.intervalReductionPerDifficulty : 5f);
                float interval = Random.Range(
                    spawnConfig != null ? spawnConfig.minSpawnInterval : 40f, 
                    spawnConfig != null ? spawnConfig.maxSpawnInterval : 100f);
                
                nextSpawnDistance = currentDistance + Mathf.Max(20f, interval - reduction);
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

        private void SpawnPattern(int stage)
        {
            // Simple weighted pattern selection placeholder (assuming weights logic)
            int pattern = Random.Range(0, 5);
            float startY = -150f; // Spawns above screen
            float riverHalfWidth = gameConfig != null ? gameConfig.riverHalfWidth : 150f;
            
            switch (pattern)
            {
                case 0: // FoodLine
                    int count = Random.Range(3, 7);
                    float startX = Random.Range(-riverHalfWidth + 30f, riverHalfWidth - 30f);
                    for (int i = 0; i < count; i++)
                    {
                        bool isGold = spawnConfig != null && Random.value < (spawnConfig.goldFoodBaseChance + stage * spawnConfig.goldFoodChanceIncrease);
                        var food = isGold ? (EntityBase)goldFoodPool.Get() : foodPool.Get();
                        food.OnSpawn(new Vector2(startX + Mathf.Sin(i) * 20f, startY - i * 30f));
                        activeEntities.Add(food);
                    }
                    break;
                    
                case 1: // RockWithFood (leaves gap)
                    var rock = rockPool.Get();
                    float rockX = Random.Range(-riverHalfWidth + 30f, riverHalfWidth - 30f);
                    rock.OnSpawn(new Vector2(rockX, startY));
                    activeEntities.Add(rock);

                    var rfood = foodPool.Get();
                    float foodX = rockX > 0 ? rockX - 80f : rockX + 80f;
                    rfood.OnSpawn(new Vector2(foodX, startY));
                    activeEntities.Add(rfood);
                    break;
                    
                case 2: // LogWithFood
                    var log = logPool.Get();
                    bool fromLeft = Random.value > 0.5f;
                    float logX = fromLeft ? -riverHalfWidth : riverHalfWidth;
                    log.OnSpawn(new Vector2(logX, startY)); 
                    activeEntities.Add(log);
                    
                    var lfood = foodPool.Get();
                    lfood.OnSpawn(new Vector2(fromLeft ? 50f : -50f, startY + 20f));
                    activeEntities.Add(lfood);
                    break;
                    
                case 3: // Shark
                    var shark = sharkPool.Get();
                    shark.Initialize(playerTransform);
                    if (spawnConfig != null) {
                        shark.isLarge = stage >= spawnConfig.largeSharksAfterStage && Random.value > 0.4f;
                    }
                    shark.OnSpawn(new Vector2(Random.Range(-riverHalfWidth + 40f, riverHalfWidth - 40f), startY));
                    activeEntities.Add(shark);
                    break;
                    
                case 4: // HookWithFood
                    var hook = hookPool.Get();
                    float hookX = Random.Range(-riverHalfWidth + 30f, riverHalfWidth - 30f);
                    hook.OnSpawn(new Vector2(hookX, startY));
                    activeEntities.Add(hook);
                    
                    var hfood = foodPool.Get();
                    hfood.OnSpawn(new Vector2(hookX > 0 ? hookX - 60f : hookX + 60f, startY - 40f));
                    activeEntities.Add(hfood);
                    break;
            }
        }

        private void ReturnToPool(EntityBase entity)
        {
            entity.OnDespawn();
            switch (entity.Type)
            {
                case EntityType.Food: foodPool.Return((FoodPickup)entity); break;
                case EntityType.GoldFood: goldFoodPool.Return((GoldFoodPickup)entity); break;
                case EntityType.Shark: sharkPool.Return((SharkEnemy)entity); break;
                case EntityType.Rock: rockPool.Return((RockObstacle)entity); break;
                case EntityType.Log: logPool.Return((LogObstacle)entity); break;
                case EntityType.Hook: hookPool.Return((HookTrap)entity); break;
            }
        }
    }
}
