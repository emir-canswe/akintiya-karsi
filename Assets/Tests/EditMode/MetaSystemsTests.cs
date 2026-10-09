using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Upstream.Ads;
using Upstream.Core;
using Upstream.Data;

namespace Upstream.Tests
{
    public class MetaSystemsTests
    {
        // --- Localization ---

        [Test]
        public void EveryLocalizationKey_HasTextInEveryLanguage()
        {
            foreach (string key in LocalizationManager.Keys)
            {
                foreach (string lang in LocalizationManager.Languages)
                {
                    string text = LocalizationManager.Get(key, lang);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(text), $"'{key}' is empty in '{lang}'");
                    Assert.AreNotEqual(key, text, $"'{key}' is missing in '{lang}'");
                }
            }
        }

        [Test]
        public void FormatKeys_HavePlaceholderInEveryLanguage()
        {
            string[] formatKeys = { "best_score", "devour_toast", "unlock_note", "start_stage_unlocked" };
            foreach (string key in formatKeys)
            {
                foreach (string lang in LocalizationManager.Languages)
                {
                    StringAssert.Contains("{0}", LocalizationManager.Get(key, lang), $"'{key}' in '{lang}'");
                }
            }
        }

        [Test]
        public void StageKeys_ExistForEveryGrowthStage()
        {
            var growth = ScriptableObject.CreateInstance<GrowthConfig>();
            var keys = LocalizationManager.Keys.ToList();
            for (int i = 0; i < growth.stages.Length; i++)
            {
                CollectionAssert.Contains(keys, $"stage_name_{i}");
                CollectionAssert.Contains(keys, $"stage_badge_{i}");
                if (i > 0) CollectionAssert.Contains(keys, $"stage_up_{i}");
            }
        }

        [Test]
        public void DefaultLanguage_TurkishPhonesGetTurkish_OthersEnglish()
        {
            Assert.AreEqual("tr", LocalizationManager.DefaultLanguageFor(SystemLanguage.Turkish));
            Assert.AreEqual("en", LocalizationManager.DefaultLanguageFor(SystemLanguage.English));
            Assert.AreEqual("en", LocalizationManager.DefaultLanguageFor(SystemLanguage.German));
        }

        [Test]
        public void FreshSave_HasNoLanguageChosen_SoPhoneLanguageDecides()
        {
            Assert.AreEqual("", new SaveData().settingsLanguage);
        }

        // --- Interstitial frequency ---

        [Test]
        public void Interstitial_NeverInFirstGames()
        {
            for (int games = 0; games <= InterstitialPolicy.GraceGamesCount; games++)
            {
                Assert.IsFalse(InterstitialPolicy.ShouldShowInterstitial(games, false));
            }
        }

        [Test]
        public void Interstitial_EveryThirdGameAfterGrace()
        {
            Assert.IsFalse(InterstitialPolicy.ShouldShowInterstitial(4, false));
            Assert.IsFalse(InterstitialPolicy.ShouldShowInterstitial(5, false));
            Assert.IsTrue(InterstitialPolicy.ShouldShowInterstitial(6, false));
            Assert.IsFalse(InterstitialPolicy.ShouldShowInterstitial(7, false));
            Assert.IsTrue(InterstitialPolicy.ShouldShowInterstitial(9, false));
        }

        [Test]
        public void Interstitial_NotAfterRewardedContinue_NorWhenAdsRemoved()
        {
            Assert.IsFalse(InterstitialPolicy.ShouldShowInterstitial(6, usedRewardedContinueThisRun: true));
            Assert.IsFalse(InterstitialPolicy.ShouldShowInterstitial(6, false, adsRemoved: true));
        }

        // --- Persistent progression ---

        [Test]
        public void StartStage_UnlocksYoungAfterReachingAlpha()
        {
            var growth = ScriptableObject.CreateInstance<GrowthConfig>();
            Assert.AreEqual(0, growth.GetStartStage(0));
            Assert.AreEqual(0, growth.GetStartStage(2));
            Assert.AreEqual(1, growth.GetStartStage(3));
            Assert.AreEqual(1, growth.GetStartStage(4), "Reaching Titan doesn't skip more of the run.");
        }

        [Test]
        public void StartStage_CanBeDisabledFromConfig()
        {
            var growth = ScriptableObject.CreateInstance<GrowthConfig>();
            growth.unlockedStartStage = 0;
            Assert.AreEqual(0, growth.GetStartStage(4));
        }

        // --- Save migration ---

        [Test]
        public void Migrate_V1Save_KeepsUnlockedStartStage()
        {
            var v1 = new SaveData { version = 1, bestScore = 120, unlockedStartStage = 1 };
            SaveSystem.Migrate(v1);
            Assert.AreEqual(2, v1.version);
            Assert.AreEqual(120, v1.bestScore);
            Assert.GreaterOrEqual(v1.bestStageReached, 1);
            Assert.IsFalse(v1.adsRemoved);
        }
    }
}
