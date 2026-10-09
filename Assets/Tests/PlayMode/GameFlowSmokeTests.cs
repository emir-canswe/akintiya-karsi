using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using Upstream.Ads;
using Upstream.Core;
using Upstream.Mechanics;
using Upstream.UI;

namespace Upstream.Tests
{
    /// <summary>
    /// Boots the whole game (via UpstreamGameBootstrap's auto-bootstrap) and walks the
    /// menu -> play -> game over -> continue -> game over -> replay loop.
    /// Any exception logged along the way fails the test.
    /// </summary>
    public class GameFlowSmokeTests
    {
        private string savePath;
        private string saveBackup;

        [SetUp]
        public void BackupSave()
        {
            // Don't let the test pollute the developer's real save file.
            savePath = Application.persistentDataPath + "/save.json";
            saveBackup = File.Exists(savePath) ? File.ReadAllText(savePath) : null;
        }

        [TearDown]
        public void RestoreSave()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            LocalizationManager.ResetCache();
            if (saveBackup != null) File.WriteAllText(savePath, saveBackup);
            else if (File.Exists(savePath)) File.Delete(savePath);
        }

        private static T FindIncludingInactive<T>() where T : Object
        {
            return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        }

        [UnityTest]
        public IEnumerator FullLoop_ScreensFollowGameState()
        {
            // Let the auto-bootstrap build everything.
            yield return null;
            yield return null;

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "Bootstrap should create a GameManager.");

            var hud = FindIncludingInactive<HUDController>();
            var over = FindIncludingInactive<GameOverScreen>();
            var toast = FindIncludingInactive<ToastUI>();
            var health = FindIncludingInactive<HealthSystem>();
            Assert.IsNotNull(hud);
            Assert.IsNotNull(over);
            Assert.IsNotNull(toast);

            Assert.IsFalse(toast.gameObject.activeSelf, "Toast must start hidden (no stray 'TURBO!' on the menu).");
            Assert.IsFalse(hud.gameObject.activeSelf, "HUD hidden on menu.");

            // Start
            gm.StartGame();
            yield return null;
            Assert.AreEqual(GameState.Playing, gm.CurrentState);
            Assert.IsTrue(hud.gameObject.activeSelf, "HUD must appear when the run starts.");

            yield return new WaitForSeconds(0.5f);

            // Die
            gm.GameOver();
            yield return new WaitForSecondsRealtime(gm.Config.deathAnimDuration + 0.2f);
            Assert.AreEqual(GameState.GameOver, gm.CurrentState);
            Assert.IsTrue(over.gameObject.activeSelf, "Game over screen must appear after dying.");
            Assert.IsFalse(hud.gameObject.activeSelf);

            // Continue (as if the rewarded ad completed)
            gm.ContinueWithAd();
            yield return null;
            Assert.AreEqual(GameState.Playing, gm.CurrentState);
            Assert.IsTrue(hud.gameObject.activeSelf, "HUD must come back after a continue.");
            Assert.IsFalse(over.gameObject.activeSelf);
            Assert.AreEqual(gm.Config.continueHP, health.CurrentHP);

            // Die again, then replay
            gm.GameOver();
            yield return new WaitForSecondsRealtime(gm.Config.deathAnimDuration + 0.2f);
            Assert.IsTrue(over.gameObject.activeSelf);

