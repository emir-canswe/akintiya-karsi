using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Upstream.Audio;
using Upstream.Core;

namespace Upstream.UI
{
    /// <summary>
    /// Settings panel: sound and vibration switches, TR/EN language picker. Opened from the menu and the pause overlay.
    /// Every change is saved immediately (AudioManager / LocalizationManager persist it).
    /// </summary>
    public class SettingsScreen : MonoBehaviour
    {
        private static readonly Color LangSelectedBg = new Color(1f, 0.54f, 0.36f, 1f);
        private static readonly Color LangSelectedText = new Color(0.17f, 0.05f, 0f, 1f);
        private static readonly Color LangIdleBg = new Color(1f, 1f, 1f, 0f);
        private static readonly Color LangIdleText = new Color(1f, 1f, 1f, 0.7f);
        private const float PopDuration = 0.18f;

        private ToggleSwitch soundSwitch;
        private ToggleSwitch vibrationSwitch;
        private Button turkishButton;
        private Button englishButton;
        private RectTransform card;
        private Button[] sensitivityButtons;
        private float popTime = PopDuration;

        /// <summary>Low / Medium / High steering sensitivity picker (index = ControlSettings level).</summary>
        public void ConfigureSensitivity(Button[] buttons)
        {
            sensitivityButtons = buttons;
            for (int i = 0; i < buttons.Length; i++)
            {
                int level = i;
                buttons[i].onClick.AddListener(() => ControlSettings.SetLevel(level));
            }
            Refresh(false);
        }

        public void Configure(Button soundRow, ToggleSwitch soundSw, Button vibrationRow, ToggleSwitch vibrationSw,
                              Button turkish, Button english, Button close, RectTransform cardRect)
        {
            soundSwitch = soundSw;
            vibrationSwitch = vibrationSw;
            turkishButton = turkish;
            englishButton = english;
            card = cardRect;

            // The whole row is the tap target, not just the small switch.
            soundRow.onClick.AddListener(ToggleSound);
            vibrationRow.onClick.AddListener(ToggleVibration);
            turkishButton.onClick.AddListener(() => LocalizationManager.SetLanguage(LocalizationManager.Turkish));
            englishButton.onClick.AddListener(() => LocalizationManager.SetLanguage(LocalizationManager.English));
            close.onClick.AddListener(Close);
            Refresh(false);
        }

        private void Awake()
        {
            AudioManager.OnSettingsChanged += RefreshAnimated;
            LocalizationManager.OnLanguageChanged += RefreshAnimated;
            ControlSettings.OnChanged += RefreshAnimated;
            GameEvents.OnGameStart += Close;
            GameEvents.OnGameResume += Close;
        }

        private void OnDestroy()
        {
            AudioManager.OnSettingsChanged -= RefreshAnimated;
            LocalizationManager.OnLanguageChanged -= RefreshAnimated;
            ControlSettings.OnChanged -= RefreshAnimated;
            GameEvents.OnGameStart -= Close;
            GameEvents.OnGameResume -= Close;
            UIBackStack.Remove(Close);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            UIBackStack.Push(Close);
            Refresh(false);
            popTime = 0f;
            Update();
        }

        public void Close()
        {
            UIBackStack.Remove(Close);
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (card == null || popTime >= PopDuration) return;
            // Small "pop" when the card appears; unscaled so it also plays while paused.
            popTime = Mathf.Min(popTime + Time.unscaledDeltaTime, PopDuration);
            float k = popTime / PopDuration;
            float scale = Mathf.LerpUnclamped(0.88f, 1f, 1f - (1f - k) * (1f - k));
            card.localScale = new Vector3(scale, scale, 1f);
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

        private void RefreshAnimated() => Refresh(true);

        private void Refresh(bool animate)
        {
            var audio = AudioManager.Instance;
            if (soundSwitch != null) soundSwitch.SetOn(audio == null || !audio.IsMuted, animate);
            if (vibrationSwitch != null) vibrationSwitch.SetOn(audio == null || audio.IsVibrationOn, animate);

            bool turkish = LocalizationManager.CurrentLanguage == LocalizationManager.Turkish;
            StyleLanguageButton(turkishButton, turkish);
            StyleLanguageButton(englishButton, !turkish);

            if (sensitivityButtons != null)
            {
                int level = ControlSettings.Level;
                for (int i = 0; i < sensitivityButtons.Length; i++) StyleLanguageButton(sensitivityButtons[i], i == level);
            }
        }

        private static void StyleLanguageButton(Button button, bool selected)
        {
            if (button == null) return;
            var bg = button.GetComponent<Image>();
            if (bg != null) bg.color = selected ? LangSelectedBg : LangIdleBg;
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.color = selected ? LangSelectedText : LangIdleText;
        }
    }
}
