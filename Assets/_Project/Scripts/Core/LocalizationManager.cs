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
            { "tips_title", new Entry("NASIL OYNANIR?", "HOW TO PLAY") },
            { "tip_drag", new Entry("Sürükle", "Drag") },
            { "tip_eat", new Entry("Ye ve büyü", "Eat & grow") },
            { "tip_shark", new Entry("Köpekbalığı yut", "Eat sharks") },
            { "tip_coins", new Entry("Para topla", "Get coins") },
            { "play_btn", new Entry("OYNA", "PLAY") },
            { "best_score", new Entry("En iyi: {0} m", "Best: {0} m") },
            { "start_stage_unlocked", new Entry("Artık <b>{0}</b> olarak başlıyorsun", "You now start as <b>{0}</b>") },

            // Game over
            { "wave_toast", new Entry("DALGA! Yavaşladın", "WAVE! Slowed down") },
            { "surge_toast", new Entry("DALGALAR GELİYOR!", "BIG WAVES COMING!") },
            { "surge_hit_toast", new Entry("AKINTI YAVAŞLADI", "CURRENT SLOWED") },
            { "distance_label", new Entry("MESAFE", "DISTANCE") },
            { "game_over_title", new Entry("BALIK YORULDU", "OUT OF BREATH") },
            { "new_best", new Entry("YENİ REKOR!", "NEW BEST!") },
            { "play_again", new Entry("Tekrar Oyna", "Play Again") },
            { "watch_ad_continue", new Entry("Reklam İzle, Devam Et", "Watch Ad to Continue") },
            { "watching_ad", new Entry("Reklam oynatılıyor...", "Playing ad...") },
            { "ad_unavailable", new Entry("Reklam şu an yok", "No ad available") },
            { "unlock_note", new Entry("YENİ: Artık {0} olarak başlıyorsun!", "NEW: You now start as {0}!") },

            // HUD / toasts
            { "turbo_toast", new Entry("TURBO!", "TURBO!") },
            { "heal_toast", new Entry("+{0} CAN", "+{0} HEALTH") },
            { "power_toast", new Entry("+GÜÇ!", "+POWER!") },
            { "devour_toast", new Entry("+{0}m YUTTUN!", "+{0}m DEVOURED!") },
            { "paused", new Entry("DURAKLATILDI", "PAUSED") },
            { "resume_btn", new Entry("DEVAM ET", "RESUME") },

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
            { "sound_label", new Entry("Ses", "Sound") },
            { "vib_label", new Entry("Titreşim", "Vibration") },
            { "lang_label", new Entry("Dil", "Language") },
            { "close_btn", new Entry("Kapat", "Close") },

            // Coins & fish shop
            { "shop_btn", new Entry("BALIKLAR", "FISH") },
            { "shop_title", new Entry("BALIK DÜKKANI", "FISH SHOP") },
            { "buy_for", new Entry("SATIN AL • {0}", "BUY • {0}") },
            { "need_more", new Entry("{0} para eksik", "Need {0} more") },
            { "equip_btn", new Entry("SEÇ", "SELECT") },
            { "equipped", new Entry("KULLANILIYOR", "EQUIPPED") },
            { "equipped_short", new Entry("SEÇİLİ", "IN USE") },
            { "owned", new Entry("SENDE", "OWNED") },
            { "rarity_0", new Entry("Sıradan", "Common") },
            { "rarity_1", new Entry("Seçkin", "Uncommon") },
            { "rarity_2", new Entry("Nadir", "Rare") },
            { "rarity_3", new Entry("Destansı", "Epic") },
            { "rarity_4", new Entry("Efsanevi", "Legendary") },
            { "ability_magnet", new Entry("Mıknatıs: paraları ve yemleri çeker", "Magnet: pulls coins & food in") },
            { "ability_agile", new Entry("Çevik: %{0} daha hızlı döner", "Agile: steers {0}% faster") },
            { "ability_longturbo", new Entry("Uzun turbo: +%{0} süre", "Long turbo: +{0}% time") },
            { "ability_power", new Entry("Güçlü: turbo %{0} hızlı dolar", "Power: turbo fills {0}% faster") },
            { "ability_lucky", new Entry("Şanslı: daha çok yıldız ve para", "Lucky: more stars & coins") },
            { "ability_shield", new Entry("Kalkan: ilk darbeyi engeller", "Shield: blocks the first hit") },
            { "ability_shield2", new Entry("Kalkan: ilk 2 darbeyi engeller", "Shield: blocks the first 2 hits") },
            { "ability_waverider", new Entry("Dalga sörfçüsü: dalgalar yavaşlatmaz", "Wave rider: waves can't slow you") },
            { "ability_hookproof", new Entry("Kanca kıran: kancalar zarar vermez", "Hook breaker: hooks can't hurt you") },
            { "ability_rockbreaker", new Entry("Kaya kıran: kayalar zarar vermez", "Rock breaker: rocks can't hurt you") },
            { "ability_slim", new Entry("İnce: %{0} küçük vuruş alanı", "Slim: {0}% smaller hitbox") },
            { "perk_coin", new Entry("+%{0} para", "+{0}% coins") },
            { "perk_heart", new Entry("+15 can", "+15 health") },
            { "perk_none", new Entry("Başlangıç balığı", "Starter fish") },
            { "next_fish_goal", new Entry("{0} için {1} para daha!", "{1} more coins for {0}!") },
            { "can_buy_fish", new Entry("{0} alabilirsin!", "You can buy {0}!") },
            { "all_fish_owned", new Entry("Tüm balıklar sende!", "You own every fish!") },
            { "coins_earned", new Entry("+{0} para", "+{0} coins") },
            { "menu_btn", new Entry("Ana Menü", "Main Menu") },
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
