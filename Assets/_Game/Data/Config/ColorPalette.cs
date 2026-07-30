using UnityEngine;

namespace Game
{
    /// <summary>
    /// ColorId → render rengi eşlemesi, tek doğruluk kaynağı.
    /// Değerler görsel plan Bölüm 4.1'deki hex paletidir.
    /// Kod değil asset olmasının sebebi: Faz 3 boyunca renk üzerinde sık
    /// oynanacak, her denemede derleme beklememek için ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "ColorPalette", menuName = "Scriptable Objects/ColorPalette")]
    public class ColorPalette : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public ColorId id;
            public Color color;
        }

        // Doygunluk ve parlaklık referans ekran görüntülerinden ölçülerek
        // kalibre edildi; GDD §4.1'in ham hex'leri artık geçerli değil.
        // Referansta atıcı gövdeleri S≈0.52 (bizde 0.72 idi) ve board küpleri
        // V 0.42-1.00 arasına yayılıyor (bizde 0.71-0.83'te sıkışıktı).
        // Koyu ve nötr tonlar o aralığın uçlarını açmak için var.
        //
        // 13 oynanabilir renk birbirine yaklaşıyor, o yüzden ayırt edilebilirlik
        // ölçüldü: en yakın çift CIELAB'da dE 26.1 (White/LightGray). Karışma
        // riski asıl olarak level'ın SEÇTİĞİ alt palette'te; yeni renk eklerken
        // veya bir level'a renk seçerken o alt kümenin dE'si kontrol edilmeli.
        public Entry[] entries = new Entry[]
        {
            new Entry { id = ColorId.Red,        color = Hex(0xC5615C) },
            new Entry { id = ColorId.Blue,       color = Hex(0x608BD1) },
            new Entry { id = ColorId.Green,      color = Hex(0x5DBD66) },
            new Entry { id = ColorId.Yellow,     color = Hex(0xD5BB55) },
            new Entry { id = ColorId.Purple,     color = Hex(0x9F69D2) },
            new Entry { id = ColorId.Navy,       color = Hex(0x233361) },
            new Entry { id = ColorId.White,      color = Hex(0xE9EDF2) },
            new Entry { id = ColorId.Khaki,      color = Hex(0x909954) },
            new Entry { id = ColorId.Maroon,     color = Hex(0x75313D) },
            new Entry { id = ColorId.DarkPurple, color = Hex(0x763980) },
            new Entry { id = ColorId.DarkGray,   color = Hex(0x585A61) },
            new Entry { id = ColorId.LightGray,  color = Hex(0xA0A5AD) },
            new Entry { id = ColorId.Black,      color = Hex(0x18181F) },
            new Entry { id = ColorId.Crate,      color = Hex(0xA37A56) },
        };

        public Color Of(ColorId id)
        {
            foreach (var e in entries)
                if (e.id == id) return e.color;
            return Color.magenta; // tanımsız = göze batsın
        }

        private static Color Hex(int rgb) => new Color(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >> 8) & 0xFF) / 255f,
            (rgb & 0xFF) / 255f, 1f);
    }
}