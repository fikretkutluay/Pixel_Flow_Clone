using UnityEngine;
using MobileCore;
namespace Game
{
    public class TrackController : MonoBehaviour
    {
        [SerializeField] private BoardController boardController;

        private float baseSpeed;                 // cells per second
        private float rampSeconds = 0.35f;
        private float speedMultiplier = 1f;      // current, eased toward the target
        private float targetMultiplier = 1f;

        private TrackPath path;
        private BoundedBuffer<Shooter> shooters;
        private bool running;
        public event System.Action<Shooter> OnShooterFinishedLap;

        public void Init(int boardWidth, int boardHeight, Rect centerline, int trackCapacity,
                          float lapSeconds, float cornerRadius, float startOffset,
                          float rampSeconds)
        {
            path = new TrackPath(boardWidth, boardHeight, centerline, cornerRadius, startOffset);

            // Distance is measured in cells but the rail is a fixed rectangle in
            // world space, so a bigger board means more cells over the same
            // physical loop. Deriving the speed from the lap duration cancels
            // that out and every level runs at the same visual pace.
            baseSpeed = path.Perimeter / Mathf.Max(lapSeconds, 0.01f);

            this.rampSeconds = Mathf.Max(rampSeconds, 0.01f);
            speedMultiplier = targetMultiplier = 1f;

            running = true;
            shooters = new BoundedBuffer<Shooter>(trackCapacity);
            shooters.OnChanged += () => GameEvents.TriggerTrackOccupancyChanged(shooters.Count, shooters.Capacity);
        }

        public int Count => shooters != null ? shooters.Count : 0;

        /// <summary>
        /// Set by GameManager, the only place that weighs the whole board. Eased in
        /// rather than snapped. Deliberately never random: the design leans on the
        /// player predicting when a shooter lands (GDD 1.4), so a change of pace has
        /// to be something they can see coming.
        /// </summary>
        public void SetSpeedScale(float scale)
        {
            targetMultiplier = Mathf.Max(scale, 0.1f);
        }

        /// <summary>
        /// Is anyone in the last stretch before landing? The rail runs
        /// Bottom - Right - Top - Left, so this is the left-hand run home, and it is
        /// the window the player has to clear a park slot.
        /// </summary>
        public bool HasShooterApproachingLapEnd(float fraction)
        {
            if (path == null || shooters == null) return false;

            float threshold = path.Perimeter * (1f - Mathf.Clamp01(fraction));
            for (int i = 0; i < shooters.Count; i++)
            {
                Shooter s = shooters[i];
                if (!s.IsWaitingForPark && s.Distance >= threshold) return true;
            }
            return false;
        }
        /// <summary>
        /// The rail may only turn while a level is actually being played. Left
        /// running past the win/lose decision, shooters kept completing laps and
        /// falling into a full park, raising the failure event a second time. The
        /// duplicate event stacked a self-closing Hide on top of the panel in
        /// UIManager, so the lose screen never became visible.
        /// </summary>
        public void SetRunning(bool value) => running = value;

        public void Clear()
        {
            running = false;
            if (shooters == null) return;

            foreach (Shooter s in shooters)
            {
                ObjectPooler.Instance.ReturnToPool("Shooter", s.gameObject);
            }
            // Empty the buffer as well. Init builds a fresh one, but any call in
            // between must not treat pooled-away shooters as still live.
            shooters.Clear();
        }

        public bool HasFreeTrackSlot => shooters != null && shooters.HasFreeSlot;
        public TrackPath Path => path;

        private void Update()
        {
            if (!running || path == null || shooters == null) return;

            speedMultiplier = Mathf.MoveTowards(speedMultiplier, targetMultiplier,
                                                Time.deltaTime / rampSeconds);
            float speed = baseSpeed * speedMultiplier;

            for (int i = shooters.Count - 1; i >= 0; i--)
            {
                Shooter s = shooters[i];
                s.Distance += speed * Time.deltaTime;

                if (s.IsWaitingForPark)
                {
                    continue;
                }

                if (s.Distance >= path.Perimeter)
                {
                    OnLapCompleted(s);
                    continue;
                }

                TrackSample sample = path.Evaluate(s.Distance);
                s.transform.position = sample.worldPos;

                // A shooter faces where it will fire, not where it is moving:
                // 90 degrees left of the tangent. The tangent turns smoothly along
                // the corner arcs, so the rotation follows without snapping.
                Vector3 ahead = path.Evaluate(s.Distance + 0.1f).worldPos;
                Vector3 forward = ahead - sample.worldPos;
                if (forward.sqrMagnitude > 0.0001f)
                {
                    float tangentAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
                    s.SetFacing(-tangentAngle);
                }

                if (s.HasFiredAt(sample.edge, sample.lane))
                    continue;

                Direction dir = TrackPath.FireDirectionOf(sample.edge);
                bool hit = boardController.TryBreakCube(sample.lane, dir, s.Color,
                                                        s.MuzzlePosition, s.DisplayColor);

                s.MarkFired(sample.edge, sample.lane);

                if (hit)
                {
                    s.ConsumeAmmo();
                    if (s.IsSpent)
                        RetireShooter(s);
                }
            }
        }

        private void OnLapCompleted(Shooter s)
        {
            s.IsWaitingForPark = true;
            OnShooterFinishedLap?.Invoke(s);
        }

        public bool TryAddShooter(Shooter shooter) => shooters.TryAdd(shooter);

        /// <summary>
        /// Out of ammo. It leaves the buffer at once — the rail must stop driving it
        /// and the freed slot must count immediately — then spins out and only then
        /// goes back to the pool.
        /// </summary>
        private void RetireShooter(Shooter s)
        {
            shooters.TryRemove(s);

            if (s.Animator == null)
            {
                ObjectPooler.Instance.ReturnToPool("Shooter", s.gameObject);
                return;
            }

            s.Animator.PlaySpentExit(() =>
                ObjectPooler.Instance.ReturnToPool("Shooter", s.gameObject));
        }

        public void ReleaseShooter(Shooter s)
        {
            shooters.TryRemove(s);
            // Deliberately NOT returned to the pool: the object stays in the
            // scene, ownership just moves to ParkController.
        }
    }
}