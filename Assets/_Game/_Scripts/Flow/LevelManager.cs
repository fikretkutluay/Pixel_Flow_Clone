using UnityEngine;
using MobileCore;
using System.Collections;

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

        [SerializeField] private LevelData testLevel;
        [SerializeField] private LevelData currentLevel;
        [SerializeField] private LevelData nextTestLevel;   // rescue senaryosu için crate/park-full level

        [SerializeField] private float midLevelDelaySeconds = 0.5f;
        [SerializeField] private float rescueTestDelaySeconds = 6f;

        public void LoadLevel(LevelData data)
        {
            currentLevel = data;
            float cellSize = config.boardPhysicalSize / data.boardSize.x;

            boardController.Setup(data, cellSize);
            trackController.Init(data.boardSize.x, data.boardSize.y, cellSize, Vector3.zero, data.trackCapacity);
            queueController.Init(data.queue, data.columnCount);
            parkController.Init(data.parkCapacity);
            gameManager.StartLevel(data);
        }

        public void ReloadLevel()
        {
            gameManager.Clear();
            parkController.Clear();
            trackController.Clear();
            queueController.Clear();
            LoadLevel(currentLevel);
        }

        public void LoadNext(LevelData newData)
        {
            gameManager.Clear();
            parkController.Clear();
            trackController.Clear();
            queueController.Clear();
            LoadLevel(newData);
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
                queueController.OnShooterTapped(s);   // gerçek tap akışının aynısı
            else
                Debug.LogWarning("Kuyrukta shooter bulunamadı — level'daki queue[0].column değerini kontrol et.");

            yield return new WaitForSeconds(delaySeconds);

            Debug.Log($"--- {delaySeconds}s sonra Reload tetikleniyor ---");
            ReloadLevel();
            LogPoolStatus();
        }
    }
}