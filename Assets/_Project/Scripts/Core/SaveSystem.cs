using System;
using System.IO;
using UnityEngine;

namespace Upstream.Core
{
    [Serializable]
    public class SaveData
    {
        public int version;
        public int bestScore;
        public int totalGamesPlayed;
        public bool settingsSoundOn = true;
        public bool settingsVibrationOn = true;
        /// <summary>"" = not chosen yet: the phone language decides on first launch.</summary>
        public string settingsLanguage = "";
        public int unlockedStartStage;
        public int totalSharksEaten;
        // v2
        /// <summary>Highest growth stage index ever reached in any run (drives start-stage unlocks).</summary>
        public int bestStageReached;
        /// <summary>Set by the future "Remove Ads" purchase; disables interstitials (rewarded stays opt-in).</summary>
        public bool adsRemoved;
    }

    public static class SaveSystem
    {
        private const int CurrentVersion = 2;
        private static string SavePath => Application.persistentDataPath + "/save.json";
        private static object fileLock = new object();

        public static SaveData Load()
        {
            lock (fileLock)
            {
                SaveData data = new SaveData { version = CurrentVersion };
                if (File.Exists(SavePath))
                {
                    try
                    {
                        string json = File.ReadAllText(SavePath);
                        // An empty/truncated file makes FromJson return null; fall back to defaults.
                        var loaded = string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<SaveData>(json);
                        if (loaded != null)
                        {
                            data = loaded;
                            if (data.version < CurrentVersion)
                            {
                                Migrate(data);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to load save file: {e.Message}");
                    }
                }
                return data;
            }
        }

        public static void Save(SaveData data)
        {
            lock (fileLock)
            {
                try
                {
                    if (data == null) return;
                    data.version = CurrentVersion;
                    string json = JsonUtility.ToJson(data, true);

                    // Write to a temp file first so a crash/kill mid-write can't leave a corrupt save.
                    string tmpPath = SavePath + ".tmp";
                    File.WriteAllText(tmpPath, json);
                    if (File.Exists(SavePath)) File.Delete(SavePath);
                    File.Move(tmpPath, SavePath);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to save file: {e.Message}");
                }
            }
        }

        public static void Reset()
        {
            lock (fileLock)
            {
                try
                {
                    if (File.Exists(SavePath))
                    {
                        File.Delete(SavePath);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to delete save file: {e.Message}");
                }
            }
        }

        public static void Migrate(SaveData data)
        {
            // v1 -> v2: new fields default to 0/false, which is correct for old players.
            // A v1 player who had already unlocked a start stage keeps it.
            if (data.version < 2)
            {
                data.bestStageReached = Mathf.Max(data.bestStageReached, data.unlockedStartStage);
            }
            data.version = CurrentVersion;
        }
    }
}
