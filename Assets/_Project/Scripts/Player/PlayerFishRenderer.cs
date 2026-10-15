using UnityEngine;
using Upstream.Core;
using Upstream.Data;
using Upstream.Mechanics;
using Upstream.River;

namespace Upstream.Player
{
    /// <summary>
    /// Renders and animates the multi-part watercolor fish (body, wagging tail, pectoral fins, turbo/alpha aura,
    /// invincibility blink, and 5-stage growth scaling).
    /// </summary>
    public class PlayerFishRenderer : MonoBehaviour
    {
        private PlayerController playerController;
        private HealthSystem healthSystem;
        private TurboSystem turboSystem;
        private GrowthSystem growthSystem;

        private Transform visualRoot;
        private SpriteRenderer bodyRenderer;
        private SpriteRenderer tailRenderer;
        private SpriteRenderer leftFinRenderer;
        private SpriteRenderer rightFinRenderer;
        private SpriteRenderer auraRenderer;
        private SpriteRenderer shadowRenderer;
        private SpriteRenderer shieldRenderer;

        private float wagClock;
        private float currentVisualScale = 1f;
        private float stagePopScale;
        private FishDef skin;

        // What was last written to the renderers: a SpriteRenderer setter is a native call that marks the sprite
        // dirty even when the value is unchanged, so Update only writes on a change.
        private Color shownTint = Color.white;
        private Color shownAura = Color.clear;
        private bool auraShown;
        private bool partsHidden;
        private bool shieldShown;
        private Color shownShield;
        private float shownShieldScale = 2.9f;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            healthSystem = GetComponent<HealthSystem>();
            turboSystem = GetComponent<TurboSystem>();
            growthSystem = GetComponent<GrowthSystem>();

            BuildFishHierarchy();
            ApplySkin(FishShop.Equipped);
        }

        private void OnEnable()
        {
            GameEvents.OnGrowthStageChanged += HandleStageChanged;
            GameEvents.OnSharkEaten += HandleSharkEaten;
            GameEvents.OnTurboSmash += HandleTurboSmash;
            FishShop.OnFishEquipped += ApplySkin;
        }

        private void OnDisable()
        {
            GameEvents.OnGrowthStageChanged -= HandleStageChanged;
            GameEvents.OnSharkEaten -= HandleSharkEaten;
            GameEvents.OnTurboSmash -= HandleTurboSmash;
            FishShop.OnFishEquipped -= ApplySkin;
        }

        /// <summary>Swaps in the bought fish's sprites and proportions (also live, from the shop behind the menu).</summary>
        public void ApplySkin(FishDef fish)
        {
            if (fish == null || bodyRenderer == null) return;
            skin = fish;

            bodyRenderer.sprite = FishSkinFactory.GetBody(fish);
            tailRenderer.sprite = FishSkinFactory.GetTail(fish);
            leftFinRenderer.sprite = FishSkinFactory.GetFin(fish);
            rightFinRenderer.sprite = FishSkinFactory.GetFin(fish);

            bodyRenderer.transform.localScale = new Vector3(fish.width, fish.length, 1f);
            shadowRenderer.sprite = bodyRenderer.sprite;
            shadowRenderer.transform.localScale = bodyRenderer.transform.localScale;
            tailRenderer.transform.localPosition = new Vector3(0f, -18f * fish.length, 0f);
            tailRenderer.transform.localScale = Vector3.one * fish.tailSize;
            leftFinRenderer.transform.localPosition = new Vector3(-13f * fish.width, 2f, 0f);
            rightFinRenderer.transform.localPosition = new Vector3(13f * fish.width, 2f, 0f);
        }

        private void HandleStageChanged(int stage)
        {
            if (stage > 0)
            {
                stagePopScale = 0.42f;
            }
        }

        private void HandleSharkEaten()
        {
            stagePopScale = 0.55f;
        }

        /// <summary>The fish bulges on impact when it bursts through something at turbo speed.</summary>
        private void HandleTurboSmash()
        {
            stagePopScale = Mathf.Max(stagePopScale, 0.3f);
        }

