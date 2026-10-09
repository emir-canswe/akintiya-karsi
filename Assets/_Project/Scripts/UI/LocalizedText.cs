using TMPro;
using UnityEngine;
using Upstream.Core;

namespace Upstream.UI
{
    /// <summary>
    /// Keeps a TMP label in sync with a localization key, including live language switches.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;

        private TMP_Text label;

        public string Key
        {
            get => key;
            set
            {
                key = value;
                Refresh();
            }
        }

        // Awake/OnDestroy so labels on hidden screens are already correct when the screen opens.
        private void Awake()
        {
            label = GetComponent<TMP_Text>();
            LocalizationManager.OnLanguageChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            LocalizationManager.OnLanguageChanged -= Refresh;
        }

        public void Refresh()
        {
            if (label == null) label = GetComponent<TMP_Text>();
            if (label != null && !string.IsNullOrEmpty(key)) label.text = LocalizationManager.Get(key);
        }
    }
}
