using UnityEngine;

namespace Game
{
    /// <summary>
    /// Direction arrows that drift around the rail, showing which way shooters go.
    ///
    /// Everything here works in world arc length rather than the path's distance
    /// parameter. TrackPath is world-parameterised now too, so the two agree along
    /// the straights — but the corners still are not: an arc spans r of distance
    /// while covering only r·pi/4 of actual length, so arrows spaced on the raw
    /// parameter would bunch up at every corner.
    ///
    /// They travel the whole loop rather than sitting still and blinking. A closed
    /// loop has no seam, so nothing pops when an arrow comes round again.
    ///
    /// TrackPath is not touched — visuals do not get to alter gameplay maths.
    /// </summary>
    public class TrackChevrons : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private TrackController trackController;
        [SerializeField] private GameObject chevronPrefab;

        [Header("Layout")]
        [Tooltip("Distance between arrows, in world units rather than cells.")]
        [SerializeField] private float spacing = 0.45f;
        [Tooltip("Path sampling resolution. Raising it smooths the corners.")]
        [SerializeField] private int sampleCount = 720;
        [SerializeField] private Vector3 offset = Vector3.zero;
        [Tooltip("Arrow size. 0 keeps the prefab's own scale.")]
        [SerializeField] private float chevronScale = 0f;
        [Tooltip("Correction if the model's forward is not +X. Use 180 if the arrows point backwards.")]
        [SerializeField] private float facingOffsetDeg = 0f;

        [Header("Flow")]
        [Tooltip("How fast the arrows travel, in world units per second.")]
        [SerializeField] private float flowSpeed = 0.6f;

        private Transform[] chevrons;
        private float[] baseArc;

        private Vector3[] samples;
        private float[] arc;              // arc[i] = distance from samples[0] to samples[i]
        private float totalArc;
        private float travel;

        /// <summary>
        /// Call after TrackController.Init — the path has to exist first. Also on the
        /// context menu, so spacing and facing can be dialled in while playing.
        /// </summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            Clear();
            if (chevronPrefab == null || trackController == null) return;

            TrackPath path = trackController.Path;
            if (path == null) return;

            BuildArcTable(path);
            if (totalArc <= 0.001f) return;

            int count = Mathf.Max(1, Mathf.RoundToInt(totalArc / Mathf.Max(spacing, 0.05f)));
            chevrons = new Transform[count];
            baseArc = new float[count];

            float step = totalArc / count;   // spacing is a wish; an even loop is the rule
            for (int i = 0; i < count; i++)
            {
                GameObject obj = Instantiate(chevronPrefab, transform);
                obj.name = $"Chevron_{i:00}";
                chevrons[i] = obj.transform;
                baseArc[i] = i * step;

                if (chevronScale > 0f)
                    chevrons[i].localScale = Vector3.one * chevronScale;
            }

            travel = 0f;
            Place();
        }

        public void Clear()
        {
            if (chevrons != null)
                foreach (Transform t in chevrons)
                    if (t != null) Destroy(t.gameObject);

            chevrons = null;
            baseArc = null;
        }

        private void Update()
        {
            if (chevrons == null || totalArc <= 0.001f) return;

            travel = Mathf.Repeat(travel + flowSpeed * Time.deltaTime, totalArc);
            Place();
        }

        private void Place()
        {
            for (int i = 0; i < chevrons.Length; i++)
            {
                if (chevrons[i] == null) continue;

                Sample(baseArc[i] + travel, out Vector3 pos, out Vector3 dir);
                chevrons[i].position = pos + offset;

                if (dir.sqrMagnitude > 0.000001f)
                {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    chevrons[i].rotation = Quaternion.Euler(0f, 0f, angle + facingOffsetDeg);
                }
            }
        }

        // ------------------------------------------------------------- geometry

        private void BuildArcTable(TrackPath path)
        {
            int n = Mathf.Max(sampleCount, 64);
            samples = new Vector3[n];
            for (int i = 0; i < n; i++)
                samples[i] = path.Evaluate(path.Perimeter * i / n).worldPos;

            arc = new float[n + 1];
            for (int i = 1; i <= n; i++)
                arc[i] = arc[i - 1] + Vector3.Distance(samples[i - 1], samples[i % n]);

            totalArc = arc[n];
        }

        /// <summary>Position and heading at a world-space distance around the loop.</summary>
        private void Sample(float at, out Vector3 pos, out Vector3 dir)
        {
            at = Mathf.Repeat(at, totalArc);

            int lo = 0, hi = arc.Length - 1;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (arc[mid] <= at) lo = mid + 1;
                else hi = mid;
            }

            int i = Mathf.Clamp(lo - 1, 0, samples.Length - 1);
            int j = (i + 1) % samples.Length;

            float segment = arc[i + 1] - arc[i];
            float t = segment > 0.0001f ? (at - arc[i]) / segment : 0f;

            pos = Vector3.Lerp(samples[i], samples[j], t);
            dir = (samples[j] - samples[i]).normalized;
        }
    }
}
