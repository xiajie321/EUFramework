#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using EUFramework.Extension.EUUI;
using EUFramework.Extension.EUUI.Editor.Templates;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// EUUI 配置工具窗口（与 EUResKit 配置工具同风格）
    /// </summary>
    public class EUUIEditorWindow : EditorWindow
    {
        private Button _selectedButton;
        private VisualElement _selectedContainer;
        private Vector2 _scrollPos;
        
        // 扩展模板创建相关字段
        private EUUIExtensionTemplateCreator.ExtensionType _extensionType = EUUIExtensionTemplateCreator.ExtensionType.ResourceLoader;
        private EUUIExtensionTemplateCreator.TemplatePreset _templatePreset = EUUIExtensionTemplateCreator.TemplatePreset.ResourceLoader;
        private string _extensionName = "";
        private string _extensionSavePath = "Assets/Script/Game/UI/Extensions";
        private bool _autoAddToConfig = true;

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

            // 隐藏默认提示标签
            var contentLabel = rootVisualElement.Q<Label>("content-label");
            if (contentLabel != null)
            {
                contentLabel.style.display = DisplayStyle.None;
            }

            // 默认显示第一个栏位：SO配置管理
            var btnSOConfig = rootVisualElement.Q<Button>("btn-so-config");
            if (btnSOConfig != null)
            {
                SetSelectedButton(btnSOConfig);
                ShowSOConfigPanel();
            }
            else
            {
                Debug.LogError("[EUUI] 无法找到 btn-so-config 按钮，请检查 UXML 文件");
            }
        }

        private void BindButtons()
        {
            var btnSOConfig = rootVisualElement.Q<Button>("btn-so-config");
            var btnExtensions = rootVisualElement.Q<Button>("btn-extensions");
            var btnResourceCreation = rootVisualElement.Q<Button>("btn-resource-creation");

            if (btnSOConfig != null)
            {
                btnSOConfig.clicked += () =>
                {
                    SetSelectedButton(btnSOConfig);
                    ShowSOConfigPanel();
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

            if (btnResourceCreation != null)
            {
                btnResourceCreation.clicked += () =>
                {
                    SetSelectedButton(btnResourceCreation);
                    ShowResourceCreationPanel();
                };
            }
        }

        private void SetSelectedButton(Button button)
        {
            // 移除之前选中按钮的样式
            if (_selectedButton != null)
            {
                _selectedButton.RemoveFromClassList("sidebar-button-selected");
            }
            
            // 移除之前选中容器的样式
            if (_selectedContainer != null)
            {
                _selectedContainer.RemoveFromClassList("sidebar-button-container-selected");
            }
            
            // 设置新的选中按钮
            button.AddToClassList("sidebar-button-selected");
            _selectedButton = button;
            
            // 设置新的选中容器
            _selectedContainer = button.parent;
            if (_selectedContainer != null)
            {
                _selectedContainer.AddToClassList("sidebar-button-container-selected");
            }
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

        /// <summary>
        /// 第一个栏位：SO 配置管理
        /// </summary>
        private void ShowSOConfigPanel()
        {
            var contentArea = rootVisualElement.Q<VisualElement>("content-area");
            if (contentArea == null) return;

            contentArea.Clear();
            contentArea.style.alignItems = Align.Stretch;
            contentArea.style.justifyContent = Justify.FlexStart;

            contentArea.Add(CreateContentHeader("SO 配置管理", "管理 EUUI 所有 ScriptableObject 配置文件"));

            // Tab 栏：EUUIEditorConfig | EUUITemplateRegistry | EUUIKitConfig
            var tabContainer = new VisualElement();
            tabContainer.style.flexDirection = FlexDirection.Row;
            tabContainer.style.marginTop = 15;
            tabContainer.style.marginBottom = 10;
            tabContainer.style.borderBottomWidth = 1;
            tabContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            tabContainer.style.alignSelf = Align.Stretch;

            var tabEditorConfig = CreateTabButton("EUUIEditorConfig", true);
            var tabTemplateRegistry = CreateTabButton("EUUITemplateRegistry", false);
            var tabKitConfig = CreateTabButton("EUUIKitConfig", false);

            tabContainer.Add(tabEditorConfig);
            tabContainer.Add(tabTemplateRegistry);
            tabContainer.Add(tabKitConfig);
            contentArea.Add(tabContainer);

            var tabContentContainer = new VisualElement();
            tabContentContainer.style.flexGrow = 1;
            tabContentContainer.style.alignSelf = Align.Stretch;
            contentArea.Add(tabContentContainer);

            ShowEUUIEditorConfigTabContent(tabContentContainer);

            tabEditorConfig.clicked += () =>
            {
                SetActiveTab(tabEditorConfig, tabTemplateRegistry, tabKitConfig);
                ShowEUUIEditorConfigTabContent(tabContentContainer);
            };
            tabTemplateRegistry.clicked += () =>
            {
                SetActiveTab(tabTemplateRegistry, tabEditorConfig, tabKitConfig);
                ShowEUUITemplateRegistryTabContent(tabContentContainer);
            };
            tabKitConfig.clicked += () =>
            {
                SetActiveTab(tabKitConfig, tabEditorConfig, tabTemplateRegistry);
                ShowEUUIKitConfigTabContent(tabContentContainer);
            };
        }

        /// <summary>
        /// 第二个栏位：拓展管理
        /// </summary>
        private void ShowExtensionsPanel()
        {
            var contentArea = rootVisualElement.Q<VisualElement>("content-area");
            if (contentArea == null) return;

            contentArea.Clear();
            contentArea.style.alignItems = Align.Stretch;
            contentArea.style.justifyContent = Justify.FlexStart;

            contentArea.Add(CreateContentHeader("拓展管理", "管理 .sbn 模板文件和 ExportsCS 导出器"));

            // 创建 Tab 容器
            var tabContainer = new VisualElement();
            tabContainer.style.flexDirection = FlexDirection.Row;
            tabContainer.style.marginTop = 15;
            tabContainer.style.marginBottom = 10;
            tabContainer.style.borderBottomWidth = 1;
            tabContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            tabContainer.style.alignSelf = Align.Stretch;

            var tabTemplates = CreateTabButton("模板管理", true);
            var tabGenerate = CreateTabButton("生成绑定模板", false);
            var tabExtension = CreateTabButton("模板拓展", false);

            tabContainer.Add(tabTemplates);
            tabContainer.Add(tabGenerate);
            tabContainer.Add(tabExtension);
            contentArea.Add(tabContainer);

            // Tab 内容容器
            var tabContentContainer = new VisualElement();
            tabContentContainer.style.flexGrow = 1;
            tabContentContainer.style.alignSelf = Align.Stretch;
            contentArea.Add(tabContentContainer);

            // 默认显示模板管理 Tab
            ShowTemplatesManagementTabContent(tabContentContainer);

            tabTemplates.clicked += () =>
            {
                SetActiveTab(tabTemplates, tabGenerate, tabExtension);
                ShowTemplatesManagementTabContent(tabContentContainer);
            };

            tabGenerate.clicked += () =>
            {
                SetActiveTab(tabGenerate, tabTemplates, tabExtension);
                ShowExtensionsTabContent(tabContentContainer);
            };

            tabExtension.clicked += () =>
            {
                SetActiveTab(tabExtension, tabTemplates, tabGenerate);
                ShowCreateExtensionTabContent(tabContentContainer);
            };
        }

        /// <summary>
        /// 第三个栏位：EUUI 资源制作相关
        /// </summary>
        private void ShowResourceCreationPanel()
        {
            var contentArea = rootVisualElement.Q<VisualElement>("content-area");
            if (contentArea == null) return;

            contentArea.Clear();
            contentArea.style.alignItems = Align.Stretch;
            contentArea.style.justifyContent = Justify.FlexStart;

            contentArea.Add(CreateContentHeader("EUUI 资源制作", "UI 场景、节点绑定、Prefab 导出流程"));

            // 创建 Tab 容器
            var tabContainer = new VisualElement();
            tabContainer.style.flexDirection = FlexDirection.Row;
            tabContainer.style.marginTop = 15;
            tabContainer.style.marginBottom = 10;
            tabContainer.style.borderBottomWidth = 1;
            tabContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            tabContainer.style.alignSelf = Align.Stretch;

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
            tabContentContainer.style.alignSelf = Align.Stretch;
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

        private void ShowCreateExtensionTabContent(VisualElement container)
        {
            container.Clear();
            
            // 加载 UXML 模板
            var template = LoadUXMLTemplate("CreateExtensionTab.uxml");
            if (template == null) return;
            
            var tab = template.Instantiate();
            
            // 查询 UI 元素
            var typeField = tab.Q<EnumField>("extension-type");
            var nameField = tab.Q<TextField>("extension-name");
            var presetField = tab.Q<EnumField>("template-preset");
            var pathField = tab.Q<TextField>("save-path");
            var browseBtn = tab.Q<Button>("btn-browse");
            var autoAddToggle = tab.Q<Toggle>("auto-add-config");
            var createBtn = tab.Q<Button>("btn-create");
            
            var typeHint = tab.Q<HelpBox>("type-hint");
            var nameValidation = tab.Q<HelpBox>("name-validation");
            var presetHint = tab.Q<HelpBox>("preset-hint");
            var pathHint = tab.Q<HelpBox>("path-hint");
            var previewLabel = tab.Q<Label>("preview-label");
            var previewFilename = tab.Q<TextField>("preview-filename");
            
            // 初始化枚举字段
            typeField.Init(_extensionType);
            presetField.Init(_templatePreset);
            
            // 设置初始值
            nameField.value = _extensionName;
            pathField.value = _extensionSavePath;
            autoAddToggle.value = _autoAddToConfig;
            
            // 更新提示信息
            UpdateTypeHint(typeHint, _extensionType);
            UpdatePresetHint(presetHint, _templatePreset);
            
            // 绑定事件
            typeField.RegisterValueChangedCallback(evt => {
                _extensionType = (EUUIExtensionTemplateCreator.ExtensionType)evt.newValue;
                UpdateTypeHint(typeHint, _extensionType);
                UpdatePreview(previewLabel, previewFilename, nameField.value);
            });
            
            nameField.RegisterValueChangedCallback(evt => {
                _extensionName = evt.newValue;
                ValidateExtensionName(nameValidation, evt.newValue);
                UpdatePreview(previewLabel, previewFilename, evt.newValue);
                UpdateCreateButtonState(createBtn);
            });
            
            presetField.RegisterValueChangedCallback(evt => {
                _templatePreset = (EUUIExtensionTemplateCreator.TemplatePreset)evt.newValue;
                UpdatePresetHint(presetHint, _templatePreset);
            });
            
            pathField.RegisterValueChangedCallback(evt => {
                _extensionSavePath = evt.newValue;
                UpdatePathHint(pathHint, evt.newValue);
            });
            
            autoAddToggle.RegisterValueChangedCallback(evt => {
                _autoAddToConfig = evt.newValue;
            });
            
            browseBtn.clicked += () => {
                string selectedPath = EditorUtility.OpenFolderPanel("选择保存位置", _extensionSavePath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        _extensionSavePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                        pathField.value = _extensionSavePath;
                    }
                }
            };
            
            createBtn.clicked += CreateExtensionTemplate;
            
            // 初始验证
            ValidateExtensionName(nameValidation, _extensionName);
            UpdatePathHint(pathHint, _extensionSavePath);
            UpdateCreateButtonState(createBtn);
            
            container.Add(tab);
        }
        
        private void UpdateTypeHint(HelpBox helpBox, EUUIExtensionTemplateCreator.ExtensionType type)
        {
            string hint = type switch
            {
                EUUIExtensionTemplateCreator.ExtensionType.ResourceLoader => "创建自定义资源加载系统（如 Addressables、AssetBundle）",
                EUUIExtensionTemplateCreator.ExtensionType.PanelExtension => "为 EUUIPanelBase 添加静态扩展方法（如 OSA、DoTween）",
                EUUIExtensionTemplateCreator.ExtensionType.KitExtension => "为 EUUIKit 添加功能扩展（如分析统计、日志）",
                _ => ""
            };
            helpBox.text = hint;
        }
        
        private void UpdatePresetHint(HelpBox helpBox, EUUIExtensionTemplateCreator.TemplatePreset preset)
        {
            string hint = preset switch
            {
                EUUIExtensionTemplateCreator.TemplatePreset.Empty => "仅包含基础结构和 TODO 注释",
                EUUIExtensionTemplateCreator.TemplatePreset.ResourceLoader => "包含完整的资源加载/释放方法框架",
                EUUIExtensionTemplateCreator.TemplatePreset.StaticExtension => "包含静态扩展方法示例",
                EUUIExtensionTemplateCreator.TemplatePreset.Utility => "包含独立功能方法示例",
                _ => ""
            };
            helpBox.text = hint;
        }
        
        private void ValidateExtensionName(HelpBox helpBox, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                helpBox.text = "请输入扩展名称（如：MyLoader、OSA、DoTween）";
                helpBox.messageType = HelpBoxMessageType.Warning;
                helpBox.style.display = DisplayStyle.Flex;
            }
            else if (!IsValidExtensionName(name))
            {
                helpBox.text = "名称只能包含字母、数字和下划线，且必须以字母开头";
                helpBox.messageType = HelpBoxMessageType.Error;
                helpBox.style.display = DisplayStyle.Flex;
            }
            else
            {
                helpBox.style.display = DisplayStyle.None;
            }
        }
        
        private void UpdatePathHint(HelpBox helpBox, string path)
        {
            if (!System.IO.Directory.Exists(path))
            {
                helpBox.text = $"目录不存在，将自动创建：{path}";
                helpBox.messageType = HelpBoxMessageType.Info;
                helpBox.style.display = DisplayStyle.Flex;
            }
            else
            {
                helpBox.style.display = DisplayStyle.None;
            }
        }
        
        private void UpdatePreview(Label label, TextField field, string name)
        {
            if (!string.IsNullOrEmpty(name) && IsValidExtensionName(name))
            {
                string fileName = GetExtensionFileName();
                field.value = System.IO.Path.Combine(_extensionSavePath, fileName);
                label.style.display = DisplayStyle.Flex;
                field.style.display = DisplayStyle.Flex;
            }
            else
            {
                label.style.display = DisplayStyle.None;
                field.style.display = DisplayStyle.None;
            }
        }
        
        private void UpdateCreateButtonState(Button button)
        {
            button.SetEnabled(CanCreateExtension());
        }
        
        private VisualTreeAsset LoadUXMLTemplate(string filename)
        {
            string editorUIPath = GetEditorUIPath();
            string uxmlPath = Path.Combine(editorUIPath, filename).Replace("\\", "/");
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            
            if (template == null)
            {
                Debug.LogError($"[EUUI] 无法加载 UXML 模板: {uxmlPath}");
            }
            
            return template;
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

        private void ShowEUUIEditorConfigTabContent(VisualElement container)
        {
            container.Clear();
            var template = LoadUXMLTemplate("ConfigTab.uxml");
            if (template == null) return;
            var tab = template.Instantiate();
            var statusHelpBox = tab.Q<HelpBox>("config-status");
            var openBtn = tab.Q<Button>("btn-open-config");
            var createBtn = tab.Q<Button>("btn-create-config");
            var config = AssetDatabase.LoadAssetAtPath<EUUIEditorConfig>(GetConfigAssetPath());
            if (config != null)
            {
                statusHelpBox.text = $"当前配置文件：{GetConfigAssetPath()}";
                statusHelpBox.messageType = HelpBoxMessageType.Info;
                openBtn.style.display = DisplayStyle.Flex;
                createBtn.style.display = DisplayStyle.None;
                openBtn.clicked += () => { Selection.activeObject = config; EditorGUIUtility.PingObject(config); };
            }
            else
            {
                statusHelpBox.text = "未找到配置文件，点击下方按钮创建";
                statusHelpBox.messageType = HelpBoxMessageType.Warning;
                openBtn.style.display = DisplayStyle.None;
                createBtn.style.display = DisplayStyle.Flex;
                createBtn.clicked += () => { EUUIEditorConfigEditor.CreateConfig(); ShowEUUIEditorConfigTabContent(container); };
            }
            container.Add(tab);
        }

        private static string GetTemplateRegistryAssetPath()
        {
            string editorDir = EUUITemplateManager.GetEditorDirectory();
            return string.IsNullOrEmpty(editorDir) ? null : Path.Combine(editorDir, "EUUITemplateRegistry.asset").Replace("\\", "/");
        }

        private static string GetKitConfigAssetPath()
        {
            string resourcesPath = EUUIEditorConfigEditor.GetResourcesPath();
            return Path.Combine(resourcesPath, "EUUIKitConfig.asset").Replace("\\", "/");
        }

        private void ShowEUUITemplateRegistryTabContent(VisualElement container)
        {
            container.Clear();
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.style.alignSelf = Align.Stretch;
            var box = new HelpBox { messageType = HelpBoxMessageType.Info };
            var path = GetTemplateRegistryAssetPath();
            var registry = path != null ? AssetDatabase.LoadAssetAtPath<EUUITemplateRegistryAsset>(path) : null;
            if (registry != null)
            {
                box.text = $"当前注册表：{path}";
                var openBtn = new Button(() => { Selection.activeObject = registry; EditorGUIUtility.PingObject(registry); }) { text = "打开注册表" };
                var refreshBtn = new Button(() => { EUUITemplateRegistryGenerator.RefreshRegistry(); box.text = $"已刷新。当前注册表：{path}"; }) { text = "刷新注册表" };
                scroll.Add(box);
                scroll.Add(openBtn);
                scroll.Add(refreshBtn);
            }
            else
            {
                box.text = "未找到模板注册表，点击下方按钮生成。";
                box.messageType = HelpBoxMessageType.Warning;
                var refreshBtn = new Button(() => { EUUITemplateRegistryGenerator.RefreshRegistry(); ShowEUUITemplateRegistryTabContent(container); }) { text = "生成注册表" };
                scroll.Add(box);
                scroll.Add(refreshBtn);
            }
            container.Add(scroll);
        }

        private void ShowEUUIKitConfigTabContent(VisualElement container)
        {
            container.Clear();
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.style.alignSelf = Align.Stretch;
            var path = GetKitConfigAssetPath();
            var kitConfig = AssetDatabase.LoadAssetAtPath<EUUIKitConfig>(path);
            var box = new HelpBox { messageType = HelpBoxMessageType.Info };
            if (kitConfig != null)
            {
                box.text = $"当前运行时配置：{path}（由 EUUIKit 运行时加载）";
                var openBtn = new Button(() => { Selection.activeObject = kitConfig; EditorGUIUtility.PingObject(kitConfig); }) { text = "打开 EUUIKitConfig" };
                var editorConfig = AssetDatabase.LoadAssetAtPath<EUUIEditorConfig>(GetConfigAssetPath());
                var syncBtn = new Button(() =>
                {
                    if (editorConfig == null) { EditorUtility.DisplayDialog("提示", "请先创建或打开 EUUIEditorConfig。", "确定"); return; }
                    EUUIEditorConfigEditorSync.SyncEditorConfigToKitConfig(editorConfig);
                    EditorUtility.DisplayDialog("完成", $"已同步到：{path}", "确定");
                    ShowEUUIKitConfigTabContent(container);
                }) { text = "从 EditorConfig 同步" };
                scroll.Add(box);
                scroll.Add(openBtn);
                scroll.Add(syncBtn);
            }
            else
            {
                box.text = "未找到 EUUIKitConfig。可从 EUUIEditorConfig 同步生成，或手动创建于 Resources 目录。";
                box.messageType = HelpBoxMessageType.Warning;
                var editorConfig = AssetDatabase.LoadAssetAtPath<EUUIEditorConfig>(GetConfigAssetPath());
                var syncBtn = new Button(() =>
                {
                    if (editorConfig == null) { EditorUtility.DisplayDialog("提示", "请先创建 EUUIEditorConfig。", "确定"); return; }
                    EUUIEditorConfigEditorSync.SyncEditorConfigToKitConfig(editorConfig);
                    EditorUtility.DisplayDialog("完成", $"已创建并同步：{path}", "确定");
                    ShowEUUIKitConfigTabContent(container);
                }) { text = "从 EditorConfig 同步并创建" };
                scroll.Add(box);
                scroll.Add(syncBtn);
            }
            container.Add(scroll);
        }

        private void ShowExtensionsTabContent(VisualElement container)
        {
            container.Clear();
            
            var template = LoadUXMLTemplate("ExtensionsTab.uxml");
            if (template == null) return;
            
            var tab = template.Instantiate();
            var itemsList = tab.Q<ScrollView>("generatable-items-list");
            var coreStatus = tab.Q<VisualElement>("core-extension-status");
            var additionalStatus = tab.Q<VisualElement>("additional-extensions-status");
            var generateBtn = tab.Q<Button>("btn-generate");
            var deleteBtn = tab.Q<Button>("btn-delete");
            var manualHint = tab.Q<HelpBox>("manual-hint");
            
            var config = EUUIPanelExporter.GetConfig();
            if (config == null)
            {
                var errorLabel = new Label("未找到配置文件！");
                errorLabel.style.color = new Color(1f, 0.5f, 0.5f);
                coreStatus.Add(errorLabel);
                generateBtn.SetEnabled(false);
                container.Add(tab);
                return;
            }

            // 与 .sbn 一一对应：列表显示全部项（含未启用），可在此勾选「加入管理」并创建/删除
            if (itemsList != null)
            {
                var rows = EUUIStaticExporter.GetManageableRows(config);
                var content = itemsList.contentContainer;
                content.Clear();
                foreach (var row in rows)
                {
                    var item = row.Item;
                    var rowEl = new VisualElement();
                    rowEl.style.flexDirection = FlexDirection.Row;
                    rowEl.style.alignItems = Align.Center;
                    rowEl.style.marginBottom = 4;
                    rowEl.style.paddingLeft = 4;
                    rowEl.style.paddingRight = 4;
                    rowEl.style.paddingTop = 2;
                    rowEl.style.paddingBottom = 2;
                    rowEl.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);

                    // 核心项显示「核心」标签，手动扩展显示勾选+「管理」，左右对齐一致
                    if (row.ManualExt != null)
                    {
                        var toggle = new Toggle { value = row.Enabled };
                        toggle.style.width = 18;
                        toggle.RegisterValueChangedCallback(evt =>
                        {
                            row.ManualExt.enabled = evt.newValue;
                            EditorUtility.SetDirty(config);
                            AssetDatabase.SaveAssets();
                            ShowExtensionsTabContent(container);
                        });
                        rowEl.Add(toggle);
                        rowEl.Add(new Label("管理") { style = { minWidth = 28, fontSize = 11 } });
                    }
                    else
                    {
                        rowEl.Add(new Label("核心") { style = { minWidth = 46, fontSize = 11, color = new Color(0.7f, 0.7f, 0.7f) } });
                    }

                    var nameLabel = new Label(item.DisplayName) { style = { minWidth = 120, unityFontStyleAndWeight = FontStyle.Bold } };
                    rowEl.Add(nameLabel);

                    string statusText = row.Enabled ? (item.Exists ? "已生成" : "未生成") : "未加入管理";
                    var statusLabel = new Label(statusText);
                    statusLabel.style.minWidth = 56;
                    if (row.Enabled)
                        statusLabel.style.color = item.Exists ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.9f, 0.7f, 0.3f);
                    else
                        statusLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                    rowEl.Add(statusLabel);
                    rowEl.Add(new VisualElement { style = { flexGrow = 1 } });

                    // 有输出路径：显示创建/删除；无输出路径（如 WithData 由 EUUIPanelExporter 处理）：显示说明，避免右侧空白
                    if (row.Enabled && !string.IsNullOrEmpty(item.OutputAssetPath))
                    {
                        var btn = new Button();
                        if (item.Exists)
                        {
                            btn.text = "删除";
                            btn.clicked += () =>
                            {
                                EUUIStaticExporter.DeleteSingleItem(item);
                                ShowExtensionsTabContent(container);
                            };
                        }
                        else
                        {
                            btn.text = "创建";
                            btn.clicked += () =>
                            {
                                try
                                {
                                    EUUIStaticExporter.ExportSingleItem(config, item);
                                    ShowExtensionsTabContent(container);
                                }
                                catch (Exception ex)
                                {
                                    EditorUtility.DisplayDialog("生成失败", ex.Message, "确定");
                                }
                            };
                        }
                        btn.style.minWidth = 56;
                        rowEl.Add(btn);
                    }
                    else if (row.Enabled && string.IsNullOrEmpty(item.OutputAssetPath))
                    {
                        var hint = new Label("由其他导出器生成");
                        hint.style.minWidth = 56;
                        hint.style.fontSize = 11;
                        hint.style.color = new Color(0.55f, 0.55f, 0.55f);
                        hint.tooltip = "如 EUUIPanel.Generated、MVC 由「模板拓展」或生成面板时通过 EUUIPanelExporter 生成";
                        rowEl.Add(hint);
                    }
                    content.Add(rowEl);
                }
            }
            
            // 显示核心扩展状态
            string loaderTypeName = config.resourceLoader switch
            {
                ResourceLoaderType.EURes => "EURes（框架默认）",
                ResourceLoaderType.Custom => "自定义模板",
                ResourceLoaderType.None => "无（手动实现）",
                _ => "未知"
            };
            
            var coreLabel = new Label($"资源加载器类型：{loaderTypeName}");
            coreLabel.AddToClassList("extension-name");
            coreStatus.Add(coreLabel);
            
            if (config.resourceLoader == ResourceLoaderType.Custom && !string.IsNullOrEmpty(config.customLoaderTemplate))
            {
                var customPath = new Label($"模板: {config.customLoaderTemplate}");
                customPath.AddToClassList("extension-path");
                coreStatus.Add(customPath);
            }
            
            // 显示附加扩展状态
            int manualCount = config.manualExtensions?.Count ?? 0;
            int enabledCount = config.manualExtensions?.FindAll(e => e.enabled).Count ?? 0;
            
            var autoDiscoverLabel = new Label($"自动发现：{(config.autoDiscoverExtensions ? "✓ 已启用" : "✗ 未启用")}");
            autoDiscoverLabel.AddToClassList("section-description");
            additionalStatus.Add(autoDiscoverLabel);
            
            if (config.autoDiscoverExtensions)
            {
                var dirLabel = new Label($"目录: {config.extensionsDirectory}");
                dirLabel.AddToClassList("extension-path");
                additionalStatus.Add(dirLabel);
            }
            
            var manualLabel = new Label($"手动配置扩展：{enabledCount} / {manualCount} 个已启用");
            manualLabel.AddToClassList("section-description");
            additionalStatus.Add(manualLabel);
            
            generateBtn.clicked += () => { EUUIStaticExporter.ExportAll(); ShowExtensionsTabContent(container); };
            if (deleteBtn != null)
                deleteBtn.clicked += () => { EUUIStaticExporter.DeleteAllGeneratedFiles(); ShowExtensionsTabContent(container); };
            
            if (config.resourceLoader == ResourceLoaderType.None)
            {
                manualHint.text = "当前设置为手动实现，不会生成核心扩展代码。\n请创建 EUUIKit.*.cs 分部类并实现 LoadPanelPrefabAsync<T>() 方法。";
                manualHint.messageType = HelpBoxMessageType.Info;
                manualHint.style.display = DisplayStyle.Flex;
            }
            
            container.Add(tab);
        }
        
        private void ShowTemplatesManagementTabContent(VisualElement container)
        {
            container.Clear();
            container.style.paddingLeft = 20;
            container.style.paddingRight = 20;
            container.style.paddingTop = 10;
            container.style.alignSelf = Align.Stretch;
            
            var imgui = new IMGUIContainer(() =>
            {
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                GUILayout.Space(10);
                GUILayout.Label("模板管理", EditorStyles.boldLabel);
                GUILayout.Space(10);

                var config = EUUIPanelExporter.GetConfig();
                if (config == null)
                {
                    EditorGUILayout.HelpBox("未找到配置文件！可在下方点击按钮创建 EUUIEditorConfig。", MessageType.Error);
                    GUILayout.Space(8);
                    if (GUILayout.Button("创建配置文件", GUILayout.Height(28)))
                    {
                        EUUIEditorConfigEditor.CreateConfig();
                        Repaint();
                    }
                    GUILayout.EndScrollView();
                    return;
                }
                
                // 扫描 Templates/Sbn/ 目录下的所有 .sbn 文件
                string templatesDir = EUUITemplateManager.GetTemplatesDirectory();
                if (string.IsNullOrEmpty(templatesDir))
                {
                    EditorGUILayout.HelpBox("无法找到模板目录！", MessageType.Error);
                    GUILayout.EndScrollView();
                    return;
                }

                // 确保配置列表已初始化
                if (config.manualExtensions == null)
                {
                    config.manualExtensions = new System.Collections.Generic.List<EUUIAdditionalExtension>();
                }

                // 扫描所有 .sbn 文件
                var sbnFiles = new System.Collections.Generic.List<string>();
                if (System.IO.Directory.Exists(templatesDir))
                {
                    string[] files = System.IO.Directory.GetFiles(templatesDir, "*.sbn", System.IO.SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        string relativePath = file.Replace("\\", "/");
                        if (relativePath.StartsWith(Application.dataPath))
                        {
                            relativePath = "Assets" + relativePath.Substring(Application.dataPath.Length);
                        }
                        sbnFiles.Add(relativePath);
                    }
                }

                // 扫描 ExportsCS 目录下的导出脚本
                string exportsDir = templatesDir.Replace("/Sbn", "/ExportsCS");
                var exporterScripts = new System.Collections.Generic.List<string>();
                if (System.IO.Directory.Exists(exportsDir))
                {
                    string[] scripts = System.IO.Directory.GetFiles(exportsDir, "*.cs", System.IO.SearchOption.TopDirectoryOnly);
                    foreach (var script in scripts)
                    {
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(script);
                        exporterScripts.Add(fileName);
                    }
                }

                if (exporterScripts.Count == 0)
                {
                    EditorGUILayout.HelpBox("未找到导出脚本！请确保 ExportsCS 目录下有导出脚本。", MessageType.Warning);
                }

                GUILayout.Space(5);
                EditorGUILayout.LabelField("模板文件配置", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("为每个 .sbn 模板文件选择对应的导出脚本。\n" +
                    "• 导出脚本：选择用于生成代码的导出器\n" +
                    "• 导出目标路径由导出脚本自动处理", MessageType.Info);
                GUILayout.Space(5);

                if (sbnFiles.Count == 0)
                {
                    EditorGUILayout.HelpBox("未找到任何 .sbn 模板文件！", MessageType.Warning);
                }
                else
                {
                    // 为每个 .sbn 文件显示配置项
                    foreach (var sbnPath in sbnFiles)
                    {
                        // 查找或创建配置项
                        var ext = config.manualExtensions.Find(e => e.templatePath == sbnPath);
                        if (ext == null)
                        {
                            // 默认使用 EUUIStaticExporter，如果不存在则使用列表中的第一个
                            string defaultExporter = "EUUIStaticExporter";
                            if (!exporterScripts.Contains(defaultExporter) && exporterScripts.Count > 0)
                            {
                                defaultExporter = exporterScripts[0];
                            }
                            else if (exporterScripts.Count == 0)
                            {
                                defaultExporter = "";
                            }
                            
                            ext = new EUUIAdditionalExtension
                            {
                                templatePath = sbnPath,
                                exporterScript = defaultExporter,
                                enabled = false
                            };
                            config.manualExtensions.Add(ext);
                            EditorUtility.SetDirty(config);
                        }

                        // 提取模板文件名用于显示
                        string fileName = System.IO.Path.GetFileName(sbnPath);
                        
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        
                        // 第一行：勾选框（是否加入生成面板管理）+ 模板文件名 + 操作按钮
                        EditorGUILayout.BeginHorizontal();
                        bool newEnabled = EditorGUILayout.Toggle(ext.enabled, GUILayout.Width(18));
                        if (newEnabled != ext.enabled)
                        {
                            ext.enabled = newEnabled;
                            EditorUtility.SetDirty(config);
                        }
                        EditorGUILayout.LabelField("生成面板管理", GUILayout.Width(72));
                        EditorGUILayout.LabelField(fileName, EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        
                        if (GUILayout.Button("打开", GUILayout.Width(50)))
                        {
                            if (System.IO.File.Exists(sbnPath))
                            {
                                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(sbnPath, 1);
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("文件不存在", $"模板文件不存在：\n{sbnPath}", "确定");
                            }
                        }
                        
                        EditorGUILayout.EndHorizontal();
                        
                        // 第二行：完整路径（小字，便于查看位置）
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(sbnPath, EditorStyles.miniLabel);
                        EditorGUILayout.EndHorizontal();
                        
                        // 第三行：导出脚本选择
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("导出脚本：", GUILayout.Width(80));
                        
                        if (exporterScripts.Count == 0)
                        {
                            EditorGUILayout.LabelField("（无可用导出脚本）", EditorStyles.miniLabel);
                        }
                        else
                        {
                            int currentIndex = exporterScripts.IndexOf(ext.exporterScript);
                            // 如果当前配置无效，优先选择 EUUIStaticExporter，否则选择第一个
                            if (currentIndex < 0)
                            {
                                int staticExporterIndex = exporterScripts.IndexOf("EUUIStaticExporter");
                                currentIndex = staticExporterIndex >= 0 ? staticExporterIndex : 0;
                                // 自动更新配置为默认值
                                ext.exporterScript = exporterScripts[currentIndex];
                                EditorUtility.SetDirty(config);
                            }
                            
                            int newIndex = EditorGUILayout.Popup(currentIndex, exporterScripts.ToArray());
                            if (newIndex != currentIndex && newIndex >= 0 && newIndex < exporterScripts.Count)
                            {
                                ext.exporterScript = exporterScripts[newIndex];
                                EditorUtility.SetDirty(config);
                            }
                        }
                        
                        EditorGUILayout.EndHorizontal();
                        
                        EditorGUILayout.EndVertical();
                        GUILayout.Space(5);
                    }
                }

                GUILayout.Space(15);
                
                // 快捷操作
                EditorGUILayout.LabelField("快捷操作", EditorStyles.boldLabel);
                GUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("打开配置文件"))
                {
                    Selection.activeObject = config;
                    EditorGUIUtility.PingObject(config);
                }
                if (GUILayout.Button("刷新模板列表"))
                {
                    AssetDatabase.Refresh();
                }
                if (GUILayout.Button("打开模板目录"))
                {
                    if (System.IO.Directory.Exists(templatesDir))
                    {
                        EditorUtility.RevealInFinder(templatesDir);
                    }
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            });

            imgui.style.flexGrow = 1;
            imgui.style.alignSelf = Align.Stretch;
            container.Add(imgui);
        }
        

        private void ShowSceneTabContent(VisualElement container)
        {
            container.Clear();
            container.style.paddingLeft = 20;
            container.style.paddingRight = 20;
            container.style.paddingTop = 10;
            container.style.alignSelf = Align.Stretch;
            
            var imgui = new IMGUIContainer(() =>
            {
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                GUILayout.Space(10);
                GUILayout.Label("场景将保存到配置中的 uiSceneSavePath（或默认 Excluded/CreateUIScenes）。", EditorStyles.wordWrappedLabel);
                GUILayout.Space(5);
                GUILayout.Label("层级结构：Excluded_Bottom、UIRoot、Excluded_Top。", EditorStyles.wordWrappedLabel);

                GUILayout.Space(20);

                if (GUILayout.Button("创建 UI 场景", GUILayout.Height(36), GUILayout.ExpandWidth(true)))
                {
                    EUUISceneEditor.ShowCreateSceneWindow();
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            });

            imgui.style.flexGrow = 1;
            imgui.style.alignSelf = Align.Stretch;
            container.Add(imgui);
        }

        private void ShowLocateTabContent(VisualElement container)
        {
            container.Clear();
            container.style.paddingLeft = 20;
            container.style.paddingRight = 20;
            container.style.paddingTop = 10;
            container.style.alignSelf = Align.Stretch;
            
            var imgui = new IMGUIContainer(() =>
            {
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                GUILayout.Space(10);
                GUILayout.Label("快速定位当前场景中的 UIRoot 节点。", EditorStyles.wordWrappedLabel);

                GUILayout.Space(20);

                if (GUILayout.Button("定位 UIRoot", GUILayout.Height(36), GUILayout.ExpandWidth(true)))
                {
                    EUUISceneEditor.LocateUIRoot();
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            });

            imgui.style.flexGrow = 1;
            imgui.style.alignSelf = Align.Stretch;
            container.Add(imgui);
        }

        private void ShowBindTabContent(VisualElement container)
        {
            container.Clear();
            container.style.paddingLeft = 20;
            container.style.paddingRight = 20;
            container.style.paddingTop = 10;
            container.style.alignSelf = Align.Stretch;
            
            var imgui = new IMGUIContainer(() =>
            {
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                GUILayout.Space(10);
                GUILayout.Label("为当前选中的节点添加 EUUINodeBind 组件，用于代码生成和字段绑定。", EditorStyles.wordWrappedLabel);

                GUILayout.Space(20);

                if (GUILayout.Button("为选中节点添加 NodeBind", GUILayout.Height(36), GUILayout.ExpandWidth(true)))
                {
                    EUUINodeBindEditor.AddBindComponent();
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            });

            imgui.style.flexGrow = 1;
            imgui.style.alignSelf = Align.Stretch;
            container.Add(imgui);
        }

        private void ShowExportTabContent(VisualElement container)
        {
            container.Clear();
            container.style.paddingLeft = 20;
            container.style.paddingRight = 20;
            container.style.paddingTop = 10;
            container.style.alignSelf = Align.Stretch;
            
            var imgui = new IMGUIContainer(() =>
            {
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                GUILayout.Space(10);
                GUILayout.Label("此功能仅导出 Prefab，不进行代码生成和字段绑定。\n适用于已有代码的情况。", EditorStyles.wordWrappedLabel);

                GUILayout.Space(20);

                if (GUILayout.Button("导出 Prefab", GUILayout.Height(36), GUILayout.ExpandWidth(true)))
                {
                    EUUIPanelExporter.ExportCurrentPanelToPrefab();
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            });

            imgui.style.flexGrow = 1;
            imgui.style.alignSelf = Align.Stretch;
            container.Add(imgui);
        }

        private void ShowAutoTabContent(VisualElement container)
        {
            container.Clear();
            container.style.paddingLeft = 20;
            container.style.paddingRight = 20;
            container.style.paddingTop = 10;
            container.style.alignSelf = Align.Stretch;
            
            var imgui = new IMGUIContainer(() =>
            {
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                GUILayout.Space(10);
                GUILayout.Label("此功能将自动执行完整的 UI 导出流程：", EditorStyles.wordWrappedLabel);
                GUILayout.Space(5);
                GUILayout.Label("1. 校验场景节点（检查命名冲突、NodeBind 完整性）\n2. 生成绑定代码（.Generated.cs + 业务逻辑 .cs）\n3. 等待 Unity 编译完成\n4. 自动绑定字段到 Prefab\n5. 导出 Prefab 到配置路径", EditorStyles.helpBox);

                GUILayout.Space(15);
                GUILayout.Label("⚠ 重要提示：", EditorStyles.boldLabel);
                GUILayout.Label("• 确保当前场景包含 EUUIPanelDescription 组件\n• 确保 UIRoot 节点下的所有需要绑定的节点已添加 EUUINodeBind\n• 导出过程中会触发脚本重新编译", EditorStyles.wordWrappedLabel);

                GUILayout.Space(20);

                if (GUILayout.Button("开始自动绑定并导出", GUILayout.Height(40), GUILayout.ExpandWidth(true)))
                {
                    EUUIPanelExporter.StartExportProcess();
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            });

            imgui.style.flexGrow = 1;
            imgui.style.alignSelf = Align.Stretch;
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
        
        // ========== 扩展模板创建辅助方法 ==========
        
        private bool IsValidExtensionName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            
            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }
            
            return char.IsLetter(name[0]);
        }
        
        private string GetExtensionFileName()
        {
            return _extensionType == EUUIExtensionTemplateCreator.ExtensionType.PanelExtension
                ? $"EUUIPanelBase.{_extensionName}.sbn"
                : $"EUUIKit.{_extensionName}.sbn";
        }
        
        private bool CanCreateExtension()
        {
            return !string.IsNullOrEmpty(_extensionName) && IsValidExtensionName(_extensionName);
        }
        
        private void CreateExtensionTemplate()
        {
            try
            {
                // 使用 EUUIExtensionTemplateCreator 的静态方法
                string fileName = GetExtensionFileName();
                string fullPath = Path.Combine(_extensionSavePath, fileName);

                // 确保目录存在
                if (!Directory.Exists(_extensionSavePath))
                {
                    Directory.CreateDirectory(_extensionSavePath);
                }

                // 检查文件是否已存在
                if (File.Exists(fullPath))
                {
                    if (!EditorUtility.DisplayDialog("文件已存在", 
                        $"文件 {fileName} 已存在，是否覆盖？", 
                        "覆盖", "取消"))
                    {
                        return;
                    }
                }

                // 生成模板内容
                string templateContent = EUUIExtensionTemplateCreator.GenerateTemplateContent(
                    _extensionType, 
                    _templatePreset, 
                    _extensionName
                );

                // 写入文件
                File.WriteAllText(fullPath, templateContent, System.Text.Encoding.UTF8);
                AssetDatabase.Refresh();

                // 自动添加到配置
                if (_autoAddToConfig)
                {
                    var config = EUUIPanelExporter.GetConfig();
                    if (config != null)
                    {
                        if (config.manualExtensions == null)
                        {
                            config.manualExtensions = new System.Collections.Generic.List<EUUIAdditionalExtension>();
                        }

                        var ext = new EUUIAdditionalExtension
                        {
                            templatePath = fullPath,
                            exporterScript = "EUUIStaticExporter",
                            enabled = false
                        };

                        config.manualExtensions.Add(ext);
                        EditorUtility.SetDirty(config);
                        AssetDatabase.SaveAssets();
                    }
                }

                // 在 Project 窗口中高亮显示
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fullPath);
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;

                // 显示成功消息
                EditorUtility.DisplayDialog("创建成功", 
                    $"扩展模板已创建：\n{fullPath}\n\n" +
                    (_autoAddToConfig ? "已添加到配置列表。\n" : "") +
                    "请在模板中实现 TODO 标记的部分。", 
                    "确定");

                Debug.Log($"[EUUI] 扩展模板已创建: {fullPath}");
                
                // 重置表单
                _extensionName = "";
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("创建失败", 
                    $"创建扩展模板失败：\n{e.Message}", 
                    "确定");
                Debug.LogError($"[EUUI] 创建扩展模板失败: {e}");
            }
        }
    }
}
#endif
