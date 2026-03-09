using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// 多人同屏区域划分模式
    /// </summary>
    public enum MultiplayerLayoutMode
    {
        /// <summary>按 X 或 Y 轴等分（条状）</summary>
        Linear,
        /// <summary>田字 2×2 四宫格，可 1～4 人，空位黑屏/占位</summary>
        Grid
    }

    /// <summary>
    /// 线性等分时的轴向（仅 Linear 时有效）
    /// </summary>
    public enum MultiplayerLayoutAxis
    {
        /// <summary>按 X 等分：多列，从左到右 Player 1,2,...</summary>
        X,
        /// <summary>按 Y 等分：多行，从上到下 Player 1,2,...</summary>
        Y
    }

    public static partial class EUUIKit
    {
        private const int MultiplayerMaxPlayers = 4;
        private const int MultiplayerSlotCountFixed = 4;

        /// <summary>MultiplayerUIEvent InputActionAsset 在 Resources 下的加载路径名</summary>
        public const string MultiplayerUIEventAssetName = "MultiplayerUIEvent";

        /// <summary>各玩家槽位对应的 ActionMap 名，与 MultiplayerUIEvent.inputactions 中 Map 顺序一致</summary>
        private static readonly string[] MultiplayerMapNames =
            { "PlayerUI_1", "PlayerUI_2", "PlayerUI_3", "PlayerUI_4" };

        private static GameObject[] _multiplayerRoots;
        private static EventSystem[] _multiplayerEventSystems;
        /// <summary>各玩家克隆的 InputActionAsset，用于设备限定（asset.devices）和 Map 隔离</summary>
        private static InputActionAsset[] _multiplayerPlayerAssets;
        private static int _multiplayerPlayerCount;
        private static bool _multiplayerInitialized;

        // ── per-player 面板状态 ────────────────────────────────
        // 索引 [0..3] 对应 Player1Root..Player4Root
        private static Dictionary<EUUILayerEnum, RectTransform>[] _playerLayers;
        private static Dictionary<string, IEUUIPanel>[]           _playerActivePanels;
        private static Stack<string>[]                            _playerPanelStacks;
        private static HashSet<string>[]                          _playerOpening;

        /// <summary>
        /// 多人系统初始化完成后的钩子，由 EUInputController Generated 分部实现。
        /// 用于注册设备热插拔监听并执行初次设备分配。
        /// </summary>
        static partial void OnMultiplayerInitialized(int playerCount);

        /// <summary>
        /// InputControllerAsset 模式下的 Asset 解析钩子，由 EUInputController Generated 分部实现。
        /// 若当前模式为 InputControllerAsset，Generated 将 asset 设为对应 PlayerInputController 的
        /// InputController Asset，mapName 设为其 UI Map 名称；否则保持 null 不干预。
        /// </summary>
        static partial void OnResolveMultiplayerPlayerAsset(int slotIndex, ref InputActionAsset asset, ref string mapName);

        /// <summary>当前多人玩家数量</summary>
        public static int MultiplayerPlayerCount => _multiplayerPlayerCount;

        /// <summary>固定槽位数量，始终为 4</summary>
        public static int MultiplayerSlotCount => MultiplayerSlotCountFixed;

        /// <summary>获取第 index 个槽位根节点（0～3），未初始化时为 null</summary>
        public static GameObject GetMultiplayerRoot(int index)
        {
            if (_multiplayerRoots == null || index < 0 || index >= _multiplayerRoots.Length)
                return null;
            return _multiplayerRoots[index];
        }

        /// <summary>获取第 index 个玩家的 EventSystem（0～3），未初始化时为 null</summary>
        public static EventSystem GetMultiplayerEventSystem(int index)
        {
            if (_multiplayerEventSystems == null || index < 0 || index >= _multiplayerEventSystems.Length)
                return null;
            return _multiplayerEventSystems[index];
        }

        /// <summary>
        /// 获取第 index 个玩家的克隆 InputActionAsset（0～3），未初始化或无 Asset 时为 null。
        /// 可通过 asset.devices 限制哪些物理设备可触发此玩家的 Action。
        /// </summary>
        public static InputActionAsset GetMultiplayerPlayerAsset(int index)
        {
            if (_multiplayerPlayerAssets == null || index < 0 || index >= _multiplayerPlayerAssets.Length)
                return null;
            return _multiplayerPlayerAssets[index];
        }

        /// <summary>已废弃：PlayerInput 已移除，统一改用 InputActionAsset.devices 管理设备。始终返回 null。</summary>
        public static PlayerInput GetMultiplayerPlayerInput(int index) => null;

        /// <summary>
        /// 初始化多人：固定创建 4 个根节点 + 4 个 EventSystem（仅首次创建，之后复用）。
        /// 默认布局为 4 人田字。需在 EUUIKit.Initialize() 之后调用。
        /// </summary>
        public static void InitMultiplayerEventSystem()
        {
            InitMultiplayerEventSystem(4, MultiplayerLayoutMode.Grid, MultiplayerLayoutAxis.X);
        }

        /// <summary>
        /// 初始化多人：固定 4 节点 + 4 EventSystem。若已初始化则只切换布局。
        /// </summary>
        public static void InitMultiplayerEventSystem(int playerCount, MultiplayerLayoutMode layout, MultiplayerLayoutAxis axis = MultiplayerLayoutAxis.X)
        {
            if (EUUIRoot == null)
            {
                Debug.LogError("[EUUIKit.Multiplayer] EUUIRoot 为空，请先调用 EUUIKit.Initialize()");
                return;
            }

            playerCount = Mathf.Clamp(playerCount, 1, MultiplayerMaxPlayers);

            if (!_multiplayerInitialized)
            {
                _multiplayerRoots        = new GameObject[MultiplayerSlotCountFixed];
                _multiplayerEventSystems = new EventSystem[MultiplayerSlotCountFixed];
                _multiplayerPlayerAssets = new InputActionAsset[MultiplayerSlotCountFixed];

                _playerLayers       = new Dictionary<EUUILayerEnum, RectTransform>[MultiplayerSlotCountFixed];
                _playerActivePanels = new Dictionary<string, IEUUIPanel>[MultiplayerSlotCountFixed];
                _playerPanelStacks  = new Stack<string>[MultiplayerSlotCountFixed];
                _playerOpening      = new HashSet<string>[MultiplayerSlotCountFixed];
                for (int i = 0; i < MultiplayerSlotCountFixed; i++)
                {
                    _playerLayers[i]       = new Dictionary<EUUILayerEnum, RectTransform>();
                    _playerActivePanels[i] = new Dictionary<string, IEUUIPanel>();
                    _playerPanelStacks[i]  = new Stack<string>();
                    _playerOpening[i]      = new HashSet<string>();
                }

                for (int i = 0; i < MultiplayerSlotCountFixed; i++)
                    CreatePlayerSlotUnderRoot(EUUIRoot, i);

                InputActionAsset sourceAsset = LoadMultiplayerUIInputAsset();
                if (sourceAsset == null)
                    Debug.LogWarning("[EUUIKit.Multiplayer] 未找到 MultiplayerUIEvent Asset，InputModule 将使用 DefaultInputActions（无键盘分区）。");

                for (int i = 0; i < MultiplayerSlotCountFixed; i++)
                    CreatePlayerEventSystem(i, sourceAsset);

                _multiplayerInitialized = true;
                OnMultiplayerInitialized(playerCount);
            }

            SetMultiplayerLayout(playerCount, layout, axis);
        }

        /// <summary>
        /// 为指定玩家槽位分配键盘
        /// </summary>
        public static void AssignKeyboardPlayer(int index)
        {
            var asset = GetValidPlayerAsset(index);
            if (asset == null) return;

            var list = new List<InputDevice>(2);
            if (Keyboard.current != null) list.Add(Keyboard.current);
            if (Mouse.current != null) list.Add(Mouse.current);
            asset.devices = list.Count > 0 ? list.ToArray() : null;
            Debug.Log($"[EUUIKit.Multiplayer] P{index + 1} → 键盘区域 {MultiplayerMapNames[index]}（仅键鼠，不接收手柄）");
        }
        /// <summary>
        /// 为指定玩家槽位分配手柄
        /// </summary>
        public static void AssignGamepadPlayer(int index, Gamepad gamepad)
        {
            var asset = GetValidPlayerAsset(index);
            if (asset == null || gamepad == null) return;

            // 限定 Asset 只接受来自此手柄的输入，实现手柄的独占分配
            asset.devices = new InputDevice[] { gamepad };
            Debug.Log($"[EUUIKit.Multiplayer] P{index + 1} → 手柄 {gamepad.displayName}（独占锁定）");
        }

        /// <summary>
        /// 仅切换布局：更新 4 个 Canvas 的区域与空位占位，不创建/销毁节点或 EventSystem。
        /// </summary>
        public static void SetMultiplayerLayout(int playerCount, MultiplayerLayoutMode layout, MultiplayerLayoutAxis axis = MultiplayerLayoutAxis.X)
        {
            if (_multiplayerRoots == null)
            {
                Debug.LogWarning("[EUUIKit.Multiplayer] 未初始化多人，请先调用 InitMultiplayerEventSystem。");
                return;
            }

            playerCount = Mathf.Clamp(playerCount, 1, MultiplayerMaxPlayers);
            _multiplayerPlayerCount = playerCount;

            int effectiveSlotCount = layout == MultiplayerLayoutMode.Grid ? 4 : playerCount;

            for (int i = 0; i < MultiplayerSlotCountFixed; i++)
            {
                Rect rect = i < effectiveSlotCount
                    ? GetSlotRect(i, effectiveSlotCount, layout, axis)
                    : GetZeroRect();
                ApplySlotRect(_multiplayerRoots[i], rect);

                bool isEmptySlot = layout == MultiplayerLayoutMode.Grid && i >= playerCount;
                ApplyEmptySlotIfNeeded(_multiplayerRoots[i], isEmptySlot);
            }

            Debug.Log($"[EUUIKit.Multiplayer] 布局已切换：玩家数={playerCount}，布局={layout}");
        }

        /// <summary>
        /// 计算槽位的归一化范围矩形（xMin/yMin → xMax/yMax，左下 0,0，右上 1,1）。
        /// 使用 Rect.MinMaxRect 避免 new Rect(x,y,w,h) 将第三/四参数误解为 width/height。
        /// </summary>
        public static Rect GetSlotRect(int slotIndex, int slotCount, MultiplayerLayoutMode layout, MultiplayerLayoutAxis axis)
        {
            if (layout == MultiplayerLayoutMode.Grid)
            {
                float half = 0.5f;
                int row = slotIndex >= 2 ? 0 : 1;
                int col = slotIndex % 2;
                return Rect.MinMaxRect(col * half, row * half, (col + 1) * half, (row + 1) * half);
            }

            float step = 1f / slotCount;
            if (axis == MultiplayerLayoutAxis.X)
                return Rect.MinMaxRect(slotIndex * step, 0f, (slotIndex + 1) * step, 1f);
            return Rect.MinMaxRect(0f, 1f - (slotIndex + 1) * step, 1f, 1f - slotIndex * step);
        }

        private static Rect GetZeroRect() => new Rect(0f, 0f, 0f, 0f);

        /// <summary>
        /// 将归一化矩形写入 RectTransform，使用拉伸锚点（anchorMin/anchorMax = 归一化 rect）
        /// + 零 offset，使玩家区域随父 Canvas 尺寸自适应，不依赖 referenceResolution。
        /// 这样无论屏幕比例（1920×1080、2340×1080 等）如何变化，区域始终正确填满对应位置。
        /// </summary>
        private static void ApplySlotRect(GameObject root, Rect anchorRect)
        {
            if (root == null) return;
            var rt = root.GetComponent<RectTransform>();
            if (rt == null) return;

            if (anchorRect.width <= 0f || anchorRect.height <= 0f)
            {
                // 隐藏槽位：零尺寸，不占屏幕空间
                rt.anchorMin        = new Vector2(0.5f, 0.5f);
                rt.anchorMax        = new Vector2(0.5f, 0.5f);
                rt.sizeDelta        = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
                return;
            }

            // 拉伸锚点：anchorMin/anchorMax 直接对应归一化区域，offset 归零
            // 随父 Canvas 逻辑尺寸自动拉伸，不依赖 referenceResolution 固定像素值
            rt.anchorMin        = new Vector2(anchorRect.xMin, anchorRect.yMin);
            rt.anchorMax        = new Vector2(anchorRect.xMax, anchorRect.yMax);
            rt.sizeDelta        = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static InputActionAsset LoadMultiplayerUIInputAsset()
        {
            var asset = Resources.Load<InputActionAsset>(MultiplayerUIEventAssetName);
            if (asset == null)
                asset = Resources.Load<InputActionAsset>("EUUI/" + MultiplayerUIEventAssetName);
            return asset;
        }

        private static void CreatePlayerSlotUnderRoot(GameObject euuiRoot, int slotIndex)
        {
            string rootName = "Player" + (slotIndex + 1) + "Root";
            Transform existing = euuiRoot.transform.Find(rootName);
            if (existing != null)
            {
                _multiplayerRoots[slotIndex] = existing.gameObject;
                InitPlayerLayers(existing.gameObject, slotIndex);
                return;
            }

            // PlayerXRoot 只作为布局容器，不挂 Canvas / GraphicRaycaster：
            //   - 避免子 Canvas 创建独立渲染上下文（子Canvas坐标系与父Canvas不同步）
            //   - 避免 CanvasScaler/renderMode 在子 Canvas 上的不确定行为
            //   - 所有渲染/点击由父 EUUIRoot 的 Canvas + GraphicRaycaster 统一处理
            //   - MultiplayerEventSystem.playerRoot 只需 RectTransform 即可做空间过滤
            var rootGo = new GameObject(rootName, typeof(RectTransform));
            rootGo.layer = LayerMask.NameToLayer("UI");
            rootGo.transform.SetParent(euuiRoot.transform, false);

            var rt = rootGo.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            _multiplayerRoots[slotIndex] = rootGo;
            InitPlayerLayers(rootGo, slotIndex);
        }

        /// <summary>
        /// 在玩家根节点下创建与全局层级同名的子层，用于承载 per-player 面板。
        /// 仅在首次创建根节点时调用（复用路径已跳过）。
        /// </summary>
        private static void InitPlayerLayers(GameObject playerRoot, int slotIndex)
        {
            var dict = _playerLayers[slotIndex];
            dict.Clear();

            foreach (EUUILayerEnum layer in Enum.GetValues(typeof(EUUILayerEnum)))
            {
                string layerName = layer.ToString();
                Transform existing = playerRoot.transform.Find(layerName);
                RectTransform rt;

                if (existing != null)
                {
                    rt = existing as RectTransform ?? existing.GetComponent<RectTransform>();
                }
                else
                {
                    var layerGO = new GameObject(layerName, typeof(RectTransform));
                    layerGO.transform.SetParent(playerRoot.transform, false);
                    rt = layerGO.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;
                    rt.anchoredPosition = Vector2.zero;
                }
                dict[layer] = rt;
            }
        }

        private static void ApplyEmptySlotIfNeeded(GameObject root, bool isEmptySlot)
        {
            Transform empty = root.transform.Find("EmptySlot");
            if (isEmptySlot && empty == null)
                CreateEmptySlotPlaceholder(root);
            else if (!isEmptySlot && empty != null)
                UnityEngine.Object.Destroy(empty.gameObject);
        }

        private static void CreateEmptySlotPlaceholder(GameObject slotRoot)
        {
            var go = new GameObject("EmptySlot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(slotRoot.transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;
        }

        private static void CreatePlayerEventSystem(int index, InputActionAsset sourceAsset)
        {
            string esName = "EventSystem_Player" + (index + 1) + "Root";
            var esGo = new GameObject(esName);
            UnityEngine.Object.DontDestroyOnLoad(esGo);

            // MultiplayerEventSystem.playerRoot → 将该 EventSystem 的鼠标点击与导航限定在玩家区域内
            var mpEventSystem = esGo.AddComponent<MultiplayerEventSystem>();
            mpEventSystem.playerRoot = _multiplayerRoots[index];

            var inputModule = esGo.AddComponent<InputSystemUIInputModule>();

            // 尝试由 EUInputController Generated 提供 Asset（InputControllerAsset 模式）
            InputActionAsset resolvedAsset = null;
            string resolvedMapName = null;
            OnResolveMultiplayerPlayerAsset(index, ref resolvedAsset, ref resolvedMapName);

            if (resolvedAsset != null)
            {
                // InputControllerAsset 模式：直接使用 PlayerInputController 自带的 Asset，
                // 设备隔离由 PlayerInputController.BindGamepad() 自动维护，EUUI 不克隆也不做设备管理。
                var activeMap = resolvedAsset.FindActionMap(resolvedMapName);
                inputModule.actionsAsset = resolvedAsset;
                if (activeMap != null)
                    BindInputModuleActions(inputModule, activeMap);

                _multiplayerPlayerAssets[index] = resolvedAsset;
            }
            else if (sourceAsset != null)
            {
                // MultiplayerUIEvent 模式：克隆 Asset，各玩家持有独立实例，使 Map 启用状态互不影响
                var playerAsset = UnityEngine.Object.Instantiate(sourceAsset);
                string mapName = MultiplayerMapNames[index];

                // 禁用所有 Map，仅启用本玩家的 Map → 确保只有本玩家的按键路径会触发 Action
                foreach (var map in playerAsset.actionMaps)
                    map.Disable();
                var activeMap = playerAsset.FindActionMap(mapName);
                activeMap?.Enable();

                // 将克隆 Asset 的 Action 槽手动绑定到 InputSystemUIInputModule
                inputModule.actionsAsset = playerAsset;
                if (activeMap != null)
                    BindInputModuleActions(inputModule, activeMap);

                _multiplayerPlayerAssets[index] = playerAsset;
            }

            _multiplayerEventSystems[index] = mpEventSystem;

            // 初始禁用：仅在 EnterMultiplayerMode 时才激活，避免与全局 EventSystem 竞争
            esGo.SetActive(false);
        }

        /// <summary>
        /// 将 actionMap 中的标准 UI Action 绑定到 InputSystemUIInputModule 的对应槽位。
        /// </summary>
        private static void BindInputModuleActions(InputSystemUIInputModule inputModule, InputActionMap map)
        {
            TryBind(map, "Navigate",                 a => inputModule.move = a);
            TryBind(map, "Submit",                   a => inputModule.submit = a);
            TryBind(map, "Cancel",                   a => inputModule.cancel = a);
            TryBind(map, "Point",                    a => inputModule.point = a);
            TryBind(map, "Click",                    a => inputModule.leftClick = a);
            TryBind(map, "MiddleClick",              a => inputModule.middleClick = a);
            TryBind(map, "RightClick",               a => inputModule.rightClick = a);
            TryBind(map, "ScrollWheel",              a => inputModule.scrollWheel = a);
            TryBind(map, "TrackedDevicePosition",    a => inputModule.trackedDevicePosition = a);
            TryBind(map, "TrackedDeviceOrientation", a => inputModule.trackedDeviceOrientation = a);
        }

        private static void TryBind(InputActionMap map, string actionName, System.Action<InputActionReference> setter)
        {
            var action = map.FindAction(actionName);
            if (action != null)
                setter(InputActionReference.Create(action));
        }

        private static InputActionAsset GetValidPlayerAsset(int index)
        {
            if (_multiplayerPlayerAssets == null || index < 0 || index >= _multiplayerPlayerAssets.Length)
                return null;
            return _multiplayerPlayerAssets[index];
        }

        #region 多人模式切换

        /// <summary>
        /// 进入多人模式：禁用全局单人 EventSystem，激活多人 EventSystem，切换屏幕布局。
        /// 若已处于多人模式则只更新布局，不重复切换 EventSystem。
        /// 需在 EUUIKit.Initialize() 之后调用。
        /// </summary>
        /// <param name="playerCount">参与玩家数量（1～4）</param>
        /// <param name="layout">屏幕分割方式（Linear 条状 / Grid 田字）</param>
        /// <param name="axis">Linear 时的分割轴向（默认 X 横向）</param>
        public static void EnterMultiplayerMode(int playerCount, MultiplayerLayoutMode layout, MultiplayerLayoutAxis axis = MultiplayerLayoutAxis.X)
        {
            // 确保基础设施存在，并更新布局（SetMultiplayerLayout 在 InitMultiplayerEventSystem 内调用）
            InitMultiplayerEventSystem(playerCount, layout, axis);

            if (_isMultiplayer)
            {
                // 已在多人模式，只切换布局（上方已调用），无需重复开关 EventSystem
                Debug.Log($"[EUUIKit] 多人模式布局已更新：{playerCount} 人，{layout}");
                return;
            }

            // 禁用全局单人 EventSystem
            if (_globalEventSystemGO != null)
                _globalEventSystemGO.SetActive(false);

            // 激活 4 个 MultiplayerEventSystem
            if (_multiplayerEventSystems != null)
                foreach (var es in _multiplayerEventSystems)
                    if (es != null) es.gameObject.SetActive(true);

            _isMultiplayer = true;
            Debug.Log($"[EUUIKit] 进入多人模式：{playerCount} 人，布局={layout}");
        }

        /// <summary>
        /// 退出多人模式：关闭所有玩家面板，禁用多人 EventSystem，恢复全局单人 EventSystem。
        /// </summary>
        public static void ExitMultiplayerMode()
        {
            if (!_isMultiplayer) return;

            // 关闭所有玩家的面板并清空导航栈
            if (_playerActivePanels != null)
                for (int i = 0; i < MultiplayerSlotCountFixed; i++)
                    CloseAllForPlayer(i);

            // 禁用 4 个 MultiplayerEventSystem
            if (_multiplayerEventSystems != null)
                foreach (var es in _multiplayerEventSystems)
                    if (es != null) es.gameObject.SetActive(false);

            // 恢复全局单人 EventSystem
            if (_globalEventSystemGO != null)
                _globalEventSystemGO.SetActive(true);

            _isMultiplayer = false;
            Debug.Log("[EUUIKit] 已退出多人模式，全局 EventSystem 已恢复");
        }

        #endregion

        #region Per-Player 面板管理

        // ── 内部辅助 ──────────────────────────────────────────

        /// <summary>
        /// 检查 playerIndex 是否合法且多人系统已初始化，失败时打印错误并返回 false。
        /// </summary>
        private static bool ValidatePlayerIndex(int playerIndex, string callerName = "")
        {
            if (!_multiplayerInitialized || _playerLayers == null)
            {
                Debug.LogError($"[EUUIKit.Multiplayer]{callerName} 请先调用 InitMultiplayerEventSystem()");
                return false;
            }
            if (playerIndex < 0 || playerIndex >= MultiplayerSlotCountFixed)
            {
                Debug.LogError($"[EUUIKit.Multiplayer]{callerName} playerIndex={playerIndex} 超出范围 0～{MultiplayerSlotCountFixed - 1}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 获取指定玩家指定层级的 RectTransform（用作面板的父节点）。
        /// </summary>
        public static RectTransform GetPlayerLayer(int playerIndex, EUUILayerEnum layer)
        {
            if (!ValidatePlayerIndex(playerIndex, " GetPlayerLayer")) return null;
            return _playerLayers[playerIndex].TryGetValue(layer, out var rt) ? rt : null;
        }

        // ── 从导航栈中移除（同全局实现，针对 per-player 栈）─────
        private static void RemoveFromPlayerPanelStack(int playerIndex, string panelName)
        {
            var stack = _playerPanelStacks[playerIndex];
            if (!stack.Contains(panelName)) return;

            var tempList = stack.ToList();
            tempList.Remove(panelName);
            stack.Clear();
            tempList.Reverse();
            foreach (var name in tempList)
                stack.Push(name);
        }

        // ── 打开 ──────────────────────────────────────────────

        /// <summary>
        /// 为指定玩家打开面板（异步）。面板挂载到玩家专属层级下，不记录导航历史。
        /// 适用于弹窗、HUD 等；主流程页面请使用 NavigateForPlayerAsync。
        /// </summary>
        /// <param name="playerIndex">玩家槽位 0～3</param>
        /// <param name="data">面板初始化数据（可选）</param>
        public static async UniTask<T> OpenForPlayerAsync<T>(int playerIndex, IEUUIPanelData data = null)
            where T : EUUIPanelBase<T>
        {
            if (!ValidatePlayerIndex(playerIndex, " OpenForPlayerAsync")) return null;

            string panelName = typeof(T).Name;
            var activePanels = _playerActivePanels[playerIndex];
            var opening      = _playerOpening[playerIndex];

            // 已打开则直接显示并返回
            if (activePanels.TryGetValue(panelName, out var existingPanel))
            {
                existingPanel.Show();
                return existingPanel as T;
            }

            // 防止重复并发打开
            if (opening.Contains(panelName))
            {
                Debug.LogWarning($"[EUUIKit.Multiplayer] P{playerIndex + 1} 面板 {panelName} 正在打开中，请勿重复调用");
                return null;
            }

            opening.Add(panelName);
            try
            {
                GameObject prefabAsset = await LoadPanelPrefabAsync<T>();
                if (prefabAsset == null)
                {
                    Debug.LogError($"[EUUIKit.Multiplayer] P{playerIndex + 1} 加载面板 Prefab 失败: {panelName}");
                    return null;
                }

                GameObject panelGO = UnityEngine.Object.Instantiate(prefabAsset);
                var panel = panelGO.GetComponent<T>();
                if (panel == null)
                {
                    Debug.LogError($"[EUUIKit.Multiplayer] P{playerIndex + 1} Prefab 上未找到组件: {panelName}");
                    UnityEngine.Object.Destroy(panelGO);
                    return null;
                }

                // 挂载到玩家专属层级
                var layerRT = GetPlayerLayer(playerIndex, panel.DefaultLayer);
                if (layerRT == null)
                {
                    Debug.LogError($"[EUUIKit.Multiplayer] P{playerIndex + 1} 层级 {panel.DefaultLayer} 未找到，请检查 InitPlayerLayers");
                    UnityEngine.Object.Destroy(panelGO);
                    return null;
                }

                panelGO.transform.SetParent(layerRT, false);

                // 强制根 RectTransform 全拉伸填充玩家分区层级：
                // 确保无论 Prefab 原始布局如何，面板都自动适配 PlayerXRoot 坐标系。
                // 面板内部子节点的尺寸与锚点不受影响，仍由 Prefab 设计决定。
                var panelRT = panelGO.GetComponent<RectTransform>();
                if (panelRT != null)
                {
                    panelRT.anchorMin        = Vector2.zero;
                    panelRT.anchorMax        = Vector2.one;
                    panelRT.sizeDelta        = Vector2.zero;
                    panelRT.anchoredPosition = Vector2.zero;
                }

                // 记录归属玩家，供面板内部（焦点、数据等）直接使用
                panel.OwnerPlayerIndex = playerIndex;

                panelGO.SetActive(true);

                activePanels[panelName] = panel;
                await panel.OpenAsync(data);
                return panel;
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUUIKit.Multiplayer] P{playerIndex + 1} 打开面板 {panelName} 失败: {e.Message}\n{e.StackTrace}");
                return null;
            }
            finally
            {
                opening.Remove(panelName);
            }
        }

        // ── 关闭 ──────────────────────────────────────────────

        /// <summary>
        /// 关闭指定玩家的某个面板（若在导航栈中一并移除）。
        /// </summary>
        /// <param name="playerIndex">玩家槽位 0～3</param>
        public static void CloseForPlayer<T>(int playerIndex) where T : EUUIPanelBase<T>
        {
            if (!ValidatePlayerIndex(playerIndex, " CloseForPlayer")) return;

            string panelName    = typeof(T).Name;
            var activePanels    = _playerActivePanels[playerIndex];

            if (activePanels.TryGetValue(panelName, out var panel))
            {
                activePanels.Remove(panelName);
                RemoveFromPlayerPanelStack(playerIndex, panelName);
                panel.Close();
            }
        }

        /// <summary>
        /// 关闭指定玩家的所有面板并清空导航栈。
        /// </summary>
        /// <param name="playerIndex">玩家槽位 0～3</param>
        public static void CloseAllForPlayer(int playerIndex)
        {
            if (!ValidatePlayerIndex(playerIndex, " CloseAllForPlayer")) return;

            var activePanels = _playerActivePanels[playerIndex];
            foreach (var panel in activePanels.Values)
            {
                panel.Hide();
                if (panel.EnableClose) panel.Close();
            }
            activePanels.Clear();
            _playerPanelStacks[playerIndex].Clear();
        }

        // ── 导航（带历史栈）──────────────────────────────────

        /// <summary>
        /// 导航到指定玩家的目标面板（隐藏当前栈顶并记录历史）。
        /// 适用于主流程页面，支持 BackForPlayerAsync 返回上一页。
        /// </summary>
        /// <param name="playerIndex">玩家槽位 0～3</param>
        /// <param name="data">面板初始化数据（可选）</param>
        public static async UniTask NavigateForPlayerAsync<T>(int playerIndex, IEUUIPanelData data = null)
            where T : EUUIPanelBase<T>
        {
            if (!ValidatePlayerIndex(playerIndex, " NavigateForPlayerAsync")) return;

            string panelName    = typeof(T).Name;
            var activePanels    = _playerActivePanels[playerIndex];
            var stack           = _playerPanelStacks[playerIndex];

            // 已在栈顶：无操作
            if (stack.Count > 0 && stack.Peek() == panelName) return;

            // 已在栈中但非栈顶 → 回退到该面板
            if (stack.Contains(panelName))
            {
                await BackForPlayerToAsync<T>(playerIndex);
                return;
            }

            // 隐藏当前栈顶
            if (stack.Count > 0)
            {
                if (activePanels.TryGetValue(stack.Peek(), out var topPanel))
                    topPanel.Hide();
            }

            await OpenForPlayerAsync<T>(playerIndex, data);
            stack.Push(panelName);
        }

        /// <summary>
        /// 返回指定玩家的上一个面板（关闭/隐藏当前栈顶，显示新栈顶）。
        /// </summary>
        /// <param name="playerIndex">玩家槽位 0～3</param>
        /// <param name="showNext">是否显示新的栈顶面板（连续回退时可传 false）</param>
        public static async UniTask BackForPlayerAsync(int playerIndex, bool showNext = true)
        {
            if (!ValidatePlayerIndex(playerIndex, " BackForPlayerAsync")) return;

            var activePanels = _playerActivePanels[playerIndex];
            var stack        = _playerPanelStacks[playerIndex];

            if (stack.Count == 0)
            {
                Debug.LogWarning($"[EUUIKit.Multiplayer] P{playerIndex + 1} 面板历史栈为空，无法返回");
                return;
            }

            var topName = stack.Pop();
            if (activePanels.TryGetValue(topName, out var topPanel))
            {
                topPanel.Hide();
                if (topPanel.EnableClose)
                {
                    activePanels.Remove(topName);
                    topPanel.Close();
                }
            }

            await UniTask.Yield();

            if (showNext && stack.Count > 0)
            {
                if (activePanels.TryGetValue(stack.Peek(), out var newTop))
                    newTop.Show();
            }
        }

        /// <summary>
        /// 回退到指定玩家的目标面板（关闭中间所有面板）。
        /// </summary>
        /// <param name="playerIndex">玩家槽位 0～3</param>
        public static async UniTask BackForPlayerToAsync<T>(int playerIndex)
            where T : EUUIPanelBase<T>
        {
            if (!ValidatePlayerIndex(playerIndex, " BackForPlayerToAsync")) return;

            string targetName   = typeof(T).Name;
            var activePanels    = _playerActivePanels[playerIndex];
            var stack           = _playerPanelStacks[playerIndex];

            while (stack.Count > 0 && stack.Peek() != targetName)
                await BackForPlayerAsync(playerIndex, showNext: false);

            if (stack.Count > 0 && activePanels.TryGetValue(targetName, out var targetPanel))
                targetPanel.Show();
        }

        /// <summary>
        /// 清空指定玩家的导航历史（关闭所有栈内面板）。
        /// </summary>
        /// <param name="playerIndex">玩家槽位 0～3</param>
        public static async UniTask ClearPlayerHistoryAsync(int playerIndex)
        {
            if (!ValidatePlayerIndex(playerIndex, " ClearPlayerHistoryAsync")) return;

            while (_playerPanelStacks[playerIndex].Count > 0)
                await BackForPlayerAsync(playerIndex, showNext: false);
        }

        // ── 查询 ──────────────────────────────────────────────

        /// <summary>
        /// 获取指定玩家当前激活的面板实例（未打开时返回 null）。
        /// </summary>
        /// <param name="playerIndex">玩家槽位 0～3</param>
        public static T GetPlayerPanel<T>(int playerIndex) where T : EUUIPanelBase<T>
        {
            if (!ValidatePlayerIndex(playerIndex, " GetPlayerPanel")) return null;
            string panelName = typeof(T).Name;
            return _playerActivePanels[playerIndex].TryGetValue(panelName, out var p) ? p as T : null;
        }

        /// <summary>
        /// 检查指定玩家某面板是否已打开。
        /// </summary>
        /// <param name="playerIndex">玩家槽位 0～3</param>
        public static bool IsPanelOpenForPlayer<T>(int playerIndex) where T : EUUIPanelBase<T>
        {
            if (!ValidatePlayerIndex(playerIndex, " IsPanelOpenForPlayer")) return false;
            return _playerActivePanels[playerIndex].ContainsKey(typeof(T).Name);
        }

        /// <summary>
        /// 获取指定玩家的导航栈深度。
        /// </summary>
        /// <param name="playerIndex">玩家槽位 0～3</param>
        public static int GetPlayerHistoryCount(int playerIndex)
        {
            if (!ValidatePlayerIndex(playerIndex, " GetPlayerHistoryCount")) return 0;
            return _playerPanelStacks[playerIndex].Count;
        }

        /// <summary>
        /// 获取指定玩家当前栈顶面板名称（栈为空时返回 null）。
        /// </summary>
        /// <param name="playerIndex">玩家槽位 0～3</param>
        public static string GetPlayerCurrentPanelName(int playerIndex)
        {
            if (!ValidatePlayerIndex(playerIndex, " GetPlayerCurrentPanelName")) return null;
            var stack = _playerPanelStacks[playerIndex];
            return stack.Count > 0 ? stack.Peek() : null;
        }

        #endregion
    }
}
