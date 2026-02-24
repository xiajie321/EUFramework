#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// 功能编排面板：左侧展示所有发现的 [EUHotboxEntry] 条目，
    /// 右侧管理区域（新增/改名/删除），拖拽左侧条目到区域以完成编排。
    /// 配置保存至 EUHotboxConfigSO，供 Space 键弹出层读取渲染。
    /// </summary>
    internal class EUUIOrchestrationPanel : IEUUIPanel
    {
        // ── 状态 ──────────────────────────────────────────────────────────────

        private EUHotboxConfigSO _config;
        private Vector2          _leftScroll;
        private Vector2          _rightScroll;
        private string           _searchText         = "";
        private int              _renamingZoneIndex  = -1;
        private string           _renameBuffer       = "";

        private readonly Dictionary<string, bool> _groupFoldouts =
            new Dictionary<string, bool>();

        // 左侧条目列表 → 区域（新增）
        private const string k_DragKey      = "EUHotboxEntryId";
        private const string k_DragKeyLabel = "EUHotboxEntryLabel";

        // 区域内条目 → 其他区域（移动/交换）
        private const string k_ZoneDragKey      = "EUHotboxZoneEntryId";
        private const string k_ZoneDragLabel    = "EUHotboxZoneEntryLabel";
        private const string k_ZoneDragZoneIdx  = "EUHotboxZoneEntryZoneIdx";
        private const string k_ZoneDragEntryIdx = "EUHotboxZoneEntryEntryIdx";

        // ── 延迟拖放操作（避免绘制循环中直接改索引）──────────────────────────
        //    _pdSrcZone == -1 → 来自左侧列表（新增），否则为区域内移动/交换
        private int    _pdSrcZone  = -1;
        private int    _pdSrcEntry = -1;
        private int    _pdDstZone  = -1;   // -1 = 无待操作
        private int    _pdDstEntry = -1;   // -1 = 追加到末尾，>=0 = 与该位置交换/插入前
        private string _pdNewId    = null; // 来自左侧列表时非 null

        private bool HasPendingDrop => _pdDstZone >= 0;

        private void ResetPendingDrop()
        {
            _pdSrcZone = _pdSrcEntry = _pdDstZone = _pdDstEntry = -1;
            _pdNewId = null;
        }

        private void ApplyPendingDrop()
        {
            if (!HasPendingDrop) return;

            // A：从左侧列表拖入（新增，可指定插入位置）
            if (_pdNewId != null)
            {
                var dstZone = _config.zones[_pdDstZone];
                if (!dstZone.entries.Exists(e => e.entryId == _pdNewId))
                {
                    Undo.RecordObject(_config, "EUHotbox Add Entry");
                    var newEntry = new HotboxZoneEntry { entryId = _pdNewId };
                    if (_pdDstEntry >= 0 && _pdDstEntry < dstZone.entries.Count)
                        dstZone.entries.Insert(_pdDstEntry, newEntry);
                    else
                        dstZone.entries.Add(newEntry);
                    EditorUtility.SetDirty(_config);
                    AssetDatabase.SaveAssets();
                }
                return;
            }

            // B：区域内/跨区域移动或交换
            if (_pdSrcZone < 0 || _pdSrcZone >= _config.zones.Count) return;
            var srcZone = _config.zones[_pdSrcZone];
            var tgtZone = _config.zones[_pdDstZone];
            if (_pdSrcEntry < 0 || _pdSrcEntry >= srcZone.entries.Count) return;

            Undo.RecordObject(_config, "EUHotbox Reorder Entry");
            var moving = srcZone.entries[_pdSrcEntry];

            if (_pdDstEntry < 0)
            {
                // 追加到目标区域末尾
                srcZone.entries.RemoveAt(_pdSrcEntry);
                tgtZone.entries.Add(moving);
            }
            else if (_pdSrcZone == _pdDstZone)
            {
                // 同区域：交换两个条目位置
                int dstIdx = Mathf.Clamp(_pdDstEntry, 0, srcZone.entries.Count - 1);
                srcZone.entries[_pdSrcEntry] = srcZone.entries[dstIdx];
                srcZone.entries[dstIdx]      = moving;
            }
            else
            {
                // 跨区域：移动到目标位置
                srcZone.entries.RemoveAt(_pdSrcEntry);
                int insertIdx = Mathf.Clamp(_pdDstEntry, 0, tgtZone.entries.Count);
                tgtZone.entries.Insert(insertIdx, moving);
            }

            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
        }

        // ── Build ─────────────────────────────────────────────────────────────

        public void Build(VisualElement contentArea)
        {
            contentArea.Clear();
            contentArea.style.alignItems     = Align.Stretch;
            contentArea.style.justifyContent = Justify.FlexStart;

            contentArea.Add(EUUIEditorWindowHelper.CreateContentHeader(
                "功能编排",
                "拖拽条目到区域，按 Space 键在 Scene 视图弹出快捷面板"));

            _config = EUHotboxEntryScanner.GetOrCreateConfig();

            var imgui = new IMGUIContainer(DrawContent);
            imgui.style.flexGrow  = 1;
            imgui.style.alignSelf = Align.Stretch;
            contentArea.Add(imgui);
        }

        // ── 主绘制 ────────────────────────────────────────────────────────────

        private void DrawContent()
        {
            if (_config == null)
            {
                EditorGUILayout.HelpBox(
                    "功能编排配置（EUHotboxConfigSO）未找到，请点击下方按钮创建。",
                    MessageType.Error);
                if (GUILayout.Button("创建配置文件", GUILayout.Height(32)))
                    _config = EUHotboxEntryScanner.GetOrCreateConfig();
                return;
            }

            // 在绘制开始前应用上一帧记录的拖放操作（避免绘制中改索引）
            if (HasPendingDrop)
            {
                ApplyPendingDrop();
                ResetPendingDrop();
            }

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            DrawLeftPanel();

            // 竖向分割线
            Rect divRect = GUILayoutUtility.GetRect(2f, 2f, GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(divRect, new Color(0.13f, 0.13f, 0.13f));

            DrawRightPanel();

            EditorGUILayout.EndHorizontal();
        }

        // ── 左侧面板：条目列表 ────────────────────────────────────────────────

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(264), GUILayout.ExpandHeight(true));

            GUILayout.Space(10);
            GUILayout.Label("条目列表", EditorStyles.boldLabel);
            GUILayout.Space(4);

            // 搜索栏
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _searchText = GUILayout.TextField(
                _searchText, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
            if (!string.IsNullOrEmpty(_searchText)
                && GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(22)))
                _searchText = "";
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, GUILayout.ExpandHeight(true));

            var allEntries = EUHotboxEntryScanner.AllEntries;
            IEnumerable<EUHotboxEntryScanner.DiscoveredEntry> filtered = allEntries;
            if (!string.IsNullOrEmpty(_searchText))
            {
                string kw = _searchText;
                filtered = allEntries.Where(e =>
                    e.Label.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    e.Group.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            var groups = filtered.GroupBy(e => e.Group).OrderBy(g => g.Key).ToList();

            if (groups.Count == 0)
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox(
                    "未发现任何 [EUHotboxEntry] 条目。\n" +
                    "请为编辑器静态方法添加 [EUHotboxEntry(\"名称\", \"分组\")] 特性。",
                    MessageType.Info);
            }
            else
            {
                foreach (var group in groups)
                {
                    if (!_groupFoldouts.TryGetValue(group.Key, out bool expanded))
                    {
                        _groupFoldouts[group.Key] = true;
                        expanded = true;
                    }

                    bool newExp = EditorGUILayout.Foldout(
                        expanded, group.Key, true, EditorStyles.foldoutHeader);
                    if (newExp != expanded)
                        _groupFoldouts[group.Key] = newExp;

                    if (newExp)
                    {
                        foreach (var entry in group)
                            DrawDraggableEntry(entry);
                    }

                    GUILayout.Space(3);
                }
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新条目", GUILayout.Height(24)))
                EUHotboxEntryScanner.Refresh();
            if (GUILayout.Button("定位配置 SO", GUILayout.Height(24)))
            {
                Selection.activeObject = _config;
                EditorGUIUtility.PingObject(_config);
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8);

            EditorGUILayout.EndVertical();
        }

        private static void DrawDraggableEntry(EUHotboxEntryScanner.DiscoveredEntry entry)
        {
            Rect rowRect = EditorGUILayout.GetControlRect(false, 26);
            rowRect.x     += 8;
            rowRect.width -= 8;

            bool isHover = rowRect.Contains(Event.current.mousePosition);
            EditorGUI.DrawRect(rowRect,
                isHover
                    ? new Color(0.27f, 0.40f, 0.60f, 0.55f)
                    : new Color(0.21f, 0.21f, 0.21f, 0.35f));

            // 拖拽图标
            GUI.Label(
                new Rect(rowRect.x + 4, rowRect.y + 7, 12, 12),
                "☰",
                new GUIStyle(EditorStyles.miniLabel) { fontSize = 9, normal = { textColor = new Color(0.6f, 0.6f, 0.6f) } });

            // 标签
            GUI.Label(
                new Rect(rowRect.x + 20, rowRect.y + 5, rowRect.width - 24, rowRect.height - 5),
                new GUIContent(entry.Label, entry.Tooltip),
                EditorStyles.label);

            // 启动拖拽
            if (Event.current.type == EventType.MouseDown
                && rowRect.Contains(Event.current.mousePosition))
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.SetGenericData(k_DragKey,      entry.EntryId);
                DragAndDrop.SetGenericData(k_DragKeyLabel, entry.Label);
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                DragAndDrop.StartDrag(entry.Label);
                Event.current.Use();
            }
        }

        // ── 右侧面板：编排区域 ────────────────────────────────────────────────

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            GUILayout.Space(10);

            // 顶部工具栏
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("编排区域", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("+ 新增区域", GUILayout.Width(88), GUILayout.Height(22)))
            {
                Undo.RecordObject(_config, "EUHotbox Add Zone");
                _config.zones.Add(new HotboxZone { zoneName = "新区域" });
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
            }
            GUILayout.Space(4);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);

            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll, GUILayout.ExpandHeight(true));

            if (_config.zones.Count == 0)
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox(
                    "点击「+ 新增区域」创建区域，然后从左侧拖入功能条目。\n\n" +
                    "配置完成后，在 Scene 视图中按住 Space 键即可弹出快捷面板。",
                    MessageType.Info);
            }
            else
            {
                int toDeleteZone  = -1;
                for (int zi = 0; zi < _config.zones.Count; zi++)
                {
                    bool wantsDelete = DrawZone(zi);
                    if (wantsDelete) toDeleteZone = zi;
                    GUILayout.Space(8);
                }
                if (toDeleteZone >= 0)
                {
                    Undo.RecordObject(_config, "EUHotbox Remove Zone");
                    _config.zones.RemoveAt(toDeleteZone);
                    EditorUtility.SetDirty(_config);
                    AssetDatabase.SaveAssets();
                }
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(6);
            EditorGUILayout.EndVertical();
        }

        /// <summary>绘制单个区域，返回 true 表示需要删除</summary>
        private bool DrawZone(int zoneIndex)
        {
            var zone = _config.zones[zoneIndex];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // ── 区域标题栏 ─────────────────────────────────────────────────
            EditorGUILayout.BeginHorizontal();

            if (_renamingZoneIndex == zoneIndex)
            {
                _renameBuffer = EditorGUILayout.TextField(_renameBuffer, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("✓", GUILayout.Width(24)))
                {
                    Undo.RecordObject(_config, "EUHotbox Rename Zone");
                    zone.zoneName     = _renameBuffer;
                    _renamingZoneIndex = -1;
                    EditorUtility.SetDirty(_config);
                    AssetDatabase.SaveAssets();
                }
                if (GUILayout.Button("✗", GUILayout.Width(24)))
                    _renamingZoneIndex = -1;
            }
            else
            {
                GUILayout.Label(zone.zoneName, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

                if (GUILayout.Button("改名", GUILayout.Width(40), GUILayout.Height(20)))
                {
                    _renamingZoneIndex = zoneIndex;
                    _renameBuffer      = zone.zoneName;
                }

                var prevBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.75f, 0.28f, 0.28f);
                bool wantsDelete = GUILayout.Button("×", GUILayout.Width(22), GUILayout.Height(20));
                GUI.backgroundColor = prevBg;

                if (wantsDelete)
                {
                    if (EditorUtility.DisplayDialog(
                            "确认删除",
                            $"删除区域「{zone.zoneName}」？\n其中的条目配置将一并移除。",
                            "删除", "取消"))
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        return true;
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // ── 条目 2 列网格 ──────────────────────────────────────────────
            int toRemove = -1;
            int count    = zone.entries.Count;

            for (int i = 0; i < count; i += 2)
            {
                EditorGUILayout.BeginHorizontal();
                if (DrawZoneEntry(zone.entries[i], i, zoneIndex)) toRemove = i;

                if (i + 1 < count)
                {
                    if (DrawZoneEntry(zone.entries[i + 1], i + 1, zoneIndex)) toRemove = i + 1;
                }
                else
                {
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2);
            }

            if (toRemove >= 0)
            {
                Undo.RecordObject(_config, "EUHotbox Remove Entry");
                zone.entries.RemoveAt(toRemove);
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
            }

            GUILayout.Space(4);

            // ── 拖放区域（新增 或 移动至此区域末尾）─────────────────────────
            bool anyDragActive = DragAndDrop.GetGenericData(k_DragKey)     is string
                              || DragAndDrop.GetGenericData(k_ZoneDragKey) is string;
            var dropStyle = new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 11,
                normal    = { textColor = anyDragActive ? new Color(0.45f, 0.85f, 0.45f) : new Color(0.45f, 0.45f, 0.45f) }
            };

            string dropLabel = anyDragActive ? "→ 松开鼠标，添加到此区域" : "+ 拖入条目到此处";
            GUILayout.Box(dropLabel, dropStyle, GUILayout.Height(28), GUILayout.ExpandWidth(true));
            HandleZoneDrop(GUILayoutUtility.GetLastRect(), zone, zoneIndex);

            EditorGUILayout.EndVertical();
            return false;
        }

        /// <summary>绘制区域内单个条目格（可拖出交换，可作为放置目标），返回 true 表示需要移除</summary>
        private bool DrawZoneEntry(HotboxZoneEntry entry, int entryIndex, int zoneIndex)
        {
            var discovered = EUHotboxEntryScanner.FindById(entry.entryId);
            bool isValid   = discovered != null;

            string label   = !string.IsNullOrEmpty(entry.labelOverride) ? entry.labelOverride
                           : isValid ? discovered.Label
                           : $"[?] {entry.entryId}";
            string tooltip = discovered?.Tooltip
                           ?? (isValid ? "" : "未找到对应功能，请检查程序集是否已编译");

            var   ev         = Event.current;
            bool  anyDragging = DragAndDrop.GetGenericData(k_ZoneDragKey) is string
                             || DragAndDrop.GetGenericData(k_DragKey)     is string;

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

            // ── 可拖拽 / 可放置的标签块 ────────────────────────────────────
            Rect entryRect  = EditorGUILayout.GetControlRect(false, 26, GUILayout.ExpandWidth(true));
            bool isHover    = entryRect.Contains(ev.mousePosition);
            bool isDropTarget = anyDragging && isHover;

            // 背景色：正常 / 悬停 / 放置高亮 / 无效
            Color bgColor;
            if (isDropTarget)
                bgColor = new Color(0.25f, 0.72f, 0.35f, 0.55f);   // 绿色放置提示
            else if (!isValid)
                bgColor = new Color(0.55f, 0.20f, 0.20f, 0.75f);
            else if (isHover)
                bgColor = new Color(0.27f, 0.40f, 0.60f, 0.55f);
            else
                bgColor = new Color(0.22f, 0.22f, 0.22f, 0.85f);

            EditorGUI.DrawRect(entryRect, bgColor);

            // 放置时在顶部画一条细线作为插入指示
            if (isDropTarget)
                EditorGUI.DrawRect(
                    new Rect(entryRect.x, entryRect.y, entryRect.width, 2f),
                    new Color(0.3f, 0.9f, 0.4f));

            // 拖拽手柄图标
            GUI.Label(
                new Rect(entryRect.x + 4, entryRect.y + 7, 12, 12),
                "☰",
                new GUIStyle(EditorStyles.miniLabel)
                    { fontSize = 9, normal = { textColor = new Color(0.55f, 0.55f, 0.55f) } });

            // 条目标签
            GUI.Label(
                new Rect(entryRect.x + 18, entryRect.y + 5, entryRect.width - 20, entryRect.height - 5),
                new GUIContent(label, tooltip),
                new GUIStyle(EditorStyles.label) { fontSize = 11 });

            EditorGUIUtility.AddCursorRect(entryRect, anyDragging ? MouseCursor.ArrowPlus : MouseCursor.Pan);

            // ── 拖出：启动区域条目拖拽 ────────────────────────────────────
            if (ev.type == EventType.MouseDown && entryRect.Contains(ev.mousePosition))
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.SetGenericData(k_ZoneDragKey,      entry.entryId);
                DragAndDrop.SetGenericData(k_ZoneDragLabel,    label);
                DragAndDrop.SetGenericData(k_ZoneDragZoneIdx,  (object)zoneIndex);
                DragAndDrop.SetGenericData(k_ZoneDragEntryIdx, (object)entryIndex);
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                DragAndDrop.StartDrag(label);
                ev.Use();
            }

            // ── 放置：记录待执行操作（实际在下一帧 ApplyPendingDrop 中执行）
            if (isHover && anyDragging)
            {
                if (ev.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    ev.Use();
                }
                else if (ev.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    if (DragAndDrop.GetGenericData(k_ZoneDragKey) is string
                        && DragAndDrop.GetGenericData(k_ZoneDragZoneIdx)  is int srcZ
                        && DragAndDrop.GetGenericData(k_ZoneDragEntryIdx) is int srcE)
                    {
                        // 区域条目拖拽 → 与当前条目交换/移动
                        _pdSrcZone  = srcZ;
                        _pdSrcEntry = srcE;
                        _pdDstZone  = zoneIndex;
                        _pdDstEntry = entryIndex;
                    }
                    else if (DragAndDrop.GetGenericData(k_DragKey) is string newId)
                    {
                        // 左侧列表拖入 → 插入到当前条目位置之前
                        _pdNewId    = newId;
                        _pdDstZone  = zoneIndex;
                        _pdDstEntry = entryIndex;
                    }

                    ev.Use();
                }
            }

            // ── 移除按钮 ───────────────────────────────────────────────────
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.7f, 0.28f, 0.28f);
            bool wantsRemove = GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(26));
            GUI.backgroundColor = prevBg;

            EditorGUILayout.EndHorizontal();

            return wantsRemove;
        }

        // ── 拖放处理 ──────────────────────────────────────────────────────────

        /// <summary>底部拖放区：新增或移动到末尾（记录 pending，下帧执行）</summary>
        private void HandleZoneDrop(Rect dropRect, HotboxZone targetZone, int targetZoneIndex)
        {
            var ev = Event.current;
            if (!dropRect.Contains(ev.mousePosition)) return;

            bool isLeftDrag = DragAndDrop.GetGenericData(k_DragKey)     is string;
            bool isZoneDrag = DragAndDrop.GetGenericData(k_ZoneDragKey) is string;
            if (!isLeftDrag && !isZoneDrag) return;

            if (ev.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = isLeftDrag
                    ? DragAndDropVisualMode.Copy
                    : DragAndDropVisualMode.Move;
                ev.Use();
            }
            else if (ev.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                if (isLeftDrag && DragAndDrop.GetGenericData(k_DragKey) is string newId)
                {
                    // 左侧列表 → 追加到末尾
                    _pdNewId    = newId;
                    _pdDstZone  = targetZoneIndex;
                    _pdDstEntry = -1; // 末尾
                }
                else if (isZoneDrag
                      && DragAndDrop.GetGenericData(k_ZoneDragZoneIdx)  is int srcZ
                      && DragAndDrop.GetGenericData(k_ZoneDragEntryIdx) is int srcE)
                {
                    // 区域条目 → 移动到目标区域末尾
                    _pdSrcZone  = srcZ;
                    _pdSrcEntry = srcE;
                    _pdDstZone  = targetZoneIndex;
                    _pdDstEntry = -1; // 末尾
                }

                ev.Use();
            }
        }
    }
}
#endif
