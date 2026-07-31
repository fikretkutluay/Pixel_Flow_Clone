using UnityEngine;

namespace Game
{
    /// <summary>
    /// Rayın merkez hattını (atıcıların üzerinde yürüdüğü çizgi) tanımlar.
    /// Path artık board'dan değil BURADAN türüyor — rail sahnede sabit bir
    /// obje olduğu için hizalama board boyutundan bağımsız kalır.
    /// Gizmo, mesh'e gözle hizalamak için merkez hattını çizer.
    /// </summary>
    public class TrackRailAnchor : MonoBehaviour
    {
        [Tooltip("Merkez hattı dikdörtgeninin boyutu (dünya birimi).")]
        [SerializeField] private Vector2 centerlineSize = new Vector2(7.2f, 8.8f);

        [Tooltip("Köşe yuvarlanma yarıçapı. Mesh'in görsel köşe kavisine yaklaştır.")]
        [SerializeField] private float cornerRadius = 1.2f;

        [Tooltip("Path üzerinde '0 noktası'nın faz kayması.")]
        [SerializeField] private float startOffset = 0f;

        [Header("Board alanı")]
        [Tooltip("HER level'ın board'u bu dikdörtgenin içine sığdırılır. Sahne " +
                 "görünümünde yeşil çerçeve olarak çizilir — rayla arasındaki " +
                 "boşluğu sürükleyerek ayarla, board asla dışına taşmaz.")]
        [SerializeField] private Vector2 boardAreaSize = new Vector2(6.5f, 8.1f);

        public float CornerRadius => cornerRadius;
        public float StartOffset => startOffset;

        public Rect GetCenterlineRect() => RectAround(centerlineSize);

        /// <summary>
        /// Board'un sığdırılacağı alan. Ayrı bir dikdörtgen, çünkü ray merkez
        /// hattından sabit bir sayı düşmek yeterli değildi: rayın köşe kavisi ve
        /// mesh kalınlığı yüzünden istenen boşluk dört kenarda aynı değil. Burası
        /// sahnede görünür, böylece göz kararı bir sayı yerine sürüklenerek
        /// ayarlanıyor.
        /// </summary>
        public Rect GetBoardAreaRect() => RectAround(boardAreaSize);

        private Rect RectAround(Vector2 size)
        {
            Vector3 c = transform.position;
            return new Rect(c.x - size.x * 0.5f, c.y - size.y * 0.5f, size.x, size.y);
        }

        private void OnDrawGizmos()
        {
            // Şekil width/height'tan bağımsız (onlar sadece lane sayısı), o yüzden
            // gizmo için sabit bir değer yeterli.
            var preview = new TrackPath(10, 10, GetCenterlineRect(), cornerRadius, 0f);

            Gizmos.color = new Color(1f, 0.85f, 0.2f);
            const int steps = 160;
            Vector3 prev = preview.Evaluate(0f).worldPos;
            for (int i = 1; i <= steps; i++)
            {
                Vector3 cur = preview.Evaluate(preview.Perimeter * i / steps).worldPos;
                Gizmos.DrawLine(prev, cur);
                prev = cur;
            }

            // Board alanı — her level bu çerçevenin içinde kalır.
            Rect area = GetBoardAreaRect();
            Gizmos.color = new Color(0.35f, 1f, 0.45f);
            Gizmos.DrawWireCube(new Vector3(area.center.x, area.center.y, 0f),
                                new Vector3(area.width, area.height, 0f));
        }
    }
}