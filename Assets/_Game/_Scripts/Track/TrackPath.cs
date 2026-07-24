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
        private readonly float margin;   // board kenarı ile ray arası sabit dünya mesafesi

        public float Perimeter => 2f * (width + height);

        public TrackPath(int width, int height, float cellSize, Vector3 origin, float margin)
        {
            this.width = width;
            this.height = height;
            this.cellSize = cellSize;
            this.origin = origin;
            this.margin = margin;
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

        // Kenar-boyu koordinat cellSize ile ölçeklenir (lane hizası korunur);
        // dikey/yatay marj ise SABİT dünya mesafesidir (cellSize'dan bağımsız).
        // Böylece ray footprint'i = boardPhysicalSize + 2*margin → her board boyutunda sabit.
        private Vector3 WorldPosOf(TrackEdge edge, float offset)
        {
            // Board'un küp-kenar sınırları (hücre merkezleri 0..width-1, küpler ±0.5 hücre taşar):
            float halfCell = 0.5f * cellSize;
            float leftX   = origin.x - halfCell - margin;
            float rightX  = origin.x + (width - 1) * cellSize + halfCell + margin;
            float bottomY = origin.y - halfCell - margin;
            float topY    = origin.y + (height - 1) * cellSize + halfCell + margin;

            switch (edge)
            {
                case TrackEdge.Bottom:
                    return new Vector3(origin.x + offset * cellSize, bottomY, 0f);
                case TrackEdge.Right:
                    return new Vector3(rightX, origin.y + offset * cellSize, 0f);
                case TrackEdge.Top:
                    return new Vector3(origin.x + (width - 1 - offset) * cellSize, topY, 0f);
                case TrackEdge.Left:
                    return new Vector3(leftX, origin.y + (height - 1 - offset) * cellSize, 0f);
                default:
                    throw new System.ArgumentException($"Invalid edge: {edge}");
            }
        }
    }
}