#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using EUFramework.Extension.EUUI;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// 创建 Area 设计参考框的输入窗口。
    /// </summary>
    public class EUUIAreaCreateWindow : EditorWindow
    {
        private static readonly string[] PlayerCountOptions = { "2 人", "3 人", "4 人" };
        private static readonly string[] LayoutOptions = { "Linear X（左右等分）", "Linear Y（上下等分）", "Grid 2×2" };

        private int _playerCountIndex = 0;
        private int _layoutIndex = 0;

        private System.Action<int, MultiplayerLayoutMode, MultiplayerLayoutAxis> _onConfirm;

        public static void ShowWindow(
            System.Action<int, MultiplayerLayoutMode, MultiplayerLayoutAxis> onConfirm)
        {
            var window = GetWindow<EUUIAreaCreateWindow>(true, "创建 Area", true);
            window._onConfirm = onConfirm;
            window.minSize = new Vector2(380, 180);
            window.CenterOnMainWin();
            window.Focus();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("分屏布局设置", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _playerCountIndex = EditorGUILayout.Popup("玩家总数", _playerCountIndex, PlayerCountOptions);
            _layoutIndex = EditorGUILayout.Popup("布局方式", _layoutIndex, LayoutOptions);

            int playerCount = _playerCountIndex + 2;
            bool isGrid = _layoutIndex == 2;
            var layout = isGrid ? MultiplayerLayoutMode.Grid : MultiplayerLayoutMode.Linear;
            var axis = _layoutIndex == 1 ? MultiplayerLayoutAxis.Y : MultiplayerLayoutAxis.X;

            EditorGUILayout.Space(8);

            var config = AssetDatabase.LoadAssetAtPath<EUUIEditorConfig>(EUUISceneEditor.GetEditorConfigPath());
            if (config != null)
            {
                var rect = EUUIKit.GetSlotRect(0, isGrid ? 4 : playerCount, layout, axis);
                float w = rect.width * config.referenceResolution.x;
                float h = rect.height * config.referenceResolution.y;
                EditorGUILayout.HelpBox(
                    $"玩家区域参考尺寸：{w} × {h} px\n" +
                    $"（基于 EUUIEditorConfig 参考分辨率 {config.referenceResolution.x}×{config.referenceResolution.y}）\n" +
                    "Area 将以全拉伸方式填满 UIRoot，referenceResolution 不会被修改。",
                    MessageType.Info);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("创建 / 更新 Area", GUILayout.Height(36)))
            {
                _onConfirm?.Invoke(isGrid ? 4 : playerCount, layout, axis);
                Close();
            }
            EditorGUILayout.Space(10);
        }
    }
}
#endif
