using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    public class LevelDesignerWindow : EditorWindow
    {
        private LevelData target;
        private ColorId activePaint = ColorId.Red;
        private float cellSize = 22f;

        private Vector2 centerScroll;
        private Vector2 rightScroll;
        private bool paletteFoldout = true;

        private int selectedQueueIndex = -1;
        private int queueStampAmmo = 20;

        // Queue preview metrics
        private const float BoxH = 26f;
        private const float BoxGap = 3f;
        private const float AddH = 20f;
        private const float QueueHeaderH = 16f;
        private const float BoardQueueGap = 20f;

        [MenuItem("Tools/Level Designer")]
        public static void Open()
        {
            var win = GetWindow<LevelDesignerWindow>("Level Designer");
            win.minSize = new Vector2(900, 500);
        }

        private void OnGUI()
        {
            DrawTopStrip();

            if (target == null)
            {
                EditorGUILayout.HelpBox("Düzenlemek için bir LevelData asset'i seç (üstteki alan) ya da 'Yeni Level' oluştur.", MessageType.Info);
                return;
            }

            EnsureUsableBoard();

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawCenterPreview();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();

            if (Event.current.type == EventType.MouseUp)
                LevelGridDrawer.EndStroke();
        }

        // ---- TOOLBAR ----
        private void DrawTopStrip()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();
            target = (LevelData)EditorGUILayout.ObjectField(target, typeof(LevelData), false, GUILayout.Width(220));
            if (EditorGUI.EndChangeCheck()) { GUI.FocusControl(null); selectedQueueIndex = -1; }

            if (target != null)
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Boyut", GUILayout.Width(38));
                int newW = EditorGUILayout.IntField(target.boardSize.x, GUILayout.Width(45));
                EditorGUILayout.LabelField("x", GUILayout.Width(12));
                int newH = EditorGUILayout.IntField(target.boardSize.y, GUILayout.Width(45));
                if ((newW != target.boardSize.x || newH != target.boardSize.y) && newW > 0 && newH > 0)
                    ResizeBoard(Mathf.Max(1, newW), Mathf.Max(1, newH));

                if (GUILayout.Button("Board'u Sıfırla", EditorStyles.toolbarButton, GUILayout.Width(100)))
                    InitBoardPixels();

                // Boards opened up wide "to be safe" and filled in the middle
                // leave empty rows around the edge. An empty cell takes no space in
                // game, but it does distort the board's aspect ratio, which shrinks
                // the cells needlessly when the board is fitted to its area.
                if (TryGetContentBounds(out int bx0, out int by0, out int bx1, out int by1))
                {
                    int tw = bx1 - bx0 + 1, th = by1 - by0 + 1;
                    bool canTrim = tw != target.boardSize.x || th != target.boardSize.y;
                    using (new EditorGUI.DisabledScope(!canTrim))
                    {
                        string label = canTrim
                            ? $"Kırp ({target.boardSize.x}×{target.boardSize.y} → {tw}×{th})"
                            : "Kırp";
                        if (GUILayout.Button(label, EditorStyles.toolbarButton, GUILayout.Width(150)))
                            TrimBoard();
                    }
                }

                GUILayout.Space(10);
                EditorGUILayout.LabelField("Sütun", GUILayout.Width(42));
                int newCols = Mathf.Clamp(EditorGUILayout.IntField(target.columnCount, GUILayout.Width(40)), 1, 8);
                if (newCols != target.columnCount)
                {
                    Undo.RecordObject(target, "Edit Column Count");
                    target.columnCount = newCols;
                    EditorUtility.SetDirty(target);
                }

                GUILayout.Space(10);
                EditorGUILayout.LabelField("Hücre", GUILayout.Width(38));
                cellSize = EditorGUILayout.Slider(cellSize, 6f, 40f, GUILayout.Width(120));
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Yeni Level", EditorStyles.toolbarButton, GUILayout.Width(90)))
                CreateNewLevel();

            using (new EditorGUI.DisabledScope(target == null))
            {
                if (GUILayout.Button("Kaydet", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    Save();
            }

            EditorGUILayout.EndHorizontal();
        }

        // ---- LEFT PANEL: tools, palette and brush ----
        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(160));
            EditorGUILayout.LabelField("Araçlar", EditorStyles.boldLabel);

            if (target.palette == null || target.palette.Length == 0)
                EditorGUILayout.HelpBox("Palet boş. Aşağıdan renk ekle.", MessageType.Warning);
            else
                foreach (var col in target.palette)
                    DrawToolButton(col, col.ToString());

            EditorGUILayout.Space(4);
            DrawToolButton(ColorId.Crate, "Sandık (Crate)");
            DrawToolButton(ColorId.None, "Sil (Erase)");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField($"Aktif fırça: {activePaint}", EditorStyles.miniBoldLabel);

            // Ammo given to a shooter added with the queue's "+" button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Atıcı ammo:", GUILayout.Width(70));
            queueStampAmmo = Mathf.Max(0, EditorGUILayout.IntField(queueStampAmmo, GUILayout.Width(50)));
            EditorGUILayout.EndHorizontal();
            if (!IsQueueBrushValid())
                EditorGUILayout.HelpBox("Atıcı eklemek için bir palette rengi seç (Crate/Erase ile eklenemez).", MessageType.None);

            EditorGUILayout.Space(10);
            DrawPaletteEditor();

            EditorGUILayout.EndVertical();
        }

        private void DrawToolButton(ColorId id, string label)
        {
            bool selected = activePaint == id;
            var style = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };
            if (selected) style.fontStyle = FontStyle.Bold;

            EditorGUILayout.BeginHorizontal();
            Rect swatch = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16), GUILayout.Height(16));
            EditorGUI.DrawRect(swatch, LevelDesignerColors.Of(id));
            if (GUILayout.Button((selected ? "▶ " : "") + label, style))
                activePaint = id;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPaletteEditor()
        {
            paletteFoldout = EditorGUILayout.Foldout(paletteFoldout, "Palet Düzenle");
            if (!paletteFoldout) return;

            var list = new List<ColorId>(target.palette ?? new ColorId[0]);
            bool changed = false;
            int removeIndex = -1;

            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                Rect swatch = GUILayoutUtility.GetRect(14, 14, GUILayout.Width(14), GUILayout.Height(14));
                EditorGUI.DrawRect(swatch, LevelDesignerColors.Of(list[i]));
                ColorId picked = (ColorId)EditorGUILayout.EnumPopup(list[i]);
                if (picked != list[i]) { list[i] = picked; changed = true; }
                if (GUILayout.Button("−", GUILayout.Width(22)))
                    removeIndex = i;
                EditorGUILayout.EndHorizontal();
            }

            if (removeIndex >= 0) { list.RemoveAt(removeIndex); changed = true; }
            if (GUILayout.Button("+ Renk Ekle")) { list.Add(ColorId.Red); changed = true; }
            if (changed) CommitPalette(list);
        }

        private void CommitPalette(List<ColorId> list)
        {
            Undo.RecordObject(target, "Edit Palette");
            target.palette = list.ToArray();
            EditorUtility.SetDirty(target);
        }

        // ---- CENTRE: board and queue preview, one scroll view, absolute rects ----
        private void DrawCenterPreview()
        {
            Rect outer = GUILayoutUtility.GetRect(
                100, 4000, 100, 4000,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            float boardW = target.boardSize.x * cellSize;
            float boardH = target.boardSize.y * cellSize;

            // Compute the queue metrics up front, for the content height
            int cols = Mathf.Max(1, target.columnCount);
            var queue = target.queue ?? new ShooterDef[0];
            int maxInCol = 0;
            int overflow = 0;
            var perCol = new int[cols];
            foreach (var s in queue)
            {
                if (s.column >= 0 && s.column < cols) { perCol[s.column]++; if (perCol[s.column] > maxInCol) maxInCol = perCol[s.column]; }
                else overflow++;
            }

            float queueColH = QueueHeaderH + maxInCol * (BoxH + BoxGap) + AddH + 6f;
            float overflowH = overflow > 0 ? (18f + BoxH + 8f) : 0f;
            float queueH = queueColH + overflowH;

            float contentW = Mathf.Max(boardW, 200f);
            float contentH = boardH + BoardQueueGap + queueH + 10f;

            centerScroll = GUI.BeginScrollView(outer, centerScroll, new Rect(0, 0, contentW, contentH));

            // The board starts at (0,0)
            LevelGridDrawer.Draw(new Rect(0, 0, boardW, boardH), target, activePaint, cellSize);

            // The queue sits below the board, spread across its width and aligned to the same x
            Rect queueArea = new Rect(0, boardH + BoardQueueGap, boardW, queueH);
            DrawQueuePreview(queueArea, cols, overflow);

            GUI.EndScrollView();
        }

        private void DrawQueuePreview(Rect area, int cols, int overflowCount)
        {
            var list = new List<ShooterDef>(target.queue ?? new ShooterDef[0]);
            bool changed = false;
            int addColumn = -1;
            bool brushValid = IsQueueBrushValid();

            float colW = area.width / cols;

            // Queue preview label
            GUI.Label(new Rect(area.x, area.y - 16f, 200f, 14f), "Kuyruk (oyundaki gibi)", EditorStyles.miniBoldLabel);

            // Column backgrounds and headers
            for (int c = 0; c < cols; c++)
            {
                float cx = area.x + c * colW;
                EditorGUI.DrawRect(new Rect(cx + 1, area.y, colW - 2, area.height), new Color(0f, 0f, 0f, 0.15f));
                GUI.Label(new Rect(cx + 4, area.y, colW, 14f), $"S{c}", EditorStyles.miniLabel);
            }

            // Track the stack y per column
            var colY = new float[cols];
            for (int c = 0; c < cols; c++) colY[c] = area.y + QueueHeaderH;

            // Shooter boxes, in list order, which is the order they are played
            for (int i = 0; i < list.Count; i++)
            {
                int c = list[i].column;
                if (c < 0 || c >= cols) continue;

                float cx = area.x + c * colW;
                Rect box = new Rect(cx + 3, colY[c], colW - 6, BoxH);
                colY[c] += BoxH + BoxGap;

                DrawShooterBoxAt(box, i, list[i]);
                if (GUI.Button(box, GUIContent.none, GUIStyle.none))
                    selectedQueueIndex = i;
            }

            // An "add" button under each column
            for (int c = 0; c < cols; c++)
            {
                float cx = area.x + c * colW;
                Rect addR = new Rect(cx + 3, colY[c], colW - 6, AddH);
                bool prev = GUI.enabled;
                GUI.enabled = brushValid;
                if (GUI.Button(addR, "+")) addColumn = c;
                GUI.enabled = prev;
            }

            // Shooters with an invalid column, shown on a separate row below
            if (overflowCount > 0)
            {
                float oy = area.y + (QueueHeaderH + MaxColHeight(colY, area.y));
                oy = area.yMax - BoxH - 6f;
                GUI.Label(new Rect(area.x, oy - 16f, area.width, 14f), "⚠ Geçersiz sütun (columnCount dışında):", EditorStyles.miniBoldLabel);
                float ox = area.x + 3f;
                for (int i = 0; i < list.Count; i++)
                {
                    int c = list[i].column;
                    if (c >= 0 && c < cols) continue;
                    Rect box = new Rect(ox, oy, 40f, BoxH);
                    ox += 44f;
                    DrawShooterBoxAt(box, i, list[i]);
                    if (GUI.Button(box, GUIContent.none, GUIStyle.none))
                        selectedQueueIndex = i;
                }
            }

            if (addColumn >= 0)
            {
                list.Add(new ShooterDef
                {
                    column = addColumn,
                    color = activePaint,
                    ammo = queueStampAmmo,
                    isHidden = false,
                    linkedCount = 1
                });
                selectedQueueIndex = list.Count - 1;
                changed = true;
            }

            if (changed)
            {
                Undo.RecordObject(target, "Edit Queue");
                target.queue = list.ToArray();
                EditorUtility.SetDirty(target);
                Repaint();
            }
        }

        private float MaxColHeight(float[] colY, float top)
        {
            float max = 0f;
            foreach (var y in colY) max = Mathf.Max(max, y - top);
            return max;
        }

        private void DrawShooterBoxAt(Rect r, int index, ShooterDef s)
        {
            Color bg = LevelDesignerColors.Of(s.color);
            EditorGUI.DrawRect(r, bg);

            if (selectedQueueIndex == index)
                DrawOutline(r, Color.white, 2f);

            string lbl = s.isHidden ? $"?{s.ammo}" : s.ammo.ToString();
            if (s.linkedCount > 1) lbl += $" x{s.linkedCount}";

            var style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = TextColorFor(bg);
            GUI.Label(r, lbl, style);
        }

        private static void DrawOutline(Rect r, Color c, float t)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, t), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - t, r.width, t), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, t, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        // ---- RIGHT: validation and the selected shooter editor ----
        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            rightScroll = EditorGUILayout.BeginScrollView(rightScroll);

            // Selected shooter editor
            var list = new List<ShooterDef>(target.queue ?? new ShooterDef[0]);
            if (selectedQueueIndex >= 0 && selectedQueueIndex < list.Count)
            {
                if (DrawSelectedShooterEditor(list, ref selectedQueueIndex))
                {
                    Undo.RecordObject(target, "Edit Queue");
                    target.queue = list.ToArray();
                    EditorUtility.SetDirty(target);
                }
            }
            else
            {
                selectedQueueIndex = Mathf.Min(selectedQueueIndex, list.Count - 1);
                EditorGUILayout.HelpBox("Önizlemedeki bir atıcıya tıkla ya da bir sütuna '+' ile ekle.", MessageType.Info);
            }

            EditorGUILayout.Space(8);
            bool deficit = LevelValidationView.Draw(target, out bool surplus);
            if (deficit)
                EditorGUILayout.HelpBox("Eksik ammo var — level çözülemeyebilir. Yine de kaydedebilirsin.", MessageType.Warning);
            if (surplus)
                EditorGUILayout.HelpBox("Fazla ammo var — bir atıcı hiçbir zaman boşalmaz ve rayda/parkta " +
                                        "takılı kalır. İstenmeyen bir durum; kaydetmeden önce düzelt.", MessageType.Warning);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private bool DrawSelectedShooterEditor(List<ShooterDef> list, ref int sel)
        {
            var s = list[sel];
            bool changed = false;

            EditorGUILayout.LabelField($"Seçili atıcı  #{sel}  (S{s.column})", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Renk", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (target.palette != null)
            {
                foreach (var col in target.palette)
                {
                    var prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = LevelDesignerColors.Of(col);
                    bool isCur = s.color == col;
                    if (GUILayout.Button(isCur ? "●" : " ", GUILayout.Width(26), GUILayout.Height(20)))
                        if (s.color != col) { s.color = col; changed = true; }
                    GUI.backgroundColor = prevBg;
                }
            }
            EditorGUILayout.EndHorizontal();

            int newCol = Mathf.Clamp(EditorGUILayout.IntField("Sütun", s.column), 0, target.columnCount - 1);
            if (newCol != s.column) { s.column = newCol; changed = true; }

            int ammo = Mathf.Max(0, EditorGUILayout.IntField("Ammo", s.ammo));
            if (ammo != s.ammo) { s.ammo = ammo; changed = true; }

            bool hidden = EditorGUILayout.Toggle("Gizli (?)", s.isHidden);
            if (hidden != s.isHidden) { s.isHidden = hidden; changed = true; }

            int linked = Mathf.Max(1, EditorGUILayout.IntField("Linked", s.linkedCount));
            if (linked != s.linkedCount) { s.linkedCount = linked; changed = true; }

            if (changed) list[sel] = s;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("▲ Yukarı"))
            {
                int prev = PrevInColumn(list, sel);
                if (prev >= 0) { Swap(list, sel, prev); sel = prev; changed = true; }
            }
            if (GUILayout.Button("▼ Aşağı"))
            {
                int next = NextInColumn(list, sel);
                if (next >= 0) { Swap(list, sel, next); sel = next; changed = true; }
            }
            if (GUILayout.Button("Sil"))
            {
                list.RemoveAt(sel);
                sel = -1;
                changed = true;
            }
            EditorGUILayout.EndHorizontal();

            return changed;
        }

        private bool IsQueueBrushValid() => activePaint != ColorId.None && activePaint != ColorId.Crate;

        private int PrevInColumn(List<ShooterDef> list, int index)
        {
            int col = list[index].column;
            for (int i = index - 1; i >= 0; i--) if (list[i].column == col) return i;
            return -1;
        }

        private int NextInColumn(List<ShooterDef> list, int index)
        {
            int col = list[index].column;
            for (int i = index + 1; i < list.Count; i++) if (list[i].column == col) return i;
            return -1;
        }

        private void Swap(List<ShooterDef> list, int a, int b)
        {
            var tmp = list[a]; list[a] = list[b]; list[b] = tmp;
        }

        private static Color TextColorFor(Color bg)
        {
            float lum = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;
            return lum > 0.55f ? Color.black : Color.white;
        }

        // ---- Operations ----
        private void EnsureUsableBoard()
        {
            bool invalidSize = target.boardSize.x <= 0 || target.boardSize.y <= 0;
            int expected = target.boardSize.x * target.boardSize.y;
            bool invalidPixels = target.boardPixels == null || target.boardPixels.Length != expected;

            if (invalidSize)
            {
                EditorGUILayout.HelpBox("Board boyutu 0. Başlamak için varsayılan 10×10 uygulandı — Boyut alanından değiştirebilirsin.", MessageType.Info);
                Undo.RecordObject(target, "Init Empty Level");
                target.boardSize = new Vector2Int(10, 10);
                target.boardPixels = new ColorId[100];
                EditorUtility.SetDirty(target);
            }
            else if (invalidPixels)
            {
                var next = new ColorId[expected];
                if (target.boardPixels != null)
                {
                    int copy = Mathf.Min(target.boardPixels.Length, expected);
                    for (int i = 0; i < copy; i++) next[i] = target.boardPixels[i];
                }
                Undo.RecordObject(target, "Fix Board Pixels");
                target.boardPixels = next;
                EditorUtility.SetDirty(target);
            }
        }

        private void ResizeBoard(int nw, int nh)
        {
            int ow = target.boardSize.x, oh = target.boardSize.y;
            var old = target.boardPixels;
            var next = new ColorId[nw * nh];

            if (old != null && old.Length == ow * oh)
            {
                for (int y = 0; y < Mathf.Min(oh, nh); y++)
                    for (int x = 0; x < Mathf.Min(ow, nw); x++)
                        next[y * nw + x] = old[y * ow + x];
            }

            Undo.RecordObject(target, "Resize Board");
            target.boardSize = new Vector2Int(nw, nh);
            target.boardPixels = next;
            EditorUtility.SetDirty(target);
        }

        /// <summary>
        /// Bounding box of the occupied cells. Crates count as content: if trimming
        /// left one outside, the level would lose its obstacle.
        /// </summary>
        private bool TryGetContentBounds(out int minX, out int minY, out int maxX, out int maxY)
        {
            minX = minY = int.MaxValue;
            maxX = maxY = -1;

            int w = target.boardSize.x, h = target.boardSize.y;
            var px = target.boardPixels;
            if (px == null || px.Length != w * h) return false;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (px[y * w + x] == ColorId.None) continue;

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            return maxX >= 0;
        }

        /// <summary>Shrinks the board to its occupied region. Leaves queue and palette untouched.</summary>
        private void TrimBoard()
        {
            if (!TryGetContentBounds(out int minX, out int minY, out int maxX, out int maxY))
                return;

            int w = target.boardSize.x;
            int nw = maxX - minX + 1, nh = maxY - minY + 1;
            if (nw == w && nh == target.boardSize.y) return;

            var old = target.boardPixels;
            var next = new ColorId[nw * nh];
            for (int y = 0; y < nh; y++)
                for (int x = 0; x < nw; x++)
                    next[y * nw + x] = old[(y + minY) * w + (x + minX)];

            Undo.RecordObject(target, "Trim Board");
            target.boardSize = new Vector2Int(nw, nh);
            target.boardPixels = next;
            EditorUtility.SetDirty(target);
        }

        private void InitBoardPixels()
        {
            Undo.RecordObject(target, "Init Board");
            target.boardPixels = new ColorId[target.boardSize.x * target.boardSize.y];
            EditorUtility.SetDirty(target);
        }

        private void CreateNewLevel()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Yeni Level", "Level_New", "asset", "Yeni LevelData asset'i oluştur");
            if (string.IsNullOrEmpty(path)) return;

            var level = ScriptableObject.CreateInstance<LevelData>();
            level.boardSize = new Vector2Int(10, 10);
            level.boardPixels = new ColorId[100];
            level.palette = new[] { ColorId.Red, ColorId.Blue };
            level.queue = new ShooterDef[0];
            level.columnCount = 4;
            level.trackCapacity = 5;
            level.parkCapacity = 5;

            AssetDatabase.CreateAsset(level, path);
            AssetDatabase.SaveAssets();
            target = level;
            selectedQueueIndex = -1;
            Selection.activeObject = level;
        }

        private void Save()
        {
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Level Designer] '{target.name}' kaydedildi.");
        }
    }
}