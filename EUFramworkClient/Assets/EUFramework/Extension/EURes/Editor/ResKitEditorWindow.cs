#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using YooAsset.Editor;
using YooAsset;
using EUFramework.Extension.EURes;

namespace EUFramework.Extension.EURes.Editor
{
    public class ResKitEditorWindow : EditorWindow
    {
        private ResServerConfig _resServerConfig;
        private AssetBundleCollectorSetting _collectorSetting;
        private ScriptableObject _yooAssetSettings; // YooAssetSettings 是 internal，用 ScriptableObject 引用
        private ResKitPackageConfig _packageConfig;
        private const string SETTINGS_PATH = "Assets/EUFramework/Resources/ResKitSettings";
        
        // 记录哪个配置面板被展开
        private bool _showResServerConfig = false;
        private bool _showYooAssetSettings = false;
        private bool _showPackageConfig = false;
        
        // 当前选中的按钮
        private Button _selectedButton;
        
        [MenuItem("EUFramework/拓展/ResKit 配置工具", priority = 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<ResKitEditorWindow>();
            window.titleContent = new GUIContent("ResKit 配置工具");
            
            // 设置窗口大小（扩大100px）
            Vector2 windowSize = new Vector2(900, 700);
            window.minSize = windowSize;
            
            // 居中显示窗口
            var mainWindowPos = EditorGUIUtility.GetMainWindowPosition();
            var centerX = mainWindowPos.x + (mainWindowPos.width - windowSize.x) * 0.5f;
            var centerY = mainWindowPos.y + (mainWindowPos.height - windowSize.y) * 0.5f;
            window.position = new Rect(centerX, centerY, windowSize.x, windowSize.y);
        }

        private void CreateGUI()
        {
            // 加载 UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/EUFramework/Extension/EURes/Editor/UI/ResKitEditorWindow.uxml");
            
            if (visualTree != null)
            {
                visualTree.CloneTree(rootVisualElement);
            }
            else
            {
                CreateFallbackUI();
                return;
            }

            // 加载样式
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/EUFramework/Extension/EURes/Editor/UI/ResKitEditorWindow.uss");
            
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            // 绑定按钮事件
            BindButtons();
            
            // 初始加载配置并显示状态
            LoadConfigs();
            
            // 默认选中"配置文件"
            var btnConfigFiles = rootVisualElement.Q<Button>("btn-config-files");
            if (btnConfigFiles != null)
            {
                SetSelectedButton(btnConfigFiles);
            }
            
            ShowFileStatusPanel();
        }

        private void LoadConfigs()
        {
            // 加载 ResServerConfig
            string resServerPath = Path.Combine(SETTINGS_PATH, "ResServerConfig.asset");
            _resServerConfig = AssetDatabase.LoadAssetAtPath<ResServerConfig>(resServerPath);
            
            // 加载 AssetBundleCollectorSetting
            string collectorPath = Path.Combine(SETTINGS_PATH, "AssetBundleCollectorSetting.asset");
            _collectorSetting = AssetDatabase.LoadAssetAtPath<AssetBundleCollectorSetting>(collectorPath);
            
            // 加载 YooAssetSettings
            string yooSettingsPath = Path.Combine(SETTINGS_PATH, "YooAssetSettings.asset");
            _yooAssetSettings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(yooSettingsPath);
            
            // 加载 ResKitPackageConfig
            string packageConfigPath = Path.Combine(SETTINGS_PATH, "ResKitPackageConfig.asset");
            _packageConfig = AssetDatabase.LoadAssetAtPath<ResKitPackageConfig>(packageConfigPath);
        }

        /// <summary>
        /// 创建内容区域标题
        /// </summary>
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
        
        private void ShowFileStatusPanel()
        {
            var contentArea = rootVisualElement.Q<VisualElement>("content-area");
            if (contentArea == null) return;
            
            contentArea.Clear();
            
            // 设置 contentArea 从左上角开始对齐
            contentArea.style.alignItems = Align.FlexStart;
            contentArea.style.justifyContent = Justify.FlexStart;
            
            // 添加标题
            var header = CreateContentHeader("配置文件管理", "管理 ResKit 所需的各项配置文件");
            contentArea.Add(header);
            
            // 创建 IMGUIContainer 来显示文件状态和配置编辑
            var imguiContainer = new IMGUIContainer(() =>
            {
                DrawFileStatusAndConfig();
            });
            
            // 设置 IMGUIContainer 占满整个区域且从左上角开始
            imguiContainer.style.width = Length.Percent(100);
            imguiContainer.style.flexGrow = 1;
            
            contentArea.Add(imguiContainer);
        }
        
        private void DrawFileStatusAndConfig()
        {
            // 绘制文件状态
            DrawFileStatusPanel();
            
            // 如果有展开的配置，在下方绘制
            if (_showResServerConfig || _showYooAssetSettings || _showPackageConfig)
            {
                GUILayout.Space(20);
                DrawConfigEditPanel();
            }
        }

        private void DrawFileStatusPanel()
        {
            GUILayout.BeginVertical();
            GUILayout.Space(5);
            
            // 检查 AssetBundleCollectorSetting
            string collectorPath = Path.Combine(SETTINGS_PATH, "AssetBundleCollectorSetting.asset");
            bool collectorExists = File.Exists(collectorPath);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("AssetBundleCollectorSetting:", GUILayout.Width(250));
            if (collectorExists)
            {
                GUILayout.Label("✓ 已创建", EditorStyles.boldLabel);
                if (GUILayout.Button("配置资源收集", GUILayout.Width(150)))
                {
                    OpenAssetBundleCollectorWindow();
                }
            }
            else
            {
                GUILayout.Label("✗ 未创建", EditorStyles.boldLabel);
                if (GUILayout.Button("创建配置文件", GUILayout.Width(150)))
                {
                    CreateAssetBundleCollectorSetting(SETTINGS_PATH);
                    LoadConfigs();
                    ShowFileStatusPanel();
                }
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // 检查 ResServerConfig
            string resServerPath = Path.Combine(SETTINGS_PATH, "ResServerConfig.asset");
            bool resServerExists = File.Exists(resServerPath);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("ResServerConfig:", GUILayout.Width(250));
            if (resServerExists)
            {
                GUILayout.Label("✓ 已创建", EditorStyles.boldLabel);
                string buttonText = _showResServerConfig ? "收起配置" : "配置服务器信息";
                if (GUILayout.Button(buttonText, GUILayout.Width(150)))
                {
                    _showResServerConfig = !_showResServerConfig;
                    _showYooAssetSettings = false;
                    _showPackageConfig = false;
                }
            }
            else
            {
                GUILayout.Label("✗ 未创建", EditorStyles.boldLabel);
                if (GUILayout.Button("创建配置文件", GUILayout.Width(150)))
                {
                    CreateResServerConfig(SETTINGS_PATH);
                    LoadConfigs();
                }
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // 检查 YooAssetSettings
            string yooSettingsPath = Path.Combine(SETTINGS_PATH, "YooAssetSettings.asset");
            bool yooSettingsExists = File.Exists(yooSettingsPath);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("YooAssetSettings:", GUILayout.Width(250));
            if (yooSettingsExists)
            {
                GUILayout.Label("✓ 已创建", EditorStyles.boldLabel);
                string buttonText = _showYooAssetSettings ? "收起配置" : "配置 YooAsset 设置";
                if (GUILayout.Button(buttonText, GUILayout.Width(150)))
                {
                    _showYooAssetSettings = !_showYooAssetSettings;
                    _showResServerConfig = false;
                    _showPackageConfig = false;
                }
            }
            else
            {
                GUILayout.Label("✗ 未创建", EditorStyles.boldLabel);
                if (GUILayout.Button("创建配置文件", GUILayout.Width(150)))
                {
                    CreateYooAssetSettings(SETTINGS_PATH);
                    LoadConfigs();
                }
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // 检查 ResKitPackageConfig
            string packageConfigPath = Path.Combine(SETTINGS_PATH, "ResKitPackageConfig.asset");
            bool packageConfigExists = File.Exists(packageConfigPath);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("ResKitPackageConfig:", GUILayout.Width(250));
            if (packageConfigExists)
            {
                GUILayout.Label("✓ 已创建", EditorStyles.boldLabel);
                string buttonText = _showPackageConfig ? "收起配置" : "配置 Package 信息";
                if (GUILayout.Button(buttonText, GUILayout.Width(150)))
                {
                    _showPackageConfig = !_showPackageConfig;
                    _showResServerConfig = false;
                    _showYooAssetSettings = false;
                }
            }
            else
            {
                GUILayout.Label("✗ 未创建", EditorStyles.boldLabel);
                if (GUILayout.Button("创建配置文件", GUILayout.Width(150)))
                {
                    CreateResKitPackageConfig(SETTINGS_PATH);
                    LoadConfigs();
                }
            }
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
        }

        private void DrawConfigEditPanel()
        {
            GUILayout.BeginVertical("box");
            
            // ResServerConfig 配置编辑
            if (_showResServerConfig && _resServerConfig != null)
            {
                DrawResServerConfigPanel();
            }
            
            // YooAssetSettings 配置编辑
            if (_showYooAssetSettings && _yooAssetSettings != null)
            {
                DrawYooAssetSettingsPanel();
            }
            
            // PackageConfig 配置编辑
            if (_showPackageConfig && _packageConfig != null)
            {
                DrawPackageConfigPanel();
            }
            
            GUILayout.EndVertical();
        }
        
        private void DrawResServerConfigPanel()
        {
            GUILayout.Label("资源服务器配置", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            EditorGUI.BeginChangeCheck();
            
            _resServerConfig.protocol = (ServerProtocol)EditorGUILayout.EnumPopup("协议类型", _resServerConfig.protocol);
            
            if (_resServerConfig.protocol == ServerProtocol.Custom)
            {
                _resServerConfig.customUrl = EditorGUILayout.TextField("自定义URL", _resServerConfig.customUrl);
            }
            else
            {
                _resServerConfig.hostServer = EditorGUILayout.TextField("服务器地址", _resServerConfig.hostServer);
                _resServerConfig.port = EditorGUILayout.IntSlider("端口号", _resServerConfig.port, 1, 65535);
            }
            
            _resServerConfig.appVersion = EditorGUILayout.TextField("应用版本", _resServerConfig.appVersion);
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_resServerConfig);
                AssetDatabase.SaveAssets();
            }
            
            GUILayout.Space(10);
            EditorGUILayout.HelpBox($"完整服务器地址: {_resServerConfig.GetServerUrl()}", MessageType.Info);
        }
        
        private void DrawYooAssetSettingsPanel()
        {
            GUILayout.Label("YooAsset 设置", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            EditorGUI.BeginChangeCheck();
            
            var so = new SerializedObject(_yooAssetSettings);
            var folderNameProp = so.FindProperty("DefaultYooFolderName");
            var manifestPrefixProp = so.FindProperty("PackageManifestPrefix");
            
            if (folderNameProp != null)
                EditorGUILayout.PropertyField(folderNameProp, new GUIContent("YooAsset 文件夹名称"));
            
            if (manifestPrefixProp != null)
                EditorGUILayout.PropertyField(manifestPrefixProp, new GUIContent("资源清单前缀"));
            
            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(_yooAssetSettings);
                AssetDatabase.SaveAssets();
            }
            
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("YooAsset 文件夹名称用于缓存和资源目录，清单前缀用于多包配置", MessageType.Info);
        }
        
        private void DrawPackageConfigPanel()
        {
            GUILayout.Label("Package 运行配置（仅配置模式，不可添加/删除）", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            EditorGUILayout.HelpBox(
                "📋 配置说明：\n" +
                "• 本界面仅用于配置 Package 的运行参数\n" +
                "• Package 列表完全由 AssetBundleCollector 管理\n" +
                "• 不支持手动添加、删除或重命名 Package\n" +
                "• 可配置项：运行模式（PlayMode）、默认包设置", 
                MessageType.Info);
            
            GUILayout.Space(10);
            
            // 数据管理按钮
            GUILayout.Label("数据管理", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 从 AssetBundleCollector 同步", GUILayout.Height(35)))
            {
                SyncPackagesFromCollector();
            }
            if (GUILayout.Button("✓ 验证数据一致性", GUILayout.Height(35)))
            {
                ValidatePackagesWithCollector();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // 清理工具
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("🧹 清理重复数据", GUILayout.Width(150), GUILayout.Height(25)))
            {
                CleanDuplicatePackages();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // 检查是否有 Collector 和包
            if (_collectorSetting == null || _collectorSetting.Packages == null || _collectorSetting.Packages.Count == 0)
            {
                EditorGUILayout.HelpBox("暂未配置包信息，请先在 AssetBundleCollector 中配置 Package", MessageType.Warning);
                return;
            }
            
            var packages = _packageConfig.GetAllPackages();
            if (packages == null || packages.Count == 0)
            {
                EditorGUILayout.HelpBox("暂未配置包信息，请点击上方\"从 AssetBundleCollector 同步\"按钮同步 Package", MessageType.Warning);
                return;
            }
            
            // 自定义绘制 Package 列表
            EditorGUI.BeginChangeCheck();
            
            for (int i = 0; i < packages.Count; i++)
            {
                var pkg = packages[i];
                
                GUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Package {i + 1}", EditorStyles.boldLabel, GUILayout.Width(100));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                // Package 名称（只读显示）
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Package 名称", pkg.packageName);
                EditorGUI.EndDisabledGroup();
                
                // 运行模式（可编辑）
                pkg.playMode = (EPlayMode)EditorGUILayout.EnumPopup("运行模式", pkg.playMode);
                
                // 是否为默认包（单选）
                bool newIsDefault = EditorGUILayout.Toggle("是否为默认包", pkg.isDefault);
                if (newIsDefault != pkg.isDefault)
                {
                    if (newIsDefault)
                    {
                        // 取消其他所有包的默认状态
                        foreach (var otherPkg in packages)
                        {
                            if (otherPkg != pkg)
                            {
                                otherPkg.isDefault = false;
                            }
                        }
                    }
                    pkg.isDefault = newIsDefault;
                }
                
                // 包描述（只读显示，从 Collector 同步）
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("包描述", pkg.description);
                EditorGUI.EndDisabledGroup();
                
                GUILayout.EndVertical();
                GUILayout.Space(5);
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_packageConfig);
                AssetDatabase.SaveAssets();
            }
            
            GUILayout.Space(10);
            
            // 验证按钮
            if (GUILayout.Button("验证配置"))
            {
                if (_packageConfig.Validate(out string errorMessage))
                {
                    EditorUtility.DisplayDialog("验证成功", "Package 配置有效", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("验证失败", errorMessage, "确定");
                }
            }
            
            EditorGUILayout.HelpBox(
                "💡 配置说明：\n" +
                "• Package 名称和描述：由 AssetBundleCollector 管理（只读）\n" +
                "• 运行模式：可配置（EditorSimulate/Offline/Host/WebPlay 等）\n" +
                "• 默认包：只能设置一个默认 Package\n" +
                "• 数据来源：所有 Package 必须从 AssetBundleCollector 同步", 
                MessageType.Info);
        }
        
        private void CleanDuplicatePackages()
        {
            if (_packageConfig == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到 ResKitPackageConfig", "确定");
                return;
            }
            
            int beforeCount = _packageConfig.GetAllPackages().Count;
            _packageConfig.RemoveDuplicatePackages();
            int afterCount = _packageConfig.GetAllPackages().Count;
            
            EditorUtility.SetDirty(_packageConfig);
            AssetDatabase.SaveAssets();
            
            if (beforeCount > afterCount)
            {
                EditorUtility.DisplayDialog("清理完成", 
                    $"已清理重复的 Package\n\n" +
                    $"清理前: {beforeCount} 个\n" +
                    $"清理后: {afterCount} 个\n" +
                    $"移除: {beforeCount - afterCount} 个重复项", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("清理完成", "没有发现重复的 Package", "确定");
            }
        }
        
        private void SyncPackagesFromCollector()
        {
            if (_collectorSetting == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到 AssetBundleCollectorSetting，请先创建", "确定");
                return;
            }
            
            if (_packageConfig == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到 ResKitPackageConfig", "确定");
                return;
            }
            
            var collectorPackages = _collectorSetting.Packages;
            if (collectorPackages == null || collectorPackages.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "AssetBundleCollectorSetting 中没有配置任何 Package", "确定");
                return;
            }
            
            // 同步前先清理重复的包
            _packageConfig.RemoveDuplicatePackages();
            
            bool confirm = EditorUtility.DisplayDialog("同步确认", 
                $"将从 AssetBundleCollectorSetting 同步 {collectorPackages.Count} 个 Package。\n\n" +
                "已存在的 Package 会保留其配置（PlayMode、IsDefault）。\n" +
                "新 Package 将使用默认配置。\n" +
                "不存在于 Collector 的 Package 将被移除。\n\n" +
                "是否继续？", "确定", "取消");
            
            if (!confirm) return;
            
            // 执行同步
            int addedCount = 0;
            int updatedCount = 0;
            int removedCount = 0;
            
            // 创建 Collector 中的包名集合
            var collectorPackageNames = new HashSet<string>(
                collectorPackages.Select(p => p.PackageName)
            );
            
            // 移除不存在的包
            var configPackages = _packageConfig.GetAllPackages();
            var packagesToRemove = new List<string>();
            
            foreach (var pkg in configPackages)
            {
                if (!collectorPackageNames.Contains(pkg.packageName))
                {
                    packagesToRemove.Add(pkg.packageName);
                }
            }
            
            foreach (var packageName in packagesToRemove)
            {
                _packageConfig.RemovePackage(packageName);
                removedCount++;
            }
            
            // 添加或更新包
            bool hasDefaultPackage = configPackages.Any(p => p.isDefault && collectorPackageNames.Contains(p.packageName));
            
            foreach (var collectorPkg in collectorPackages)
            {
                var existingPkg = _packageConfig.GetPackage(collectorPkg.PackageName);
                if (existingPkg != null)
                {
                    // 更新描述
                    existingPkg.description = collectorPkg.PackageDesc;
                    updatedCount++;
                }
                else
                {
                    // 添加新 Package，如果还没有默认包，第一个设为默认
                    _packageConfig.AddPackage(
                        collectorPkg.PackageName, 
                        EPlayMode.EditorSimulateMode, 
                        !hasDefaultPackage && addedCount == 0
                    );
                    
                    // 更新描述
                    var newPkg = _packageConfig.GetPackage(collectorPkg.PackageName);
                    if (newPkg != null)
                    {
                        newPkg.description = collectorPkg.PackageDesc;
                        if (!hasDefaultPackage && addedCount == 0)
                        {
                            hasDefaultPackage = true;
                        }
                    }
                    
                    addedCount++;
                }
            }
            
            EditorUtility.SetDirty(_packageConfig);
            AssetDatabase.SaveAssets();
            
            string message = "同步完成！\n\n";
            if (addedCount > 0) message += $"新增: {addedCount} 个\n";
            if (updatedCount > 0) message += $"更新: {updatedCount} 个\n";
            if (removedCount > 0) message += $"移除: {removedCount} 个\n";
            
            EditorUtility.DisplayDialog("同步完成", message, "确定");
        }
        
        private void ValidatePackagesWithCollector()
        {
            if (_collectorSetting == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到 AssetBundleCollectorSetting，请先创建", "确定");
                return;
            }
            
            if (_packageConfig == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到 ResKitPackageConfig", "确定");
                return;
            }
            
            var configPackages = _packageConfig.GetAllPackages();
            var collectorPackages = _collectorSetting.Packages;
            
            var collectorPackageNames = new HashSet<string>(
                collectorPackages.Select(p => p.PackageName)
            );
            
            var matchedPackages = new List<string>();
            var unmatchedPackages = new List<string>();
            
            foreach (var pkg in configPackages)
            {
                if (collectorPackageNames.Contains(pkg.packageName))
                {
                    matchedPackages.Add(pkg.packageName);
                }
                else
                {
                    unmatchedPackages.Add(pkg.packageName);
                }
            }
            
            var missingInConfig = new List<string>();
            foreach (var collectorPkg in collectorPackages)
            {
                bool existsInConfig = configPackages.Any(p => p.packageName == collectorPkg.PackageName);
                if (!existsInConfig)
                {
                    missingInConfig.Add(collectorPkg.PackageName);
                }
            }
            
            string message = "验证结果：\n\n";
            
            if (matchedPackages.Count() > 0)
            {
                message += $"✓ 匹配成功 ({matchedPackages.Count()} 个):\n";
                foreach (var name in matchedPackages)
                {
                    message += $"  • {name}\n";
                }
                message += "\n";
            }
            
            if (unmatchedPackages.Count() > 0)
            {
                message += $"✗ 未在 Collector 中找到 ({unmatchedPackages.Count()} 个):\n";
                foreach (var name in unmatchedPackages)
                {
                    message += $"  • {name}\n";
                }
                message += "\n";
            }
            
            if (missingInConfig.Count() > 0)
            {
                message += $"⚠ Collector 中存在但未配置 ({missingInConfig.Count()} 个):\n";
                foreach (var name in missingInConfig)
                {
                    message += $"  • {name}\n";
                }
                message += "\n建议点击\"从 AssetBundleCollector 同步\"按钮同步。\n";
            }
            
            if (unmatchedPackages.Count() == 0 && missingInConfig.Count() == 0)
            {
                message += "✓ 所有 Package 完全匹配！";
            }
            
            EditorUtility.DisplayDialog("验证结果", message, "确定");
        }

        private void OpenAssetBundleCollectorWindow()
        {
            // 打开 YooAsset 的 AssetBundle Collector 窗口
            var windowType = System.Type.GetType("YooAsset.Editor.AssetBundleCollectorWindow,YooAsset.Editor");
            if (windowType != null)
            {
                var window = EditorWindow.GetWindow(windowType, false, "AssetBundle Collector", true);
                window.Show();
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "未找到 YooAsset 的 AssetBundleCollectorWindow 窗口类型", "确定");
            }
        }

        private void BindButtons()
        {
            var btnConfigFiles = rootVisualElement.Q<Button>("btn-config-files");
            var btnResFacade = rootVisualElement.Q<Button>("btn-res-facade");

            if (btnConfigFiles != null)
            {
                btnConfigFiles.clicked += () =>
                {
                    SetSelectedButton(btnConfigFiles);
                    ShowFileStatusPanel();
                };
            }
            
            if (btnResFacade != null)
            {
                btnResFacade.clicked += () =>
                {
                    SetSelectedButton(btnResFacade);
                    ShowResFacadePanel();
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
            
            // 添加选中样式到新按钮
            button.AddToClassList("sidebar-button-selected");
            _selectedButton = button;
        }

        private void ShowResFacadePanel()
        {
            var contentArea = rootVisualElement.Q<VisualElement>("content-area");
            if (contentArea == null) return;
            
            contentArea.Clear();
            
            // 设置 contentArea 从左上角开始对齐
            contentArea.style.alignItems = Align.FlexStart;
            contentArea.style.justifyContent = Justify.FlexStart;
            
            // 添加标题
            var header = CreateContentHeader("ResFacade 生成工具", "生成资源管理相关的代码和 UI 预制体");
            contentArea.Add(header);
            
            // 创建 IMGUIContainer 来显示 ResFacade 功能
            var imguiContainer = new IMGUIContainer(() =>
            {
                DrawResFacadePanel();
            });
            
            // 设置 IMGUIContainer 占满整个区域且从左上角开始
            imguiContainer.style.width = Length.Percent(100);
            imguiContainer.style.flexGrow = 1;
            
            contentArea.Add(imguiContainer);
        }
        
        private void DrawResFacadePanel()
        {
            GUILayout.BeginVertical();
            GUILayout.Space(5);
            
            // UI Prefab 生成区域
            GUILayout.Label("UI Prefab 和脚本", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            string prefabPath = "Assets/EUFramework/Resources/ResKitUI/ResKitUserOpePopUp.prefab";
            string scriptPath = "Assets/EUFramework/Extension/EURes/Script/ResKitUserOpePopUp.cs";
            bool prefabExists = File.Exists(prefabPath);
            bool scriptExists = File.Exists(scriptPath);
            
            // 显示脚本状态
            GUILayout.BeginHorizontal();
            GUILayout.Label("ResKitUserOpePopUp.cs:", GUILayout.Width(250));
            if (scriptExists)
            {
                GUILayout.Label("✓ 已生成", EditorStyles.boldLabel);
            }
            else
            {
                GUILayout.Label("✗ 未生成", EditorStyles.boldLabel);
            }
            GUILayout.EndHorizontal();
            
            // 显示 prefab 状态
            GUILayout.BeginHorizontal();
            GUILayout.Label("ResKitUserOpePopUp.prefab:", GUILayout.Width(250));
            if (prefabExists)
            {
                GUILayout.Label("✓ 已生成", EditorStyles.boldLabel);
            }
            else
            {
                GUILayout.Label("✗ 未生成", EditorStyles.boldLabel);
            }
            GUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox("⚠️ 业务脚本：用户可自定义 UI 交互逻辑，请勿覆盖！\nPrefab：位于 Resources/ResKitUI/ 目录", MessageType.Warning);
            
            if (prefabExists && scriptExists)
            {
                // 业务脚本和 Prefab 都存在，只提供定位功能
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("📍 定位到脚本", GUILayout.Height(40)))
                {
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                    EditorGUIUtility.PingObject(script);
                    Selection.activeObject = script;
                }
                if (GUILayout.Button("📍 定位到 Prefab", GUILayout.Height(40)))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    EditorGUIUtility.PingObject(prefab);
                    Selection.activeObject = prefab;
                }
                GUILayout.EndHorizontal();
            }
            else if (scriptExists && !prefabExists)
            {
                EditorGUILayout.HelpBox("脚本已存在，但 Prefab 未生成", MessageType.Warning);
                if (GUILayout.Button("生成 Prefab（保留现有脚本）", GUILayout.Height(40)))
                {
                    OnCreatePrefabClicked();
                }
            }
            else if (!scriptExists && prefabExists)
            {
                EditorGUILayout.HelpBox("Prefab 已存在，但脚本未生成", MessageType.Warning);
                if (GUILayout.Button("生成脚本并重新创建 Prefab", GUILayout.Height(40)))
                {
                    OnCreatePrefabClicked();
                }
            }
            else
            {
                if (GUILayout.Button("生成 UI Prefab 和脚本", GUILayout.Height(40)))
                {
                    OnCreatePrefabClicked();
                }
            }
            
            GUILayout.Space(20);
            
            // ResKit 分部类生成区域（同时生成）
            GUILayout.Label("ResKit 分部类（Partial Class）", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            string codeGeneratedPath = "Assets/EUFramework/Extension/EURes/Script/Generated/ResKit.Generated.cs";
            string codeUserPath = "Assets/EUFramework/Extension/EURes/Script/ResKit.cs";
            bool codeGeneratedExists = File.Exists(codeGeneratedPath);
            bool codeUserExists = File.Exists(codeUserPath);
            bool bothExist = codeGeneratedExists && codeUserExists;
            
            // 显示两个文件的状态
            GUILayout.BeginVertical("box");
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("ResKit.Generated.cs:", GUILayout.Width(200));
            if (codeGeneratedExists)
            {
                GUILayout.Label("✓ 已生成", EditorStyles.boldLabel);
            }
            else
            {
                GUILayout.Label("✗ 未生成", EditorStyles.boldLabel);
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("ResKit.cs:", GUILayout.Width(200));
            if (codeUserExists)
            {
                GUILayout.Label("✓ 已生成", EditorStyles.boldLabel);
            }
            else
            {
                GUILayout.Label("✗ 未生成", EditorStyles.boldLabel);
            }
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            
            EditorGUILayout.HelpBox(
                "📋 分部类说明：\n" +
                "• ResKit.Generated.cs - 自动生成的基础工具类（可重新生成）\n" +
                "• ResKit.cs - 用户编辑的业务逻辑类（请勿覆盖）\n" +
                "• 两个文件作为 partial class 相互引用，必须同时存在", 
                MessageType.Info);
            
            if (bothExist)
            {
                // 两个文件都存在
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("📍 定位到 Generated", GUILayout.Height(35)))
                {
                    var script = AssetDatabase.LoadAssetAtPath<TextAsset>(codeGeneratedPath);
                    EditorGUIUtility.PingObject(script);
                    Selection.activeObject = script;
                }
                if (GUILayout.Button("📍 定位到用户脚本", GUILayout.Height(35)))
                {
                    var script = AssetDatabase.LoadAssetAtPath<TextAsset>(codeUserPath);
                    EditorGUIUtility.PingObject(script);
                    Selection.activeObject = script;
                }
                GUILayout.EndHorizontal();
                
                if (GUILayout.Button("🔄 重新生成 Generated 部分", GUILayout.Height(35)))
                {
                    if (EditorUtility.DisplayDialog("确认", 
                        "是否重新生成 ResKit.Generated.cs？\n\n" +
                        "ResKit.cs（用户脚本）不会被修改", 
                        "确定", "取消"))
                    {
                        OnGenerateResKitGeneratedOnly();
                    }
                }
            }
            else if (codeUserExists && !codeGeneratedExists)
            {
                // 只有用户脚本存在
                EditorGUILayout.HelpBox("⚠️ 缺少 Generated 部分，可能导致编译错误！", MessageType.Warning);
                if (GUILayout.Button("生成 ResKit.Generated.cs", GUILayout.Height(40)))
                {
                    OnGenerateResKitGeneratedOnly();
                }
            }
            else if (!codeUserExists && codeGeneratedExists)
            {
                // 只有 Generated 存在
                EditorGUILayout.HelpBox("⚠️ 缺少用户脚本部分，可能导致编译错误！", MessageType.Warning);
                if (GUILayout.Button("生成 ResKit.cs", GUILayout.Height(40)))
                {
                    OnGenerateUserResKitClicked();
                }
            }
            else
            {
                // 都不存在
                EditorGUILayout.HelpBox("⚠️ ResKit 分部类尚未生成", MessageType.Warning);
                if (GUILayout.Button("🎯 生成 ResKit 分部类（同时生成两个文件）", GUILayout.Height(40)))
                {
                    OnGenerateBothResKitFiles();
                }
            }
            
            GUILayout.Space(20);
            
            // 程序集引用管理区域
            GUILayout.Label("程序集引用管理", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            EditorGUILayout.HelpBox("刷新 YooAsset 和 UniTask 的程序集引用，解决引用丢失问题", MessageType.Info);
            
            if (GUILayout.Button("🔄 刷新程序集引用", GUILayout.Height(40)))
            {
                RefreshAssemblyReferences();
            }
            
            GUILayout.EndVertical();
        }

        #region 生成操作

        private void OnCreatePrefabClicked()
        {
            // 1. 先生成 ResKitUserOpePopUp.cs 脚本
            string scriptPath = "Assets/EUFramework/Extension/EURes/Script/ResKitUserOpePopUp.cs";
            bool scriptGenerated = GenerateResKitUserOpePopUpScript(scriptPath);
            
            if (!scriptGenerated)
            {
                EditorUtility.DisplayDialog("错误", "ResKitUserOpePopUp.cs 脚本生成失败，无法继续", "确定");
                return;
            }
            
            // 刷新资源数据库以编译新脚本
            AssetDatabase.Refresh();
            
            // 等待编译完成
            System.Threading.Thread.Sleep(500);
            
            // 2. 创建 Prefab
            string prefabPath = "Assets/EUFramework/Resources/ResKitUI";
            
            if (!Directory.Exists(prefabPath))
            {
                Directory.CreateDirectory(prefabPath);
                AssetDatabase.Refresh();
            }

            string fullPath = Path.Combine(prefabPath, "ResKitUserOpePopUp.prefab");

            // 创建默认的弹窗预制体
            GameObject popup = CreateDefaultPopupPrefab();
            
            // 保存为预制体
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(popup, fullPath);
            DestroyImmediate(popup);

            // 添加 ResKitUserOpePopUp 组件到 prefab
            AddResKitUserOpePopUpComponent(prefabAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // 选中创建的预制体
            EditorGUIUtility.PingObject(prefabAsset);
            Selection.activeObject = prefabAsset;
            
            EditorUtility.DisplayDialog("成功", 
                $"UI Prefab 和脚本创建完成！\n\n" +
                $"Prefab 路径: {fullPath}\n" +
                $"脚本路径: {scriptPath}\n\n" +
                $"已自动添加并绑定 ResKitUserOpePopUp 组件", 
                "确定");
        }
        
        private bool GenerateResKitUserOpePopUpScript(string outputPath)
        {
            string templatePath = "Assets/EUFramework/Extension/EURes/Editor/Templates/ResKitUserOpePopUp.cs.sbn";

            if (!File.Exists(templatePath))
            {
                Debug.LogError($"[ResKit] 模板文件不存在: {templatePath}");
                return false;
            }

            // 读取模板
            string template = File.ReadAllText(templatePath);

            // 替换变量
            string generated = template
                .Replace("{{ namespace }}", "EUFramework.Extension.EURes")
                .Replace("{{ class_name }}", "ResKitUserOpePopUp");

            // 确保输出目录存在
            string outputDir = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // 保存生成的代码
            File.WriteAllText(outputPath, generated);
            Debug.Log($"[ResKit] ResKitUserOpePopUp.cs 生成成功: {outputPath}");
            
            return true;
        }
        
        private void AddResKitUserOpePopUpComponent(GameObject prefabAsset)
        {
            // 使用反射添加组件，避免直接引用运行时类型
            var assemblyName = "EURes";
            var typeName = "EUFramework.Extension.EURes.ResKitUserOpePopUp";
            
            var assembly = System.AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName);
            
            if (assembly != null)
            {
                var componentType = assembly.GetType(typeName);
                if (componentType != null)
                {
                    var component = prefabAsset.AddComponent(componentType);
                    EditorUtility.SetDirty(prefabAsset);
                    Debug.Log($"[ResKit] 已添加 {typeName} 组件到 Prefab");
                }
                else
                {
                    Debug.LogWarning($"[ResKit] 未找到类型 {typeName}，请确保 ResKitUserOpePopUp.cs 已编译");
                }
            }
            else
            {
                Debug.LogWarning($"[ResKit] 未找到程序集 {assemblyName}");
            }
        }

        /// <summary>
        /// 同时生成 ResKit 的两个分部类文件
        /// </summary>
        private void OnGenerateBothResKitFiles()
        {
            bool generatedSuccess = OnGenerateResKitGeneratedOnly();
            if (!generatedSuccess)
            {
                return;
            }
            
            bool userSuccess = OnGenerateResKitUserOnly();
            if (!userSuccess)
            {
                return;
            }
            
            EditorUtility.DisplayDialog("生成完成", 
                "ResKit 分部类已生成完成！\n\n" +
                "✓ ResKit.Generated.cs（自动生成）\n" +
                "✓ ResKit.cs（用户编辑）\n\n" +
                "两个文件作为 partial class 相互引用，已同时创建", 
                "确定");
        }
        
        /// <summary>
        /// 只生成 ResKit.Generated.cs（自动生成部分）
        /// </summary>
        private bool OnGenerateResKitGeneratedOnly()
        {
            string templatePath = "Assets/EUFramework/Extension/EURes/Editor/Templates/DefaultResKit.Generated.sbn";
            string outputPath = "Assets/EUFramework/Extension/EURes/Script/Generated/ResKit.Generated.cs";

            if (!File.Exists(templatePath))
            {
                EditorUtility.DisplayDialog("错误", $"模板文件不存在！\n\n路径: {templatePath}", "确定");
                return false;
            }

            // 读取模板
            string template = File.ReadAllText(templatePath);

            // 替换变量
            string generated = template
                .Replace("{{ namespace }}", "EUFramework.Extension.EURes")
                .Replace("{{ class_name }}", "ResKit");

            // 确保输出目录存在
            string outputDir = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // 保存生成的代码
            File.WriteAllText(outputPath, generated);
            AssetDatabase.Refresh();

            // 选中生成的文件
            var script = AssetDatabase.LoadAssetAtPath<TextAsset>(outputPath);
            if (script != null)
            {
                EditorGUIUtility.PingObject(script);
                Selection.activeObject = script;
            }
            
            return true;
        }
        
        /// <summary>
        /// 只生成 ResKit.cs（用户编辑部分）
        /// </summary>
        private bool OnGenerateResKitUserOnly()
        {
            string templatePath = "Assets/EUFramework/Extension/EURes/Editor/Templates/DefaultResKit.cs.sbn";
            string outputPath = "Assets/EUFramework/Extension/EURes/Script/ResKit.cs";

            if (!File.Exists(templatePath))
            {
                EditorUtility.DisplayDialog("错误", $"模板文件不存在！\n\n路径: {templatePath}", "确定");
                return false;
            }

            // 读取模板
            string template = File.ReadAllText(templatePath);

            // 替换变量
            string generated = template
                .Replace("{{ namespace }}", "EUFramework.Extension.EURes")
                .Replace("{{ class_name }}", "ResKit");

            // 确保输出目录存在
            string outputDir = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // 保存生成的代码
            File.WriteAllText(outputPath, generated);
            AssetDatabase.Refresh();

            // 选中生成的文件
            var script = AssetDatabase.LoadAssetAtPath<TextAsset>(outputPath);
            if (script != null)
            {
                EditorGUIUtility.PingObject(script);
                Selection.activeObject = script;
            }
            
            return true;
        }
        
        /// <summary>
        /// 生成用户脚本（兼容性方法，调用新方法）
        /// </summary>
        private void OnGenerateUserResKitClicked()
        {
            OnGenerateResKitUserOnly();
        }

        #endregion

        #region 创建配置文件

        private void CreateAssetBundleCollectorSetting(string basePath)
        {
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
                AssetDatabase.Refresh();
            }
            
            string path = Path.Combine(basePath, "AssetBundleCollectorSetting.asset");
            
            var existing = AssetDatabase.LoadAssetAtPath<AssetBundleCollectorSetting>(path);
            if (existing != null)
            {
                Debug.Log($"[ResKit] AssetBundleCollectorSetting 已存在: {path}");
                EditorGUIUtility.PingObject(existing);
                _collectorSetting = existing;
                return;
            }

            var setting = ScriptableObject.CreateInstance<AssetBundleCollectorSetting>();
            AssetDatabase.CreateAsset(setting, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ResKit] AssetBundleCollectorSetting 创建成功: {path}");
            _collectorSetting = setting;
        }

        private void CreateResServerConfig(string basePath)
        {
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
                AssetDatabase.Refresh();
            }
            
            string path = Path.Combine(basePath, "ResServerConfig.asset");
            
            var existing = AssetDatabase.LoadAssetAtPath<ResServerConfig>(path);
            if (existing != null)
            {
                Debug.Log($"[ResKit] ResServerConfig 已存在: {path}");
                EditorGUIUtility.PingObject(existing);
                _resServerConfig = existing;
                return;
            }

            var config = ScriptableObject.CreateInstance<ResServerConfig>();
            config.protocol = ServerProtocol.HTTP;
            config.hostServer = "127.0.0.1";
            config.port = 80;
            config.appVersion = "1.0.0";
            
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ResKit] ResServerConfig 创建成功: {path}");
            _resServerConfig = config;
        }
        
        private void CreateYooAssetSettings(string basePath)
        {
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
                AssetDatabase.Refresh();
            }
            
            string path = Path.Combine(basePath, "YooAssetSettings.asset");
            
            var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (existing != null)
            {
                Debug.Log($"[ResKit] YooAssetSettings 已存在: {path}");
                EditorGUIUtility.PingObject(existing);
                _yooAssetSettings = existing;
                return;
            }

            // 使用反射创建 YooAssetSettings（因为是 internal 类）
            var yooAssetSettingsType = typeof(YooAssets).Assembly.GetType("YooAsset.YooAssetSettings");
            if (yooAssetSettingsType != null)
            {
                var settings = ScriptableObject.CreateInstance(yooAssetSettingsType);
                
                // 设置默认值
                var folderNameField = yooAssetSettingsType.GetField("DefaultYooFolderName");
                var manifestPrefixField = yooAssetSettingsType.GetField("PackageManifestPrefix");
                
                if (folderNameField != null)
                    folderNameField.SetValue(settings, "yoo");
                if (manifestPrefixField != null)
                    manifestPrefixField.SetValue(settings, string.Empty);
                
                AssetDatabase.CreateAsset(settings, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[ResKit] YooAssetSettings 创建成功: {path}");
                _yooAssetSettings = settings;
            }
            else
            {
                Debug.LogError("[ResKit] 无法找到 YooAsset.YooAssetSettings 类型");
            }
        }
        
        private void CreateResKitPackageConfig(string basePath)
        {
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
                AssetDatabase.Refresh();
            }
            
            string path = Path.Combine(basePath, "ResKitPackageConfig.asset");
            
            var existing = AssetDatabase.LoadAssetAtPath<ResKitPackageConfig>(path);
            if (existing != null)
            {
                Debug.Log($"[ResKit] ResKitPackageConfig 已存在: {path}");
                EditorGUIUtility.PingObject(existing);
                _packageConfig = existing;
                return;
            }

            var config = ScriptableObject.CreateInstance<ResKitPackageConfig>();
            
            // 注意：创建时不添加默认 Package，应该从 AssetBundleCollector 同步
            // 如果需要默认配置，请在创建后使用"从 AssetBundleCollector 同步"功能
            
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ResKit] ResKitPackageConfig 创建成功: {path}");
            _packageConfig = config;
        }

        #endregion

        #region 创建默认 UI Prefab

        private GameObject CreateDefaultPopupPrefab()
        {
            // 创建根对象
            GameObject root = new GameObject("ResKitUserOpePopUp");
            
            // 添加 Canvas 组件
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            var canvasScaler = root.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            
            root.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // 创建背景面板
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);
            
            var rectTransform = panel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(600, 400);
            
            var image = panel.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            // 创建标题
            CreateText(panel.transform, "Title", "提示标题-Text", new Vector2(0, 150), 24);

            // 创建内容
            CreateText(panel.transform, "Content", "context -Text 居中", new Vector2(0, 0), 18);

            // 创建按钮
            CreateButton(panel.transform, "BtnConfirm", "按钮 确认", new Vector2(-100, -120));
            CreateButton(panel.transform, "BtnCancel", "按钮取消", new Vector2(100, -120));

            return root;
        }

        private void CreateText(Transform parent, string name, string text, Vector2 position, int fontSize)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            
            var rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(500, 50);
            
            var textComponent = textObj.AddComponent<UnityEngine.UI.Text>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.white;
            
            // 使用 Unity 默认字体（LegacyRuntime.ttf 适用于新版本 Unity）
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void CreateButton(Transform parent, string name, string text, Vector2 position)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);
            
            var rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(150, 40);
            
            var image = buttonObj.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.8f, 0.4f, 0.2f, 1f);
            
