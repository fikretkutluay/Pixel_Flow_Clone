using UnityEngine;
using MobileCore;
using System.Collections;
using System;

namespace Game
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private BoardController boardController;
        [SerializeField] private TrackController trackController;
        [SerializeField] private QueueController queueController;
        [SerializeField] private ParkController parkController;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameConfig config;
        [SerializeField] private LevelData[] levels;
        private int currentLevelIndex;
        private readonly ISerializer serializer = new JsonSaveSystem();
        [SerializeField] private LevelData testLevel;
        [SerializeField] private LevelData currentLevel;
        [SerializeField] private LevelData nextTestLevel;   // rescue senaryosu için crate/park-full level

        [SerializeField] private float midLevelDelaySeconds = 0.5f;
        [SerializeField] private float rescueTestDelaySeconds = 6f;

        [SerializeField] private TrackRailAnchor railAnchor;
        [SerializeField] private TrackChevrons chevrons;

        private void OnEnable()
        {
            GameEvents.OnLevelCompleted += HandleLevelCompleted;
            GameEvents.OnPlayRequested += HandlePlayRequested;
            GameEvents.OnRetryRequested += ReloadLevel;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelCompleted -= HandleLevelCompleted;
            GameEvents.OnPlayRequested -= HandlePlayRequested;
            GameEvents.OnRetryRequested -= ReloadLevel;
        }

        private void HandlePlayRequested()
        {
            LoadLevel(levels[currentLevelIndex]);
        }

        private void HandleLevelCompleted()
        {
            currentLevelIndex++;

            if (currentLevelIndex >= levels.Length)
            {
                // Final level stays clamped; nothing further to persist.
                currentLevelIndex = levels.Length - 1;
                return;
            }
            serializer.Save(new SaveData { currentLevelIndex = currentLevelIndex }, "save");
        }

        public void LoadLevel(LevelData data)
        {
            currentLevel = data;

            // Ray sabit bir dikdörtgen, board onun İÇİNE oturur ve küpler küçülür.
            // Hücre boyutu bu yüzden İKİ eksenin de sığmasına göre seçilmeli:
            // yalnızca x'e bölmek kareden uzaklaşan her board'da (32x47, 39x27)
            // dikeyde ya da yatayda rayın dışına taşırıyordu.
            Rect rail = railAnchor.GetCenterlineRect();
            float gap = config.boardRailGap;
            float cellSize = Mathf.Min(
                (rail.width - 2f * gap) / data.boardSize.x,
                (rail.height - 2f * gap) / data.boardSize.y);

            // Board rayın merkezine oturur — boşluk dört kenarda da eşit kalsın.
            Vector3 boardOrigin = new Vector3(
                rail.center.x - (data.boardSize.x - 1) * cellSize * 0.5f,
                rail.center.y - (data.boardSize.y - 1) * cellSize * 0.5f,
                0f);

            boardController.Setup(data, cellSize, boardOrigin);
            trackController.Init(data.boardSize.x, data.boardSize.y,
                railAnchor.GetCenterlineRect(), data.trackCapacity, config.trackLapSeconds,
                railAnchor.CornerRadius, railAnchor.StartOffset, config.tensionRampSeconds);
            queueController.Init(data.queue, data.columnCount);
            parkController.Init(data.parkCapacity);
            if (chevrons != null) chevrons.Rebuild();   // needs the path Init just built
            gameManager.StartLevel();

            // levelID is authored per asset; test levels leave it at 0, so fall
            // back to the position in the campaign list.
            GameEvents.TriggerLevelStarted(data.levelID > 0 ? data.levelID : currentLevelIndex + 1);
        }

        public void ReloadLevel()
        {
            gameManager.Clear();
            boardController.Clear();
            parkController.Clear();
            trackController.Clear();
            queueController.Clear();
            LoadLevel(currentLevel);
        }

        public void LoadNext(LevelData newData)
        {
            gameManager.Clear();
            boardController.Clear();
            parkController.Clear();
            trackController.Clear();
            queueController.Clear();
            LoadLevel(newData);
        }

        [ContextMenu("Continue From Save")]
        private void ContinueFromSave()
        {
            serializer.Load("save", out SaveData data);

            currentLevelIndex = data != null ? data.currentLevelIndex : 0;
            currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, levels.Length - 1);

            LoadLevel(levels[currentLevelIndex]);
        }

        [ContextMenu("Load Test Level")]
        private void LoadTestLevel()
        {
            LoadLevel(testLevel);
        }

        [ContextMenu("Reload Level")]
        private void ReloadTestLevel()
        {
            ReloadLevel();
        }

        [ContextMenu("Load Next Test Level")]
        private void LoadNextTestLevel()
        {
            LoadNext(nextTestLevel);
        }

        [ContextMenu("Log Pool Status")]
        private void LogPoolStatus()
        {
            int available = ObjectPooler.Instance.GetAvailableCount("Shooter");
            Debug.Log($"[Pool] 'Shooter' havuzda bekleyen (kullanılmayan) obje sayısı: {available}");
        }

        [ContextMenu("Test: Auto Reload Mid-Flight (testLevel)")]
        private void TestAutoReloadMidLevel()
        {
            StartCoroutine(AutoReloadTestRoutine(testLevel, midLevelDelaySeconds));
        }

        [ContextMenu("Test: Auto Reload During Rescue (nextTestLevel)")]
        private void TestAutoReloadDuringRescue()
        {
            StartCoroutine(AutoReloadTestRoutine(nextTestLevel, rescueTestDelaySeconds));
        }

        private IEnumerator AutoReloadTestRoutine(LevelData level, float delaySeconds)
        {
            LoadLevel(level);
            yield return null;   // bir frame bekle, Init'ler otursun

            LogPoolStatus();

            Shooter s = queueController.PeekTopShooter(0);
            if (s != null)
                queueController.OnShooterTapped(s);   // Same path as a real tap
            else
                Debug.LogWarning("No shooter found in queue — check the level data's queue[0].column value.");

            yield return new WaitForSeconds(delaySeconds);

            ReloadLevel();
            LogPoolStatus();
        }
    }
}