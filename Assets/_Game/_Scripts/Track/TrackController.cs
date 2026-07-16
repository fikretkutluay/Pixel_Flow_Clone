using System.Collections.Generic;
using UnityEngine;
using MobileCore;
namespace Game
{
    public class TrackController : MonoBehaviour
    {
        [SerializeField] private BoardController boardController;
        [SerializeField] private float trackSpeed = 3f;

        private TrackPath path;
        private readonly List<Shooter> shooters = new List<Shooter>();

        public void Init(int boardWidth, int boardHeight, float cellSize, Vector3 origin)
        {
            path = new TrackPath(boardWidth, boardHeight, cellSize, origin);
        }

        public bool TryAddShooter(Shooter shooter)
        {
            shooters.Add(shooter);
            return true;
        }

        private void Update()
        {
            if (path == null) return;

            for (int i = shooters.Count - 1; i >= 0; i--)
            {
                Shooter s = shooters[i];
                s.Distance += trackSpeed * Time.deltaTime;

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
            Debug.Log($"Lap done. Ammo left: {s.Ammo}");
            RemoveShooter(s);
        }

        private void RemoveShooter(Shooter s)
        {
            shooters.Remove(s);
            ObjectPooler.Instance.ReturnToPool("Shooter", s.gameObject);
        }

        [ContextMenu("Spawn Test Shooter")]
        private void SpawnTestShooter()
        {
            GameObject obj = ObjectPooler.Instance.SpawnFromPool("Shooter", Vector3.zero, Quaternion.identity);
            if (obj == null) return;
            Shooter s = obj.GetComponent<Shooter>();
            s.Init(ColorId.Red, 3);
            TryAddShooter(s);
        }

        private void Start()
        {
            Init(4, 4, 1f, Vector3.zero);
        }
    }
}