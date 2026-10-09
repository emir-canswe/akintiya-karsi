using UnityEngine;
using UnityEngine.UI;
using Upstream.Core;

namespace Upstream.UI
{
    /// <summary>
    /// Pause menu shown when the game pauses (Android back button, app backgrounded):
    /// Resume, Settings (wired by the bootstrap) and Main Menu (ends the run; its coins still count).
    /// </summary>
    public class PauseOverlay : MonoBehaviour
    {
        private Button resumeButton;
        private Button menuButton;

        public void Configure(Button resume, Button menu)
        {
            resumeButton = resume;
            menuButton = menu;
            resumeButton.onClick.AddListener(Resume);
            menuButton.onClick.AddListener(QuitToMenu);
        }

        // Awake/OnDestroy subscriptions: this object is hidden most of the time.
        private void Awake()
        {
            GameEvents.OnGamePause += Show;
            GameEvents.OnGameResume += Hide;
            GameEvents.OnGameStart += Hide;
            GameEvents.OnGameOver += Hide;
            GameEvents.OnReturnToMenu += Hide;
        }

        private void OnDestroy()
        {
            GameEvents.OnGamePause -= Show;
            GameEvents.OnGameResume -= Hide;
            GameEvents.OnGameStart -= Hide;
            GameEvents.OnGameOver -= Hide;
            GameEvents.OnReturnToMenu -= Hide;
        }

        private void Show() => gameObject.SetActive(true);
        private void Hide() => gameObject.SetActive(false);

        private void Resume()
        {
            if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
        }

        private void QuitToMenu()
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitRunToMenu();
        }
    }
}
