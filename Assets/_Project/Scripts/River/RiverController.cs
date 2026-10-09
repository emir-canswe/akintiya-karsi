using UnityEngine;
using Upstream.Core;
using Upstream.Data;
using Upstream.Mechanics;

namespace Upstream.River
{
    /// <summary>
    /// Controls the flow of the river, managing scroll speed and offsets.
    /// </summary>
    public class RiverController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private DifficultyConfig diffConfig;
        [SerializeField] private DifficultyScaler scaler;
        [SerializeField] private ScoreSystem scoreSystem;

        private float scrollOffset;
        private float currentBaseSpeed = 150f;
        private float slowdownTimer;
        private bool isTurbo;
        // River speed taken away for good by big waves; the distance curve keeps climbing on top of it.
        private float speedRelief;
        // Base speed at which the next set of big waves rolls in.
        private float nextSurgeSpeed = 260f;

        /// <summary>River speed (before turbo and slowdowns) the distance curve currently aims for.</summary>
        public float BaseSpeed => currentBaseSpeed;
        public float SpeedRelief => speedRelief;

        public GameConfig Config
        {
            get
            {
                if (config == null)
                {
                    config = GameManager.Instance != null
                        ? GameManager.Instance.Config
                        : ScriptableObject.CreateInstance<GameConfig>();
                }
                return config;
            }
            set => config = value;
        }

        public DifficultyConfig DiffConfig
        {
            get
            {
                if (diffConfig == null) diffConfig = ScriptableObject.CreateInstance<DifficultyConfig>();
                return diffConfig;
            }
            set => diffConfig = value;
        }

        public float FlowSpeed => GetFlowSpeed();
        public float ScrollOffset => scrollOffset;
        public RegionData ActiveRegion => DifficultyScaler.FindRegion(
            DiffConfig.regions,
            GameManager.Instance != null ? GameManager.Instance.CurrentDistance : 0f);

        private void Awake()
        {
            if (scaler == null) scaler = FindFirstObjectByType<DifficultyScaler>();
            if (scoreSystem == null) scoreSystem = FindFirstObjectByType<ScoreSystem>();
            currentBaseSpeed = Config.startSpeed;
            nextSurgeSpeed = Config.waveSurgeSpeed;
        }

        private void OnEnable()
        {
            GameEvents.OnGameStart += ResetRiver;
            GameEvents.OnReturnToMenu += ResetRiver;
            GameEvents.OnTurboStart += OnTurboStart;
            GameEvents.OnTurboEnd += OnTurboEnd;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= ResetRiver;
            GameEvents.OnReturnToMenu -= ResetRiver;
            GameEvents.OnTurboStart -= OnTurboStart;
            GameEvents.OnTurboEnd -= OnTurboEnd;
        }

        private void OnTurboStart() => isTurbo = true;
        private void OnTurboEnd() => isTurbo = false;

        public void ApplySlowdown(float duration = 0.6f)
        {
            slowdownTimer = Mathf.Max(slowdownTimer, duration);
        }

        /// <summary>A big wave rolled over the fish: the river loses some speed for good, plus a short jolt.</summary>
        public void ApplyBigWave()
        {
            speedRelief += Config.waveSpeedDrop;
            ApplySlowdown(0.5f);
        }

        /// <summary>
        /// Speed the river eases toward: the distance curve minus what big waves took away, never below the start speed.
        /// </summary>
        public static float TargetSpeed(float meters, float startSpeed, float maxSpeed, float relief) =>
            Mathf.Max(startSpeed, DifficultyScaler.SpeedForDistance(meters, startSpeed, maxSpeed) - Mathf.Max(0f, relief));

        private void Update()
        {
            if (GameManager.Instance == null) return;

            if (slowdownTimer > 0f) slowdownTimer -= Time.deltaTime;

            GameState state = GameManager.Instance.CurrentState;
            if (state == GameState.Playing)
            {
                // Steps up every few hundred meters; each step is eased in over about a second so it never jolts.
                float meters = Config.distanceToMetersDivisor > 0f ? GameManager.Instance.CurrentDistance / Config.distanceToMetersDivisor : 0f;
                float target = TargetSpeed(meters, Config.startSpeed, Config.maxSpeed, speedRelief);
                // Speeds up gently; a big wave's slowdown is felt quickly.
                float rate = target < currentBaseSpeed ? 45f : 15f;
                currentBaseSpeed = Mathf.MoveTowards(currentBaseSpeed, target, rate * Time.deltaTime);

                // Very fast, far into the run: send a set of big waves; the next set waits until the river is
                // back at this speed.
                if (meters >= Config.waveSurgeFromMeters && currentBaseSpeed >= nextSurgeSpeed)
                {
                    nextSurgeSpeed = currentBaseSpeed + Mathf.Max(1f, Config.waveSurgeSpeedStep);
                    GameEvents.OnWaveSurge?.Invoke();
                }
            }

            float flow = GetFlowSpeed();
            scrollOffset += flow * Time.deltaTime;

            if (state == GameState.Playing)
            {
                float deltaDist = flow * Time.deltaTime;
                GameManager.Instance.CurrentDistance += deltaDist;
                if (scoreSystem != null)
                {
                    scoreSystem.AddDistance(deltaDist);
                }
            }
        }

        public float GetFlowSpeed()
        {
            if (GameManager.Instance == null) return 70f;

            GameState state = GameManager.Instance.CurrentState;

            if (state == GameState.Menu) return 70f;
            if (state == GameState.Dying) return currentBaseSpeed * 0.3f;
            if (state == GameState.GameOver || state == GameState.Paused) return 0f;

            float speed = currentBaseSpeed;
            if (isTurbo) speed *= Config.turboSpeedMultiplier;
            if (slowdownTimer > 0f) speed *= 0.68f;

            RegionData region = ActiveRegion;
            float regionMultiplier = region != null && region.speedMultiplier > 0f ? region.speedMultiplier : 1f;

            return speed * regionMultiplier;
        }

        public void ResetRiver()
        {
            scrollOffset = 0f;
            currentBaseSpeed = Config.startSpeed;
            slowdownTimer = 0f;
            isTurbo = false;
            speedRelief = 0f;
            nextSurgeSpeed = Config.waveSurgeSpeed;
        }
    }
}
