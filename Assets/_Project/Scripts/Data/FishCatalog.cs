using System.Collections.Generic;
using UnityEngine;
using Upstream.Core;

namespace Upstream.Data
{
    public enum FishRarity { Common, Uncommon, Rare, Epic, Legendary }

    /// <summary>Body decoration drawn by <see cref="Upstream.River.FishSkinFactory"/>.</summary>
    public enum FishPattern { Plain, Spots, Stripes, Bands, Scales, Patches, Stars, Cracks, Facets, Rainbow }

    /// <summary>
    /// Each fish's special power. Strength grows with rarity (see the Ability* helpers on <see cref="FishDef"/>).
    /// </summary>
    public enum FishAbility
    {
        None,
        Magnet,       // pulls coins and food in
        Agile,        // steers faster
        LongTurbo,    // turbo lasts longer
        Power,        // turbo bar fills faster
        Lucky,        // more gold stars and coin trails
        Shield,       // blocks the first hit(s) of each run
        WaveRider,    // waves don't slow it down
        HookProof,    // hooks can't hurt it
        RockBreaker,  // rocks can't hurt it
        Slim          // smaller hitbox
    }

    /// <summary>One buyable fish: looks (colors, pattern, proportions), a special ability and small perks.</summary>
    public class FishDef
    {
        public int id;
        public string nameTr;
        public string nameEn;
        public int price;
        public FishRarity rarity;
        public FishAbility ability;

        public Color body;
        public Color belly;
        public Color accent;   // pattern color
        public Color fin;
        public Color tail;
        public FishPattern pattern;

        public float width = 1f;      // body width multiplier
        public float length = 1f;     // body length multiplier
        public float tailSize = 1f;
        public float alpha = 1f;      // < 1 = see-through (ghost)
        public Color glow = Color.clear; // aura behind the fish; alpha 0 = none

        /// <summary>Extra coins earned per run (0.25 = +25%). Assigned by catalog order: better fish earn more.</summary>
        public float coinBonus;
        public bool extraHeart;

        public string Name => LocalizationManager.CurrentLanguage == LocalizationManager.Turkish ? nameTr : nameEn;

        private int Tier => (int)rarity; // 0 common .. 4 legendary

        public bool Has(FishAbility a) => ability == a;
        /// <summary>Light pull every fish has, so coins and food right next to it drift in.</summary>
        public const float BaseMagnetRadius = 28f;
        /// <summary>Magnet pull radius (world units): the light base pull, much wider for magnet fish.</summary>
        public float MagnetRadius => Has(FishAbility.Magnet) ? 45f + 20f * Tier : BaseMagnetRadius;
        /// <summary>Steering speed multiplier.</summary>
        public float SteeringMultiplier => Has(FishAbility.Agile) ? 1.15f + 0.1f * Tier : 1f;
        /// <summary>Turbo duration multiplier.</summary>
        public float TurboTimeMultiplier => Has(FishAbility.LongTurbo) ? 1.2f + 0.1f * Tier : 1f;
        /// <summary>Power bar fill multiplier.</summary>
        public float PowerMultiplier => Has(FishAbility.Power) ? 1.15f + 0.1f * Tier : 1f;
        /// <summary>Gold star / coin trail chance multiplier.</summary>
        public float LuckMultiplier => Has(FishAbility.Lucky) ? 1.5f + 0.35f * Tier : 1f;
        /// <summary>Hits blocked at the start of every run.</summary>
        public int ShieldCharges => Has(FishAbility.Shield) ? (rarity == FishRarity.Legendary ? 2 : 1) : 0;
        /// <summary>Hitbox multiplier.</summary>
        public float HitboxMultiplier => Has(FishAbility.Slim) ? 0.92f - 0.04f * Tier : 1f;
    }

    /// <summary>
    /// The 30 fish, ordered from the free starter (worst) to the most expensive (best).
    /// IDs are saved in the player's save file: never change or reuse them (new fish get new IDs,
    /// they can sit anywhere in the order).
    /// </summary>
    public static class FishCatalog
    {
        public const int StarterId = 0;