        private void BuildFishHierarchy()
        {
            if (visualRoot != null) return;

            GameObject rootObj = new GameObject("FishVisualRoot");
            rootObj.transform.SetParent(transform, false);
            visualRoot = rootObj.transform;

            // Aura glow behind fish
            GameObject auraObj = new GameObject("Aura");
            auraObj.transform.SetParent(visualRoot, false);
            auraObj.transform.localScale = new Vector3(1.5f, 1.8f, 1f);
            auraRenderer = auraObj.AddComponent<SpriteRenderer>();
            auraRenderer.sprite = WatercolorSpriteFactory.GetCircleGlowSprite();
            auraRenderer.sortingOrder = 8;
            auraRenderer.color = Color.clear;
            auraRenderer.enabled = false; // a fully transparent glow would still cost a draw call and fill rate

            // Soft drop shadow on the water (sprites are assigned by ApplySkin)
            GameObject shadowObj = new GameObject("Shadow");
            shadowObj.transform.SetParent(visualRoot, false);
            shadowObj.transform.localPosition = new Vector3(6f, -9f, 0.1f);
            shadowRenderer = shadowObj.AddComponent<SpriteRenderer>();
            shadowRenderer.color = new Color(0f, 0.12f, 0.28f, 0.28f);
            shadowRenderer.sortingOrder = 3;

            // Shield ability bubble (shown while the shield still has charges)
            GameObject shieldObj = new GameObject("Shield");
            shieldObj.transform.SetParent(visualRoot, false);
            shieldObj.transform.localScale = Vector3.one * 2.9f;
            shieldRenderer = shieldObj.AddComponent<SpriteRenderer>();
            shieldRenderer.sprite = CartoonSpriteFactory.GetBubble();
            shieldRenderer.sortingOrder = 11;
            shieldRenderer.enabled = false;

            // Pectoral fins
            GameObject lFinObj = new GameObject("LeftFin");
            lFinObj.transform.SetParent(visualRoot, false);
            lFinObj.transform.localPosition = new Vector3(-13f, 2f, 0f);
            lFinObj.transform.localRotation = Quaternion.Euler(0f, 0f, 32f);
            leftFinRenderer = lFinObj.AddComponent<SpriteRenderer>();
            leftFinRenderer.sortingOrder = 9;

            GameObject rFinObj = new GameObject("RightFin");
            rFinObj.transform.SetParent(visualRoot, false);
            rFinObj.transform.localPosition = new Vector3(13f, 2f, 0f);
            rFinObj.transform.localRotation = Quaternion.Euler(0f, 0f, -32f);
            rightFinRenderer = rFinObj.AddComponent<SpriteRenderer>();
            rightFinRenderer.sortingOrder = 9;

            // Tail fin
            GameObject tailObj = new GameObject("Tail");
            tailObj.transform.SetParent(visualRoot, false);
            tailObj.transform.localPosition = new Vector3(0f, -18f, 0f);
            tailRenderer = tailObj.AddComponent<SpriteRenderer>();
            tailRenderer.sortingOrder = 9;

            // Main body
            GameObject bodyObj = new GameObject("Body");
            bodyObj.transform.SetParent(visualRoot, false);
            bodyRenderer = bodyObj.AddComponent<SpriteRenderer>();
            bodyRenderer.sortingOrder = 10;
        }

        private void Update()
        {
            if (visualRoot == null) return;

            GameState state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.Menu;
            // The menu has its own mascot; the run fish waiting at its start spot would peek out between the buttons.
            bool shown = state != GameState.Menu;
            if (visualRoot.gameObject.activeSelf != shown) visualRoot.gameObject.SetActive(shown);
            if (!shown) return;
            bool isTurbo = turboSystem != null && turboSystem.IsTurboActive;
            bool isInvincible = healthSystem != null && healthSystem.IsInvincible;
            int stage = growthSystem != null ? growthSystem.CurrentStage : 0;
            float targetScale = growthSystem != null ? growthSystem.SizeMultiplier : 1f;

            // Smooth growth scale + stage-up pop
            stagePopScale = Mathf.MoveTowards(stagePopScale, 0f, Time.deltaTime * 1.8f);
            currentVisualScale = Mathf.Lerp(currentVisualScale, targetScale, Time.deltaTime * 8f);
            float finalScale = currentVisualScale + Mathf.Sin(stagePopScale * Mathf.PI) * 0.35f;
            visualRoot.localScale = Vector3.one * finalScale;

            // Tilt rotation
            float tilt = playerController != null ? playerController.Tilt : 0f;
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, tilt);

