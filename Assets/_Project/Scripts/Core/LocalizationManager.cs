using System;
using System.Collections.Generic;
using UnityEngine;

namespace Upstream.Core
{
    /// <summary>
    /// Lightweight localization system supporting Turkish (tr) and English (en) with live language switching.
    /// Keys mirror what a Unity Localization string table would use, so migrating later is mechanical.
    /// To add a language: add a column to <see cref="Entry"/>, fill it for every key, extend <see cref="Languages"/>.
    /// </summary>
    public static class LocalizationManager
    {
        public const string Turkish = "tr";
        public const string English = "en";
        public static readonly string[] Languages = { Turkish, English };

        public static event Action OnLanguageChanged;

        private static string currentLang;

        private readonly struct Entry
        {
            public readonly string Tr;
            public readonly string En;
            public Entry(string tr, string en) { Tr = tr; En = en; }
        }

        private static readonly Dictionary<string, Entry> Table = new Dictionary<string, Entry>
        {
            // Menu
            { "title", new Entry("AKINTIYA\n<color=#FF8A5C>KARŞI</color>", "SWIM\n<color=#FF8A5C>UPSTREAM</color>") },
            { "how_to", new Entry("Parmağını sürükle, balığı yönlendir.\nYemleri ye, büyü ve köpekbalıklarını yut!", "Drag to steer your fish.\nEat shrimp, grow bigger and devour sharks!") },
            { "tap_to_start", new Entry("Başlamak için dokun", "Tap to start") },
            { "best_score", new Entry("En iyi: {0} m", "Best: {0} m") },
            { "start_stage_unlocked", new Entry("Artık <b>{0}</b> olarak başlıyorsun", "You now start as <b>{0}</b>") },

            // Game over
            { "game_over_title", new Entry("BALIK YORULDU", "OUT OF BREATH") },
            { "new_best", new Entry("YENİ REKOR!", "NEW BEST!") },
            { "play_again", new Entry("Tekrar Oyna", "Play Again") },
            { "watch_ad_continue", new Entry("Reklam İzle, Devam Et", "Watch Ad to Continue") },
            { "watching_ad", new Entry("Reklam oynatılıyor...", "Playing ad...") },
            { "ad_unavailable", new Entry("Reklam şu an yok", "No ad available") },
            { "unlock_note", new Entry("YENİ: Artık {0} olarak başlıyorsun!", "NEW: You now start as {0}!") },

            // HUD / toasts
            { "sound_short_on", new Entry("SES", "SOUND") },
            { "sound_short_off", new Entry("KAPALI", "OFF") },
            { "turbo_toast", new Entry("TURBO!", "TURBO!") },
            { "heal_toast", new Entry("+1 CAN", "+1 LIFE") },
            { "power_toast", new Entry("+GÜÇ!", "+POWER!") },
            { "devour_toast", new Entry("+{0}m YUTTUN!", "+{0}m DEVOURED!") },
            { "paused", new Entry("DURAKLATILDI\n<size=55%>Devam etmek için dokun</size>", "PAUSED\n<size=55%>Tap to continue</size>") },

            // Growth stages (names / HUD badge / stage-up toast)
            { "stage_name_0", new Entry("YAVRU", "FRY") },
            { "stage_name_1", new Entry("GENÇ", "YOUNG") },
            { "stage_name_2", new Entry("YETİŞKİN", "ADULT") },
            { "stage_name_3", new Entry("ALFA", "ALPHA") },
            { "stage_name_4", new Entry("DEV TİTAN", "GIANT TITAN") },
            { "stage_badge_0", new Entry("1. YAVRU", "1. FRY") },
            { "stage_badge_1", new Entry("2. GENÇ", "2. YOUNG") },
            { "stage_badge_2", new Entry("3. YETİŞKİN (Turbo = Avcı)", "3. ADULT (Turbo = Hunter)") },
            { "stage_badge_3", new Entry("4. ALFA (Köpekbalığı Avcısı!)", "4. ALPHA (Shark Hunter!)") },
            { "stage_badge_4", new Entry("5. DEV TİTAN!", "5. GIANT TITAN!") },
            { "stage_up_1", new Entry("GENÇ!", "YOUNG!") },
            { "stage_up_2", new Entry("YETİŞKİN!", "ADULT!") },
            { "stage_up_3", new Entry("ALFA!", "ALPHA!") },
            { "stage_up_4", new Entry("DEV TİTAN!", "GIANT TITAN!") },

            // Settings
            { "settings_title", new Entry("AYARLAR", "SETTINGS") },
            { "sound_on", new Entry("Ses: AÇIK", "Sound: ON") },
            { "sound_off", new Entry("Ses: KAPALI", "Sound: OFF") },
            { "vib_on", new Entry("Titreşim: AÇIK", "Vibration: ON") },
            { "vib_off", new Entry("Titreşim: KAPALI", "Vibration: OFF") },
            { "lang_btn", new Entry("Dil: Türkçe", "Language: English") },
            { "close_btn", new Entry("Kapat", "Close") },
        };

        public static IEnumerable<string> Keys => Table.Keys;

        public static string CurrentLanguage
        {
            get
            {
                if (string.IsNullOrEmpty(currentLang))
                {
                    var save = SaveSystem.Load();
                    currentLang = IsSupported(save.settingsLanguage)
                        ? save.settingsLanguage
                        : DefaultLanguageFor(Application.systemLanguage);
                }
                return currentLang;
            }
        }

        public static bool IsSupported(string langCode) => langCode == Turkish || langCode == English;

        /// <summary>First launch: Turkish phones get Turkish, everyone else English (global market).</summary>
        public static string DefaultLanguageFor(SystemLanguage systemLanguage)
        {
            return systemLanguage == SystemLanguage.Turkish ? Turkish : English;
        }

        public static void SetLanguage(string langCode)
        {
            currentLang = IsSupported(langCode) ? langCode : English;
            var save = SaveSystem.Load();
            save.settingsLanguage = currentLang;
            SaveSystem.Save(save);
            OnLanguageChanged?.Invoke();
        }

        public static void ToggleLanguage()
        {
            SetLanguage(CurrentLanguage == Turkish ? English : Turkish);
        }

        public static string Get(string key)
        {
            return Get(key, CurrentLanguage);
        }

        public static string Get(string key, string langCode)
        {
            if (Table.TryGetValue(key, out var entry))
            {
                return langCode == English ? entry.En : entry.Tr;
            }
            Debug.LogWarning($"[Localization] Missing key '{key}'");
            return key;
        }

        public static string Format(string key, object arg0)
        {
            return string.Format(Get(key), arg0);
        }

        /// <summary>Clears the cached language (tests / save reset).</summary>
        public static void ResetCache()
        {
            currentLang = null;
        }
    }
}
