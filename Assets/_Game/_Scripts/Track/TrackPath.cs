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
        private readonly float cornerRadius;
        private readonly float startOffset;

        private readonly float leftX, rightX, bottomY, topY;

        public float Perimeter => 2f * (width + height);

        // centerline: rayın merkez hattı dikdörtgeni (dünya uzayı).
        // width/height yalnızca LANE SAYISI için — dünya konumu artık board'a
        // değil bu dikdörtgene bağlı.
        public TrackPath(int width, int height, Rect centerline, float cornerRadius, float startOffset)
        {
            this.width = width;
            this.height = height;
            this.startOffset = startOffset;

            leftX = centerline.xMin;
            rightX = centerline.xMax;
            bottomY = centerline.yMin;
            topY = centerline.yMax;

            float maxRadius = Mathf.Min(rightX - leftX, topY - bottomY) * 0.5f - 0.01f;
            this.cornerRadius = Mathf.Clamp(cornerRadius, 0f, Mathf.Max(0f, maxRadius));
        }

        public TrackSample Evaluate(float distance)
        {
            distance = (distance + startOffset) % Perimeter;
            if (distance < 0f) distance += Perimeter;

            TrackEdge edge;
            float offset;
            int lane;
            if (distance < width)
            {
                edge = TrackEdge.Bottom;
                offset = distance;
                lane = Mathf.Clamp(Mathf.FloorToInt(offset), 0, width - 1);
            }
            else if (distance < width + height)
            {
                edge = TrackEdge.Right;
                offset = distance - width;
                lane = Mathf.Clamp(Mathf.FloorToInt(offset), 0, height - 1);
            }
            else if (distance < 2 * width + height)
            {
                edge = TrackEdge.Top;
                offset = distance - (width + height);
                lane = Mathf.Clamp(width - 1 - Mathf.FloorToInt(offset), 0, width - 1);
            }
            else
            {
                edge = TrackEdge.Left;
                offset = distance - (2 * width + height);
                lane = Mathf.Clamp(height - 1 - Mathf.FloorToInt(offset), 0, height - 1);
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
            float r = cornerRadius;

            switch (edge)
            {
                case TrackEdge.Bottom:
                {
                    float L = rightX - leftX;
                    float d = (offset / width) * L;
                    if (d < r) return ArcPoint(new Vector2(leftX + r, bottomY + r), 225f, 270f, d / r, r);
                    if (d > L - r) return ArcPoint(new Vector2(rightX - r, bottomY + r), 270f, 315f, (d - (L - r)) / r, r);
                    float t = (d - r) / (L - 2f * r);
                    return new Vector3(Mathf.Lerp(leftX + r, rightX - r, t), bottomY, 0f);
                }
                case TrackEdge.Right:
                {
                    float L = topY - bottomY;
                    float d = (offset / height) * L;
                    if (d < r) return ArcPoint(new Vector2(rightX - r, bottomY + r), 315f, 360f, d / r, r);
                    if (d > L - r) return ArcPoint(new Vector2(rightX - r, topY - r), 0f, 45f, (d - (L - r)) / r, r);
                    float t = (d - r) / (L - 2f * r);
                    return new Vector3(rightX, Mathf.Lerp(bottomY + r, topY - r, t), 0f);
                }
                case TrackEdge.Top:
                {
                    float L = rightX - leftX;
                    float d = (offset / width) * L;
                    if (d < r) return ArcPoint(new Vector2(rightX - r, topY - r), 45f, 90f, d / r, r);
                    if (d > L - r) return ArcPoint(new Vector2(leftX + r, topY - r), 90f, 135f, (d - (L - r)) / r, r);
                    float t = (d - r) / (L - 2f * r);
                    return new Vector3(Mathf.Lerp(rightX - r, leftX + r, t), topY, 0f);
                }
                case TrackEdge.Left:
                {
                    float L = topY - bottomY;
                    float d = (offset / height) * L;
                    if (d < r) return ArcPoint(new Vector2(leftX + r, topY - r), 135f, 180f, d / r, r);
                    if (d > L - r) return ArcPoint(new Vector2(leftX + r, bottomY + r), 180f, 225f, (d - (L - r)) / r, r);
                    float t = (d - r) / (L - 2f * r);
                    return new Vector3(leftX, Mathf.Lerp(topY - r, bottomY + r, t), 0f);
                }
                default:
                    throw new System.ArgumentException($"Invalid edge: {edge}");
            }
        }

        private static Vector3 ArcPoint(Vector2 center, float fromDeg, float toDeg, float u, float r)
        {
            float angle = Mathf.Lerp(fromDeg, toDeg, u) * Mathf.Deg2Rad;
            return new Vector3(center.x + r * Mathf.Cos(angle), center.y + r * Mathf.Sin(angle), 0f);
        }
    }
}