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
    public class EUResKitEditorWindow : EditorWindow
    {
        private EUResServerConfig _resServerConfig;
        private AssetBundleCollectorSetting _collectorSetting;
        private ScriptableObject _yooAssetSettings; // YooAssetSettings 是 internal，用 ScriptableObject 引用
        private EUResKitPackageConfig _packageConfig;
        
        // 动态路径（通过 EUResKitPathHelper 获取）
        private static string SETTINGS_PATH => EUResKitPathHelper.GetSettingsPath();
        
        // 记录哪个配置面板被展开
        private bool _showEUResServerConfig = false;
        private bool _showYooAssetSettings = false;
        private bool _showPackageConfig = false;
        private bool _showResourceDirectory = false;
        
        // 当前选中的按钮
        private Button _selectedButton;
        
        // 滚动位置
        private Vector2 _resFacadeScrollPos;
        private Vector2 _fileStatusScrollPos;
        
        [MenuItem("EUFramework/拓展/EUResKit 配置工具", priority = 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<EUResKitEditorWindow>();
            window.titleContent = new GUIContent("EUResKit 配置工具");
            
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
            // 加载 UXML（动态路径）
            string uxmlPath = Path.Combine(EUResKitPathHelper.GetEditorPath(), "UI/EUResKitEditorWindow.uxml").Replace("\\", "/");
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

            // 加载样式（动态路径）
            string ussPath = Path.Combine(EUResKitPathHelper.GetEditorPath(), "UI/EUResKitEditorWindow.uss").Replace("\\", "/");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            
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
            // 加载 EUResServerConfig
            string resServerPath = Path.Combine(SETTINGS_PATH, "EUResServerConfig.asset");
            _resServerConfig = AssetDatabase.LoadAssetAtPath<EUResServerConfig>(resServerPath);
            
            // 加载 AssetBundleCollectorSetting
            string collectorPath = Path.Combine(SETTINGS_PATH, "AssetBundleCollectorSetting.asset");
            _collectorSetting = AssetDatabase.LoadAssetAtPath<AssetBundleCollectorSetting>(collectorPath);
            
            // 加载 YooAssetSettings
            string yooSettingsPath = Path.Combine(SETTINGS_PATH, "YooAssetSettings.asset");
            _yooAssetSettings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(yooSettingsPath);
            
            // 加载 EUResKitPackageConfig
            string packageConfigPath = Path.Combine(SETTINGS_PATH, "EUResKitPackageConfig.asset");
            _packageConfig = AssetDatabase.LoadAssetAtPath<EUResKitPackageConfig>(packageConfigPath);
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
            var header = CreateContentHeader("资源配置", "管理资源目录结构和配置文件");
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
            _fileStatusScrollPos = GUILayout.BeginScrollView(_fileStatusScrollPos);
            
            // 绘制文件状态
            DrawFileStatusPanel();
            
            // 如果有展开的配置，在下方绘制
            if (_showEUResServerConfig || _showYooAssetSettings || _showPackageConfig || _showResourceDirectory)
            {
                GUILayout.Space(20);
                DrawConfigEditPanel();
            }
            
            GUILayout.EndScrollView();
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
            
            // 检查 EUResServerConfig
            string resServerPath = Path.Combine(SETTINGS_PATH, "EUResServerConfig.asset");
            bool resServerExists = File.Exists(resServerPath);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("EUResServerConfig:", GUILayout.Width(250));
            if (resServerExists)
            {
                GUILayout.Label("✓ 已创建", EditorStyles.boldLabel);
                string buttonText = _showEUResServerConfig ? "收起配置" : "配置服务器信息";
                if (GUILayout.Button(buttonText, GUILayout.Width(150)))
                {
                    _showEUResServerConfig = !_showEUResServerConfig;
                    _showYooAssetSettings = false;
                    _showPackageConfig = false;
                }
            }
            else
            {
                GUILayout.Label("✗ 未创建", EditorStyles.boldLabel);
                if (GUILayout.Button("创建配置文件", GUILayout.Width(150)))
                {
                    CreateEUResServerConfig(SETTINGS_PATH);
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
                    _showEUResServerConfig = false;
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
            
            // 检查 EUResKitPackageConfig
            string packageConfigPath = Path.Combine(SETTINGS_PATH, "EUResKitPackageConfig.asset");
            bool packageConfigExists = File.Exists(packageConfigPath);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("EUResKitPackageConfig:", GUILayout.Width(250));
            if (packageConfigExists)
            {
                GUILayout.Label("✓ 已创建", EditorStyles.boldLabel);
                string buttonText = _showPackageConfig ? "收起配置" : "配置 Package 信息";
                if (GUILayout.Button(buttonText, GUILayout.Width(150)))
                {
                    _showPackageConfig = !_showPackageConfig;
                    _showEUResServerConfig = false;
                    _showYooAssetSettings = false;
                }
            }
            else
            {
                GUILayout.Label("✗ 未创建", EditorStyles.boldLabel);
                if (GUILayout.Button("创建配置文件", GUILayout.Width(150)))
                {
                    CreateEUResKitPackageConfig(SETTINGS_PATH);
                    LoadConfigs();
                }
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // 检查资源目录关联
            string resRootPath = "Assets/EUResources";
            string builtinPath = Path.Combine(resRootPath, "Builtin");
            string excludedPath = Path.Combine(resRootPath, "Excluded");
            string remotePath = Path.Combine(resRootPath, "Remote");
            
            bool resExists = Directory.Exists(resRootPath);
            bool builtinExists = Directory.Exists(builtinPath);
            bool excludedExists = Directory.Exists(excludedPath);
            bool remoteExists = Directory.Exists(remotePath);
            bool allDirsExist = resExists && builtinExists && excludedExists && remoteExists;
            
            // 检查 Collector 中是否有对应的 Package
            bool hasBuiltinPackage = false;
            bool hasRemotePackage = false;
            int totalPackages = 0;
            
            if (_collectorSetting != null && _collectorSetting.Packages != null)
            {
                totalPackages = _collectorSetting.Packages.Count;
                hasBuiltinPackage = _collectorSetting.Packages.Any(p => p.PackageName == "Builtin");
                hasRemotePackage = _collectorSetting.Packages.Any(p => p.PackageName == "Remote");
            }
            
            bool allPackagesExist = hasBuiltinPackage && hasRemotePackage;
            bool resourceDirComplete = allDirsExist && allPackagesExist;
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("资源目录关联:", GUILayout.Width(250));
            if (resourceDirComplete)
            {
                GUILayout.Label("✓ 已创建", EditorStyles.boldLabel);
                string buttonText = _showResourceDirectory ? "收起配置" : "查看目录状态";
                if (GUILayout.Button(buttonText, GUILayout.Width(150)))
                {
                    _showResourceDirectory = !_showResourceDirectory;
                    _showEUResServerConfig = false;
                    _showYooAssetSettings = false;
                    _showPackageConfig = false;
                }
            }
            else
            {
                GUILayout.Label("✗ 未创建", EditorStyles.boldLabel);
                if (GUILayout.Button("创建资源目录结构", GUILayout.Width(150)))
                {
                    CreateCompleteResStructure();
                    _showResourceDirectory = true;
                    _showEUResServerConfig = false;
                    _showYooAssetSettings = false;
                    _showPackageConfig = false;
                }
            }
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
        }

        private void DrawConfigEditPanel()
        {
            GUILayout.BeginVertical("box");
            
            // EUResServerConfig 配置编辑
            if (_showEUResServerConfig && _resServerConfig != null)
            {
                DrawEUResServerConfigPanel();
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
            
            // 资源目录状态展示
            if (_showResourceDirectory)
            {
                DrawResourceDirectoryPanel();
            }
            
            GUILayout.EndVertical();
        }
        
        private void DrawEUResServerConfigPanel()
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
        
        private void DrawResourceDirectoryPanel()
        {
            GUILayout.Label("资源目录状态", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            // 获取目录状态
            string resRootPath = "Assets/EUResources";
            string builtinPath = Path.Combine(resRootPath, "Builtin");
            string excludedPath = Path.Combine(resRootPath, "Excluded");
            string remotePath = Path.Combine(resRootPath, "Remote");
            
            bool resExists = Directory.Exists(resRootPath);
            bool builtinExists = Directory.Exists(builtinPath);
            bool excludedExists = Directory.Exists(excludedPath);
            bool remoteExists = Directory.Exists(remotePath);
            bool allDirsExist = resExists && builtinExists && excludedExists && remoteExists;
            
            // 检查 Collector 中的 Package
            bool hasBuiltinPackage = false;
            bool hasRemotePackage = false;
            int totalPackages = 0;
            
            if (_collectorSetting != null && _collectorSetting.Packages != null)
            {
                totalPackages = _collectorSetting.Packages.Count;
                hasBuiltinPackage = _collectorSetting.Packages.Any(p => p.PackageName == "Builtin");
                hasRemotePackage = _collectorSetting.Packages.Any(p => p.PackageName == "Remote");
            }
            
            bool allPackagesExist = hasBuiltinPackage && hasRemotePackage;
            
            // 目录状态显示
            GUILayout.Label("📁 目录结构", EditorStyles.boldLabel);
            GUILayout.BeginVertical("box");
            GUILayout.Label($"  • EUResources/Builtin/   {(builtinExists ? "✓" : "✗")}", EditorStyles.miniLabel);
            GUILayout.Label($"  • EUResources/Excluded/  {(excludedExists ? "✓" : "✗")} (不打包)", EditorStyles.miniLabel);
            GUILayout.Label($"  • EUResources/Remote/    {(remoteExists ? "✓" : "✗")}", EditorStyles.miniLabel);
            GUILayout.EndVertical();
            
            GUILayout.Space(10);
            
            // Package 状态显示
            GUILayout.Label("📦 Collector Packages", EditorStyles.boldLabel);
            GUILayout.BeginVertical("box");
            GUILayout.Label($"  • Builtin   {(hasBuiltinPackage ? "✓" : "✗")}", EditorStyles.miniLabel);
            GUILayout.Label($"  • Remote    {(hasRemotePackage ? "✓" : "✗")}", EditorStyles.miniLabel);
            GUILayout.Label($"  • Excluded  (不需要配置)", EditorStyles.miniLabel);
            GUILayout.EndVertical();
            
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "🎯 说明：\n" +
                "• 创建标准目录结构：Builtin / Excluded / Remote\n" +
                "• 在 YooAsset Collector 中创建 Builtin 和 Remote 两个 Package\n" +
                "• Excluded 仅作为本地目录，不参与打包\n" +
                "• Package 创建后，请在 YooAsset Collector 中手动添加 Group 和 Collector", 
                MessageType.Info);
            
            GUILayout.Space(10);
            
            // 操作按钮
            bool allComplete = allDirsExist && allPackagesExist;
            if (!allComplete)
            {
                EditorGUILayout.HelpBox("⚠️ 资源目录结构未完整创建", MessageType.Warning);
                if (GUILayout.Button("🚀 一键生成目录结构与配置", GUILayout.Height(40)))
                {
                    CreateCompleteResStructure();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("✓ 资源目录结构已完整创建", MessageType.Info);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("🔍 打开 EUResources 目录", GUILayout.Height(35)))
                {
                    var resFolder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(resRootPath);
                    EditorGUIUtility.PingObject(resFolder);
                    Selection.activeObject = resFolder;
                }
                if (GUILayout.Button("🔄 重新同步配置", GUILayout.Height(35)))
                {
                    SyncPackagesFromCollector();
                }
                GUILayout.EndHorizontal();
            }
        }
        
        private void CleanDuplicatePackages()
        {
            if (_packageConfig == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到 EUResKitPackageConfig", "确定");
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
                Debug.Log("[EUResKit] AssetBundleCollectorSetting 不存在，正在自动创建...");
                CreateAssetBundleCollectorSetting(SETTINGS_PATH);
                
                if (_collectorSetting == null)
                {
                    EditorUtility.DisplayDialog("错误", "AssetBundleCollectorSetting 创建失败，请检查控制台错误信息", "确定");
                    return;
                }
                
                Debug.Log("[EUResKit] AssetBundleCollectorSetting 自动创建成功");
            }
            
            if (_packageConfig == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到 EUResKitPackageConfig", "确定");
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
                Debug.Log("[EUResKit] AssetBundleCollectorSetting 不存在，正在自动创建...");
                CreateAssetBundleCollectorSetting(SETTINGS_PATH);
                
                if (_collectorSetting == null)
                {
                    EditorUtility.DisplayDialog("错误", "AssetBundleCollectorSetting 创建失败，请检查控制台错误信息", "确定");
                    return;
                }
                
                Debug.Log("[EUResKit] AssetBundleCollectorSetting 自动创建成功");
            }
            
            if (_packageConfig == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到 EUResKitPackageConfig", "确定");
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
            var btnModuleManager = rootVisualElement.Q<Button>("btn-module-manager");

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
            
            if (btnModuleManager != null)
            {
                btnModuleManager.clicked += () =>
                {
                    SetSelectedButton(btnModuleManager);
                    ShowModuleManagerPanel();
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
            var header = CreateContentHeader("代码生成", "生成资源管理代码和开发工具");
            contentArea.Add(header);
            
            // 创建 IMGUIContainer 来显示 EUResFacade 功能
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
            _resFacadeScrollPos = GUILayout.BeginScrollView(_resFacadeScrollPos);
            GUILayout.BeginVertical();
            GUILayout.Space(5);
            
            // UI Prefab 生成区域
            GUILayout.Label("UI Prefab 和脚本", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            string prefabPath = Path.Combine(EUResKitPathHelper.GetResourcesPath(), "EUResKitUI/EUResKitUserOpePopUp.prefab").Replace("\\", "/");
            string scriptPath = Path.Combine(EUResKitPathHelper.GetScriptPath(), "EUResKitUserOpePopUp.cs").Replace("\\", "/");
            bool prefabExists = File.Exists(prefabPath);
            bool scriptExists = File.Exists(scriptPath);
            
            // 显示脚本状态
            GUILayout.BeginHorizontal();
            GUILayout.Label("EUResKitUserOpePopUp.cs:", GUILayout.Width(250));
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
            GUILayout.Label("EUResKitUserOpePopUp.prefab:", GUILayout.Width(250));
            if (prefabExists)
            {
                GUILayout.Label("✓ 已生成", EditorStyles.boldLabel);
            }
            else
            {
                GUILayout.Label("✗ 未生成", EditorStyles.boldLabel);
            }
            GUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox("⚠️ 业务脚本：用户可自定义 UI 交互逻辑，请勿覆盖！\nPrefab：位于 Resources/EUResKitUI/ 目录", MessageType.Warning);
            
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
            
            // EUResKit 分部类生成区域（同时生成）
            GUILayout.Label("EUResKit 分部类（Partial Class）", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            string codeGeneratedPath = Path.Combine(EUResKitPathHelper.GetScriptPath(), "Generated/EUResKit.Generated.cs").Replace("\\", "/");
            string codeUserPath = Path.Combine(EUResKitPathHelper.GetScriptPath(), "EUResKit.cs").Replace("\\", "/");
            bool codeGeneratedExists = File.Exists(codeGeneratedPath);
            bool codeUserExists = File.Exists(codeUserPath);
            bool bothExist = codeGeneratedExists && codeUserExists;
            
            // 显示两个文件的状态
            GUILayout.BeginVertical("box");
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("EUResKit.Generated.cs:", GUILayout.Width(200));
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
            GUILayout.Label("EUResKit.cs:", GUILayout.Width(200));
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
                "• EUResKit.Generated.cs - 自动生成的基础工具类（可重新生成）\n" +
                "• EUResKit.cs - 用户编辑的业务逻辑类（请勿覆盖）\n" +
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
                        "是否重新生成 EUResKit.Generated.cs？\n\n" +
                        "EUResKit.cs（用户脚本）不会被修改", 
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
                if (GUILayout.Button("生成 EUResKit.Generated.cs", GUILayout.Height(40)))
                {
                    OnGenerateResKitGeneratedOnly();
                }
            }
            else if (!codeUserExists && codeGeneratedExists)
            {
                // 只有 Generated 存在
                EditorGUILayout.HelpBox("⚠️ 缺少用户脚本部分，可能导致编译错误！", MessageType.Warning);
                if (GUILayout.Button("生成 EUResKit.cs", GUILayout.Height(40)))
                {
                    OnGenerateUserResKitClicked();
                }
            }
            else
            {
                // 都不存在
                EditorGUILayout.HelpBox("⚠️ EUResKit 分部类尚未生成", MessageType.Warning);
                if (GUILayout.Button("🎯 生成 EUResKit 分部类（同时生成两个文件）", GUILayout.Height(40)))
                {
                    OnGenerateBothResKitFiles();
                }
            }
            
            GUILayout.Space(20);
            
            // 提示用户使用模块管理工具面板
            EditorGUILayout.HelpBox(
                "💡 提示：一键生成、删除文件、刷新命名空间等功能已移至【模块管理工具】面板",
                MessageType.Info);
            
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }
        
        private void ShowModuleManagerPanel()
        {
            var contentArea = rootVisualElement.Q<VisualElement>("content-area");
            if (contentArea == null) return;
            
            contentArea.Clear();
            
            // 设置 contentArea 从左上角开始对齐
            contentArea.style.alignItems = Align.FlexStart;
            contentArea.style.justifyContent = Justify.FlexStart;
            
            // 添加标题
            var header = CreateContentHeader("模块管理工具", "一键生成和清理所有配置");
            contentArea.Add(header);
            
            // 创建 IMGUIContainer 来显示模块管理功能
            var imguiContainer = new IMGUIContainer(() =>
            {
                DrawModuleManagerPanel();
            });
            
            // 设置 IMGUIContainer 占满整个区域且从左上角开始
            imguiContainer.style.width = Length.Percent(100);
            imguiContainer.style.flexGrow = 1;
            
            contentArea.Add(imguiContainer);
        }
        
        private Vector2 _moduleManagerScrollPos;
        
        private void DrawModuleManagerPanel()
        {
            _moduleManagerScrollPos = GUILayout.BeginScrollView(_moduleManagerScrollPos);
            GUILayout.BeginVertical();
            GUILayout.Space(5);
            
            // ===== 一键生成所有配置 =====
            GUILayout.Label("快速初始化", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            EditorGUILayout.HelpBox(
                "🚀 一键生成所有配置\n\n" +
                "将创建以下内容：\n" +
                "• AssetBundleCollectorSetting（资源收集配置）\n" +
                "• EUResServerConfig（服务器配置）\n" +
                "• YooAssetSettings（YooAsset 全局设置）\n" +
                "• EUResKitPackageConfig（包配置）\n" +
                "• Assets/EUResources/Builtin（内置资源目录）\n" +
                "• Assets/EUResources/Excluded（排除资源目录）\n" +
                "• Assets/EUResources/Remote（远程资源目录）\n" +
                "• YooAsset Collector Packages（Builtin/Remote）\n" +
                "• EUResKit.cs + EUResKit.Generated.cs（代码文件）\n" +
                "• EUResKitUserOpePopUp.cs + Prefab（UI 文件）\n\n" +
                "⚠️ 已存在的文件将被保留，不会覆盖", 
                MessageType.Info);
            
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f); // 绿色
            if (GUILayout.Button("🚀 一键生成所有配置和代码", GUILayout.Height(60)))
            {
                GenerateAllConfigsAndCode();
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(20);
            
            // ===== 模块状态 =====
            GUILayout.Label("模块状态", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            DrawModuleStatus();
            
            GUILayout.Space(20);
            
            // ===== 模块管理工具 =====
            GUILayout.Label("模块管理工具", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            EditorGUILayout.HelpBox(
                "🔧 工具说明：\n" +
                "• 刷新命名空间：当模块位置改变时，自动更新命名空间和 asmdef\n" +
                "• 删除生成的文件：清理所有生成的代码和资源文件",
                MessageType.Info);
            
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("🔄 刷新命名空间", GUILayout.Height(40)))
            {
                OnRefreshNamespace();
            }
            
            if (GUILayout.Button("🗑️ 删除所有生成的文件", GUILayout.Height(40)))
            {
                OnDeleteGeneratedFiles();
            }
            
            GUILayout.EndHorizontal();
            
            // 显示当前模块信息
            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                $"📍 当前模块位置：\n{EUResKitPathHelper.GetModuleRoot()}\n\n" +
                $"📦 当前命名空间：\n{EUResKitPathHelper.GetNamespace()}",
                MessageType.None);
            
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }
        
        private void DrawModuleStatus()
        {
            GUILayout.BeginVertical("box");
            
            // 检查配置文件
            string collectorPath = Path.Combine(SETTINGS_PATH, "AssetBundleCollectorSetting.asset");
            string resServerPath = Path.Combine(SETTINGS_PATH, "EUResServerConfig.asset");
            string yooSettingsPath = Path.Combine(SETTINGS_PATH, "YooAssetSettings.asset");
            string packageConfigPath = Path.Combine(SETTINGS_PATH, "EUResKitPackageConfig.asset");
            
            bool collectorExists = File.Exists(collectorPath);
            bool resServerExists = File.Exists(resServerPath);
            bool yooSettingsExists = File.Exists(yooSettingsPath);
            bool packageConfigExists = File.Exists(packageConfigPath);
            
            // 检查资源目录
            string resRootPath = "Assets/EUResources";
            bool resExists = Directory.Exists(resRootPath);
            bool builtinExists = Directory.Exists(Path.Combine(resRootPath, "Builtin"));
            bool excludedExists = Directory.Exists(Path.Combine(resRootPath, "Excluded"));
            bool remoteExists = Directory.Exists(Path.Combine(resRootPath, "Remote"));
            
            // 检查代码文件
            string codeGeneratedPath = Path.Combine(EUResKitPathHelper.GetScriptPath(), "Generated/EUResKit.Generated.cs");
            string codeUserPath = Path.Combine(EUResKitPathHelper.GetScriptPath(), "EUResKit.cs");
            string scriptPath = Path.Combine(EUResKitPathHelper.GetScriptPath(), "EUResKitUserOpePopUp.cs");
            string prefabPath = Path.Combine(EUResKitPathHelper.GetResourcesPath(), "EUResKitUI/EUResKitUserOpePopUp.prefab");
            
            bool codeGeneratedExists = File.Exists(codeGeneratedPath);
            bool codeUserExists = File.Exists(codeUserPath);
            bool scriptExists = File.Exists(scriptPath);
            bool prefabExists = File.Exists(prefabPath);
            
            // 配置文件状态
            GUILayout.Label("配置文件:", EditorStyles.boldLabel);
            DrawStatusLine("AssetBundleCollectorSetting", collectorExists);
            DrawStatusLine("EUResServerConfig", resServerExists);
            DrawStatusLine("YooAssetSettings", yooSettingsExists);
            DrawStatusLine("EUResKitPackageConfig", packageConfigExists);
            
            GUILayout.Space(5);
            
            // 资源目录状态
            GUILayout.Label("资源目录:", EditorStyles.boldLabel);
            DrawStatusLine("Assets/EUResources", resExists);
            DrawStatusLine("  ├─ Builtin", builtinExists);
            DrawStatusLine("  ├─ Excluded", excludedExists);
            DrawStatusLine("  └─ Remote", remoteExists);
            
            GUILayout.Space(5);
            
            // 代码文件状态
            GUILayout.Label("代码文件:", EditorStyles.boldLabel);
            DrawStatusLine("EUResKit.Generated.cs", codeGeneratedExists);
            DrawStatusLine("EUResKit.cs", codeUserExists);
            DrawStatusLine("EUResKitUserOpePopUp.cs", scriptExists);
            DrawStatusLine("EUResKitUserOpePopUp.prefab", prefabExists);
            
            GUILayout.EndVertical();
        }
        
        private void DrawStatusLine(string name, bool exists)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, GUILayout.Width(280));
            if (exists)
            {
                GUI.color = Color.green;
                GUILayout.Label("✓ 已创建", EditorStyles.boldLabel);
            }
            else
            {
                GUI.color = Color.red;
                GUILayout.Label("✗ 未创建", EditorStyles.boldLabel);
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
        }

        #region 生成操作

        private void OnCreatePrefabClicked()
        {
            // 1. 先生成 EUResKitUserOpePopUp.cs 脚本
            string scriptPath = Path.Combine(EUResKitPathHelper.GetScriptPath(), "EUResKitUserOpePopUp.cs").Replace("\\", "/");
            bool scriptGenerated = GenerateEUResKitUserOpePopUpScript(scriptPath);
            
            if (!scriptGenerated)
            {
                EditorUtility.DisplayDialog("错误", "EUResKitUserOpePopUp.cs 脚本生成失败，无法继续", "确定");
                return;
            }
            
            // 刷新资源数据库以编译新脚本
            AssetDatabase.Refresh();
            
            // 等待编译完成
            System.Threading.Thread.Sleep(500);
            
            // 2. 创建 Prefab
            string prefabPath = Path.Combine(EUResKitPathHelper.GetResourcesPath(), "EUResKitUI").Replace("\\", "/");
            
            if (!Directory.Exists(prefabPath))
            {
                Directory.CreateDirectory(prefabPath);
                AssetDatabase.Refresh();
            }

            string fullPath = Path.Combine(prefabPath, "EUResKitUserOpePopUp.prefab");

            // 创建默认的弹窗预制体
            GameObject popup = CreateDefaultPopupPrefab();
            
            // 保存为预制体
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(popup, fullPath);
            DestroyImmediate(popup);

            // 添加 EUResKitUserOpePopUp 组件到 prefab
            AddEUResKitUserOpePopUpComponent(prefabAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // 选中创建的预制体
            EditorGUIUtility.PingObject(prefabAsset);
            Selection.activeObject = prefabAsset;
            
            EditorUtility.DisplayDialog("成功", 
                $"UI Prefab 和脚本创建完成！\n\n" +
                $"Prefab 路径: {fullPath}\n" +
                $"脚本路径: {scriptPath}\n\n" +
                $"已自动添加并绑定 EUResKitUserOpePopUp 组件", 
                "确定");
        }
        
        private bool GenerateEUResKitUserOpePopUpScript(string outputPath)
        {
            string templatePath = Path.Combine(EUResKitPathHelper.GetTemplatesPath(), "EUResKitUserOpePopUp.cs.sbn").Replace("\\", "/");

            if (!File.Exists(templatePath))
            {
                Debug.LogError($"[EUResKit] 模板文件不存在: {templatePath}");
                return false;
            }

            // 读取模板
            string template = File.ReadAllText(templatePath);

            // 替换变量（使用动态命名空间）
            string generated = template
                .Replace("{{ namespace }}", EUResKitPathHelper.GetNamespace())
                .Replace("{{ class_name }}", "EUResKitUserOpePopUp");

            // 确保输出目录存在
            string outputDir = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // 保存生成的代码
            File.WriteAllText(outputPath, generated);
            Debug.Log($"[EUResKit] EUResKitUserOpePopUp.cs 生成成功: {outputPath}");
            
            return true;
        }
        
        private void AddEUResKitUserOpePopUpComponent(GameObject prefabAsset)
        {
            // 使用反射添加组件，避免直接引用运行时类型
            var assemblyName = "EURes";
            var typeName = "EUFramework.Extension.EURes.EUResKitUserOpePopUp";
            
            var assembly = System.AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName);
            
            if (assembly != null)
            {
                var componentType = assembly.GetType(typeName);
                if (componentType != null)
                {
                    var component = prefabAsset.AddComponent(componentType);
                    EditorUtility.SetDirty(prefabAsset);
                    Debug.Log($"[EUResKit] 已添加 {typeName} 组件到 Prefab");
                }
                else
                {
                    Debug.LogWarning($"[EUResKit] 未找到类型 {typeName}，请确保 EUResKitUserOpePopUp.cs 已编译");
                }
            }
            else
            {
                Debug.LogWarning($"[EUResKit] 未找到程序集 {assemblyName}");
            }
        }

        /// <summary>
        /// 同时生成 EUResKit 的两个分部类文件
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
                "EUResKit 分部类已生成完成！\n\n" +
                "✓ EUResKit.Generated.cs（自动生成）\n" +
                "✓ EUResKit.cs（用户编辑）\n\n" +
                "两个文件作为 partial class 相互引用，已同时创建", 
                "确定");
        }
        
        /// <summary>
        /// 只生成 EUResKit.Generated.cs（自动生成部分）
        /// </summary>
        private bool OnGenerateResKitGeneratedOnly()
        {
            string templatePath = Path.Combine(EUResKitPathHelper.GetTemplatesPath(), "DefaultResKit.Generated.sbn").Replace("\\", "/");
            string outputPath = Path.Combine(EUResKitPathHelper.GetScriptPath(), "Generated/EUResKit.Generated.cs").Replace("\\", "/");

            if (!File.Exists(templatePath))
            {
                EditorUtility.DisplayDialog("错误", $"模板文件不存在！\n\n路径: {templatePath}", "确定");
                return false;
            }

            // 读取模板
            string template = File.ReadAllText(templatePath);

            // 替换变量（使用动态命名空间）
            string generated = template
                .Replace("{{ namespace }}", EUResKitPathHelper.GetNamespace())
                .Replace("{{ class_name }}", "EUResKit");

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
        /// 只生成 EUResKit.cs（用户编辑部分）
        /// </summary>
        private bool OnGenerateResKitUserOnly()
        {
            string templatePath = Path.Combine(EUResKitPathHelper.GetTemplatesPath(), "DefaultResKit.cs.sbn").Replace("\\", "/");
            string outputPath = Path.Combine(EUResKitPathHelper.GetScriptPath(), "EUResKit.cs").Replace("\\", "/");

            if (!File.Exists(templatePath))
            {
                EditorUtility.DisplayDialog("错误", $"模板文件不存在！\n\n路径: {templatePath}", "确定");
                return false;
            }

            // 读取模板
            string template = File.ReadAllText(templatePath);

            // 替换变量（使用动态命名空间）
            string generated = template
                .Replace("{{ namespace }}", EUResKitPathHelper.GetNamespace())
                .Replace("{{ class_name }}", "EUResKit");

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
                Debug.Log($"[EUResKit] AssetBundleCollectorSetting 已存在: {path}");
                EditorGUIUtility.PingObject(existing);
                _collectorSetting = existing;
                return;
            }

            var setting = ScriptableObject.CreateInstance<AssetBundleCollectorSetting>();
            setting.ShowPackageView = true; // 默认显示 Package 视图
            AssetDatabase.CreateAsset(setting, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[EUResKit] AssetBundleCollectorSetting 创建成功: {path}");
            _collectorSetting = setting;
        }

        private void CreateEUResServerConfig(string basePath)
        {
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
                AssetDatabase.Refresh();
            }
            
            string path = Path.Combine(basePath, "EUResServerConfig.asset");
            
            var existing = AssetDatabase.LoadAssetAtPath<EUResServerConfig>(path);
            if (existing != null)
            {
                Debug.Log($"[EUResKit] EUResServerConfig 已存在: {path}");
                EditorGUIUtility.PingObject(existing);
                _resServerConfig = existing;
                return;
            }

            var config = ScriptableObject.CreateInstance<EUResServerConfig>();
            config.protocol = ServerProtocol.HTTP;
            config.hostServer = "127.0.0.1";
            config.port = 80;
            config.appVersion = "1.0.0";
            
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[EUResKit] EUResServerConfig 创建成功: {path}");
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
                Debug.Log($"[EUResKit] YooAssetSettings 已存在: {path}");
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
                Debug.Log($"[EUResKit] YooAssetSettings 创建成功: {path}");
                _yooAssetSettings = settings;
            }
            else
            {
                Debug.LogError("[EUResKit] 无法找到 YooAsset.YooAssetSettings 类型");
            }
        }
        
        private void CreateEUResKitPackageConfig(string basePath)
        {
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
                AssetDatabase.Refresh();
            }
            
            string path = Path.Combine(basePath, "EUResKitPackageConfig.asset");
            
            var existing = AssetDatabase.LoadAssetAtPath<EUResKitPackageConfig>(path);
            if (existing != null)
            {
                Debug.Log($"[EUResKit] EUResKitPackageConfig 已存在: {path}");
                EditorGUIUtility.PingObject(existing);
                _packageConfig = existing;
                return;
            }

            var config = ScriptableObject.CreateInstance<EUResKitPackageConfig>();
            
            // 注意：创建时不添加默认 Package，应该从 AssetBundleCollector 同步
            // 如果需要默认配置，请在创建后使用"从 AssetBundleCollector 同步"功能
            
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[EUResKit] EUResKitPackageConfig 创建成功: {path}");
            _packageConfig = config;
        }

        #endregion

        #region 创建默认 UI Prefab

        private GameObject CreateDefaultPopupPrefab()
        {
            // 创建根对象
            GameObject root = new GameObject("EUResKitUserOpePopUp");
            
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
        
        #endregion

        #region 模块管理工具

        /// <summary>
        /// 刷新命名空间（根据模块位置自动更新）
        /// </summary>
        private void OnRefreshNamespace()
        {
            try
            {
                // 1. 计算当前命名空间
                EUResKitPathHelper.ClearCache(); // 清除缓存确保获取最新路径
                string currentNamespace = EUResKitPathHelper.GetNamespace();
                string moduleRoot = EUResKitPathHelper.GetModuleRoot();
                
                if (string.IsNullOrEmpty(currentNamespace) || string.IsNullOrEmpty(moduleRoot))
                {
                    EditorUtility.DisplayDialog("错误", "无法检测模块位置，请确保 EURes.asmdef 文件存在", "确定");
                    return;
                }
                
                // 2. 显示确认对话框
                bool confirm = EditorUtility.DisplayDialog("刷新命名空间",
                    $"检测到模块位置:\n{moduleRoot}\n\n" +
                    $"将更新命名空间为:\n{currentNamespace}\n\n" +
                    $"此操作会:\n" +
                    $"1. 更新 EURes.asmdef 的 rootNamespace\n" +
                    $"2. 更新 EURes.Editor.asmdef 的 rootNamespace\n" +
                    $"3. 可选择重新生成所有代码文件\n\n" +
                    $"是否继续？",
                    "确定", "取消");
                
                if (!confirm) return;
                
                // 3. 更新 asmdef 文件
                bool success = true;
                success &= UpdateAsmdefNamespace("EURes.asmdef", currentNamespace);
                success &= UpdateAsmdefNamespace("EURes.Editor.asmdef", currentNamespace + ".Editor");
                
                if (!success)
                {
                    EditorUtility.DisplayDialog("警告", "部分 asmdef 文件更新失败，请检查控制台日志", "确定");
                    return;
                }
                
                AssetDatabase.Refresh();
                
                // 4. 提示是否重新生成代码
                bool regenerate = EditorUtility.DisplayDialog("重新生成代码？",
                    "命名空间已更新！\n\n" +
                    "是否重新生成所有代码文件以匹配新命名空间？\n" +
                    "（包括 EUResKit.cs, EUResKit.Generated.cs, EUResKitUserOpePopUp.cs）",
                    "是", "稍后手动生成");
                
                if (regenerate)
                {
                    // 重新生成所有代码
                    OnGenerateBothResKitFiles();
                    OnCreatePrefabClicked();
                }
                
                Debug.Log($"[EUResKit] ✓ 命名空间已更新为: {currentNamespace}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EUResKit] 刷新命名空间失败: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("错误", $"刷新命名空间时出错：\n{e.Message}", "确定");
            }
        }
        
        /// <summary>
        /// 更新 asmdef 文件的命名空间
        /// </summary>
        private bool UpdateAsmdefNamespace(string asmdefFileName, string newNamespace)
        {
            try
            {
                string asmdefPath;
                if (asmdefFileName == "EURes.asmdef")
                {
                    asmdefPath = Path.Combine(EUResKitPathHelper.GetModuleRoot(), asmdefFileName).Replace("\\", "/");
                }
                else
                {
                    asmdefPath = Path.Combine(EUResKitPathHelper.GetEditorPath(), asmdefFileName).Replace("\\", "/");
                }
                
                if (!File.Exists(asmdefPath))
                {
                    Debug.LogError($"[EUResKit] 未找到 {asmdefFileName} 文件: {asmdefPath}");
                    return false;
                }
                
                // 读取并解析 JSON
                string jsonContent = File.ReadAllText(asmdefPath);
                
                // 使用简单的字符串替换更新 rootNamespace（避免 JsonUtility 的限制）
                var lines = jsonContent.Split('\n').ToList();
                bool updated = false;
                
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Contains("\"rootNamespace\""))
                    {
                        // 替换整行
                        lines[i] = $"    \"rootNamespace\": \"{newNamespace}\",";
                        updated = true;
                        break;
                    }
                }
                
                // 如果没有 rootNamespace 字段，在 name 字段后添加
                if (!updated)
                {
                    for (int i = 0; i < lines.Count; i++)
                    {
                        if (lines[i].Contains("\"name\""))
                        {
                            lines.Insert(i + 1, $"    \"rootNamespace\": \"{newNamespace}\",");
                            updated = true;
                            break;
                        }
                    }
                }
                
                if (!updated)
                {
                    Debug.LogWarning($"[EUResKit] 无法更新 {asmdefFileName} 的命名空间");
                    return false;
                }
                
                // 写回文件
                File.WriteAllText(asmdefPath, string.Join("\n", lines));
                AssetDatabase.ImportAsset(asmdefPath);
                
                Debug.Log($"[EUResKit] 已更新 {asmdefFileName} 命名空间为: {newNamespace}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EUResKit] 更新 {asmdefFileName} 失败: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 删除所有生成的文件
        /// </summary>
        private void OnDeleteGeneratedFiles()
        {
            try
            {
                // 1. 显示选项对话框
                int option = EditorUtility.DisplayDialogComplex(
                    "删除生成的文件",
                    "请选择删除范围：\n\n" +
                    "1. 仅删除代码和UI - 保留配置文件\n" +
                    "   (EUResKit.cs, EUResKit.Generated.cs, EUResKitUserOpePopUp等)\n\n" +
                    "2. 完全清理 - 删除所有生成内容\n" +
                    "   (包括配置文件：EUResKitPackageConfig等)\n\n" +
                    "⚠️ 此操作不可撤销！",
                    "仅删除代码和UI",  // 0
                    "取消",            // 1
                    "完全清理"         // 2
                );
                
                if (option == 1) return; // 取消
                
                bool deleteConfig = (option == 2); // 完全清理
                
                // 2. 二次确认
                bool confirm = EditorUtility.DisplayDialog("确认删除",
                    deleteConfig 
                        ? "即将删除所有生成的文件（包括配置）！\n此操作不可撤销！"
                        : "即将删除代码和UI文件（保留配置）！\n此操作不可撤销！",
                    "确定删除", "取消");
                
                if (!confirm) return;
                
                // 3. 执行删除
                List<string> deletedFiles = new List<string>();
                string moduleRoot = EUResKitPathHelper.GetModuleRoot();
                
                // 删除代码文件
                DeleteFileIfExists(Path.Combine(EUResKitPathHelper.GetScriptPath(), "EUResKit.cs").Replace("\\", "/"), deletedFiles);
                DeleteFileIfExists(Path.Combine(EUResKitPathHelper.GetScriptPath(), "EUResKitUserOpePopUp.cs").Replace("\\", "/"), deletedFiles);
                DeleteDirectoryIfExists(Path.Combine(EUResKitPathHelper.GetScriptPath(), "Generated").Replace("\\", "/"), deletedFiles);
                
                // 删除 UI Prefab
                string prefabPath = Path.Combine(EUResKitPathHelper.GetResourcesPath(), "EUResKitUI/EUResKitUserOpePopUp.prefab").Replace("\\", "/");
                DeleteFileIfExists(prefabPath, deletedFiles);
                
                // 可选：删除配置文件
                if (deleteConfig)
                {
                    string settingsPath = EUResKitPathHelper.GetSettingsPath();
                    DeleteDirectoryIfExists(settingsPath, deletedFiles);
                }
                
                AssetDatabase.Refresh();
                
                // 4. 显示结果
                string message = $"删除完成！\n\n已删除 {deletedFiles.Count} 个文件/文件夹：\n\n";
                if (deletedFiles.Count > 0)
                {
                    int displayCount = Mathf.Min(deletedFiles.Count, 10);
                    for (int i = 0; i < displayCount; i++)
                    {
                        message += $"• {Path.GetFileName(deletedFiles[i])}\n";
                    }
                    if (deletedFiles.Count > 10)
                        message += $"... 还有 {deletedFiles.Count - 10} 个";
                }
                else
                {
                    message = "没有找到需要删除的文件";
                }
                
                EditorUtility.DisplayDialog("删除完成", message, "确定");
                Debug.Log($"[EUResKit] 删除完成，共删除 {deletedFiles.Count} 个文件/文件夹");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EUResKit] 删除文件失败: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("错误", $"删除文件时出错：\n{e.Message}", "确定");
            }
        }
        
        /// <summary>
        /// 删除文件（如果存在）
        /// </summary>
        private void DeleteFileIfExists(string path, List<string> deletedFiles)
        {
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    string metaPath = path + ".meta";
                    if (File.Exists(metaPath))
                    {
                        File.Delete(metaPath);
                    }
                    deletedFiles.Add(path);
                    Debug.Log($"[EUResKit] 已删除文件: {path}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[EUResKit] 删除文件失败 {path}: {e.Message}");
                }
            }
        }
        
        /// <summary>
        /// 删除目录（如果存在）
        /// </summary>
        private void DeleteDirectoryIfExists(string path, List<string> deletedFiles)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path, true);
                    string metaPath = path + ".meta";
                    if (File.Exists(metaPath))
                    {
                        File.Delete(metaPath);
                    }
                    deletedFiles.Add(path);
                    Debug.Log($"[EUResKit] 已删除目录: {path}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[EUResKit] 删除目录失败 {path}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 一键创建完整的资源目录结构和配置
        /// </summary>
        private void CreateCompleteResStructure()
        {
            try
            {
                // Step 1: 创建目录结构
                Debug.Log("[EUResKit] === 开始创建资源目录结构 ===");
                CreateResDirectoryStructure();
                
                // Step 2: 创建 Collector Packages
                Debug.Log("[EUResKit] === 开始配置 YooAsset Collector ===");
                CreateCollectorPackages();
                
                // Step 3: 同步到 EUResKitPackageConfig
                Debug.Log("[EUResKit] === 开始同步 Package 配置 ===");
                SyncPackagesFromCollector();
                
                // 刷新资源数据库
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("完成", 
                    "✅ 资源结构创建完成！\n\n" +
                    "已完成：\n" +
                    "• 创建目录：Builtin / Excluded / Remote\n" +
                    "• 配置 YooAsset Collector（2个 Package）\n" +
                    "  - Builtin（离线模式）\n" +
                    "  - Remote（热更新模式）\n" +
                    "• Excluded 目录已创建（不参与打包）\n" +
                    "• 同步到 EUResKitPackageConfig\n\n" +
                    "下一步：\n" +
                    "请在 YooAsset Collector 窗口中为 Package 添加 Group 和 Collector", 
                    "确定");
                
                Debug.Log("[EUResKit] === 资源结构创建完成 ===");
                
                // 选中并高亮显示 EUResources 目录
                var resFolder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/EUResources");
                EditorGUIUtility.PingObject(resFolder);
                Selection.activeObject = resFolder;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EUResKit] 创建资源结构失败: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("错误", 
                    $"❌ 创建失败\n\n{ex.Message}", 
                    "确定");
            }
        }
        
        /// <summary>
        /// 一键生成所有配置和代码
        /// </summary>
        private void GenerateAllConfigsAndCode()
        {
            if (!EditorUtility.DisplayDialog("确认", 
                "🚀 一键生成所有配置和代码\n\n" +
                "将创建以下内容：\n\n" +
                "📁 配置文件：\n" +
                "• AssetBundleCollectorSetting\n" +
                "• EUResServerConfig\n" +
                "• YooAssetSettings\n" +
                "• EUResKitPackageConfig\n\n" +
                "📁 资源目录：\n" +
                "• Assets/EUResources/Builtin\n" +
                "• Assets/EUResources/Excluded\n" +
                "• Assets/EUResources/Remote\n\n" +
                "📁 YooAsset Collector：\n" +
                "• Builtin Package\n" +
                "• Remote Package\n\n" +
                "📁 代码文件：\n" +
                "• EUResKit.cs\n" +
                "• EUResKit.Generated.cs\n" +
                "• EUResKitUserOpePopUp.cs\n" +
                "• EUResKitUserOpePopUp.prefab\n\n" +
                "⚠️ 已存在的文件将被保留，不会覆盖\n\n" +
                "是否继续？", 
                "继续", "取消"))
            {
                return;
            }
            
            try
            {
                Debug.Log("[EUResKit] ========================================");
                Debug.Log("[EUResKit] 开始一键生成所有配置和代码");
                Debug.Log("[EUResKit] ========================================");
                
                // Step 1: 创建所有配置文件
                Debug.Log("[EUResKit] [1/6] 创建配置文件...");
                CreateAssetBundleCollectorSetting(SETTINGS_PATH);
                CreateEUResServerConfig(SETTINGS_PATH);
                CreateYooAssetSettings(SETTINGS_PATH);
                CreateEUResKitPackageConfig(SETTINGS_PATH);
                Debug.Log("[EUResKit] ✓ 配置文件创建完成");
                
                // Step 2: 创建资源目录结构
                Debug.Log("[EUResKit] [2/6] 创建资源目录...");
                CreateResDirectoryStructure();
                Debug.Log("[EUResKit] ✓ 资源目录创建完成");
                
                // Step 3: 创建 Collector Packages
                Debug.Log("[EUResKit] [3/6] 配置 YooAsset Collector...");
                CreateCollectorPackages();
                Debug.Log("[EUResKit] ✓ Collector 配置完成");
                
                // Step 4: 同步到 EUResKitPackageConfig
                Debug.Log("[EUResKit] [4/6] 同步 Package 配置...");
                SyncPackagesFromCollector();
                Debug.Log("[EUResKit] ✓ Package 同步完成");
                
                // Step 5: 生成 UI Prefab 和脚本
                Debug.Log("[EUResKit] [5/6] 生成 UI Prefab 和脚本...");
                OnCreatePrefabClicked();
                Debug.Log("[EUResKit] ✓ UI 生成完成");
                
                // Step 6: 生成 EUResKit 分部类
                Debug.Log("[EUResKit] [6/6] 生成 EUResKit 分部类...");
                OnGenerateBothResKitFiles();
                Debug.Log("[EUResKit] ✓ 代码生成完成");
                
                // 重新加载配置
                LoadConfigs();
                
                // 刷新资源数据库
                AssetDatabase.Refresh();
                
                Debug.Log("[EUResKit] ========================================");
                Debug.Log("[EUResKit] 所有配置和代码生成完成！");
                Debug.Log("[EUResKit] ========================================");
                
                EditorUtility.DisplayDialog("完成", 
                    "✅ 所有配置和代码已生成完成！\n\n" +
                    "已创建：\n" +
                    "• 4 个配置文件\n" +
                    "• 3 个资源目录（Builtin/Excluded/Remote）\n" +
                    "• 2 个 YooAsset Package\n" +
                    "• 4 个代码文件（含 UI）\n\n" +
                    "下一步：\n" +
                    "1. 在【资源配置】面板调整各项设置\n" +
                    "2. 在 YooAsset Collector 中为 Package 添加具体资源\n" +
                    "3. 开始使用 EUResKit.InitializeAllPackagesAsync()\n\n" +
                    "详细信息请查看控制台日志", 
                    "确定");
                
                // 选中并高亮显示模块根目录
                var moduleRoot = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(EUResKitPathHelper.GetModuleRoot());
                if (moduleRoot != null)
                {
                    EditorGUIUtility.PingObject(moduleRoot);
                    Selection.activeObject = moduleRoot;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EUResKit] 生成失败: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("错误", 
                    $"❌ 生成失败\n\n{ex.Message}\n\n请查看控制台错误信息", 
                    "确定");
            }
        }

        /// <summary>
        /// 创建资源目录结构
        /// </summary>
        private void CreateResDirectoryStructure()
        {
            string resRootPath = "Assets/EUResources";
            
            // 确保 EUResources 根目录存在
            if (!Directory.Exists(resRootPath))
            {
                Directory.CreateDirectory(resRootPath);
                Debug.Log($"[EUResKit] 创建目录: {resRootPath}");
            }
            
            // 创建子目录及说明文件
            var directoryConfigs = new[]
            {
                new { Name = "Builtin", Desc = "内置资源", Detail = GetDirectoryDescription("Builtin") },
                new { Name = "Excluded", Desc = "不打包资源", Detail = GetDirectoryDescription("Excluded") },
                new { Name = "Remote", Desc = "热更新资源", Detail = GetDirectoryDescription("Remote") }
            };
            
            int createdCount = 0;
            foreach (var config in directoryConfigs)
            {
                string fullPath = Path.Combine(resRootPath, config.Name);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    Debug.Log($"[EUResKit] 创建目录: {fullPath}");
                    createdCount++;
                }
                
                // 创建 README 文件
                string readmePath = Path.Combine(fullPath, "README.md");
                if (!File.Exists(readmePath))
                {
                    File.WriteAllText(readmePath, config.Detail);
                    Debug.Log($"[EUResKit] 创建说明文档: {readmePath}");
                }
                
                // 创建 .gitkeep 确保空目录被 Git 追踪
                string gitkeepPath = Path.Combine(fullPath, ".gitkeep");
                if (!File.Exists(gitkeepPath))
                {
                    File.WriteAllText(gitkeepPath, "");
                }
            }
            
            if (createdCount > 0)
            {
                Debug.Log($"[EUResKit] 目录创建完成，共创建 {createdCount} 个新目录");
            }
            else
            {
                Debug.Log("[EUResKit] 所有目录已存在");
            }
        }

        /// <summary>
        /// 创建 YooAsset Collector 的 Package 配置
        /// </summary>
        private void CreateCollectorPackages()
        {
            if (_collectorSetting == null)
            {
                Debug.Log("[EUResKit] AssetBundleCollectorSetting 不存在，正在自动创建...");
                
                // 自动创建 AssetBundleCollectorSetting
                CreateAssetBundleCollectorSetting(SETTINGS_PATH);
                
                // 如果创建后仍然为空，说明创建失败
                if (_collectorSetting == null)
                {
                    Debug.LogError("[EUResKit] AssetBundleCollectorSetting 自动创建失败");
                    EditorUtility.DisplayDialog("错误", 
                        "AssetBundleCollectorSetting 创建失败！\n\n请检查控制台错误信息。", 
                        "确定");
                    return;
                }
                
                Debug.Log("[EUResKit] AssetBundleCollectorSetting 自动创建成功");
            }
            
            // 只为 Builtin 和 Remote 创建 Package（Excluded 不参与打包，无需配置）
            var packageConfigs = new[]
            {
                new
                {
                    Name = "Builtin",
                    Desc = "内置资源包（打入应用内，离线可用）",
                    AutoCollectShaders = false
                },
                new
                {
                    Name = "Remote",
                    Desc = "远程热更新资源包（从服务器下载，支持热更新）",
                    AutoCollectShaders = true
                }
            };
            
            int createdCount = 0;
            foreach (var config in packageConfigs)
            {
                // 检查是否已存在
                bool exists = _collectorSetting.Packages.Any(p => p.PackageName == config.Name);
                if (exists)
                {
                    Debug.Log($"[EUResKit] Package 已存在: {config.Name}");
                    continue;
                }
                
                // 创建 Package（只创建 Package，不创建 Group 和 Collector）
                Undo.RecordObject(_collectorSetting, "EUResKit Create Package");
                var package = AssetBundleCollectorSettingData.CreatePackage(config.Name);
                package.PackageDesc = config.Desc;
                package.EnableAddressable = true;
                package.SupportExtensionless = true;
                package.LocationToLower = false;
                package.IncludeAssetGUID = false;
                package.AutoCollectShaders = config.AutoCollectShaders;
                
                Debug.Log($"[EUResKit] 创建 Package: {config.Name} (AutoCollectShaders: {config.AutoCollectShaders})");
                
                createdCount++;
            }
            
            if (createdCount > 0)
            {
                // 确保 ShowPackageView 被启用
                _collectorSetting.ShowPackageView = true;
                
                // 保存配置
                EditorUtility.SetDirty(_collectorSetting);
                AssetDatabase.SaveAssets();
                Debug.Log($"[EUResKit] Collector 配置完成，共创建 {createdCount} 个 Package");
                Debug.Log("[EUResKit] 提示：Package 创建完成，请在 YooAsset Collector 窗口中根据需要添加 Group 和 Collector");
            }
            else
            {
                Debug.Log("[EUResKit] 所有 Package 已存在");
            }
        }

        /// <summary>
        /// 获取目录说明文档内容
        /// </summary>
        private string GetDirectoryDescription(string dirName)
        {
            switch (dirName)
            {
                case "Builtin":
                    return @"# Builtin 目录

## 📦 用途
存放**内置资源**，这些资源会直接打包到应用程序中。

## 🎯 适用场景
- **编辑器模拟模式** (EditorSimulateMode)
- **离线模式** (OfflinePlayMode)
- **必须随应用一起发布的核心资源**

## 📋 推荐内容
- 启动 Logo、Splash 界面
- 核心 UI 框架和基础界面
- 必需的配置文件
- 启动流程所需的关键资源

## ⚠️ 注意事项
- 内置资源会**增加应用包体大小**
- 一旦发布，**无法通过热更新修改**
- 建议只放置启动必需的最小资源集
- 资源更新需要重新发布应用

## 🔧 YooAsset 设置
- **Package Name**: Builtin
- **Play Mode**: OfflinePlayMode / EditorSimulateMode
- **Directory**: Assets/EUResources/Builtin
";

                case "Excluded":
                    return @"# Excluded 目录

## 📦 用途
存放**不参与打包的资源**，仅在编辑器开发阶段使用。

## 🎯 适用场景
- 临时测试资源
- 开发阶段的占位资源
- 原始设计稿和参考图
- 不需要发布的辅助资源
- 编辑器工具所需的资源

## 📋 推荐内容
- 测试用的临时资源
- UI 设计稿 PSD/Sketch 源文件
- 资源制作的中间文件
- 开发文档和说明
- 编辑器扩展所需的资源

## ⚠️ 注意事项
- 这些资源**不会被 YooAsset 打包**
- **仅在 Unity 编辑器中可用**
- 不占用应用包体大小
- 运行时无法访问这些资源
- **不会在 YooAsset Collector 中创建 Package**

## 🔧 目录说明
- **Directory**: Assets/EUResources/Excluded
- **用途**: 仅作为本地开发目录
- 建议添加到 `.gitignore`（根据项目需求）
";

                case "Remote":
                    return @"# Remote 目录

## 📦 用途
存放**远程热更新资源**，可以通过服务器动态下载和更新。

## 🎯 适用场景
- **主机模式** (HostPlayMode)
- **Web 模式** (WebPlayMode)
- 需要热更新的游戏内容
- 频繁变化的运营资源

## 📋 推荐内容
- 游戏关卡、场景资源
- UI 界面（非核心框架）
- 角色、特效、音效资源
- 配置表和数据文件
- 运营活动相关资源
- 所有 Shader（启用 AutoCollectShaders）

## ⚠️ 注意事项
- 资源会**上传到资源服务器**
- 可以**不更新应用**的情况下更新内容
- 首次运行需要**联网下载**
- 建议资源按功能模块划分，便于按需下载
- 大文件建议分包管理

## 🔧 YooAsset 设置
- **Package Name**: Remote
- **Play Mode**: HostPlayMode / WebPlayMode
- **Auto Collect Shaders**: true（收集所有 Shader）
- **Enable Addressable**: true（支持资源寻址）
- **Directory**: Assets/EUResources/Remote

## 🌐 热更新流程
1. 检查资源版本
2. 下载更新的资源
3. 验证资源完整性
4. 应用新资源
";

                default:
                    return $"# {dirName}\n\nYooAsset 资源目录";
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
            
            string uxmlPath = Path.Combine(EUResKitPathHelper.GetEditorPath(), "UI/EUResKitEditorWindow.uxml").Replace("\\", "/");
            var label = new Label($"UXML 文件未找到！\n请确保文件存在:\n{uxmlPath}");
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
