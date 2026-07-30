using MobileCore;
using UnityEngine;

namespace Game
{
    public enum GameState { Loading, Playing, Won, Lost }

    /// <summary>
    /// Owns the win and lose decision, and the two states that depend on reading the
    /// whole board at once: the near-loss warning and the endgame run (RULE 7).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private TrackController trackController;
        [SerializeField] private ParkController parkController;
        [SerializeField] private BoardController boardController;
        [SerializeField] private QueueController queueController;
        [SerializeField] private GameConfig config;

        private GameState currentState;
        private bool parkWasFull;
        private bool approachWarned;
        private bool endgameRunning;

        private void OnEnable()
        {
            trackController.OnShooterFinishedLap += HandleLapCompleted;
        }

        private void OnDisable()
        {
            trackController.OnShooterFinishedLap -= HandleLapCompleted;
        }

        public void StartLevel()
        {
            parkWasFull = false;
            approachWarned = false;
            endgameRunning = false;
            currentState = GameState.Playing;
        }

        public void Clear()
        {
            parkWasFull = false;
            approachWarned = false;
            endgameRunning = false;
            currentState = GameState.Loading;
        }

        /// <summary>
        /// A shooter finished its lap with ammo left. There is no grace period: the
        /// warning happens while it is still coming round, so by the time it lands a
        /// full park is simply a loss.
        /// </summary>
        private void HandleLapCompleted(Shooter shooter)
        {
            // In the endgame nobody parks — they keep circling until they run dry.
            if (endgameRunning)
            {
                shooter.ResetLap();
                return;
            }

            if (parkController.TryPark(shooter))
            {
                shooter.IsWaitingForPark = false;
                trackController.ReleaseShooter(shooter);
                return;
            }

            currentState = GameState.Lost;
            GameEvents.TriggerLevelFailed();
        }

        private void Update()
        {
            if (currentState != GameState.Playing) return;

            UpdateEndgame();
            UpdateWarning();
            UpdateSpeed();

            if (boardController.RemainingCubes <= 0)
            {
                currentState = GameState.Won;
                GameEvents.TriggerLevelCompleted();
            }
        }

        private int ShootersLeft =>
            trackController.Count + parkController.Count +
            (queueController != null ? queueController.RemainingCount : 0);

        /// <summary>
        /// Once too few shooters remain to ever fill the park, the level cannot be
        /// lost. Holding the player there is just waiting, so the rail speeds up,
        /// shooters stop parking, and the crates — which only ever existed to block
        /// a lane — lift away.
        /// </summary>
        private void UpdateEndgame()
        {
            if (endgameRunning || config == null) return;
            if (ShootersLeft > config.endgameShooterThreshold) return;

            endgameRunning = true;
            boardController.ClearCrates();
            ReleaseParkedShooters();
        }

        private void ReleaseParkedShooters()
        {
            while (parkController.Count > 0 && trackController.HasFreeTrackSlot)
            {
                if (!parkController.LaunchFirst()) break;
            }
        }

        /// <summary>
        /// Flashes the park twice: once when it fills, and again each time a shooter
        /// enters its run home while it is still full. The second one is the one that
        /// matters — that is the window the player has to act in.
        /// </summary>
        private void UpdateWarning()
        {
            if (config == null || endgameRunning) return;

            bool parkFull = !parkController.HasFreeSlot;

            if (parkFull && !parkWasFull) Warn();
            parkWasFull = parkFull;

            bool approaching = parkFull &&
                               trackController.HasShooterApproachingLapEnd(config.warnLapFraction);

            if (approaching && !approachWarned) Warn();
            approachWarned = approaching;
        }

        private void Warn()
        {
            parkController.PulseWarning(config.warnPulseCount, config.warnPulseSeconds);
            GameEvents.TriggerRescueStarted();
        }

        private void UpdateSpeed()
        {
            if (config == null) return;

            float scale = 1f;
            if (endgameRunning)
                scale = config.endgameSpeedMultiplier;
            else if (trackController.Count + parkController.Count >= config.tensionShooterThreshold)
                scale = config.tensionSpeedMultiplier;

            trackController.SetSpeedScale(scale);
        }
    }
}
