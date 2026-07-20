using NUnit.Framework.Internal;
using UnityEngine;

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

        public void LoadLevel(LevelData data)
        {
            float cellSize = config.boardPhysicalSize / data.boardSize.x;

            boardController.Setup(data, cellSize);
            trackController.Init(data.boardSize.x, data.boardSize.y, cellSize, Vector3.zero, data.trackCapacity);
            queueController.Init(data.queue, data.columnCount);
            parkController.Init(data.parkCapacity);
            gameManager.StartLevel(data);

        }

        [ContextMenu("Load Test Level")]
        private void LoadTestLevel()
        {
            LoadLevel(testLevel);
        }
    }
}