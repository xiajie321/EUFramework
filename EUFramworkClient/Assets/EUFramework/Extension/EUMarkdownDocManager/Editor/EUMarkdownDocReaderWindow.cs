#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using UnityEngine.Video;

namespace EUFramework.Extension.MarkdownDocManagerKit.Editor
{
    public class EUMarkdownDocReaderWindow : EditorWindow
    {
        private VisualElement rootContainer;
        private TreeView docTreeView;
        private ScrollView contentScrollView;
        private Label titleLabel;
        private Label emptyStateLabel;
        private TextField searchField;
        private PopupField<string> searchModeField;
        private Label docCountLabel;
        private Label currentDocPathLabel;
        private ScrollView navScrollView;
        private ProgressBar searchProgressBar;
        private VisualElement navDrawerContainer;
        private VisualElement navPanel;
        private Button navToggleButton;
        private VisualElement navHeader;
        
        // 左侧栏相关
        private VisualElement leftSidebar;
        private VisualElement docTreeContainer;
        private ListView searchResultListView; // 新增：搜索结果列表
        private Button docTreeToggleButton;
        private bool isDocTreeOpen = true;

        private bool isNavOpen = true;
        private List<DocNode> docNodes = new List<DocNode>();
        private List<DocNode> allDocNodes = new List<DocNode>();
        private Dictionary<int, DocNode> nodeIdMap = new Dictionary<int, DocNode>();
        private int currentNodeId = 0;
        private string currentSearchText = "";
        private string currentDocPath = "";
        private List<HeaderInfo> currentHeaders = new List<HeaderInfo>();
        private Dictionary<string, string> fileContentCache = new Dictionary<string, string>();
        private List<Texture2D> loadedTextures = new List<Texture2D>();
        private List<VideoPlayer> activeVideoPlayers = new List<VideoPlayer>();
        private bool isSearching = false;
        private SearchMode currentSearchMode = SearchMode.FileName;
        private double lastSearchTime;
        private const double SearchDelay = 0.3f; // 300ms 防抖
        
        // 搜索结果数据
        private List<SearchResultItem> searchResults = new List<SearchResultItem>();
        
        // 滚动监听相关
        private bool isAutoScrolling = false;
        private VisualElement currentActiveNavItem = null;
        
        // 搜索结果导航
        private List<VisualElement> currentSearchMatches = new List<VisualElement>();
        private int currentMatchIndex = -1;
        private VisualElement searchNavContainer;
        private Label searchResultLabel;
        
        // 跳转目标
        private int pendingScrollLineIndex = -1;

        // 异步渲染相关状态
        private bool isRendering = false;
        private string[] pendingRenderLines;
        private int currentRenderIndex;
        private VisualElement pendingContentPanel;
        private const int LINES_PER_FRAME = 100; // 每帧处理的行数，可根据性能调整
        
        // 渲染上下文状态
        private bool renderInCodeBlock = false;
        private string renderCodeBlockContent = "";
        private string renderCodeBlockLanguage = "";
        private bool renderInList = false;

        // 段落合并缓冲
        private string pendingParagraphContent = "";
        private int pendingParagraphStartLine = -1;

        // 表格渲染状态
        private bool renderInTable = false;
        private VisualElement currentTable;
        private List<TextAnchor> tableAlignments;
        
        // 预读缓冲，用于检测表格头
        private string potentialTableHeader = null;
        private int potentialTableHeaderLineIndex = -1;

        private void FlushParagraph(VisualElement contentPanel)
        {
            if (!string.IsNullOrEmpty(pendingParagraphContent))
            {
                // 创建段落时，使用 buffered content
                var paragraph = CreateParagraph(pendingParagraphContent, pendingParagraphStartLine);
                contentPanel.Add(paragraph);
                
                // 重置 buffer
                pendingParagraphContent = "";
                pendingParagraphStartLine = -1;
            }
        }
        
        private void FlushPotentialTableHeader(VisualElement contentPanel)
        {
            if (potentialTableHeader != null)
            {
                ProcessParagraphLine(potentialTableHeader, potentialTableHeaderLineIndex, contentPanel);
                potentialTableHeader = null;
                potentialTableHeaderLineIndex = -1;
            }
        }

        public enum SearchMode
        {
            FileName,
            Content
        }

        public enum DocReadMode
        {
            Default,
            Universal
        }

        private DocReadMode currentReadMode = DocReadMode.Default;
        private string universalPath = "";
        private const string PREF_READ_MODE = "EUMarkdownDoc_ReadMode";
        private const string PREF_UNIVERSAL_PATH = "EUMarkdownDoc_UniversalPath";

        [MenuItem("EUFramework/拓展/Markdown文档阅读器")]
        public static void ShowWindow()
        {
            var window = GetWindow<EUMarkdownDocReaderWindow>();
            window.titleContent = new GUIContent("Markdown文档阅读器");
            window.minSize = new Vector2(900, 600);
        }
        
        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            isRendering = false;
            EditorApplication.update -= OnEditorUpdate;
            CleanupResources();
        }

        private void OnDestroy()
        {
            CleanupResources();
        }

        private void CleanupResources()
        {
            foreach (var tex in loadedTextures)
            {
                if (tex != null)
                {
                    DestroyImmediate(tex);
                }
            }
            loadedTextures.Clear();

            foreach (var player in activeVideoPlayers)
            {
                if (player != null)
                {
                    if (player.targetTexture != null)
                    {
                        player.targetTexture.Release();
                        DestroyImmediate(player.targetTexture);
                    }
                    DestroyImmediate(player.gameObject);
                }
            }
            activeVideoPlayers.Clear();
        }

        private void OnEditorUpdate()
        {
            // 处理搜索防抖
            if (isSearching && EditorApplication.timeSinceStartup - lastSearchTime > SearchDelay)
            {
                isSearching = false;
                PerformSearchAsync();
            }

            // 处理异步渲染
            if (isRendering)
            {
                ContinueRendering();
            }
        }

        private void CreateGUI()
        {
            LoadStyleSheet();
            BuildUI();
            LoadDocuments();
            
            // 注册快捷键
            rootVisualElement.RegisterCallback<KeyDownEvent>(evt => {
                if ((evt.ctrlKey || evt.commandKey) && evt.keyCode == KeyCode.F)
                {
                    searchField.Focus();
                    evt.StopPropagation();
                }
            });
        }
        
