using UnityEngine;
using MobileCore;
namespace Game
{
    public class TrackController : MonoBehaviour
    {
        [SerializeField] private BoardController boardController;

        private float baseSpeed;                 // cells per second
        private float tensionMultiplier = 1f;
        private float rampSeconds = 0.35f;
        private float speedMultiplier = 1f;      // current, eased toward the target
        private float targetMultiplier = 1f;

        private TrackPath path;
        private BoundedBuffer<Shooter> shooters;
        public event System.Action<Shooter> OnShooterFinishedLap;

        public void Init(int boardWidth, int boardHeight, Rect centerline, int trackCapacity,
                          float lapSeconds, float cornerRadius, float startOffset,
                          float tensionMultiplier, float rampSeconds)
        {
            path = new TrackPath(boardWidth, boardHeight, centerline, cornerRadius, startOffset);

            // Distance is measured in cells but the rail is a fixed rectangle in
            // world space, so a bigger board means more cells over the same
            // physical loop. Deriving the speed from the lap duration cancels
            // that out and every level runs at the same visual pace.
            baseSpeed = path.Perimeter / Mathf.Max(lapSeconds, 0.01f);

            this.tensionMultiplier = Mathf.Max(tensionMultiplier, 1f);
            this.rampSeconds = Mathf.Max(rampSeconds, 0.01f);
            speedMultiplier = targetMultiplier = 1f;

            shooters = new BoundedBuffer<Shooter>(trackCapacity);
            shooters.OnChanged += () => GameEvents.TriggerTrackOccupancyChanged(shooters.Count, shooters.Capacity);
        }

        public int Count => shooters != null ? shooters.Count : 0;

        /// <summary>
        /// Raised by GameManager, which is the only place that judges combined
        /// rail + park pressure. Deliberately not random: the design leans on the
        /// player being able to predict when a shooter lands (GDD 1.4), so the
        /// speed-up has to be something they can see coming.
        /// </summary>
        public void SetUnderPressure(bool pressured)
        {
            targetMultiplier = pressured ? tensionMultiplier : 1f;
        }
        public void Clear()
        {
            if (shooters == null) return;
            foreach (Shooter s in shooters)
            {
                ObjectPooler.Instance.ReturnToPool("Shooter", s.gameObject);
            }
        }

        public bool HasFreeTrackSlot => shooters != null && shooters.HasFreeSlot;
        public TrackPath Path => path;

        private void Update()
        {
            if (path == null || shooters == null) return;

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

                // Atıcı ATEŞ EDECEĞİ yöne bakar (hareket yönüne değil) = teğetin 90° solu.
                // Köşelerde teğet yay boyunca yumuşak döndüğü için dönüş de smooth.
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
            // ObjectPooler'a DÖNMÜYOR — hâlâ sahnede, ParkController'a taşındı
        }

        // Ray yolunu (atıcıların gerçekten dolaştığı perimetre) çizer — placeholder.
        // Gerçek rail görseli Faz 3'te gelene kadar test için dikdörtgeni gösterir.
        private void OnDrawGizmos()
        {
            if (path == null) return;

            Gizmos.color = new Color(0.56f, 0.53f, 0.78f);   // açık lavanta (ray rengi)
            const int steps = 160;
            Vector3 prev = path.Evaluate(0f).worldPos;
            for (int i = 1; i <= steps; i++)
            {
                float d = path.Perimeter * i / steps;
                Vector3 cur = path.Evaluate(d).worldPos;
                Gizmos.DrawLine(prev, cur);
                prev = cur;
            }
        }

        [ContextMenu("Spawn Test Shooter")]
        private void SpawnTestShooter()
        {
            if (!shooters.HasFreeSlot) return;

            GameObject obj = ObjectPooler.Instance.SpawnFromPool("Shooter", Vector3.zero, Quaternion.identity);
            if (obj == null) return;

            Shooter s = obj.GetComponent<Shooter>();
            s.Init(ColorId.Red, 3, false);
            TryAddShooter(s);
        }
    }
}