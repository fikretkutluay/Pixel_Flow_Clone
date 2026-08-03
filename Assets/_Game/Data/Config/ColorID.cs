namespace Game
{
    public enum ColorId
    {
        None,
        Crate,
        Red,
        Blue,
        Green,
        Yellow,
        Purple,

        // Contrast extremes and mid tones. More than half of the reference board is
        // a dark mass with the remainder as bright accent, and that range is what
        // makes the picture readable. The first six colours were bunched in the
        // middle band (V 0.71-0.83); these open it out to 0.12-0.95.
        //
        // All of them are appended at the END so existing ids do not shift and
        // authored levels stay intact. Never delete a colour: every id after it
        // would shift and every level would be corrupted.
        Navy,
        White,
        Khaki,
        Maroon,
        DarkPurple,
        DarkGray,
        LightGray,
        Black,

        Pink,
        Orange,
        Flesh,
        Brawn,
        LightBrawn
    }
}