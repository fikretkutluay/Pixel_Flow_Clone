using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Editör içi ColorId → görüntü rengi eşlemesi. SADECE editör önizlemesi için;
    /// runtime CubeView'dan bağımsız (o gerçek renklerini Faz 3'te alacak).
    /// Değerler görsel plan Bölüm 4.1'deki hex paletidir.
    /// </summary>
    public static class LevelDesignerColors
    {
        public static readonly Color Empty = new Color(0.17f, 0.16f, 0.29f); // boş hücre (koyu)
        public static readonly Color GridLine = new Color(0f, 0f, 0f, 0.25f);

        public static Color Of(ColorId c)
        {
            switch (c)
            {
                case ColorId.Red:    return Hex(0xE8453C);
                case ColorId.Blue:   return Hex(0x3B82F6);
                case ColorId.Green:  return Hex(0x4ADE58);
                case ColorId.Yellow: return Hex(0xFACC15);
                case ColorId.Purple: return Hex(0xA855F7);
                case ColorId.Crate:  return Hex(0xC08552);
                case ColorId.None:   return Empty;
                default:             return Color.magenta; // tanımsız = göze batsın
            }
        }

        private static Color Hex(int rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f,
                1f);
        }
    }
}