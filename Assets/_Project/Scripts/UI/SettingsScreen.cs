using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Upstream.Audio;
using Upstream.Core;

namespace Upstream.UI
{
    /// <summary>
    /// Settings panel: sound, vibration, language. Opened from the menu and the pause overlay.
    /// Every change is saved immediately (AudioManager / LocalizationManager persist it).
    /// </summary>
    public class SettingsScreen : MonoBehaviour
    {
        private Button soundButton;
        private Button vibrationButton;
        private Button languageButton;
        private Button closeButton;
        private TMP_Text soundLabel;
        private TMP_Text vibrationLabel;
        private TMP_Text languageLabel;

        public void Configure(Button sound, TMP_Text soundLbl, Button vibration, TMP_Text vibrationLbl,
                              Button language, TMP_Text languageLbl, Button close)
        {
            soundButton = sound;
            soundLabel = soundLbl;
            vibrationButton = vibration;
            vibrationLabel = vibrationLbl;
            languageButton = language;
            languageLabel = languageLbl;
            closeButton = close;

            soundButton.onClick.AddListener(ToggleSound);
            vibrationButton.onClick.AddListener(ToggleVibration);
            languageButton.onClick.AddListener(LocalizationManager.ToggleLanguage);
            closeButton.onClick.AddListener(Close);
            Refresh();
        }

        private void Awake()
        {
            AudioManager.OnSettingsChanged += Refresh;
            LocalizationManager.OnLanguageChanged += Refresh;
            GameEvents.OnGameStart += Close;
            GameEvents.OnGameResume += Close;
        }

        private void OnDestroy()
        {
            AudioManager.OnSettingsChanged -= Refresh;
            LocalizationManager.OnLanguageChanged -= Refresh;
            GameEvents.OnGameStart -= Close;
            GameEvents.OnGameResume -= Close;
        }

        public void Open()
        {
            Refresh();
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private static void ToggleSound()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.ToggleMute();
        }

        private static void ToggleVibration()
        {
            var audio = AudioManager.Instance;
            if (audio == null) return;
            audio.SetVibration(!audio.IsVibrationOn);
            if (audio.IsVibrationOn) audio.Vibrate(40); // feedback: you feel that it's on
        }

        private void Refresh()
        {
            var audio = AudioManager.Instance;
            bool soundOn = audio == null || !audio.IsMuted;
            bool vibrationOn = audio == null || audio.IsVibrationOn;

            if (soundLabel != null) soundLabel.text = LocalizationManager.Get(soundOn ? "sound_on" : "sound_off");
            if (vibrationLabel != null) vibrationLabel.text = LocalizationManager.Get(vibrationOn ? "vib_on" : "vib_off");
            if (languageLabel != null) languageLabel.text = LocalizationManager.Get("lang_btn");
        }
    }
}
