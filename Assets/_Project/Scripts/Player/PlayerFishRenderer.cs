using UnityEngine;
using Upstream.Core;
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

        private float wagClock;
        private float currentVisualScale = 1f;
        private float stagePopScale;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            healthSystem = GetComponent<HealthSystem>();
            turboSystem = GetComponent<TurboSystem>();
            growthSystem = GetComponent<GrowthSystem>();

            BuildFishHierarchy();
        }

        private void OnEnable()
        {
            GameEvents.OnGrowthStageChanged += HandleStageChanged;
            GameEvents.OnSharkEaten += HandleSharkEaten;
        }

        private void OnDisable()
        {
            GameEvents.OnGrowthStageChanged -= HandleStageChanged;
            GameEvents.OnSharkEaten -= HandleSharkEaten;
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

            // Pectoral fins
            GameObject lFinObj = new GameObject("LeftFin");
            lFinObj.transform.SetParent(visualRoot, false);
            lFinObj.transform.localPosition = new Vector3(-13f, 2f, 0f);
            lFinObj.transform.localRotation = Quaternion.Euler(0f, 0f, 32f);
            leftFinRenderer = lFinObj.AddComponent<SpriteRenderer>();
            leftFinRenderer.sprite = WatercolorSpriteFactory.GetFishFinSprite();
            leftFinRenderer.sortingOrder = 9;

            GameObject rFinObj = new GameObject("RightFin");
            rFinObj.transform.SetParent(visualRoot, false);
            rFinObj.transform.localPosition = new Vector3(13f, 2f, 0f);
            rFinObj.transform.localRotation = Quaternion.Euler(0f, 0f, -32f);
            rightFinRenderer = rFinObj.AddComponent<SpriteRenderer>();
            rightFinRenderer.sprite = WatercolorSpriteFactory.GetFishFinSprite();
            rightFinRenderer.sortingOrder = 9;

            // Tail fin
            GameObject tailObj = new GameObject("Tail");
            tailObj.transform.SetParent(visualRoot, false);
            tailObj.transform.localPosition = new Vector3(0f, -18f, 0f);
            tailRenderer = tailObj.AddComponent<SpriteRenderer>();
            tailRenderer.sprite = WatercolorSpriteFactory.GetFishTailSprite();
            tailRenderer.sortingOrder = 9;

            // Main body
            GameObject bodyObj = new GameObject("Body");
            bodyObj.transform.SetParent(visualRoot, false);
            bodyRenderer = bodyObj.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = WatercolorSpriteFactory.GetFishBodySprite();
            bodyRenderer.sortingOrder = 10;
        }

        private void Update()
        {
            if (visualRoot == null) return;

            GameState state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.Menu;
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
            bodyRenderer.color = stageTint;
            tailRenderer.color = stageTint;

            // Aura glow
            if (isTurbo)
            {
                float pulse = 0.55f + 0.25f * Mathf.Sin(Time.time * 18f);
                auraRenderer.color = new Color(0.55f, 0.98f, 0.86f, pulse);
            }
            else if (stage >= 2)
            {
                // Subtle golden aura showing shark-eating power
                float pulse = 0.18f + 0.10f * Mathf.Sin(Time.time * 6f);
                auraRenderer.color = new Color(1f, 0.84f, 0.35f, pulse);
            }
            else
            {
                auraRenderer.color = Color.clear;
            }

            // Invincibility / Dying blink
            bool blinkHide = (isInvincible && !isTurbo && Mathf.FloorToInt(Time.time * 14f) % 2 == 0) ||
                             (state == GameState.Dying && Mathf.FloorToInt(Time.time * 8f) % 2 == 0);
            bodyRenderer.enabled = !blinkHide;
            tailRenderer.enabled = !blinkHide;
            leftFinRenderer.enabled = !blinkHide;
            rightFinRenderer.enabled = !blinkHide;
        }
    }
}
