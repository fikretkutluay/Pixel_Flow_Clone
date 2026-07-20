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

        private void OnEnable()
        {
            trackController.OnShooterFinishedLap += HandleLapCompleted;
        }

        private void OnDisable()
        {
            trackController.OnShooterFinishedLap -= HandleLapCompleted;
        }

        private void HandleLapCompleted(Shooter shooter)
        {
            if (parkController.TryPark(shooter))
            {
                shooter.IsWaitingForPark = false;
                trackController.ReleaseShooter(shooter);
                return;
            }
            rescueTimers[shooter] = levelData.rescueWindowSeconds;
        }

        private void Update()
        {
            if (currentState != GameState.Playing ) return;

            if (boardController.RemainingCubes <= 0)
            {
                currentState = GameState.Won;
                GameEvents.TriggerLevelCompleted();
                return;
            } 
            rescuedOrExpired.Clear();
            foreach (KeyValuePair<Shooter, float> kvp in rescueTimers)
            {
                Shooter shooter = kvp.Key;
                float timeleft = kvp.Value;

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
                rescueTimers[shooter] = timeleft;
            }
            foreach (Shooter shooter in rescuedOrExpired)
            {
                rescueTimers.Remove(shooter);
            }
        }
    }
}