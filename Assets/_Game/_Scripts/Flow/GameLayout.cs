using UnityEngine;

namespace Game
{
    /// <summary>
    /// Ekran bölge bantlarını dünya koordinatına çeviren saf yardımcı.
    /// Durumsuz, statik. Y hesabı artık kameradan board düzlemine (Z=0) ışın
    /// gönderip kesişim noktasını okuyor — bu yüzden kamera tilt'li olsa
    /// (X ekseninde eğik) bile doğru sonuç verir, açıya göre elle trig
    /// türetmeye gerek yok. X ekseni tilt'ten etkilenmediği için genişlik
    /// hesabı hâlâ doğrudan orthographicSize'dan.
    /// </summary>
    public static class GameLayout
    {
        private static readonly Plane BoardPlane = new Plane(Vector3.forward, Vector3.zero); // Z=0 düzlemi

        public static float VisibleWidth(Camera cam) => cam.orthographicSize * 2f * cam.aspect;
        public static float VisibleHeight(Camera cam) => cam.orthographicSize * 2f; // yalnızca referans; band hesaplarında artık kullanılmıyor

        /// <summary>Ekranın üstten verilen orana (0=üst kenar, 1=alt kenar) karşılık gelen dünya-Y'si.</summary>
        public static float WorldYAtViewportFraction(Camera cam, float fractionFromTop)
        {
            float viewportY = 1f - fractionFromTop;
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, viewportY, 0f));

            if (BoardPlane.Raycast(ray, out float dist))
                return ray.GetPoint(dist).y;

            // Fallback: kamera board düzlemine paralel bakıyorsa (olmamalı) eski düz formül
            float topY = cam.transform.position.y + cam.orthographicSize;
            return topY - fractionFromTop * VisibleHeight(cam);
        }

        public static float BandCenterY(Camera cam, float cumulativeBefore, float bandFraction)
            => WorldYAtViewportFraction(cam, cumulativeBefore + bandFraction * 0.5f);

        /// <summary>Bir bandın ÜST kenarının dünya-Y'si (queue gibi "tepeden aşağı dizilim" için).</summary>
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