using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Shows the same checks as LevelData.OnValidate live, plus an ammo budget table.
    ///
    /// The budget now requires exact equality. Surplus ammo used to count as
    /// "enough" and pass green, but once a colour's ammo exceeds its cube count
    /// there is no cube left to spend the surplus on: the shooter never empties and
    /// stays stuck on the rail or in the park forever (Level_2's DarkGray +10 was
    /// exactly this).
    /// </summary>
    public static class LevelValidationView
    {
        /// <param name="hasSurplus">Whether any colour's ammo exceeds its cube count.
        /// Not a shortfall, but still unwanted: there is no cube to spend it on.</param>
        public static bool Draw(LevelData level, out bool hasSurplus)
        {
            EditorGUILayout.LabelField("Doğrulama", EditorStyles.boldLabel);

            bool hasDeficit = false;
            hasSurplus = false;
            int w = level.boardSize.x, h = level.boardSize.y;

            // 1) boardPixels length
            if (level.boardPixels == null || level.boardPixels.Length != w * h)
                Msg($"boardPixels uzunluğu ({level.boardPixels?.Length ?? 0}) != {w}x{h} = {w * h}", MessageType.Error);

            // 2) queue renkleri palette'te mi
            if (level.queue != null && level.palette != null)
            {
                foreach (var s in level.queue)
                    if (System.Array.IndexOf(level.palette, s.color) < 0)
                        Msg($"queue rengi '{s.color}' palette'te yok", MessageType.Error);
            }

            // 4) queue columns within the columnCount bound
            if (level.queue != null)
            {
                foreach (var s in level.queue)
                    if (s.column < 0 || s.column >= level.columnCount)
                        Msg($"queue'da column {s.column}, geçerli aralık 0..{level.columnCount - 1}", MessageType.Error);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Ammo Bütçesi (renk bazında)", EditorStyles.boldLabel);

            // 3) per-colour board cube count against queue ammo
            var cubeByColor = new Dictionary<ColorId, int>();
            if (level.boardPixels != null)
                foreach (var p in level.boardPixels)
                {
                    if (p == ColorId.None || p == ColorId.Crate) continue;
                    cubeByColor.TryGetValue(p, out int c);
                    cubeByColor[p] = c + 1;
                }

            var ammoByColor = new Dictionary<ColorId, int>();
            if (level.queue != null)
                foreach (var s in level.queue)
                {
                    ammoByColor.TryGetValue(s.color, out int a);
                    ammoByColor[s.color] = a + s.ammo;
                }

            // union of every colour appearing on the board or in the queue
            var colors = new HashSet<ColorId>(cubeByColor.Keys);
            colors.UnionWith(ammoByColor.Keys);

            if (colors.Count == 0)
            {
                EditorGUILayout.HelpBox("Henüz renkli küp veya atıcı yok.", MessageType.Info);
            }

            foreach (var color in colors)
            {
                cubeByColor.TryGetValue(color, out int cubes);
                ammoByColor.TryGetValue(color, out int ammo);
                int diff = ammo - cubes;
                bool exact = diff == 0;
                bool deficit = diff < 0;
                if (deficit) hasDeficit = true;
                else if (!exact) hasSurplus = true;   // diff > 0

                var prev = GUI.color;
                string diffText, mark;
                if (exact) { GUI.color = new Color(0.7f, 1f, 0.7f); diffText = "tam eşit"; mark = "✓"; }
                else if (deficit) { GUI.color = new Color(1f, 0.6f, 0.6f); diffText = $"EKSİK: {diff}"; mark = "✗"; }
                else { GUI.color = new Color(1f, 0.85f, 0.4f); diffText = $"FAZLA: +{diff}"; mark = "⚠"; }

                EditorGUILayout.LabelField(
                    $"{color,-7} board: {cubes,3}   |   ammo: {ammo,3}   |   {diffText}   {mark}");
                GUI.color = prev;
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.HelpBox(
                "Her rengin ammo'su küp sayısına TAM eşit olmalı. Eksik level'ı çözülemez " +
                "yapar; fazla ise o rengi taşıyan bir atıcının hiçbir zaman boşalmamasına " +
                "yol açar — sıralama yüzünden çözülemeyen level'lar yine de mümkün, en az " +
                "3 tam oynanışla doğrula.",
                MessageType.None);

            return hasDeficit;
        }

        private static void Msg(string text, MessageType type) => EditorGUILayout.HelpBox(text, type);
    }
}