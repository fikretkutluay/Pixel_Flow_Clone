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

        public float CornerRadius => cornerRadius;
        public float StartOffset => startOffset;

        public Rect GetCenterlineRect()
        {
            Vector3 c = transform.position;
            return new Rect(
                c.x - centerlineSize.x * 0.5f,
                c.y - centerlineSize.y * 0.5f,
                centerlineSize.x,
                centerlineSize.y);
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
        }
    }
}