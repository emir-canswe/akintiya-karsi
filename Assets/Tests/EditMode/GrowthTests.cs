using NUnit.Framework;
using UnityEngine;
using Upstream.Data;
using Upstream.Mechanics;

namespace Upstream.Tests
{
    public class GrowthTests
    {
        private GrowthConfig CreateTestConfig()
        {
            return ScriptableObject.CreateInstance<GrowthConfig>();
        }

        [Test]
        public void GrowthStage_Thresholds_AreInAscendingOrder()
        {
            var config = CreateTestConfig();
            Assert.AreEqual(5, config.stages.Length);

            for (int i = 1; i < config.stages.Length; i++)
            {
                Assert.Greater(config.stages[i].foodRequired, config.stages[i - 1].foodRequired,
                    $"Threshold at stage {i} must be greater than stage {i - 1}");
            }
        }

        [Test]
        public void GrowthStage_SizeMultiplier_IncreasesWithStage()
        {
            var config = CreateTestConfig();
            for (int i = 1; i < config.stages.Length; i++)
            {
                Assert.Greater(config.stages[i].sizeMultiplier, config.stages[i - 1].sizeMultiplier,
                    $"Size at stage {i} must be greater than stage {i - 1}");
            }
        }

        [Test]
        public void GrowthStage_HitboxMultiplier_IncreasesWithStage()
        {
            var config = CreateTestConfig();
            for (int i = 1; i < config.stages.Length; i++)
            {
                Assert.Greater(config.stages[i].hitboxMultiplier, config.stages[i - 1].hitboxMultiplier,
                    $"Hitbox at stage {i} must be greater than stage {i - 1}");
            }
        }

        [Test]
        public void GrowthStage_PowerBarFillMultiplier_DecreasesWithStage()
        {
            var config = CreateTestConfig();
            for (int i = 1; i < config.stages.Length; i++)
            {
                Assert.Less(config.stages[i].powerBarFillMultiplier, config.stages[i - 1].powerBarFillMultiplier,
                    $"Power fill multiplier at stage {i} must be less than stage {i - 1}");
            }
        }

        [Test]
        public void GrowthSystem_TransitionsAndSharkEating_WorkCorrectly()
        {
            var go = new GameObject("TestPlayer");
            var growth = go.AddComponent<GrowthSystem>();
            var config = CreateTestConfig();
            growth.Config = config;
            growth.ResetGrowth();

            Assert.AreEqual(0, growth.CurrentStage);
            Assert.IsFalse(growth.CanEatShark(false, true));

            // Eat 35 food -> should reach Stage 2 (Adult: 35 food)
            for (int i = 0; i < 35; i++)
            {
                growth.OnFoodEaten(false);
            }

            Assert.AreEqual(2, growth.CurrentStage);
            Assert.IsTrue(growth.CanEatShark(false, true), "Adult should eat small sharks with Turbo");
            Assert.IsFalse(growth.CanEatShark(false, false), "Adult should NOT eat small sharks without Turbo");

            // Eat 25 more food (total 60) -> Stage 3 (Alpha: 60 food)
            for (int i = 0; i < 25; i++)
            {
                growth.OnFoodEaten(false);
            }

            Assert.AreEqual(3, growth.CurrentStage);
            Assert.IsTrue(growth.CanEatShark(false, false), "Alpha should eat small sharks without Turbo");
            Assert.IsTrue(growth.CanEatShark(true, true), "Alpha should eat large sharks with Turbo");

            Object.DestroyImmediate(go);
        }
    }
}
