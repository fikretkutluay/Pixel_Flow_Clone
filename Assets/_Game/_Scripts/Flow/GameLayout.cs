using UnityEngine;

namespace Game
{
    /// <summary>
    /// Ekran bölge bantlarını dünya koordinatına çeviren saf yardımcı.
    /// Durumsuz, statik — LaneRaycaster/TrackPath gibi. Yeni singleton/MonoBehaviour değil.
    /// Görünür yüksekliği tek doğruluk kaynağı olan cam.orthographicSize'dan okur.
    /// Bant sıralaması (topBar→board→park→queue→bottom) ekran yapısıdır, kodda sabittir.
    /// </summary>
    public static class GameLayout
    {
        public static float VisibleHeight(Camera cam) => cam.orthographicSize * 2f;
        public static float VisibleWidth(Camera cam) => cam.orthographicSize * 2f * cam.aspect;

        /// <summary>Bir bandın dünya-Y merkezi. cumulativeBefore = üstündeki bantların toplam oranı.</summary>
        public static float BandCenterY(Camera cam, float cumulativeBefore, float bandFraction)
        {
            float topY = cam.transform.position.y + cam.orthographicSize;
            return topY - (cumulativeBefore + bandFraction * 0.5f) * VisibleHeight(cam);
        }

        public static float BoardBandCenterY(Camera cam, GameConfig c)
            => BandCenterY(cam, c.topBarBand, c.boardBand);

        public static float ParkBandCenterY(Camera cam, GameConfig c)
            => BandCenterY(cam, c.topBarBand + c.boardBand, c.parkBand);

        public static float QueueBandCenterY(Camera cam, GameConfig c)
            => BandCenterY(cam, c.topBarBand + c.boardBand + c.parkBand, c.queueBand);
    }
}