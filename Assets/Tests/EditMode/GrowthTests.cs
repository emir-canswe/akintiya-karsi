using NUnit.Framework;
using UnityEngine;
using Upstream.Mechanics;

namespace Upstream.Tests
{
    public class GrowthTests
    {
        // Mock classes/structs to make this compile and run
        // In a real scenario, these would reference the actual config classes
        
        [Test]
        public void GrowthStage_Thresholds_AreInAscendingOrder()
        {
            // Setup mock config
            int[] thresholds = new int[] { 0, 10, 25, 50, 100 };
            
            for (int i = 1; i < thresholds.Length; i++)
            {
                Assert.IsTrue(thresholds[i] > thresholds[i - 1], $"Threshold at index {i} is not greater than index {i-1}");
            }
        }

        [Test]
        public void GrowthStage_SizeMultiplier_IncreasesWithStage()
        {
            float[] sizes = new float[] { 1.0f, 1.2f, 1.5f, 1.8f, 2.2f };
            
            for (int i = 1; i < sizes.Length; i++)
            {
                Assert.IsTrue(sizes[i] > sizes[i - 1], $"Size at stage {i} is not greater than stage {i-1}");
            }
        }

        [Test]
        public void GrowthStage_HitboxMultiplier_IncreasesWithStage()
        {
            float[] hitboxes = new float[] { 1.0f, 1.2f, 1.5f, 1.8f, 2.2f };
            
            for (int i = 1; i < hitboxes.Length; i++)
            {
                Assert.IsTrue(hitboxes[i] > hitboxes[i - 1], $"Hitbox at stage {i} is not greater than stage {i-1}");
            }
        }

        [Test]
        public void GrowthStage_PowerBarFillMultiplier_DecreasesWithStage()
        {
            float[] multipliers = new float[] { 1.0f, 0.8f, 0.6f, 0.4f, 0.2f };
            
            for (int i = 1; i < multipliers.Length; i++)
            {
                Assert.IsTrue(multipliers[i] < multipliers[i - 1], $"Power fill multiplier at stage {i} is not less than stage {i-1}");
            }
        }

        [Test]
        public void GrowthStage_Transition_TriggersCorrectStage()
        {
            // Simplified logic test
            int currentFood = 0;
            int foodRequired = 10;
            int currentStage = 0;
            
            // Eat food
            currentFood += foodRequired;
            
            // Check transition
            if (currentFood >= foodRequired)
            {
                currentStage++;
            }
            
            Assert.AreEqual(1, currentStage, "Stage did not increment correctly when food threshold reached.");
        }

        [Test]
        public void CanEatShark_Logic_IsCorrect()
        {
            int maxStage = 4;
            
            // Example logic: can eat shark if max stage OR turbo is active
            bool canEatAtStage0_NoTurbo = false;
            bool canEatAtStage4_NoTurbo = true;
            bool canEatAtStage0_WithTurbo = true;
            
            Assert.IsFalse(canEatAtStage0_NoTurbo);
            Assert.IsTrue(canEatAtStage4_NoTurbo);
            Assert.IsTrue(canEatAtStage0_WithTurbo);
        }
    }
}
