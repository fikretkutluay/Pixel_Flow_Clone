using UnityEngine;
using MobileCore;
using System.Linq;

namespace Game
{
    public class BoardController : MonoBehaviour
    {
        private GridManager<CubeCell> board;
        private int remainingCubes;

        public int RemainingCubes => remainingCubes;
        private CubeView[,] cubeViews;
        public bool TryBreakCube(int laneIndex, Direction dir, ColorId shooterColor)
        {
            if (board == null) return false;
            if (!LaneRaycaster.TryBreak(board, laneIndex, dir, shooterColor, out Vector2Int brokenPos))
                return false;

            Debug.Log("Hit! Lane: " + laneIndex + ", Direction: " + dir + ", Shooter Color: " + shooterColor);
            remainingCubes--;
            GameEvents.TriggerRemainingCubesChanged(remainingCubes);

            CubeView view = cubeViews[brokenPos.x, brokenPos.y];
            if (view != null)
            {
                view.PlayBreakAndReturn();
                cubeViews[brokenPos.x, brokenPos.y] = null;
            }

            return true;
        }

        public void Setup(LevelData data, float cellSize)
        {
            board = new GridManager<CubeCell>(data.boardSize.x, data.boardSize.y, cellSize, Vector3.zero);
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
