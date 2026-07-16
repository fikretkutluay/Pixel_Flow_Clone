namespace Game
{
    [System.Serializable]
    public struct ShooterDef
    {
        public int column;
        public ColorId color;
        public int ammo;
        public bool isHidden;
        public int linkedCount;
    }
}