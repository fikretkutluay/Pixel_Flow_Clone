using UnityEngine;
using MobileCore;
using System.Linq;

namespace Game
{
    public class BoardController : MonoBehaviour
    {
        public const string CubePoolTag = "Cube";
        public const string CratePoolTag = "Crate";

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
            Vector3 pos = board.GetWorldPosition(x, y);

            // Crates carry their own model, so they have their own pool. Falling back
            // to the cube pool keeps levels playable before that model exists.
            string tag = color == ColorId.Crate ? CratePoolTag : CubePoolTag;
            GameObject obj = ObjectPooler.Instance.SpawnFromPool(tag, pos, Quaternion.identity);
            if (obj == null && tag != CubePoolTag)
            {
                tag = CubePoolTag;
                obj = ObjectPooler.Instance.SpawnFromPool(tag, pos, Quaternion.identity);
            }
            if (obj == null) return;

            CubeView view = obj.GetComponent<CubeView>();
            if (view == null)
            {
                Debug.LogError($"[{name}] '{tag}' havuzundaki prefab'da CubeView yok.");
                ObjectPooler.Instance.ReturnToPool(tag, obj);
                return;
            }
            view.PoolTag = tag;

            // Hücre kare, küp değil: dikey boşluğu yataydan küçük tutmak küpü
            // eninden uzun gösteriyor — referanstaki boncuk oranı bu.
            float gapX = config != null ? config.cubeGap : 0f;
            float gapY = config != null ? config.cubeGapVertical : gapX;
            float width = cellSize * (1f - gapX);
            float height = cellSize * (1f - gapY);

            // Prefab'ın kendi oranını EZMEDEN hücreye sığdır: 1:1:2 yazan bir prefab
            // her board boyutunda o oranı korur.
            Vector3 scale = Vector3.Scale(view.BaseScale, new Vector3(width, height, width));
            obj.transform.localScale = scale;

            // A piece deeper than it is wide would otherwise sit half-sunk in the
            // board plane. Pulling it toward the camera by the extra depth leaves the
            // back face where a plain cube's would be and stands the rest proud.
            float extraDepth = Mathf.Max(scale.z - scale.x, 0f);
            obj.transform.position = pos + Vector3.back * (extraDepth * 0.5f);

            view.SetColor(color);
            cubeViews[x, y] = view;
        }

        /// <summary>
        /// Lifts every crate off the board. Called once the level can no longer be
        /// lost: a crate exists to block a lane, and blocking stops meaning anything
        /// at that point. Clears the cells too, so lanes really do open up.
        /// </summary>
        public void ClearCrates()
        {
            if (board == null || cubeViews == null) return;

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if (!board.GetValue(x, y).isCrate) continue;

                    board.SetValue(x, y, CubeCell.Create(ColorId.None, false));

                    CubeView view = cubeViews[x, y];
                    if (view == null) continue;

                    view.PlayLiftAway();
                    cubeViews[x, y] = null;
                }
            }
        }

        public void Clear()
        {
            if (cubeViews == null) return;
            foreach (CubeView view in cubeViews)
            {
                if (view != null)
                    ObjectPooler.Instance.ReturnToPool(view.PoolTag, view.gameObject);
            }
        }
    }
}