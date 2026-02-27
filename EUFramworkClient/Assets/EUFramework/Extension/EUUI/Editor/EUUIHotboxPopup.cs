#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// Scene 视图 Hotbox 弹出层。
    /// 在 Scene 视图按下 Space 键显示，再次按下隐藏。
    /// 内容由 EUHotboxConfigSO 配置，通过功能编排面板管理。
    /// </summary>
    [InitializeOnLoad]
    public static class EUUIHotboxPopup
    {
        // ── 状态 ──────────────────────────────────────────────────────────────

        private static bool              _isShowing;
        private static Rect              _hotboxRect;
        private static EUHotboxConfigSO  _config;
        private static Texture2D         _bgTexture;

        // ── 尺寸常量 ──────────────────────────────────────────────────────────

        private const float BoxWidth    = 340f;
        private const float TitleHeight = 30f;
        private const float ZoneHeader  = 22f;
        private const float ButtonGap   = 3f;
        private const float ZoneGap     = 12f;
        private const float FooterH     = 22f;
        private const float PaddingV    = 10f;

        // 两列按钮的固定宽度：(总宽 - 左右边距 - 列间距) / 2
        private const float PadH       = 8f;    // 左右各 8px
        private const float ColGap     = 6f;    // 两列之间 6px
        private static float ButtonW => (BoxWidth - PadH * 2f - ColGap) / 2f;  // ≈ 155px

        // 按钮高度根据当前区域行数动态决定：行数越少，按钮越大
        private static float GetButtonHeight(int entryCount)
        {
            int rows = Mathf.CeilToInt(entryCount / 2f);
            return rows switch
            {
                1    => 52f,   // 1 行（1-2 个）：大按钮
                2    => 40f,   // 2 行（3-4 个）：中按钮
                3    => 32f,   // 3 行（5-6 个）：标准
                _    => 26f    // 4 行及以上    ：紧凑
            };
        }

        // ── 颜色 ──────────────────────────────────────────────────────────────

        private static readonly Color BgColor      = new Color(0.11f, 0.11f, 0.12f, 0.97f);
        private static readonly Color BorderColor  = new Color(0.28f, 0.55f, 0.88f, 0.85f);
        private static readonly Color TitleColor   = new Color(0.42f, 0.76f, 1.00f);
        private static readonly Color HeaderColor  = new Color(0.65f, 0.65f, 0.65f);
        private static readonly Color FooterColor  = new Color(0.40f, 0.40f, 0.40f);

        // ── 注册 ──────────────────────────────────────────────────────────────

        static EUUIHotboxPopup()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        // ── Scene GUI ─────────────────────────────────────────────────────────

        private static void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;

            // Space 按下：显示
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Space && !_isShowing)
            {
                _config = EUHotboxEntryScanner.GetOrCreateConfig();

                // 没有配置或没有区域则不弹出
                if (_config == null || _config.zones.Count == 0) return;

                bool hasAnyEntry = false;
                foreach (var z in _config.zones)
                    if (z.entries.Count > 0) { hasAnyEntry = true; break; }
                if (!hasAnyEntry) return;

                _isShowing = true;

                float height = CalculateHeight(_config);
                float mx     = e.mousePosition.x;
                float my     = e.mousePosition.y;
                float x = Mathf.Clamp(mx - BoxWidth  / 2f, 6f, sceneView.position.width  - BoxWidth  - 6f);
                float y = Mathf.Clamp(my - height    / 2f, 6f, sceneView.position.height - height    - 6f);
                _hotboxRect = new Rect(x, y, BoxWidth, height);

                e.Use();
                sceneView.Repaint();
                return;
            }

            // Space 再次按下：隐藏
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Space && _isShowing)
            {
                _isShowing = false;
                e.Use();
                sceneView.Repaint();
                return;
            }

            if (!_isShowing) return;

            // 持续重绘
            sceneView.Repaint();

            Handles.BeginGUI();
            DrawHotbox();
            Handles.EndGUI();
        }

        // ── 高度计算 ──────────────────────────────────────────────────────────

        private static float CalculateHeight(EUHotboxConfigSO config)
        {
            float h = PaddingV + TitleHeight + 6f;

            foreach (var zone in config.zones)
            {
                if (zone.entries.Count == 0) continue;
                float bh   = GetButtonHeight(zone.entries.Count);
                int   rows = Mathf.CeilToInt(zone.entries.Count / 2f);
                h += ZoneHeader;
                h += rows * (bh + ButtonGap);
                h += ZoneGap;
            }

            h += FooterH + PaddingV;
            return Mathf.Max(h, 80f);
        }

        // ── 绘制 Hotbox ───────────────────────────────────────────────────────

        private static void DrawHotbox()
        {
            DrawBackground();

            GUILayout.BeginArea(_hotboxRect);
            GUILayout.BeginVertical();

            GUILayout.Space(PaddingV);

            // 标题
            DrawCenteredLabel("EUUI Hot Box", TitleColor, 13, FontStyle.Bold);
            GUILayout.Space(6f);

            // 区域
            foreach (var zone in _config.zones)
            {
                if (zone.entries.Count == 0) continue;

                float bh    = GetButtonHeight(zone.entries.Count);
                int   count = zone.entries.Count;

                // 区域名
                DrawCenteredLabel(zone.zoneName, HeaderColor, 11, FontStyle.Bold);
                GUILayout.Space(3f);

                // 2 列按钮网格，宽度固定等分，高度由区域条目数决定
                float bw = ButtonW;
                for (int i = 0; i < count; i += 2)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(PadH);

                    DrawEntryButton(zone.entries[i], bh, bw);

                    GUILayout.Space(ColGap);

                    if (i + 1 < count)
                        DrawEntryButton(zone.entries[i + 1], bh, bw);
                    else
                        GUILayout.Box(GUIContent.none,
                            GUIStyle.none,
                            GUILayout.Width(bw),
                            GUILayout.Height(bh));   // 空槽：占位保持对称

                    GUILayout.Space(PadH);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(ButtonGap);
                }

                GUILayout.Space(ZoneGap - ButtonGap);
            }

            GUILayout.FlexibleSpace();

            DrawCenteredLabel("再按 Space 关闭", FooterColor, 10, FontStyle.Italic);
            GUILayout.Space(PaddingV);

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private static void DrawEntryButton(HotboxZoneEntry entry, float buttonHeight, float buttonWidth)
        {
            var    discovered = EUHotboxEntryScanner.FindById(entry.entryId);
            string label      = !string.IsNullOrEmpty(entry.labelOverride) ? entry.labelOverride
                              : discovered != null ? discovered.Label
                              : "?";

            int fontSize = buttonHeight >= 48f ? 13
                         : buttonHeight >= 36f ? 12
                         : 11;

            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize  = fontSize,
                fontStyle = FontStyle.Normal,
                padding   = new RectOffset(4, 4, 4, 4),
                wordWrap  = true,
                alignment = TextAnchor.MiddleCenter
            };

            if (GUILayout.Button(
                new GUIContent(label, discovered?.Tooltip ?? ""),
                style,
                GUILayout.Width(buttonWidth),
                GUILayout.Height(buttonHeight)))
            {
                _isShowing = false;
                if (discovered != null)
                {
                    try   { discovered.Invoke(); }
                    catch (Exception ex)
                    { Debug.LogError($"[EUHotbox] 执行 {entry.entryId} 失败: {ex.Message}"); }
                }
            }
        }

        // ── 绘制辅助 ──────────────────────────────────────────────────────────

        private static void DrawBackground()
        {
            if (_bgTexture == null)
            {
                _bgTexture = new Texture2D(1, 1);
                _bgTexture.SetPixel(0, 0, BgColor);
                _bgTexture.Apply();
            }

            GUI.DrawTexture(_hotboxRect, _bgTexture);
            DrawBorder(_hotboxRect, BorderColor, 2);
        }

        private static void DrawBorder(Rect r, Color c, int t)
        {
            EditorGUI.DrawRect(new Rect(r.x,            r.y,              r.width, t), c);
            EditorGUI.DrawRect(new Rect(r.x,            r.yMax - t,       r.width, t), c);
            EditorGUI.DrawRect(new Rect(r.x,            r.y,              t, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - t,     r.y,              t, r.height), c);
        }

        private static void DrawCenteredLabel(string text, Color color, int fontSize, FontStyle fontStyle)
        {
            var style = new GUIStyle(EditorStyles.label)
            {
                alignment  = TextAnchor.MiddleCenter,
                fontSize   = fontSize,
                fontStyle  = fontStyle,
                normal     = { textColor = color }
            };
            GUILayout.Label(text, style);
        }
    }
}
#endif
