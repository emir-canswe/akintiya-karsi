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
        private TurboSystem turboSystem;
        private bool isMuted;

        public bool IsMuted => isMuted;

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
        }

        public void Vibrate(int milliseconds)
        {
            if (isMuted) return;

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
