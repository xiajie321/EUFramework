#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using EUFramework.Extension.EUUI;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// EUUI 配置工具窗口（与 EUResKit 配置工具同风格）
    /// </summary>
    public class EUUIEditorWindow : EditorWindow
    {
        private Button _selectedButton;
        private Vector2 _scrollPos;

        /// <summary>
        /// 动态解析 Editor/UI 目录路径（基于当前脚本位置）
        /// </summary>
        private static string GetEditorUIPath()
        {
            var scriptGuids = AssetDatabase.FindAssets("EUUIEditorWindow t:MonoScript");
            if (scriptGuids != null && scriptGuids.Length > 0)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuids[0]);
                string scriptDir = Path.GetDirectoryName(scriptPath).Replace("\\", "/");
                return Path.Combine(scriptDir, "UI").Replace("\\", "/");
            }
            return "Assets/EUFramework/Extension/EUUI/Editor/UI"; // 兜底路径
        }

        /// <summary>
        /// 动态查找 EUUIEditorConfig 资源路径
        /// </summary>
        private static string GetConfigAssetPath()
        {
            string[] guids = AssetDatabase.FindAssets("EUUIEditorConfig t:EUUIEditorConfig");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path != null && path.EndsWith("EUUIEditorConfig.asset", StringComparison.OrdinalIgnoreCase))
                    return path;
            }
            return "Assets/EUFramework/Extension/EUUI/Editor/EUUIEditorConfig.asset"; // 兜底路径
        }

        private static string ConfigFullPath => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), GetConfigAssetPath()));

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
            string editorUIPath = GetEditorUIPath();
            string uxmlPath = Path.Combine(editorUIPath, "EUUIEditorWindow.uxml").Replace("\\", "/");
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

            string ussPath = Path.Combine(editorUIPath, "EUUIEditorWindow.uss").Replace("\\", "/");
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
            var btnLocate = rootVisualElement.Q<Button>("btn-locate");
            var btnBind = rootVisualElement.Q<Button>("btn-bind");
            var btnExport = rootVisualElement.Q<Button>("btn-export");
            var btnAuto = rootVisualElement.Q<Button>("btn-auto");
            var btnExtensions = rootVisualElement.Q<Button>("btn-extensions");

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

            if (btnLocate != null)
            {
                btnLocate.clicked += () =>
                {
                    SetSelectedButton(btnLocate);
                    ShowLocatePanel();
                };
            }

            if (btnBind != null)
            {
                btnBind.clicked += () =>
                {
                    SetSelectedButton(btnBind);
                    ShowBindPanel();
                };
            }

            if (btnExport != null)
            {
                btnExport.clicked += () =>
                {
                    SetSelectedButton(btnExport);
                    ShowExportPanel();
                };
            }

            if (btnAuto != null)
            {
                btnAuto.clicked += () =>
                {
                    SetSelectedButton(btnAuto);
                    ShowAutoPanel();
                };
            }

            if (btnExtensions != null)
            {
                btnExtensions.clicked += () =>
                {
                    SetSelectedButton(btnExtensions);
                    ShowExtensionsPanel();
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

        private void ShowLocatePanel()
        {
            var contentArea = rootVisualElement.Q<VisualElement>("content-area");
            if (contentArea == null) return;

            contentArea.Clear();
            contentArea.style.alignItems = Align.FlexStart;
            contentArea.style.justifyContent = Justify.FlexStart;

            contentArea.Add(CreateContentHeader("定位 UIRoot", "快速定位当前场景中的 UIRoot 节点"));

            var imgui = new IMGUIContainer(() =>
            {
                _scrollPos = GUILayout.BeginScrollView(_scrollPos);

                GUILayout.Space(10);
                GUILayout.Label("点击按钮将自动定位并展开 Hierarchy 中的 UIRoot 节点。", EditorStyles.wordWrappedLabel);
                GUILayout.Space(5);
                GUILayout.Label("提示：当前场景必须包含 EUUIPanelDescription 组件。", EditorStyles.helpBox);

                GUILayout.Space(20);

                if (GUILayout.Button("定位 UIRoot (Alt+F)", GUILayout.Height(36)))
                {
                    EUUISceneEditor.LocateUIRoot();
                }

                GUILayout.EndScrollView();
            });

            imgui.style.width = Length.Percent(100);
            imgui.style.flexGrow = 1;
            contentArea.Add(imgui);
        }

        private void ShowBindPanel()
        {
            var contentArea = rootVisualElement.Q<VisualElement>("content-area");
            if (contentArea == null) return;

            contentArea.Clear();
            contentArea.style.alignItems = Align.FlexStart;
            contentArea.style.justifyContent = Justify.FlexStart;

            contentArea.Add(CreateContentHeader("节点绑定", "为选中的节点添加 EUUINodeBind 组件"));

            var imgui = new IMGUIContainer(() =>
            {
                _scrollPos = GUILayout.BeginScrollView(_scrollPos);

                GUILayout.Space(10);
                GUILayout.Label("选择 Hierarchy 中的节点后点击按钮，系统将：", EditorStyles.wordWrappedLabel);
                GUILayout.Space(5);
                GUILayout.Label("• 添加 EUUINodeBind 组件\n• 自动检测 UI 类型（Button / Image / Text / TMP / RectTransform）\n• 在 Hierarchy 中显示绑定标识", EditorStyles.helpBox);

                GUILayout.Space(15);

                int selectedCount = Selection.gameObjects.Length;
                EditorGUILayout.LabelField("当前选中节点：", selectedCount > 0 ? $"{selectedCount} 个" : "无");

                GUILayout.Space(10);

                GUI.enabled = selectedCount > 0;
                if (GUILayout.Button($"为选中节点添加 NodeBind (Alt+B)", GUILayout.Height(36)))
                {
                    EUUINodeBindEditor.AddBindComponent();
                }
                GUI.enabled = true;

                GUILayout.EndScrollView();
            });

            imgui.style.width = Length.Percent(100);
            imgui.style.flexGrow = 1;
            contentArea.Add(imgui);
        }

        private void ShowExportPanel()
        {
            var contentArea = rootVisualElement.Q<VisualElement>("content-area");
            if (contentArea == null) return;

            contentArea.Clear();
            contentArea.style.alignItems = Align.FlexStart;
            contentArea.style.justifyContent = Justify.FlexStart;

            contentArea.Add(CreateContentHeader("导出 Prefab", "仅导出 Prefab（不执行绑定和代码生成）"));

            var imgui = new IMGUIContainer(() =>
            {
                _scrollPos = GUILayout.BeginScrollView(_scrollPos);

                GUILayout.Space(10);
                GUILayout.Label("此功能仅将当前场景的 UIRoot 导出为 Prefab。", EditorStyles.wordWrappedLabel);
                GUILayout.Space(5);
                GUILayout.Label("提示：\n• 不会生成代码\n• 不会执行自动绑定\n• 适用于已经完成绑定的场景", EditorStyles.helpBox);

                GUILayout.Space(20);

                if (GUILayout.Button("导出 Prefab", GUILayout.Height(36)))
                {
                    EUUIPrefabExportEditor.ExportCurrentPanelToPrefab();
                }

                GUILayout.EndScrollView();
            });

            imgui.style.width = Length.Percent(100);
            imgui.style.flexGrow = 1;
            contentArea.Add(imgui);
        }

        private void ShowAutoPanel()
        {
            var contentArea = rootVisualElement.Q<VisualElement>("content-area");
            if (contentArea == null) return;

            contentArea.Clear();
            contentArea.style.alignItems = Align.FlexStart;
            contentArea.style.justifyContent = Justify.FlexStart;

            contentArea.Add(CreateContentHeader("自动绑定并导出", "完整工作流：代码生成 → 编译 → 自动绑定 → 导出 Prefab"));

            var imgui = new IMGUIContainer(() =>
            {
                _scrollPos = GUILayout.BeginScrollView(_scrollPos);

                GUILayout.Space(10);
                GUILayout.Label("此功能将自动执行完整的 UI 导出流程：", EditorStyles.wordWrappedLabel);
                GUILayout.Space(5);
                GUILayout.Label("1. 校验场景节点（检查命名冲突、NodeBind 完整性）\n2. 生成绑定代码（.Generated.cs + 业务逻辑 .cs）\n3. 等待 Unity 编译完成\n4. 自动绑定字段到 Prefab\n5. 导出 Prefab 到配置路径", EditorStyles.helpBox);

                GUILayout.Space(15);
                GUILayout.Label("⚠ 重要提示：", EditorStyles.boldLabel);
                GUILayout.Label("• 确保当前场景包含 EUUIPanelDescription 组件\n• 确保 UIRoot 节点下的所有需要绑定的节点已添加 EUUINodeBind\n• 导出过程中会触发脚本重新编译", EditorStyles.wordWrappedLabel);

                GUILayout.Space(20);

                if (GUILayout.Button("开始自动绑定并导出", GUILayout.Height(40)))
                {
                    EUUIPrefabExportEditor.StartExportProcess();
                }

                GUILayout.EndScrollView();
            });

            imgui.style.width = Length.Percent(100);
            imgui.style.flexGrow = 1;
            contentArea.Add(imgui);
        }

        private void ShowExtensionsPanel()
        {
            var contentArea = rootVisualElement.Q<VisualElement>("content-area");
            if (contentArea == null) return;

            contentArea.Clear();
            contentArea.style.alignItems = Align.FlexStart;
            contentArea.style.justifyContent = Justify.FlexStart;

            contentArea.Add(CreateContentHeader("生成扩展代码", "根据配置生成模块化扩展（EURes/MVC 等）"));

            var imgui = new IMGUIContainer(() =>
            {
                _scrollPos = GUILayout.BeginScrollView(_scrollPos);

                GUILayout.Space(10);
                GUILayout.Label("EUUI 采用模块化扩展设计，通过 .sbn 模板生成可选功能：", EditorStyles.wordWrappedLabel);
                GUILayout.Space(10);

                EditorGUILayout.LabelField("可用扩展模块", EditorStyles.boldLabel);
                GUILayout.Space(5);

                // EURes 扩展
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("• EURes 资源加载扩展：", GUILayout.Width(200));
                var config = EUUIPrefabExportEditor.GetConfig();
                bool euresEnabled = config != null && config.enableEUResExtension;
                GUILayout.Label(euresEnabled ? "✓ 已启用" : "✗ 未启用", euresEnabled ? EditorStyles.boldLabel : EditorStyles.label);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5);
                GUILayout.Label("  - EUUIPanelBase 静态扩展：LoadSprite、SetImage、LoadPrefabAsync", EditorStyles.wordWrappedLabel);
                GUILayout.Label("  - EUUIKit 分部类：LoadUIPrefabAsync、LoadAtlas", EditorStyles.wordWrappedLabel);

                GUILayout.Space(15);
                EditorGUILayout.LabelField("生成位置", EditorStyles.boldLabel);
                GUILayout.Space(5);
                GUILayout.Label("• EUUIPanelBaseEUResExtensions.Generated.cs\n• EUUIKit.EURes.Generated.cs\n（位于 EUUI/Script/ 目录）", EditorStyles.helpBox);

                GUILayout.Space(15);
                GUILayout.Label("⚠ 提示：在 EUUIEditorConfig 中启用/禁用扩展后，需要点击按钮重新生成代码。", EditorStyles.wordWrappedLabel);

                GUILayout.Space(20);

                GUI.enabled = euresEnabled;
                if (GUILayout.Button("生成 EURes 扩展代码", GUILayout.Height(36)))
                {
                    EUUIPrefabExportEditor.GenerateEUResExtensions();
                }
                GUI.enabled = true;

                GUILayout.Space(10);

                if (!euresEnabled)
                {
                    EditorGUILayout.HelpBox("请在 EUUIEditorConfig 中勾选 'enableEUResExtension' 以启用 EURes 扩展", MessageType.Warning);
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

            string editorUIPath = GetEditorUIPath();
            string uxmlPath = Path.Combine(editorUIPath, "EUUIEditorWindow.uxml").Replace("\\", "/");
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
