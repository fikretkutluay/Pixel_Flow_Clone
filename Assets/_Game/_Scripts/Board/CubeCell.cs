namespace Game
{
    public struct CubeCell
    {
        public ColorId color;
        public bool isCrate;
        public static CubeCell Create(ColorId color, bool isCrate)
        {
            return new CubeCell
            {
                color = color,
                isCrate = isCrate
            };
        }
    }
}