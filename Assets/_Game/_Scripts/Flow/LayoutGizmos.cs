using UnityEngine;

namespace Game
{
    public class LayoutGizmos : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private Camera cam;
        [SerializeField] private bool show = true;

        // LayoutGizmosEditor reads these to drive its draggable handles.
        public GameConfig GizmoConfig => config;
        public Camera GizmoCamera { get { if (cam == null) cam = Camera.main; return cam; } }

        private void OnDrawGizmos()
        {
            if (!show || config == null) return;
            Camera c = GizmoCamera;
            if (c == null || !c.orthographic) return;

            float cx = c.transform.position.x;
            float visW = GameLayout.VisibleWidth(c);

            float cum = 0f;
            DrawBand(c, cx, visW, cum, config.topBarBand, Color.red);    cum += config.topBarBand;
            DrawBand(c, cx, visW, cum, config.boardBand,  Color.green); cum += config.boardBand;
            DrawBand(c, cx, visW, cum, config.parkBand,   Color.cyan);  cum += config.parkBand;
            DrawBand(c, cx, visW, cum, config.queueBand,  Color.yellow); cum += config.queueBand;
            DrawBand(c, cx, visW, cum, config.bottomBand, Color.magenta);

            Gizmos.color = Color.white;
            float halfCW = visW * config.contentWidthFactor * 0.5f;
            float topY = GameLayout.WorldYAtViewportFraction(c, 0f);
            float botY = GameLayout.WorldYAtViewportFraction(c, 1f);
            Gizmos.DrawLine(new Vector3(cx - halfCW, topY, 0), new Vector3(cx - halfCW, botY, 0));
            Gizmos.DrawLine(new Vector3(cx + halfCW, topY, 0), new Vector3(cx + halfCW, botY, 0));
        }

        private void DrawBand(Camera c, float cx, float visW, float cumBefore, float frac, Color col)
        {
            float topY = GameLayout.WorldYAtViewportFraction(c, cumBefore);
            float botY = GameLayout.WorldYAtViewportFraction(c, cumBefore + frac);
            float centerY = (topY + botY) * 0.5f;
            float height = Mathf.Abs(topY - botY);

            Gizmos.color = col;
            Gizmos.DrawWireCube(new Vector3(cx, centerY, 0f), new Vector3(visW, height, 0.01f));
        }
    }
}