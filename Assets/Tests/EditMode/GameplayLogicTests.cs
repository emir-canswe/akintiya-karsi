using NUnit.Framework;
using UnityEngine;
using Upstream.Data;
using Upstream.Mechanics;
using Upstream.Spawning;

namespace Upstream.Tests
{
    public class GameplayLogicTests
    {
        // --- Health / Growth interaction ---

        [Test]
        public void StageUp_HealsPlayer_EvenWhenHealthSystemIsAddedAfterGrowthSystem()
        {
            // Same component order as UpstreamGameBootstrap: Growth first, Health second.
            var go = new GameObject("TestPlayer");
            var growth = go.AddComponent<GrowthSystem>();
            var health = go.AddComponent<HealthSystem>();
            growth.Config = ScriptableObject.CreateInstance<GrowthConfig>();
            health.Config = ScriptableObject.CreateInstance<GameConfig>();
            growth.ResetGrowth();
            health.ResetHealth();

            int startStage = growth.CurrentStage;
            Assume.That(startStage, Is.EqualTo(0), "Test assumes no unlocked start stage in the save file.");

            Assert.IsTrue(health.TakeDamage());
            Assert.AreEqual(2, health.CurrentHP);

            // Enough food for Young, which has healOnStageUp = true
            for (int i = 0; i < growth.Config.stages[1].foodRequired; i++) growth.OnFoodEaten(false);

            Assert.AreEqual(1, growth.CurrentStage);
            Assert.AreEqual(3, health.CurrentHP, "Reaching a new stage should restore one heart.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Heal_AtFullHealth_ReturnsFalseAndKeepsMax()
        {
            var go = new GameObject("TestPlayer");
            var health = go.AddComponent<HealthSystem>();
            health.Config = ScriptableObject.CreateInstance<GameConfig>();
            health.ResetHealth();

            Assert.IsFalse(health.Heal(1));
            Assert.AreEqual(health.MaxHP, health.CurrentHP);

            Object.DestroyImmediate(go);
        }

        // --- Spawn pattern weights ---

        [Test]
        public void PickPattern_RespectsZeroWeights()
        {
            var onlyLogs = new float[] { 0f, 0f, 1f, 0f, 0f };
            for (float r = 0f; r <= 1f; r += 0.1f)
            {
                Assert.AreEqual(SpawnManager.PatternType.Log, SpawnManager.PickPattern(r, onlyLogs));
            }
        }

        [Test]
        public void PickPattern_SplitsByRelativeWeight()
        {
            // Weights don't need to sum to 1.
            var weights = new float[] { 3f, 1f, 0f, 0f, 0f };
            Assert.AreEqual(SpawnManager.PatternType.FoodLine, SpawnManager.PickPattern(0.0f, weights));
            Assert.AreEqual(SpawnManager.PatternType.FoodLine, SpawnManager.PickPattern(0.74f, weights));
            Assert.AreEqual(SpawnManager.PatternType.Rocks, SpawnManager.PickPattern(0.76f, weights));
            Assert.AreEqual(SpawnManager.PatternType.Rocks, SpawnManager.PickPattern(1.0f, weights));
        }

        [Test]
        public void PickPattern_AllZero_FallsBackToFood()
        {
            var zeros = new float[] { 0f, 0f, 0f, 0f, 0f };
            Assert.AreEqual(SpawnManager.PatternType.FoodLine, SpawnManager.PickPattern(0.5f, zeros));
        }

        [Test]
        public void DefaultSpawnWeights_MakeSharksMoreCommonWithDifficulty()
        {
            var cfg = ScriptableObject.CreateInstance<SpawnConfig>();
            Assert.Greater(cfg.diff1SharkWeight, cfg.diff0SharkWeight);
            Assert.Greater(cfg.diff0FoodLineWeight, 0f);
        }

        // --- Predator line-up and crowding ---

        private static System.Collections.Generic.HashSet<PredatorKind> KindsAt(float meters)
        {
            var kinds = new System.Collections.Generic.HashSet<PredatorKind>();
            for (float r = 0f; r < 1f; r += 0.01f) kinds.Add(SpawnManager.PickPredator(meters, r));
            return kinds;
        }

        [Test]
        public void PickPredator_NewKindsJoinTheFurtherYouSwim()
        {
            CollectionAssert.AreEquivalent(new[] { PredatorKind.Shark }, KindsAt(0f), "only sharks at the start");
            CollectionAssert.AreEquivalent(new[] { PredatorKind.Shark, PredatorKind.Piranha }, KindsAt(SpawnManager.PiranhaFromMeters));
            CollectionAssert.AreEquivalent(new[] { PredatorKind.Shark, PredatorKind.Piranha, PredatorKind.Pike }, KindsAt(SpawnManager.PikeFromMeters));
            CollectionAssert.AreEquivalent(new[] { PredatorKind.Shark, PredatorKind.Piranha, PredatorKind.Pike, PredatorKind.Catfish },
                KindsAt(SpawnManager.CatfishFromMeters));
        }

        [Test]
        public void Predators_AreCappedAndSpacedInTime()
        {
            Assert.AreEqual(2, SpawnManager.MaxPredatorsFor(0f), "two at most at the start");
            int prev = 0;
            for (float m = 0f; m <= 5000f; m += 100f)
            {
                int cap = SpawnManager.MaxPredatorsFor(m);
                Assert.GreaterOrEqual(cap, prev, "the cap never shrinks with distance");
                Assert.LessOrEqual(cap, 5, "never a swarm");
                prev = cap;
            }
            Assert.AreEqual(2, SpawnManager.PiranhaSchoolSize(SpawnManager.PiranhaFromMeters));
            Assert.AreEqual(3, SpawnManager.PiranhaSchoolSize(SpawnManager.PiranhaTrioFromMeters));

            Assert.AreEqual(1.7f, SpawnManager.SpawnGapSeconds(0f), 1e-4f);
            Assert.AreEqual(1.0f, SpawnManager.SpawnGapSeconds(1f), 1e-4f);
            Assert.Greater(SpawnManager.SpawnGapSeconds(0.3f), SpawnManager.SpawnGapSeconds(0.7f), "denser as it gets harder");
        }

        // --- Difficulty / regions ---

        [Test]
        public void SpeedForDistance_RisesInSmallSteps_AndKeepsRisingPastMax()
        {
            Assert.AreEqual(140f, DifficultyScaler.SpeedForDistance(0f, 140f, 420f), 1e-3f);
            Assert.AreEqual(140f, DifficultyScaler.SpeedForDistance(DifficultyScaler.SpeedStepMeters - 1f, 140f, 420f), 1e-3f,
                "no speed-up before the first step");

            float prev = DifficultyScaler.SpeedForDistance(0f, 140f, 420f);
            for (float m = DifficultyScaler.SpeedStepMeters; m <= 3000f; m += DifficultyScaler.SpeedStepMeters)
            {
                float v = DifficultyScaler.SpeedForDistance(m, 140f, 420f);
                Assert.Greater(v, prev, $"speed must step up at {m} m");
                Assert.LessOrEqual(v - prev, DifficultyScaler.SpeedStep + 1e-3f, "steps stay small");
                prev = v;
            }
            Assert.Less(DifficultyScaler.SpeedForDistance(1000f, 140f, 420f), 420f, "top speed is not reached in the first km");
            Assert.LessOrEqual(DifficultyScaler.SpeedForDistance(1e6f, 140f, 420f), 420f * 1.2f + 1e-3f, "capped");
        }

        [Test]
        public void CalculateDifficulty_IsClampedLinearRamp()
        {
            Assert.AreEqual(0f, DifficultyScaler.CalculateDifficulty(0f, 5000f), 1e-5f);
            Assert.AreEqual(0.5f, DifficultyScaler.CalculateDifficulty(2500f, 5000f), 1e-5f);
            Assert.AreEqual(1f, DifficultyScaler.CalculateDifficulty(99999f, 5000f), 1e-5f);
        }

        [Test]
        public void FindRegion_PicksLatestStartedRegion_EvenIfUnsorted()
        {
            var regions = new[]
            {
                new RegionData { regionName = "C", startDistance = 20000f },
                new RegionData { regionName = "A", startDistance = 0f },
                new RegionData { regionName = "B", startDistance = 10000f },
            };

            Assert.AreEqual("A", DifficultyScaler.FindRegion(regions, 0f).regionName);
            Assert.AreEqual("A", DifficultyScaler.FindRegion(regions, 9999f).regionName);
            Assert.AreEqual("B", DifficultyScaler.FindRegion(regions, 10000f).regionName);
            Assert.AreEqual("C", DifficultyScaler.FindRegion(regions, 50000f).regionName);
        }

        [Test]
        public void DefaultDifficultyConfig_FirstRegionStartsAtZero()
        {
            var cfg = ScriptableObject.CreateInstance<DifficultyConfig>();
            Assert.AreEqual(cfg.regions[0], DifficultyScaler.FindRegion(cfg.regions, 0f));
        }
    }
}
