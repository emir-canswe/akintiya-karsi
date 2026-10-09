using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Upstream.Core;
using Upstream.Data;

namespace Upstream.Tests
{
    public class ShopTests
    {
        // --- Catalog ---

        [Test]
        public void Catalog_Has30Fish_WithUniqueIds()
        {
            Assert.AreEqual(30, FishCatalog.Count);
            // IDs live in save files: every one from 0..29 exists exactly once (order may differ).
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 30).ToList(), FishCatalog.All.Select(f => f.id).ToList());
        }

        [Test]
        public void EveryFishButTheStarter_HasAnAbility_WithADescription()
        {
            foreach (var fish in FishCatalog.All)
            {
                if (fish.id == FishCatalog.StarterId) { Assert.AreEqual(FishAbility.None, fish.ability); continue; }
                Assert.AreNotEqual(FishAbility.None, fish.ability, fish.nameEn);
                Assert.IsFalse(string.IsNullOrWhiteSpace(Upstream.UI.FishShopScreen.AbilityText(fish)), fish.nameEn);
            }
        }

        [Test]
        public void Catalog_GoesFromFreeStarterToBest()
        {
            var all = FishCatalog.All;
            Assert.AreEqual(0, all[0].price, "Starter fish is free.");
            Assert.AreEqual(FishCatalog.StarterId, all[0].id);
            for (int i = 1; i < all.Count; i++)
            {
                Assert.Greater(all[i].price, all[i - 1].price, $"Fish {i} must cost more than fish {i - 1}.");
                Assert.GreaterOrEqual((int)all[i].rarity, (int)all[i - 1].rarity, $"Rarity order at {i}.");
                Assert.GreaterOrEqual(all[i].coinBonus, all[i - 1].coinBonus, $"Coin bonus order at {i}.");
            }
            Assert.IsTrue(all[all.Count - 1].extraHeart, "The best fish gives the extra heart.");
        }

        [Test]
        public void Catalog_EveryFishHasNamesInBothLanguages()
        {
            foreach (var fish in FishCatalog.All)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(fish.nameTr), $"fish {fish.id} TR");
                Assert.IsFalse(string.IsNullOrWhiteSpace(fish.nameEn), $"fish {fish.id} EN");
            }
        }

        [Test]
        public void Catalog_UnknownId_FallsBackToStarter()
        {
            Assert.AreEqual(FishCatalog.StarterId, FishCatalog.Get(999).id);
        }

        // --- Buying / equipping ---

        [Test]
        public void TryBuy_NotEnoughCoins_ChangesNothing()
        {
            var save = new SaveData { coins = 10 };
            var fish = FishCatalog.Get(1);
            Assert.AreEqual(BuyResult.NotEnoughCoins, FishShop.TryBuy(save, fish));
            Assert.AreEqual(10, save.coins);
            Assert.IsFalse(FishShop.Owns(save, fish.id));
        }

        [Test]
        public void TryBuy_PaysAndOwns_OnlyOnce()
        {
            var fish = FishCatalog.Get(2);
            var save = new SaveData { coins = fish.price + 5 };
            Assert.AreEqual(BuyResult.Bought, FishShop.TryBuy(save, fish));
            Assert.AreEqual(5, save.coins);
            Assert.IsTrue(FishShop.Owns(save, fish.id));
            Assert.AreEqual(BuyResult.AlreadyOwned, FishShop.TryBuy(save, fish));
            Assert.AreEqual(5, save.coins, "Owned fish are never charged twice.");
        }

        [Test]
        public void TryEquip_OnlyOwnedFish()
        {
            var save = new SaveData();
            Assert.IsFalse(FishShop.TryEquip(save, 5));
            Assert.AreEqual(0, save.equippedFish);
            save.ownedFish.Add(5);
            Assert.IsTrue(FishShop.TryEquip(save, 5));
            Assert.AreEqual(5, save.equippedFish);
        }

        [Test]
        public void StarterFish_IsAlwaysOwned()
        {
            var save = new SaveData { ownedFish = new List<int>() };
            Assert.IsTrue(FishShop.Owns(save, FishCatalog.StarterId));
        }

        [Test]
        public void NextGoal_IsTheCheapestFishNotOwned()
        {
            var save = new SaveData();
            FishDef first = FishShop.NextGoal(save);
            Assert.IsNotNull(first);
            foreach (var fish in FishCatalog.All)
                if (fish.id != FishCatalog.StarterId) Assert.LessOrEqual(first.price, fish.price);

            save.ownedFish.Add(first.id);
            Assert.AreNotEqual(first.id, FishShop.NextGoal(save).id, "Owned fish is no longer the goal.");
            Assert.AreEqual(2, FishShop.OwnedCount(save));

            foreach (var fish in FishCatalog.All) if (!save.ownedFish.Contains(fish.id)) save.ownedFish.Add(fish.id);
            Assert.IsNull(FishShop.NextGoal(save), "No goal once every fish is owned.");
        }

        // --- Rewards ---

        [Test]
        public void RunReward_PickupsPlusDistance_WithFishBonus()
        {
            Assert.AreEqual(0, FishShop.RunReward(0, 0, 0f));
            Assert.AreEqual(12, FishShop.RunReward(10, 2 * FishShop.MetersPerCoin + 3, 0f));
            Assert.AreEqual(18, FishShop.RunReward(10, 2 * FishShop.MetersPerCoin, 0.5f));
            Assert.AreEqual(0, FishShop.RunReward(-5, -100, 0.5f), "Never negative.");
        }

        // --- Save migration ---

        [Test]
        public void Migrate_V2Save_OwnsAndWearsStarter()
        {
            var v2 = new SaveData { version = 2, ownedFish = null, equippedFish = 7, bestScore = 300 };
            SaveSystem.Migrate(v2);
            CollectionAssert.Contains(v2.ownedFish, FishCatalog.StarterId);
            Assert.AreEqual(FishCatalog.StarterId, v2.equippedFish, "Can't wear a fish you don't own.");
            Assert.AreEqual(300, v2.bestScore);
        }
    }
}
