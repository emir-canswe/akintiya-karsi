using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TMPro;
using Upstream.Core;

namespace Upstream.UI
{
    /// <summary>
    /// Controls the main menu screen.
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private TMP_Text tapToStartText;
        [SerializeField] private TMP_Text bestScoreText;

        public void Configure(TMP_Text title, TMP_Text how, TMP_Text tap, TMP_Text best)
        {
            titleText = title;
            instructionText = how;
            tapToStartText = tap;
            bestScoreText = best;
            UpdateBestScore();
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            GameEvents.OnGameStart += HandleGameStart;
            UpdateBestScore();
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Menu)
            {
                if (tapToStartText != null)
                {
                    Color c = tapToStartText.color;
                    c.a = 0.45f + Mathf.PingPong(Time.unscaledTime * 1.4f, 0.55f);
                    tapToStartText.color = c;
                }

                bool tapped = false;
                if (Touch.activeTouches.Count > 0 && Touch.activeTouches[0].phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    tapped = true;
                }
                else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    tapped = true;
                }
                else if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    tapped = true;
                }

                if (tapped)
                {
                    GameManager.Instance.StartGame();
                }
            }
        }

        public void UpdateBestScore()
        {
            var saveData = SaveSystem.Load();
            if (bestScoreText != null)
            {
                bestScoreText.text = saveData.bestScore > 0 ? $"En iyi: {saveData.bestScore} m" : "";
            }
        }

        private void HandleGameStart()
        {
            gameObject.SetActive(false);
        }
    }
}
