using UnityEngine;
using Upstream.Camera;
using Upstream.Core;
using Upstream.Data;
using Upstream.Entities;
using Upstream.Mechanics;
using Upstream.River;
using Upstream.UI;

namespace Upstream.Spawning
{
    /// <summary>
    /// Performs non-physics based collision checks for all river entities against the player.
    /// Triggers the signature Shark Devour ("O AN") sequence when a large enough fish eats a shark.
    /// </summary>
    public class CollisionManager : MonoBehaviour
    {
        [SerializeField] private SpawnManager spawnManager;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private HealthSystem healthSystem;
        [SerializeField] private TurboSystem turboSystem;
        [SerializeField] private GrowthSystem growthSystem;
        [SerializeField] private ScoreSystem scoreSystem;
        [SerializeField] private RiverController riverController;
        [SerializeField] private GameCamera gameCamera;
        [SerializeField] private ToastUI toastUI;
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private GrowthConfig growthConfig;


        public void Configure(
            SpawnManager spawner,
            Transform player,
            HealthSystem health,
            TurboSystem turbo,
            GrowthSystem growth,
            ScoreSystem scorer,
            RiverController river,
            GameCamera cam,
            ToastUI toast,
            GameConfig gConfig,
            GrowthConfig grConfig)
        {
            spawnManager = spawner;
            playerTransform = player;
            healthSystem = health;
            turboSystem = turbo;
            growthSystem = growth;
            scoreSystem = scorer;
            riverController = river;
            gameCamera = cam;
            toastUI = toast;
            gameConfig = gConfig;
            growthConfig = grConfig;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (playerTransform == null || spawnManager == null) return;

            Vector2 playerPos = playerTransform.position;
            float baseRadius = gameConfig != null ? gameConfig.playerRadius : 14f;
            float hitboxMult = growthSystem != null ? growthSystem.HitboxMultiplier : 1f;
            FishDef fish = FishShop.Active;
            float pRadius = baseRadius * hitboxMult * fish.HitboxMultiplier;
            float magnet = fish.MagnetRadius;
            bool isTurbo = turboSystem != null && turboSystem.IsTurboActive;

            var entities = spawnManager.ActiveEntities;
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity == null || !entity.isActive || entity.isFlying) continue;

                Vector2 ePos = entity.transform.position;
                if (Mathf.Abs(ePos.y - playerPos.y) > 140f) continue;

