using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;
using Upstream.Core;
using Upstream.Mechanics;
using Upstream.UI;

namespace Upstream.Tests
{
    /// <summary>
    /// Plays a busy stretch of a run (late game with big waves) and measures the garbage the game's own
    /// scripts allocate per frame: steady allocations make the garbage collector kick in and show up as
    /// hitches on phones. Probes run first and last in Update so the editor's own allocations
    /// (hundreds of KB per frame in batch mode) don't count.
    /// </summary>
    public class PerformanceTests
    {
        private static long startBytes, total;
        private static bool recording;

        [DefaultExecutionOrder(-32000)]
        public class FirstProbe : MonoBehaviour
        {
            private void Update() => startBytes = Profiler.GetMonoUsedSizeLong();
        }

        [DefaultExecutionOrder(32000)]
        public class LastProbe : MonoBehaviour
        {
            private void Update()
            {
                // A collection during the frame shrinks the heap: ignore that frame rather than count negative.
                if (recording) total += System.Math.Max(0L, Profiler.GetMonoUsedSizeLong() - startBytes);
            }
        }

        private string savePath;
        private string saveBackup;

        [SetUp]
        public void BackupSave()
        {
            savePath = Application.persistentDataPath + "/save.json";
            saveBackup = File.Exists(savePath) ? File.ReadAllText(savePath) : null;
        }

        [TearDown]
        public void RestoreSave()
        {
            recording = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            LocalizationManager.ResetCache();
            ControlSettings.ResetCache();
            UIBackStack.Clear();
            if (saveBackup != null) File.WriteAllText(savePath, saveBackup);
            else if (File.Exists(savePath)) File.Delete(savePath);
        }

        [UnityTest]
        public IEnumerator BusyRun_GameScriptsAllocateAlmostNothingPerFrame()
        {
            yield return null;
            yield return null;

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm);
            var health = Object.FindFirstObjectByType<HealthSystem>(FindObjectsInactive.Include);
            var probe = new GameObject("AllocProbe");
            probe.AddComponent<FirstProbe>();
            probe.AddComponent<LastProbe>();

            gm.StartGame();
            // Jump to the late game (2500 m): more predators, big waves on the way.
            gm.CurrentDistance = 25000f;

            // Warm-up: pools fill up, sprites get generated, first texts are laid out.
            for (int i = 0; i < 120; i++)
            {
                health.SetInvincible(10f);
                yield return null;
            }

            total = 0;
            recording = true;
            int frames = 0;
            for (int i = 0; i < 360 && gm.CurrentState == GameState.Playing; i++)
            {
                health.SetInvincible(10f);
                yield return null;
                frames++;
            }
            recording = false;
            Object.Destroy(probe);

            float avg = frames > 0 ? (float)total / frames : 0f;
            Debug.Log($"[Perf] game scripts: {avg:0} B/frame over {frames} frames");
            Assert.Greater(frames, 100, "The run should keep going while invincible.");
            Assert.Less(avg, 256f, "Game scripts allocate garbage every frame (causes GC hitches on phones).");
        }
    }
}
