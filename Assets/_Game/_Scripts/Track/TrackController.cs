using UnityEngine;
using MobileCore;
namespace Game
{
    public class TrackController : MonoBehaviour
    {
        [SerializeField] private BoardController boardController;
        [SerializeField] private float trackSpeed = 3f;

        private TrackPath path;
        private BoundedBuffer<Shooter> shooters;
        public event System.Action<Shooter> OnShooterFinishedLap;

        public void Init(int boardWidth, int boardHeight, float cellSize, Vector3 origin, float trackMargin, int trackCapacity)
        {
            path = new TrackPath(boardWidth, boardHeight, cellSize, origin, trackMargin);
            shooters = new BoundedBuffer<Shooter>(trackCapacity);
            shooters.OnChanged += () => GameEvents.TriggerTrackOccupancyChanged(shooters.Count, shooters.Capacity);
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

        private void Update()
        {
            if (path == null || shooters == null) return;

            for (int i = shooters.Count - 1; i >= 0; i--)
            {
                Shooter s = shooters[i];
                s.Distance += trackSpeed * Time.deltaTime;

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

                if (s.HasFiredAt(sample.edge, sample.lane))
                    continue;

                Direction dir = TrackPath.FireDirectionOf(sample.edge);
                bool hit = boardController.TryBreakCube(sample.lane, dir, s.Color);

                s.MarkFired(sample.edge, sample.lane);

                if (hit)
                {
                    s.ConsumeAmmo();
                    if (s.IsSpent)
                        RemoveShooter(s);
                }
            }
        }

        private void OnLapCompleted(Shooter s)
        {
            s.IsWaitingForPark = true;
            OnShooterFinishedLap?.Invoke(s);
        }

        public bool TryAddShooter(Shooter shooter) => shooters.TryAdd(shooter);

        private void RemoveShooter(Shooter s)
        {
            shooters.TryRemove(s);
            ObjectPooler.Instance.ReturnToPool("Shooter", s.gameObject);
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
            if (!shooters.HasFreeSlot) { Debug.Log("Track full"); return; }

            GameObject obj = ObjectPooler.Instance.SpawnFromPool("Shooter", Vector3.zero, Quaternion.identity);
            if (obj == null) return;

            Shooter s = obj.GetComponent<Shooter>();
            s.Init(ColorId.Red, 3, false);
            TryAddShooter(s);
        }
    }
}