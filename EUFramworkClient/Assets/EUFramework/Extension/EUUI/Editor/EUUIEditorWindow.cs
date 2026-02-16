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

            Vector2 windowSize = new Vector2(1000, 700);
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

            var btnConfigGroup = rootVisualElement.Q<Button>("btn-config-group");
            if (btnConfigGroup != null)
            {
                SetSelectedButton(btnConfigGroup);
                ShowConfigGroupPanel();
            }
        }

        private void BindButtons()
        {
            var btnConfigGroup = rootVisualElement.Q<Button>("btn-config-group");
            var btnWorkflowGroup = rootVisualElement.Q<Button>("btn-workflow-group");

            if (btnConfigGroup != null)
            {
                btnConfigGroup.clicked += () =>
                {
                    SetSelectedButton(btnConfigGroup);
                    ShowConfigGroupPanel();
                };
            }

            if (btnWorkflowGroup != null)
            {
                btnWorkflowGroup.clicked += () =>
                {
                    SetSelectedButton(btnWorkflowGroup);
                    ShowWorkflowGroupPanel();
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

            var subtitleLabel = new Label(subtitle);
            subtitleLabel.AddToClassList("content-subtitle");
            header.Add(subtitleLabel);

            return header;
        }

        private void ShowConfigGroupPanel()
        {
            var contentArea = rootVisualElement.Q<VisualElement>("content-area");
            if (contentArea == null) return;

            contentArea.Clear();
            contentArea.style.alignItems = Align.FlexStart;
            contentArea.style.justifyContent = Justify.FlexStart;

            contentArea.Add(CreateContentHeader("配置管理", "管理 EUUI 配置文件和扩展代码生成"));

            // 创建 Tab 容器
            var tabContainer = new VisualElement();
            tabContainer.style.flexDirection = FlexDirection.Row;
            tabContainer.style.marginTop = 15;
            tabContainer.style.marginBottom = 10;
            tabContainer.style.borderBottomWidth = 1;
            tabContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

            var tabConfig = CreateTabButton("配置文件", true);
            var tabExtensions = CreateTabButton("生成扩展代码", false);

            tabContainer.Add(tabConfig);
            tabContainer.Add(tabExtensions);
            contentArea.Add(tabContainer);

            // Tab 内容容器
            var tabContentContainer = new VisualElement();
            tabContentContainer.style.flexGrow = 1;
            contentArea.Add(tabContentContainer);

            // 默认显示配置文件 Tab
            ShowConfigTabContent(tabContentContainer);

            tabConfig.clicked += () =>
            {
                SetActiveTab(tabConfig, tabExtensions);
                ShowConfigTabContent(tabContentContainer);
            };

            tabExtensions.clicked += () =>
            {
                SetActiveTab(tabExtensions, tabConfig);
                ShowExtensionsTabContent(tabContentContainer);
            };
        }

        private void ShowWorkflowGroupPanel()
        {
            var contentArea = rootVisualElement.Q<VisualElement>("content-area");
            if (contentArea == null) return;

            contentArea.Clear();
            contentArea.style.alignItems = Align.FlexStart;
            contentArea.style.justifyContent = Justify.FlexStart;

            contentArea.Add(CreateContentHeader("UI 制作", "UI 场景、节点绑定、Prefab 导出流程"));

            // 创建 Tab 容器
            var tabContainer = new VisualElement();
            tabContainer.style.flexDirection = FlexDirection.Row;
            tabContainer.style.marginTop = 15;
            tabContainer.style.marginBottom = 10;
            tabContainer.style.borderBottomWidth = 1;
            tabContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

            var tabScene = CreateTabButton("创建场景", true);
            var tabLocate = CreateTabButton("定位 UIRoot", false);
            var tabBind = CreateTabButton("节点绑定", false);
            var tabExport = CreateTabButton("导出 Prefab", false);
            var tabAuto = CreateTabButton("自动流程", false);

            tabContainer.Add(tabScene);
            tabContainer.Add(tabLocate);
            tabContainer.Add(tabBind);
            tabContainer.Add(tabExport);
            tabContainer.Add(tabAuto);
            contentArea.Add(tabContainer);

            // Tab 内容容器
            var tabContentContainer = new VisualElement();
            tabContentContainer.style.flexGrow = 1;
            contentArea.Add(tabContentContainer);

            // 默认显示创建场景 Tab
            ShowSceneTabContent(tabContentContainer);

            tabScene.clicked += () =>
            {
                SetActiveTab(tabScene, tabLocate, tabBind, tabExport, tabAuto);
                ShowSceneTabContent(tabContentContainer);
            };

            tabLocate.clicked += () =>
            {
                SetActiveTab(tabLocate, tabScene, tabBind, tabExport, tabAuto);
                ShowLocateTabContent(tabContentContainer);
            };

            tabBind.clicked += () =>
            {
                SetActiveTab(tabBind, tabScene, tabLocate, tabExport, tabAuto);
                ShowBindTabContent(tabContentContainer);
            };

            tabExport.clicked += () =>
            {
                SetActiveTab(tabExport, tabScene, tabLocate, tabBind, tabAuto);
                ShowExportTabContent(tabContentContainer);
            };

            tabAuto.clicked += () =>
            {
                SetActiveTab(tabAuto, tabScene, tabLocate, tabBind, tabExport);
                ShowAutoTabContent(tabContentContainer);
            };
        }

        private Button CreateTabButton(string text, bool isActive)
        {
            var button = new Button { text = text };
            button.style.height = 32;
            button.style.paddingLeft = 15;
            button.style.paddingRight = 15;
            button.style.marginRight = 5;
            button.style.borderLeftWidth = 0;
            button.style.borderRightWidth = 0;
            button.style.borderTopWidth = 0;
            button.style.borderBottomWidth = 0;
            button.style.borderTopLeftRadius = 4;
            button.style.borderTopRightRadius = 4;
            button.style.backgroundColor = isActive ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.2f, 0.2f, 0.2f);
            button.style.color = isActive ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.6f, 0.6f, 0.6f);

            if (isActive)
            {
                button.AddToClassList("tab-active");
            }

            return button;
        }

        private void SetActiveTab(Button activeTab, params Button[] inactiveTabs)
        {
            activeTab.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            activeTab.style.color = new Color(0.9f, 0.9f, 0.9f);
            activeTab.AddToClassList("tab-active");

            foreach (var tab in inactiveTabs)
            {
                tab.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                tab.style.color = new Color(0.6f, 0.6f, 0.6f);
                tab.RemoveFromClassList("tab-active");
            }
        }

        private void ShowConfigTabContent(VisualElement container)
        {
            container.Clear();
            var imgui = new IMGUIContainer(() =>
            {
                _scrollPos = GUILayout.BeginScrollView(_scrollPos);

                GUILayout.Space(10);
                GUILayout.Label("EUUIEditorConfig 配置文件管理", EditorStyles.boldLabel);
                GUILayout.Space(10);

                var config = AssetDatabase.LoadAssetAtPath<EUUIEditorConfig>(GetConfigAssetPath());
                if (config != null)
                {
                    EditorGUILayout.HelpBox($"当前配置文件：{GetConfigAssetPath()}", MessageType.Info);

                    if (GUILayout.Button("打开 UI 配置", GUILayout.Height(36)))
                    {
                        Selection.activeObject = config;
                        EditorGUIUtility.PingObject(config);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("未找到配置文件，点击下方按钮创建", MessageType.Warning);

                    if (GUILayout.Button("创建 UI 配置", GUILayout.Height(36)))
                    {
                        EUUIEditorConfigEditor.CreateConfig();
                    }
                }

                GUILayout.EndScrollView();
            });

            imgui.style.flexGrow = 1;
            container.Add(imgui);
        }

        private void ShowExtensionsTabContent(VisualElement container)
        {
            container.Clear();
            var imgui = new IMGUIContainer(() =>
            {
                _scrollPos = GUILayout.BeginScrollView(_scrollPos);

                GUILayout.Space(10);
                GUILayout.Label("EUUI 采用模块化扩展设计，通过 .sbn 模板生成可选功能：", EditorStyles.wordWrappedLabel);
                GUILayout.Space(10);

                EditorGUILayout.LabelField("可用扩展模块", EditorStyles.boldLabel);
                GUILayout.Space(5);

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
                GUILayout.Label("• EUUI/Script/Generate/\n  - EUUIPanelBaseEUResExtensions.Generated.cs\n  - EUUIKit.EURes.Generated.cs", EditorStyles.helpBox);

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

            imgui.style.flexGrow = 1;
            container.Add(imgui);
        }

        private void ShowSceneTabContent(VisualElement container)
        {
            container.Clear();
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

            imgui.style.flexGrow = 1;
            container.Add(imgui);
        }

        private void ShowLocateTabContent(VisualElement container)
        {
            container.Clear();
            var imgui = new IMGUIContainer(() =>
            {
                _scrollPos = GUILayout.BeginScrollView(_scrollPos);

                GUILayout.Space(10);
                GUILayout.Label("快速定位当前场景中的 UIRoot 节点。", EditorStyles.wordWrappedLabel);

                GUILayout.Space(20);

                if (GUILayout.Button("定位 UIRoot", GUILayout.Height(36)))
                {
                    EUUISceneEditor.LocateUIRoot();
                }

                GUILayout.EndScrollView();
            });

            imgui.style.flexGrow = 1;
            container.Add(imgui);
        }

        private void ShowBindTabContent(VisualElement container)
        {
            container.Clear();
            var imgui = new IMGUIContainer(() =>
            {
                _scrollPos = GUILayout.BeginScrollView(_scrollPos);

                GUILayout.Space(10);
                GUILayout.Label("为当前选中的节点添加 EUUINodeBind 组件，用于代码生成和字段绑定。", EditorStyles.wordWrappedLabel);

                GUILayout.Space(20);

                if (GUILayout.Button("为选中节点添加 NodeBind", GUILayout.Height(36)))
                {
                    EUUINodeBindEditor.AddBindComponent();
                }

                GUILayout.EndScrollView();
            });

            imgui.style.flexGrow = 1;
            container.Add(imgui);
        }

        private void ShowExportTabContent(VisualElement container)
        {
            container.Clear();
            var imgui = new IMGUIContainer(() =>
            {
                _scrollPos = GUILayout.BeginScrollView(_scrollPos);

                GUILayout.Space(10);
                GUILayout.Label("此功能仅导出 Prefab，不进行代码生成和字段绑定。\n适用于已有代码的情况。", EditorStyles.wordWrappedLabel);

                GUILayout.Space(20);

                if (GUILayout.Button("导出 Prefab", GUILayout.Height(36)))
                {
                    EUUIPrefabExportEditor.ExportCurrentPanelToPrefab();
                }

                GUILayout.EndScrollView();
            });

            imgui.style.flexGrow = 1;
            container.Add(imgui);
        }

        private void ShowAutoTabContent(VisualElement container)
        {
            container.Clear();
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

            imgui.style.flexGrow = 1;
            container.Add(imgui);
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
