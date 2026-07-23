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

        public bool TryBreakCube(int laneIndex, Direction dir, ColorId shooterColor)
        {
            if (board == null) return false;
            if (!LaneRaycaster.TryBreak(board, laneIndex, dir, shooterColor))
                return false;

            Debug.Log("Hit! Lane: " + laneIndex + ", Direction: " + dir + ", Shooter Color: " + shooterColor);
            remainingCubes--;
            GameEvents.TriggerRemainingCubesChanged(remainingCubes);
            return true;
        }

        public void Setup(LevelData data, float cellSize)
        {
            board = new GridManager<CubeCell>(data.boardSize.x, data.boardSize.y, cellSize, Vector3.zero);
            for (int y = 0; y < data.boardSize.y; y++)
            {
                for(int x = 0; x < data.boardSize.x; x++)
                {
                    int index = y * data.boardSize.x + x;
                    ColorId pixel = data.boardPixels[index];

                    if (pixel == ColorId.Crate)
                    {
                        board.SetValue(x,y, CubeCell.Create(ColorId.None, true));
                    }
                    else if(pixel == ColorId.None)
                    {
                        
                    }
                    else
                    {
                        board.SetValue(x,y, CubeCell.Create(pixel, false));
                    }
                }
            }
            remainingCubes = data.boardPixels.Count(p => p != ColorId.None && p != ColorId.Crate);

        }
    }


}