            gm.StartGame();
            yield return null;
            Assert.AreEqual(GameState.Playing, gm.CurrentState);
            Assert.IsTrue(hud.gameObject.activeSelf);
            Assert.IsFalse(over.gameObject.activeSelf);
            Assert.AreEqual(gm.Config.maxHP, health.CurrentHP);
        }

        [UnityTest]
        public IEnumerator PauseResume_GoldOverflow_AndCorruptSave()
        {
            yield return null;
            yield return null;

            var gm = GameManager.Instance;
            var pause = FindIncludingInactive<PauseOverlay>();
            var turbo = FindIncludingInactive<TurboSystem>();
            var health = FindIncludingInactive<HealthSystem>();
            Assert.IsNotNull(pause);

            gm.StartGame();
            yield return null;

            // Pause (what backgrounding the app / Android back does)
            gm.PauseGame();
            yield return null;
            Assert.AreEqual(GameState.Paused, gm.CurrentState);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(pause.gameObject.activeSelf, "Pause overlay should show.");

            gm.ResumeGame();
            yield return null;
            Assert.AreEqual(GameState.Playing, gm.CurrentState);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(pause.gameObject.activeSelf);
            Assert.IsTrue(health.IsInvincible, "Short grace period after resuming.");

            // Gold food at full HP turns into power instead of being wasted.
            Assert.AreEqual(health.MaxHP, health.CurrentHP);
            float before = turbo.PowerPercent;
            GameEvents.OnFoodEaten?.Invoke(true);
            Assert.Greater(turbo.PowerPercent, before);

            // A truncated/empty save file must not break loading.
            File.WriteAllText(savePath, "");
            var loaded = SaveSystem.Load();
            Assert.IsNotNull(loaded);
            Assert.AreEqual(0, loaded.bestScore);
        }

        private static Button FindButton(string objectName)
        {
            foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (b.name == objectName) return b;
            }
            return null;
        }

        [UnityTest]
        public IEnumerator Settings_And_LanguageSwitch_UpdateLabels()
        {
            yield return null;
            yield return null;

            var settings = FindIncludingInactive<SettingsScreen>();
            Assert.IsNotNull(settings);
            Assert.IsFalse(settings.gameObject.activeSelf);

            settings.Open();
            yield return null;
            Assert.IsTrue(settings.gameObject.activeSelf);

            TMP_Text title = null;
            foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == "Title" && t.GetComponent<LocalizedText>() != null && t.GetComponent<LocalizedText>().Key == "title") title = t;
            }
            Assert.IsNotNull(title);

            LocalizationManager.SetLanguage("en");
            yield return null;
            StringAssert.Contains("UPSTREAM", title.text);

            LocalizationManager.SetLanguage("tr");
            yield return null;
            StringAssert.Contains("KARŞI", title.text);

            FindButton("Close").onClick.Invoke();
            yield return null;
            Assert.IsFalse(settings.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator PlayAgain_ShowsInterstitialWhenDue_ThenStartsRun()
        {
            yield return null;
            yield return null;
            var gm = GameManager.Instance;
            Assert.IsNotNull(AdsManager.Instance);
            // Mock consent + init happen in Start.
            float waitUntil = Time.realtimeSinceStartup + 2f;
            while (!AdsManager.Instance.IsInitialized && Time.realtimeSinceStartup < waitUntil) yield return null;
            Assert.IsTrue(AdsManager.Instance.IsInitialized);

            // Make this the 6th game -> interstitial is due when tapping "Play Again".
            var save = SaveSystem.Load();
            save.totalGamesPlayed = 5;
            SaveSystem.Save(save);

            gm.StartGame();
            yield return null;
            gm.GameOver();
            yield return new WaitForSecondsRealtime(gm.Config.deathAnimDuration + 0.2f);
            Assert.AreEqual(GameState.GameOver, gm.CurrentState);

            FindButton("PlayAgainBtn").onClick.Invoke();
            yield return null;
            Assert.AreEqual(GameState.GameOver, gm.CurrentState, "The run must wait for the interstitial to close.");

            yield return new WaitForSecondsRealtime(2.5f);
            Assert.AreEqual(GameState.Playing, gm.CurrentState, "The run starts after the interstitial closes.");
        }

        [UnityTest]
        public IEnumerator UnattendedRun_SpawnsAndCollidesWithoutErrors()
        {
            yield return null;
            yield return null;

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm);
            var spawner = FindIncludingInactive<Upstream.Spawning.SpawnManager>();
            Assert.IsNotNull(spawner);

            gm.StartGame();
            yield return null;

            // ~24s of game time with nobody steering: entities spawn, scroll, hit the fish.
            // Any exception logged during this fails the test.
            Time.timeScale = 4f;
            bool sawEntities = false;
            float end = Time.realtimeSinceStartup + 6f;
            while (Time.realtimeSinceStartup < end)
            {
                if (spawner.ActiveEntities.Count > 0) sawEntities = true;
                if (gm.CurrentState == GameState.GameOver) gm.StartGame();
                yield return null;
            }
            Time.timeScale = 1f;

            Assert.IsTrue(sawEntities, "Spawner should have produced river entities.");
            Assert.Greater(gm.CurrentDistance + gm.LastScore, 0f, "The river should have scrolled.");
        }
    }
}