        private static List<FishDef> all;

        public static IReadOnlyList<FishDef> All => all ??= Build();
        public static int Count => All.Count;

        public static FishDef Get(int id)
        {
            var list = All;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].id == id) return list[i];
            }
            return list[0];
        }

        private static Color C(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }

        private static List<FishDef> Build()
        {
            var list = new List<FishDef>
            {
                // ---- COMMON ----
                // The hero of Blue's River (same look as the app icon).
                new FishDef { id = 0, nameTr = "Blue", nameEn = "Blue", price = 0, rarity = FishRarity.Common, ability = FishAbility.None,
                    body = C("#3E9BF0"), belly = C("#CDEBFF"), accent = C("#1B4F9C"), fin = C("#CDEBFF"), tail = C("#FF6B5E"), pattern = FishPattern.Spots },
                new FishDef { id = 1, nameTr = "Çamur Balığı", nameEn = "Mudfish", price = 300, rarity = FishRarity.Common, ability = FishAbility.Slim,
                    body = C("#8A6A4A"), belly = C("#C9AE8A"), accent = C("#5A4330"), fin = C("#A88A66"), tail = C("#7A5C3E"), pattern = FishPattern.Plain,
                    width = 0.92f, length = 1.02f },
                new FishDef { id = 20, nameTr = "Kızıl Yüzgeç", nameEn = "Redfin", price = 450, rarity = FishRarity.Common, ability = FishAbility.Power,
                    body = C("#B8C7B0"), belly = C("#EEF3EA"), accent = C("#7E9277"), fin = C("#E5482F"), tail = C("#E5482F"), pattern = FishPattern.Scales },
                new FishDef { id = 2, nameTr = "Gümüş Sazan", nameEn = "Silver Carp", price = 600, rarity = FishRarity.Common, ability = FishAbility.Magnet,
                    body = C("#AFC3CF"), belly = C("#EEF4F7"), accent = C("#7F97A6"), fin = C("#D5E2EA"), tail = C("#9FB4C1"), pattern = FishPattern.Scales,
                    width = 1.05f },
                new FishDef { id = 3, nameTr = "Yeşil Levrek", nameEn = "Green Perch", price = 1000, rarity = FishRarity.Common, ability = FishAbility.Agile,
                    body = C("#7FB24A"), belly = C("#E3EFB5"), accent = C("#2F5A1E"), fin = C("#F08A3C"), tail = C("#F08A3C"), pattern = FishPattern.Stripes },

                // ---- UNCOMMON ----
                new FishDef { id = 4, nameTr = "Mavi Çipura", nameEn = "Blue Bream", price = 1500, rarity = FishRarity.Uncommon, ability = FishAbility.LongTurbo,
                    body = C("#4C7FC0"), belly = C("#CFE0F5"), accent = C("#F4D35E"), fin = C("#9CC2EE"), tail = C("#3D6BA8"), pattern = FishPattern.Plain,
                    width = 1.12f, length = 0.92f },
                new FishDef { id = 23, nameTr = "Kelebek Balığı", nameEn = "Butterflyfish", price = 1800, rarity = FishRarity.Uncommon, ability = FishAbility.Lucky,
                    body = C("#FFD93D"), belly = C("#FFF3B0"), accent = C("#1E1E24"), fin = C("#FFFFFF"), tail = C("#FFC300"), pattern = FishPattern.Spots,
                    width = 1.18f, length = 0.9f },
                new FishDef { id = 5, nameTr = "Pembe Somon", nameEn = "Pink Salmon", price = 2000, rarity = FishRarity.Uncommon, ability = FishAbility.Power,
                    body = C("#F08FA4"), belly = C("#FFE1E7"), accent = C("#8E3B52"), fin = C("#FFC2CF"), tail = C("#E07890"), pattern = FishPattern.Spots,
                    length = 1.06f },
                new FishDef { id = 6, nameTr = "Kaplan Barbus", nameEn = "Tiger Barb", price = 2700, rarity = FishRarity.Uncommon, ability = FishAbility.Agile,
                    body = C("#F5C542"), belly = C("#FFF0B8"), accent = C("#1E1E24"), fin = C("#E5482F"), tail = C("#E5482F"), pattern = FishPattern.Stripes,
                    width = 1.1f },
                new FishDef { id = 7, nameTr = "Neon Tetra", nameEn = "Neon Tetra", price = 3500, rarity = FishRarity.Uncommon, ability = FishAbility.Magnet,
                    body = C("#2EC4E8"), belly = C("#E8475F"), accent = C("#B9F6FF"), fin = C("#B9F6FF"), tail = C("#E8475F"), pattern = FishPattern.Plain,
                    width = 0.88f, length = 1.08f, glow = new Color(0.4f, 0.95f, 1f, 0.18f) },
                new FishDef { id = 21, nameTr = "Sarı Kuyruk", nameEn = "Yellowtail", price = 4000, rarity = FishRarity.Uncommon, ability = FishAbility.WaveRider,
                    body = C("#5E86B8"), belly = C("#E6EEF8"), accent = C("#FFD23F"), fin = C("#FFD23F"), tail = C("#FFD23F"), pattern = FishPattern.Stripes,
                    width = 0.95f, length = 1.08f },

                // ---- RARE ----
                new FishDef { id = 8, nameTr = "Palyaço Balığı", nameEn = "Clownfish", price = 4500, rarity = FishRarity.Rare, ability = FishAbility.Shield,
                    body = C("#FF7B1C"), belly = C("#FFB066"), accent = C("#FFFFFF"), fin = C("#FF7B1C"), tail = C("#FF7B1C"), pattern = FishPattern.Bands,
                    width = 1.06f, length = 0.94f },
                new FishDef { id = 29, nameTr = "Kraliyet Mavisi", nameEn = "Royal Tang", price = 5000, rarity = FishRarity.Rare, ability = FishAbility.Lucky,
                    body = C("#2F6FE8"), belly = C("#7FB2FF"), accent = C("#0E1B3D"), fin = C("#2F6FE8"), tail = C("#FFD23F"), pattern = FishPattern.Patches,
                    width = 1.1f },
                new FishDef { id = 9, nameTr = "Zümrüt Turna", nameEn = "Emerald Pike", price = 5500, rarity = FishRarity.Rare, ability = FishAbility.Agile,
                    body = C("#1F9E6E"), belly = C("#BFF0D8"), accent = C("#D9FFB0"), fin = C("#5CCB97"), tail = C("#178056"), pattern = FishPattern.Spots,
                    width = 0.9f, length = 1.14f },
                new FishDef { id = 10, nameTr = "Mor Beta", nameEn = "Purple Betta", price = 6500, rarity = FishRarity.Rare, ability = FishAbility.LongTurbo,
                    body = C("#7B3FC4"), belly = C("#C9A6F2"), accent = C("#E05FD0"), fin = C("#E05FD0"), tail = C("#B04BD8"), pattern = FishPattern.Scales,
                    tailSize = 1.45f },
                new FishDef { id = 25, nameTr = "Uçan Balık", nameEn = "Flying Fish", price = 7200, rarity = FishRarity.Rare, ability = FishAbility.WaveRider,
                    body = C("#3A8DDE"), belly = C("#E3F2FF"), accent = C("#BFE3FF"), fin = C("#A9D8FF"), tail = C("#2D72B8"), pattern = FishPattern.Scales,
                    width = 0.9f, length = 1.1f, tailSize = 1.3f },
                new FishDef { id = 11, nameTr = "Buz Balığı", nameEn = "Ice Fish", price = 8000, rarity = FishRarity.Rare, ability = FishAbility.Shield,
                    body = C("#BFEFFF"), belly = C("#FFFFFF"), accent = C("#7FD3F0"), fin = C("#E6FAFF"), tail = C("#9EDFF5"), pattern = FishPattern.Facets,
                    alpha = 0.88f, glow = new Color(0.75f, 0.95f, 1f, 0.22f) },
                new FishDef { id = 22, nameTr = "Melek Balığı", nameEn = "Angelfish", price = 8800, rarity = FishRarity.Rare, ability = FishAbility.Magnet,
                    body = C("#F2F2F2"), belly = C("#FFFFFF"), accent = C("#1E1E24"), fin = C("#FFE066"), tail = C("#FFE066"), pattern = FishPattern.Stripes,
                    width = 1.28f, length = 0.86f, tailSize = 1.2f },

                // ---- EPIC ----
                new FishDef { id = 12, nameTr = "Lav Balığı", nameEn = "Lava Fish", price = 9500, rarity = FishRarity.Epic, ability = FishAbility.RockBreaker,
                    body = C("#3A1612"), belly = C("#6B241A"), accent = C("#FF7A1A"), fin = C("#FF5A1F"), tail = C("#FF5A1F"), pattern = FishPattern.Cracks,
                    glow = new Color(1f, 0.42f, 0.1f, 0.30f) },
                new FishDef { id = 13, nameTr = "Altın Koi", nameEn = "Golden Koi", price = 11000, rarity = FishRarity.Epic, ability = FishAbility.Lucky,
                    body = C("#F2B530"), belly = C("#FFE8A3"), accent = C("#FFFFFF"), fin = C("#FFF3D1"), tail = C("#F2B530"), pattern = FishPattern.Patches,
                    width = 1.05f, length = 1.05f, tailSize = 1.2f, glow = new Color(1f, 0.85f, 0.35f, 0.22f) },
                new FishDef { id = 24, nameTr = "Mandarin Balığı", nameEn = "Mandarin Fish", price = 12000, rarity = FishRarity.Epic, ability = FishAbility.Power,
                    body = C("#FF8A1F"), belly = C("#FFC46B"), accent = C("#2B6CF0"), fin = C("#2B6CF0"), tail = C("#2B6CF0"), pattern = FishPattern.Stripes,
                    width = 1.08f, glow = new Color(1f, 0.6f, 0.2f, 0.22f) },
                new FishDef { id = 14, nameTr = "Gece Balığı", nameEn = "Midnight Fish", price = 13000, rarity = FishRarity.Epic, ability = FishAbility.Magnet,
                    body = C("#1B2350"), belly = C("#3A4A8C"), accent = C("#FFF6C8"), fin = C("#5B6BD6"), tail = C("#2B3678"), pattern = FishPattern.Stars,
                    glow = new Color(0.45f, 0.5f, 1f, 0.25f) },
                new FishDef { id = 15, nameTr = "Zehirli Balon", nameEn = "Toxic Puffer", price = 15000, rarity = FishRarity.Epic, ability = FishAbility.HookProof,
                    body = C("#9BE53A"), belly = C("#E9FFC2"), accent = C("#7A2DA8"), fin = C("#C6F57C"), tail = C("#7CC421"), pattern = FishPattern.Spots,
                    width = 1.3f, length = 0.88f, glow = new Color(0.65f, 1f, 0.2f, 0.22f) },
                new FishDef { id = 26, nameTr = "Aslan Balığı", nameEn = "Lionfish", price = 16500, rarity = FishRarity.Epic, ability = FishAbility.Shield,
                    body = C("#E8463A"), belly = C("#FFC9B8"), accent = C("#FFF4EC"), fin = C("#FF9E8A"), tail = C("#E8463A"), pattern = FishPattern.Bands,
                    tailSize = 1.35f, glow = new Color(1f, 0.4f, 0.35f, 0.22f) },
                new FishDef { id = 16, nameTr = "Hayalet Balık", nameEn = "Ghost Fish", price = 18000, rarity = FishRarity.Epic, ability = FishAbility.Slim,
                    body = C("#F4F8FF"), belly = C("#FFFFFF"), accent = C("#B9C8E8"), fin = C("#E6EEFF"), tail = C("#DCE6FA"), pattern = FishPattern.Plain,
                    alpha = 0.62f, tailSize = 1.25f, glow = new Color(0.85f, 0.92f, 1f, 0.35f) },
                new FishDef { id = 28, nameTr = "Şimşek Balığı", nameEn = "Lightning Fish", price = 19500, rarity = FishRarity.Epic, ability = FishAbility.LongTurbo,
                    body = C("#4B2A8C"), belly = C("#7A55C9"), accent = C("#FFE433"), fin = C("#FFE433"), tail = C("#FFD000"), pattern = FishPattern.Cracks,
                    glow = new Color(1f, 0.92f, 0.3f, 0.32f) },

                // ---- LEGENDARY ----
                new FishDef { id = 17, nameTr = "Ejder Balığı", nameEn = "Dragon Fish", price = 21000, rarity = FishRarity.Legendary, ability = FishAbility.RockBreaker,
                    body = C("#C8102E"), belly = C("#FFC94A"), accent = C("#FFD86B"), fin = C("#FFC94A"), tail = C("#A00D25"), pattern = FishPattern.Scales,
                    length = 1.12f, tailSize = 1.3f, glow = new Color(1f, 0.3f, 0.15f, 0.38f), extraHeart = true },
                new FishDef { id = 27, nameTr = "Ay Balığı", nameEn = "Moonfish", price = 23000, rarity = FishRarity.Legendary, ability = FishAbility.Shield,
                    body = C("#D7DEEA"), belly = C("#FFFFFF"), accent = C("#9AA8C4"), fin = C("#F0F4FA"), tail = C("#C2CCDD"), pattern = FishPattern.Facets,
                    width = 1.35f, length = 0.86f, glow = new Color(0.9f, 0.95f, 1f, 0.42f), extraHeart = true },
                new FishDef { id = 18, nameTr = "Kristal Balık", nameEn = "Crystal Fish", price = 25000, rarity = FishRarity.Legendary, ability = FishAbility.Agile,
                    body = C("#8FE3FF"), belly = C("#F0FCFF"), accent = C("#FFFFFF"), fin = C("#C9F3FF"), tail = C("#6FD0F5"), pattern = FishPattern.Facets,
                    width = 1.05f, glow = new Color(0.6f, 0.95f, 1f, 0.42f), extraHeart = true },
                new FishDef { id = 19, nameTr = "Gökkuşağı Efsanesi", nameEn = "Rainbow Legend", price = 30000, rarity = FishRarity.Legendary, ability = FishAbility.Magnet,
                    body = C("#FFFFFF"), belly = C("#FFFFFF"), accent = C("#FFFFFF"), fin = C("#FFFFFF"), tail = C("#FFFFFF"), pattern = FishPattern.Rainbow,
                    length = 1.08f, tailSize = 1.35f, glow = new Color(1f, 1f, 1f, 0.42f), extraHeart = true },
            };

            // Coin bonus climbs steadily with the fish's place in the line-up (0% .. +50%).
            for (int i = 0; i < list.Count; i++)
            {
                list[i].coinBonus = Mathf.Round(50f * i / (list.Count - 1)) / 100f;
            }
            return list;
        }

        public static Color RarityColor(FishRarity rarity)
        {
            switch (rarity)
            {
                case FishRarity.Uncommon: return new Color(0.42f, 0.86f, 0.52f, 1f);
                case FishRarity.Rare: return new Color(0.36f, 0.66f, 1f, 1f);
                case FishRarity.Epic: return new Color(0.72f, 0.47f, 1f, 1f);
                case FishRarity.Legendary: return new Color(1f, 0.78f, 0.25f, 1f);
                default: return new Color(0.72f, 0.78f, 0.84f, 1f);
            }
        }
    }
}
