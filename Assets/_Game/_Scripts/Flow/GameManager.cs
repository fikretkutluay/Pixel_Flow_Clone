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

            if (boardController.RemainingCubes <= 0)
            {
                currentState = GameState.Won;
                Debug.Log("WON!");
                GameEvents.TriggerLevelCompleted();
                return;
            }

            rescuedOrExpired.Clear();
            rescueKeysSnapshot.Clear();
            rescueKeysSnapshot.AddRange(rescueTimers.Keys);   // canlı dictionary yerine kopya üzerinde dön

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
                    Debug.Log("LOST - rescue window expired");
                    GameEvents.TriggerLevelFailed();
                    return;
                }

                rescueTimers[shooter] = timeleft;   // artık güvenli — snapshot üzerinde enumerate ediyoruz
            }

            foreach (Shooter shooter in rescuedOrExpired)
            {
                rescueTimers.Remove(shooter);
            }

            if (rescueTimers.Count == 0 && rescuedOrExpired.Count > 0)
            {
                GameEvents.TriggerRescueEnded();
                parkController.SetRescueAlert(false);
            }
        }
    }
}