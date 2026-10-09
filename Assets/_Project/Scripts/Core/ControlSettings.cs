using System;

namespace Upstream.Core
{
    /// <summary>
    /// Steering sensitivity (Settings: Low / Medium / High), saved with the other settings.
    /// The fish moves by the finger's sideways drag times <see cref="Gain"/>, so a higher level means
    /// less finger travel for the same move.
    /// </summary>
    public static class ControlSettings
    {
        public const int Low = 0, Medium = 1, High = 2;

        // Finger-to-fish ratio per level. At Medium a drag across ~30% of the screen crosses the river.
        private static readonly float[] Gains = { 1.2f, 1.6f, 2.2f };

        private static int level = -1;

        public static event Action OnChanged;

        public static int Level
        {
            get
            {
                if (level < 0) level = Clamp(SaveSystem.Load().settingsSensitivity);
                return level;
            }
        }

        public static float Gain => GainFor(Level);

        public static float GainFor(int sensitivityLevel) => Gains[Clamp(sensitivityLevel)];

        public static void SetLevel(int sensitivityLevel)
        {
            level = Clamp(sensitivityLevel);
            var save = SaveSystem.Load();
            save.settingsSensitivity = level;
            SaveSystem.Save(save);
            OnChanged?.Invoke();
        }

        /// <summary>Forget the cached level (tests / save reset).</summary>
        public static void ResetCache() => level = -1;

        private static int Clamp(int l) => l < Low ? Low : l > High ? High : l;
    }
}
