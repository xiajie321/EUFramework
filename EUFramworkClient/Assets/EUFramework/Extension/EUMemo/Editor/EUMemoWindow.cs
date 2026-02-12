using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EUFramework.Extension.MemoKit.Editor
{
    public class EUMemoWindow : EditorWindow
    {
        private const string DATA_FILE_NAME = "EUMemoData.json";
        private string DataPath => Path.Combine(Application.persistentDataPath, DATA_FILE_NAME);

        private MemoDataList memoData;
        private List<MemoItem> selectedMemos = new List<MemoItem>();
        private MemoItem currentMemo => selectedMemos.Count > 0 ? selectedMemos[selectedMemos.Count - 1] : null;
        private string currentLayerId = "default";
        
        // UI Elements
        private VisualElement sidebarPanel;
        private ScrollView sidebarList;
        private VisualElement canvasContainer;
        private VisualElement nodesLayer;
        private VisualElement connectionsLayer;
        private VisualElement selectionRect;
        private VisualElement inspectorPanel;
        private ScrollView inspectorContent;
        private Label statusLabel;
        private Button inspectorNextBtn;
        private ToolbarSearchField searchField;
        
        // Inspector Fields
        private TextField titleField;
        private TextField contentField;
        private Toggle completedToggle;
        private Toggle pinnedToggle;
        
        // Canvas State
        private Vector2 panOffset = Vector2.zero;
        private float zoomLevel = 1.0f;
        private bool isPanning = false;
        private bool isDraggingNode = false;
        private bool isConnecting = false;
        private bool isSelecting = false;
        
        private Vector2 dragStartPos;
        private Vector2 selectionStartPos;
        private Dictionary<string, Vector2> initialNodePositions = new Dictionary<string, Vector2>();
        
        // Animation State
        private Dictionary<string, float> targetRotations = new Dictionary<string, float>();
        private Dictionary<string, float> currentRotations = new Dictionary<string, float>();
        private Dictionary<string, float> rotationVelocities = new Dictionary<string, float>(); // For SmoothDamp
        
        // Drag Physics State
        private Dictionary<string, Vector2> targetPositions = new Dictionary<string, Vector2>();
        private Dictionary<string, Vector2> currentPositions = new Dictionary<string, Vector2>();
        private Dictionary<string, Vector2> positionVelocities = new Dictionary<string, Vector2>();

        // View Animation State
        private bool isViewAnimating = false;
        private Vector2 targetPanOffset;
        private float targetZoomLevel;
        private Vector2 viewVelocity; // For SmoothDamp
        private float zoomVelocity;   // For SmoothDamp
        
        private string connectingSourceId;
        private Vector2 currentMousePos;
        private string currentSearchTerm = "";

        private bool isDirty = false;
        private double lastEditTime = 0;
        private const double AUTO_SAVE_DELAY = 1.0f;
        
        // Time tracking for smooth animations
        private double lastFrameTime;

        [MenuItem("EUFramework/拓展/EU 备忘录")]
        public static void ShowWindow()
        {
            EUMemoWindow wnd = GetWindow<EUMemoWindow>();
            wnd.titleContent = new GUIContent("EU 备忘录");
            wnd.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            lastFrameTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += OnUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
            if (isDirty) SaveData();
        }

        private void OnUpdate()
        {
            double currentTime = EditorApplication.timeSinceStartup;
            float deltaTime = (float)(currentTime - lastFrameTime);
            lastFrameTime = currentTime;

            // Cap deltaTime to prevent huge jumps if editor pauses
            if (deltaTime > 0.1f) deltaTime = 0.1f;

            if (isDirty && currentTime - lastEditTime > AUTO_SAVE_DELAY)
            {
                SaveData();
                UpdateStatus("已自动保存");
            }
            
            UpdateAnimations(deltaTime);
            UpdateViewAnimation(deltaTime);
        }

        private void UpdateViewAnimation(float deltaTime)
        {
            if (!isViewAnimating) return;

            bool reachedTarget = true;
            float smoothTime = 0.2f;

            // Smoothly interpolate panOffset
            if (Vector2.Distance(panOffset, targetPanOffset) > 0.1f)
            {
                panOffset = Vector2.SmoothDamp(panOffset, targetPanOffset, ref viewVelocity, smoothTime, float.PositiveInfinity, deltaTime);
                reachedTarget = false;
            }
            else
            {
                panOffset = targetPanOffset;
            }

            // Smoothly interpolate zoomLevel
            if (Mathf.Abs(zoomLevel - targetZoomLevel) > 0.001f)
            {
                zoomLevel = Mathf.SmoothDamp(zoomLevel, targetZoomLevel, ref zoomVelocity, smoothTime, float.PositiveInfinity, deltaTime);
                reachedTarget = false;
            }
            else
            {
                zoomLevel = targetZoomLevel;
            }

            UpdateCanvasTransform();
            connectionsLayer.MarkDirtyRepaint();

            if (reachedTarget)
            {
                isViewAnimating = false;
            }
        }

        private void UpdateAnimations(float deltaTime)
        {
            bool needsRepaint = false;
            
            // Update Drag Physics (Position & Rotation)
            var posKeys = new List<string>(targetPositions.Keys);
            foreach (var id in posKeys)
            {
                if (!currentPositions.ContainsKey(id)) currentPositions[id] = targetPositions[id];
                if (!positionVelocities.ContainsKey(id)) positionVelocities[id] = Vector2.zero;

                Vector2 currentPos = currentPositions[id];
                Vector2 velocity = positionVelocities[id];
                Vector2 newPos = currentPos;

                if (isDraggingNode && selectedMemos.Any(m => m.id == id))
                {
                    // Dragging: Smooth follow mouse target
                    Vector2 targetPos = targetPositions[id];
                    // Increased smoothTime to reduce jitter and add "weight"
                    newPos = Vector2.SmoothDamp(currentPos, targetPos, ref velocity, 0.08f, float.PositiveInfinity, deltaTime); 
                }
                else
                {
                    // Inertia: Slide with friction
                    velocity *= Mathf.Pow(0.90f, deltaTime * 60f); // Friction
                    newPos = currentPos + velocity * deltaTime;
                    
                    // Stop if slow
                    if (velocity.magnitude < 0.1f)
                    {
                        velocity = Vector2.zero;
                    }
                }

                positionVelocities[id] = velocity;
                currentPositions[id] = newPos;

                // Update actual memo position
                var memo = memoData.items.FirstOrDefault(m => m.id == id);
                if (memo != null)
                {
                    memo.position = newPos;
                    
                    // Calculate rotation based on velocity
                    float targetRot = -velocity.x * 0.1f; 
                    targetRot = Mathf.Clamp(targetRot, -20f, 20f);
                    targetRotations[id] = targetRot;

                    var nodeVisual = nodesLayer.Children().FirstOrDefault(e => e.userData == memo);
                    if (nodeVisual != null)
                    {
                        UpdateNodeTransform(nodeVisual, memo);
                    }
                    needsRepaint = true;
                }
            }
            
            // Cleanup stopped items
            if (!isDraggingNode)
            {
                var stopped = posKeys.Where(id => positionVelocities[id].magnitude < 0.1f && Mathf.Abs(currentRotations.GetValueOrDefault(id, 0)) < 0.1f).ToList();
                foreach (var id in stopped)
                {
                    targetPositions.Remove(id);
                    positionVelocities.Remove(id);
                    currentPositions.Remove(id);
                    targetRotations.Remove(id);
                    currentRotations.Remove(id);
                    rotationVelocities.Remove(id);
                    
                    var memo = memoData.items.FirstOrDefault(m => m.id == id);
                    if (memo != null)
                    {
                        var nodeVisual = nodesLayer.Children().FirstOrDefault(e => e.userData == memo);
                        if (nodeVisual != null)
                        {
                            nodeVisual.style.rotate = new StyleRotate(new Rotate(0));
                        }
                    }
                }
            }

            // Update rotations (Secondary smoothing for rotation)
            var rotKeys = new List<string>(targetRotations.Keys);
            foreach (var id in rotKeys)
            {
                if (!currentRotations.ContainsKey(id)) currentRotations[id] = 0;
                if (!rotationVelocities.ContainsKey(id)) rotationVelocities[id] = 0;
                
                float current = currentRotations[id];
                float target = targetRotations[id];
                float velocity = rotationVelocities[id];
                
                float newRotation = Mathf.SmoothDamp(current, target, ref velocity, 0.15f, float.PositiveInfinity, deltaTime);
                rotationVelocities[id] = velocity;
                
                if (Mathf.Abs(newRotation - target) > 0.01f || Mathf.Abs(velocity) > 0.01f)
                {
                    currentRotations[id] = newRotation;
                    
                    var memo = memoData.items.FirstOrDefault(m => m.id == id);
                    if (memo != null)
                    {
                        var nodeVisual = nodesLayer.Children().FirstOrDefault(e => e.userData == memo);
                        if (nodeVisual != null)
                        {
                            nodeVisual.style.rotate = new StyleRotate(new Rotate(newRotation));
                        }
                    }
                    needsRepaint = true;
                }
                else
                {
                     currentRotations[id] = newRotation;
                }
            }
            
            if (needsRepaint)
            {
                connectionsLayer.MarkDirtyRepaint();
            }
        }

        public void CreateGUI()
        {
            LoadData();

            // 动态加载 UXML
            var visualTree = LoadAsset<VisualTreeAsset>("EUMemo", "VisualTreeAsset");
            if (visualTree == null)
            {
                Debug.LogError("找不到 EUMemo.uxml，请确保文件存在于项目中。");
                return;
            }
            visualTree.CloneTree(rootVisualElement);

            // 动态加载 USS
            var styleSheet = LoadAsset<StyleSheet>("EUMemo", "StyleSheet");
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            InitializeUI();
            SetupCanvasInteractions();
            
            // 延迟刷新，等待布局完成
            rootVisualElement.schedule.Execute(() => {
                RefreshCanvas();
                RefreshSidebar();
            });
        }

        /// <summary>
        /// 动态查找并加载资源，支持文件移动后仍能找到
        /// </summary>
        private T LoadAsset<T>(string name, string type) where T : UnityEngine.Object
        {
            // 查找所有匹配名称和类型的资源 GUID
            string[] guids = AssetDatabase.FindAssets($"{name} t:{type}");
            if (guids.Length == 0)
                return null;

            // 遍历所有找到的资源
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                // 优先匹配路径中包含 "EUMemo/Editor" 的资源，避免同名文件冲突
                // 同时确保文件名完全匹配（FindAssets 是模糊匹配）
                if (path.Contains("EUMemo/Editor") && Path.GetFileNameWithoutExtension(path) == name)
                {
                    return AssetDatabase.LoadAssetAtPath<T>(path);
                }
            }

            // 如果没有精确匹配，尝试放宽条件，只要文件名匹配即可
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == name)
                {
                    return AssetDatabase.LoadAssetAtPath<T>(path);
                }
            }

            return null;
        }

        private void InitializeUI()
        {
            sidebarPanel = rootVisualElement.Q<VisualElement>(className: "sidebar-panel");
            sidebarList = rootVisualElement.Q<ScrollView>(className: "sidebar-list");
            canvasContainer = rootVisualElement.Q<VisualElement>(className: "canvas-container");
            nodesLayer = rootVisualElement.Q<VisualElement>(className: "nodes-layer");
            connectionsLayer = rootVisualElement.Q<VisualElement>(className: "connections-layer");
            inspectorPanel = rootVisualElement.Q<VisualElement>(className: "inspector-panel");
            inspectorContent = rootVisualElement.Q<ScrollView>(className: "inspector-content");
            statusLabel = rootVisualElement.Q<Label>(className: "status-label");

            // Make canvas focusable to receive key events
            canvasContainer.focusable = true;

            // Selection Rect
            selectionRect = new VisualElement();
            selectionRect.AddToClassList("selection-rect");
            canvasContainer.Add(selectionRect);
            selectionRect.BringToFront();

            // Toolbar Buttons
            rootVisualElement.Q<Button>(className: "toggle-sidebar-button").clicked += ToggleSidebar;
            rootVisualElement.Q<Button>(className: "add-button").clicked += CreateNewMemo;
            rootVisualElement.Q<Button>(className: "save-button").clicked += () => { SaveData(); UpdateStatus("已保存"); };
            rootVisualElement.Q<Button>(className: "center-button").clicked += ResetView;
            
            // Search Field
            searchField = rootVisualElement.Q<ToolbarSearchField>(className: "search-field");
            searchField.RegisterValueChangedCallback(evt => {
                currentSearchTerm = evt.newValue;
                RefreshCanvas();
                RefreshSidebar();
            });
            
            // Inspector
            rootVisualElement.Q<Button>(className: "close-inspector-button").clicked += () => ToggleInspector(false);
            rootVisualElement.Q<Button>(className: "delete-node-button").clicked += DeleteSelectedMemos;
            inspectorNextBtn = rootVisualElement.Q<Button>(className: "inspector-next-btn");
            inspectorNextBtn.clicked += () => {
                if (currentMemo != null && !string.IsNullOrEmpty(currentMemo.nextMemoId))
                {
                    FocusMemo(currentMemo.nextMemoId);
                }
            };

            titleField = rootVisualElement.Q<TextField>(className: "inspector-title-field");
            contentField = rootVisualElement.Q<TextField>(className: "inspector-content-field");
            completedToggle = rootVisualElement.Q<Toggle>(className: "inspector-completed-toggle");
            pinnedToggle = rootVisualElement.Q<Toggle>(className: "inspector-pinned-toggle");

            titleField.RegisterValueChangedCallback(evt => UpdateSelectedMemos(m => m.title = evt.newValue));
            contentField.RegisterValueChangedCallback(evt => UpdateSelectedMemos(m => m.content = evt.newValue));
            completedToggle.RegisterValueChangedCallback(evt => UpdateSelectedMemos(m => m.isCompleted = evt.newValue));
            pinnedToggle.RegisterValueChangedCallback(evt => UpdateSelectedMemos(m => m.isPinned = evt.newValue));

            // Color Buttons
            for (int i = 0; i <= 4; i++)
            {
                var btn = rootVisualElement.Q<Button>($"color-{i}");
                if (btn != null)
                {
                    int index = i;
                    btn.clicked += () => UpdateSelectedMemos(m => m.colorIndex = index);
                }
            }

            // Grid Background
            var grid = rootVisualElement.Q<VisualElement>(className: "grid-background");
            grid.generateVisualContent += DrawGrid;
            
            // Connections Layer
            connectionsLayer.generateVisualContent += DrawConnections;

            ToggleInspector(false);
        }

        private void SetupCanvasInteractions()
        {
            canvasContainer.RegisterCallback<WheelEvent>(OnCanvasWheel);
            canvasContainer.RegisterCallback<MouseDownEvent>(OnCanvasMouseDown);
            canvasContainer.RegisterCallback<MouseUpEvent>(OnCanvasMouseUp);
            canvasContainer.RegisterCallback<MouseMoveEvent>(OnCanvasMouseMove);
            // Key events for delete
            canvasContainer.RegisterCallback<KeyDownEvent>(OnCanvasKeyDown);
        }

        private void DrawGrid(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            painter.lineWidth = 1.0f;
            painter.strokeColor = new Color(1, 1, 1, 0.05f); // Lighter grid for dark bg

            float step = 50 * zoomLevel;
            // 避免网格过密
            while (step < 25) step *= 2;
            
            Vector2 offset = new Vector2(panOffset.x % step, panOffset.y % step);

            // Use layout width/height, fallback to arbitrary large size if not ready
            float width = canvasContainer.layout.width > 0 ? canvasContainer.layout.width : 5000;
            float height = canvasContainer.layout.height > 0 ? canvasContainer.layout.height : 5000;

            for (float x = offset.x; x < width; x += step)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, 0));
                painter.LineTo(new Vector2(x, height));
                painter.Stroke();
            }

            for (float y = offset.y; y < height; y += step)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, y));
                painter.LineTo(new Vector2(width, y));
                painter.Stroke();
            }
        }

        private void DrawConnections(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            painter.lineWidth = 2.0f * zoomLevel;
            painter.strokeColor = new Color(1f, 0.46f, 0.46f, 0.8f); // Light red for dark bg

            // 建立 ID 到 VisualElement 的映射，以便快速查找布局
            var nodeMap = new Dictionary<string, VisualElement>();
            foreach (var child in nodesLayer.Children())
            {
                if (child.userData is MemoItem memo)
                {
                    nodeMap[memo.id] = child;
                }
            }

            foreach (var memo in memoData.items)
            {
                // 如果当前节点被搜索过滤掉了，不绘制它的连线
                if (!IsMatch(memo)) continue;
                // 仅显示当前图层的连线
                if (currentLayerId != "all" && memo.layerId != currentLayerId && !string.IsNullOrEmpty(memo.layerId)) continue;
                if (currentLayerId != "all" && string.IsNullOrEmpty(memo.layerId) && currentLayerId != "default") continue;

                if (!string.IsNullOrEmpty(memo.nextMemoId) && nodeMap.ContainsKey(memo.id) && nodeMap.ContainsKey(memo.nextMemoId))
                {
                    var sourceNode = nodeMap[memo.id];
                    var targetNode = nodeMap[memo.nextMemoId];
                    
                    // 获取局部坐标（相对于 nodesLayer，即未缩放/平移的坐标）
                    Rect sourceRect = sourceNode.layout;
                    Rect targetRect = targetNode.layout;
                    
                    // 如果布局未就绪（例如刚创建），使用 memo.position 和默认大小
                    if (float.IsNaN(sourceRect.width) || sourceRect.width < 1) sourceRect = new Rect(memo.position, new Vector2(220, 100));
                    if (float.IsNaN(targetRect.width) || targetRect.width < 1) targetRect = new Rect(memoData.items.First(m => m.id == memo.nextMemoId).position, new Vector2(220, 100));

                    // 计算连接点（基于 layout）
                    // 默认：右 -> 左
                    Vector2 startPos = sourceRect.position + new Vector2(sourceRect.width, sourceRect.height / 2);
                    Vector2 endPos = targetRect.position + new Vector2(0, targetRect.height / 2);
                    
                    // 如果目标在源的左边（回环），改为 左->右
                    bool isReverse = targetRect.center.x < sourceRect.center.x;
                    if (isReverse)
                    {
                        startPos = sourceRect.position + new Vector2(0, sourceRect.height / 2);
                        endPos = targetRect.position + new Vector2(targetRect.width, targetRect.height / 2);
                    }

                    // 如果正在搜索，且源节点不匹配，可以让连线半透明
                    bool isDimmed = !string.IsNullOrEmpty(currentSearchTerm) && !IsMatch(memo);
                    DrawConnectionLine(painter, startPos, endPos, isReverse, isDimmed);
                }
            }

            // Draw active connection line
            if (isConnecting && !string.IsNullOrEmpty(connectingSourceId))
            {
                var source = memoData.items.FirstOrDefault(m => m.id == connectingSourceId);
                if (source != null && nodeMap.ContainsKey(source.id))
                {
                    var sourceNode = nodeMap[source.id];
                    Rect sourceRect = sourceNode.layout;
                    if (float.IsNaN(sourceRect.width)) sourceRect = new Rect(source.position, new Vector2(220, 100));
                    
                    Vector2 startPos = sourceRect.position + new Vector2(sourceRect.width, sourceRect.height / 2);
                    
                    // Convert mouse pos to local canvas space for drawing
                    Vector2 targetPos = (currentMousePos - panOffset) / zoomLevel;
                    
                    bool isReverse = targetPos.x < sourceRect.center.x;
                    if (isReverse)
                    {
                        startPos = sourceRect.position + new Vector2(0, sourceRect.height / 2);
                    }
                    
                    DrawConnectionLine(painter, startPos, targetPos, isReverse, false);
                }
            }
        }

        private void DrawConnectionLine(Painter2D painter, Vector2 startNodeLayerPos, Vector2 endNodeLayerPos, bool isReverse, bool isDimmed)
        {
            Vector2 startScreen = startNodeLayerPos * zoomLevel + panOffset;
            Vector2 endScreen = endNodeLayerPos * zoomLevel + panOffset;

            painter.BeginPath();
            painter.MoveTo(startScreen);
            
            // 贝塞尔曲线控制点
            float dist = Vector2.Distance(startScreen, endScreen);
            float tangentLen = Mathf.Min(dist * 0.5f, 150 * zoomLevel);
            if (tangentLen < 50 * zoomLevel) tangentLen = 50 * zoomLevel;

            Vector2 startTan = startScreen + new Vector2(tangentLen, 0);
            Vector2 endTan = endScreen - new Vector2(tangentLen, 0);

            if (isReverse)
            {
                startTan = startScreen - new Vector2(tangentLen, 0);
                endTan = endScreen + new Vector2(tangentLen, 0);
            }

            painter.BezierCurveTo(startTan, endTan, endScreen);
            
            // 设置颜色
            Color strokeColor = new Color(1f, 0.46f, 0.46f, 0.8f);
            if (isDimmed) strokeColor.a = 0.2f;
            painter.strokeColor = strokeColor;
            
            painter.Stroke();

            // Draw arrow
            Vector2 dir = (endScreen - endTan).normalized;
            if (dir == Vector2.zero) dir = Vector2.right;
            DrawArrow(painter, endScreen, dir, strokeColor);
        }

        private void DrawArrow(Painter2D painter, Vector2 position, Vector2 direction, Color color)
        {
            float size = 10 * zoomLevel;
            Vector2 right = new Vector2(-direction.y, direction.x);
            
            Vector2 p1 = position;
            Vector2 p2 = position - direction * size + right * (size * 0.5f);
            Vector2 p3 = position - direction * size - right * (size * 0.5f);

            painter.BeginPath();
            painter.MoveTo(p1);
            painter.LineTo(p2);
            painter.LineTo(p3);
            painter.ClosePath();
            painter.fillColor = color;
            painter.Fill();
        }

        private bool IsMatch(MemoItem memo)
        {
            if (string.IsNullOrEmpty(currentSearchTerm)) return true;
            
            bool titleMatch = !string.IsNullOrEmpty(memo.title) && memo.title.IndexOf(currentSearchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
            bool contentMatch = !string.IsNullOrEmpty(memo.content) && memo.content.IndexOf(currentSearchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
            
            return titleMatch || contentMatch;
        }

        private void RefreshCanvas()
        {
            nodesLayer.Clear();
            foreach (var memo in memoData.items)
            {
                // Filter by layer
                if (currentLayerId != "all" && memo.layerId != currentLayerId && !string.IsNullOrEmpty(memo.layerId)) continue;
                if (currentLayerId != "all" && string.IsNullOrEmpty(memo.layerId) && currentLayerId != "default") continue;

                // 始终创建所有节点，不进行过滤
                CreateNodeVisual(memo);
            }
            UpdateCanvasTransform();
            // 延迟一帧重绘连线，确保布局已更新
            rootVisualElement.schedule.Execute(() => connectionsLayer.MarkDirtyRepaint());
        }

        private void RefreshSidebar()
        {
            sidebarList.Clear();
            
            // Add Layer Header
            var layerHeader = new Label("图层");
            layerHeader.AddToClassList("sidebar-title");
            layerHeader.style.marginTop = 10;
            layerHeader.style.marginLeft = 8;
            sidebarList.Add(layerHeader);

            // Add "All" Layer Item
            var allLayerItem = new VisualElement();
            allLayerItem.AddToClassList("sidebar-item");
            if (currentLayerId == "all") allLayerItem.AddToClassList("selected");
            var allLayerTitle = new Label("全部");
            allLayerTitle.AddToClassList("sidebar-item-title");
            allLayerItem.Add(allLayerTitle);
            allLayerItem.RegisterCallback<MouseDownEvent>(evt => {
                currentLayerId = "all";
                RefreshSidebar();
                RefreshCanvas();
            });
            sidebarList.Add(allLayerItem);

            // Add Layer Items
            foreach (var layer in memoData.layers)
            {
                var layerItem = new VisualElement();
                layerItem.AddToClassList("sidebar-item");
                if (currentLayerId == layer.id) layerItem.AddToClassList("selected");
                
                var layerTitle = new Label(layer.name);
                layerTitle.AddToClassList("sidebar-item-title");
                layerItem.Add(layerTitle);
                
                // Layer Context Menu
                layerItem.RegisterCallback<MouseDownEvent>(evt => {
                    if (evt.button == 1)
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("重命名"), false, () => {
                            // Simple rename dialog simulation (in real app use a popup)
                            // For now just append '*' to show it works
                            // layer.name += "*"; 
                            // SaveData(); RefreshSidebar();
                        });
                        if (layer.id != "default")
                        {
                            menu.AddItem(new GUIContent("删除图层"), false, () => {
                                if (EditorUtility.DisplayDialog("删除图层", "删除图层将同时删除该图层下的所有备忘录，确定吗？", "删除", "取消"))
                                {
                                    memoData.items.RemoveAll(m => m.layerId == layer.id);
                                    memoData.layers.Remove(layer);
                                    currentLayerId = "default";
                                    SaveData();
                                    RefreshSidebar();
                                    RefreshCanvas();
                                }
                            });
                        }
                        menu.ShowAsContext();
                        evt.StopPropagation();
                    }
                    else if (evt.button == 0)
                    {
                        currentLayerId = layer.id;
                        RefreshSidebar();
                        RefreshCanvas();
                    }
                });
                
                sidebarList.Add(layerItem);
            }
            
            // Add "New Layer" button
            var addLayerBtn = new Button(() => {
                var newLayer = new MemoLayer($"新图层 {memoData.layers.Count}");
                memoData.layers.Add(newLayer);
                SaveData();
                RefreshSidebar();
            });
            addLayerBtn.text = "+ 新建图层";
            addLayerBtn.AddToClassList("node-btn"); // Reuse style
            addLayerBtn.style.width = Length.Percent(100);
            addLayerBtn.style.marginTop = 5;
            sidebarList.Add(addLayerBtn);

            // Add Memo Header
            var memoHeader = new Label("备忘录");
            memoHeader.AddToClassList("sidebar-title");
            memoHeader.style.marginTop = 20;
            memoHeader.style.marginLeft = 8;
            sidebarList.Add(memoHeader);

            foreach (var memo in memoData.items.OrderByDescending(m => m.isPinned).ThenByDescending(m => m.timestamp))
            {
                // Filter by layer
                if (currentLayerId != "all" && memo.layerId != currentLayerId && !string.IsNullOrEmpty(memo.layerId)) continue;
                if (currentLayerId != "all" && string.IsNullOrEmpty(memo.layerId) && currentLayerId != "default") continue;

                // 侧边栏只显示匹配项
                if (!IsMatch(memo)) continue;

                var item = new VisualElement();
                item.AddToClassList("sidebar-item");
                if (selectedMemos.Contains(memo)) item.AddToClassList("selected");
                
                var title = new Label(string.IsNullOrEmpty(memo.title) ? "无标题" : memo.title);
                title.AddToClassList("sidebar-item-title");
                item.Add(title);
                
                item.RegisterCallback<MouseDownEvent>(evt => {
                    FocusMemo(memo.id);
                });
                
                sidebarList.Add(item);
            }
        }

        private void CreateNodeVisual(MemoItem memo)
        {
            var node = new VisualElement();
            node.AddToClassList("memo-node");
            node.userData = memo;
            
            // Apply styles based on data
            if (memo.isCompleted) node.AddToClassList("completed");
            if (memo.isPinned) node.AddToClassList("pinned");
            if (memo.colorIndex > 0) node.AddToClassList($"color-{memo.colorIndex}");
            if (selectedMemos.Contains(memo)) node.AddToClassList("selected");

            // 搜索高亮逻辑
            if (!string.IsNullOrEmpty(currentSearchTerm))
            {
                if (IsMatch(memo))
                {
                    node.AddToClassList("highlighted");
                }
                else
                {
                    node.AddToClassList("dimmed");
                }
            }

            // Header
            var header = new VisualElement();
            header.AddToClassList("memo-node-header");
            
            var title = new Label(string.IsNullOrEmpty(memo.title) ? "无标题" : memo.title);
            title.AddToClassList("memo-node-title");
            header.Add(title);
            
            // Header Actions (Fav & Delete)
            var actions = new VisualElement();
            actions.AddToClassList("memo-node-actions");
            
            var favBtn = new Button(() => TogglePin(memo));
            favBtn.AddToClassList("node-btn");
            favBtn.AddToClassList("node-fav-btn");
            if (memo.isPinned) favBtn.AddToClassList("active");
            actions.Add(favBtn);
            
            var delBtn = new Button(() => DeleteMemo(memo));
            delBtn.AddToClassList("node-btn");
            delBtn.AddToClassList("node-delete-btn");
            // delBtn.text = "×"; // Removed text, using icon
            actions.Add(delBtn);
            
            header.Add(actions);
            node.Add(header);

            // Content
            var content = new VisualElement();
            content.AddToClassList("memo-node-content");
            var preview = new Label(string.IsNullOrEmpty(memo.content) ? "无内容" : memo.content);
            preview.AddToClassList("memo-node-preview");
            content.Add(preview);
            node.Add(content);

            // Connectors (Red Circles)
            AddConnector(node, "top", memo);
            AddConnector(node, "bottom", memo);
            AddConnector(node, "left", memo);
            AddConnector(node, "right", memo);

            // Next Step Button (Inside Node)
            var nextBtn = new Button(() => FocusMemo(memo.nextMemoId));
            nextBtn.AddToClassList("node-next-btn");
            nextBtn.text = "→";
            if (!string.IsNullOrEmpty(memo.nextMemoId)) nextBtn.AddToClassList("visible");
            node.Add(nextBtn);

            // Events
            node.RegisterCallback<MouseDownEvent>(evt => OnNodeMouseDown(evt, memo, node));
            // 监听布局变化以重绘连线
            node.RegisterCallback<GeometryChangedEvent>(evt => connectionsLayer.MarkDirtyRepaint());
            
            nodesLayer.Add(node);
            UpdateNodeTransform(node, memo);
        }

        private void AddConnector(VisualElement node, string className, MemoItem memo)
        {
            var connector = new VisualElement();
            connector.AddToClassList("connector");
            connector.AddToClassList(className);
            connector.RegisterCallback<MouseDownEvent>(evt => OnConnectorMouseDown(evt, memo));
            connector.RegisterCallback<MouseUpEvent>(evt => OnConnectorMouseUp(evt, memo));
            node.Add(connector);
        }

        private void UpdateNodeTransform(VisualElement node, MemoItem memo)
        {
            node.style.left = memo.position.x;
            node.style.top = memo.position.y;
        }

        private void UpdateCanvasTransform()
        {
            nodesLayer.style.translate = new StyleTranslate(new Translate(panOffset.x, panOffset.y));
            nodesLayer.style.scale = new StyleScale(new Scale(new Vector2(zoomLevel, zoomLevel)));
            
            var grid = rootVisualElement.Q<VisualElement>(className: "grid-background");
            grid.MarkDirtyRepaint();
        }

        // Interaction Handlers

        private void OnCanvasWheel(WheelEvent evt)
        {
            // Cancel view animation on manual interaction
            isViewAnimating = false;

            float zoomDelta = -evt.delta.y * 0.005f; // Increased zoom speed
            float newZoom = Mathf.Clamp(zoomLevel + zoomDelta, 0.2f, 3.0f);
            
            // Zoom towards mouse position
            // 关键修复：确保缩放中心正确
            // 1. 计算鼠标在当前缩放下的画布坐标（相对于 nodesLayer 原点）
            Vector2 mousePos = evt.localMousePosition;
            Vector2 canvasPos = (mousePos - panOffset) / zoomLevel;
            
            // 2. 计算新的偏移量，使得鼠标下的点在新的缩放级别下仍然在鼠标位置
            // mousePos = newPanOffset + canvasPos * newZoom
            // newPanOffset = mousePos - canvasPos * newZoom
            panOffset = mousePos - canvasPos * newZoom;
            zoomLevel = newZoom;
            
            UpdateCanvasTransform();
            connectionsLayer.MarkDirtyRepaint();
            evt.StopPropagation();
        }

        private void OnCanvasMouseDown(MouseDownEvent evt)
        {
            // Ensure canvas has focus for key events
            canvasContainer.Focus();
            
            // Cancel view animation on manual interaction
            isViewAnimating = false;

            if (evt.button == 1) // Right click
            {
                ShowCanvasContextMenu(evt.localMousePosition);
                evt.StopPropagation();
                return;
            }

            if (evt.button == 2 || (evt.button == 0 && evt.altKey)) // Middle mouse or Alt+Left
            {
                isPanning = true;
                dragStartPos = evt.localMousePosition;
                canvasContainer.CaptureMouse(); // Capture mouse for smooth panning
                evt.StopPropagation();
            }
            else if (evt.button == 0)
            {
                // Start Selection Box
                isSelecting = true;
                selectionStartPos = evt.localMousePosition;
                selectionRect.style.display = DisplayStyle.Flex;
                UpdateSelectionRect(evt.localMousePosition);
                canvasContainer.CaptureMouse(); // Capture mouse for selection
                
                if (!evt.shiftKey && !evt.ctrlKey)
                {
                    ClearSelection();
                }
            }
        }

        private void OnCanvasMouseUp(MouseUpEvent evt)
        {
            if (isPanning || isSelecting || isDraggingNode)
            {
                canvasContainer.ReleaseMouse();
            }

            if (isDraggingNode)
            {
                MarkDirty();
            }

            isPanning = false;
            isDraggingNode = false;
            
            // Reset rotation targets to 0
            foreach (var memo in selectedMemos)
            {
                targetRotations[memo.id] = 0;
            }

            if (isSelecting)
            {
                isSelecting = false;
                selectionRect.style.display = DisplayStyle.None;
                SelectMemosInRect(GetRectFromPoints(selectionStartPos, evt.localMousePosition));
            }
            
            if (isConnecting)
            {
                isConnecting = false;
                connectingSourceId = null;
                connectionsLayer.MarkDirtyRepaint();
            }
        }

        private void OnCanvasMouseMove(MouseMoveEvent evt)
        {
            currentMousePos = evt.localMousePosition;

            if (isPanning)
            {
                Vector2 delta = evt.localMousePosition - dragStartPos;
                panOffset += delta;
                dragStartPos = evt.localMousePosition;
                UpdateCanvasTransform();
                connectionsLayer.MarkDirtyRepaint();
            }
            else if (isDraggingNode && selectedMemos.Count > 0)
            {
                // dragStartPos is in canvasContainer space (screen pixels)
                // We need to convert screen delta to node space delta
                Vector2 delta = (evt.localMousePosition - dragStartPos) / zoomLevel;
                
                foreach (var memo in selectedMemos)
                {
                    if (initialNodePositions.TryGetValue(memo.id, out Vector2 startPos))
                    {
                        // Instead of setting position directly, set target position for physics
                        Vector2 targetPos = startPos + delta;
                        targetPositions[memo.id] = targetPos;
                        
                        // Initialize current position if not present
                        if (!currentPositions.ContainsKey(memo.id))
                        {
                            currentPositions[memo.id] = memo.position;
                        }
                    }
                }
                
                // Repaint handled in UpdateAnimations
            }
            else if (isSelecting)
            {
                UpdateSelectionRect(evt.localMousePosition);
            }
            else if (isConnecting)
            {
                connectionsLayer.MarkDirtyRepaint();
            }
        }

        private void OnCanvasKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Delete)
            {
                DeleteSelectedMemos();
                evt.StopPropagation();
            }
        }

        private void OnNodeMouseDown(MouseDownEvent evt, MemoItem memo, VisualElement nodeVisual)
        {
            // Ensure canvas has focus for key events
            canvasContainer.Focus();

            if (evt.button == 1) // Right click
            {
                ShowNodeContextMenu(memo);
                evt.StopPropagation();
                return;
            }

            if (evt.button == 0)
            {
                if (evt.clickCount == 2)
                {
                    // Double click to focus/edit
                    ClearSelection();
                    AddToSelection(memo);
                    titleField.Focus();
                    evt.StopPropagation();
                    return;
                }

                if (evt.ctrlKey || evt.shiftKey)
                {
                    ToggleSelection(memo);
                }
                else if (!selectedMemos.Contains(memo))
                {
                    ClearSelection();
                    AddToSelection(memo);
                }
                
                isDraggingNode = true;
                // Capture mouse on container to handle drag outside node bounds
                canvasContainer.CaptureMouse(); 
                
                // Record start position in CanvasContainer space
                // evt.localMousePosition is relative to nodeVisual
                // Convert to world then to canvasContainer local
                dragStartPos = canvasContainer.WorldToLocal(nodeVisual.LocalToWorld(evt.localMousePosition));
                
                // Record initial positions for all selected nodes
                initialNodePositions.Clear();
                foreach (var m in selectedMemos)
                {
                    initialNodePositions[m.id] = m.position;
                    // Bring visual to front
                    var visual = nodesLayer.Children().FirstOrDefault(e => e.userData == m);
                    visual?.BringToFront();
                    
                    // Initialize physics state
                    targetPositions[m.id] = m.position;
                    currentPositions[m.id] = m.position;
                    positionVelocities[m.id] = Vector2.zero;
                }
                
                evt.StopPropagation();
            }
        }

        private void OnConnectorMouseDown(MouseDownEvent evt, MemoItem memo)
        {
            if (evt.button == 0)
            {
                isConnecting = true;
                connectingSourceId = memo.id;
                evt.StopPropagation();
            }
        }

        private void OnConnectorMouseUp(MouseUpEvent evt, MemoItem targetMemo)
        {
            if (isConnecting && !string.IsNullOrEmpty(connectingSourceId) && connectingSourceId != targetMemo.id)
            {
                var source = memoData.items.FirstOrDefault(m => m.id == connectingSourceId);
                if (source != null)
                {
                    source.nextMemoId = targetMemo.id;
                    MarkDirty();
                    RefreshCanvas(); // Refresh to show next button
                    UpdateInspector(); // Update inspector to show/hide next button
                }
            }
            
            isConnecting = false;
            connectingSourceId = null;
            connectionsLayer.MarkDirtyRepaint();
            evt.StopPropagation();
        }

        // Context Menus
        private void ShowCanvasContextMenu(Vector2 mousePosition)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("新建备忘录"), false, () => {
                CreateNewMemoAt(mousePosition);
            });
            menu.AddItem(new GUIContent("重置视图"), false, ResetView);
            menu.ShowAsContext();
        }

        private void ShowNodeContextMenu(MemoItem memo)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("编辑"), false, () => {
                ClearSelection();
                AddToSelection(memo);
                titleField.Focus();
            });
            menu.AddSeparator("");
            
            // 颜色菜单
            menu.AddItem(new GUIContent("标记/默认"), memo.colorIndex == 0, () => SetMemoColor(memo, 0));
            menu.AddItem(new GUIContent("标记/红色 (重要)"), memo.colorIndex == 1, () => SetMemoColor(memo, 1));
            menu.AddItem(new GUIContent("标记/绿色 (工作)"), memo.colorIndex == 2, () => SetMemoColor(memo, 2));
            menu.AddItem(new GUIContent("标记/蓝色 (个人)"), memo.colorIndex == 3, () => SetMemoColor(memo, 3));
            menu.AddItem(new GUIContent("标记/黄色 (灵感)"), memo.colorIndex == 4, () => SetMemoColor(memo, 4));
            
            menu.AddSeparator("");
            
            // 图层移动菜单
            foreach (var layer in memoData.layers)
            {
                menu.AddItem(new GUIContent($"移动到图层/{layer.name}"), memo.layerId == layer.id || (string.IsNullOrEmpty(memo.layerId) && layer.id == "default"), () => {
                    memo.layerId = layer.id;
                    SaveData();
                    RefreshCanvas(); // Might disappear if current layer is different
                    RefreshSidebar();
                });
            }

            menu.AddSeparator("");
            
            if (!string.IsNullOrEmpty(memo.nextMemoId))
            {
                menu.AddItem(new GUIContent("删除连线"), false, () => {
                    memo.nextMemoId = "";
                    MarkDirty();
                    RefreshCanvas();
                    UpdateInspector();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("删除连线"));
            }
            
            menu.AddItem(new GUIContent(memo.isPinned ? "取消置顶" : "置顶"), false, () => TogglePin(memo));
            menu.AddItem(new GUIContent(memo.isCompleted ? "标记为未完成" : "标记为完成"), false, () => {
                memo.isCompleted = !memo.isCompleted;
                MarkDirty();
                RefreshCanvas();
                if (selectedMemos.Contains(memo)) UpdateInspector();
            });
            
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("删除"), false, () => DeleteMemo(memo));
            
            menu.ShowAsContext();
        }

        private void CreateNewMemoAt(Vector2 screenPos)
        {
            // Convert screen pos (relative to canvasContainer) to node space
            Vector2 nodePos = (screenPos - panOffset) / zoomLevel;
            // Center the node on mouse
            nodePos -= new Vector2(110, 50);
            
            var newMemo = new MemoItem();
            newMemo.position = nodePos;
            newMemo.layerId = currentLayerId == "all" ? "default" : currentLayerId;
            
            memoData.items.Add(newMemo);
            SaveData();
            RefreshCanvas();
            RefreshSidebar();
            
            ClearSelection();
            AddToSelection(newMemo);
        }

        private void SetMemoColor(MemoItem memo, int colorIndex)
        {
            memo.colorIndex = colorIndex;
            MarkDirty();
            RefreshCanvas();
            if (selectedMemos.Contains(memo)) UpdateInspector();
        }

        // Selection Logic

        private void UpdateSelectionRect(Vector2 endPos)
        {
            var rect = GetRectFromPoints(selectionStartPos, endPos);
            selectionRect.style.left = rect.x;
            selectionRect.style.top = rect.y;
            selectionRect.style.width = rect.width;
            selectionRect.style.height = rect.height;
        }

        private Rect GetRectFromPoints(Vector2 p1, Vector2 p2)
        {
            return new Rect(Mathf.Min(p1.x, p2.x), Mathf.Min(p1.y, p2.y), Mathf.Abs(p1.x - p2.x), Mathf.Abs(p1.y - p2.y));
        }

        private void SelectMemosInRect(Rect selectionRect)
        {
            // Convert selection rect to canvas space
            Rect canvasRect = new Rect(
                (selectionRect.x - panOffset.x) / zoomLevel,
                (selectionRect.y - panOffset.y) / zoomLevel,
                selectionRect.width / zoomLevel,
                selectionRect.height / zoomLevel
            );

            foreach (var memo in memoData.items)
            {
                // Simple AABB check (node size approx 220x100)
                Rect nodeRect = new Rect(memo.position.x, memo.position.y, 220, 100);
                if (canvasRect.Overlaps(nodeRect))
                {
                    AddToSelection(memo);
                }
            }
        }

        private void ClearSelection()
        {
            foreach (var memo in selectedMemos)
            {
                var node = nodesLayer.Children().FirstOrDefault(e => e.userData == memo);
                node?.RemoveFromClassList("selected");
            }
            selectedMemos.Clear();
            ToggleInspector(false);
            RefreshSidebar(); // Update selection in sidebar
        }

        private void AddToSelection(MemoItem memo)
        {
            if (!selectedMemos.Contains(memo))
            {
                selectedMemos.Add(memo);
                var node = nodesLayer.Children().FirstOrDefault(e => e.userData == memo);
                node?.AddToClassList("selected");
                UpdateInspector();
                RefreshSidebar(); // Update selection in sidebar
            }
        }

        private void ToggleSelection(MemoItem memo)
        {
            if (selectedMemos.Contains(memo))
            {
                selectedMemos.Remove(memo);
                var node = nodesLayer.Children().FirstOrDefault(e => e.userData == memo);
                node?.RemoveFromClassList("selected");
            }
            else
            {
                selectedMemos.Add(memo);
                var node = nodesLayer.Children().FirstOrDefault(e => e.userData == memo);
                node?.AddToClassList("selected");
            }
            UpdateInspector();
            RefreshSidebar();
        }

        // Logic Methods

        private void CreateNewMemo()
        {
            var newMemo = new MemoItem();
            // Position at center of view
            Vector2 center = (new Vector2(canvasContainer.layout.width, canvasContainer.layout.height) * 0.5f - panOffset) / zoomLevel;
            newMemo.position = center - new Vector2(110, 50); // Center node
            newMemo.layerId = currentLayerId == "all" ? "default" : currentLayerId;
            
            memoData.items.Add(newMemo);
            SaveData();
            RefreshCanvas();
            RefreshSidebar();
            
            ClearSelection();
            AddToSelection(newMemo);
        }

        private void UpdateInspector()
        {
            if (selectedMemos.Count == 1)
            {
                var memo = selectedMemos[0];
                titleField.SetValueWithoutNotify(memo.title);
                contentField.SetValueWithoutNotify(memo.content);
                completedToggle.SetValueWithoutNotify(memo.isCompleted);
                pinnedToggle.SetValueWithoutNotify(memo.isPinned);
                UpdateColorButtons(memo.colorIndex);
                
                // Update Next Button visibility
                if (!string.IsNullOrEmpty(memo.nextMemoId))
                {
                    inspectorNextBtn.AddToClassList("visible");
                }
                else
                {
                    inspectorNextBtn.RemoveFromClassList("visible");
                }
                
                ToggleInspector(true);
            }
            else
            {
                ToggleInspector(false);
            }
        }

        private void UpdateSelectedMemos(Action<MemoItem> updateAction)
        {
            foreach (var memo in selectedMemos)
            {
                updateAction(memo);
                memo.timestamp = DateTime.Now.Ticks;
            }
            
            if (selectedMemos.Count > 0)
            {
                MarkDirty();
                RefreshCanvas();
                RefreshSidebar(); // Update titles in sidebar
                if (selectedMemos.Count == 1) UpdateColorButtons(selectedMemos[0].colorIndex);
            }
        }

        private void DeleteSelectedMemos()
        {
            if (selectedMemos.Count == 0) return;

            if (EditorUtility.DisplayDialog("删除备忘录", $"确定要删除选中的 {selectedMemos.Count} 个备忘录吗？", "删除", "取消"))
            {
                foreach (var memo in new List<MemoItem>(selectedMemos))
                {
                    DeleteMemoInternal(memo);
                }
                selectedMemos.Clear();
                ToggleInspector(false);
                SaveData();
                RefreshCanvas();
                RefreshSidebar();
            }
        }

        private void DeleteMemo(MemoItem memo)
        {
            if (EditorUtility.DisplayDialog("删除备忘录", "确定要删除这个备忘录吗？", "删除", "取消"))
            {
                DeleteMemoInternal(memo);
                if (selectedMemos.Contains(memo))
                {
                    selectedMemos.Remove(memo);
                    UpdateInspector();
                }
                SaveData();
                RefreshCanvas();
                RefreshSidebar();
            }
        }

        private void DeleteMemoInternal(MemoItem memo)
        {
            memoData.items.Remove(memo);
            // Remove connections to this memo
            foreach (var item in memoData.items)
            {
                if (item.nextMemoId == memo.id) item.nextMemoId = "";
            }
        }

        private void TogglePin(MemoItem memo)
        {
            memo.isPinned = !memo.isPinned;
            MarkDirty();
            RefreshCanvas();
            RefreshSidebar();
            if (selectedMemos.Count == 1 && selectedMemos[0] == memo) UpdateInspector();
        }

        private void FocusMemo(string memoId)
        {
            var target = memoData.items.FirstOrDefault(m => m.id == memoId);
            if (target != null)
            {
                // Switch layer if needed
                if (currentLayerId != "all" && target.layerId != currentLayerId)
                {
                    // Handle empty layerId as "default"
                    string targetLayer = string.IsNullOrEmpty(target.layerId) ? "default" : target.layerId;
                    if (targetLayer != currentLayerId)
                    {
                        currentLayerId = targetLayer;
                        RefreshSidebar();
                        RefreshCanvas();
                    }
                }

                // Center view on target
                Vector2 center = new Vector2(canvasContainer.layout.width, canvasContainer.layout.height) * 0.5f;
                
                // Set target for animation
                targetPanOffset = center - target.position * zoomLevel - new Vector2(110, 50) * zoomLevel;
                targetZoomLevel = zoomLevel; // Keep current zoom or set a default focus zoom
                
                isViewAnimating = true;
                
                ClearSelection();
                AddToSelection(target);
            }
        }

        private void ToggleInspector(bool show)
        {
            if (show) inspectorPanel.RemoveFromClassList("hidden");
            else inspectorPanel.AddToClassList("hidden");
        }

        private void ToggleSidebar()
        {
            if (sidebarPanel.ClassListContains("collapsed"))
                sidebarPanel.RemoveFromClassList("collapsed");
            else
                sidebarPanel.AddToClassList("collapsed");
        }

        private void UpdateColorButtons(int selectedIndex)
        {
            for (int i = 0; i <= 4; i++)
            {
                var btn = rootVisualElement.Q<Button>($"color-{i}");
                if (btn != null)
                {
                    if (i == selectedIndex) btn.AddToClassList("selected");
                    else btn.RemoveFromClassList("selected");
                }
            }
        }

        private void ResetView()
        {
            // Animate reset
            targetPanOffset = Vector2.zero;
            targetZoomLevel = 1.0f;
            isViewAnimating = true;
        }

        private void UpdateStatus(string message)
        {
            if (statusLabel != null) statusLabel.text = message;
        }

        private void LoadData()
        {
            if (File.Exists(DataPath))
            {
                try
                {
                    string json = File.ReadAllText(DataPath);
                    memoData = JsonUtility.FromJson<MemoDataList>(json);
                    
                    // Restore view state
                    if (memoData.viewZoom > 0)
                    {
                        panOffset = memoData.viewOffset;
                        zoomLevel = memoData.viewZoom;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"加载备忘录数据失败: {e.Message}");
                    memoData = new MemoDataList();
                }
            }
            else
            {
                memoData = new MemoDataList();
            }
        }

        private void MarkDirty()
        {
            isDirty = true;
            lastEditTime = EditorApplication.timeSinceStartup;
        }

        private void SaveData()
        {
            try
            {
                // Save view state
                memoData.viewOffset = panOffset;
                memoData.viewZoom = zoomLevel;
                
                string json = JsonUtility.ToJson(memoData, true);
                File.WriteAllText(DataPath, json);
                isDirty = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"保存备忘录数据失败: {e.Message}");
            }
        }
    }
}
