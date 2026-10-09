using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Upstream.Core;
using Upstream.Data;

namespace Upstream.UI
{
    /// <summary>
    /// Fish shop (opened from the main menu): a detail panel for the selected fish with its Buy / Select
    /// button, and a scrollable grid of all fish. Buying a fish equips it right away.
    /// The bootstrap builds the visuals and hands them over in <see cref="Configure"/>.
    /// </summary>
    public class FishShopScreen : MonoBehaviour
    {
        /// <summary>One grid card's widgets.</summary>
        public class Card
        {
            public FishDef fish;
            public Button button;
            public Image frame;
            public FishPreview preview;
            public TMP_Text priceText;
            public TMP_Text nameText;
            public GameObject coinIcon;
        }

        private static readonly Color BuyColor = new Color(1f, 0.54f, 0.36f, 1f);
        private static readonly Color SelectColor = new Color(0.30f, 0.80f, 0.66f, 1f);
        private static readonly Color EquippedColor = new Color(1f, 1f, 1f, 0.14f);
        private static readonly Color DisabledColor = new Color(1f, 1f, 1f, 0.10f);
        private static readonly Color DarkText = new Color(0.17f, 0.05f, 0f, 1f);

        private FishPreview bigPreview;
        private TMP_Text nameText;
        private TMP_Text rarityText;
        private TMP_Text perkText;
        private Button actionButton;
        private Image actionBg;
        private TMP_Text actionLabel;
        private TMP_Text balanceText;
        private Card[] cards;
        private RectTransform previewPop;

        private int selectedId;
        private float popTime = 1f;

        public void Configure(FishPreview preview, TMP_Text fishName, TMP_Text rarity, TMP_Text perk,
                              Button action, TMP_Text balance, Card[] gridCards, Button close)
        {
            bigPreview = preview;
            nameText = fishName;
            rarityText = rarity;
            perkText = perk;
            actionButton = action;
            actionBg = action.GetComponent<Image>();
            actionLabel = action.GetComponentInChildren<TMP_Text>();
            balanceText = balance;
            cards = gridCards;
            previewPop = (RectTransform)preview.transform;

            actionButton.onClick.AddListener(OnActionClicked);
            close.onClick.AddListener(Close);
            foreach (var card in cards)
            {
                int id = card.fish.id;
                card.button.onClick.AddListener(() => Select(id));
            }
        }

        private void Awake()
        {
            FishShop.OnBalanceChanged += HandleBalanceChanged;
            LocalizationManager.OnLanguageChanged += Refresh;
            GameEvents.OnGameStart += Close;
        }

        private void OnDestroy()
        {
            FishShop.OnBalanceChanged -= HandleBalanceChanged;
            LocalizationManager.OnLanguageChanged -= Refresh;
            GameEvents.OnGameStart -= Close;
            UIBackStack.Remove(Close);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            UIBackStack.Push(Close);
            selectedId = SaveSystem.Load().equippedFish;
            Refresh();
        }

        public void Close()
        {
            UIBackStack.Remove(Close);
            gameObject.SetActive(false);
        }

        public void Select(int fishId)
        {
            selectedId = fishId;
            popTime = 0f;
            Refresh();
        }

        private void HandleBalanceChanged(int balance)
        {
            if (isActiveAndEnabled) Refresh();
        }

        private void OnActionClicked()
        {
            FishDef fish = FishCatalog.Get(selectedId);
            var save = SaveSystem.Load();
            if (!FishShop.Owns(save, fish.id))
            {
                if (FishShop.Buy(fish) != BuyResult.Bought) return;
                popTime = 0f; // little celebration bounce
            }
            FishShop.Equip(fish);
            Refresh();
        }

        private void Update()
        {
            if (previewPop == null || popTime >= 1f) return;
            popTime = Mathf.Min(1f, popTime + Time.unscaledDeltaTime * 4f);
            float s = 1f + Mathf.Sin(popTime * Mathf.PI) * 0.12f;
            previewPop.localScale = new Vector3(s, s, 1f);
        }

        private void Refresh()
        {
            if (cards == null) return;
            var save = SaveSystem.Load();
            FishDef fish = FishCatalog.Get(selectedId);
            bool owned = FishShop.Owns(save, fish.id);
            bool equipped = save.equippedFish == fish.id;

            if (balanceText != null) balanceText.text = save.coins.ToString();

            // Detail panel
            bigPreview.SetFish(fish);
            nameText.text = fish.Name;
            rarityText.text = LocalizationManager.Get($"rarity_{(int)fish.rarity}");
            rarityText.color = FishCatalog.RarityColor(fish.rarity);
            perkText.text = PerkText(fish);

            if (equipped)
            {
                SetAction(LocalizationManager.Get("equipped"), EquippedColor, Color.white, false);
            }
            else if (owned)
            {
                SetAction(LocalizationManager.Get("equip_btn"), SelectColor, DarkText, true);
            }
            else if (save.coins >= fish.price)
            {
                SetAction(LocalizationManager.Format("buy_for", fish.price), BuyColor, DarkText, true);
            }
            else
            {
                SetAction(LocalizationManager.Format("need_more", fish.price - save.coins), DisabledColor, new Color(1f, 1f, 1f, 0.6f), false);
            }

            // Grid
            foreach (var card in cards)
            {
                bool cardOwned = FishShop.Owns(save, card.fish.id);
                bool cardEquipped = save.equippedFish == card.fish.id;
                bool selected = card.fish.id == selectedId;

                Color rarity = FishCatalog.RarityColor(card.fish.rarity);
                card.frame.color = selected ? Color.white : new Color(rarity.r, rarity.g, rarity.b, 0.55f);

                card.nameText.text = card.fish.Name;
                card.coinIcon.SetActive(!cardOwned);
                if (cardEquipped)
                {
                    card.priceText.text = LocalizationManager.Get("equipped_short");
                    card.priceText.color = BuyColor;
                }
                else if (cardOwned)
                {
                    card.priceText.text = LocalizationManager.Get("owned");
                    card.priceText.color = SelectColor;
                }
                else
                {
                    card.priceText.text = card.fish.price.ToString();
                    card.priceText.color = save.coins >= card.fish.price ? new Color(1f, 0.84f, 0.35f, 1f) : new Color(1f, 1f, 1f, 0.55f);
                }
            }
        }

        private void SetAction(string label, Color bg, Color text, bool interactable)
        {
            actionLabel.text = label;
            actionLabel.color = text;
            actionBg.color = bg;
            actionButton.interactable = interactable;
        }

        public static string PerkText(FishDef fish)
        {
            if (fish.coinBonus <= 0f && !fish.extraHeart) return LocalizationManager.Get("perk_none");
            string text = "";
            if (fish.coinBonus > 0f) text = LocalizationManager.Format("perk_coin", Mathf.RoundToInt(fish.coinBonus * 100f));
            if (fish.extraHeart) text += (text.Length > 0 ? "\n" : "") + LocalizationManager.Get("perk_heart");
            return text;
        }
    }
}
