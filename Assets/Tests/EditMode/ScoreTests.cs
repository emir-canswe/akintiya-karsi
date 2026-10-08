using NUnit.Framework;
using UnityEngine;
using Upstream.Data;
using Upstream.Mechanics;

namespace Upstream.Tests
{
    public class ScoreTests
    {
        [Test]
        public void DistanceToDisplayScore_ConversionIsCorrect()
        {
            var go = new GameObject("TestScorer");
            var scorer = go.AddComponent<ScoreSystem>();
            scorer.Config = ScriptableObject.CreateInstance<GameConfig>();
            scorer.ResetScore();

            scorer.AddDistance(35.6f);
            Assert.AreEqual(3, scorer.DisplayScore);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void BonusDistance_AddsCorrectly()
        {
            var go = new GameObject("TestScorer");
            var scorer = go.AddComponent<ScoreSystem>();
            scorer.Config = ScriptableObject.CreateInstance<GameConfig>();
            scorer.ResetScore();

            scorer.AddDistance(100f);
            scorer.AddBonus(500f); // +50m shark eat bonus

            Assert.AreEqual(60, scorer.DisplayScore);

            Object.DestroyImmediate(go);
        }
    }
}
