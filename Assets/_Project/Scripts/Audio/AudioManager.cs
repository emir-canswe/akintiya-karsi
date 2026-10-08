using UnityEngine;
using Upstream.Core;
using Upstream.Mechanics;

namespace Upstream.Audio
{
    /// <summary>
    /// Manages audio playback for SFX and ambient music.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource[] sfxSources;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip eatFood;
        [SerializeField] private AudioClip eatGoldFood;
        [SerializeField] private AudioClip turboStart;
        [SerializeField] private AudioClip turboLoop;
        [SerializeField] private AudioClip hurt;
        [SerializeField] private AudioClip die;
        [SerializeField] private AudioClip sharkEat;
        [SerializeField] private AudioClip stageUp;
        [SerializeField] private AudioClip buttonClick;
        [SerializeField] private AudioClip ambientWater;

        private AudioSource turboSource;
        private bool isMuted;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Initialize default setup if not assigned
                if (sfxSources == null || sfxSources.Length == 0)
                {
                    sfxSources = new AudioSource[4];
                    for (int i = 0; i < sfxSources.Length; i++)
                    {
                        sfxSources[i] = gameObject.AddComponent<AudioSource>();
                    }
                }
                
                if (ambientSource == null)
                {
                    ambientSource = gameObject.AddComponent<AudioSource>();
                    ambientSource.loop = true;
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            var saveData = SaveSystem.Load();
            SetMuted(saveData.IsMuted);

            if (ambientWater != null)
            {
                ambientSource.clip = ambientWater;
                ambientSource.Play();
            }
        }

        private void OnEnable()
        {
            GameEvents.OnFoodEaten += HandleFoodEaten;
            GameEvents.OnTurboStart += HandleTurboStart;
            GameEvents.OnTurboEnd += HandleTurboEnd;
            GameEvents.OnPlayerHurt += HandlePlayerHurt;
            GameEvents.OnPlayerDied += HandlePlayerDied;
            GameEvents.OnSharkEaten += HandleSharkEaten;
            GameEvents.OnGrowthStageChanged += HandleGrowthStageChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnFoodEaten -= HandleFoodEaten;
            GameEvents.OnTurboStart -= HandleTurboStart;
            GameEvents.OnTurboEnd -= HandleTurboEnd;
            GameEvents.OnPlayerHurt -= HandlePlayerHurt;
            GameEvents.OnPlayerDied -= HandlePlayerDied;
            GameEvents.OnSharkEaten -= HandleSharkEaten;
            GameEvents.OnGrowthStageChanged -= HandleGrowthStageChanged;
        }

        public void PlaySFX(AudioClip clip)
        {
            if (isMuted || clip == null) return;

            foreach (var source in sfxSources)
            {
                if (!source.isPlaying)
                {
                    source.pitch = 1f;
                    source.PlayOneShot(clip);
                    return;
                }
            }

            // Fallback: use first source if all busy
            if (sfxSources.Length > 0)
            {
                sfxSources[0].pitch = 1f;
                sfxSources[0].PlayOneShot(clip);
            }
        }

        public void PlaySFXPitchVaried(AudioClip clip, float minPitch, float maxPitch)
        {
            if (isMuted || clip == null) return;

            foreach (var source in sfxSources)
            {
                if (!source.isPlaying)
                {
                    source.pitch = Random.Range(minPitch, maxPitch);
                    source.PlayOneShot(clip);
                    return;
                }
            }
        }

        public void StopAmbient()
        {
            ambientSource.Stop();
        }

        public void SetMuted(bool muted)
        {
            isMuted = muted;
            AudioListener.volume = muted ? 0f : 1f;
        }

        public void Vibrate(int milliseconds)
        {
            if (isMuted) return; // Usually you might want a separate setting for vibration, but tying it to mute for now.
            
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    vibrator.Call("vibrate", (long)milliseconds);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Vibration failed: " + e.Message);
                Handheld.Vibrate();
            }
#else
            Handheld.Vibrate();
#endif
        }

        private void HandleFoodEaten(bool isGold)
        {
            if (isGold)
            {
                PlaySFX(eatGoldFood);
            }
            else
            {
                // Slight pitch increase based on power level
                float pitch = 1f + (TurboSystem.PowerPercent * 0.5f);
                PlaySFXPitchVaried(eatFood, pitch - 0.05f, pitch + 0.05f);
            }
        }

        private void HandleTurboStart()
        {
            PlaySFX(turboStart);
            
            if (isMuted) return;

            foreach (var source in sfxSources)
            {
                if (!source.isPlaying)
                {
                    turboSource = source;
                    turboSource.clip = turboLoop;
                    turboSource.loop = true;
                    turboSource.Play();
                    break;
                }
            }
            Vibrate(50);
        }

        private void HandleTurboEnd()
        {
            if (turboSource != null)
            {
                turboSource.Stop();
                turboSource.loop = false;
                turboSource = null;
            }
        }

        private void HandlePlayerHurt(int currentHp)
        {
            PlaySFX(hurt);
            Vibrate(100);
        }

        private void HandlePlayerDied()
        {
            PlaySFX(die);
            Vibrate(200);
        }

        private void HandleSharkEaten()
        {
            PlaySFX(sharkEat); // Deep bass hit
            Vibrate(100);
        }

        private void HandleGrowthStageChanged(int stage)
        {
            PlaySFX(stageUp);
        }
    }
}
