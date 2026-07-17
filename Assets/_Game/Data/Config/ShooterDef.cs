namespace Game
{
    [System.Serializable]
    public struct ShooterDef
    {
        public int column; //0..(LevelData.columnCount-1)
        public ColorId color;
        public int ammo;
        public bool isHidden;
        public int linkedCount;
    }
}