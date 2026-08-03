using UnityEngine;

namespace Game
{
    /// <summary>
    /// Defines the rail's centreline, the line the shooters travel along. The path
    /// now derives from here rather than from the board: the rail is a fixed object
    /// in the scene, so alignment stays independent of board size. The gizmo draws
    /// the centreline so it can be lined up against the mesh by eye.
    /// </summary>
    public class TrackRailAnchor : MonoBehaviour
    {
        [Tooltip("Size of the centreline rectangle, in world units.")]
        [SerializeField] private Vector2 centerlineSize = new Vector2(7.2f, 8.8f);

        [Tooltip("Corner rounding radius. Match it to the mesh's visual corner arc.")]
        [SerializeField] private float cornerRadius = 1.2f;

        [Tooltip("Phase shift of the path's zero point.")]
        [SerializeField] private float startOffset = 0f;

        [Header("Board area")]
        [Tooltip("Every level's board is fitted inside this rectangle. Drawn as a " +
                 "green frame in the scene view — drag it to set the clearance " +
                 "against the rail; the board never spills outside it.")]
        [SerializeField] private Vector2 boardAreaSize = new Vector2(6.5f, 8.1f);

        public float CornerRadius => cornerRadius;
        public float StartOffset => startOffset;

        public Rect GetCenterlineRect() => RectAround(centerlineSize);

        /// <summary>
        /// The area the board is fitted into. It is a separate rectangle because
        /// insetting the centreline by a fixed amount was not enough: the rail's
        /// corner arc and mesh thickness mean the clearance differs on all four
        /// sides. Being visible in the scene, it is dragged into place rather than
        /// guessed at as a number.
        /// </summary>
        public Rect GetBoardAreaRect() => RectAround(boardAreaSize);

        private Rect RectAround(Vector2 size)
        {
            Vector3 c = transform.position;
            return new Rect(c.x - size.x * 0.5f, c.y - size.y * 0.5f, size.x, size.y);
        }

        private void OnDrawGizmos()
        {
            // The shape does not depend on width/height — those only set the lane
            // count — so a fixed value is enough for the gizmo.
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

            // Board area: every level stays inside this frame.
            Rect area = GetBoardAreaRect();
            Gizmos.color = new Color(0.35f, 1f, 0.45f);
            Gizmos.DrawWireCube(new Vector3(area.center.x, area.center.y, 0f),
                                new Vector3(area.width, area.height, 0f));
        }
    }
}