            // Tail & fin wag
            float wagSpeed = state == GameState.Playing ? (isTurbo ? 26f : 14f) : 7f;
            wagClock += Time.deltaTime * wagSpeed;
            float tailAngle = Mathf.Sin(wagClock) * 24f;
            tailRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, tailAngle);
            leftFinRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, 32f + tailAngle * 0.3f);
            rightFinRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, -32f - tailAngle * 0.3f);

            // Stage & Turbo tint
            Color stageTint = Color.white;
            if (isTurbo)
            {
                stageTint = new Color(1f, 0.85f, 0.72f, 1f);
            }
            else if (stage >= 3) // Alpha / Titan predator glow tint
            {
                stageTint = new Color(1f, 0.92f, 0.80f, 1f);
            }
            if (stageTint != shownTint)
            {
                shownTint = stageTint;
                bodyRenderer.color = stageTint;
                tailRenderer.color = stageTint;
            }

            // Aura glow
            Color aura;
            if (isTurbo)
            {
                float pulse = 0.55f + 0.25f * Mathf.Sin(Time.time * 18f);
                aura = new Color(0.55f, 0.98f, 0.86f, pulse);
            }
            else if (stage >= 2)
            {
                // Subtle golden aura showing shark-eating power
                float pulse = 0.18f + 0.10f * Mathf.Sin(Time.time * 6f);
                aura = new Color(1f, 0.84f, 0.35f, pulse);
            }
            else if (skin != null && skin.pattern == FishPattern.Rainbow)
            {
                // Rainbow Legend: aura cycles through every hue.
                aura = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.25f, 1f), 0.55f, 1f);
                aura.a = skin.glow.a * (0.8f + 0.2f * Mathf.Sin(Time.time * 5f));
            }
            else if (skin != null && skin.glow.a > 0f)
            {
                aura = skin.glow;
                aura.a *= 0.8f + 0.2f * Mathf.Sin(Time.time * 4f);
            }
            else
            {
                aura = Color.clear;
            }

            // Most fish have no glow at all: then the aura sprite is switched off instead of drawn invisibly.
            bool auraVisible = aura.a > 0.004f;
            if (auraVisible != auraShown)
            {
                auraShown = auraVisible;
                auraRenderer.enabled = auraVisible;
            }
            if (auraVisible && aura != shownAura)
            {
                shownAura = aura;
                auraRenderer.color = aura;
            }

            // Invincibility / Dying blink
            bool blinkHide = (isInvincible && !isTurbo && Mathf.FloorToInt(Time.time * 14f) % 2 == 0) ||
                             (state == GameState.Dying && Mathf.FloorToInt(Time.time * 8f) % 2 == 0);
            if (blinkHide != partsHidden)
            {
                partsHidden = blinkHide;
                bodyRenderer.enabled = !blinkHide;
                tailRenderer.enabled = !blinkHide;
                leftFinRenderer.enabled = !blinkHide;
                rightFinRenderer.enabled = !blinkHide;
                shadowRenderer.enabled = !blinkHide;
            }

            // Shield bubble: gently pulsing while charged
            bool shielded = healthSystem != null && healthSystem.ShieldCharges > 0;
            if (shielded != shieldShown)
            {
                shieldShown = shielded;
                shieldRenderer.enabled = shielded;
            }
            if (shielded)
            {
                float pulse = 0.55f + 0.2f * Mathf.Sin(Time.time * 4f);
                Color shield = new Color(0.55f, 0.95f, 1f, pulse);
                if (shield != shownShield)
                {
                    shownShield = shield;
                    shieldRenderer.color = shield;
                }
                float shieldScale = 2.9f + 0.1f * Mathf.Sin(Time.time * 3f);
                if (!Mathf.Approximately(shieldScale, shownShieldScale))
                {
                    shownShieldScale = shieldScale;
                    shieldRenderer.transform.localScale = Vector3.one * shieldScale;
                }
            }
        }
    }
}
