using UnityEngine;
using MobileCore;

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
        [SerializeField] private LevelData currentLevel;

        [SerializeField] private TrackRailAnchor railAnchor;
        [SerializeField] private TrackChevrons chevrons;

        [Header("Testing — used in Play mode")]
        [Tooltip("One-based: 'Load Test Level' loads the level at this position. " +
                 "Skips the campaign flow (starting from Level 1) so the level " +
                 "being worked on can be tested directly.")]
        [SerializeField] private int testLevelIndex = 1;

        private void Awake()
        {
            // Without reading saved progress here, Play always restarts at Level 1.
            // Save() ran at the end of every level, but the only place that read it
            // back was a context menu, so nothing called it at runtime.
            serializer.Load("save", out SaveData data);
            currentLevelIndex = data != null
                ? Mathf.Clamp(data.currentLevelIndex, 0, Mathf.Max(0, levels.Length - 1))
                : 0;
        }

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
            // Everything left over from the previous level is torn down here, in one
            // place.
            //
            // These four Clear() calls used to be repeated by each caller
            // (ReloadLevel, LoadTestLevel), and HandlePlayRequested — the actual Play
            // button in the menu — never made them at all. The win/lose panel's
            // continue and close buttons only open the main menu; they do not touch
            // the board, park or queue. So if the player pressed Play again while
            // shooters were still parked or queued, the new level's shooters landed
            // on those exact slots and overlapped them.
            //
            // Every Clear() guards its own null/uninitialised state, so this is safe
            // on the first call too, before anything has been built.
            gameManager.Clear();
            boardController.Clear();
            parkController.Clear();
            trackController.Clear();
            queueController.Clear();

            currentLevel = data;

            // The board is fitted inside the board area drawn in the scene and the
            // cubes shrink to suit; the rail never grows. Cell size is chosen so
            // both axes fit — dividing by x alone overflowed on every board far
            // from square (32x47, 39x27).
            //
            // Cells stay square, so the board may not fill the area: when its
            // aspect ratio differs from the area's, the surplus is left as margin.
            // Stretching would distort the cubes and with them the picture.
            Rect area = railAnchor.GetBoardAreaRect();
            float cellSize = Mathf.Min(area.width / data.boardSize.x,
                                       area.height / data.boardSize.y);

            // Centre the board in the area so the surplus splits evenly on all sides.
            Vector3 boardOrigin = new Vector3(
                area.center.x - (data.boardSize.x - 1) * cellSize * 0.5f,
                area.center.y - (data.boardSize.y - 1) * cellSize * 0.5f,
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

        public void ReloadLevel() => LoadLevel(currentLevel);

        /// <summary>
        /// Skips the campaign order and loads the level at <see cref="testLevelIndex"/>.
        /// Right-click the component header in Play mode.
        /// </summary>
        [ContextMenu("Load Test Level")]
        private void LoadTestLevel()
        {
            if (levels == null || levels.Length == 0)
            {
                Debug.LogWarning($"[{name}] The levels array is empty.");
                return;
            }

            int index = Mathf.Clamp(testLevelIndex - 1, 0, levels.Length - 1);
            currentLevelIndex = index;
            LoadLevel(levels[index]);
        }
    }
}
