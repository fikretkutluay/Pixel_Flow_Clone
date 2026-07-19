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
        [SerializeField] private LevelData levelData;   // geçici — M3'te LevelManager verecek

        private GameState currentState;
        private readonly Dictionary<Shooter, float> rescueTimers = new Dictionary<Shooter, float>();
        private readonly List<Shooter> rescuedOrExpired = new List<Shooter>();   // döngü içi silme için

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
            /*
            if parkController.TryPark(shooter):
                shooter.IsWaitingForPark = false
                trackController.ReleaseShooter(shooter)
                return

            // park dolu → kurtarma penceresi başlat
            rescueTimers[shooter] = levelData.rescueWindowSeconds
            */
        }

        private void Update()
        {
            /*
            if currentState != Playing: return

            // 1. kazanma kontrolü
            if boardController.RemainingCubes <= 0:
                currentState = Won
                GameEvents.TriggerLevelCompleted()
                return

            // 2. kurtarma pencereleri
            rescuedOrExpired.Clear()

            foreach (kvp in rescueTimers):     // Dictionary üzerinde SADECE okuma yapıyoruz burada
                shooter = kvp.Key
                timeLeft = kvp.Value

                if parkController.HasFreeSlot:
                    parkController.TryPark(shooter)
                    shooter.IsWaitingForPark = false
                    trackController.ReleaseShooter(shooter)
                    rescuedOrExpired.Add(shooter)
                    continue

                timeLeft -= Time.deltaTime
                if timeLeft <= 0:
                    currentState = Lost
                    GameEvents.TriggerLevelFailed()
                    return                       // tek kayıp yeter

                rescueTimers[shooter] = timeLeft   // güncellenmiş süreyi geri yaz

            // 3. döngü bitti, artık güvenle silebiliriz
            foreach (shooter in rescuedOrExpired):
                rescueTimers.Remove(shooter)
            */
        }
    }
}