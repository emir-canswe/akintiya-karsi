using UnityEngine;
using Upstream.Core;
using Upstream.Entities;
using Upstream.Mechanics;
using Upstream.Data;

namespace Upstream.Spawning
{
    /// <summary>
    /// Performs non-physics based collision checks for all river entities against the player.
    /// </summary>
    public class CollisionManager : MonoBehaviour
    {
        [SerializeField] private SpawnManager spawnManager;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private HealthSystem healthSystem;
        [SerializeField] private TurboSystem turboSystem;
        [SerializeField] private GrowthSystem growthSystem;
        [SerializeField] private ScoreSystem scoreSystem;
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private GrowthConfig growthConfig;

        private void Update()
        {
            // Only check collisions if playing
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (playerTransform == null || spawnManager == null) return;

            Vector2 playerPos = playerTransform.position;
            float pRadius = gameConfig != null ? gameConfig.playerRadius : 14f;
            bool isTurbo = turboSystem != null && turboSystem.IsTurboActive;

            foreach (var entity in spawnManager.ActiveEntities)
            {
                if (!entity.isActive) continue;
                if (entity is SharkEnemy s && s.isDead) continue;

                Vector2 ePos = entity.transform.position;
                
                // Broadphase Y check - skip far entities
                if (Mathf.Abs(ePos.y - playerPos.y) > 150f) continue;

                bool hit = false;
                
                switch (entity.Type)
                {
                    case EntityType.Food:
                        hit = Vector2.Distance(playerPos, ePos) < (pRadius + 15f);
                        if (hit) ((FoodPickup)entity).Collect();
                        break;

                    case EntityType.GoldFood:
                        hit = Vector2.Distance(playerPos, ePos) < (pRadius + 15f);
                        if (hit) ((GoldFoodPickup)entity).Collect();
                        break;

                    case EntityType.Rock:
                        var rock = (RockObstacle)entity;
                        hit = Vector2.Distance(playerPos, ePos) < (pRadius + rock.radius);
                        if (hit)
                        {
                            if (isTurbo) 
                            {
                                rock.Smash();
                            }
                            else 
                            {
                                // If small rock and player immune, we could implement slow down here.
                                // For now, taking damage unless immune logic is handled inside HealthSystem.
                                healthSystem.TakeDamage(); 
                                rock.Smash(); 
                            }
                        }
                        break;

                    case EntityType.Shark:
                        var sh = (SharkEnemy)entity;
                        // Check against 3 body circles
                        float[] offsets = { -14f, 6f, 24f };
                        foreach(var off in offsets) 
                        {
                            // Adjusting offset direction assuming shark is pointing somewhat downwards
                            // Using local Up based on rotation
                            Vector3 partPos = ePos + sh.transform.up * off;
                            if (Vector2.Distance(playerPos, partPos) < (pRadius + 15f)) 
                            { 
                                hit = true; 
                                break; 
                            }
                        }
                        
                        if (hit)
                        {
                            if (isTurbo || (growthSystem != null && growthSystem.CanEatShark(sh.isLarge, isTurbo)))
                            {
                                sh.Smash();
                                GameEvents.OnSharkEaten?.Invoke();
                                if (scoreSystem != null && growthConfig != null)
                                {
                                    scoreSystem.AddBonus(growthConfig.sharkEatBonusDistance);
                                }
                                else if (scoreSystem != null)
                                {
                                    scoreSystem.AddBonus(50f);
                                }
                            }
                            else
                            {
                                healthSystem.TakeDamage();
                                sh.Smash();
                            }
                        }
                        break;

                    case EntityType.Log:
                        var log = (LogObstacle)entity;
                        // AABB closest point to circle center check
                        float minX = ePos.x - log.width / 2f;
                        float maxX = ePos.x + log.width / 2f;
                        float minY = ePos.y - log.height / 2f;
                        float maxY = ePos.y + log.height / 2f;
                        
                        float closestX = Mathf.Clamp(playerPos.x, minX, maxX);
                        float closestY = Mathf.Clamp(playerPos.y, minY, maxY);
                        
                        hit = Vector2.Distance(playerPos, new Vector2(closestX, closestY)) < pRadius;
                        if (hit)
                        {
                            if (isTurbo) log.Smash();
                            else { healthSystem.TakeDamage(); log.Smash(); }
                        }
                        break;

                    case EntityType.Hook:
                        var hook = (HookTrap)entity;
                        // Collision point sways with the hook
                        Vector2 hookPos = new Vector2(hook.CurrentX, ePos.y);
                        hit = Vector2.Distance(playerPos, hookPos) < (pRadius + 10f);
                        if (hit)
                        {
                            if (isTurbo) hook.Smash();
                            else { healthSystem.TakeDamage(); hook.Smash(); }
                        }
                        break;
                }
            }
        }
    }
}
