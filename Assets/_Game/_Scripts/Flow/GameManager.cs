using System.Collections.Generic;
using UnityEngine;
using MobileCore;
namespace Game
{
    public enum GameState { Loading, Playing, Won, Lost }

    public class GameManager : MonoBehaviour
    {
        [SerializeField] private TrackController trackController;
        [SerializeField] private ParkController parkController;
        [SerializeField] private BoardController boardController;
        [SerializeField] private GameConfig config;
        [SerializeField] private LevelData levelData;

        private GameState currentState;
        private readonly Dictionary<Shooter, float> rescueTimers = new Dictionary<Shooter, float>();
        private readonly List<Shooter> rescuedOrExpired = new List<Shooter>();
        private readonly List<Shooter> rescueKeysSnapshot = new List<Shooter>();

        private void OnEnable()
        {
            trackController.OnShooterFinishedLap += HandleLapCompleted;
        }

        private void OnDisable()
        {
            trackController.OnShooterFinishedLap -= HandleLapCompleted;
        }

        public void StartLevel(LevelData data)
        {
            levelData = data;
            rescueTimers.Clear();
            currentState = GameState.Playing;
        }

        public void Clear()
        {
            rescueTimers.Clear();
            rescuedOrExpired.Clear();
            currentState = GameState.Loading;
        }

        /// <summary>
        /// Both buffers filling up is the game's tension curve, so the rail speeds
        /// up to match. This lives here for the same reason the lose decision does
        /// (RULE 7): it is the one place that sees rail and park together.
        /// </summary>
        private void UpdatePressure()
        {
            if (config == null) return;

            int occupied = trackController.Count + parkController.Count;
            trackController.SetUnderPressure(occupied >= config.tensionShooterThreshold);
        }

        private void HandleLapCompleted(Shooter shooter)
        {
            if (parkController.TryPark(shooter))
            {
                shooter.IsWaitingForPark = false;
                trackController.ReleaseShooter(shooter);
                return;
            }

            if (rescueTimers.Count == 0)
            {
                GameEvents.TriggerRescueStarted();
                parkController.SetRescueAlert(true);
            }
            rescueTimers[shooter] = levelData.rescueWindowSeconds;
        }

        private void Update()
        {
            if (currentState != GameState.Playing) return;

            UpdatePressure();

            if (boardController.RemainingCubes <= 0)
            {
                currentState = GameState.Won;
                GameEvents.TriggerLevelCompleted();
                return;
            }

            rescuedOrExpired.Clear();
            rescueKeysSnapshot.Clear();
            rescueKeysSnapshot.AddRange(rescueTimers.Keys);   // Iterate a snapshot, not the live dictionary

            foreach (Shooter shooter in rescueKeysSnapshot)
            {
                float timeleft = rescueTimers[shooter];

                if (parkController.HasFreeSlot)
                {
                    parkController.TryPark(shooter);
                    shooter.IsWaitingForPark = false;
                    trackController.ReleaseShooter(shooter);
                    rescuedOrExpired.Add(shooter);
                    continue;
                }

                timeleft -= Time.deltaTime;
                if (timeleft <= 0)
                {
                    currentState = GameState.Lost;
                    GameEvents.TriggerLevelFailed();
                    return;
                }

                rescueTimers[shooter] = timeleft;   // Safe to mutate now — enumerating the snapshot, not the dictionary
            }

            foreach (Shooter shooter in rescuedOrExpired)
            {
                rescueTimers.Remove(shooter);
            }

            if (rescueTimers.Count == 0)
            {
                parkController.SetRescueAlert(false);
                GameEvents.TriggerRescueEnded();
            }
        }
    }
}