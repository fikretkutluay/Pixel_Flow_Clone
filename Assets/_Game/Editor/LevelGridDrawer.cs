using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Board grid'ini çizer ve tıklama/sürükleme ile boyar.
    /// boardPixels index'i: (height-1-editorRow)*width + x  →  editör üstü = oyun tepesi.
    /// Sol tık: aktif renk. Sağ tık: sil (None). Bir sürükleme = tek undo adımı.
    /// </summary>
    public static class LevelGridDrawer
    {
        private static int activeUndoGroup = -1;

        /// <summary>Editör hücresi → boardPixels index'i (y-flip burada).</summary>
        public static int IndexOf(LevelData level, int x, int editorRow)
        {
            int gameY = level.boardSize.y - 1 - editorRow;
            return gameY * level.boardSize.x + x;
        }

        /// <summary>Grid'i verilen alana çizer, tıklamayı işler.</summary>
        public static void Draw(Rect area, LevelData level, ColorId activePaint, float cellSize)
        {
            int w = level.boardSize.x;
            int h = level.boardSize.y;
            if (w <= 0 || h <= 0) return;
            if (level.boardPixels == null || level.boardPixels.Length != w * h)
            {
                EditorGUI.HelpBox(area, "boardPixels boyutu boardSize ile uyuşmuyor. Üstteki 'Board'u Sıfırla' ile başlat.", MessageType.Warning);
                return;
            }

            Vector2 gridOrigin = new Vector2(area.x, area.y);

            for (int row = 0; row < h; row++)
            {
                for (int x = 0; x < w; x++)
                {
                    Rect cell = new Rect(
                        gridOrigin.x + x * cellSize,
                        gridOrigin.y + row * cellSize,
                        cellSize, cellSize);

                    ColorId id = level.boardPixels[IndexOf(level, x, row)];
                    EditorGUI.DrawRect(cell, LevelDesignerColors.Of(id));

                    if (cellSize >= 6f)
                        DrawOutline(cell, LevelDesignerColors.GridLine);
                }
            }

            Rect gridRect = new Rect(gridOrigin.x, gridOrigin.y, w * cellSize, h * cellSize);
            HandleMouse(gridRect, level, activePaint, cellSize, w, h);
        }

        private static void HandleMouse(Rect gridRect, LevelData level, ColorId activePaint,
                                        float cellSize, int w, int h)
        {
            Event e = Event.current;
            if (!gridRect.Contains(e.mousePosition)) return;

            bool isPaint = (e.type == EventType.MouseDown || e.type == EventType.MouseDrag);
            if (!isPaint) return;

            ColorId value;
            if (e.button == 0) value = activePaint;
            else if (e.button == 1) value = ColorId.None;
            else return;

            if (e.type == EventType.MouseDown)
            {
                Undo.IncrementCurrentGroup();
                activeUndoGroup = Undo.GetCurrentGroup();
            }

            int localX = Mathf.FloorToInt((e.mousePosition.x - gridRect.x) / cellSize);
            int localRow = Mathf.FloorToInt((e.mousePosition.y - gridRect.y) / cellSize);
            if (localX < 0 || localX >= w || localRow < 0 || localRow >= h) return;

            int index = IndexOf(level, localX, localRow);

            if (level.boardPixels[index] != value)
            {
                Undo.RecordObject(level, "Paint Board");
                level.boardPixels[index] = value;
                EditorUtility.SetDirty(level);
            }

            GUI.changed = true;   // pencere kendini repaint etsin (dışarıdan Repaint yok)
            e.Use();
        }

        /// <summary>MouseUp'ta çağrılır: stroke'u tek undo adımına toplar.</summary>
        public static void EndStroke()
        {
            if (activeUndoGroup >= 0)
            {
                Undo.CollapseUndoOperations(activeUndoGroup);
                activeUndoGroup = -1;
            }
        }

        private static void DrawOutline(Rect r, Color c)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1f), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1f, r.width, 1f), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 1f, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - 1f, r.y, 1f, r.height), c);
        }
    }
}