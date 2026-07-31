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
            // Level'in sonucu belliyse hicbir tur artik bir sey degistirmemeli.
            // Bu kontrol yokken kazanilan bir level'da bile parka sigmayan bir
            // atici kaybi tetikleyebiliyordu.
            if (currentState != GameState.Playing) return;

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

            Finish(GameState.Lost);
        }

        /// <summary>
        /// Sonucu BİR KEZ ilan eder ve rayı durdurur.
        ///
        /// İkisi de şarttı: ray dönmeye devam ettiği için atıcılar yeni tur
        /// bitirip kaybı tekrar tetikliyordu, ve UIManager.SwitchPanel aynı
        /// paneli ikinci kez açarken önce Hide'ını çağırıyor — o Hide'ın
        /// OnComplete'i paneli kapatıyor ve kayıp ekranı hiç görünmüyordu.
        /// Ray durmayınca ayrıca level bittikten sonra da atıcılar turlamaya
        /// devam edip ekranda takılı kalıyordu.
        /// </summary>
        private void Finish(GameState result)
        {
            if (currentState != GameState.Playing) return;

            currentState = result;
            trackController.SetRunning(false);

            if (result == GameState.Won) GameEvents.TriggerLevelCompleted();
            else GameEvents.TriggerLevelFailed();
        }

        private void Update()
        {
            if (currentState != GameState.Playing) return;

            UpdateEndgame();
            UpdateWarning();
            UpdateSpeed();

            if (boardController.RemainingCubes <= 0)
                Finish(GameState.Won);
        }

        private int ShootersLeft =>
            trackController.Count + parkController.Count +
            (queueController != null ? queueController.RemainingCount : 0);

        /// <summary>
        /// Once too few shooters remain to ever fill the park, the level cannot be
        /// lost. Holding the player there is just waiting, so the rail speeds up,
        /// shooters stop parking, and the crates — which only ever existed to block
        /// a lane — lift away.
        ///
        /// Deliberately does NOT empty the park or the queue. Only the shooters
        /// already on the rail keep circling; the rest wait for the player to send
        /// them, exactly as they do the rest of the level. Auto-launching them took
        /// the last of the aiming away and finished the level on its own.
        /// </summary>
        private void UpdateEndgame()
        {
            if (endgameRunning || config == null) return;
            if (ShootersLeft > config.endgameShooterThreshold) return;

            endgameRunning = true;
            boardController.ClearCrates();
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
