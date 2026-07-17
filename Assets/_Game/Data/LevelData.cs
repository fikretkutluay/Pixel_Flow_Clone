using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "Scriptable Objects/LevelData")]
    public class LevelData : ScriptableObject
    {
        public int levelID;
        public Vector2Int boardSize;
        public ColorId[] boardPixels;
        public ColorId[] palette;
        public ShooterDef[] queue;
        public int columnCount = 4;
        public int trackCapacity = 5;
        public int parkCapacity = 5;
        public float trackSpeed;
        public float rescueWindowSeconds = 2f;


        private void OnValidate()
        {
            if (boardPixels != null && boardPixels.Length != boardSize.x * boardSize.y)
            {
                Debug.LogError($"[{name}] boardPixels length ({boardPixels.Length}) " +
                                $"!= boardSize ({boardSize.x}x{boardSize.y} = {boardSize.x * boardSize.y})");
            }

            if (queue != null && palette != null)
            {
                foreach (var shooter in queue)
                {
                    bool colorInPalette = System.Array.IndexOf(palette, shooter.color) >= 0;
                    if (!colorInPalette)
                    {
                        Debug.LogError($"[{name}] queue contains color '{shooter.color}' not in palette");
                    }
                }
            }

            if (boardPixels != null && queue != null)
            {
                var cubeCountByColor = new System.Collections.Generic.Dictionary<ColorId, int>();
                foreach (var pixel in boardPixels)
                {
                    if (pixel == ColorId.None || pixel == ColorId.Crate) continue;
                    cubeCountByColor.TryGetValue(pixel, out int count);
                    cubeCountByColor[pixel] = count + 1;
                }

                var ammoByColor = new System.Collections.Generic.Dictionary<ColorId, int>();
                foreach (var shooter in queue)
                {
                    ammoByColor.TryGetValue(shooter.color, out int ammo);
                    ammoByColor[shooter.color] = ammo + shooter.ammo;
                }

                foreach (var kvp in cubeCountByColor)
                {
                    ammoByColor.TryGetValue(kvp.Key, out int totalAmmo);
                    if (totalAmmo < kvp.Value)
                    {
                        Debug.LogError($"[{name}] color '{kvp.Key}' has {kvp.Value} cubes but only {totalAmmo} ammo in queue");
                    }
                }


            }

            if (queue != null)
            {
                foreach (var shooter in queue)
                {
                    if (shooter.column < 0 || shooter.column >= columnCount)
                    {
                        Debug.LogError($"[{name}] queue has shooter with column {shooter.column}, " +
                                        $"but columnCount is {columnCount} (valid: 0..{columnCount - 1})");
                    }
                }
            }
        }
    }
}