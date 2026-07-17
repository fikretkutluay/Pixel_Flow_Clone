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
            if (!LaneRaycaster.TryBreak(board, laneIndex, dir, shooterColor))
                return false;

            Debug.Log("Hit! Lane: " + laneIndex + ", Direction: " + dir + ", Shooter Color: " + shooterColor);
            remainingCubes--;
            return true;
        }
    }


}
