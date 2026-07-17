using UnityEngine;
using MobileCore;

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
            return true;
        }
        [ContextMenu("Build Test Board")]
        private void BuildTestBoard()
        {
            board = new GridManager<CubeCell>(4, 4, 1f, Vector3.zero);

            board.SetValue(0, 0, CubeCell.Create(ColorId.Red, false));
            board.SetValue(1, 0, CubeCell.Create(ColorId.Red, false));
            board.SetValue(2, 0, CubeCell.Create(ColorId.Blue, false));
            board.SetValue(3, 0, CubeCell.Create(ColorId.None, true));   // crate

            remainingCubes = 2;

            Debug.Log("Test board built. Remaining cubes: " + remainingCubes);
        }
    }


}
