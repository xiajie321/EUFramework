#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EUFramework.Extension.ExtensionManagerKit.Editor
{

    public class EUExtensionManagerWindow : EditorWindow
    {
        private List<EUExtensionInfo> m_Extensions = new List<EUExtensionInfo>();
        private List<EUExtensionInfo> m_LocalExtensions = new List<EUExtensionInfo>();
        private List<EUExtensionInfo> m_RemoteExtensions = new List<EUExtensionInfo>();
        private List<EUExtensionInfo> m_FilteredExtensions = new List<EUExtensionInfo>();
        
        private ListView m_ExtensionListView;
        private VisualElement m_DetailPanel;
        private TextField m_SearchField;
        
        // Sorting & Filtering
        private PopupField<string> m_SortPopup;
        private PopupField<string> m_CategoryPopup;
        private List<string> m_Categories = new List<string> { "全部" };
        private string m_CurrentCategory = "全部";
        private string m_CurrentSort = "名称";

        private bool m_ShowRemote = false;
        private string m_LastSelectedName;
        private bool m_IsProcessing = false;
        private bool m_IsLoadingRemote = false;
        private int m_UpdateAvailableCount = 0;

        // UI Elements
        private Button m_BtnInstalled;
        private Button m_BtnCommunity;
        private Button m_BtnSettings;
        private Label m_InstalledCountLabel;
        private Label m_CommunityCountLabel;
        private Button m_RefreshBtn;
        private Label m_StatusLabel;

        [MenuItem("EUFramework/拓展管理器")]
        public static void ShowWindow()
        {
            EUExtensionManagerWindow wnd = GetWindow<EUExtensionManagerWindow>();
            wnd.titleContent = new GUIContent("EU Manager");
            wnd.minSize = new Vector2(900, 600);
        }

        private void OnEnable()
        {
            // CreateGUI 中会调用 RefreshList，此处不需要重复调用
            // 因为 OnEnable 在 CreateGUI 之前调用，此时 UI 还未创建
        }

        public void CreateGUI()
        {
            var styleSheet = LoadStyleSheet();
            VisualElement root = rootVisualElement;
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);
            else
                Debug.LogError("[EUExtensionManager] 无法找到样式文件 EUExtensionManager.uss，请确保文件存在于项目中。");
            
            root.AddToClassList("root-container");

            // 1. 左侧侧边栏
            VisualElement sidebar = new VisualElement();
            sidebar.AddToClassList("sidebar");
            root.Add(sidebar);

            // Sidebar Header
            Label logoLabel = new Label("EU\nManager");
            logoLabel.AddToClassList("sidebar-logo");
            sidebar.Add(logoLabel);

            // Sidebar Menu with count badges
            var installedContainer = new VisualElement();
            installedContainer.style.flexDirection = FlexDirection.Row;
            installedContainer.style.alignItems = Align.Center;
            m_BtnInstalled = CreateSidebarButton("已安装", "icon-installed", () => SwitchTab(false));
            m_BtnInstalled.style.flexGrow = 1;
            installedContainer.Add(m_BtnInstalled);
            m_InstalledCountLabel = new Label("0");
            m_InstalledCountLabel.AddToClassList("sidebar-count");
            installedContainer.Add(m_InstalledCountLabel);
            sidebar.Add(installedContainer);

            var communityContainer = new VisualElement();
            communityContainer.style.flexDirection = FlexDirection.Row;
            communityContainer.style.alignItems = Align.Center;
            m_BtnCommunity = CreateSidebarButton("社区仓库", "icon-community", () => SwitchTab(true));
            m_BtnCommunity.style.flexGrow = 1;
            communityContainer.Add(m_BtnCommunity);
            m_CommunityCountLabel = new Label("-");
            m_CommunityCountLabel.AddToClassList("sidebar-count");
            communityContainer.Add(m_CommunityCountLabel);
            sidebar.Add(communityContainer);

            VisualElement spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            sidebar.Add(spacer);

            m_BtnSettings = CreateSidebarButton("设置", "icon-settings", ShowSettings);
            sidebar.Add(m_BtnSettings);

            // 2. 右侧主内容区
            VisualElement mainContent = new VisualElement();
            mainContent.AddToClassList("main-content");
            root.Add(mainContent);

            // Top Bar
            VisualElement topBar = new VisualElement();
            topBar.AddToClassList("top-bar");
            mainContent.Add(topBar);

            Label sectionTitle = new Label("拓展列表");
            sectionTitle.AddToClassList("section-title");
            sectionTitle.name = "section-title";
            topBar.Add(sectionTitle);

            VisualElement searchContainer = new VisualElement();
            searchContainer.AddToClassList("search-container");
            topBar.Add(searchContainer);

            m_SearchField = new TextField();
            m_SearchField.AddToClassList("search-field");
            m_SearchField.RegisterValueChangedCallback(evt => FilterList());
            searchContainer.Add(m_SearchField);

            Label placeholder = new Label("搜索拓展...");
            placeholder.AddToClassList("search-placeholder");
            placeholder.pickingMode = PickingMode.Ignore;
            m_SearchField.Add(placeholder);

            Button clearBtn = new Button(() => m_SearchField.value = "") { text = "×" };
            clearBtn.AddToClassList("search-clear-button");
            m_SearchField.Add(clearBtn);

            // Search Field Logic
            m_SearchField.RegisterCallback<FocusInEvent>(evt => placeholder.style.display = DisplayStyle.None);
            m_SearchField.RegisterCallback<FocusOutEvent>(evt => {
                if (string.IsNullOrEmpty(m_SearchField.value)) placeholder.style.display = DisplayStyle.Flex;
            });
            m_SearchField.RegisterValueChangedCallback(evt => {
                placeholder.style.display = string.IsNullOrEmpty(evt.newValue) ? DisplayStyle.Flex : DisplayStyle.None;
                clearBtn.style.display = string.IsNullOrEmpty(evt.newValue) ? DisplayStyle.None : DisplayStyle.Flex;
            });
            clearBtn.style.display = DisplayStyle.None;

            // Sort & Filter Controls
            VisualElement filterContainer = new VisualElement();
            filterContainer.style.flexDirection = FlexDirection.Row;
            filterContainer.style.marginLeft = 10;
            topBar.Add(filterContainer);

            // Category Filter
            m_CategoryPopup = new PopupField<string>(m_Categories, 0);
            m_CategoryPopup.RegisterValueChangedCallback(evt => {
                m_CurrentCategory = evt.newValue;
                FilterList();
            });
            m_CategoryPopup.style.width = 100;
            m_CategoryPopup.style.marginRight = 5;
            filterContainer.Add(m_CategoryPopup);

            // Sort
            var sortOptions = new List<string> { "名称", "状态" };
            m_SortPopup = new PopupField<string>(sortOptions, 0);
            m_SortPopup.RegisterValueChangedCallback(evt => {
                m_CurrentSort = evt.newValue;
                FilterList();
            });
            m_SortPopup.style.width = 80;
            filterContainer.Add(m_SortPopup);

            Button refreshBtn = new Button(() => RefreshList()) { text = "↻" };
            refreshBtn.tooltip = "刷新列表";
            refreshBtn.AddToClassList("icon-button");
            refreshBtn.style.marginLeft = 10;
            topBar.Add(refreshBtn);

            // Split View (List + Detail)
            VisualElement splitView = new VisualElement();
            splitView.AddToClassList("split-view");
            mainContent.Add(splitView);

            // List View
            m_ExtensionListView = new ListView();
            m_ExtensionListView.AddToClassList("extension-list");
            m_ExtensionListView.fixedItemHeight = 72; // 更高的列表项
            m_ExtensionListView.makeItem = MakeListItem;
            m_ExtensionListView.bindItem = BindListItem;
            m_ExtensionListView.selectionChanged += OnSelectionChanged;
            splitView.Add(m_ExtensionListView);

            // Detail View
            m_DetailPanel = new ScrollView();
            m_DetailPanel.AddToClassList("detail-panel");
            VisualElement detailContent = new VisualElement();
            detailContent.name = "detail-content";
            detailContent.AddToClassList("detail-content");
            m_DetailPanel.Add(detailContent);
            splitView.Add(m_DetailPanel);

            // Init - 设置默认标签页为"已安装"并立即刷新数据
            m_ShowRemote = false;
            m_BtnInstalled.AddToClassList("sidebar-button--active");
            var initTitle = rootVisualElement.Q<Label>("section-title");
            if (initTitle != null) initTitle.text = "已安装拓展";
            
            // 立即加载并显示本地扩展
            RefreshList();
        }

        private Button CreateSidebarButton(string text, string iconClass, Action onClick)
        {
            Button btn = new Button(onClick);
            btn.AddToClassList("sidebar-button");
            // 这里可以用 VisualElement 做图标，简化起见用文字首字或特殊字符
            // 实际项目中建议用 BackgroundImage
            VisualElement icon = new VisualElement();
            icon.AddToClassList("sidebar-icon");
            icon.AddToClassList(iconClass);
            btn.Add(icon);
            
            Label label = new Label(text);
            label.AddToClassList("sidebar-label");
            btn.Add(label);
            
            return btn;
        }

        private VisualElement MakeListItem()
        {
            var item = new VisualElement();
            item.AddToClassList("list-item");

            // Icon Placeholder
            var iconBox = new VisualElement();
            iconBox.AddToClassList("item-icon");
            var iconLabel = new Label();
            iconLabel.name = "icon-char";
            iconBox.Add(iconLabel);
            item.Add(iconBox);

            // Info Container
            var infoBox = new VisualElement();
            infoBox.style.flexGrow = 1;
            infoBox.style.justifyContent = Justify.Center;
            infoBox.style.marginLeft = 10;
            item.Add(infoBox);

            var row1 = new VisualElement();
            row1.style.flexDirection = FlexDirection.Row;
            row1.style.alignItems = Align.Center;
            infoBox.Add(row1);

            var nameLabel = new Label();
            nameLabel.AddToClassList("item-name");
            row1.Add(nameLabel);

            var versionLabel = new Label();
            versionLabel.AddToClassList("item-version");
            row1.Add(versionLabel);

            var descLabel = new Label();
            descLabel.AddToClassList("item-desc");
            infoBox.Add(descLabel);

            // Status Badge
            var badge = new Label();
            badge.AddToClassList("status-badge");
            badge.name = "status-badge";
            item.Add(badge);

            return item;
        }

        private void BindListItem(VisualElement element, int index)
        {
            if (index < 0 || index >= m_FilteredExtensions.Count) return;
            var info = m_FilteredExtensions[index];

            // Icon
            var iconLabel = element.Q<Label>("icon-char");
            string firstChar = !string.IsNullOrEmpty(info.displayName) ? info.displayName.Substring(0, 1).ToUpper() : "?";
            iconLabel.text = firstChar;
            
            // Color based on first char to make it look lively
            int colorIndex = (firstChar[0] % 6);
            string[] colors = new[] { "#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", "#FFEEAD", "#D4A5A5" };
            element.Q(className: "item-icon").style.backgroundColor = new StyleColor(ColorUtility.TryParseHtmlString(colors[colorIndex], out Color c) ? c : Color.gray);

            // Text
            element.Q<Label>(className: "item-name").text = info.displayName;
            element.Q<Label>(className: "item-desc").text = info.description;

            // Status & Version
            var badge = element.Q<Label>("status-badge");
            badge.style.display = DisplayStyle.None;
            badge.ClearClassList();
            badge.AddToClassList("status-badge");

            var versionLabel = element.Q<Label>(className: "item-version");
            versionLabel.text = info.version;
            versionLabel.style.color = new Color(0.6f, 0.6f, 0.6f);

            // 检查是否已安装（即使在社区仓库标签页中也要检查）
            var localInfo = FindLocalExtension(info);
            bool isInstalled = localInfo != null;

            if (isInstalled)
            {
                var remote = m_RemoteExtensions?.FirstOrDefault(r => r.name == info.name || r.name == localInfo.name);
                if (remote != null && IsVersionNewer(remote.version, localInfo.version))
                {
                    badge.text = "Update";
                    badge.AddToClassList("badge-update");
                    badge.style.display = DisplayStyle.Flex;
                    versionLabel.text = $"{localInfo.version} → {remote.version}";
                    versionLabel.style.color = new Color(0.2f, 0.6f, 1f);
                }
                else
                {
                    // 在社区仓库标签页中，显示已安装状态
                    if (m_ShowRemote)
                    {
                        badge.text = "Installed";
                        badge.AddToClassList("badge-installed");
                        badge.style.display = DisplayStyle.Flex;
                    }
                }
            }
        }

        /// <summary>
        /// 在本地扩展列表中查找匹配的扩展（支持按name和文件夹名匹配）
        /// </summary>
        private EUExtensionInfo FindLocalExtension(EUExtensionInfo targetInfo)
        {
            if (targetInfo == null) return null;
            
            // 首先按name精确匹配
            var local = m_LocalExtensions.FirstOrDefault(e => 
                !string.IsNullOrEmpty(e.name) && e.name == targetInfo.name);
            
            // 如果按name找不到，且有remoteFolderName，尝试按文件夹名匹配
            if (local == null && !string.IsNullOrEmpty(targetInfo.remoteFolderName))
            {
                local = m_LocalExtensions.FirstOrDefault(e => 
                    !string.IsNullOrEmpty(e.folderPath) && 
                    Path.GetFileName(e.folderPath) == targetInfo.remoteFolderName);
            }
            
            // 如果targetInfo本身是已安装的，直接返回
            if (local == null && targetInfo.isInstalled)
            {
                local = targetInfo;
            }
            
            return local;
        }

        private void SwitchTab(bool showRemote, bool forceRefresh = false)
        {
            if (!forceRefresh && m_ShowRemote == showRemote) return;
            
            m_ShowRemote = showRemote;
            m_ExtensionListView?.ClearSelection(); // 清除选中状态，避免事件冲突
            
            // Update Sidebar State
            m_BtnInstalled?.RemoveFromClassList("sidebar-button--active");
            m_BtnCommunity?.RemoveFromClassList("sidebar-button--active");
            
            if (showRemote) m_BtnCommunity?.AddToClassList("sidebar-button--active");
            else m_BtnInstalled?.AddToClassList("sidebar-button--active");

            // Update Title
            var title = rootVisualElement?.Q<Label>("section-title");
            if (title != null) title.text = showRemote ? "社区仓库" : "已安装拓展";

            UpdateListView();
        }

        private void ShowSettings()
        {
            EUExtensionSettingsWindow.ShowSettings(RefreshList);
        }

        private void RefreshList()
        {
            if (m_DetailPanel == null) return;

            // 立即加载本地扩展列表（同步操作，速度很快）
            m_LocalExtensions = EUExtensionLoader.GetAllLocalExtensions();
            
            // 无论在哪个标签页，都立即更新列表视图
            // 这样可以确保安装状态徽章能够正确显示
            UpdateListView();
            
            // 如果在社区仓库标签页，还需要刷新详情面板以显示正确的安装状态
            if (m_ShowRemote && m_ExtensionListView.selectedIndex >= 0)
            {
                // 保存当前选中，重新触发选择以刷新详情
                int selectedIndex = m_ExtensionListView.selectedIndex;
                m_ExtensionListView.ClearSelection();
                rootVisualElement.schedule.Execute(() => 
                {
                    if (selectedIndex >= 0 && selectedIndex < m_FilteredExtensions.Count)
                    {
                        m_ExtensionListView.SetSelection(selectedIndex);
                    }
                }).ExecuteLater(50);
            }
            
            // 异步加载远程扩展列表（后台进行，不阻塞UI）
            m_IsLoadingRemote = true;
            UpdateSidebarCounts();
            
            EUExtensionLoader.FetchRemoteRegistry(extensions =>
            {
                m_RemoteExtensions = extensions;
                m_IsLoadingRemote = false;
                
                // 远程数据加载完成后更新列表
                UpdateListView();
                
                // 如果在社区仓库标签页且有选中项，重新触发选择以刷新详情面板
                if (m_ShowRemote && !string.IsNullOrEmpty(m_LastSelectedName))
                {
                    int idx = m_FilteredExtensions.FindIndex(e => e.name == m_LastSelectedName);
                    if (idx >= 0)
                    {
                        m_ExtensionListView.SetSelection(idx);
                    }
                }
            });
        }

        private void UpdateListView()
        {
            m_Extensions = m_ShowRemote ? m_RemoteExtensions : m_LocalExtensions;
            
            // Update Categories
            UpdateCategories();

            // 更新侧边栏计数
            UpdateSidebarCounts();
            
            FilterList();
        }

        private void UpdateCategories()
        {
            var categories = new HashSet<string> { "全部" };
            if (m_Extensions != null)
            {
                foreach (var ext in m_Extensions)
                {
                    if (!string.IsNullOrEmpty(ext.category))
                        categories.Add(ext.category);
                    else
                        categories.Add("未分类");
                }
            }
            
            m_Categories = categories.OrderBy(c => c).ToList();
            // Put 'All' first
            m_Categories.Remove("全部");
            m_Categories.Insert(0, "全部");

            if (m_CategoryPopup != null)
            {
                string current = m_CategoryPopup.value;
                m_CategoryPopup.choices = m_Categories;
                if (m_Categories.Contains(current))
                    m_CategoryPopup.value = current;
                else
                    m_CategoryPopup.value = "全部";
            }
        }
        
        private void UpdateSidebarCounts()
        {
            // 更新已安装数量
            if (m_InstalledCountLabel != null)
            {
                int installedCount = m_LocalExtensions?.Count ?? 0;
                m_InstalledCountLabel.text = installedCount.ToString();
            }
            
            // 更新社区仓库数量
            if (m_CommunityCountLabel != null)
            {
                if (m_RemoteExtensions == null || m_RemoteExtensions.Count == 0)
                {
                    m_CommunityCountLabel.text = m_IsLoadingRemote ? "..." : "-";
                }
                else
                {
                    m_CommunityCountLabel.text = m_RemoteExtensions.Count.ToString();
                }
            }
            
            // 计算可更新数量
            m_UpdateAvailableCount = 0;
            if (m_LocalExtensions != null && m_RemoteExtensions != null)
            {
                foreach (var local in m_LocalExtensions)
                {
                    var remote = m_RemoteExtensions.FirstOrDefault(r => r.name == local.name);
                    if (remote != null && IsVersionNewer(remote.version, local.version))
                    {
                        m_UpdateAvailableCount++;
                    }
                }
            }
        }

        private void FilterList(string search = null)
        {
            if (search == null) search = m_SearchField?.value ?? "";
            search = search.ToLower();

            // 1. Filter
            var filtered = m_Extensions.Where(e => 
                // Search Text
                (string.IsNullOrEmpty(search) || 
                 (e.displayName != null && e.displayName.ToLower().Contains(search)) || 
                 (e.name != null && e.name.ToLower().Contains(search)) ||
                 (e.description != null && e.description.ToLower().Contains(search))) &&
                // Category
                (m_CurrentCategory == "全部" || 
                 (string.IsNullOrEmpty(e.category) && m_CurrentCategory == "未分类") ||
                 e.category == m_CurrentCategory)
            );

            // 2. Sort
            if (m_CurrentSort == "名称")
            {
                m_FilteredExtensions = filtered.OrderByDescending(e => e.name == "com.eu.extension-manager") // Manager first
                                              .ThenBy(e => e.displayName).ToList();
            }
            else // 状态
            {
                // Sort by update available (if installed) or just name
                m_FilteredExtensions = filtered.OrderByDescending(e => e.name == "com.eu.extension-manager") // Manager first
                                              .ThenByDescending(e => {
                                                  // Logic to put updates on top
                                                  if (m_ShowRemote) return false; // In remote view, maybe sort by date if available?
                                                  // In local view, check if update available
                                                  var remote = m_RemoteExtensions?.FirstOrDefault(r => r.name == e.name);
                                                  return remote != null && IsVersionNewer(remote.version, e.version);
                                              }).ThenBy(e => e.displayName).ToList();
            }

            m_ExtensionListView.itemsSource = m_FilteredExtensions;
            m_ExtensionListView.Rebuild();
            
            // 延迟执行选中逻辑，确保 ListView 已完成重建
            rootVisualElement.schedule.Execute(() => 
            {
                int selectIndex = -1;
                if (!string.IsNullOrEmpty(m_LastSelectedName))
                    selectIndex = m_FilteredExtensions.FindIndex(e => e.name == m_LastSelectedName);

                if (selectIndex >= 0)
                {
                    m_ExtensionListView.SetSelection(selectIndex);
                    m_ExtensionListView.ScrollToItem(selectIndex);
                }
                else if (m_FilteredExtensions.Count > 0)
                {
                    m_ExtensionListView.SetSelection(0);
                }
                else
                {
                    ShowEmptyDetails();
                }
            });
        }

        private void OnSelectionChanged(IEnumerable<object> selectedItems)
        {
            // 使用 schedule 确保在布局更新后执行
            rootVisualElement.schedule.Execute(() => 
            {
                var content = m_DetailPanel.Q("detail-content");
                if (content == null) return;
                
                // 触发淡出动画（移除 visible 类）
                content.RemoveFromClassList("detail-content--visible");

                // 延迟一小段时间后更新内容并触发淡入
                rootVisualElement.schedule.Execute(() =>
                {
                    content.Clear();
                    
                    var selectedInfo = selectedItems?.FirstOrDefault() as EUExtensionInfo;
                    if (selectedInfo != null)
                    {
                        m_LastSelectedName = selectedInfo.name;
                        
                        // 使用 FindLocalExtension 来正确查找本地安装的扩展
                        var localInfo = FindLocalExtension(selectedInfo);
                        if (localInfo == null) localInfo = selectedInfo;

                        // 查找远程信息（支持name和remoteFolderName匹配）
                        var remoteInfo = m_RemoteExtensions?.FirstOrDefault(r => r.name == selectedInfo.name);
                        if (remoteInfo == null && !string.IsNullOrEmpty(selectedInfo.remoteFolderName))
                        {
                            remoteInfo = m_RemoteExtensions?.FirstOrDefault(r => r.remoteFolderName == selectedInfo.remoteFolderName);
                        }
                        if (remoteInfo == null) remoteInfo = selectedInfo;

                        try 
                        { 
                            ShowDetails(localInfo, remoteInfo, content); 
                        } 
                        catch (Exception e) 
                        { 
                            Debug.LogError($"[EUExtensionManager] Detail Error: {e}");
                            content.Add(new Label($"Error: {e.Message}")); 
                        }
                    }
                    else
                    {
                        m_LastSelectedName = null;
                        ShowEmptyDetails();
                    }

                    // 强制布局刷新后添加 visible 类以触发动画
                    content.schedule.Execute(() => content.AddToClassList("detail-content--visible"));
                }).ExecuteLater(50); // 50ms 延迟，给淡出一点时间（虽然这里是直接切换，但延迟有助于视觉上的重置感）
            });
        }

        private void ShowDetails(EUExtensionInfo localInfo, EUExtensionInfo remoteInfo, VisualElement container)
        {
            if (remoteInfo == null) remoteInfo = localInfo;

            // Header Banner
            VisualElement banner = new VisualElement();
            banner.AddToClassList("detail-banner");
            container.Add(banner);

            Label title = new Label(remoteInfo.displayName ?? "Unknown");
            title.AddToClassList("detail-title");
            banner.Add(title);

            Label subtitle = new Label($"{remoteInfo.name} • {remoteInfo.version}");
            subtitle.AddToClassList("detail-subtitle");
            banner.Add(subtitle);

            // Action Bar
            VisualElement actionBar = new VisualElement();
            actionBar.AddToClassList("action-bar");
            container.Add(actionBar);

            bool hasUpdate = localInfo.isInstalled && IsVersionNewer(remoteInfo.version, localInfo.version);

            if (localInfo.isInstalled)
            {
                if (hasUpdate)
                {
                    Button upBtn = CreateActionButton("更新", "btn-primary", () => {
                        if (m_IsProcessing) return;
                        m_IsProcessing = true;
                        string dirName = !string.IsNullOrEmpty(remoteInfo.remoteFolderName) ? remoteInfo.remoteFolderName : remoteInfo.name;
                        EUExtensionLoader.DownloadAndInstall(remoteInfo, dirName, s => { m_IsProcessing = false; if(s) RefreshList(); });
                    });
                    if (m_IsProcessing) upBtn.SetEnabled(false);
                    actionBar.Add(upBtn);
                }

                actionBar.Add(CreateActionButton("文档", "btn-secondary", () => EUExtensionLoader.OpenDocumentation(localInfo)));
                actionBar.Add(CreateActionButton("定位", "btn-secondary", () => EditorUtility.RevealInFinder(localInfo.folderPath)));
                
                // 管理器自身不允许卸载 (通过包名判断，假设包名为 com.eu.extension-manager)
                // 也可以结合 category 判断，这里使用包名检查更准确
                bool isSelf = localInfo.name == "com.eu.extension-manager";
                
                if (!isSelf)
                {
                    Button unBtn = CreateActionButton("卸载", "btn-danger", () => {
                        if (m_IsProcessing) return;
                        if (EditorUtility.DisplayDialog("卸载", "确定卸载吗？", "确定", "取消")) { EUExtensionLoader.Uninstall(localInfo); RefreshList(); }
                    });
                    if (m_IsProcessing) unBtn.SetEnabled(false);
                    actionBar.Add(unBtn);
                }
            }
            else
            {
                Button insBtn = CreateActionButton("安装", "btn-primary", () => {
                    if (m_IsProcessing) return;
                    m_IsProcessing = true;
                    string dirName = !string.IsNullOrEmpty(remoteInfo.remoteFolderName) ? remoteInfo.remoteFolderName : remoteInfo.name;
                    EUExtensionLoader.DownloadAndInstall(remoteInfo, dirName, s => { m_IsProcessing = false; if(s) RefreshList(); });
                });
                if (m_IsProcessing) insBtn.SetEnabled(false);
                actionBar.Add(insBtn);
            }

            // Info Grid
            VisualElement grid = new VisualElement();
            grid.AddToClassList("info-grid");
            container.Add(grid);

            AddInfoCard(grid, "版本", (localInfo.isInstalled ? localInfo.version : remoteInfo.version) ?? "-");
            AddInfoCard(grid, "作者", remoteInfo.author ?? "-");
            AddInfoCard(grid, "分类", remoteInfo.category ?? "General");
            
            if (localInfo.isInstalled)
            {
                string shortPath = localInfo.folderPath;
                int idx = shortPath.IndexOf("Assets", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0) shortPath = shortPath.Substring(idx);
                AddInfoCard(grid, "路径", shortPath);
            }

            // Description
            Label descTitle = new Label("简介");
            descTitle.AddToClassList("section-header");
            container.Add(descTitle);

            Label desc = new Label(remoteInfo.description ?? "暂无描述");
            desc.AddToClassList("detail-description");
            container.Add(desc);

            // Dependencies
            if (remoteInfo.dependencies != null && remoteInfo.dependencies.Length > 0)
            {
                Label depTitle = new Label("依赖项");
                depTitle.AddToClassList("section-header");
                container.Add(depTitle);

                VisualElement depList = new VisualElement();
                depList.AddToClassList("dep-list");
                foreach (var dep in remoteInfo.dependencies)
                {
                    VisualElement depItem = new VisualElement();
                    depItem.AddToClassList("dep-item");
                    
                    Label depLabel = new Label(dep.name);
                    depLabel.AddToClassList("dep-tag");
                    
                    // 检查是否已安装：如果是本地扩展包名匹配，或者依赖指定的目录已存在
                    bool isInstalled = m_LocalExtensions.Any(e => e.name == dep.name);
                    // 或者检查指定安装路径
                    
                    if (isInstalled) 
                    {
                        depLabel.AddToClassList("dep-tag--installed");
                        depLabel.tooltip = "已安装";
                    }
                    else
                    {
                        depLabel.AddToClassList("dep-tag--missing");
                        depLabel.tooltip = "点击下载";
                        depLabel.RegisterCallback<ClickEvent>(evt => {
                            if (m_IsProcessing) return;
                            
                            string msg = $"下载依赖 {dep.name}？\n";
                            if (!string.IsNullOrEmpty(dep.gitUrl))
                                msg += $"来源: {dep.gitUrl}\n";
                            if (!string.IsNullOrEmpty(dep.installPath))
                                msg += $"目标: {dep.installPath}";
                            else
                                msg += "目标: 默认扩展目录";

                            if (EditorUtility.DisplayDialog("下载依赖", msg, "下载", "取消"))
                            {
                                m_IsProcessing = true;
                                
                                if (!string.IsNullOrEmpty(dep.gitUrl))
                                {
                                    // 从外部 Git 下载
                                    EUExtensionLoader.DownloadDependency(dep, s => {
                                        m_IsProcessing = false;
                                        if (s) RefreshList();
                                    });
                                }
                                else
                                {
                                    // 尝试从社区仓库查找
                                    var depInfo = m_RemoteExtensions.FirstOrDefault(r => r.name == dep.name);
                                    if (depInfo != null)
                                    {
                                        string dirName = !string.IsNullOrEmpty(depInfo.remoteFolderName) ? depInfo.remoteFolderName : depInfo.name;
                                        EUExtensionLoader.DownloadAndInstall(depInfo, dirName, s => { 
                                            m_IsProcessing = false; 
                                            if(s) RefreshList(); 
                                        });
                                    }
                                    else
                                    {
                                        m_IsProcessing = false;
                                        EditorUtility.DisplayDialog("提示", $"未配置 gitUrl 且在远程仓库中未找到依赖项: {dep.name}", "确定");
                                    }
                                }
                            }
                        });
                    }
                    depList.Add(depLabel);
                }
                container.Add(depList);
            }
        }

        private Button CreateActionButton(string text, string className, Action onClick)
        {
            Button btn = new Button(onClick) { text = text };
            btn.AddToClassList("action-button");
            btn.AddToClassList(className);
            return btn;
        }

        private void AddInfoCard(VisualElement parent, string label, string value)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("info-card");
            Label l = new Label(label);
            l.AddToClassList("info-card-label");
            Label v = new Label(value);
            v.AddToClassList("info-card-value");
            card.Add(l);
            card.Add(v);
            parent.Add(card);
        }

        private bool IsVersionNewer(string remote, string local)
        {
            if (string.IsNullOrEmpty(remote) || string.IsNullOrEmpty(local)) return false;
            try { return new Version(remote) > new Version(local); } catch { return false; }
        }

        private void ShowEmptyDetails()
        {
            var content = m_DetailPanel.Q("detail-content");
            if (content != null) 
            { 
                content.Clear(); 
                var empty = new Label("选择一个拓展以查看详情");
                empty.AddToClassList("empty-state");
                content.Add(empty); 
            }
        }

        private StyleSheet LoadStyleSheet()
        {
            // 1. 尝试通过 GUID 查找（最稳健）
            string[] guids = AssetDatabase.FindAssets("EUExtensionManager t:StyleSheet");
            if (guids.Length > 0)
            {
                // 可能会有多个同名文件，优先匹配路径中包含 ConfigPanel 的
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith("ConfigPanel/EUExtensionManager.uss"))
                    {
                        return AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                    }
                }
                // 如果没有完全匹配的，返回第一个找到的
                return AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            // 2. 回退到默认路径
            string defaultPath = "Assets/EUFramework/Extension/EUExtensionManager/ConfigPanel/EUExtensionManager.uss";
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(defaultPath);
        }
    }

    public class EUExtensionSettingsWindow : EditorWindow
    {
        private Action m_OnClose;
        private TextField m_UrlField;
        private TextField m_PathField;
        private TextField m_CorePathField;
        private Label m_StatusLabel;

        public static void ShowSettings(Action onClose = null)
        {
            var wnd = GetWindow<EUExtensionSettingsWindow>(true, "EU 设置", true);
            wnd.minSize = new Vector2(600, 700);
            wnd.maxSize = new Vector2(600, 700);
            wnd.m_OnClose = onClose;
            wnd.Show();
        }

        public void CreateGUI()
        {
            // 使用相同的方式加载 StyleSheet，确保兼容不同路径
            var styleSheet = LoadStyleSheet();
            VisualElement root = rootVisualElement;
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);
            else
                Debug.LogError("[EUExtensionSettingsWindow] 无法找到样式文件 EUExtensionManager.uss，请确保文件存在于项目中。");

            root.AddToClassList("settings-window");

            // Scrollable Content
            VisualElement content = new VisualElement();
            content.AddToClassList("settings-content");
            root.Add(content);

            // Title
            Label title = new Label("设置");
            title.AddToClassList("settings-title");
            content.Add(title);

            // Group 1: Repository Settings
            VisualElement group1 = new VisualElement();
            group1.AddToClassList("settings-group");
            content.Add(group1);

            Label group1Label = new Label("远程仓库");
            group1Label.AddToClassList("settings-group-label");
            group1.Add(group1Label);

            VisualElement row1 = new VisualElement();
            row1.AddToClassList("settings-row");
            group1.Add(row1);

            Label label1 = new Label("社区仓库地址 (GitHub)");
            label1.AddToClassList("settings-label");
            row1.Add(label1);

            m_UrlField = new TextField();
            m_UrlField.AddToClassList("settings-text-field");
            m_UrlField.value = EUExtensionLoader.CommunityUrl;
            m_UrlField.RegisterValueChangedCallback(evt => {
                EUExtensionLoader.CommunityUrl = evt.newValue;
                UpdateStatus();
            });
            row1.Add(m_UrlField);

            Label help1 = new Label("仅支持 GitHub 公开仓库，用于获取拓展列表。");
            help1.AddToClassList("settings-help-text");
            row1.Add(help1);

            // Group 2: Local Settings
            VisualElement group2 = new VisualElement();
            group2.AddToClassList("settings-group");
            content.Add(group2);

            Label group2Label = new Label("本地配置");
            group2Label.AddToClassList("settings-group-label");
            group2.Add(group2Label);

            // Extension Path
            VisualElement row2 = new VisualElement();
            row2.AddToClassList("settings-row");
            group2.Add(row2);

            Label label2 = new Label("插件安装路径");
            label2.AddToClassList("settings-label");
            row2.Add(label2);

            VisualElement inputRow = new VisualElement();
            inputRow.AddToClassList("settings-input-row");
            row2.Add(inputRow);

            m_PathField = new TextField();
            m_PathField.AddToClassList("settings-text-field");
            m_PathField.isReadOnly = true;
            m_PathField.value = EUExtensionLoader.ExtensionRootPath;
            inputRow.Add(m_PathField);

            Button selectBtn = new Button(() => OnSelectPath(false)) { text = "..." };
            selectBtn.tooltip = "选择文件夹";
            selectBtn.AddToClassList("settings-icon-btn");
            inputRow.Add(selectBtn);

            Label help2 = new Label("建议保持在 Assets 目录下，更改后可能需要重启编辑器。");
            help2.AddToClassList("settings-help-text");
            row2.Add(help2);

            // Core Path
            VisualElement row3 = new VisualElement();
            row3.AddToClassList("settings-row");
            group2.Add(row3);

            Label label3 = new Label("核心安装路径");
            label3.AddToClassList("settings-label");
            row3.Add(label3);

            VisualElement inputRow3 = new VisualElement();
            inputRow3.AddToClassList("settings-input-row");
            row3.Add(inputRow3);

            m_CorePathField = new TextField();
            m_CorePathField.AddToClassList("settings-text-field");
            m_CorePathField.isReadOnly = true;
            m_CorePathField.value = EUExtensionLoader.CoreInstallPath;
            inputRow3.Add(m_CorePathField);

            Button selectCoreBtn = new Button(() => OnSelectPath(true)) { text = "..." };
            selectCoreBtn.tooltip = "选择文件夹";
            selectCoreBtn.AddToClassList("settings-icon-btn");
            inputRow3.Add(selectCoreBtn);

            Label help3 = new Label("核心框架所在路径，将被视为特殊的本地扩展。");
            help3.AddToClassList("settings-help-text");
            row3.Add(help3);

            // Status Panel within Group 2
            VisualElement statusPanel = new VisualElement();
            statusPanel.AddToClassList("settings-status-panel");
            group2.Add(statusPanel);
            
            m_StatusLabel = new Label();
            m_StatusLabel.AddToClassList("settings-status-text");
            statusPanel.Add(m_StatusLabel);
            UpdateStatus();

            // Footer (Fixed at bottom)
            VisualElement footer = new VisualElement();
            footer.AddToClassList("settings-footer");
            root.Add(footer);

            Button resetBtn = new Button(OnReset) { text = "重置默认" };
            resetBtn.AddToClassList("settings-btn");
            resetBtn.AddToClassList("btn-ghost");
            footer.Add(resetBtn);

            Button closeBtn = new Button(() => Close()) { text = "保存并关闭" };
            closeBtn.AddToClassList("settings-btn");
            closeBtn.AddToClassList("btn-action");
            footer.Add(closeBtn);
        }
        
        private void UpdateStatus()
        {
            if (m_StatusLabel == null) return;
            m_StatusLabel.text = $"平台: GitHub\n扩展路径: {EUExtensionLoader.ExtensionRootPath}\n核心路径: {EUExtensionLoader.CoreInstallPath}";
        }

        private void OnSelectPath(bool isCore)
        {
            string currentPath = isCore ? EUExtensionLoader.CoreInstallPath : EUExtensionLoader.ExtensionRootPath;
            string title = isCore ? "选择核心安装路径" : "选择插件安装路径";
            
            string path = EditorUtility.OpenFolderPanel(title, currentPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                string projectPath = Path.GetFullPath(Application.dataPath).Replace("\\", "/");
                path = path.Replace("\\", "/");
                if (path.StartsWith(projectPath))
                {
                    path = "Assets" + path.Substring(projectPath.Length);
                }
                
                if (path != currentPath)
                {
                    if (EditorUtility.DisplayDialog("迁移扩展", 
                        $"检测到路径变更。\n从: {currentPath}\n到: {path}\n\n是否将旧路径下的扩展迁移到新路径？", 
                        "迁移", "不迁移"))
                    {
                        EUExtensionLoader.MigrateExtensions(currentPath, path);
                    }

                    if (isCore)
                    {
                        EUExtensionLoader.CoreInstallPath = path;
                        m_CorePathField.value = path;
                    }
                    else
                    {
                        EUExtensionLoader.ExtensionRootPath = path;
                        m_PathField.value = path;
                    }
                    UpdateStatus();
                }
            }
        }

        private void OnReset()
        {
            if (EditorUtility.DisplayDialog("重置设置", "确定要恢复默认设置吗？", "确定", "取消"))
            {
                EUExtensionLoader.ResetSettings();
                m_UrlField.value = EUExtensionLoader.CommunityUrl;
                m_PathField.value = EUExtensionLoader.ExtensionRootPath;
                m_CorePathField.value = EUExtensionLoader.CoreInstallPath;
                UpdateStatus();
            }
        }

        private void OnDestroy()
        {
            m_OnClose?.Invoke();
        }

        private StyleSheet LoadStyleSheet()
        {
            // 1. 尝试通过 GUID 查找（最稳健）
            string[] guids = AssetDatabase.FindAssets("EUExtensionManager t:StyleSheet");
            if (guids.Length > 0)
            {
                // 可能会有多个同名文件，优先匹配路径中包含 ConfigPanel 的
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith("ConfigPanel/EUExtensionManager.uss"))
                    {
                        return AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                    }
                }
                // 如果没有完全匹配的，返回第一个找到的
                return AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            // 2. 回退到默认路径
            string defaultPath = "Assets/EUFramework/Extension/EUExtensionManager/ConfigPanel/EUExtensionManager.uss";
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(defaultPath);
        }
    }

    public class EditorInputDialog : EditorWindow
    {
        private string m_Description;
        private string m_InputText;
        private Action<string> m_OnOk;
        private bool m_Initialized = false;

        public static void Show(string title, string description, string defaultText, Action<string> onOk)
        {
            var window = GetWindow<EditorInputDialog>(true, title, true);
            window.m_Description = description;
            window.m_InputText = defaultText;
            window.m_OnOk = onOk;
            window.minSize = new Vector2(300, 150);
            window.maxSize = new Vector2(300, 150);
            window.Show();
        }

        void OnGUI()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    m_OnOk?.Invoke(m_InputText);
                    Close();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Escape)
                {
                    Close();
                    e.Use();
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(m_Description, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(10);
            GUI.SetNextControlName("InputField");
            m_InputText = EditorGUILayout.TextField(m_InputText);
            if (!m_Initialized) { EditorGUI.FocusTextInControl("InputField"); m_Initialized = true; }
            EditorGUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("确定")) { m_OnOk?.Invoke(m_InputText); Close(); }
            if (GUILayout.Button("取消")) { Close(); }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
