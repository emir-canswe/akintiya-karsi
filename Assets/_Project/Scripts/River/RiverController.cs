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
        }

        private void OnEnable()
        {
            GameEvents.OnGameStart += ResetRiver;
            GameEvents.OnTurboStart += OnTurboStart;
            GameEvents.OnTurboEnd += OnTurboEnd;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= ResetRiver;
            GameEvents.OnTurboStart -= OnTurboStart;
            GameEvents.OnTurboEnd -= OnTurboEnd;
        }

        private void OnTurboStart() => isTurbo = true;
        private void OnTurboEnd() => isTurbo = false;

        public void ApplySlowdown(float duration = 0.6f)
        {
            slowdownTimer = Mathf.Max(slowdownTimer, duration);
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;

            if (slowdownTimer > 0f) slowdownTimer -= Time.deltaTime;

            GameState state = GameManager.Instance.CurrentState;
            if (state == GameState.Playing)
            {
                if (scaler != null)
                {
                    currentBaseSpeed = scaler.GetCurrentSpeed(currentBaseSpeed, Config.maxSpeed, Config.speedIncrease, Time.deltaTime);
                }
                else
                {
                    currentBaseSpeed = Mathf.Min(Config.maxSpeed, currentBaseSpeed + Config.speedIncrease * Time.deltaTime);
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
        }
    }
}
