using NUnit.Framework;
using UnityEngine;
using Upstream.Data;
using Upstream.Entities;
using Upstream.Mechanics;
using Upstream.River;

namespace Upstream.Tests
{
    public class WaveSurgeTests
    {
        [Test]
        public void BigWaves_TakeSpeedAway_ButNeverBelowStart()
        {
            var cfg = ScriptableObject.CreateInstance<GameConfig>();
            float meters = 2000f;
            float plain = DifficultyScaler.SpeedForDistance(meters, cfg.startSpeed, cfg.maxSpeed);

            Assert.AreEqual(plain, RiverController.TargetSpeed(meters, cfg.startSpeed, cfg.maxSpeed, 0f));
            Assert.AreEqual(plain - 75f, RiverController.TargetSpeed(meters, cfg.startSpeed, cfg.maxSpeed, 75f), 0.001f);
            Assert.AreEqual(cfg.startSpeed, RiverController.TargetSpeed(10f, cfg.startSpeed, cfg.maxSpeed, 500f));
        }

        [Test]
        public void Sets_StartAt2000m_ThenEvery500m()
        {
            var cfg = ScriptableObject.CreateInstance<GameConfig>();
            Assert.AreEqual(2000f, cfg.waveSurgeFromMeters, "Big waves only come late, when the river is really fast.");
            Assert.AreEqual(500f, cfg.waveSurgeEveryMeters);
            Assert.AreEqual(2500f, RiverController.NextSurgeMeters(2000f, 2000f, 500f));
            Assert.AreEqual(3000f, RiverController.NextSurgeMeters(2500.5f, 2000f, 500f));
            // A continue far past a missed set doesn't fire several sets at once.
            Assert.AreEqual(4000f, RiverController.NextSurgeMeters(3720f, 2000f, 500f));
        }

        [Test]
        public void ASet_IsFelt_AndTheSpeedIsBackBeforeTheNextSet()
        {
            var cfg = ScriptableObject.CreateInstance<GameConfig>();
            float setDrop = cfg.waveSurgeCount * cfg.waveSpeedDrop;
            Assert.GreaterOrEqual(setDrop, 50f, "A set should be felt.");
            float recoverMeters = setDrop / cfg.waveReliefRecoveryPerMeter;
            Assert.Less(recoverMeters, cfg.waveSurgeEveryMeters, "Full speed again before the next set: the river doesn't keep slowing down.");
            Assert.Greater(recoverMeters, 200f, "The breather lasts a while.");
            float speedAt2000 = DifficultyScaler.SpeedForDistance(cfg.waveSurgeFromMeters, cfg.startSpeed, cfg.maxSpeed);
            Assert.Greater(speedAt2000 - setDrop, cfg.startSpeed * 2f, "Still a fast river during the breather.");
        }

        [Test]
        public void PooledWave_SwitchesBetweenBigAndSmall()
        {
            var go = new GameObject("Wave");
            go.AddComponent<SpriteRenderer>().sprite = CartoonSpriteFactory.GetWave();
            var wave = go.AddComponent<WaveObstacle>();

            wave.isBig = true;
            wave.width = 324f;
            wave.OnSpawn(new Vector2(0f, 100f));
            var sr = go.GetComponent<SpriteRenderer>();
            Assert.AreEqual(324f, sr.size.x, 0.01f);
            Assert.AreSame(CartoonSpriteFactory.GetBigWave(), sr.sprite);
            Transform parts = go.transform.Find("BigWaveParts");
            Assert.IsNotNull(parts);
            Assert.IsTrue(parts.gameObject.activeSelf);
            Assert.Less(wave.HitCenterY, 100f, "The foam crest at the front (bottom) is what reaches the fish.");
            for (int i = 0; i < 30; i++) wave.Tick(0.05f, 200f); // animates without errors
            wave.Spend(0f);
            Assert.IsTrue(wave.spent);

            wave.isBig = false;
            wave.width = 160f;
            wave.OnSpawn(new Vector2(0f, 100f));
            Assert.IsFalse(parts.gameObject.activeSelf);
            Assert.AreSame(CartoonSpriteFactory.GetWave(), sr.sprite);
            Assert.AreEqual(100f, wave.HitCenterY);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void BigWaveArt_TilesWithoutSeams()
        {
            foreach (var sprite in new[] { CartoonSpriteFactory.GetBigWave(), CartoonSpriteFactory.GetFoamStrip() })
            {
                var tex = sprite.texture;
                float diff = 0f;
                for (int y = 0; y < tex.height; y++)
                {
                    Color a = tex.GetPixel(0, y), b = tex.GetPixel(tex.width - 1, y);
                    diff += Mathf.Abs(a.a - b.a) + Mathf.Abs(a.r - b.r) + Mathf.Abs(a.b - b.b);
                }
                Assert.Less(diff / tex.height, 0.2f, sprite.name + " should repeat seamlessly side by side.");
            }
        }
    }
}
