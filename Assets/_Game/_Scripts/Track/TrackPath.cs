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
        private readonly float edgeX, edgeY;   // kenarların DÜNYA uzunlukları

        /// <summary>
        /// Turun dünya birimindeki uzunluğu. Eskiden lane sayısıydı
        /// (2*(width+height)) ve bu, board kareden uzaklaşınca atıcının hızını
        /// kenara göre değiştiriyordu: 39x27 bir board'da alt kenarın 39 lane'i
        /// 7.2 birime, sağ kenarın 27 lane'i 8.8 birime yayılıyor, yani sabit
        /// lane hızı sağ/solda 1.77 kat daha hızlı görünüyordu.
        /// </summary>
        public float Perimeter => 2f * (edgeX + edgeY);

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

            edgeX = rightX - leftX;
            edgeY = topY - bottomY;

            float maxRadius = Mathf.Min(rightX - leftX, topY - bottomY) * 0.5f - 0.01f;
            this.cornerRadius = Mathf.Clamp(cornerRadius, 0f, Mathf.Max(0f, maxRadius));
        }

        public TrackSample Evaluate(float distance)
        {
            distance = (distance + startOffset) % Perimeter;
            if (distance < 0f) distance += Perimeter;

            // offset artık DÜNYA birimi, lane değil.
            TrackEdge edge;
            float offset;
            int lane;
            if (distance < edgeX)
            {
                edge = TrackEdge.Bottom;
                offset = distance;
                lane = LaneOf(offset, edgeX, width, false);
            }
            else if (distance < edgeX + edgeY)
            {
                edge = TrackEdge.Right;
                offset = distance - edgeX;
                lane = LaneOf(offset, edgeY, height, false);
            }
            else if (distance < 2f * edgeX + edgeY)
            {
                edge = TrackEdge.Top;
                offset = distance - (edgeX + edgeY);
                lane = LaneOf(offset, edgeX, width, true);
            }
            else
            {
                edge = TrackEdge.Left;
                offset = distance - (2f * edgeX + edgeY);
                lane = LaneOf(offset, edgeY, height, true);
            }

            Vector3 worldPos = WorldPosOf(edge, offset);
            return new TrackSample { edge = edge, lane = lane, worldPos = worldPos };
        }

        /// <summary>Kenar üzerindeki dünya konumunu lane indeksine çevirir.</summary>
        private static int LaneOf(float offset, float edgeLength, int laneCount, bool reversed)
        {
            int i = Mathf.Clamp(Mathf.FloorToInt(offset / edgeLength * laneCount),
                                0, laneCount - 1);
            return reversed ? laneCount - 1 - i : i;
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
                    float L = edgeX;
                    float d = offset;
                    if (d < r) return ArcPoint(new Vector2(leftX + r, bottomY + r), 225f, 270f, d / r, r);
                    if (d > L - r) return ArcPoint(new Vector2(rightX - r, bottomY + r), 270f, 315f, (d - (L - r)) / r, r);
                    float t = (d - r) / (L - 2f * r);
                    return new Vector3(Mathf.Lerp(leftX + r, rightX - r, t), bottomY, 0f);
                }
                case TrackEdge.Right:
                {
                    float L = edgeY;
                    float d = offset;
                    if (d < r) return ArcPoint(new Vector2(rightX - r, bottomY + r), 315f, 360f, d / r, r);
                    if (d > L - r) return ArcPoint(new Vector2(rightX - r, topY - r), 0f, 45f, (d - (L - r)) / r, r);
                    float t = (d - r) / (L - 2f * r);
                    return new Vector3(rightX, Mathf.Lerp(bottomY + r, topY - r, t), 0f);
                }
                case TrackEdge.Top:
                {
                    float L = edgeX;
                    float d = offset;
                    if (d < r) return ArcPoint(new Vector2(rightX - r, topY - r), 45f, 90f, d / r, r);
                    if (d > L - r) return ArcPoint(new Vector2(leftX + r, topY - r), 90f, 135f, (d - (L - r)) / r, r);
                    float t = (d - r) / (L - 2f * r);
                    return new Vector3(Mathf.Lerp(rightX - r, leftX + r, t), topY, 0f);
                }
                case TrackEdge.Left:
                {
                    float L = edgeY;
                    float d = offset;
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