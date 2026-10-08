using UnityEngine;
using Upstream.Core;
using Upstream.Data;
using Upstream.Mechanics;
using System.Reflection;

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
        private float currentBaseSpeed;
        private bool isTurbo;

        /// <summary>
        /// The active flow speed factoring in state, turbo, and regions.
        /// </summary>
        public float FlowSpeed => GetFlowSpeed();

        /// <summary>
        /// The accumulated scroll offset.
        /// </summary>
        public float ScrollOffset => scrollOffset;

        private void Awake()
        {
            if (scaler == null) scaler = FindObjectOfType<DifficultyScaler>();
            if (scoreSystem == null) scoreSystem = FindObjectOfType<ScoreSystem>();
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

        private void Update()
        {
            if (GameManager.Instance == null) return;

            GameState state = GameManager.Instance.CurrentState;
            if (state == GameState.Playing)
            {
                currentBaseSpeed = scaler.GetCurrentSpeed(currentBaseSpeed, config.maxSpeed, config.speedIncrease, Time.deltaTime);
            }

            float flow = GetFlowSpeed();
            scrollOffset += flow * Time.deltaTime;

            if (state == GameState.Playing && scoreSystem != null)
            {
                scoreSystem.AddDistance(flow * Time.deltaTime);
            }
        }

        /// <summary>
        /// Evaluates and returns the active flow speed based on current game states and conditions.
        /// </summary>
        public float GetFlowSpeed()
        {
            if (GameManager.Instance == null) return 0f;

            GameState state = GameManager.Instance.CurrentState;
            
            if (state == GameState.Menu) return 70f;
            if (state == GameState.Dying) return currentBaseSpeed * 0.3f;
            if (state == GameState.GameOver || state == GameState.Paused) return 0f;

            // Playing state
            float speed = currentBaseSpeed;
            if (isTurbo) speed *= config.turboSpeedMultiplier;

            RegionData region = scaler.GetCurrentRegion(GameManager.Instance.CurrentDistance);
            float regionMultiplier = 1f;
            
            if (region != null)
            {
                FieldInfo field = region.GetType().GetField("speedMultiplier");
                if (field != null) regionMultiplier = (float)field.GetValue(region);
            }

            return speed * regionMultiplier;
        }

        /// <summary>
        /// Resets river properties for a new run.
        /// </summary>
        public void ResetRiver()
        {
            scrollOffset = 0f;
            currentBaseSpeed = config.startSpeed;
            isTurbo = false;
        }
    }
}
