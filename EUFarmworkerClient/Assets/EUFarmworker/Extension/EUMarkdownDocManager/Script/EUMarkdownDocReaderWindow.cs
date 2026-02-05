#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EUFarmworker.MarkdownDocManager
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
        private VisualElement navPanel;
        private VisualElement navHeader;
        
        private List<DocNode> docNodes = new List<DocNode>();
        private List<DocNode> allDocNodes = new List<DocNode>();
        private Dictionary<int, DocNode> nodeIdMap = new Dictionary<int, DocNode>();
        private int currentNodeId = 0;
        private string currentSearchText = "";
        private string currentDocPath = "";
        private List<HeaderInfo> currentHeaders = new List<HeaderInfo>();
        private Dictionary<string, string> fileContentCache = new Dictionary<string, string>();
        private List<Texture2D> loadedTextures = new List<Texture2D>();
        private bool isSearching = false;
        private SearchMode currentSearchMode = SearchMode.FileName;
        private double lastSearchTime;
        private const double SearchDelay = 0.3f; // 300ms 防抖
        
        // 滚动监听相关
        private bool isAutoScrolling = false;
        private VisualElement currentActiveNavItem = null;

        public enum SearchMode
        {
            FileName,
            Content
        }

        [MenuItem("EUFarmworker/Markdown文档阅读器")]
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
            EditorApplication.update -= OnEditorUpdate;
            CleanupTextures();
        }

        private void OnDestroy()
        {
            CleanupTextures();
        }

        private void CleanupTextures()
        {
            foreach (var tex in loadedTextures)
            {
                if (tex != null)
                {
                    DestroyImmediate(tex);
                }
            }
            loadedTextures.Clear();
        }

        private void OnEditorUpdate()
        {
            // 处理搜索防抖
            if (isSearching && EditorApplication.timeSinceStartup - lastSearchTime > SearchDelay)
            {
                isSearching = false;
                PerformSearch();
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
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/EUFarmworker/Extension/EUMarkdownDocManager/ConfigPanel/EUMarkdownDocReader.uss");
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
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
            
            topBar.Add(searchContainer);

            // 搜索进度条
            searchProgressBar = new ProgressBar();
            searchProgressBar.style.display = DisplayStyle.None;
            searchProgressBar.style.width = 100;
            searchProgressBar.style.marginLeft = 10;
            topBar.Add(searchProgressBar);
            
            // 刷新按钮
            var refreshButton = new Button(() => LoadDocuments()) { text = "刷新" };
            refreshButton.AddToClassList("action-button");
            refreshButton.AddToClassList("btn-primary");
            topBar.Add(refreshButton);
            
            rootContainer.Add(topBar);
            
            // 分割视图
            var splitView = new VisualElement();
            splitView.AddToClassList("split-view");
            
            // 左侧文档树
            var treeContainer = new VisualElement();
            treeContainer.AddToClassList("doc-tree-container");
            
            // 文档统计标签
            docCountLabel = new Label("文档: 0");
            docCountLabel.AddToClassList("doc-count-label");
            treeContainer.Add(docCountLabel);
            
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
            treeContainer.Add(docTreeView);
            
            splitView.Add(treeContainer);
            
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
            contentSplitView.Add(navPanel);
            
            contentContainer.Add(contentSplitView);
            
            splitView.Add(contentContainer);
            
            rootContainer.Add(splitView);
        }
        
        private void BuildNavigationPanel()
        {
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
        }
        
        private void LoadDocuments()
        {
            docNodes.Clear();
            nodeIdMap.Clear();
            fileContentCache.Clear();
            currentNodeId = 0;
            
            try
            {
                // 扫描扩展目录
                List<string> extensionPaths = new List<string>();
                
                // 从EditorPrefs读取EUExtensionManager配置的路径
                string extensionRootPath = EditorPrefs.GetString("EUExtensionManager_ExtensionRootPath", "Assets/EUFarmworker/Extension");
                string coreInstallPath = EditorPrefs.GetString("EUExtensionManager_CoreInstallPath", "Assets/EUFarmworker/Core");
                
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
                    RebuildTreeView();
                    ShowEmptyState("未找到任何扩展目录");
                    return;
                }
                
                foreach (var extPath in extensionPaths)
                {
                    string docPath = Path.Combine(extPath, "Doc");
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
                
                RebuildTreeView();
                
                // 收集所有文档节点用于搜索
                allDocNodes.Clear();
                CollectAllDocNodes(docNodes, allDocNodes);
                
                if (docNodes.Count == 0)
                {
                    ShowEmptyState("未找到任何文档");
                }
                
                UpdateDocCount();
            }
            catch (Exception e)
            {
                Debug.LogError($"加载文档失败: {e.Message}\n{e.StackTrace}");
                ShowEmptyState($"加载文档失败: {e.Message}");
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
            
            // 如果是清空搜索，立即执行
            if (string.IsNullOrEmpty(searchText))
            {
                isSearching = false;
                PerformSearch();
            }
        }

        private void PerformSearch()
        {
            if (currentSearchMode == SearchMode.Content && !string.IsNullOrEmpty(currentSearchText))
            {
                searchProgressBar.style.display = DisplayStyle.Flex;
                // 延迟一帧执行以显示进度条
                rootVisualElement.schedule.Execute(() => {
                    EnsureFileCache();
                    FilterDocuments();
                    searchProgressBar.style.display = DisplayStyle.None;
                });
            }
            else
            {
                FilterDocuments();
            }
        }

        private void EnsureFileCache()
        {
            // 简单的按需缓存
            // 遍历所有节点，如果缓存中没有，则读取
            foreach (var node in allDocNodes)
            {
                if (!fileContentCache.ContainsKey(node.path))
                {
                    try
                    {
                        fileContentCache[node.path] = File.ReadAllText(node.path).ToLower();
                    }
                    catch
                    {
                        fileContentCache[node.path] = "";
                    }
                }
            }
        }
        
        private void FilterDocuments()
        {
            if (string.IsNullOrEmpty(currentSearchText))
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
                var filteredNode = FilterNode(node);
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
            UpdateDocCount();
        }
        
        private DocNode FilterNode(DocNode node)
        {
            if (node.isDirectory)
            {
                // 对于目录，检查其子节点
                var filteredChildren = new List<DocNode>();
                if (node.children != null)
                {
                    foreach (var child in node.children)
                    {
                        var filteredChild = FilterNode(child);
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
                // 文件匹配逻辑
                bool isMatch = false;
                
                if (currentSearchMode == SearchMode.FileName)
                {
                    if (node.name.ToLower().Contains(currentSearchText))
                    {
                        isMatch = true;
                    }
                }
                else // SearchMode.Content
                {
                    // 先检查文件名，如果匹配也算
                    if (node.name.ToLower().Contains(currentSearchText))
                    {
                        isMatch = true;
                    }
                    else
                    {
                        // 检查内容
                        if (fileContentCache.TryGetValue(node.path, out string content))
                        {
                            if (content.Contains(currentSearchText))
                            {
                                isMatch = true;
                            }
                        }
                        else
                        {
                            // 如果缓存未命中（不应该发生，如果调用了EnsureFileCache），尝试直接读取
                            try
                            {
                                string text = File.ReadAllText(node.path).ToLower();
                                fileContentCache[node.path] = text;
                                if (text.Contains(currentSearchText)) isMatch = true;
                            }
                            catch { }
                        }
                    }
                }

                return isMatch ? node : null;
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
        
        private void LoadMarkdownFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    ShowEmptyState("文件不存在");
                    return;
                }
                
                currentDocPath = filePath;
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
            contentScrollView.Clear();
            currentHeaders.Clear();
            CleanupTextures(); // 清理旧图片的纹理
            
            var contentPanel = new VisualElement();
            contentPanel.AddToClassList("markdown-content");
            
            // 标题
            var titleElement = new Label(title);
            titleElement.AddToClassList("markdown-title");
            contentPanel.Add(titleElement);
            
            // 解析并渲染Markdown
            var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            bool inCodeBlock = false;
            string codeBlockContent = "";
            string codeBlockLanguage = "";
            bool inList = false;
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                
                // 图片处理 - 简单匹配 ![](url) 格式
                var imgMatch = Regex.Match(line, @"!\[(.*?)\]\((.*?)\)");
                if (imgMatch.Success)
                {
                    string altText = imgMatch.Groups[1].Value;
                    string imgPath = imgMatch.Groups[2].Value;
                    CreateImage(imgPath, altText, contentPanel);
                    continue;
                }

                // 分割线处理
                if (Regex.IsMatch(line, @"^(\*{3,}|-{3,}|_{3,})$"))
                {
                    var separator = new VisualElement();
                    separator.AddToClassList("markdown-separator");
                    contentPanel.Add(separator);
                    continue;
                }

                // 代码块处理
                if (line.TrimStart().StartsWith("```"))
                {
                    if (!inCodeBlock)
                    {
                        inCodeBlock = true;
                        codeBlockLanguage = line.TrimStart().Substring(3).Trim();
                        codeBlockContent = "";
                    }
                    else
                    {
                        inCodeBlock = false;
                        var codeBlock = CreateCodeBlock(codeBlockContent, codeBlockLanguage);
                        contentPanel.Add(codeBlock);
                        codeBlockContent = "";
                        codeBlockLanguage = "";
                    }
                    continue;
                }
                
                if (inCodeBlock)
                {
                    codeBlockContent += line + "\n";
                    continue;
                }
                
                // 引用块处理
                if (line.TrimStart().StartsWith(">"))
                {
                    inList = false;
                    var quote = CreateBlockquote(line);
                    contentPanel.Add(quote);
                    continue;
                }

                // 标题处理
                if (line.StartsWith("#"))
                {
                    inList = false;
                    var header = CreateHeader(line, contentPanel);
                    contentPanel.Add(header);
                    continue;
                }
                
                // 列表处理
                if (Regex.IsMatch(line, @"^\s*[-*+]\s+") || Regex.IsMatch(line, @"^\s*\d+\.\s+"))
                {
                    if (!inList)
                    {
                        inList = true;
                    }
                    var listItem = CreateListItem(line);
                    contentPanel.Add(listItem);
                    continue;
                }
                else
                {
                    inList = false;
                }
                
                // 空行
                if (string.IsNullOrWhiteSpace(line))
                {
                    var spacer = new VisualElement();
                    spacer.AddToClassList("markdown-spacer");
                    contentPanel.Add(spacer);
                    continue;
                }
                
                // 普通段落
                var paragraph = CreateParagraph(line);
                contentPanel.Add(paragraph);
            }
            
            contentScrollView.Add(contentPanel);
            
            // 使用 USS 过渡动画
            contentPanel.schedule.Execute(() => {
                contentPanel.AddToClassList("markdown-content-visible");
            }).StartingIn(50);

            // 等待布局完成后更新导航
            contentPanel.RegisterCallback<GeometryChangedEvent>(OnContentLayoutUpdated);
        }

        private void OnContentLayoutUpdated(GeometryChangedEvent evt)
        {
            var contentPanel = evt.target as VisualElement;
            contentPanel.UnregisterCallback<GeometryChangedEvent>(OnContentLayoutUpdated);
            UpdateNavigation();
        }

        private void CreateImage(string path, string altText, VisualElement parent)
        {
            try
            {
                // 处理相对路径
                string fullPath = path;
                if (!Path.IsPathRooted(path))
                {
                    // 假设图片相对于当前文档
                    string docDir = Path.GetDirectoryName(currentDocPath);
                    fullPath = Path.Combine(docDir, path);
                }

                // 尝试加载图片
                Texture2D texture = null;
                if (fullPath.StartsWith("http"))
                {
                    // 网络图片暂不支持直接加载显示，或者可以显示一个占位符/链接
                    var linkLabel = new Label($"[图片: {altText}] ({path})");
                    linkLabel.AddToClassList("markdown-paragraph"); // 使用普通段落样式
                    parent.Add(linkLabel);
                    return;
                }
                else
                {
                    // 本地图片
                    if (File.Exists(fullPath))
                    {
                        byte[] fileData = File.ReadAllBytes(fullPath);
                        texture = new Texture2D(2, 2);
                        texture.LoadImage(fileData);
                        loadedTextures.Add(texture); // 记录以便销毁
                    }
                }

                if (texture != null)
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
            }
            catch (Exception e)
            {
                Debug.LogWarning($"加载图片失败: {path}, {e.Message}");
            }
        }
        
        private VisualElement CreateHeader(string line, VisualElement parent)
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
        
        private VisualElement CreateParagraph(string line)
        {
            var label = new Label(ProcessInlineMarkdown(line));
            label.AddToClassList("markdown-paragraph");
            label.enableRichText = true; // 启用富文本
            return label;
        }
        
        private VisualElement CreateListItem(string line)
        {
            // 移除列表标记（无序列表的 -、*、+ 或有序列表的数字.）
            string text = Regex.Replace(line, @"^\s*([-*+]|\d+\.)\s+", "");
            var label = new Label("• " + ProcessInlineMarkdown(text));
            label.AddToClassList("markdown-list-item");
            label.enableRichText = true; // 启用富文本
            return label;
        }

        private VisualElement CreateBlockquote(string line)
        {
            string text = line.TrimStart().Substring(1).Trim();
            var label = new Label(ProcessInlineMarkdown(text));
            label.AddToClassList("markdown-blockquote");
            label.enableRichText = true; // 启用富文本
            return label;
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
            
            // 行内代码 `code` -> <color=#...>code</color>
            text = Regex.Replace(text, @"`([^`]+)`", "<color=#DCDCAA>$1</color>");
            
            // 粗体 **text** -> <b>text</b>
            text = Regex.Replace(text, @"\*\*([^*]+)\*\*", "<b>$1</b>");
            text = Regex.Replace(text, @"__([^_]+)__", "<b>$1</b>");
            
            // 斜体 *text* -> <i>text</i>
            text = Regex.Replace(text, @"\*([^*]+)\*", "<i>$1</i>");
            text = Regex.Replace(text, @"_([^_]+)_", "<i>$1</i>");
            
            // 链接 [text](url) -> <color=#...>text</color> (Unity Label链接支持有限，主要做颜色区分)
            text = Regex.Replace(text, @"\[([^\]]+)\]\([^\)]+\)", "<color=#4EC9B0>$1</color>");
            
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
    }
}
#endif