                switch (entity.Type)
                {
                    case EntityType.Food:
                        if (magnet > 0f && Vector2.Distance(playerPos, ePos) < pRadius + magnet) entity.Attract(playerPos, Time.deltaTime);
                        if (Vector2.Distance(playerPos, ePos) < (pRadius + 14f))
                        {
                            ((FoodPickup)entity).Collect();
                        }
                        break;

                    case EntityType.Coin:
                        if (magnet > 0f && Vector2.Distance(playerPos, ePos) < pRadius + magnet) entity.Attract(playerPos, Time.deltaTime);
                        if (Vector2.Distance(playerPos, ePos) < (pRadius + 13f))
                        {
                            ((CoinPickup)entity).Collect();
                        }
                        break;

                    case EntityType.GoldFood:
                        if (magnet > 0f && Vector2.Distance(playerPos, ePos) < pRadius + magnet) entity.Attract(playerPos, Time.deltaTime);
                        if (Vector2.Distance(playerPos, ePos) < (pRadius + 16f))
                        {
                            ((GoldFoodPickup)entity).Collect();
                        }
                        break;

                    case EntityType.Rock:
                        var rock = (RockObstacle)entity;
                        if (Vector2.Distance(playerPos, ePos) < (pRadius + rock.radius - 4f))
                        {
                            if (isTurbo || fish.Has(FishAbility.RockBreaker))
                            {
                                rock.Smash(playerPos.x);
                                if (!isTurbo) gameCamera?.Shake(3f);
                            }
                            else if (rock.isSmall && growthSystem != null &&
                                     growthSystem.CurrentStageData != null &&
                                     growthSystem.CurrentStageData.smallRockImmune)
                            {
                                rock.Smash(playerPos.x);
                                riverController?.ApplySlowdown(0.55f);
                                gameCamera?.Shake(3.5f);
                            }
                            else
                            {
                                if (healthSystem != null && healthSystem.TakeDamage())
                                {
                                    gameCamera?.Shake(9f);
                                }
                                rock.Smash(playerPos.x);
                            }
                        }
                        break;

                    case EntityType.Shark:
                        var sh = (SharkEnemy)entity;
                        float sharkScale = sh.HitScale;
                        float[] parts = sh.HitOffsets;
                        float partRadius = sh.HitRadius * sharkScale;
                        bool sharkHit = false;
                        for (int k = 0; k < parts.Length; k++)
                        {
                            // Follow the body's heading so a turning predator is hit where it's drawn
                            Vector2 partPos = ePos + (Vector2)(sh.transform.rotation * new Vector3(0f, parts[k] * sharkScale, 0f));
                            if (Vector2.Distance(playerPos, partPos) < (pRadius + partRadius))
                            {
                                sharkHit = true;
                                break;
                            }
                        }

                        if (sharkHit)
                        {
                            bool canEat = growthSystem != null && growthSystem.CanEatShark(sh.isLarge, isTurbo);
                            if (canEat)
                            {
                                // SIGNATURE MOMENT: Devouring a shark!
                                sh.Smash(playerPos.x);
                                GameEvents.OnSharkEaten?.Invoke();

                                float bonus = growthConfig != null ? growthConfig.sharkEatBonusDistance : 500f;
                                scoreSystem?.AddBonus(bonus);

                                float btScale = growthConfig != null ? growthConfig.bulletTimeScale : 0.3f;
                                float btDur = growthConfig != null ? growthConfig.bulletTimeDuration : 0.32f;
                                float zoomAmt = growthConfig != null ? growthConfig.zoomInAmount : 0.2f;

                                gameCamera?.BulletTime(btScale, btDur);
                                gameCamera?.ZoomIn(zoomAmt, 0.22f);
                                gameCamera?.Shake(12f);
                                float divisor = gameConfig != null && gameConfig.distanceToMetersDivisor > 0f
                                    ? gameConfig.distanceToMetersDivisor
                                    : 10f;
                                int bonusMeters = Mathf.RoundToInt(bonus / divisor);
                                toastUI?.Show(LocalizationManager.Format("devour_toast", bonusMeters), new Color(1f, 0.54f, 0.36f));
                            }
                            else if (isTurbo)
                            {
                                sh.Smash(playerPos.x);
                                gameCamera?.Shake(6f);
                            }
                            else
                            {
                                if (healthSystem != null && healthSystem.TakeDamage())
                                {
                                    gameCamera?.Shake(10f);
                                }
                                sh.Smash(playerPos.x);
                            }
                        }
                        break;

                    case EntityType.Log:
                        var log = (LogObstacle)entity;
                        float minX = ePos.x - log.width * 0.5f;
                        float maxX = ePos.x + log.width * 0.5f;
                        float minY = ePos.y - log.height * 0.5f;
                        float maxY = ePos.y + log.height * 0.5f;

                        float closestX = Mathf.Clamp(playerPos.x, minX, maxX);
                        float closestY = Mathf.Clamp(playerPos.y, minY, maxY);

                        if (Vector2.Distance(playerPos, new Vector2(closestX, closestY)) < (pRadius - 2f))
                        {
                            if (isTurbo)
                            {
                                log.Smash(playerPos.x);
                            }
                            else
                            {
                                if (healthSystem != null && healthSystem.TakeDamage())
                                {
                                    gameCamera?.Shake(9f);
                                }
                                log.Smash(playerPos.x);
                            }
                        }
                        break;

                    case EntityType.Wave:
                        var wave = (WaveObstacle)entity;
                        if (wave.spent) break;
                        bool waveRider = fish.Has(FishAbility.WaveRider);
                        float halfW = wave.width * 0.5f;
                        float wx = Mathf.Clamp(playerPos.x, ePos.x - halfW, ePos.x + halfW);
                        float wy = Mathf.Clamp(playerPos.y, ePos.y - WaveObstacle.Height * 0.5f, ePos.y + WaveObstacle.Height * 0.5f);
                        if (Vector2.Distance(playerPos, new Vector2(wx, wy)) < pRadius)
                        {
                            wave.Spend();
                            if (!isTurbo && !waveRider)
                            {
                                // Not damage: the wave pushes back and the river slows for a moment.
                                riverController?.ApplySlowdown(1.6f);
                                gameCamera?.Shake(4f);
                                GameEvents.OnWaveHit?.Invoke();
                                toastUI?.Show(LocalizationManager.Get("wave_toast"), new Color(0.55f, 0.85f, 1f));
                            }
                        }
                        break;

                    case EntityType.Hook:
                        var hook = (HookTrap)entity;
                        Vector2 hookPos = new Vector2(hook.CurrentX, ePos.y);
                        if (Vector2.Distance(playerPos, hookPos) < (pRadius + 9f))
                        {
                            if (isTurbo || fish.Has(FishAbility.HookProof))
                            {
                                hook.Smash(playerPos.x);
                            }
                            else
                            {
                                if (healthSystem != null && healthSystem.TakeDamage())
                                {
                                    gameCamera?.Shake(9f);
                                }
                                hook.Smash(playerPos.x);
                            }
                        }
                        break;
                }
            }
        }
    }
}