        private void LoadStyleSheet()
        {
            // 1. 尝试通过 GUID 查找（最稳健）
            string[] guids = AssetDatabase.FindAssets("EUMarkdownDocReader t:StyleSheet");
            if (guids.Length > 0)
            {
                // 可能会有多个同名文件，优先匹配路径中包含 ConfigPanel 的
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith("ConfigPanel/EUMarkdownDocReader.uss"))
                    {
                        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                        if (styleSheet != null)
                        {
                            rootVisualElement.styleSheets.Add(styleSheet);
                            return;
                        }
                    }
                }
                // 如果没有完全匹配的，尝试加载第一个找到的
                var firstStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(guids[0]));
                if (firstStyleSheet != null)
                {
                    rootVisualElement.styleSheets.Add(firstStyleSheet);
                    return;
                }
            }

            // 2. 回退到默认路径
            var defaultStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/EUFramework/Extension/EUMarkdownDocManager/ConfigPanel/EUMarkdownDocReader.uss");
            if (defaultStyleSheet != null)
            {
                rootVisualElement.styleSheets.Add(defaultStyleSheet);
            }
            else
            {
                Debug.LogError("[EUMarkdownDocReader] 无法找到样式文件 EUMarkdownDocReader.uss，请确保文件存在于项目中。");
            }
        }
        
        private void BuildUI()
        {
            rootContainer = new VisualElement();
            rootContainer.AddToClassList("root-container");
            // 初始隐藏，用于入场动画
            rootContainer.style.opacity = 0;
            rootContainer.style.translate = new Translate(0, 20, 0);
            
            rootVisualElement.Add(rootContainer);
            
            // 入场动画
            rootContainer.schedule.Execute(() => {
                rootContainer.style.transitionProperty = new List<StylePropertyName> { 
                    new StylePropertyName("opacity"), 
                    new StylePropertyName("translate") 
                };
                rootContainer.style.transitionDuration = new List<TimeValue> { new TimeValue(0.5f, TimeUnit.Second) };
                rootContainer.style.transitionTimingFunction = new List<EasingFunction> { new EasingFunction(EasingMode.EaseOutCubic) };
                
                rootContainer.style.opacity = 1;
                rootContainer.style.translate = new Translate(0, 0, 0);
            }).StartingIn(100);
            
            // 顶部栏
            var topBar = new VisualElement();
            topBar.AddToClassList("top-bar");
            
            titleLabel = new Label("文档阅读器");
            titleLabel.AddToClassList("section-title");
            topBar.Add(titleLabel);
            
            // 搜索容器
            var searchContainer = new VisualElement();
            searchContainer.AddToClassList("search-container");
            searchContainer.style.flexDirection = FlexDirection.Row;

            // 搜索模式选择
            var searchModeOptions = new List<string> { "文件名", "内容" };
            searchModeField = new PopupField<string>(
                searchModeOptions, 
                currentSearchMode == SearchMode.FileName ? 0 : 1
            );
            searchModeField.AddToClassList("search-mode-field");
            searchModeField.RegisterValueChangedCallback(evt => 
            {
                currentSearchMode = evt.newValue == "文件名" ? SearchMode.FileName : SearchMode.Content;
                OnSearchTextChanged(searchField.value);
            });
            searchContainer.Add(searchModeField);

            // 搜索框
            searchField = new TextField();
            searchField.AddToClassList("search-field");
            searchField.value = "";
            searchField.RegisterValueChangedCallback(evt => OnSearchTextChanged(evt.newValue));
            searchContainer.Add(searchField);
            
            // 搜索导航 (在内容搜索模式下显示)
            searchNavContainer = new VisualElement();
            searchNavContainer.AddToClassList("search-nav-container");
            searchNavContainer.style.display = DisplayStyle.None;
            
            searchResultLabel = new Label("0/0");
            searchResultLabel.AddToClassList("search-result-label");
            searchNavContainer.Add(searchResultLabel);
            
            var prevBtn = new Button(() => NavigateSearchMatch(-1)) { text = "↑" };
            prevBtn.AddToClassList("search-nav-btn");
            prevBtn.tooltip = "上一个匹配项";
            searchNavContainer.Add(prevBtn);
            
            var nextBtn = new Button(() => NavigateSearchMatch(1)) { text = "↓" };
            nextBtn.AddToClassList("search-nav-btn");
            nextBtn.tooltip = "下一个匹配项";
            searchNavContainer.Add(nextBtn);
            
            searchContainer.Add(searchNavContainer);
            
            topBar.Add(searchContainer);

            // 搜索进度条
            searchProgressBar = new ProgressBar();
            searchProgressBar.style.display = DisplayStyle.None;
            searchProgressBar.style.width = 100;
            searchProgressBar.style.marginLeft = 10;
            topBar.Add(searchProgressBar);
            
            // 设置按钮
            var settingsButton = new Button(ShowSettings) { text = "设置" };
            settingsButton.AddToClassList("action-button");
            topBar.Add(settingsButton);

            // 刷新按钮
            var refreshButton = new Button(() => LoadDocuments()) { text = "刷新" };
            refreshButton.AddToClassList("action-button");
            refreshButton.AddToClassList("btn-primary");
            topBar.Add(refreshButton);
            
            rootContainer.Add(topBar);
            
            // 分割视图
            var splitView = new VisualElement();
            splitView.AddToClassList("split-view");
            
            // 左侧侧边栏容器
            leftSidebar = new VisualElement();
            leftSidebar.AddToClassList("left-sidebar");

            // 左侧文档树容器
            docTreeContainer = new VisualElement();
            docTreeContainer.AddToClassList("doc-tree-container");
            
            // 文档统计标签
            docCountLabel = new Label("文档: 0");
            docCountLabel.AddToClassList("doc-count-label");
            docTreeContainer.Add(docCountLabel);
            
            docTreeView = new TreeView();
            docTreeView.AddToClassList("doc-tree");
            docTreeView.makeItem = () => 
            {
                var container = new VisualElement();
                container.AddToClassList("tree-item-container");
                
                var icon = new Image();
                icon.AddToClassList("tree-item-icon-image");
                container.Add(icon);
                
                var label = new Label();
                label.AddToClassList("tree-item-label");
                container.Add(label);
                
                return container;
            };
            docTreeView.bindItem = (element, index) =>
            {
                var container = element as VisualElement;
                var item = docTreeView.GetItemDataForIndex<DocNode>(index);
                if (container != null && item != null)
                {
                    var icon = container.Q<Image>(className: "tree-item-icon-image");
                    var label = container.Q<Label>(className: "tree-item-label");
                    
                    if (icon != null)
                    {
                        // 使用 Unity 内置图标
                        icon.image = item.isDirectory 
                            ? EditorGUIUtility.IconContent("Folder Icon").image 
                            : EditorGUIUtility.IconContent("TextAsset Icon").image;
                    }
                    if (label != null)
                    {
                        label.text = item.name;
                    }
                }
            };
            docTreeView.selectionChanged += OnTreeSelectionChanged;
            docTreeContainer.Add(docTreeView);
            
            // 搜索结果列表 (初始隐藏)
            searchResultListView = new ListView();
            searchResultListView.AddToClassList("search-result-list");
            searchResultListView.style.display = DisplayStyle.None;
            searchResultListView.fixedItemHeight = 44; // 增加高度以显示两行信息
            searchResultListView.makeItem = () => 
            {
                var container = new VisualElement();
                container.AddToClassList("search-result-item");
                
                var fileLabel = new Label();
                fileLabel.AddToClassList("search-result-file");
                container.Add(fileLabel);
                
                var contentLabel = new Label();
                contentLabel.AddToClassList("search-result-content");
                contentLabel.enableRichText = true;
                container.Add(contentLabel);
                
                return container;
            };
            searchResultListView.bindItem = (element, index) => 
            {
                if (index >= 0 && index < searchResults.Count)
                {
                    var item = searchResults[index];
                    var fileLabel = element.Q<Label>(className: "search-result-file");
                    var contentLabel = element.Q<Label>(className: "search-result-content");
                    
                    fileLabel.text = item.docNode.name;
                    
                    // 高亮匹配内容
                    string content = item.lineContent.Trim();
                    if (content.Length > 50) content = content.Substring(0, 50) + "...";
                    
                    if (!string.IsNullOrEmpty(currentSearchText))
                    {
                        string pattern = Regex.Escape(currentSearchText);
                        content = Regex.Replace(content, pattern, match => $"<color=#FFD700><b>{match.Value}</b></color>", RegexOptions.IgnoreCase);
                    }
                    contentLabel.text = content;
                }
            };
            searchResultListView.selectionChanged += OnSearchResultSelected;
            docTreeContainer.Add(searchResultListView);
            
            leftSidebar.Add(docTreeContainer);

            // 左侧切换按钮
            docTreeToggleButton = new Button(ToggleDocTree);
            docTreeToggleButton.AddToClassList("doc-tree-toggle-button");
            docTreeToggleButton.text = "《"; // 初始展开
            docTreeToggleButton.tooltip = "收起/展开文档树";
            leftSidebar.Add(docTreeToggleButton);
            
            splitView.Add(leftSidebar);
            
            // 右侧内容区域
            var contentContainer = new VisualElement();
            contentContainer.AddToClassList("content-container");
            
            // 当前文档路径
            currentDocPathLabel = new Label("");
            currentDocPathLabel.AddToClassList("current-doc-path");
            contentContainer.Add(currentDocPathLabel);
            
            // 内容和导航的分割视图
            var contentSplitView = new VisualElement();
            contentSplitView.AddToClassList("content-split-view");
            
            contentScrollView = new ScrollView();
            contentScrollView.AddToClassList("content-scroll");
            // 添加滚动监听以更新导航高亮
            contentScrollView.verticalScroller.valueChanged += OnContentScroll;
            
            // 空状态提示
            emptyStateLabel = new Label("请从左侧选择文档");
            emptyStateLabel.AddToClassList("empty-state");
            contentScrollView.Add(emptyStateLabel);
            
            contentSplitView.Add(contentScrollView);
            
            // 导航面板
            BuildNavigationPanel();
            contentSplitView.Add(navDrawerContainer);
            
            contentContainer.Add(contentSplitView);
            
            splitView.Add(contentContainer);
            
            rootContainer.Add(splitView);
        }
        
        private void BuildNavigationPanel()
        {
            navDrawerContainer = new VisualElement();
            navDrawerContainer.AddToClassList("nav-drawer-container");

            // 切换按钮 (页签)
            navToggleButton = new Button(ToggleNavDrawer);
            navToggleButton.AddToClassList("nav-toggle-button");
            navToggleButton.text = "》"; // 初始状态为展开，显示收起图标
            navToggleButton.tooltip = "收起/展开导航";
            navDrawerContainer.Add(navToggleButton);

            // 导航面板主体
            navPanel = new VisualElement();
            navPanel.AddToClassList("nav-panel");
            
            // 导航头部
            navHeader = new VisualElement();
            navHeader.AddToClassList("nav-header");
            
            var navTitle = new Label("目录导航");
            navTitle.AddToClassList("nav-title");
            navHeader.Add(navTitle);
            
            navPanel.Add(navHeader);
            
            // 导航内容
            navScrollView = new ScrollView();
            navScrollView.AddToClassList("nav-scroll");
            navPanel.Add(navScrollView);
            
            navDrawerContainer.Add(navPanel);
        }

        private void ToggleNavDrawer()
        {
            isNavOpen = !isNavOpen;
            
            if (isNavOpen)
            {
                navPanel.RemoveFromClassList("nav-panel-collapsed");
                navToggleButton.text = "》";
                navToggleButton.RemoveFromClassList("nav-toggle-button-collapsed");
            }
            else
            {
                navPanel.AddToClassList("nav-panel-collapsed");
                navToggleButton.text = "《"; // 收起状态，显示展开图标
                navToggleButton.AddToClassList("nav-toggle-button-collapsed");
            }
        }

        private void ToggleDocTree()
        {
            isDocTreeOpen = !isDocTreeOpen;
            
            if (isDocTreeOpen)
            {
                docTreeContainer.RemoveFromClassList("doc-tree-container-collapsed");
                docTreeToggleButton.text = "《";
                docTreeToggleButton.RemoveFromClassList("doc-tree-toggle-button-collapsed");
            }
            else
            {
                docTreeContainer.AddToClassList("doc-tree-container-collapsed");
                docTreeToggleButton.text = "》";
                docTreeToggleButton.AddToClassList("doc-tree-toggle-button-collapsed");
            }
        }
        
        private void ShowSettings()
        {
            EUMarkdownDocSettingsWindow.ShowWindow(() => {
                LoadSettings();
                LoadDocuments();
            });
        }

        private void LoadSettings()
        {
            currentReadMode = (DocReadMode)EditorPrefs.GetInt(PREF_READ_MODE, (int)DocReadMode.Default);
            universalPath = EditorPrefs.GetString(PREF_UNIVERSAL_PATH, "");
        }

        public static void SaveSettings(DocReadMode mode, string path)
        {
            EditorPrefs.SetInt(PREF_READ_MODE, (int)mode);
            EditorPrefs.SetString(PREF_UNIVERSAL_PATH, path);
        }

        private void LoadDocuments()
        {
            LoadSettings();

            docNodes.Clear();
            nodeIdMap.Clear();
            fileContentCache.Clear();
            currentNodeId = 0;
            
            try
            {
                if (currentReadMode == DocReadMode.Universal)
                {
                    LoadUniversalDocuments();
                }
                else
                {
                    LoadDefaultDocuments();
                }
                
                RebuildTreeView();
                
                // 收集所有文档节点用于搜索
                allDocNodes.Clear();
                CollectAllDocNodes(docNodes, allDocNodes);
                
                if (docNodes.Count == 0)
                {
                    string msg = currentReadMode == DocReadMode.Universal 
                        ? "未找到文档，请在设置中配置有效的路径" 
                        : "未找到任何文档";
                    ShowEmptyState(msg);
                }
                
                UpdateDocCount();
            }
            catch (Exception e)
            {
                Debug.LogError($"加载文档失败: {e.Message}\n{e.StackTrace}");
                ShowEmptyState($"加载文档失败: {e.Message}");
            }
        }

        private void LoadUniversalDocuments()
        {
            if (string.IsNullOrEmpty(universalPath) || !Directory.Exists(universalPath))
            {
                return;
            }

            var rootName = Path.GetFileName(universalPath);
            if (string.IsNullOrEmpty(rootName)) rootName = universalPath;

            var rootNode = new DocNode
            {
                id = currentNodeId++,
                name = rootName,
                path = universalPath,
                isDirectory = true,
                children = new List<DocNode>()
            };
            nodeIdMap[rootNode.id] = rootNode;
            
            ScanDirectory(universalPath, rootNode);
            
            if (rootNode.children.Count > 0)
            {
                docNodes.Add(rootNode);
            }
        }

        private void LoadDefaultDocuments()
        {
            // 扫描扩展目录
            List<string> extensionPaths = new List<string>();

            // 0. 自动定位 EUMarkdownDocManager 自身位置及向上查找 Doc 文件夹
            var script = MonoScript.FromScriptableObject(this);
            string scriptPath = AssetDatabase.GetAssetPath(script);
            if (!string.IsNullOrEmpty(scriptPath))
            {
                // 获取脚本所在的绝对路径目录
                // 使用更稳健的方式获取绝对路径：项目根目录 + 资源相对路径
                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                string scriptAbsPath = Path.Combine(projectRoot, scriptPath);
                string currentDir = Path.GetDirectoryName(scriptAbsPath);
                
                // 向上查找直到找到包含 Doc 文件夹的目录
                string searchDir = currentDir;
                
                // 限制查找深度，防止死循环
                int maxDepth = 10;
                while (maxDepth > 0 && !string.IsNullOrEmpty(searchDir))
                {
                    string potentialDocPath = Path.Combine(searchDir, "Doc");
                    if (Directory.Exists(potentialDocPath))
                    {
                        extensionPaths.Add(searchDir);
                        break; 
                    }
                    
                    // 向上移动一级
                    DirectoryInfo parentInfo = Directory.GetParent(searchDir);
                    if (parentInfo == null) break;
                    searchDir = parentInfo.FullName;
                    maxDepth--;
                }
            }
            
            // 从EditorPrefs读取EUExtensionManager配置的路径
            string extensionRootPath = EditorPrefs.GetString("EUExtensionManager_ExtensionRootPath", "Assets/EUFramework/Extension");
            string coreInstallPath = EditorPrefs.GetString("EUExtensionManager_CoreInstallPath", "Assets/EUFramework/Core");
            
            // 1. 扫描配置的扩展根目录
            string extensionRoot = extensionRootPath.StartsWith("Assets") 
                ? Path.Combine(Application.dataPath, "..", extensionRootPath)
                : extensionRootPath;
            extensionRoot = Path.GetFullPath(extensionRoot);
            
            if (Directory.Exists(extensionRoot))
            {
                extensionPaths.AddRange(Directory.GetDirectories(extensionRoot));
            }
            
            // 2. 扫描 Assets/Editor/EUExtensionManager 目录（扩展管理器自身）
            string editorExtensionRoot = Path.Combine(Application.dataPath, "Editor/EUExtensionManager");
            if (Directory.Exists(editorExtensionRoot))
            {
                extensionPaths.Add(editorExtensionRoot);
            }
            
            // 3. 扫描配置的Core目录
            string coreRoot = coreInstallPath.StartsWith("Assets")
                ? Path.Combine(Application.dataPath, "..", coreInstallPath)
                : coreInstallPath;
            coreRoot = Path.GetFullPath(coreRoot);
            
            if (Directory.Exists(coreRoot))
            {
                extensionPaths.Add(coreRoot);
                // 也扫描Core下的子目录
                var coreDirs = Directory.GetDirectories(coreRoot);
                if (coreDirs != null && coreDirs.Length > 0)
                {
                    extensionPaths.AddRange(coreDirs);
                }
            }
            
            if (extensionPaths.Count == 0)
            {
                return;
            }
            
            HashSet<string> scannedDocPaths = new HashSet<string>();

            foreach (var extPath in extensionPaths)
            {
                string docPath = Path.Combine(extPath, "Doc");
                
                // 标准化路径用于去重
                try 
                {
                    string normalizedDocPath = Path.GetFullPath(docPath).Replace("\\", "/").ToLower();
                    if (scannedDocPaths.Contains(normalizedDocPath)) continue;
                    scannedDocPaths.Add(normalizedDocPath);
                }
                catch {}

                if (!Directory.Exists(docPath)) continue;
                
                // 尝试读取extension.json获取显示名称
                string displayName = Path.GetFileName(extPath);
                string extensionJsonPath = Path.Combine(extPath, "extension.json");
                if (File.Exists(extensionJsonPath))
                {
                    try
                    {
                        string jsonContent = File.ReadAllText(extensionJsonPath, System.Text.Encoding.UTF8);
                        var match = Regex.Match(jsonContent, @"""displayName""\s*:\s*""([^""]+)""");
                        if (match.Success)
                        {
                            displayName = match.Groups[1].Value;
                        }
                    }
                    catch { /* 忽略JSON解析错误 */ }
                }
                
                // 创建扩展节点
                var extNode = new DocNode
                {
                    id = currentNodeId++,
                    name = displayName,
                    path = docPath,
                    isDirectory = true,
                    children = new List<DocNode>()
                };
                
                nodeIdMap[extNode.id] = extNode;
                
                // 扫描文档
                ScanDirectory(docPath, extNode);
                
                if (extNode.children.Count > 0)
                {
                    docNodes.Add(extNode);
                }
            }
        }
        
        private void CollectAllDocNodes(List<DocNode> nodes, List<DocNode> result)
        {
            foreach (var node in nodes)
            {
                if (!node.isDirectory)
                {
                    result.Add(node);
                }
                if (node.children != null && node.children.Count > 0)
                {
                    CollectAllDocNodes(node.children, result);
                }
            }
        }
        
        private void UpdateDocCount()
        {
            if (docCountLabel != null)
            {
                int totalDocs = allDocNodes.Count;
                int visibleDocs = CountVisibleDocs(docNodes);
                if (string.IsNullOrEmpty(currentSearchText))
                {
                    docCountLabel.text = $"文档总数: {totalDocs}";
                }
                else
                {
                    docCountLabel.text = $"搜索结果: {visibleDocs}/{totalDocs}";
                }
            }
        }
        
        private int CountVisibleDocs(List<DocNode> nodes)
        {
            int count = 0;
            foreach (var node in nodes)
            {
                if (!node.isDirectory)
                {
                    count++;
                }
                if (node.children != null && node.children.Count > 0)
                {
                    count += CountVisibleDocs(node.children);
                }
            }
            return count;
        }
        
        private void OnSearchTextChanged(string searchText)
        {
            currentSearchText = searchText.ToLower();
            lastSearchTime = EditorApplication.timeSinceStartup;
            isSearching = true;
            
            // 更新搜索导航可见性
            UpdateSearchNavVisibility();
            
            // 如果当前有打开的文档，重新渲染以更新高亮
            if (!string.IsNullOrEmpty(currentDocPath) && File.Exists(currentDocPath))
            {
                // 延迟一点执行，避免输入时频繁刷新导致卡顿
                // 这里我们暂不立即刷新内容，等搜索防抖结束后统一处理
                // 但为了更好的体验，如果文档不大，可以尝试立即刷新
                // 考虑到性能，我们还是在 PerformSearchAsync 中处理或者单独处理
            }

            // 如果是清空搜索，立即执行
            if (string.IsNullOrEmpty(searchText))
            {
                isSearching = false;
                PerformSearchAsync();
            }
        }
        
        private void UpdateSearchNavVisibility()
        {
            if (searchNavContainer != null)
            {
                bool showNav = !string.IsNullOrEmpty(currentSearchText) && 
                              currentSearchMode == SearchMode.Content && 
                              currentSearchMatches.Count > 0;
                searchNavContainer.style.display = showNav ? DisplayStyle.Flex : DisplayStyle.None;
                
                if (showNav)
                {
                    searchResultLabel.text = $"{currentMatchIndex + 1}/{currentSearchMatches.Count}";
                }
            }
        }
        
        private void NavigateSearchMatch(int direction)
        {
            if (currentSearchMatches.Count == 0) return;
            
            currentMatchIndex += direction;
            
            // 循环导航
            if (currentMatchIndex >= currentSearchMatches.Count) currentMatchIndex = 0;
            if (currentMatchIndex < 0) currentMatchIndex = currentSearchMatches.Count - 1;
            
            UpdateSearchNavVisibility();
            ScrollToMatch(currentSearchMatches[currentMatchIndex]);
        }
        
        private void ScrollToMatch(VisualElement matchElement)
        {
            if (matchElement == null || contentScrollView == null) return;
            
            try
            {
                // 移除旧的高亮
                foreach (var match in currentSearchMatches)
                {
                    match.RemoveFromClassList("search-match-active");
                    match.RemoveFromClassList("flash-highlight");
                }
                matchElement.AddToClassList("search-match-active");
                
                // 添加闪烁效果
                matchElement.AddToClassList("flash-highlight");
                // 延迟移除闪烁效果
                rootVisualElement.schedule.Execute(() => {
                    if (matchElement != null) matchElement.RemoveFromClassList("flash-highlight");
                }).StartingIn(500);
                
                isAutoScrolling = true;
                // 目标位置：元素位置减去顶部偏移，留出一点空间
                // 注意：matchElement 可能是嵌套在 Label 中的，我们需要它的世界坐标或者相对于 ScrollView 的坐标
                // 这里简化处理，假设 matchElement 是直接子元素或者我们可以获取其布局
                
                // 获取元素相对于 contentScrollView.contentContainer 的位置
                float targetY = matchElement.layout.y;
                VisualElement parent = matchElement.parent;
                while (parent != null && parent != contentScrollView.contentContainer)
                {
                    targetY += parent.layout.y;
                    parent = parent.parent;
                }
                
                targetY -= 100; // 留出更多顶部空间
                
                // 限制在可滚动范围内
                float maxScroll = contentScrollView.contentContainer.layout.height - contentScrollView.layout.height;
                if (maxScroll < 0) maxScroll = 0;
                targetY = Mathf.Clamp(targetY, 0, maxScroll);
                
                contentScrollView.scrollOffset = new Vector2(0, targetY);
                
                // 延迟重置滚动状态
                rootVisualElement.schedule.Execute(() => isAutoScrolling = false).StartingIn(500);
            }
            catch (Exception e)
            {
                Debug.LogError($"滚动到匹配项失败: {e.Message}");
                isAutoScrolling = false;
            }
        }

        private async void PerformSearchAsync()
        {
            if (string.IsNullOrEmpty(currentSearchText))
            {
                ShowSearchResults(false);
                FilterDocuments(null);
                return;
            }

            searchProgressBar.style.display = DisplayStyle.Flex;
            
            var searchText = currentSearchText;
            var mode = currentSearchMode;
            var nodesToCheck = new List<DocNode>(allDocNodes);
            
            // 用于普通模式（文件名搜索）
            var matchedIds = new HashSet<int>();
            
            // 用于内容搜索模式
            var newSearchResults = new List<SearchResultItem>();

            // 异步执行搜索
            await Task.Run(() => 
            {
                foreach (var node in nodesToCheck)
                {
                    if (mode == SearchMode.FileName)
                    {
                        if (node.name.ToLower().Contains(searchText)) 
                        {
                            lock(matchedIds) matchedIds.Add(node.id);
                        }
                    }
                    else
                    {
                        // 内容搜索
                        string content = "";
                        bool hasContent = false;
                        
                        lock(fileContentCache)
                        {
                            hasContent = fileContentCache.TryGetValue(node.path, out content);
                        }
                        
                        if (!hasContent)
                        {
                            try 
                            { 
                                content = File.ReadAllText(node.path); // 保持原始大小写用于显示
                                lock(fileContentCache)
                                {
                                    fileContentCache[node.path] = content;
                                }
                            }
                            catch { }
                        }
                        
                        if (content != null)
                        {
                            // 检查文件名
                            if (node.name.ToLower().Contains(searchText))
                            {
                                lock(newSearchResults) newSearchResults.Add(new SearchResultItem 
                                { 
                                    docNode = node, 
                                    lineContent = "文件名匹配", 
                                    lineNumber = -1 
                                });
                            }
                            
                            // 检查内容，按行分割
                            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                            for (int i = 0; i < lines.Length; i++)
                            {
                                if (lines[i].ToLower().Contains(searchText))
                                {
                                    lock(newSearchResults) newSearchResults.Add(new SearchResultItem 
                                    { 
                                        docNode = node, 
                                        lineContent = lines[i].Trim(), 
                                        lineNumber = i 
                                    });
                                    
                                    // 限制每个文件的匹配数量，防止过多
                                    // if (count > 10) break; 
                                }
                            }
                        }
                    }
                }
            });
            
            searchProgressBar.style.display = DisplayStyle.None;
            
            // 确保搜索词没有在搜索过程中改变
            if (currentSearchText == searchText)
            {
                if (mode == SearchMode.FileName)
                {
                    ShowSearchResults(false);
                    FilterDocuments(matchedIds);
                }
                else
                {
                    // 内容搜索模式，显示结果列表
                    searchResults = newSearchResults;
                    ShowSearchResults(true);
                    searchResultListView.itemsSource = searchResults;
                    searchResultListView.Rebuild();
                    
                    // 同时更新计数
                    if (docCountLabel != null)
                    {
                        docCountLabel.text = $"找到 {searchResults.Count} 个匹配项";
                    }
                }
            }
        }
        
        private void ShowSearchResults(bool show)
        {
            if (show)
            {
                docTreeView.style.display = DisplayStyle.None;
                searchResultListView.style.display = DisplayStyle.Flex;
            }
            else
            {
                docTreeView.style.display = DisplayStyle.Flex;
                searchResultListView.style.display = DisplayStyle.None;
            }
        }
        
        private void OnSearchResultSelected(IEnumerable<object> selectedItems)
        {
            var item = selectedItems.FirstOrDefault() as SearchResultItem;
            if (item != null)
            {
                LoadMarkdownFile(item.docNode.path, item.lineNumber);
            }
        }
        
        private void FilterDocuments(HashSet<int> matchedIds)
        {
            if (matchedIds == null)
            {
                // 显示所有文档
                RebuildTreeView();
                UpdateDocCount();
                return;
            }
            
            // 过滤文档
            var filteredNodes = new List<DocNode>();
            foreach (var node in docNodes)
            {
                var filteredNode = FilterNode(node, matchedIds);
                if (filteredNode != null)
                {
                    filteredNodes.Add(filteredNode);
                }
            }
            
            // 重建树视图
            var treeItems = new List<TreeViewItemData<DocNode>>();
            foreach (var node in filteredNodes)
            {
                treeItems.Add(BuildTreeItem(node));
            }
            
            docTreeView.SetRootItems(treeItems);
            docTreeView.Rebuild();
            
            // 如果有搜索结果，自动展开所有节点
            if (matchedIds.Count > 0)
            {
                docTreeView.ExpandAll();
            }
            
            UpdateDocCount();
        }
        
        private DocNode FilterNode(DocNode node, HashSet<int> matchedIds)
        {
            if (node.isDirectory)
            {
                // 对于目录，检查其子节点
                var filteredChildren = new List<DocNode>();
                if (node.children != null)
                {
                    foreach (var child in node.children)
                    {
                        var filteredChild = FilterNode(child, matchedIds);
                        if (filteredChild != null)
                        {
                            filteredChildren.Add(filteredChild);
                        }
                    }
                }
                
                if (filteredChildren.Count > 0)
                {
                    return new DocNode
                    {
                        id = node.id,
                        name = node.name,
                        path = node.path,
                        isDirectory = true,
                        children = filteredChildren
                    };
                }
                return null;
            }
            else
            {
                // 文件匹配逻辑：检查ID是否在匹配集合中
                return matchedIds.Contains(node.id) ? node : null;
            }
        }
        
        private void ScanDirectory(string dirPath, DocNode parentNode)
        {
            try
            {
                // 先添加文件
                var files = Directory.GetFiles(dirPath, "*.md");
                foreach (var file in files.OrderBy(f => Path.GetFileName(f)))
                {
                    var node = new DocNode
                    {
                        id = currentNodeId++,
                        name = Path.GetFileNameWithoutExtension(file),
                        path = file,
                        isDirectory = false
                    };
                    
                    nodeIdMap[node.id] = node;
                    parentNode.children.Add(node);
                }
                
                // 再添加子目录
                var dirs = Directory.GetDirectories(dirPath);
                foreach (var dir in dirs.OrderBy(d => Path.GetFileName(d)))
                {
                    var node = new DocNode
                    {
                        id = currentNodeId++,
                        name = Path.GetFileName(dir),
                        path = dir,
                        isDirectory = true,
                        children = new List<DocNode>()
                    };
                    
                    nodeIdMap[node.id] = node;
                    ScanDirectory(dir, node);
                    
                    if (node.children.Count > 0)
                    {
                        parentNode.children.Add(node);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"扫描目录失败: {dirPath}, 错误: {e.Message}");
            }
        }
        
        private void RebuildTreeView()
        {
            var treeItems = new List<TreeViewItemData<DocNode>>();
            
            foreach (var node in docNodes)
            {
                treeItems.Add(BuildTreeItem(node));
            }
            
            docTreeView.SetRootItems(treeItems);
            docTreeView.Rebuild();
        }
        
        private TreeViewItemData<DocNode> BuildTreeItem(DocNode node)
        {
            if (node.isDirectory && node.children != null && node.children.Count > 0)
            {
                var children = node.children.Select(BuildTreeItem).ToList();
                return new TreeViewItemData<DocNode>(node.id, node, children);
            }
            else
            {
                return new TreeViewItemData<DocNode>(node.id, node);
            }
        }
        
        private void OnTreeSelectionChanged(IEnumerable<object> selectedItems)
        {
            try
            {
                var selectedItem = selectedItems.FirstOrDefault();
                if (selectedItem == null) return;
                
                // 获取选中项的ID
                int selectedId = docTreeView.selectedIndex;
                if (selectedId < 0) return;
                
                // 从TreeView获取数据
                var itemData = docTreeView.GetItemDataForIndex<DocNode>(selectedId);
                if (itemData == null || itemData.isDirectory) return;
                
                LoadMarkdownFile(itemData.path);
            }
            catch (Exception e)
            {
                Debug.LogError($"选择文档失败: {e.Message}");
                ShowEmptyState($"选择文档失败: {e.Message}");
            }
        }
        
        private void LoadMarkdownFile(string filePath, int targetLineIndex = -1)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    ShowEmptyState("文件不存在");
                    return;
                }
                
                currentDocPath = filePath;
                pendingScrollLineIndex = targetLineIndex;
                UpdateCurrentDocPath();
                
                // 使用UTF-8编码读取文件
                string content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                RenderMarkdown(content, Path.GetFileNameWithoutExtension(filePath));
            }
            catch (Exception e)
            {
                ShowEmptyState($"加载文件失败: {e.Message}");
                Debug.LogError($"加载Markdown文件失败: {filePath}, 错误: {e.Message}");
            }
        }
        
        private void UpdateCurrentDocPath()
        {
            if (currentDocPathLabel != null && !string.IsNullOrEmpty(currentDocPath))
            {
                string relativePath = currentDocPath.Replace(Application.dataPath, "Assets");
                relativePath = relativePath.Replace("\\", "/");
                // 使用更美观的分隔符
                currentDocPathLabel.text = relativePath.Replace("/", "  ›  ");
            }
        }
        
        private void ShowEmptyState(string message)
        {
            contentScrollView.Clear();
            currentDocPath = "";
            if (currentDocPathLabel != null)
            {
                currentDocPathLabel.text = "";
            }
            emptyStateLabel = new Label(message);
            emptyStateLabel.AddToClassList("empty-state");
            contentScrollView.Add(emptyStateLabel);
            
            // 清空导航
            navScrollView.Clear();
            var emptyLabel = new Label("无标题");
            emptyLabel.AddToClassList("nav-empty");
            navScrollView.Add(emptyLabel);
        }
        
        private void RenderMarkdown(string markdown, string title)
        {
            // 停止之前的渲染
            isRendering = false;

            contentScrollView.Clear();
            currentHeaders.Clear();
            currentSearchMatches.Clear();
            currentMatchIndex = -1;
            UpdateSearchNavVisibility();
            
            CleanupResources(); // 清理旧资源
            
            var contentPanel = new VisualElement();
            contentPanel.AddToClassList("markdown-content");
            pendingContentPanel = contentPanel;
            
            // 标题
            var titleElement = new Label(title);
            titleElement.AddToClassList("markdown-title");
            CheckAndRegisterSearchMatch(title, titleElement, -1);
            contentPanel.Add(titleElement);
            
            contentScrollView.Add(contentPanel);
            
            // 初始化渲染状态
            pendingRenderLines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            currentRenderIndex = 0;
            renderInCodeBlock = false;
            renderCodeBlockContent = "";
            renderCodeBlockLanguage = "";
            renderInList = false;
            pendingParagraphContent = "";
            pendingParagraphStartLine = -1;
            renderInTable = false;
            currentTable = null;
            tableAlignments = null;
            potentialTableHeader = null;
            potentialTableHeaderLineIndex = -1;
            
            // 开始异步渲染
            isRendering = true;
        }

        private void ContinueRendering()
        {
            if (pendingRenderLines == null || pendingContentPanel == null)
            {
                isRendering = false;
                return;
            }

            int endIndex = Mathf.Min(currentRenderIndex + LINES_PER_FRAME, pendingRenderLines.Length);
            
            for (int i = currentRenderIndex; i < endIndex; i++)
            {
                ProcessLine(pendingRenderLines[i], i, pendingContentPanel);
            }
            
            currentRenderIndex = endIndex;
            
            if (currentRenderIndex >= pendingRenderLines.Length)
            {
                FinishRendering();
            }
        }

        private void FinishRendering()
        {
            // 确保最后的段落被输出
            if (pendingContentPanel != null)
            {
                FlushParagraph(pendingContentPanel);
                FlushPotentialTableHeader(pendingContentPanel);
            }

            isRendering = false;
            
            // 使用 USS 过渡动画
            pendingContentPanel.schedule.Execute(() => {
                pendingContentPanel.AddToClassList("markdown-content-visible");
            }).StartingIn(50);

            // 等待布局完成后更新导航
            pendingContentPanel.RegisterCallback<GeometryChangedEvent>(OnContentLayoutUpdated);
        }

        private void ProcessLine(string line, int index, VisualElement contentPanel)
        {
            // 代码块状态切换检测
            bool isCodeBlockFence = line.TrimStart().StartsWith("```");
            
            // 如果是代码块标记，或者是代码块内部内容
            if (isCodeBlockFence || renderInCodeBlock)
            {
                // 如果是开始新的代码块，先输出之前的段落
                if (isCodeBlockFence && !renderInCodeBlock)
                {
                    FlushParagraph(contentPanel);
                    FlushPotentialTableHeader(contentPanel);
                }
                
                // 代码块处理逻辑
                if (isCodeBlockFence)
                {
                    if (!renderInCodeBlock)
                    {
                        renderInCodeBlock = true;
                        renderCodeBlockLanguage = line.TrimStart().Substring(3).Trim();
                        renderCodeBlockContent = "";
                    }
                    else
                    {
                        renderInCodeBlock = false;
                        var codeBlock = CreateCodeBlock(renderCodeBlockContent, renderCodeBlockLanguage);
                        contentPanel.Add(codeBlock);
                        renderCodeBlockContent = "";
                        renderCodeBlockLanguage = "";
                    }
                    return;
                }
                
                if (renderInCodeBlock)
                {
                    renderCodeBlockContent += line + "\n";
                    return;
                }
            }

            // 表格处理
            if (renderInTable)
            {
                if (line.Trim().StartsWith("|"))
                {
                    AddTableRow(currentTable, line, false, index);
                    return;
                }
                else
                {
                    renderInTable = false;
                    currentTable = null;
                    tableAlignments = null;
                }
            }
            
            // 检测表格头
            if (potentialTableHeader != null)
            {
                // 检查当前行是否是分隔行 |---|---|
                if (IsTableSeparatorLine(line))
                {
                    // 是表格！
                    FlushParagraph(contentPanel); // 清空之前的段落
                    
                    renderInTable = true;
                    currentTable = new VisualElement();
                    currentTable.AddToClassList("markdown-table");
                    contentPanel.Add(currentTable);
                    
                    // 解析对齐方式
                    tableAlignments = ParseTableAlignments(line);
                    
                    // 添加表头
                    AddTableRow(currentTable, potentialTableHeader, true, potentialTableHeaderLineIndex);
                    
                    potentialTableHeader = null;
                    potentialTableHeaderLineIndex = -1;
                    return;
                }
                else
                {
                    // 不是表格，先把缓存的表头行输出了
                    FlushPotentialTableHeader(contentPanel);
                }
            }
            
            // 尝试缓存当前行作为潜在表头
            if (line.Trim().StartsWith("|") && line.Contains("|"))
            {
                FlushParagraph(contentPanel); // 遇到可能的表格，先结束上一段
                potentialTableHeader = line;
                potentialTableHeaderLineIndex = index;
                return;
            }

            // 预先检测其他块级元素，如果存在，先输出之前的段落
            bool isBlockElement = false;
            
            // 图片 (简单匹配)
            if (Regex.Match(line, @"!\[(.*?)\]\((.*?)\)").Success) isBlockElement = true;
            // 分割线
            else if (Regex.IsMatch(line, @"^(\*{3,}|-{3,}|_{3,})$")) isBlockElement = true;
            // 引用
            else if (line.TrimStart().StartsWith(">")) isBlockElement = true;
            // 标题
            else if (line.StartsWith("#")) isBlockElement = true;
            // 列表
            else if (Regex.IsMatch(line, @"^\s*[-*+]\s+") || Regex.IsMatch(line, @"^\s*\d+\.\s+")) isBlockElement = true;
            // 空行
            else if (string.IsNullOrWhiteSpace(line)) isBlockElement = true;

            if (isBlockElement)
            {
                FlushParagraph(contentPanel);
                FlushPotentialTableHeader(contentPanel);
            }

            // 图片处理 - 简单匹配 ![](url) 格式
            var imgMatch = Regex.Match(line, @"!\[(.*?)\]\((.*?)\)");
            if (imgMatch.Success)
            {
                string altText = imgMatch.Groups[1].Value;
                string imgPath = imgMatch.Groups[2].Value;
                
                if (IsVideoFile(imgPath) || altText.ToLower().Contains("video"))
                {
                    CreateVideoPlayer(imgPath, altText, contentPanel);
                }
                else
                {
                    CreateImage(imgPath, altText, contentPanel);
                }
                return;
            }

            // 分割线处理
            if (Regex.IsMatch(line, @"^(\*{3,}|-{3,}|_{3,})$"))
            {
                var separator = new VisualElement();
                separator.AddToClassList("markdown-separator");
                contentPanel.Add(separator);
                return;
            }
            
            // 引用块处理
            if (line.TrimStart().StartsWith(">"))
            {
                renderInList = false;
                var quote = CreateBlockquote(line, index);
                contentPanel.Add(quote);
                return;
            }

            // 标题处理
            if (line.StartsWith("#"))
            {
                renderInList = false;
                var header = CreateHeader(line, contentPanel, index);
                contentPanel.Add(header);
                return;
            }
            
            // 任务列表
            var taskMatch = Regex.Match(line, @"^\s*-\s+\[([ xX])\]\s+(.*)");
            if (taskMatch.Success)
            {
                renderInList = false; // 任务列表视为特殊列表
                bool isChecked = taskMatch.Groups[1].Value.ToLower() == "x";
                string text = taskMatch.Groups[2].Value;
                contentPanel.Add(CreateTaskItem(text, isChecked, index));
                return;
            }
            
            // 列表处理
            if (Regex.IsMatch(line, @"^\s*[-*+]\s+") || Regex.IsMatch(line, @"^\s*\d+\.\s+"))
            {
                if (!renderInList)
                {
                    renderInList = true;
                }
                var listItem = CreateListItem(line, index);
                contentPanel.Add(listItem);
                return;
            }
            else
            {
                renderInList = false;
            }
            
            // 空行
            if (string.IsNullOrWhiteSpace(line))
            {
                var spacer = new VisualElement();
                spacer.AddToClassList("markdown-spacer");
                contentPanel.Add(spacer);
                return;
            }
            
            // 普通段落 - 累积到 buffer
            ProcessParagraphLine(line, index, contentPanel);
        }

        private void ProcessParagraphLine(string line, int index, VisualElement contentPanel)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine)) return; 

            if (string.IsNullOrEmpty(pendingParagraphContent))
            {
                pendingParagraphContent = trimmedLine;
                pendingParagraphStartLine = index;
            }
            else
            {
                // 简单的合并策略：加空格
                // 优化：检测中英文边界
                char lastChar = pendingParagraphContent[pendingParagraphContent.Length - 1];
                char firstChar = trimmedLine.Length > 0 ? trimmedLine[0] : ' ';
                bool lastIsChinese = lastChar >= 0x4e00 && lastChar <= 0x9fa5;
                bool firstIsChinese = firstChar >= 0x4e00 && firstChar <= 0x9fa5;
                
                if (lastIsChinese && firstIsChinese)
                {
                    pendingParagraphContent += trimmedLine;
                }
                else
                {
                    pendingParagraphContent += " " + trimmedLine;
                }
            }
        }

        private bool IsTableSeparatorLine(string line)
        {
            string trimmed = line.Trim();
            if (!trimmed.Contains("|")) return false;
            string content = trimmed.Replace("|", "").Replace(" ", "");
            return Regex.IsMatch(content, @"^[-:]+$");
        }

        private List<TextAnchor> ParseTableAlignments(string line)
        {
            var alignments = new List<TextAnchor>();
            var parts = line.Split('|');
            // 跳过首尾空字符串
            int start = line.Trim().StartsWith("|") ? 1 : 0;
            int end = line.Trim().EndsWith("|") ? parts.Length - 1 : parts.Length;

            for (int i = start; i < end; i++)
            {
                string trimmed = parts[i].Trim();
                if (string.IsNullOrEmpty(trimmed) && parts.Length > 1) continue;
                
                bool left = trimmed.StartsWith(":");
                bool right = trimmed.EndsWith(":");
                
                if (left && right) alignments.Add(TextAnchor.MiddleCenter);
                else if (right) alignments.Add(TextAnchor.MiddleRight);
                else alignments.Add(TextAnchor.MiddleLeft);
            }
            return alignments;
        }

        private void AddTableRow(VisualElement table, string line, bool isHeader, int index)
        {
            var row = new VisualElement();
            row.AddToClassList("markdown-table-row");
            
            var cells = line.Split('|');
            int start = line.Trim().StartsWith("|") ? 1 : 0;
            int end = line.Trim().EndsWith("|") ? cells.Length - 1 : cells.Length;
            
            int colIndex = 0;
            for (int i = start; i < end; i++)
            {
                var cell = new VisualElement();
                cell.AddToClassList("markdown-table-cell");
                if (isHeader) cell.AddToClassList("markdown-table-header-cell");
                
                var content = cells[i].Trim();
                var label = new Label(ProcessInlineMarkdown(content));
                label.AddToClassList("markdown-table-cell-label");
                if (isHeader) label.AddToClassList("markdown-table-header-label");
                label.enableRichText = true;
                
                if (tableAlignments != null && colIndex < tableAlignments.Count)
                {
                    label.style.unityTextAlign = tableAlignments[colIndex];
                }
                
                CheckAndRegisterSearchMatch(content, label, index);
                
                cell.Add(label);
                row.Add(cell);
                colIndex++;
            }
            
            table.Add(row);
        }

        private VisualElement CreateTaskItem(string text, bool isChecked, int lineNumber)
        {
            var container = new VisualElement();
            container.AddToClassList("markdown-task-item");
            
            var checkbox = new VisualElement();
            checkbox.AddToClassList("markdown-task-checkbox");
            if (isChecked)
            {
                checkbox.AddToClassList("markdown-task-checkbox-checked");
                var mark = new Label("✓");
                mark.AddToClassList("markdown-task-check-mark");
                checkbox.Add(mark);
            }
            container.Add(checkbox);
            
            ParseAndAddContent(container, text, lineNumber);
            
            return container;
        }

        private void OnContentLayoutUpdated(GeometryChangedEvent evt)
        {
            var contentPanel = evt.target as VisualElement;
            contentPanel.UnregisterCallback<GeometryChangedEvent>(OnContentLayoutUpdated);
            UpdateNavigation();
            
            // 处理跳转
            if (pendingScrollLineIndex >= 0)
            {
                // 查找最接近的匹配项
                int bestMatchIndex = -1;
                int minDiff = int.MaxValue;
                
                for (int i = 0; i < currentSearchMatches.Count; i++)
                {
                    var match = currentSearchMatches[i];
                    if (match.userData is int lineNum)
                    {
                        int diff = Mathf.Abs(lineNum - pendingScrollLineIndex);
                        if (diff < minDiff)
                        {
                            minDiff = diff;
                            bestMatchIndex = i;
                        }
                    }
                }
                
                if (bestMatchIndex >= 0)
                {
                    currentMatchIndex = bestMatchIndex;
                    UpdateSearchNavVisibility();
                    ScrollToMatch(currentSearchMatches[currentMatchIndex]);
                }
                else if (currentSearchMatches.Count > 0)
                {
                    NavigateSearchMatch(1);
                }
                
                pendingScrollLineIndex = -1;
            }
            else if (currentSearchMatches.Count > 0)
            {
                // 如果不是点击跳转，而是直接打开文件且有搜索词，也跳转第一个
                NavigateSearchMatch(1);
            }
        }
        
        private void CheckAndRegisterSearchMatch(string text, VisualElement element, int lineNumber)
        {
            if (!string.IsNullOrEmpty(currentSearchText) && 
                currentSearchMode == SearchMode.Content && 
                !string.IsNullOrEmpty(text) &&
                text.ToLower().Contains(currentSearchText))
            {
                element.AddToClassList("search-match-line");
                element.userData = lineNumber;
                currentSearchMatches.Add(element);
            }
        }

        private void CreateImage(string path, string altText, VisualElement parent)
        {
            try
            {
                // 处理网络图片
                if (path.StartsWith("http://") || path.StartsWith("https://"))
                {
                    LoadRemoteImage(path, altText, parent);
                    return;
                }

                // 处理本地路径
                string fullPath = path;
                if (!Path.IsPathRooted(path))
                {
                    // 假设图片相对于当前文档
                    string docDir = Path.GetDirectoryName(currentDocPath);
                    fullPath = Path.Combine(docDir, path);
                    // 标准化路径，处理 ../
                    fullPath = Path.GetFullPath(fullPath);
                }

                if (File.Exists(fullPath))
                {
                    byte[] fileData = File.ReadAllBytes(fullPath);
                    var texture = new Texture2D(2, 2);
                    texture.LoadImage(fileData);
                    loadedTextures.Add(texture); // 记录以便销毁
                    
                    DisplayImage(texture, altText, parent);
                }
                else
                {
                    // 图片不存在，显示占位符
                    var errorLabel = new Label($"[图片丢失: {altText}] ({path})");
                    errorLabel.style.color = Color.red;
                    errorLabel.AddToClassList("markdown-paragraph");
                    parent.Add(errorLabel);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"加载图片失败: {path}, {e.Message}");
            }
        }

        private async void LoadRemoteImage(string url, string altText, VisualElement parent)
        {
            // 创建占位符
            var placeholder = new Label($"[加载图片中: {altText}]...");
            placeholder.AddToClassList("markdown-paragraph");
            parent.Add(placeholder);

            try
            {
                using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
                {
                    var operation = uwr.SendWebRequest();
                    while (!operation.isDone) await Task.Yield();

                    if (uwr.result != UnityWebRequest.Result.Success)
                    {
                        placeholder.text = $"[图片加载失败: {altText}] ({url})";
                        placeholder.style.color = Color.red;
                    }
                    else
                    {
                        var texture = DownloadHandlerTexture.GetContent(uwr);
                        if (texture != null)
                        {
                            loadedTextures.Add(texture);
                            // 移除占位符
                            parent.Remove(placeholder);
                            DisplayImage(texture, altText, parent);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                placeholder.text = $"[图片错误: {altText}]";
                Debug.LogError($"下载图片失败: {e.Message}");
            }
        }

        private void DisplayImage(Texture2D texture, string altText, VisualElement parent)
        {
            var imgContainer = new VisualElement();
            imgContainer.AddToClassList("markdown-image-container");
            
            var img = new Image();
            img.image = texture;
            img.scaleMode = ScaleMode.ScaleToFit;
            img.AddToClassList("markdown-image");
            
            // 图片点击交互 - 简单的放大/缩小效果
            img.RegisterCallback<ClickEvent>(evt => {
                if (img.ClassListContains("markdown-image-expanded"))
                {
                    img.RemoveFromClassList("markdown-image-expanded");
                }
                else
                {
                    img.AddToClassList("markdown-image-expanded");
                }
            });
            
            imgContainer.Add(img);
            
            if (!string.IsNullOrEmpty(altText))
            {
                var caption = new Label(altText);
                caption.AddToClassList("markdown-image-caption");
                imgContainer.Add(caption);
            }
            
            parent.Add(imgContainer);
        }

        private void CreateVideoPlayer(string path, string altText, VisualElement parent)
        {
            try
            {
                var container = new VisualElement();
                container.AddToClassList("markdown-video-container");
                
                // 视频显示区域
                var videoScreen = new Image();
                videoScreen.AddToClassList("markdown-video-screen");
                container.Add(videoScreen);
                
                // 控制栏
                var controls = new VisualElement();
                controls.AddToClassList("markdown-video-controls");
                
                var playBtn = new Button() { text = "▶" };
                playBtn.AddToClassList("video-control-btn");
                controls.Add(playBtn);
                
                var stopBtn = new Button() { text = "■" };
                stopBtn.AddToClassList("video-control-btn");
                controls.Add(stopBtn);
                
                var timeLabel = new Label("00:00 / 00:00");
                timeLabel.AddToClassList("video-time-label");
                controls.Add(timeLabel);
                
                container.Add(controls);
                parent.Add(container);
                
                // 创建 VideoPlayer GameObject
                var go = new GameObject($"VideoPlayer_{path}");
                go.hideFlags = HideFlags.HideAndDontSave;
                var player = go.AddComponent<VideoPlayer>();
                activeVideoPlayers.Add(player);
                
                // 设置视频源
                if (path.StartsWith("http://") || path.StartsWith("https://"))
                {
                    player.url = path;
                    player.source = VideoSource.Url;
                }
                else
                {
                    string fullPath = path;
                    if (!Path.IsPathRooted(path))
                    {
                        string docDir = Path.GetDirectoryName(currentDocPath);
                        fullPath = Path.Combine(docDir, path);
                        fullPath = Path.GetFullPath(fullPath);
                    }
                    player.url = fullPath;
                    player.source = VideoSource.Url;
                }
                
                // 设置渲染目标
                player.playOnAwake = false;
                player.isLooping = true;
                player.renderMode = VideoRenderMode.RenderTexture;
                
                // 准备完成后创建 RenderTexture
                player.prepareCompleted += (source) => {
                    var rt = new RenderTexture((int)source.width, (int)source.height, 0);
                    source.targetTexture = rt;
                    videoScreen.image = rt;
                    
                    // 调整显示比例
                    float aspect = (float)source.width / source.height;
                    // 限制最大高度，宽度自适应
                    // 但在USS中我们设置了固定高度，这里可以根据宽度调整高度
                    // 或者保持 scale-to-fit
                    
                    // 更新总时长
                    UpdateTimeLabel(player, timeLabel);
                };
                
                player.Prepare();
                
                // 按钮事件
                playBtn.clicked += () => {
                    if (player.isPlaying)
                    {
                        player.Pause();
                        playBtn.text = "▶";
                    }
                    else
                    {
                        player.Play();
                        playBtn.text = "❚❚";
                    }
                };
                
                stopBtn.clicked += () => {
                    player.Stop();
                    playBtn.text = "▶";
                    UpdateTimeLabel(player, timeLabel);
                };
                
                // 更新时间显示
                container.schedule.Execute(() => {
                    if (player != null && player.isPlaying)
                    {
                        UpdateTimeLabel(player, timeLabel);
                    }
                }).Every(500); // 每0.5秒更新一次
            }
            catch (Exception e)
            {
                Debug.LogError($"创建视频播放器失败: {e.Message}");
                var errorLabel = new Label($"[视频加载失败: {altText}]");
                errorLabel.style.color = Color.red;
                parent.Add(errorLabel);
            }
        }

        private void UpdateTimeLabel(VideoPlayer player, Label label)
        {
            if (player == null || label == null) return;
            
            string FormatTime(double seconds)
            {
                TimeSpan t = TimeSpan.FromSeconds(seconds);
                return string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
            }
            
            label.text = $"{FormatTime(player.time)} / {FormatTime(player.length)}";
        }
        
        private VisualElement CreateHeader(string line, VisualElement parent, int lineNumber)
        {
            int level = 0;
            while (level < line.Length && line[level] == '#')
            {
                level++;
            }
            
            string text = line.Substring(level).Trim();
            var label = new Label(text);
            label.AddToClassList("markdown-header");
            label.AddToClassList($"markdown-h{level}");
            
            CheckAndRegisterSearchMatch(text, label, lineNumber);
            
            // 记录标题信息用于导航
            var headerInfo = new HeaderInfo
            {
                level = level,
                text = text,
                element = label
            };
            currentHeaders.Add(headerInfo);
            
            return label;
        }
        
        private void UpdateNavigation()
        {
            navScrollView.Clear();
            currentActiveNavItem = null;
            
            if (currentHeaders.Count == 0)
            {
                var emptyLabel = new Label("无标题");
                emptyLabel.AddToClassList("nav-empty");
                navScrollView.Add(emptyLabel);
                return;
            }
            
            foreach (var header in currentHeaders)
            {
                var navItem = new Button(() => ScrollToHeader(header.element));
                navItem.text = header.text;
                navItem.AddToClassList("nav-item");
                navItem.AddToClassList($"nav-item-h{header.level}");
                // 存储对应的 header 元素以便查找
                navItem.userData = header.element; 
                navScrollView.Add(navItem);
            }
            
            // 延迟一帧计算布局，以便正确高亮第一个
            rootVisualElement.schedule.Execute(() => OnContentScroll(0)).StartingIn(100);
        }
        
        private void ScrollToHeader(VisualElement headerElement)
        {
            if (headerElement == null || contentScrollView == null) return;
            
            try
            {
                isAutoScrolling = true;
                // 目标位置：元素位置减去顶部偏移，留出一点空间
                float targetY = headerElement.layout.y - 10;
                // 限制在可滚动范围内
                float maxScroll = contentScrollView.contentContainer.layout.height - contentScrollView.layout.height;
                if (maxScroll < 0) maxScroll = 0;
                targetY = Mathf.Clamp(targetY, 0, maxScroll);
                
                // 平滑滚动模拟
                float startY = contentScrollView.scrollOffset.y;
                float duration = 0.25f; // 稍微加快一点
                float startTime = Time.realtimeSinceStartup;
                
                // 使用 Every 确保更稳定的更新频率
                rootVisualElement.schedule.Execute(() => {
                    // 如果已经被销毁或不再需要滚动
                    if (contentScrollView == null || !isAutoScrolling) return;

                    float t = (float)(Time.realtimeSinceStartup - startTime) / duration;
                    if (t >= 1.0f)
                    {
                        contentScrollView.scrollOffset = new Vector2(0, targetY);
                        isAutoScrolling = false;
                        // 滚动结束后更新高亮
                        OnContentScroll(targetY);
                    }
                    else
                    {
                        // EaseOutCubic
                        t = 1 - Mathf.Pow(1 - t, 3);
                        float currentY = Mathf.Lerp(startY, targetY, t);
                        contentScrollView.scrollOffset = new Vector2(0, currentY);
                    }
                }).Every(16).Until(() => !isAutoScrolling); // 每16ms执行一次，直到滚动结束
            }
            catch (Exception e)
            {
                Debug.LogError($"滚动到标题失败: {e.Message}");
                isAutoScrolling = false;
            }
        }

        private void OnContentScroll(float value)
        {
            if (isAutoScrolling || currentHeaders.Count == 0) return;
            
            float scrollY = contentScrollView.scrollOffset.y;
            VisualElement activeHeader = null;
            
            // 查找当前可见的标题
            // 简单的算法：找到最后一个 layout.y <= scrollY + offset 的标题
            float offset = 50; // 顶部偏移量
            
            foreach (var header in currentHeaders)
            {
                if (header.element.layout.y <= scrollY + offset)
                {
                    activeHeader = header.element;
                }
                else
                {
                    break;
                }
            }
            
            // 如果没有找到（比如在最顶部），默认第一个
            if (activeHeader == null && currentHeaders.Count > 0)
            {
                activeHeader = currentHeaders[0].element;
            }
            
            // 更新导航高亮
            UpdateActiveNavItem(activeHeader);
        }

        private void UpdateActiveNavItem(VisualElement activeHeader)
        {
            if (activeHeader == null) return;
            
            // 查找对应的导航项
            VisualElement targetNavItem = null;
            foreach (var child in navScrollView.Children())
            {
                if (child.userData == activeHeader)
                {
                    targetNavItem = child;
                    break;
                }
            }
            
            if (targetNavItem != currentActiveNavItem)
            {
                if (currentActiveNavItem != null)
                {
                    currentActiveNavItem.RemoveFromClassList("nav-item-active");
                }
                
                if (targetNavItem != null)
                {
                    targetNavItem.AddToClassList("nav-item-active");
                    currentActiveNavItem = targetNavItem;
                    
                    // 确保导航项在视图中
                    navScrollView.ScrollTo(targetNavItem);
                }
            }
        }
        
        private VisualElement CreateParagraph(string line, int lineNumber)
        {
            var container = new VisualElement();
            container.AddToClassList("markdown-paragraph-container");
            
            ParseAndAddContent(container, line, lineNumber);
            
            return container;
        }
        
        private VisualElement CreateListItem(string line, int lineNumber)
        {
            var container = new VisualElement();
            container.AddToClassList("markdown-list-item-container");
            
            // 列表标记
            var bullet = new Label("•");
            bullet.AddToClassList("markdown-list-bullet");
            container.Add(bullet);

            // 移除列表标记（无序列表的 -、*、+ 或有序列表的数字.）
            string text = Regex.Replace(line, @"^\s*([-*+]|\d+\.)\s+", "");
            
            ParseAndAddContent(container, text, lineNumber);
            
            return container;
        }

        private VisualElement CreateBlockquote(string line, int lineNumber)
        {
            var container = new VisualElement();
            container.AddToClassList("markdown-blockquote");
            
            string text = line.TrimStart().Substring(1).Trim();
            ParseAndAddContent(container, text, lineNumber);
            
            return container;
        }

        private bool IsVideoFile(string path)
        {
            return path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) || 
                   path.EndsWith(".mov", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".webm", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".ogv", StringComparison.OrdinalIgnoreCase);
        }

        private void ParseAndAddContent(VisualElement container, string text, int lineNumber)
        {
            // 正则匹配链接 [text](url) 和 删除线 ~~text~~
            string pattern = @"(\[([^\]]+)\]\(([^\)]+)\))|(~~([^~]+)~~)";
            var matches = Regex.Matches(text, pattern);
            
            int lastIndex = 0;
            
            foreach (Match match in matches)
            {
                // 添加匹配项前的文本
                if (match.Index > lastIndex)
                {
                    string normalText = text.Substring(lastIndex, match.Index - lastIndex);
                    AddTextSegment(container, normalText, lineNumber);
                }
                
                if (match.Groups[1].Success) // Link
                {
                    string linkText = match.Groups[2].Value;
                    string linkUrl = match.Groups[3].Value;
                    
                    if (IsVideoFile(linkUrl))
                    {
                        CreateVideoPlayer(linkUrl, linkText, container);
                    }
                    else
                    {
                        AddLinkSegment(container, linkText, linkUrl);
                    }
                }
                else if (match.Groups[4].Success) // Strikethrough
                {
                    string strikeText = match.Groups[5].Value;
                    AddStrikethroughSegment(container, strikeText, lineNumber);
                }
                
                lastIndex = match.Index + match.Length;
            }
            
            // 添加剩余文本
            if (lastIndex < text.Length)
            {
                string remainingText = text.Substring(lastIndex);
                AddTextSegment(container, remainingText, lineNumber);
            }
        }

        private void AddStrikethroughSegment(VisualElement container, string text, int lineNumber)
        {
            var label = new Label(ProcessInlineMarkdown(text));
            label.AddToClassList("markdown-paragraph");
            label.AddToClassList("markdown-strikethrough");
            label.enableRichText = true;
            CheckAndRegisterSearchMatch(text, label, lineNumber);
            container.Add(label);
        }

        private void AddTextSegment(VisualElement container, string text, int lineNumber)
        {
            var label = new Label(ProcessInlineMarkdown(text));
            label.AddToClassList("markdown-paragraph");
            label.enableRichText = true;
            CheckAndRegisterSearchMatch(text, label, lineNumber);
            container.Add(label);
        }

        private void AddLinkSegment(VisualElement container, string text, string url)
        {
            var btn = new Button(() => OnLinkClick(url));
            btn.text = text;
            btn.AddToClassList("markdown-link");
            btn.tooltip = url;
            container.Add(btn);
        }

        private void OnLinkClick(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            if (url.StartsWith("http://") || url.StartsWith("https://") || url.StartsWith("mailto:"))
            {
                Application.OpenURL(url);
            }
            else
            {
                // 处理文档内跳转或相对路径文档跳转
                HandleInternalLink(url);
            }
        }

        private void HandleInternalLink(string url)
        {
            // 1. 锚点跳转 #header
            if (url.StartsWith("#"))
            {
                string anchor = url.Substring(1);
                if (string.IsNullOrEmpty(anchor)) return;

                // 归一化函数：移除所有非字母数字汉字字符，转小写
                // 这样可以忽略标点符号、空格、连字符等的差异
                string Normalize(string s) => Regex.Replace(s, @"[^\w\u4e00-\u9fa5]", "").ToLower();
                
                string targetAnchorNormalized = Normalize(anchor);
                
                HeaderInfo targetHeader = null;
                
                // 策略1：归一化后完全匹配
                foreach (var header in currentHeaders)
                {
                    string headerNormalized = Normalize(header.text);
                    if (headerNormalized == targetAnchorNormalized)
                    {
                        targetHeader = header;
                        break;
                    }
                }
                
                // 策略2：如果策略1失败，尝试包含匹配（归一化后）
                if (targetHeader == null)
                {
                    foreach (var header in currentHeaders)
                    {
                        string headerNormalized = Normalize(header.text);
                        // 只要有一方包含另一方，就认为是匹配的
                        // 这种宽松匹配可以解决大部分锚点生成规则不一致的问题
                        if (!string.IsNullOrEmpty(headerNormalized) && !string.IsNullOrEmpty(targetAnchorNormalized) &&
                           (headerNormalized.Contains(targetAnchorNormalized) || targetAnchorNormalized.Contains(headerNormalized)))
                        {
                            targetHeader = header;
                            break;
                        }
                    }
                }

                if (targetHeader != null)
                {
                    ScrollToHeader(targetHeader.element);
                }
                else
                {
                    Debug.LogWarning($"未找到锚点对应的标题: {url} (Normalized: {targetAnchorNormalized})");
                }
                return;
            }

            // 2. 相对路径文件跳转
            try
            {
                string targetPath = url;
                if (!Path.IsPathRooted(url))
                {
                    string currentDir = Path.GetDirectoryName(currentDocPath);
                    targetPath = Path.Combine(currentDir, url);
                    targetPath = Path.GetFullPath(targetPath);
                }

                if (File.Exists(targetPath))
                {
                    // 检查是否是支持的文件类型
                    if (targetPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    {
                        // 尝试在树中找到并选中该节点（可选，或者直接加载）
                        // 为了保持状态一致性，最好是选中树节点
                        var node = FindNodeByPath(targetPath);
                        if (node != null)
                        {
                            // 选中树节点会触发加载
                            // 注意：TreeView的选中API可能比较复杂，这里直接加载文件并尝试同步树状态
                            LoadMarkdownFile(targetPath);
                            // TODO: 同步树选中状态
                        }
                        else
                        {
                            // 如果不在树中（例如在根目录之外），直接加载
                            LoadMarkdownFile(targetPath);
                        }
                    }
                    else
                    {
                        // 其他文件类型，尝试用系统默认程序打开
                        EditorUtility.OpenWithDefaultApp(targetPath);
                    }
                }
                else
                {
                    Debug.LogWarning($"链接文件不存在: {targetPath}");
                    EditorUtility.DisplayDialog("链接错误", $"文件不存在:\n{targetPath}", "确定");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"处理链接失败: {url}, {e.Message}");
            }
        }

        private DocNode FindNodeByPath(string path)
        {
            string normalizedPath = path.Replace("\\", "/").ToLower();
            return allDocNodes.FirstOrDefault(n => n.path.Replace("\\", "/").ToLower() == normalizedPath);
        }
        
        private VisualElement CreateCodeBlock(string code, string language)
        {
            var container = new VisualElement();
            container.AddToClassList("markdown-code-block");
            
            // Header
            var header = new VisualElement();
            header.AddToClassList("code-header");
            container.Add(header);

            // Language
            var langText = string.IsNullOrEmpty(language) ? "Code" : language;
            var langLabel = new Label(langText);
            langLabel.AddToClassList("code-language");
            header.Add(langLabel);

            // Copy Button
            var copyBtn = new Button(() => {
                GUIUtility.systemCopyBuffer = code;
            });
            copyBtn.text = "复制";
            copyBtn.AddToClassList("copy-button");
            
            // 复制反馈
            copyBtn.clicked += () => {
                copyBtn.text = "已复制";
                copyBtn.schedule.Execute(() => copyBtn.text = "复制").StartingIn(2000);
            };
            
            header.Add(copyBtn);
            
            var codeLabel = new Label(code.TrimEnd());
            codeLabel.AddToClassList("code-content");
            container.Add(codeLabel);
            
            return container;
        }
        
        private string ProcessInlineMarkdown(string text)
        {
            // 使用Unity支持的富文本标签替换Markdown标记
            // 性能优化：直接使用Regex替换为Rich Text Tags，减少VisualElement数量
            
            // 搜索高亮
            if (!string.IsNullOrEmpty(currentSearchText) && currentSearchMode == SearchMode.Content)
            {
                // 简单的替换，不区分大小写
                string pattern = Regex.Escape(currentSearchText);
                // 使用金色加粗高亮
                text = Regex.Replace(text, pattern, match => $"<color=#FFD700><b>{match.Value}</b></color>", RegexOptions.IgnoreCase);
            }
            
            // 行内代码 `code` -> <color=#...>code</color>
            text = Regex.Replace(text, @"`([^`]+)`", "<color=#DCDCAA>$1</color>");
            
            // 粗体 **text** -> <b>text</b>
            text = Regex.Replace(text, @"\*\*([^*]+)\*\*", "<b>$1</b>");
            text = Regex.Replace(text, @"__([^_]+)__", "<b>$1</b>");
            
            // 斜体 *text* -> <i>text</i>
            text = Regex.Replace(text, @"\*([^*]+)\*", "<i>$1</i>");
            text = Regex.Replace(text, @"_([^_]+)_", "<i>$1</i>");
            
            // 注意：链接现在由 ParseAndAddContent 单独处理，这里不再处理链接正则
            // 但为了防止漏网之鱼（例如嵌套在其他标记中的），保留一个简单的颜色处理
            // text = Regex.Replace(text, @"\[([^\]]+)\]\([^\)]+\)", "<color=#4EC9B0>$1</color>");
            
            return text;
        }
        
        private class DocNode
        {
            public int id;
            public string name;
            public string path;
            public bool isDirectory;
            public List<DocNode> children;
            
            public override string ToString()
            {
                return name;
            }
        }
        
        private class HeaderInfo
        {
            public int level;
            public string text;
            public VisualElement element;
        }
        
        private class SearchResultItem
        {
            public DocNode docNode;
            public string lineContent;
            public int lineNumber;
        }
    }

    public class EUMarkdownDocSettingsWindow : EditorWindow
    {
        private Action m_OnClose;
        private EUMarkdownDocReaderWindow.DocReadMode m_ReadMode;
        private string m_UniversalPath;
        
        private PopupField<string> m_ModeField;
        private TextField m_PathField;
        private Button m_SelectPathBtn;

        public static void ShowWindow(Action onClose = null)
        {
            var wnd = GetWindow<EUMarkdownDocSettingsWindow>(true, "文档阅读器设置", true);
            wnd.minSize = new Vector2(400, 200);
            wnd.maxSize = new Vector2(400, 250);
            wnd.m_OnClose = onClose;
            wnd.Show();
        }

        private void OnEnable()
        {
            // 加载设置
            m_ReadMode = (EUMarkdownDocReaderWindow.DocReadMode)EditorPrefs.GetInt("EUMarkdownDoc_ReadMode", 0);
            m_UniversalPath = EditorPrefs.GetString("EUMarkdownDoc_UniversalPath", "");
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingTop = 20;
            root.style.paddingBottom = 20;
            root.style.paddingLeft = 20;
            root.style.paddingRight = 20;

            var title = new Label("设置");
            title.style.fontSize = 18;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 15;
            root.Add(title);

            // 模式选择
            var modeOptions = new List<string> { "默认模式", "通用模式" };
            int index = (int)m_ReadMode;
            if (index < 0 || index >= modeOptions.Count) index = 0;
            
            m_ModeField = new PopupField<string>("读取模式", modeOptions, index);
            m_ModeField.RegisterValueChangedCallback(evt => {
                m_ReadMode = evt.newValue == "通用模式" 
                    ? EUMarkdownDocReaderWindow.DocReadMode.Universal 
                    : EUMarkdownDocReaderWindow.DocReadMode.Default;
                UpdateUIState();
            });
            root.Add(m_ModeField);

            // 路径选择容器
            var pathContainer = new VisualElement();
            pathContainer.style.marginTop = 10;
            pathContainer.name = "path-container";
            
            var pathRow = new VisualElement();
            pathRow.style.flexDirection = FlexDirection.Row;
            
            m_PathField = new TextField("文档路径");
            m_PathField.value = m_UniversalPath;
            m_PathField.style.flexGrow = 1;
            m_PathField.RegisterValueChangedCallback(evt => m_UniversalPath = evt.newValue);
            pathRow.Add(m_PathField);
            
            m_SelectPathBtn = new Button(OnSelectPath) { text = "..." };
            m_SelectPathBtn.style.width = 30;
            pathRow.Add(m_SelectPathBtn);
            
            pathContainer.Add(pathRow);
            
            var helpBox = new Label("选择包含Markdown文档的文件夹。");
            helpBox.style.fontSize = 10;
            helpBox.style.color = new Color(0.6f, 0.6f, 0.6f);
            helpBox.style.marginTop = 2;
            helpBox.style.marginLeft = 120; // 对齐输入框
            pathContainer.Add(helpBox);
            
            root.Add(pathContainer);

            // 底部按钮
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.FlexEnd;
            footer.style.marginTop = 20;
            
            var saveBtn = new Button(OnSave) { text = "保存并关闭" };
            saveBtn.style.height = 30;
            saveBtn.style.paddingLeft = 20;
            saveBtn.style.paddingRight = 20;
            footer.Add(saveBtn);
            
            root.Add(footer);

            UpdateUIState();
        }

        private void UpdateUIState()
        {
            var pathContainer = rootVisualElement.Q("path-container");
            if (pathContainer != null)
            {
                pathContainer.style.display = m_ReadMode == EUMarkdownDocReaderWindow.DocReadMode.Universal 
                    ? DisplayStyle.Flex 
                    : DisplayStyle.None;
            }
        }

        private void OnSelectPath()
        {
            string path = EditorUtility.OpenFolderPanel("选择文档目录", m_UniversalPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                // 转换为相对路径（如果在项目中）
                string projectPath = Path.GetFullPath(Application.dataPath).Replace("\\", "/");
                string fullPath = path.Replace("\\", "/");
                
                // 如果在Assets下，可以转为相对路径，但通用模式通常支持任意路径
                // 这里我们保持完整路径，或者根据需求处理
                m_UniversalPath = fullPath;
                m_PathField.value = m_UniversalPath;
            }
        }

        private void OnSave()
        {
            EUMarkdownDocReaderWindow.SaveSettings(m_ReadMode, m_UniversalPath);
            m_OnClose?.Invoke();
            Close();
        }
    }
}
#endif
