using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using Upstream.Core;
using Upstream.Data;
using Upstream.Mechanics;

namespace Upstream.Player
{
    /// <summary>
    /// Handles player input, movement, and river boundary clamping.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        public const float DefaultPlayerY = -140f;

        [SerializeField] private GameConfig config;
        [SerializeField] private GrowthConfig growthConfig;

        private CircleCollider2D col;
        private UnityEngine.Camera mainCamera;
        private GrowthSystem growthSystem;
        private float targetX;

        public GameConfig Config
        {
            get
            {
                if (config == null)
                {
                    config = GameManager.Instance != null
                        ? GameManager.Instance.Config
                        : ScriptableObject.CreateInstance<GameConfig>();
                }
                return config;
            }
            set => config = value;
        }

        public float Tilt { get; private set; }
        public Vector2 Position => transform.position;
        public float CurrentSpeed { get; private set; }

        private void Awake()
        {
            col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            growthSystem = GetComponent<GrowthSystem>();
            mainCamera = UnityEngine.Camera.main;
            transform.position = new Vector3(0f, DefaultPlayerY, 0f);
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            GameEvents.OnGameStart += ResetPosition;
            GameEvents.OnReturnToMenu += ResetPosition;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= ResetPosition;
            GameEvents.OnReturnToMenu -= ResetPosition;
        }

        public void ResetPosition()
        {
            transform.position = new Vector3(0f, DefaultPlayerY, 0f);
            targetX = 0f;
            Tilt = 0f;
            CurrentSpeed = 0f;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
            {
                // A finger held through pause/game over must not count as a drag when play resumes.
                activeTouchId = NoTouch;
                mouseDragging = false;
                return;
            }

            if (mainCamera == null) mainCamera = UnityEngine.Camera.main;

            HandleInput();
            MovePlayer();
        }

        private const int NoTouch = -1;
        private int activeTouchId = NoTouch;
        private float lastPointerX;
        private bool mouseDragging;

        /// <summary>
        /// Relative steering: the fish moves by how far the finger slides sideways (times the sensitivity
        /// gain), wherever the finger is on the screen. Touching down never makes the fish jump, the thumb
        /// never has to cover the fish, and a small slide is enough to cross the river.
        /// </summary>
        private void HandleInput()
        {
            float worldPerPixel = mainCamera != null && Screen.height > 0
                ? mainCamera.orthographicSize * 2f / Screen.height
                : 0f;
            float gain = ControlSettings.Gain;

            var touches = Touch.activeTouches;
            if (touches.Count > 0)
            {
                // Follow the finger that started the drag; if it lifts, carry on with another one.
                int index = -1;
                for (int i = 0; i < touches.Count; i++)
                {
                    if (touches[i].touchId == activeTouchId) { index = i; break; }
                }
                if (index < 0)
                {
                    activeTouchId = touches[0].touchId;
                    lastPointerX = touches[0].screenPosition.x;
                }
                else
                {
                    float x = touches[index].screenPosition.x;
                    targetX += (x - lastPointerX) * worldPerPixel * gain;
                    lastPointerX = x;
                }
            }
            else
            {
                activeTouchId = NoTouch;
                if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                {
                    float x = Mouse.current.position.ReadValue().x;
                    if (mouseDragging) targetX += (x - lastPointerX) * worldPerPixel * gain;
                    lastPointerX = x;
                    mouseDragging = true;
                }
                else
                {
                    mouseDragging = false;
                }
            }

            if (Keyboard.current != null)
            {
                float kbMove = 0f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) kbMove = -1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) kbMove = 1f;

                if (kbMove != 0f)
                {
                    targetX += kbMove * 360f * Time.deltaTime;
                }
            }

            float hitboxMult = growthSystem != null ? growthSystem.HitboxMultiplier : 1f;
            float currentRadius = Config.playerRadius * hitboxMult;
            float limit = Mathf.Max(20f, Config.riverHalfWidth - currentRadius - 4f);
            targetX = Mathf.Clamp(targetX, -limit, limit);
        }

        private void MovePlayer()
        {
            float hitboxMult = growthSystem != null ? growthSystem.HitboxMultiplier : 1f;
            float currentRadius = Config.playerRadius * hitboxMult;
            float limit = Mathf.Max(20f, Config.riverHalfWidth - currentRadius - 4f);
            float previousX = transform.position.x;

            float steering = Config.playerMoveSmoothing * FishShop.Active.SteeringMultiplier;
            // Exponential follow: the same feel at 60 or 90 fps (a plain per-frame lerp is snappier at high fps).
            float newX = Mathf.Lerp(previousX, targetX, 1f - Mathf.Exp(-steering * Time.deltaTime));
            newX = Mathf.Clamp(newX, -limit, limit);

            transform.position = new Vector3(newX, DefaultPlayerY, 0f);

            float dt = Mathf.Max(Time.deltaTime, 0.001f);
            CurrentSpeed = (newX - previousX) / dt;
            float targetTilt = -CurrentSpeed * 0.14f;
            Tilt = Mathf.Lerp(Tilt, Mathf.Clamp(targetTilt, -32f, 32f), Mathf.Min(1f, Time.deltaTime * 10f));
        }
    }
}
