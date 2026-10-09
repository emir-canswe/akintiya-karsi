using System;
using UnityEngine;
using Upstream.Data;

namespace Upstream.Core
{
    public enum BuyResult { Bought, AlreadyOwned, NotEnoughCoins }

    /// <summary>
    /// Coins and the fish shop. The pure functions work on a <see cref="SaveData"/> (unit-testable);
    /// the instance-free wrappers load, change and save the file, then raise the events.
    /// </summary>
    public static class FishShop
    {
        /// <summary>Meters of distance per coin, paid out at the end of a run.</summary>
        public const int MetersPerCoin = 25;
        public const int SharkCoins = 10;

        public static event Action<int> OnBalanceChanged;
        public static event Action<FishDef> OnFishEquipped;

        // ---- pure logic ----

        public static bool Owns(SaveData save, int fishId) =>
            fishId == FishCatalog.StarterId || (save.ownedFish != null && save.ownedFish.Contains(fishId));

        public static BuyResult TryBuy(SaveData save, FishDef fish)
        {
            if (Owns(save, fish.id)) return BuyResult.AlreadyOwned;
            if (save.coins < fish.price) return BuyResult.NotEnoughCoins;
            save.coins -= fish.price;
            if (save.ownedFish == null) save.ownedFish = new System.Collections.Generic.List<int>();
            save.ownedFish.Add(fish.id);
            return BuyResult.Bought;
        }

        public static bool TryEquip(SaveData save, int fishId)
        {
            if (!Owns(save, fishId)) return false;
            save.equippedFish = fishId;
            return true;
        }

        /// <summary>Coins a run pays: pickups + distance, multiplied by the equipped fish's bonus.</summary>
        public static int RunReward(int pickupCoins, int meters, float coinBonus)
        {
            int baseCoins = Mathf.Max(0, pickupCoins) + Mathf.Max(0, meters) / MetersPerCoin;
            return Mathf.RoundToInt(baseCoins * (1f + Mathf.Max(0f, coinBonus)));
        }

        // ---- save-file wrappers (UI / game flow) ----

        public static int Balance => SaveSystem.Load().coins;

        public static FishDef Equipped => FishCatalog.Get(SaveSystem.Load().equippedFish);

        public static bool Owns(int fishId) => Owns(SaveSystem.Load(), fishId);

        public static BuyResult Buy(FishDef fish)
        {
            var save = SaveSystem.Load();
            BuyResult result = TryBuy(save, fish);
            if (result != BuyResult.Bought) return result;
            SaveSystem.Save(save);
            OnBalanceChanged?.Invoke(save.coins);
            return result;
        }

        public static void Equip(FishDef fish)
        {
            var save = SaveSystem.Load();
            if (!TryEquip(save, fish.id)) return;
            SaveSystem.Save(save);
            OnFishEquipped?.Invoke(fish);
        }

        /// <summary>Adds coins to the save (end of run). Caller may pass an already-loaded save to batch writes.</summary>
        public static void Deposit(SaveData save, int amount)
        {
            if (amount <= 0) return;
            save.coins += amount;
        }

        public static void RaiseBalanceChanged(int balance) => OnBalanceChanged?.Invoke(balance);
    }
}
