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
        // Rail speed is game-wide, not per level: it lives in GameConfig as a lap
        // duration so every level runs at the same visual pace (RULE 2).
        //
        // There is no rescue window. A shooter landing while the park is full is an
        // immediate loss.

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

                // Per colour, total ammo must equal the cube count exactly. Too
                // little and the level is unsolvable (error). Too much and the
                // shooter carrying the surplus finds no cube to spend it on, never
                // empties, and stays stuck on the rail or in the park forever
                // (warning; Level_2's DarkGray +10 was exactly this).
                var allColors = new System.Collections.Generic.HashSet<ColorId>(cubeCountByColor.Keys);
                allColors.UnionWith(ammoByColor.Keys);

                foreach (var color in allColors)
                {
                    cubeCountByColor.TryGetValue(color, out int cubes);
                    ammoByColor.TryGetValue(color, out int totalAmmo);

                    if (totalAmmo < cubes)
                        Debug.LogError($"[{name}] color '{color}' has {cubes} cubes but only {totalAmmo} ammo in queue");
                    else if (totalAmmo > cubes)
                        Debug.LogWarning($"[{name}] color '{color}' has {totalAmmo} ammo but only {cubes} cubes — " +
                                          "the surplus can never be fired, so a shooter carrying it will never empty");
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