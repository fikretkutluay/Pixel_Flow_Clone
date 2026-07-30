using UnityEngine;
using MobileCore;
using System.Linq;

namespace Game
{
    public class BoardController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        private GridManager<CubeCell> board;
        private int remainingCubes;
        private float cellSize;   // Setup'ta saklanır, SpawnCubeView kullanır

        public int RemainingCubes => remainingCubes;
        private CubeView[,] cubeViews;

        /// <summary>
        /// Resolves a shot. The cell is cleared and the count drops immediately —
        /// the win check must not wait on presentation — while the tracer flies and
        /// the cube's break is held back to meet it.
        /// </summary>
        public bool TryBreakCube(int laneIndex, Direction dir, ColorId shooterColor,
                                 Vector3 muzzle, Color tracerColor)
        {
            if (board == null) return false;
            if (!LaneRaycaster.TryBreak(board, laneIndex, dir, shooterColor, out Vector2Int brokenPos))
                return false;
            remainingCubes--;
            GameEvents.TriggerRemainingCubesChanged(remainingCubes);

            Vector3 hitPosition = board.GetWorldPosition(brokenPos.x, brokenPos.y);
            float flightTime = FireTracer(muzzle, hitPosition, tracerColor);

            CubeView view = cubeViews[brokenPos.x, brokenPos.y];
            if (view != null)
            {
                view.PlayBreakAndReturn(flightTime);
                cubeViews[brokenPos.x, brokenPos.y] = null;
            }

            return true;
        }

        /// <summary>Returns the flight time, or zero when there is no tracer pool.</summary>
        private float FireTracer(Vector3 from, Vector3 to, Color color)
        {
            GameObject obj = ObjectPooler.Instance.SpawnFromPool(Tracer.PoolTag, from, Quaternion.identity);
            if (obj == null) return 0f;

            Tracer tracer = obj.GetComponent<Tracer>();
            if (tracer == null)
            {
                ObjectPooler.Instance.ReturnToPool(Tracer.PoolTag, obj);
                return 0f;
            }

            tracer.Fire(from, to, color);
            return tracer.DurationFor(from, to);
        }

        public void Setup(LevelData data, float cellSize, Vector3 origin)
        {
            this.cellSize = cellSize;
            board = new GridManager<CubeCell>(data.boardSize.x, data.boardSize.y, cellSize, origin);
            cubeViews = new CubeView[data.boardSize.x, data.boardSize.y];

            for (int y = 0; y < data.boardSize.y; y++)
            {
                for (int x = 0; x < data.boardSize.x; x++)
                {
                    int index = y * data.boardSize.x + x;
                    ColorId pixel = data.boardPixels[index];

                    if (pixel == ColorId.Crate)
                    {
                        board.SetValue(x, y, CubeCell.Create(ColorId.None, true));
                        SpawnCubeView(x, y, ColorId.Crate);
                    }
                    else if (pixel == ColorId.None)
                    {
                        // boş, spawn yok
                    }
                    else
                    {
                        board.SetValue(x, y, CubeCell.Create(pixel, false));
                        SpawnCubeView(x, y, pixel);
                    }
                }
            }
            remainingCubes = data.boardPixels.Count(p => p != ColorId.None && p != ColorId.Crate);
        }

        private void SpawnCubeView(int x, int y, ColorId color)
        {
            GameObject obj = ObjectPooler.Instance.SpawnFromPool("Cube", board.GetWorldPosition(x, y), Quaternion.identity);
            if (obj == null) return;

            // Küpü hücre boyutuna göre ölçekle (cubeGap kadar küçültülmüş → ızgara boşluğu).
            // Prefab'ın orijinal en-boy oranı 1:1:1 varsayılıyor; z'yi de eşitliyoruz.
            float gap = config != null ? config.cubeGap : 0f;
            float scale = cellSize * (1f - gap);
            obj.transform.localScale = new Vector3(scale, scale, scale);

            CubeView view = obj.GetComponent<CubeView>();
            view.SetColor(color);
            cubeViews[x, y] = view;
        }

        public void Clear()
        {
            if (cubeViews == null) return;
            foreach (CubeView view in cubeViews)
            {
                if (view != null)
                    ObjectPooler.Instance.ReturnToPool("Cube", view.gameObject);
            }
        }
    }
}