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
                return;

            if (mainCamera == null) mainCamera = UnityEngine.Camera.main;

            HandleInput();
            MovePlayer();
        }

        private void HandleInput()
        {
            if (Touch.activeTouches.Count > 0 && mainCamera != null)
            {
                Vector2 screenPos = Touch.activeTouches[0].screenPosition;
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
                targetX = worldPos.x;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed && mainCamera != null)
            {
                Vector2 screenPos = Mouse.current.position.ReadValue();
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
                targetX = worldPos.x;
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

            float newX = Mathf.Lerp(previousX, targetX, Mathf.Min(1f, Config.playerMoveSmoothing * Time.deltaTime));
            newX = Mathf.Clamp(newX, -limit, limit);

            transform.position = new Vector3(newX, DefaultPlayerY, 0f);

            float dt = Mathf.Max(Time.deltaTime, 0.001f);
            CurrentSpeed = (newX - previousX) / dt;
            float targetTilt = -CurrentSpeed * 0.14f;
            Tilt = Mathf.Lerp(Tilt, Mathf.Clamp(targetTilt, -32f, 32f), Mathf.Min(1f, Time.deltaTime * 10f));
        }
    }
}
