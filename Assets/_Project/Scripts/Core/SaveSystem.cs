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
        public string settingsLanguage = "en";
        public int unlockedStartStage;
        public int totalSharksEaten;
    }

    public static class SaveSystem
    {
        private const int CurrentVersion = 1;
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
                        data = JsonUtility.FromJson<SaveData>(json);

                        if (data.version < CurrentVersion)
                        {
                            Migrate(data);
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
                    data.version = CurrentVersion;
                    string json = JsonUtility.ToJson(data, true);
                    File.WriteAllText(SavePath, json);
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

        private static void Migrate(SaveData data)
        {
            data.version = CurrentVersion;
            // Add migration logic for older versions if necessary.
        }
    }
}
