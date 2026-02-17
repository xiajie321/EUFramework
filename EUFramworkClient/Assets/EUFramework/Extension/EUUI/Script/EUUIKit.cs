using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// EUUI Kit 静态管理类（管理 UIRoot、层级、面板生命周期）
    /// 分部类，可通过扩展模板添加资源加载等功能（如 EUUIKit.EURes.Generated.cs）
    /// </summary>
    public static partial class EUUIKit
    {
        private static EUUIKitConfig _config;
        private static GameObject _euuiRoot;
        private static GameObject _poolRoot;
        private static Camera _uiCamera;
        private static Canvas _canvas;
        private static CanvasScaler _canvasScaler;

        private static Dictionary<EUUILayerEnum, RectTransform> _layers = new Dictionary<EUUILayerEnum, RectTransform>();
        private static Dictionary<string, IEUUIPanel> _activePanels = new Dictionary<string, IEUUIPanel>();
        private static Stack<string> _panelStack = new Stack<string>();
        private static HashSet<string> _opening = new HashSet<string>();

        private static bool _initialized = false;

        /// <summary>
        /// 运行时配置
        /// </summary>
        public static EUUIKitConfig Config
        {
            get
            {
                if (_config == null)
                {
                    _config = Resources.Load<EUUIKitConfig>("EUUIKitConfig");
                    if (_config == null)
                    {
                        Debug.LogWarning("[EUUIKit] 未找到 Resources/EUUIKitConfig.asset，使用默认配置");
                        _config = ScriptableObject.CreateInstance<EUUIKitConfig>();
                    }
                }
                return _config;
            }
        }

        /// <summary>
        /// 初始化 UI 系统（建议在游戏启动时调用一次）
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
            {
                Debug.LogWarning("[EUUIKit] 已经初始化过，跳过");
                return;
            }

            // 1. 查找或创建 EUUIRoot
            _euuiRoot = GameObject.Find("EUUIRoot");
            if (_euuiRoot == null)
            {
                _euuiRoot = new GameObject("EUUIRoot",
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                _euuiRoot.layer = LayerMask.NameToLayer("UI");
                UnityEngine.Object.DontDestroyOnLoad(_euuiRoot);
            }

            // 2. 配置 Canvas（ScreenSpaceCamera 模式）
            _canvas = GetOrAddComponent<Canvas>(_euuiRoot);
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;

            // 3. 配置 CanvasScaler
            _canvasScaler = GetOrAddComponent<CanvasScaler>(_euuiRoot);
            _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _canvasScaler.referenceResolution = Config.referenceResolution;
            _canvasScaler.matchWidthOrHeight = Config.matchWidthOrHeight;
            _canvasScaler.referencePixelsPerUnit = Config.referencePixelsPerUnit;

            GetOrAddComponent<GraphicRaycaster>(_euuiRoot);

            // 4. 创建 UI 相机
            InitUICamera();

            // 5. 创建 PoolRoot
            var poolTrans = _euuiRoot.transform.Find("PoolRoot");
            if (poolTrans == null)
            {
                _poolRoot = new GameObject("PoolRoot");
                _poolRoot.transform.SetParent(_euuiRoot.transform, false);
            }
            else
            {
                _poolRoot = poolTrans.gameObject;
            }
            _poolRoot.SetActive(false);

            // 6. 初始化层级
            InitLayers();

            // 7. 确保 EventSystem
            EnsureEventSystem();

            _initialized = true;
            Debug.Log("[EUUIKit] 初始化完成");
        }

        private static void InitUICamera()
        {
            var camTrans = _euuiRoot.transform.Find("UICamera");
            if (camTrans == null)
            {
                var camGO = new GameObject("UICamera", typeof(Camera));
                camGO.transform.SetParent(_euuiRoot.transform, false);
                _uiCamera = camGO.GetComponent<Camera>();
            }
            else
            {
                _uiCamera = camTrans.GetComponent<Camera>();
                if (_uiCamera == null) _uiCamera = camTrans.gameObject.AddComponent<Camera>();
            }

            _uiCamera.clearFlags = Config.uiCameraClearFlags;
            _uiCamera.depth = Config.uiCameraDepth;
            _uiCamera.cullingMask = Config.uiCullingMask;
            _uiCamera.orthographic = true;

            _canvas.worldCamera = _uiCamera;
            _canvas.planeDistance = 100f;
        }

        private static void InitLayers()
        {
            _layers.Clear();
            foreach (EUUILayerEnum layer in Enum.GetValues(typeof(EUUILayerEnum)))
            {
                string layerName = layer.ToString();
                var layerTrans = _euuiRoot.transform.Find(layerName);

                if (layerTrans == null)
                {
                    var layerGO = new GameObject(layerName, typeof(RectTransform));
                    layerGO.transform.SetParent(_euuiRoot.transform, false);
                    var rt = layerGO.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;
                    rt.anchoredPosition = Vector2.zero;
                    layerTrans = rt;
                }
                _layers[layer] = layerTrans as RectTransform;
            }
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                UnityEngine.Object.DontDestroyOnLoad(es);
            }
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        /// <summary>
        /// 获取对象池根节点（业务层可用此节点管理对象池）
        /// </summary>
        public static GameObject GetPoolRoot()
        {
            if (!_initialized) Initialize();
            return _poolRoot;
        }

        /// <summary>
        /// 获取指定层级的 RectTransform
        /// </summary>
        public static RectTransform GetLayer(EUUILayerEnum layer)
        {
            if (!_initialized) Initialize();
            return _layers.TryGetValue(layer, out var rt) ? rt : null;
        }

        /// <summary>
        /// 打开面板（异步）
        /// 不记录导航历史，适用于弹窗、提示框等
        /// 如需导航功能，请使用 NavigateToAsync
        /// </summary>
        public static async UniTask<T> OpenAsync<T>(IEUUIPanelData data = null) where T : EUUIPanelBase<T>
        {
            if (!_initialized) Initialize();

            string panelName = typeof(T).Name;

            // 如果已打开，直接返回并显示
            if (_activePanels.TryGetValue(panelName, out var existingPanel))
            {
                existingPanel.Show();
                return existingPanel as T;
            }

            // 防重复打开
            if (_opening.Contains(panelName))
            {
                Debug.LogWarning($"[EUUIKit] 面板 {panelName} 正在打开中，请勿重复调用");
                return null;
            }

            _opening.Add(panelName);

            try
            {
                // 从 Prefab 加载
                GameObject panelGO = await LoadPanelPrefabAsync<T>();
                if (panelGO == null)
                {
                    Debug.LogError($"[EUUIKit] 加载面板 Prefab 失败: {panelName}");
                    return null;
                }

                var panel = panelGO.GetComponent<T>();
                if (panel == null)
                {
                    Debug.LogError($"[EUUIKit] Prefab 上未找到组件: {typeof(T).Name}");
                    UnityEngine.Object.Destroy(panelGO);
                    return null;
                }

                // 放到对应层级
                panelGO.transform.SetParent(GetLayer(panel.DefaultLayer), false);
                panelGO.SetActive(true);

                _activePanels[panelName] = panel;

                await panel.OpenAsync(data);
                return panel;
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUUIKit] 打开面板 {panelName} 失败: {e.Message}\n{e.StackTrace}");
                return null;
            }
            finally
            {
                _opening.Remove(panelName);
            }
        }

        /// <summary>
        /// 关闭面板
        /// 如果面板在导航栈中，会自动从栈中移除
        /// </summary>
        public static void Close<T>() where T : EUUIPanelBase<T>
        {
            string panelName = typeof(T).Name;
            if (_activePanels.TryGetValue(panelName, out var panel))
            {
                _activePanels.Remove(panelName);
                RemoveFromPanelStack(panelName);
                panel.Close();
                
                // 释放资源（由扩展实现）
                OnPanelClosed(panelName);
            }
        }

        /// <summary>
        /// 面板关闭后的钩子（由扩展实现，如资源释放）
        /// </summary>
        static partial void OnPanelClosed(string panelName);

        /// <summary>
        /// 获取当前激活的面板
        /// </summary>
        public static T GetPanel<T>() where T : EUUIPanelBase<T>
        {
            string panelName = typeof(T).Name;
            return _activePanels.TryGetValue(panelName, out var panel) ? panel as T : null;
        }

        #region 面板栈导航

        /// <summary>
        /// 导航到指定面板（隐藏当前面板并记录历史）
        /// 适用于主流程页面，支持 BackAsync 返回上一页
        /// </summary>
        public static async UniTask NavigateToAsync<T>(IEUUIPanelData data = null) where T : EUUIPanelBase<T>
        {
            if (!_initialized) Initialize();

            string panelName = typeof(T).Name;

            // 情况1：已经在栈顶，无需操作
            if (_panelStack.Count > 0 && _panelStack.Peek() == panelName)
            {
                return;
            }

            // 情况2：面板已在栈中但非栈顶 → 执行 BackToAsync
            if (_panelStack.Contains(panelName))
            {
                await BackToAsync<T>();
                return;
            }

            // 情况3：正常导航到新面板
            // 隐藏当前栈顶面板
            if (_panelStack.Count > 0)
            {
                var topName = _panelStack.Peek();
                if (_activePanels.TryGetValue(topName, out var topPanel))
                {
                    topPanel.Hide();
                }
            }

            // 打开新面板并压栈
            await OpenAsync<T>(data);
            _panelStack.Push(panelName);
        }

        /// <summary>
        /// 返回上一个面板
        /// </summary>
        /// <param name="showNext">是否显示新的栈顶面板（循环调用时传 false）</param>
        public static async UniTask BackAsync(bool showNext = true)
        {
            if (_panelStack.Count == 0)
            {
                Debug.LogWarning("[EUUIKit] 面板历史栈为空，无法返回");
                return;
            }

            // 弹出并关闭栈顶面板
            var topName = _panelStack.Pop();
            if (_activePanels.TryGetValue(topName, out var topPanel))
            {
                topPanel.Hide();
                if (topPanel.EnableClose)
                {
                    topPanel.Close();
                    _activePanels.Remove(topName);
                }
            }

            await UniTask.Yield();

            // 只在需要时显示新的栈顶面板
            if (showNext && _panelStack.Count > 0)
            {
                var newTopName = _panelStack.Peek();
                if (_activePanels.TryGetValue(newTopName, out var newTopPanel))
                {
                    newTopPanel.Show();
                }
            }
        }

        /// <summary>
        /// 返回到指定面板（关闭中间所有面板）
        /// </summary>
        public static async UniTask BackToAsync<T>() where T : EUUIPanelBase<T>
        {
            string targetName = typeof(T).Name;

            // 中间面板关闭时不显示新栈顶，避免不必要的 Show/Hide
            while (_panelStack.Count > 0 && _panelStack.Peek() != targetName)
            {
                await BackAsync(showNext: false);
            }

            // 最后显示目标面板
            if (_panelStack.Count > 0 && _activePanels.TryGetValue(targetName, out var targetPanel))
            {
                targetPanel.Show();
            }
        }

        /// <summary>
        /// 清空面板历史栈（关闭所有栈中的面板）
        /// </summary>
        public static async UniTask ClearHistoryAsync()
        {
            while (_panelStack.Count > 0)
            {
                await BackAsync(showNext: false);
            }
        }

        /// <summary>
        /// 独占式打开面板（关闭其他所有面板，清空导航历史）
        /// 适用于切换到全新流程，如：从大厅进入战斗
        /// </summary>
        public static async UniTask OpenExclusiveAsync<T>(IEUUIPanelData data = null) where T : EUUIPanelBase<T>
        {
            string targetName = typeof(T).Name;

            bool isTargetOpen = _activePanels.ContainsKey(targetName);

            if (!isTargetOpen)
            {
                await OpenAsync<T>(data);
            }
            else
            {
                if (_activePanels.TryGetValue(targetName, out var targetPanel))
                {
                    targetPanel.Show();
                }
            }

            CloseAllExcept<T>();
        }

        /// <summary>
        /// 关闭除指定面板外的所有面板（同时清空导航历史）
        /// </summary>
        public static void CloseAllExcept<T>() where T : EUUIPanelBase<T>
        {
            string targetName = typeof(T).Name;
            var tempList = _activePanels.Keys.ToArray();

            foreach (var name in tempList)
            {
                if (name != targetName)
                {
                    if (_activePanels.TryGetValue(name, out var panel))
                    {
                        panel.Hide();
                        if (panel.EnableClose)
                        {
                            panel.Close();
                            _activePanels.Remove(name);
                        }
                    }
                }
            }

            // 清空导航历史
            _panelStack.Clear();
        }

        /// <summary>
        /// 关闭所有面板（清空导航历史）
        /// </summary>
        public static void CloseAll()
        {
            var tempList = _activePanels.Keys.ToArray();

            foreach (var name in tempList)
            {
                if (_activePanels.TryGetValue(name, out var panel))
                {
                    panel.Hide();
                    if (panel.EnableClose)
                    {
                        panel.Close();
                        _activePanels.Remove(name);
                    }
                }
            }

            // 清空导航历史
            _panelStack.Clear();
        }

        /// <summary>
        /// 从导航栈中移除指定面板（内部方法）
        /// </summary>
        private static void RemoveFromPanelStack(string panelName)
        {
            if (!_panelStack.Contains(panelName)) return;

            // Stack 不支持随机删除，需要重建
            var tempList = _panelStack.ToList();
            tempList.Remove(panelName);
            _panelStack.Clear();

            // 反转后重新压入（保持原有顺序）
            tempList.Reverse();
            foreach (var name in tempList)
            {
                _panelStack.Push(name);
            }
        }

        #endregion

        #region 面板状态查询

        /// <summary>
        /// 获取当前栈顶面板名称
        /// </summary>
        public static string GetCurrentPanelName()
        {
            return _panelStack.Count > 0 ? _panelStack.Peek() : null;
        }

        /// <summary>
        /// 获取面板历史栈深度
        /// </summary>
        public static int GetHistoryCount()
        {
            return _panelStack.Count;
        }

        /// <summary>
        /// 检查面板是否已打开
        /// </summary>
        public static bool IsPanelOpen<T>() where T : EUUIPanelBase<T>
        {
            return _activePanels.ContainsKey(typeof(T).Name);
        }

        /// <summary>
        /// 检查面板是否在导航栈中
        /// </summary>
        public static bool IsPanelInStack<T>() where T : EUUIPanelBase<T>
        {
            return _panelStack.Contains(typeof(T).Name);
        }

        #endregion

#if !EUUI_EXTENSIONS_GENERATED
        /// <summary>
        /// 加载面板 Prefab - 占位方法（未生成扩展时编译）
        /// 生成扩展代码后，项目会定义 EUUI_EXTENSIONS_GENERATED，此方法不再编译，由生成文件提供实现
        /// </summary>
        private static async UniTask<GameObject> LoadPanelPrefabAsync<T>() where T : EUUIPanelBase<T>
        {
            Debug.LogError("[EUUIKit] 资源加载扩展未生成！\n" +
                          "请执行以下步骤：\n" +
                          "1. 打开 EUUI 配置工具（菜单：EUFramework/拓展/EUUI 配置工具）\n" +
                          "2. 在「拓展」→「生成绑定模板」中选择资源加载器类型\n" +
                          "3. 点击「生成扩展代码」按钮");
            await UniTask.Yield();
            return null;
        }
#endif
    }
}
