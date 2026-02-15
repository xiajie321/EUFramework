#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Framework;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// EUUI 配置工具窗口（与 EUResKit 配置工具同风格）
    /// </summary>
    public class EUUIEditorWindow : EditorWindow
    {
        private const string EditorUIPath = "Assets/EUFramework/Extension/EUUI/Editor/UI";
        private const string ConfigAssetPath = "Assets/EUFramework/Extension/EUUI/Editor/EUUIEditorConfig.asset";

        private static string ConfigFullPath => Path.Combine(Application.dataPath, "EUFramework/Extension/EUUI/Editor/EUUIEditorConfig.asset");

        private Button _selectedButton;
        private Vector2 _scrollPos;

        [MenuItem("EUFramework/拓展/EUUI 配置工具", false, 101)]
        public static void ShowWindow()
        {
            var window = GetWindow<EUUIEditorWindow>();
            window.titleContent = new GUIContent("EUUI 配置工具");

            Vector2 windowSize = new Vector2(700, 500);
            window.minSize = windowSize;

            var main = EditorGUIUtility.GetMainWindowPosition();
            window.position = new Rect(
                main.x + (main.width - windowSize.x) * 0.5f,
                main.y + (main.height - windowSize.y) * 0.5f,
                windowSize.x,
                windowSize.y
            );
        }

        private void CreateGUI()
        {
            string uxmlPath = Path.Combine(EditorUIPath, "EUUIEditorWindow.uxml").Replace("\\", "/");
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);

            if (visualTree != null)
            {
                visualTree.CloneTree(rootVisualElement);
            }
            else
            {
                CreateFallbackUI();
                return;
            }

            string ussPath = Path.Combine(EditorUIPath, "EUUIEditorWindow.uss").Replace("\\", "/");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            BindButtons();

            var btnConfig = rootVisualElement.Q<Button>("btn-config");
            if (btnConfig != null)
            {
                SetSelectedButton(btnConfig);
                ShowConfigPanel();
            }
        }

        private void BindButtons()
        {
            var btnConfig = rootVisualElement.Q<Button>("btn-config");
            var btnScene = rootVisualElement.Q<Button>("btn-scene");

            if (btnConfig != null)
            {
                btnConfig.clicked += () =>
                {
                    SetSelectedButton(btnConfig);
                    ShowConfigPanel();
                };
            }

            if (btnScene != null)
            {
                btnScene.clicked += () =>
                {
                    SetSelectedButton(btnScene);
                    ShowCreateScenePanel();
                };
            }
        }

        private void SetSelectedButton(Button button)
        {
            if (_selectedButton != null)
            {
                _selectedButton.RemoveFromClassList("sidebar-button-selected");
            }
            button.AddToClassList("sidebar-button-selected");
            _selectedButton = button;
        }

        private VisualElement CreateContentHeader(string title, string subtitle)
        {
            var header = new VisualElement();
            header.AddToClassList("content-header");

            var titleLabel = new Label(title);
            titleLabel.AddToClassList("content-title");
            header.Add(titleLabel);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var subtitleLabel = new Label(subtitle);
                subtitleLabel.AddToClassList("content-subtitle");
                header.Add(subtitleLabel);
            }

            return header;
        }

        private void ShowConfigPanel()
        {
            var contentArea = rootVisualElement.Q<VisualElement>("content-area");
            if (contentArea == null) return;

            contentArea.Clear();
            contentArea.style.alignItems = Align.FlexStart;
            contentArea.style.justifyContent = Justify.FlexStart;

            contentArea.Add(CreateContentHeader("配置文件", "创建或打开 EUUIEditorConfig"));

            var imgui = new IMGUIContainer(() =>
            {
                _scrollPos = GUILayout.BeginScrollView(_scrollPos);

                bool configExists = File.Exists(ConfigFullPath);

                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.Label("EUUIEditorConfig:", GUILayout.Width(200));
                GUILayout.Label(configExists ? "✓ 已创建" : "✗ 未创建", EditorStyles.boldLabel);
                GUILayout.EndHorizontal();

                GUILayout.Space(15);

                if (GUILayout.Button("创建 UI 配置", GUILayout.Height(32)))
                {
                    EUUIEditorConfigEditor.CreateConfig();
                }

                GUILayout.Space(5);

                if (GUILayout.Button("打开 UI 配置", GUILayout.Height(32)))
                {
                    EUUIEditorConfigEditor.OpenConfig();
                }

                GUILayout.EndScrollView();
            });

            imgui.style.width = Length.Percent(100);
            imgui.style.flexGrow = 1;
            contentArea.Add(imgui);
        }

        private void ShowCreateScenePanel()
        {
            var contentArea = rootVisualElement.Q<VisualElement>("content-area");
            if (contentArea == null) return;

            contentArea.Clear();
            contentArea.style.alignItems = Align.FlexStart;
            contentArea.style.justifyContent = Justify.FlexStart;

            contentArea.Add(CreateContentHeader("创建 UI 场景", "在 UISceneSavePath 下创建 UIRoot / Excluded 层级结构"));

            var imgui = new IMGUIContainer(() =>
            {
                _scrollPos = GUILayout.BeginScrollView(_scrollPos);

                GUILayout.Space(10);
                GUILayout.Label("场景将保存到配置中的 uiSceneSavePath（或默认 Excluded/CreateUIScenes）。", EditorStyles.wordWrappedLabel);
                GUILayout.Space(5);
                GUILayout.Label("层级结构：Excluded_Bottom、UIRoot、Excluded_Top。", EditorStyles.wordWrappedLabel);

                GUILayout.Space(20);

                if (GUILayout.Button("创建 UI 场景", GUILayout.Height(36)))
                {
                    EUUISceneEditor.ShowCreateSceneWindow();
                }

                GUILayout.EndScrollView();
            });

            imgui.style.width = Length.Percent(100);
            imgui.style.flexGrow = 1;
            contentArea.Add(imgui);
        }

        private void CreateFallbackUI()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;
            container.style.justifyContent = Justify.Center;
            container.style.alignItems = Align.Center;

            string uxmlPath = Path.Combine(EditorUIPath, "EUUIEditorWindow.uxml").Replace("\\", "/");
            var label = new Label($"UXML 未找到\n请确保存在: {uxmlPath}");
            label.style.fontSize = 14;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.color = new Color(1f, 0.5f, 0.5f);

            container.Add(label);
            rootVisualElement.Add(container);
        }
    }
}
#endif
