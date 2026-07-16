using UnityEngine;
namespace Game
{
    public enum TrackEdge { Bottom, Right, Top, Left }

    public struct TrackSample
    {
        public TrackEdge edge;
        public int lane;
        public Vector3 worldPos;
    }

    public class TrackPath
    {
        private readonly int width;
        private readonly int height;
        private readonly float cellSize;
        private readonly Vector3 origin;

        public float Perimeter => 2f * (width + height);

        public TrackPath(int width, int height, float cellSize, Vector3 origin)
        {
            this.width = width;
            this.height = height;
            this.cellSize = cellSize;
            this.origin = origin;
        }

        public TrackSample Evaluate(float distance)
        {
            distance = distance % Perimeter;
            TrackEdge edge;
            float offset;
            int lane;
            if (distance < width)
            {
                edge = TrackEdge.Bottom;
                offset = distance;
                lane = Mathf.FloorToInt(offset);
                lane = Mathf.Clamp(lane, 0, width - 1);
            }
            else if (distance < width + height)
            {
                edge = TrackEdge.Right;
                offset = distance - width;
                lane = Mathf.FloorToInt(offset);
                lane = Mathf.Clamp(lane, 0, height - 1);
            }
            else if (distance < 2 * width + height)
            {
                edge = TrackEdge.Top;
                offset = distance - (width + height);
                lane = width - 1 - Mathf.FloorToInt(offset);
                lane = Mathf.Clamp(lane, 0, width - 1);
            }
            else
            {
                edge = TrackEdge.Left;
                offset = distance - (2 * width + height);
                lane = height - 1 - Mathf.FloorToInt(offset); 
                lane = Mathf.Clamp(lane, 0, height - 1);
            }

            Vector3 worldPos = WorldPosOf(edge, offset);
            return new TrackSample { edge = edge, lane = lane, worldPos = worldPos };

        }

        public static Direction FireDirectionOf(TrackEdge edge) => edge switch
        {
            TrackEdge.Bottom => Direction.Up,
            TrackEdge.Right => Direction.Left,
            TrackEdge.Top => Direction.Down,
            TrackEdge.Left => Direction.Right,
            _ => throw new System.ArgumentException($"Invalid edge: {edge}")
        };

        private Vector3 WorldPosOf(TrackEdge edge, float offset)
        {
            float x, y;

            switch (edge)
            {
                case TrackEdge.Bottom:
                    x = offset;
                    y = -1f;
                    break;
                case TrackEdge.Right:
                    x = width;
                    y = offset;
                    break;
                case TrackEdge.Top:
                    x = width - 1 - offset;
                    y = height;
                    break;
                case TrackEdge.Left:
                    x = -1f;
                    y = height - 1 - offset;
                    break;
                default:
                    throw new System.ArgumentException($"Invalid edge: {edge}");
            }

            return origin + new Vector3(x, y, 0f) * cellSize;
        }
    }
}