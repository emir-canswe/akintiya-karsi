using System;
using UnityEngine;
#if USE_ADMOB
using GoogleMobileAds.Ump.Api;
#endif

namespace Upstream.Ads
{
    /// <summary>
    /// Handles Google User Messaging Platform (UMP) consent for EU (GDPR) and Turkey (KVKK) users.
    /// </summary>
    public class UmpConsentService : MonoBehaviour
    {
        public bool HasConsent { get; private set; }

        public void RequestConsent(Action<bool> onComplete)
        {
#if USE_ADMOB
            var request = new ConsentRequestParameters { TagForUnderAgeOfConsent = false };
            ConsentInformation.Update(request, error =>
            {
                if (error != null)
                {
                    Debug.LogWarning($"[UMP] Consent update error: {error.Message}");
                    HasConsent = false;
                    onComplete?.Invoke(false);
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    if (formError != null)
                    {
                        Debug.LogWarning($"[UMP] Consent form error: {formError.Message}");
                    }
                    HasConsent = ConsentInformation.CanRequestAds();
                    onComplete?.Invoke(HasConsent);
                });
            });
#else
            HasConsent = true;
            Debug.Log("[UMP] Mock consent granted (USE_ADMOB not defined).");
            onComplete?.Invoke(true);
#endif
        }
    }
}
