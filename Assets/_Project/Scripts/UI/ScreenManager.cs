using UnityEngine;
using Upstream.Core;

namespace Upstream.UI
{
    /// <summary>
    /// Manages which screens are visible based on game state and handles safe areas.
    /// </summary>
    public class ScreenManager : MonoBehaviour
    {
        [SerializeField] private MainMenuScreen mainMenuScreen;
        [SerializeField] private GameOverScreen gameOverScreen;
        [SerializeField] private HUDController hudController;
        [SerializeField] private GameObject settingsScreen;
        [SerializeField] private RectTransform safeAreaRect;

        private void Awake()
        {
            ApplySafeArea();
        }

        private void OnEnable()
        {
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnGameOver += HandleGameOver;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnGameOver -= HandleGameOver;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                UpdateScreens(GameManager.Instance.CurrentState);
            }
        }

        private void UpdateScreens(GameState state)
        {
            if (mainMenuScreen != null) mainMenuScreen.gameObject.SetActive(state == GameState.Menu);
            if (hudController != null) hudController.gameObject.SetActive(state == GameState.Playing || state == GameState.Paused || state == GameState.Dying);
            if (gameOverScreen != null) gameOverScreen.gameObject.SetActive(state == GameState.GameOver);
        }

        private void HandleGameStart()
        {
            UpdateScreens(GameState.Playing);
        }

        private void HandleGameOver()
        {
            UpdateScreens(GameState.GameOver);
        }

        private void ApplySafeArea()
        {
            if (safeAreaRect == null) return;

            Rect safeArea = Screen.safeArea;
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            safeAreaRect.anchorMin = anchorMin;
            safeAreaRect.anchorMax = anchorMax;
        }
    }
}
