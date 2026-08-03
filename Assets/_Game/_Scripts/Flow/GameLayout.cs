using UnityEngine;

namespace Game
{
    /// <summary>
    /// Pure, stateless helper that converts screen bands to world coordinates.
    ///
    /// The Y calculation casts a ray from the camera onto the board plane (Z=0) and
    /// reads the intersection, so it stays correct even with the camera tilted on X
    /// and there is no trigonometry to derive by hand from the angle. The X axis is
    /// unaffected by the tilt, so width still comes straight from orthographicSize.
    /// </summary>
    public static class GameLayout
    {
        private static readonly Plane BoardPlane = new Plane(Vector3.forward, Vector3.zero);

        public static float VisibleWidth(Camera cam) => cam.orthographicSize * 2f * cam.aspect;

        /// <summary>Reference value only; the band calculations no longer use it.</summary>
        public static float VisibleHeight(Camera cam) => cam.orthographicSize * 2f;

        /// <summary>World Y at a fraction measured down the screen (0 = top edge, 1 = bottom).</summary>
        public static float WorldYAtViewportFraction(Camera cam, float fractionFromTop)
        {
            float viewportY = 1f - fractionFromTop;
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, viewportY, 0f));

            if (BoardPlane.Raycast(ray, out float dist))
                return ray.GetPoint(dist).y;

            // Fallback for a camera looking parallel to the board plane, which should
            // not happen: the old flat formula.
            float topY = cam.transform.position.y + cam.orthographicSize;
            return topY - fractionFromTop * VisibleHeight(cam);
        }

        public static float BandCenterY(Camera cam, float cumulativeBefore, float bandFraction)
            => WorldYAtViewportFraction(cam, cumulativeBefore + bandFraction * 0.5f);

        /// <summary>World Y of a band's top edge, for top-down stacking such as the queue.</summary>
        public static float BandTopY(Camera cam, float cumulativeBefore)
            => WorldYAtViewportFraction(cam, cumulativeBefore);

        public static float BoardBandCenterY(Camera cam, GameConfig c)
            => BandCenterY(cam, c.topBarBand, c.boardBand);

        public static float ParkBandCenterY(Camera cam, GameConfig c)
            => BandCenterY(cam, c.topBarBand + c.boardBand, c.parkBand);

        public static float QueueBandCenterY(Camera cam, GameConfig c)
            => BandCenterY(cam, c.topBarBand + c.boardBand + c.parkBand, c.queueBand);

        public static float QueueBandTopY(Camera cam, GameConfig c)
            => BandTopY(cam, c.topBarBand + c.boardBand + c.parkBand);
    }
}