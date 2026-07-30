using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Editör içi ColorId → görüntü rengi eşlemesi.
    ///
    /// Renkleri ARTIK KENDİ İÇİNDE TUTMUYOR: doğrudan ColorPalette asset'ini
    /// okuyor. Önceden hex'ler burada da kopyalıydı ve palet kalibre edilince
    /// Level Designer oyunda görünmeyen renkleri göstermeye başladı — level
    /// tasarımının en çok güvenmesi gereken yerde yanlış bilgi.
    /// </summary>
    public static class LevelDesignerColors
    {
        public static readonly Color Empty = new Color(0.17f, 0.16f, 0.29f); // boş hücre (koyu)
        public static readonly Color GridLine = new Color(0f, 0f, 0f, 0.25f);

        private static ColorPalette cached;

        private static ColorPalette Palette
        {
            get
            {
                if (cached != null) return cached;

                string[] guids = AssetDatabase.FindAssets("t:ColorPalette");
                if (guids.Length == 0) return null;

                cached = AssetDatabase.LoadAssetAtPath<ColorPalette>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
                return cached;
            }
        }

        public static Color Of(ColorId c)
        {
            if (c == ColorId.None) return Empty;

            ColorPalette palette = Palette;
            return palette != null ? palette.Of(c) : Color.magenta; // tanımsız = göze batsın
        }
    }
}
