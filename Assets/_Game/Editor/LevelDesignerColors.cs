using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Maps ColorId to a display colour inside the editor tools.
    ///
    /// It no longer holds any colours of its own and reads the ColorPalette asset
    /// directly. The hex values used to be duplicated here, and once the palette was
    /// recalibrated the Level Designer began showing colours that did not appear in
    /// the game — wrong information in the one place level design has to trust.
    /// </summary>
    public static class LevelDesignerColors
    {
        public static readonly Color Empty = new Color(0.17f, 0.16f, 0.29f); // empty cell (dark)
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
            return palette != null ? palette.Of(c) : Color.magenta; // undefined ids should stand out
        }
    }
}
