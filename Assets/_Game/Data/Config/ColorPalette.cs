using UnityEngine;

namespace Game
{
    /// <summary>
    /// Single source of truth mapping ColorId to a render colour.
    ///
    /// An asset rather than code because colours were tuned constantly during the
    /// art pass, and a ScriptableObject avoids waiting on a recompile per attempt.
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

        // Saturation and value were calibrated by measuring the reference
        // screenshots; the raw hex values in GDD 4.1 no longer apply. In the
        // reference, shooter bodies sit at S~0.52 (ours were 0.72) and board cubes
        // spread across V 0.42-1.00 (ours were bunched at 0.71-0.83). The dark and
        // neutral tones exist to open out the ends of that range.
        //
        // With 13 playable colours the palette gets crowded, so distinguishability
        // was measured: the closest pair is dE 26.1 in CIELAB (White/LightGray).
        // The real risk of confusion is within the subset a level selects, so check
        // that subset's dE when adding a colour or choosing colours for a level.
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
            new Entry { id = ColorId.Pink,       color = Hex(0xDB0081) },
            new Entry { id = ColorId.Crate,      color = Hex(0xA37A56) },
            new Entry { id = ColorId.Orange,     color = Hex(0xf89800) },
            new Entry { id = ColorId.Flesh,      color = Hex(0xF8C096) },
            new Entry { id = ColorId.Brawn,      color = Hex(0x3F2004) },
            new Entry { id = ColorId.LightBrawn, color = Hex(0x90582D) }
        };

        public Color Of(ColorId id)
        {
            foreach (var e in entries)
                if (e.id == id) return e.color;
            return Color.magenta; // undefined ids should be impossible to miss
        }

        private static Color Hex(int rgb) => new Color(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >> 8) & 0xFF) / 255f,
            (rgb & 0xFF) / 255f, 1f);
    }
}