            buttonObj.AddComponent<UnityEngine.UI.Button>();

            // 创建按钮文本
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            var textComponent = textObj.AddComponent<UnityEngine.UI.Text>();
            textComponent.text = text;
            textComponent.fontSize = 16;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.white;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        
        /// <summary>
        /// 刷新程序集引用（YooAsset 和 UniTask）
        /// </summary>
        private void RefreshAssemblyReferences()
        {
            try
            {
                Debug.Log("[ResKit] 开始刷新程序集引用...");
                
                // 1. 刷新 AssetDatabase
                AssetDatabase.Refresh();
                
                // 2. 强制重新导入关键的 asmdef 文件
                string[] asmdefPaths = new[]
                {
                    "Assets/EUFramework/Extension/EURes/EURes.asmdef",
                    "Assets/EUFramework/Extension/EURes/Editor/EURes.Editor.asmdef"
                };
                
                foreach (var path in asmdefPaths)
                {
                    if (File.Exists(path))
                    {
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                        Debug.Log($"[ResKit] 重新导入: {path}");
                    }
                }
                
                // 3. 请求脚本重新编译
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                
                EditorUtility.DisplayDialog("刷新完成", 
                    "程序集引用已刷新！\n\n" +
                    "操作内容：\n" +
                    "1. 刷新 AssetDatabase\n" +
                    "2. 重新导入 .asmdef 文件\n" +
                    "3. 请求脚本重新编译\n\n" +
                    "如果仍有问题，请尝试：\n" +
                    "• 关闭并重新打开 Unity\n" +
                    "• 删除 Library 文件夹后重新打开项目", 
                    "确定");
                
                Debug.Log("[ResKit] ✓ 程序集引用刷新完成");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ResKit] 刷新程序集引用失败: {e.Message}");
                EditorUtility.DisplayDialog("刷新失败", $"刷新程序集引用时出错：\n{e.Message}", "确定");
            }
        }

        #endregion

        #region UI 更新

        private void CreateFallbackUI()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;
            container.style.justifyContent = Justify.Center;
            container.style.alignItems = Align.Center;
            
            var label = new Label("UXML 文件未找到！\n请确保文件存在:\nAssets/EUFramework/Extension/EURes/Editor/UI/ResKitEditorWindow.uxml");
            label.style.fontSize = 16;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.color = new Color(1f, 0.5f, 0.5f);
            
            container.Add(label);
            rootVisualElement.Add(container);
        }

        #endregion
    }
}
#endif
