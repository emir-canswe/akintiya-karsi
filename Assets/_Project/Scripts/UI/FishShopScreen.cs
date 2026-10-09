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
            public Image chip;
        }

        private static readonly Color BuyColor = new Color(1f, 0.54f, 0.36f, 1f);
        private static readonly Color SelectColor = new Color(0.30f, 0.80f, 0.66f, 1f);
        private static readonly Color EquippedColor = new Color(1f, 1f, 1f, 0.14f);
        private static readonly Color DisabledColor = new Color(1f, 1f, 1f, 0.10f);
        private static readonly Color DarkText = new Color(0.17f, 0.05f, 0f, 1f);
        private static readonly Color PriceChip = new Color(0.04f, 0.10f, 0.35f, 0.75f);
        private static readonly Color OwnedChip = new Color(0.25f, 0.72f, 0.40f, 1f);

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

        private Image rarityPill;
        private Image previewGlow;
        private RawImage sunlight;

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

        /// <summary>Rarity badge, glow behind the big fish, and the drifting sunlight backdrop.</summary>
        public void SetDecor(Image rarityBadge, Image glow, RawImage sunlightLayer)
        {
            rarityPill = rarityBadge;
            previewGlow = glow;
            sunlight = sunlightLayer;
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
            float t = Time.unscaledTime;

            // Sunlight drifts over the backdrop; the big fish swims back and forth in its tank.
            if (sunlight != null) sunlight.uvRect = new Rect(Mathf.Sin(t * 0.1f) * 0.2f, t * 0.04f, 2.2f, 4.5f);
            if (previewPop != null)
            {
                previewPop.anchoredPosition = new Vector2(Mathf.Sin(t * 0.7f) * 40f, Mathf.Sin(t * 1.6f) * 5f);
                // Faces the way it swims: right when moving right, flipped when moving left.
                float dir = Mathf.Cos(t * 0.7f) >= 0f ? 1f : -1f;
                float pop = popTime < 1f ? 1f + Mathf.Sin(popTime * Mathf.PI) * 0.12f : 1f;
                previewPop.localScale = new Vector3(pop, pop * dir, 1f);
            }
            if (popTime < 1f) popTime = Mathf.Min(1f, popTime + Time.unscaledDeltaTime * 4f);
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
            Color rarityColor = FishCatalog.RarityColor(fish.rarity);
            rarityText.text = LocalizationManager.Get($"rarity_{(int)fish.rarity}");
            if (rarityPill != null) rarityPill.color = rarityColor;
            if (previewGlow != null) previewGlow.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.55f);
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

                card.frame.color = selected ? Color.white : FishCatalog.RarityColor(card.fish.rarity);

                card.nameText.text = card.fish.Name;
                card.coinIcon.SetActive(!cardOwned);
                // Price tag, or a status chip once owned
                RectTransform priceRt = card.priceText.rectTransform;
                priceRt.offsetMin = new Vector2(cardOwned ? 0f : 16f, 0f);
                if (cardEquipped)
                {
                    card.priceText.text = LocalizationManager.Get("equipped_short");
                    card.priceText.color = Color.white;
                    card.chip.color = BuyColor;
                }
                else if (cardOwned)
                {
                    card.priceText.text = LocalizationManager.Get("owned");
                    card.priceText.color = Color.white;
                    card.chip.color = OwnedChip;
                }
                else
                {
                    card.priceText.text = card.fish.price.ToString();
                    card.priceText.color = save.coins >= card.fish.price ? new Color(1f, 0.86f, 0.3f, 1f) : new Color(1f, 1f, 1f, 0.6f);
                    card.chip.color = PriceChip;
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
            if (fish.ability == FishAbility.None && fish.coinBonus <= 0f && !fish.extraHeart) return LocalizationManager.Get("perk_none");
            string text = AbilityText(fish);
            string extras = "";
            if (fish.coinBonus > 0f) extras = LocalizationManager.Format("perk_coin", Mathf.RoundToInt(fish.coinBonus * 100f));
            if (fish.extraHeart) extras += (extras.Length > 0 ? " • " : "") + LocalizationManager.Get("perk_heart");
            if (extras.Length > 0) text += (text.Length > 0 ? "\n" : "") + "<size=85%><color=#FFE38A>" + extras + "</color></size>";
            return text;
        }

        /// <summary>One line describing the fish's special ability, with its strength for this rarity.</summary>
        public static string AbilityText(FishDef fish)
        {
            int Pct(float multiplier) => Mathf.RoundToInt((multiplier - 1f) * 100f);
            switch (fish.ability)
            {
                case FishAbility.Magnet: return LocalizationManager.Get("ability_magnet");
                case FishAbility.Agile: return LocalizationManager.Format("ability_agile", Pct(fish.SteeringMultiplier));
                case FishAbility.LongTurbo: return LocalizationManager.Format("ability_longturbo", Pct(fish.TurboTimeMultiplier));
                case FishAbility.Power: return LocalizationManager.Format("ability_power", Pct(fish.PowerMultiplier));
                case FishAbility.Lucky: return LocalizationManager.Get("ability_lucky");
                case FishAbility.Shield: return LocalizationManager.Get(fish.ShieldCharges > 1 ? "ability_shield2" : "ability_shield");
                case FishAbility.WaveRider: return LocalizationManager.Get("ability_waverider");
                case FishAbility.HookProof: return LocalizationManager.Get("ability_hookproof");
                case FishAbility.RockBreaker: return LocalizationManager.Get("ability_rockbreaker");
                case FishAbility.Slim: return LocalizationManager.Format("ability_slim", Mathf.RoundToInt((1f - fish.HitboxMultiplier) * 100f));
                default: return "";
            }
        }
    }
}
