using UnityEngine;
using MobileCore;
using System.Collections;
using System;
using log4net.Core;

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
            currentLevelIndex ++;

            if (currentLevelIndex >= levels.Length)
            {
                currentLevelIndex = levels.Length - 1;
                Debug.Log("All Levels are Complete!!!");
                return;
            }
            serializer.Save(new SaveData { currentLevelIndex = currentLevelIndex}, "save");
        }

        

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