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

        [Header("Test — Play modunda kullanılır")]
        [Tooltip("1-tabanlı: 'Load Test Level' bu sıradaki level'ı yükler. Kampanya " +
                 "akışını (Level 1'den başlama) atlayıp üstünde çalıştığın level'ı " +
                 "doğrudan test etmek için.")]
        [SerializeField] private int testLevelIndex = 1;

        private void Awake()
        {
            // Kaydedilen ilerleme burada okunmazsa Play her zaman Level 1'den
            // başlar — Save() her level bitişinde çalışıyordu ama onu geri okuyan
            // tek yer bir context menu'ydü ve runtime'da hiçbir şey onu çağırmıyordu.
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
            // Önceki level'dan kalan HER ŞEY burada, tek yerden temizlenir.
            //
            // Eskiden bu dört Clear() çağrısını her çağıran (ReloadLevel, LoadTestLevel)
            // kendi başına tekrarlıyordu — HandlePlayRequested
            // (menüdeki gerçek Play butonu) bunu HİÇ yapmıyordu. Win/Lose panelinin
            // "Devam Et" / "Kapat" butonu sadece ana menüyü açıyor, board'a/park'a/
            // kuyruğa dokunmuyor; oyuncu o an tahtası bitmiş ama parkta veya
            // kuyrukta hâlâ atıcı varken Play'e tekrar basınca yeni level'ın
            // atıcıları TAM O SLOTLARA binip üst üste görünüyordu.
            //
            // Clear() metodlarının hepsi kendi null/uninitialized durumunu koruyor,
            // yani ilk çağrıda (henüz hiçbir şey kurulmamışken) da güvenli.
            gameManager.Clear();
            boardController.Clear();
            parkController.Clear();
            trackController.Clear();
            queueController.Clear();

            currentLevel = data;

            // Board, sahnede çizili board alanının İÇİNE sığdırılır ve küpler
            // küçülür — ray hiç büyümez. Hücre boyutu İKİ eksenin de sığmasına
            // göre seçilir: yalnızca x'e bölmek kareden uzaklaşan her board'da
            // (32x47, 39x27) taşmaya yol açıyordu.
            //
            // Hücreler KARE kalır, o yüzden board alanı doldurmayabilir: board'un
            // en-boy oranı alanınkinden farklıysa artan pay boşluk olarak kalır.
            // Esnetmek küpleri ve dolayısıyla tabloyu bozardı.
            Rect area = railAnchor.GetBoardAreaRect();
            float cellSize = Mathf.Min(area.width / data.boardSize.x,
                                       area.height / data.boardSize.y);

            // Board alanın merkezine oturur — artan pay dört kenara eşit dağılsın.
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
        /// Kampanya sırasını atlayıp doğrudan <see cref="testLevelIndex"/>'teki
        /// level'ı yükler — Play modunda component başlığına sağ tık.
        /// </summary>
        [ContextMenu("Load Test Level")]
        private void LoadTestLevel()
        {
            if (levels == null || levels.Length == 0)
            {
                Debug.LogWarning($"[{name}] levels dizisi boş.");
                return;
            }

            int index = Mathf.Clamp(testLevelIndex - 1, 0, levels.Length - 1);
            currentLevelIndex = index;
            LoadLevel(levels[index]);
        }
    }
}
