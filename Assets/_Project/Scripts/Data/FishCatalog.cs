using System.Collections.Generic;
using UnityEngine;
using Upstream.Core;

namespace Upstream.Data
{
    public enum FishRarity { Common, Uncommon, Rare, Epic, Legendary }

    /// <summary>Body decoration drawn by <see cref="Upstream.River.FishSkinFactory"/>.</summary>
    public enum FishPattern { Plain, Spots, Stripes, Bands, Scales, Patches, Stars, Cracks, Facets, Rainbow }

    /// <summary>One buyable fish: looks (colors, pattern, proportions) plus small gameplay perks.</summary>
    public class FishDef
    {
        public int id;
        public string nameTr;
        public string nameEn;
        public int price;
        public FishRarity rarity;

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

        /// <summary>Extra coins earned per run (0.25 = +25%).</summary>
        public float coinBonus;
        public bool extraHeart;

        public string Name => LocalizationManager.CurrentLanguage == LocalizationManager.Turkish ? nameTr : nameEn;
    }

    /// <summary>
    /// The 20 fish, ordered from the free starter (worst) to the most expensive (best).
    /// IDs are saved in the player's save file: never reorder or reuse them.
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
            return new List<FishDef>
            {
                // ---- COMMON ----
                new FishDef { id = 0, nameTr = "Turuncu Alabalık", nameEn = "Orange Trout", price = 0, rarity = FishRarity.Common,
                    body = C("#FF8A5C"), belly = C("#FFD2B8"), accent = C("#4F1F14"), fin = C("#FFD2B8"), tail = C("#FF8052"), pattern = FishPattern.Spots },
                new FishDef { id = 1, nameTr = "Çamur Balığı", nameEn = "Mudfish", price = 300, rarity = FishRarity.Common,
                    body = C("#8A6A4A"), belly = C("#C9AE8A"), accent = C("#5A4330"), fin = C("#A88A66"), tail = C("#7A5C3E"), pattern = FishPattern.Plain,
                    width = 1.08f, length = 0.95f, coinBonus = 0.02f },
                new FishDef { id = 2, nameTr = "Gümüş Sazan", nameEn = "Silver Carp", price = 600, rarity = FishRarity.Common,
                    body = C("#AFC3CF"), belly = C("#EEF4F7"), accent = C("#7F97A6"), fin = C("#D5E2EA"), tail = C("#9FB4C1"), pattern = FishPattern.Scales,
                    width = 1.05f, coinBonus = 0.04f },
                new FishDef { id = 3, nameTr = "Yeşil Levrek", nameEn = "Green Perch", price = 1000, rarity = FishRarity.Common,
                    body = C("#7FB24A"), belly = C("#E3EFB5"), accent = C("#2F5A1E"), fin = C("#F08A3C"), tail = C("#F08A3C"), pattern = FishPattern.Stripes,
                    coinBonus = 0.06f },

                // ---- UNCOMMON ----
                new FishDef { id = 4, nameTr = "Mavi Çipura", nameEn = "Blue Bream", price = 1500, rarity = FishRarity.Uncommon,
                    body = C("#4C7FC0"), belly = C("#CFE0F5"), accent = C("#F4D35E"), fin = C("#9CC2EE"), tail = C("#3D6BA8"), pattern = FishPattern.Plain,
                    width = 1.12f, length = 0.92f, coinBonus = 0.08f },
                new FishDef { id = 5, nameTr = "Pembe Somon", nameEn = "Pink Salmon", price = 2000, rarity = FishRarity.Uncommon,
                    body = C("#F08FA4"), belly = C("#FFE1E7"), accent = C("#8E3B52"), fin = C("#FFC2CF"), tail = C("#E07890"), pattern = FishPattern.Spots,
                    length = 1.06f, coinBonus = 0.10f },
                new FishDef { id = 6, nameTr = "Kaplan Barbus", nameEn = "Tiger Barb", price = 2700, rarity = FishRarity.Uncommon,
                    body = C("#F5C542"), belly = C("#FFF0B8"), accent = C("#1E1E24"), fin = C("#E5482F"), tail = C("#E5482F"), pattern = FishPattern.Stripes,
                    width = 1.1f, coinBonus = 0.12f },
                new FishDef { id = 7, nameTr = "Neon Tetra", nameEn = "Neon Tetra", price = 3500, rarity = FishRarity.Uncommon,
                    body = C("#2EC4E8"), belly = C("#E8475F"), accent = C("#B9F6FF"), fin = C("#B9F6FF"), tail = C("#E8475F"), pattern = FishPattern.Plain,
                    width = 0.88f, length = 1.08f, glow = new Color(0.4f, 0.95f, 1f, 0.18f), coinBonus = 0.14f },

                // ---- RARE ----
                new FishDef { id = 8, nameTr = "Palyaço Balığı", nameEn = "Clownfish", price = 4500, rarity = FishRarity.Rare,
                    body = C("#FF7B1C"), belly = C("#FFB066"), accent = C("#FFFFFF"), fin = C("#FF7B1C"), tail = C("#FF7B1C"), pattern = FishPattern.Bands,
                    width = 1.06f, length = 0.94f, coinBonus = 0.16f },
                new FishDef { id = 9, nameTr = "Zümrüt Turna", nameEn = "Emerald Pike", price = 5500, rarity = FishRarity.Rare,
                    body = C("#1F9E6E"), belly = C("#BFF0D8"), accent = C("#D9FFB0"), fin = C("#5CCB97"), tail = C("#178056"), pattern = FishPattern.Spots,
                    width = 0.9f, length = 1.14f, coinBonus = 0.18f },
                new FishDef { id = 10, nameTr = "Mor Beta", nameEn = "Purple Betta", price = 6500, rarity = FishRarity.Rare,
                    body = C("#7B3FC4"), belly = C("#C9A6F2"), accent = C("#E05FD0"), fin = C("#E05FD0"), tail = C("#B04BD8"), pattern = FishPattern.Scales,
                    tailSize = 1.45f, coinBonus = 0.20f },
                new FishDef { id = 11, nameTr = "Buz Balığı", nameEn = "Ice Fish", price = 8000, rarity = FishRarity.Rare,
                    body = C("#BFEFFF"), belly = C("#FFFFFF"), accent = C("#7FD3F0"), fin = C("#E6FAFF"), tail = C("#9EDFF5"), pattern = FishPattern.Facets,
                    alpha = 0.88f, glow = new Color(0.75f, 0.95f, 1f, 0.22f), coinBonus = 0.22f },

                // ---- EPIC ----
                new FishDef { id = 12, nameTr = "Lav Balığı", nameEn = "Lava Fish", price = 9500, rarity = FishRarity.Epic,
                    body = C("#3A1612"), belly = C("#6B241A"), accent = C("#FF7A1A"), fin = C("#FF5A1F"), tail = C("#FF5A1F"), pattern = FishPattern.Cracks,
                    glow = new Color(1f, 0.42f, 0.1f, 0.30f), coinBonus = 0.25f },
                new FishDef { id = 13, nameTr = "Altın Koi", nameEn = "Golden Koi", price = 11000, rarity = FishRarity.Epic,
                    body = C("#F2B530"), belly = C("#FFE8A3"), accent = C("#FFFFFF"), fin = C("#FFF3D1"), tail = C("#F2B530"), pattern = FishPattern.Patches,
                    width = 1.05f, length = 1.05f, tailSize = 1.2f, glow = new Color(1f, 0.85f, 0.35f, 0.22f), coinBonus = 0.28f },
                new FishDef { id = 14, nameTr = "Gece Balığı", nameEn = "Midnight Fish", price = 13000, rarity = FishRarity.Epic,
                    body = C("#1B2350"), belly = C("#3A4A8C"), accent = C("#FFF6C8"), fin = C("#5B6BD6"), tail = C("#2B3678"), pattern = FishPattern.Stars,
                    glow = new Color(0.45f, 0.5f, 1f, 0.25f), coinBonus = 0.30f },
                new FishDef { id = 15, nameTr = "Zehirli Balon", nameEn = "Toxic Puffer", price = 15000, rarity = FishRarity.Epic,
                    body = C("#9BE53A"), belly = C("#E9FFC2"), accent = C("#7A2DA8"), fin = C("#C6F57C"), tail = C("#7CC421"), pattern = FishPattern.Spots,
                    width = 1.3f, length = 0.88f, glow = new Color(0.65f, 1f, 0.2f, 0.22f), coinBonus = 0.33f },
                new FishDef { id = 16, nameTr = "Hayalet Balık", nameEn = "Ghost Fish", price = 18000, rarity = FishRarity.Epic,
                    body = C("#F4F8FF"), belly = C("#FFFFFF"), accent = C("#B9C8E8"), fin = C("#E6EEFF"), tail = C("#DCE6FA"), pattern = FishPattern.Plain,
                    alpha = 0.62f, tailSize = 1.25f, glow = new Color(0.85f, 0.92f, 1f, 0.35f), coinBonus = 0.36f },

                // ---- LEGENDARY ----
                new FishDef { id = 17, nameTr = "Ejder Balığı", nameEn = "Dragon Fish", price = 21000, rarity = FishRarity.Legendary,
                    body = C("#C8102E"), belly = C("#FFC94A"), accent = C("#FFD86B"), fin = C("#FFC94A"), tail = C("#A00D25"), pattern = FishPattern.Scales,
                    length = 1.12f, tailSize = 1.3f, glow = new Color(1f, 0.3f, 0.15f, 0.38f), coinBonus = 0.40f, extraHeart = true },
                new FishDef { id = 18, nameTr = "Kristal Balık", nameEn = "Crystal Fish", price = 25000, rarity = FishRarity.Legendary,
                    body = C("#8FE3FF"), belly = C("#F0FCFF"), accent = C("#FFFFFF"), fin = C("#C9F3FF"), tail = C("#6FD0F5"), pattern = FishPattern.Facets,
                    width = 1.05f, glow = new Color(0.6f, 0.95f, 1f, 0.42f), coinBonus = 0.45f, extraHeart = true },
                new FishDef { id = 19, nameTr = "Gökkuşağı Efsanesi", nameEn = "Rainbow Legend", price = 30000, rarity = FishRarity.Legendary,
                    body = C("#FFFFFF"), belly = C("#FFFFFF"), accent = C("#FFFFFF"), fin = C("#FFFFFF"), tail = C("#FFFFFF"), pattern = FishPattern.Rainbow,
                    length = 1.08f, tailSize = 1.35f, glow = new Color(1f, 1f, 1f, 0.42f), coinBonus = 0.50f, extraHeart = true },
            };
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
