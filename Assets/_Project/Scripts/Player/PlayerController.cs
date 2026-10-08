using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using Upstream.Core;
using Upstream.Data;

namespace Upstream.Player
{
    /// <summary>
    /// Handles player input, movement, and river boundary clamping.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private GrowthConfig growthConfig;

        private CircleCollider2D col;
        private Camera mainCamera;
        private float targetX;

        /// <summary>
        /// Current visual tilt of the player.
        /// </summary>
        public float Tilt { get; private set; }

        /// <summary>
        /// Current world position of the player.
        /// </summary>
        public Vector2 Position => transform.position;

        /// <summary>
        /// Current horizontal speed of the player.
        /// </summary>
        public float CurrentSpeed { get; private set; }

        private void Awake()
        {
            col = GetComponent<CircleCollider2D>();
            mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            GameEvents.OnGameStart += ResetPosition;
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            GameEvents.OnGameStart -= ResetPosition;
        }

        private void ResetPosition()
        {
            transform.position = Vector3.zero;
            targetX = 0f;
            Tilt = 0f;
            CurrentSpeed = 0f;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
                return;

            HandleInput();
            MovePlayer();
        }

        private void HandleInput()
        {
            // Touch Input
            if (Touch.activeTouches.Count > 0)
            {
                Vector2 screenPos = Touch.activeTouches[0].screenPosition;
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, mainCamera.nearClipPlane));
                targetX = worldPos.x;
            }
            // Mouse Input (Fallback for Editor)
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                Vector2 screenPos = Mouse.current.position.ReadValue();
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, mainCamera.nearClipPlane));
                targetX = worldPos.x;
            }

            // Keyboard Input
            if (Keyboard.current != null)
            {
                float kbMove = 0f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) kbMove = -1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) kbMove = 1f;

                if (kbMove != 0f)
                {
                    targetX += kbMove * 15f * Time.deltaTime;
                }
            }

            // Clamp targetX
            float currentRadius = config.playerRadius * col.transform.localScale.x;
            float limit = config.riverHalfWidth - currentRadius;
            targetX = Mathf.Clamp(targetX, -limit, limit);
        }

        private void MovePlayer()
        {
            float currentRadius = config.playerRadius * col.transform.localScale.x;
            float limit = config.riverHalfWidth - currentRadius;
            float previousX = transform.position.x;
            
            float newX = Mathf.Lerp(previousX, targetX, config.playerMoveSmoothing * Time.deltaTime);
            newX = Mathf.Clamp(newX, -limit, limit);

            transform.position = new Vector3(newX, transform.position.y, transform.position.z);

            CurrentSpeed = (newX - previousX) / Time.deltaTime;
            Tilt = -CurrentSpeed * 1.5f;
            Tilt = Mathf.Clamp(Tilt, -45f, 45f);
        }
    }
}
