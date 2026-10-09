using UnityEngine;
using UnityEngine.UI;
using Upstream.Core;

namespace Upstream.UI
{
    /// <summary>
    /// Full-screen "paused, tap to continue" overlay. Shown when the game pauses (app backgrounded,
    /// Android back button). Resumes on tap release, so the finger doesn't yank the fish on resume.
    /// </summary>
    public class PauseOverlay : MonoBehaviour
    {
        // Awake/OnDestroy subscriptions: this object is hidden most of the time.
        private void Awake()
        {
            var tapArea = GetComponent<Button>();
            if (tapArea != null) tapArea.onClick.AddListener(Resume);

            GameEvents.OnGamePause += Show;
            GameEvents.OnGameResume += Hide;
            GameEvents.OnGameStart += Hide;
            GameEvents.OnGameOver += Hide;
        }

        private void OnDestroy()
        {
            GameEvents.OnGamePause -= Show;
            GameEvents.OnGameResume -= Hide;
            GameEvents.OnGameStart -= Hide;
            GameEvents.OnGameOver -= Hide;
        }

        private void Show() => gameObject.SetActive(true);
        private void Hide() => gameObject.SetActive(false);

        private void Resume()
        {
            if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
        }
    }
}
