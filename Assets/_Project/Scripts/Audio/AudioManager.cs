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

        /// <summary>Raised when sound or vibration settings change (keeps every toggle label in sync).</summary>
        public static event System.Action OnSettingsChanged;

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
        [SerializeField] private AudioClip waveSplash;

        private AudioSource turboSource;
        private TurboSystem turboSystem;
        private bool isMuted;
        private bool vibrationOn = true;

        public bool IsMuted => isMuted;
        public bool IsVibrationOn => vibrationOn;

        public void SetClips(
            AudioClip eat,
            AudioClip eatGold,
            AudioClip tStart,
            AudioClip tLoop,
            AudioClip hurtClip,
            AudioClip dieClip,
            AudioClip sharkClip,
            AudioClip stageClip,
            AudioClip clickClip,
            AudioClip ambientClip)
        {
            eatFood = eat;
            eatGoldFood = eatGold;
            turboStart = tStart;
            turboLoop = tLoop;
            hurt = hurtClip;
            die = dieClip;
            sharkEat = sharkClip;
            stageUp = stageClip;
            buttonClick = clickClip;
            ambientWater = ambientClip;

            if (ambientSource != null && ambientWater != null && !ambientSource.isPlaying)
            {
                ambientSource.clip = ambientWater;
                ambientSource.volume = 0.22f;
                ambientSource.Play();
            }
        }

        /// <summary>Crashing-water sound for the big speed-limit waves.</summary>
        public void SetWaveSplash(AudioClip clip) => waveSplash = clip;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                if (sfxSources == null || sfxSources.Length == 0)
                {
                    sfxSources = new AudioSource[6];
                    for (int i = 0; i < sfxSources.Length; i++)
                    {
                        sfxSources[i] = gameObject.AddComponent<AudioSource>();
                        sfxSources[i].playOnAwake = false;
                    }
                }

                if (ambientSource == null)
                {
                    ambientSource = gameObject.AddComponent<AudioSource>();
                    ambientSource.loop = true;
                    ambientSource.playOnAwake = false;
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
            vibrationOn = saveData.settingsVibrationOn;
            SetMuted(!saveData.settingsSoundOn);

            if (ambientWater != null && !ambientSource.isPlaying)
            {
                ambientSource.clip = ambientWater;
                ambientSource.volume = 0.22f;
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
            GameEvents.OnCoinCollected += HandleCoinCollected;
            GameEvents.OnWaveHit += HandleWaveHit;
            GameEvents.OnShieldBlocked += HandleShieldBlocked;
            GameEvents.OnWaveSurge += HandleWaveSurge;
            GameEvents.OnBigWaveHit += HandleBigWaveHit;
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
            GameEvents.OnCoinCollected -= HandleCoinCollected;
            GameEvents.OnWaveHit -= HandleWaveHit;
            GameEvents.OnShieldBlocked -= HandleShieldBlocked;
            GameEvents.OnWaveSurge -= HandleWaveSurge;
            GameEvents.OnBigWaveHit -= HandleBigWaveHit;
        }

        public void PlaySFX(AudioClip clip, float pitch = 1f)
        {
            if (isMuted || clip == null || sfxSources == null) return;

            foreach (var source in sfxSources)
            {
                if (!source.isPlaying)
                {
                    source.pitch = pitch;
                    source.PlayOneShot(clip);
                    return;
                }
            }

            if (sfxSources.Length > 0)
            {
                sfxSources[0].pitch = pitch;
                sfxSources[0].PlayOneShot(clip);
            }
        }

        public void PlaySFXPitchVaried(AudioClip clip, float minPitch, float maxPitch)
        {
            PlaySFX(clip, Random.Range(minPitch, maxPitch));
        }

        public void StopAmbient()
        {
            if (ambientSource != null) ambientSource.Stop();
        }

        public void ToggleMute()
        {
            SetMuted(!isMuted);
            var saveData = SaveSystem.Load();
            saveData.settingsSoundOn = !isMuted;
            SaveSystem.Save(saveData);
        }

        public void SetMuted(bool muted)
        {
            isMuted = muted;
            AudioListener.volume = muted ? 0f : 1f;
            OnSettingsChanged?.Invoke();
        }

        public void SetVibration(bool on)
        {
            vibrationOn = on;
            var saveData = SaveSystem.Load();
            saveData.settingsVibrationOn = on;
            SaveSystem.Save(saveData);
            OnSettingsChanged?.Invoke();
        }

        public void Vibrate(int milliseconds)
        {
            // Vibration has its own setting; muting sound shouldn't silently kill haptics.
            if (!vibrationOn) return;

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
            catch (System.Exception)
            {
                Handheld.Vibrate();
            }
#endif
        }

        // Bright "ding": the gold-food chime pitched up, slightly varied so a coin trail sounds lively.
        private void HandleCoinCollected(int amount) => PlaySFXPitchVaried(eatGoldFood, 1.75f, 1.95f);

        // Low "whoosh": the hurt sound pitched down, plus a short buzz.
        private void HandleWaveHit()
        {
            PlaySFX(hurt, 0.6f);
            Vibrate(30);
        }

        // Distant rumble as the set appears, a full crash as each big wave breaks over the fish.
        private void HandleWaveSurge() => PlaySFX(waveSplash, 0.55f);

        private void HandleBigWaveHit()
        {
            PlaySFXPitchVaried(waveSplash, 0.9f, 1.1f);
            Vibrate(45);
        }

        // Shield pop: a bright high chime instead of the hurt sound
        private void HandleShieldBlocked(int chargesLeft)
        {
            PlaySFX(eatGoldFood, 1.35f);
            Vibrate(25);
        }

        private void HandleFoodEaten(bool isGold)
        {
            if (isGold)
            {
                PlaySFX(eatGoldFood, 1f);
            }
            else
            {
                if (turboSystem == null) turboSystem = FindFirstObjectByType<TurboSystem>();
                float powerPct = turboSystem != null ? turboSystem.PowerPercent : 0f;
                float pitch = 0.95f + (powerPct * 0.55f);
                PlaySFXPitchVaried(eatFood, pitch - 0.03f, pitch + 0.03f);
            }
        }

        private void HandleTurboStart()
        {
            PlaySFX(turboStart, 1f);

            if (!isMuted && turboLoop != null && sfxSources != null)
            {
                foreach (var source in sfxSources)
                {
                    if (!source.isPlaying)
                    {
                        turboSource = source;
                        turboSource.clip = turboLoop;
                        turboSource.loop = true;
                        turboSource.pitch = 1f;
                        turboSource.Play();
                        break;
                    }
                }
            }
            Vibrate(45);
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
            PlaySFX(hurt, 1f);
            Vibrate(80);
        }

        private void HandlePlayerDied()
        {
            HandleTurboEnd();
            PlaySFX(die, 1f);
            Vibrate(150);
        }

        private void HandleSharkEaten()
        {
            PlaySFX(sharkEat, 1f);
            Vibrate(90);
        }

        private void HandleGrowthStageChanged(int stage)
        {
            if (stage > 0)
            {
                PlaySFX(stageUp, 1f + stage * 0.06f);
                Vibrate(60);
            }
        }
    }
}
