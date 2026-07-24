using UnityEngine;

namespace Game
{
    /// <summary>
    /// Bölge bantlarını Scene/Game view'da gizmo olarak çizer — sadece test için.
    /// OnDrawGizmos build'e girmez. GameLayout ile aynı matematiği kullanır.
    /// </summary>
    public class LayoutGizmos : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private Camera cam;
        [SerializeField] private bool show = true;

        private void OnDrawGizmos()
        {
            if (!show || config == null) return;
            if (cam == null) cam = Camera.main;
            if (cam == null || !cam.orthographic) return;

            float visH = cam.orthographicSize * 2f;
            float visW = visH * cam.aspect;
            float topY = cam.transform.position.y + cam.orthographicSize;
            float cx = cam.transform.position.x;

            float c = 0f;
            DrawBand(cx, topY, visW, visH, c, config.topBarBand, Color.red);    c += config.topBarBand;
            DrawBand(cx, topY, visW, visH, c, config.boardBand,  Color.green);  c += config.boardBand;
            DrawBand(cx, topY, visW, visH, c, config.parkBand,   Color.cyan);   c += config.parkBand;
            DrawBand(cx, topY, visW, visH, c, config.queueBand,  Color.yellow); c += config.queueBand;
            DrawBand(cx, topY, visW, visH, c, config.bottomBand, Color.magenta);

            // Park/kuyruk yatay içerik genişliği sınırı (contentWidthFactor)
            Gizmos.color = Color.white;
            float halfCW = visW * config.contentWidthFactor * 0.5f;
            Gizmos.DrawLine(new Vector3(cx - halfCW, topY, 0), new Vector3(cx - halfCW, topY - visH, 0));
            Gizmos.DrawLine(new Vector3(cx + halfCW, topY, 0), new Vector3(cx + halfCW, topY - visH, 0));
        }

        private void DrawBand(float cx, float topY, float visW, float visH, float cumBefore, float frac, Color col)
        {
            float centerY = topY - (cumBefore + frac * 0.5f) * visH;
            Gizmos.color = col;
            Gizmos.DrawWireCube(new Vector3(cx, centerY, 0f), new Vector3(visW, frac * visH, 0.01f));
        }
    }
}