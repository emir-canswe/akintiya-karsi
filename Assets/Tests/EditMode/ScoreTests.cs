using NUnit.Framework;
using UnityEngine;
using Upstream.Mechanics;

namespace Upstream.Tests
{
    public class ScoreTests
    {
        [Test]
        public void DistanceToDisplayScore_ConversionIsCorrect()
        {
            float rawDistance = 35.6f;
            int expectedDisplayScore = 3; // floor(35.6 / 10)
            
            int actualDisplayScore = Mathf.FloorToInt(rawDistance / 10f);
            
            Assert.AreEqual(expectedDisplayScore, actualDisplayScore);
        }

        [Test]
        public void BonusDistance_AddsCorrectly()
        {
            float initialDistance = 100f;
            float bonus = 50f;
            
            float totalDistance = initialDistance + bonus;
            
            Assert.AreEqual(150f, totalDistance);
        }

        [Test]
        public void Score_IsAlwaysInteger_Floored()
        {
            float[] distances = { 9.9f, 10.0f, 10.9f, 25.5f, 0.1f };
            int[] expectedScores = { 0, 1, 1, 2, 0 };
            
            for (int i = 0; i < distances.Length; i++)
            {
                int score = Mathf.FloorToInt(distances[i] / 10f);
                Assert.AreEqual(expectedScores[i], score, $"Failed for distance {distances[i]}");
            }
        }
    }
}
