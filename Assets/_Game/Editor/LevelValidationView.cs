using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// LevelData.OnValidate ile birebir aynı kontrolleri canlı gösterir + ammo bütçesi tablosu.
    /// Dönen değer: level'da eksik-ammo (çözülemez) durumu var mı — Kaydet uyarısı için.
    /// </summary>
    public static class LevelValidationView
    {
        public static bool Draw(LevelData level)
        {
            EditorGUILayout.LabelField("Doğrulama", EditorStyles.boldLabel);

            bool hasDeficit = false;
            int w = level.boardSize.x, h = level.boardSize.y;

            // 1) boardPixels uzunluğu
            if (level.boardPixels == null || level.boardPixels.Length != w * h)
                Msg($"boardPixels uzunluğu ({level.boardPixels?.Length ?? 0}) != {w}x{h} = {w * h}", MessageType.Error);

            // 2) queue renkleri palette'te mi
            if (level.queue != null && level.palette != null)
            {
                foreach (var s in level.queue)
                    if (System.Array.IndexOf(level.palette, s.color) < 0)
                        Msg($"queue rengi '{s.color}' palette'te yok", MessageType.Error);
            }

            // 4) queue sütunları columnCount sınırında mı
            if (level.queue != null)
            {
                foreach (var s in level.queue)
                    if (s.column < 0 || s.column >= level.columnCount)
                        Msg($"queue'da column {s.column}, geçerli aralık 0..{level.columnCount - 1}", MessageType.Error);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Ammo Bütçesi (renk bazında)", EditorStyles.boldLabel);

            // 3) renk bazında board küp sayısı vs. queue ammo
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

            // board'da veya queue'da geçen tüm renklerin birleşimi
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
                bool ok = diff >= 0;
                if (!ok) hasDeficit = true;

                var prev = GUI.color;
                GUI.color = ok ? new Color(0.7f, 1f, 0.7f) : new Color(1f, 0.6f, 0.6f);
                string diffText = ok ? $"fazla: +{diff}" : $"EKSİK: {diff}";
                EditorGUILayout.LabelField(
                    $"{color,-7} board: {cubes,3}   |   ammo: {ammo,3}   |   {diffText}   {(ok ? "✓" : "✗")}");
                GUI.color = prev;
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.HelpBox(
                "Ammo bütçesi yalnızca GEREKLİ koşuldur. Sıralama yüzünden çözülemeyen level'lar hâlâ mümkün — en az 3 tam oynanışla doğrula.",
                MessageType.None);

            return hasDeficit;
        }

        private static void Msg(string text, MessageType type) => EditorGUILayout.HelpBox(text, type);
    }
}