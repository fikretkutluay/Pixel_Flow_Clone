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

        public Entry[] entries = new Entry[]
        {
            new Entry { id = ColorId.Red,    color = Hex(0xE8453C) },
            new Entry { id = ColorId.Blue,   color = Hex(0x3B82F6) },
            new Entry { id = ColorId.Green,  color = Hex(0x4ADE58) },
            new Entry { id = ColorId.Yellow, color = Hex(0xFACC15) },
            new Entry { id = ColorId.Purple, color = Hex(0xA855F7) },
            new Entry { id = ColorId.Crate,  color = Hex(0xC08552) },
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