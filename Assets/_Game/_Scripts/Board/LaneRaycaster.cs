using UnityEngine;
using MobileCore;

namespace Game
{
    public static class LaneRaycaster
    {
        public static bool TryBreak(GridManager<CubeCell> board, int laneIndex,
                                    Direction dir, ColorId shooterColor)
        {
            Vector2Int pos = GetLaneStart(board, laneIndex, dir);
            Vector2Int step = GetStep(dir);

            while (board.IsInBounds(pos.x, pos.y))
            {
                CubeCell cell = board.GetValue(pos.x, pos.y);

                if (cell.isCrate)
                    return false;

                if (cell.color == ColorId.None)
                {
                    pos += step;
                    continue;
                }

                if (cell.color == shooterColor)
                {
                    board.SetValue(pos.x, pos.y, CubeCell.Create(ColorId.None, false));
                    return true;
                }

                return false;
            }

            return false;
        }

        private static Vector2Int GetLaneStart(GridManager<CubeCell> board, int laneIndex, Direction dir)
        {
            switch (dir)
            {
                case Direction.Up:    return new Vector2Int(laneIndex, 0);
                case Direction.Down:  return new Vector2Int(laneIndex, board.Height - 1);
                case Direction.Right: return new Vector2Int(0, laneIndex);
                case Direction.Left:  return new Vector2Int(board.Width - 1, laneIndex);
                default: throw new System.ArgumentException("Invalid direction");
            }
        }

        private static Vector2Int GetStep(Direction dir)
        {
            switch (dir)
            {
                case Direction.Up:    return new Vector2Int(0, 1);
                case Direction.Down:  return new Vector2Int(0, -1);
                case Direction.Right: return new Vector2Int(1, 0);
                case Direction.Left:  return new Vector2Int(-1, 0);
                default: throw new System.ArgumentException("Invalid direction");
            }
        }
    }
}