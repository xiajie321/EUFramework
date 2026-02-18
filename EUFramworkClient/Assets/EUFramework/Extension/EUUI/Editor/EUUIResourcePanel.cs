#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using EUFramework.Extension.EUUI.Editor.Templates;

namespace EUFramework.Extension.EUUI.Editor
{
    internal class EUUIResourcePanel : IEUUIPanel
    {
        private Vector2 _scrollPos;

        public void Build(VisualElement contentArea)
        {
            contentArea.Clear();
            contentArea.style.alignItems     = Align.Stretch;
            contentArea.style.justifyContent = Justify.FlexStart;

            contentArea.Add(EUUIEditorWindowHelper.CreateContentHeader(
                "EUUI 资源制作", "UI 场景、节点绑定、Prefab 导出流程"));

            var tabBar    = EUUIEditorWindowHelper.CreateTabBar();
            var tabScene  = EUUIEditorWindowHelper.CreateTabButton("创建场景",   true);
            var tabLocate = EUUIEditorWindowHelper.CreateTabButton("定位 UIRoot", false);
            var tabBind   = EUUIEditorWindowHelper.CreateTabButton("节点绑定",   false);
            var tabExport = EUUIEditorWindowHelper.CreateTabButton("导出 Prefab", false);
            var tabAuto   = EUUIEditorWindowHelper.CreateTabButton("自动流程",   false);
            tabBar.Add(tabScene);
            tabBar.Add(tabLocate);
            tabBar.Add(tabBind);
            tabBar.Add(tabExport);
            tabBar.Add(tabAuto);
            contentArea.Add(tabBar);

            var tabContent = EUUIEditorWindowHelper.CreateTabContentContainer();
            contentArea.Add(tabContent);

            ShowSceneTab(tabContent);

            tabScene.clicked  += () => { EUUIEditorWindowHelper.SetActiveTab(tabScene,  tabLocate, tabBind, tabExport, tabAuto); ShowSceneTab(tabContent);  };
            tabLocate.clicked += () => { EUUIEditorWindowHelper.SetActiveTab(tabLocate, tabScene,  tabBind, tabExport, tabAuto); ShowLocateTab(tabContent); };
            tabBind.clicked   += () => { EUUIEditorWindowHelper.SetActiveTab(tabBind,   tabScene,  tabLocate, tabExport, tabAuto); ShowBindTab(tabContent);   };
            tabExport.clicked += () => { EUUIEditorWindowHelper.SetActiveTab(tabExport, tabScene,  tabLocate, tabBind, tabAuto);  ShowExportTab(tabContent); };
            tabAuto.clicked   += () => { EUUIEditorWindowHelper.SetActiveTab(tabAuto,   tabScene,  tabLocate, tabBind, tabExport); ShowAutoTab(tabContent);  };
        }

        // ── Tab 内容 ─────────────────────────────────────────────────────────────

        private void ShowSceneTab(VisualElement container)
        {
            container.Clear();
            SetupIMGUIContainer(container, () =>
            {
                GUILayout.Space(10);
                GUILayout.Label("场景将保存到配置中的 uiSceneSavePath（或默认 Excluded/CreateUIScenes）。", EditorStyles.wordWrappedLabel);
                GUILayout.Space(5);
                GUILayout.Label("层级结构：Excluded_Bottom、UIRoot、Excluded_Top。", EditorStyles.wordWrappedLabel);
                GUILayout.Space(20);
                if (GUILayout.Button("创建 UI 场景", GUILayout.Height(36), GUILayout.ExpandWidth(true)))
                    EUUISceneEditor.ShowCreateSceneWindow();
            });
        }

        private void ShowLocateTab(VisualElement container)
        {
            container.Clear();
            SetupIMGUIContainer(container, () =>
            {
                GUILayout.Space(10);
                GUILayout.Label("快速定位当前场景中的 UIRoot 节点。", EditorStyles.wordWrappedLabel);
                GUILayout.Space(20);
                if (GUILayout.Button("定位 UIRoot", GUILayout.Height(36), GUILayout.ExpandWidth(true)))
                    EUUISceneEditor.LocateUIRoot();
            });
        }

        private void ShowBindTab(VisualElement container)
        {
            container.Clear();
            SetupIMGUIContainer(container, () =>
            {
                GUILayout.Space(10);
                GUILayout.Label("为当前选中的节点添加 EUUINodeBind 组件，用于代码生成和字段绑定。", EditorStyles.wordWrappedLabel);
                GUILayout.Space(20);
                if (GUILayout.Button("为选中节点添加 NodeBind", GUILayout.Height(36), GUILayout.ExpandWidth(true)))
                    EUUINodeBindEditor.AddBindComponent();
            });
        }

        private void ShowExportTab(VisualElement container)
        {
            container.Clear();
            SetupIMGUIContainer(container, () =>
            {
                GUILayout.Space(10);
                GUILayout.Label("此功能仅导出 Prefab，不进行代码生成和字段绑定。\n适用于已有代码的情况。", EditorStyles.wordWrappedLabel);
                GUILayout.Space(20);
                if (GUILayout.Button("导出 Prefab", GUILayout.Height(36), GUILayout.ExpandWidth(true)))
                    EUUIPanelExporter.ExportCurrentPanelToPrefab();
            });
        }

        private void ShowAutoTab(VisualElement container)
        {
            container.Clear();
            SetupIMGUIContainer(container, () =>
            {
                GUILayout.Space(10);
                GUILayout.Label("此功能将自动执行完整的 UI 导出流程：", EditorStyles.wordWrappedLabel);
                GUILayout.Space(5);
                GUILayout.Label("1. 校验场景节点（检查命名冲突、NodeBind 完整性）\n2. 生成绑定代码（.Generated.cs + 业务逻辑 .cs）\n3. 等待 Unity 编译完成\n4. 自动绑定字段到 Prefab\n5. 导出 Prefab 到配置路径", EditorStyles.helpBox);
                GUILayout.Space(15);
                GUILayout.Label("⚠ 重要提示：", EditorStyles.boldLabel);
                GUILayout.Label("• 确保当前场景包含 EUUIPanelDescription 组件\n• 确保 UIRoot 节点下的所有需要绑定的节点已添加 EUUINodeBind\n• 导出过程中会触发脚本重新编译", EditorStyles.wordWrappedLabel);
                GUILayout.Space(20);
                if (GUILayout.Button("开始自动绑定并导出", GUILayout.Height(40), GUILayout.ExpandWidth(true)))
                    EUUIPanelExporter.StartExportProcess();
            });
        }

        // ── 辅助 ──────────────────────────────────────────────────────────────────

        private void SetupIMGUIContainer(VisualElement container, System.Action drawContent)
        {
            container.style.paddingLeft  = 20;
            container.style.paddingRight = 20;
            container.style.paddingTop   = 10;
            container.style.alignSelf    = Align.Stretch;

            var imgui = new IMGUIContainer(() =>
            {
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                drawContent();
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            });

            imgui.style.flexGrow  = 1;
            imgui.style.alignSelf = Align.Stretch;
            container.Add(imgui);
        }
    }
}
#endif
