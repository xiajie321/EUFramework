#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// EUUI 配置工具窗口
    /// 职责：窗口骨架 + 左侧导航路由，具体页面内容由各 Panel 类实现
    /// </summary>
    public class EUUIEditorWindow : EditorWindow
    {
        private Button       _selectedButton;
        private VisualElement _selectedContainer;

        private readonly EUUISOConfigPanel        _soConfigPanel        = new EUUISOConfigPanel();
        private readonly EUUIModulePanel          _modulePanel          = new EUUIModulePanel();
        private readonly EUUIExtensionPanel       _extensionPanel       = new EUUIExtensionPanel();
        private readonly EUUIOrchestrationPanel   _orchestrationPanel   = new EUUIOrchestrationPanel();

        [MenuItem("EUFramework/拓展/EUUI 配置工具", false, 101)]
        public static void ShowWindow()
        {
            var window = GetWindow<EUUIEditorWindow>();
            window.titleContent = new GUIContent("EUUI 配置工具");

            Vector2 size = new Vector2(1000, 700);
            window.minSize = size;

            var main = EditorGUIUtility.GetMainWindowPosition();
            window.position = new Rect(
                main.x + (main.width  - size.x) * 0.5f,
                main.y + (main.height - size.y) * 0.5f,
                size.x, size.y);
        }

        private void CreateGUI()
        {
            string editorUIPath = EUUIEditorWindowHelper.GetEditorUIPath();
            string uxmlPath     = Path.Combine(editorUIPath, "EUUIEditorWindow.uxml").Replace("\\", "/");
            var visualTree      = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);

            if (visualTree != null)
                visualTree.CloneTree(rootVisualElement);
            else
            {
                CreateFallbackUI();
                return;
            }

            string ussPath    = Path.Combine(editorUIPath, "EUUIEditorWindow.uss").Replace("\\", "/");
            var styleSheet    = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);

            var contentLabel = rootVisualElement.Q<Label>("content-label");
            if (contentLabel != null)
                contentLabel.style.display = DisplayStyle.None;

            BindNavigationButtons();

            var btnSOConfig = rootVisualElement.Q<Button>("btn-so-config");
            if (btnSOConfig != null)
            {
                SetSelectedButton(btnSOConfig);
                ShowPanel(_soConfigPanel);
            }
            else
            {
                Debug.LogError("[EUUI] 无法找到 btn-so-config 按钮，请检查 UXML 文件");
            }
        }

        private void BindNavigationButtons()
        {
            var btnSOConfig      = rootVisualElement.Q<Button>("btn-so-config");
            var btnModules       = rootVisualElement.Q<Button>("btn-modules");
            var btnExtensions    = rootVisualElement.Q<Button>("btn-extensions");
            var btnOrchestration = rootVisualElement.Q<Button>("btn-orchestration");

            if (btnSOConfig != null)
                btnSOConfig.clicked += () => { SetSelectedButton(btnSOConfig); ShowPanel(_soConfigPanel); };

            if (btnModules != null)
                btnModules.clicked += () => { SetSelectedButton(btnModules); ShowPanel(_modulePanel); };

            if (btnExtensions != null)
                btnExtensions.clicked += () => { SetSelectedButton(btnExtensions); ShowPanel(_extensionPanel); };

            if (btnOrchestration != null)
                btnOrchestration.clicked += () => { SetSelectedButton(btnOrchestration); ShowPanel(_orchestrationPanel); };
        }

        private void ShowPanel(IEUUIPanel panel)
        {
            var contentArea = rootVisualElement.Q<VisualElement>("content-area");
            if (contentArea == null) return;
            panel.Build(contentArea);
        }

        private void SetSelectedButton(Button button)
        {
            if (_selectedButton != null)
                _selectedButton.RemoveFromClassList("sidebar-button-selected");
            if (_selectedContainer != null)
                _selectedContainer.RemoveFromClassList("sidebar-button-container-selected");

            button.AddToClassList("sidebar-button-selected");
            _selectedButton    = button;
            _selectedContainer = button.parent;
            if (_selectedContainer != null)
                _selectedContainer.AddToClassList("sidebar-button-container-selected");
        }

        private void CreateFallbackUI()
        {
            var container = new VisualElement();
            container.style.flexGrow       = 1;
            container.style.justifyContent = Justify.Center;
            container.style.alignItems     = Align.Center;

            string editorUIPath = EUUIEditorWindowHelper.GetEditorUIPath();
            string uxmlPath     = Path.Combine(editorUIPath, "EUUIEditorWindow.uxml").Replace("\\", "/");
            var label = new Label($"UXML 未找到\n请确保存在: {uxmlPath}");
            label.style.fontSize        = 14;
            label.style.unityTextAlign  = TextAnchor.MiddleCenter;
            label.style.color           = new Color(1f, 0.5f, 0.5f);

            container.Add(label);
            rootVisualElement.Add(container);
        }
    }
}
#endif
