using System;
using Com.ForbiddenByte.OSA.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// OSA 列表导航桥接器。
    /// 通过持有 EventSystem 引用，将自身设为 selected 对象，
    /// 由 InputSystemUIInputModule 驱动 IMoveHandler / ISubmitHandler / ICancelHandler，
    /// 不直接订阅 InputAction，完全兼容单人 / 多人 EventSystem。
    ///
    /// 使用方式（在面板 OnOpen 里）：
    ///   1. 调用 SetEventSystem() 注入对应玩家的 EventSystem。
    ///   2. 订阅 OnItemFocused / OnItemSubmitted / OnExitList 事件。
    ///   3. 入口按钮 onClick 里调用 EnterList()。
    /// </summary>
    public class EUOSAInputBridge : MonoBehaviour, IMoveHandler, ISubmitHandler, ICancelHandler
    {
        [Header("导航参数")]
        [Tooltip("是否循环滚动到边界")]
        [SerializeField] bool _loopAtExtremity  = false;
        [Tooltip("退出列表时缓存当前 index，下次 EnterList() 时从该位置恢复而非回到第 0 项")]
        [SerializeField] bool _rememberLastIndex = true;

        // ── 回调 ────────────────────────────────────────────────────────────

        /// <summary>当前高亮 item index 变化时触发（驱动高亮表现）。支持匿名方法，OnClose 里置 null 清理。</summary>
        public Action<int> OnItemFocused;

        /// <summary>确认选中当前 item 时触发（处理业务数据）。支持匿名方法，OnClose 里置 null 清理。</summary>
        public Action<int> OnItemSubmitted;

        /// <summary>退出列表时触发（将焦点还给面板按钮）。支持匿名方法，OnClose 里置 null 清理。</summary>
        public Action OnExitList;

        // ── 运行时状态 ──────────────────────────────────────────────────────

        IOSA        _osa;
        EventSystem _eventSystem;
        int         _currentIndex      = -1;
        int         _lastIndex         =  0;
        bool        _isActive;
        bool        _activeLoopAtExtremity; // 本次 EnterList 生效的循环设置

        // ── 公共注入 ────────────────────────────────────────────────────────

        /// <summary>
        /// 由 FrameworkListAdapter / FrameworkGridAdapter 的 InputBridge getter 调用，
        /// 直接传入 this 避免通过接口类型 GetComponent 在泛型继承链上失效的问题。
        /// </summary>
        public void InjectOSA(IOSA osa)
        {
            _osa = osa;
        }

        // ── 生命周期 ────────────────────────────────────────────────────────

        void Awake()
        {
            // 手动挂载时（未经 InputBridge getter 注入）在此兜底查找
            if (_osa == null)
                _osa = GetComponent(typeof(IOSA)) as IOSA;
        }

        void OnDisable()
        {
            if (_isActive) ForceExit();
        }

        // ── 公共 API ────────────────────────────────────────────────────────

        /// <summary>
        /// 注入 EventSystem。单人传 EventSystem.current，多人传玩家专属 EventSystem。
        /// 每次面板 OnOpen 时调用。
        /// </summary>
        public void SetEventSystem(EventSystem es)
        {
            _eventSystem = es;
        }

        /// <summary>
        /// 进入列表。
        /// 省略参数时使用 Inspector 配置的默认值；
        /// 传入非 null 值时覆盖对应默认值。
        /// </summary>
        /// <param name="restoreLastIndex">null = 用 Inspector 默认；true/false = 显式控制是否恢复上次位置</param>
        /// <param name="loopAtExtremity">null = 用 Inspector 默认；true/false = 显式控制到边界时是否循环</param>
        public void EnterList(bool? restoreLastIndex = null, bool? loopAtExtremity = null)
        {
            EnterListInternal(
                restoreLastIndex ?? _rememberLastIndex,
                loopAtExtremity  ?? _loopAtExtremity
            );
        }

        /// <summary>
        /// 手动重置缓存的 index（数据刷新后调用，避免恢复到越界位置）。
        /// </summary>
        public void ResetLastIndex() => _lastIndex = 0;

        /// <summary>退出列表，清除 selected，触发 OnExitList。</summary>
        public void ExitList()
        {
            if (!_isActive) return;
            ForceExit();
            OnExitList?.Invoke();
        }

        void EnterListInternal(bool restoreLastIndex, bool loopAtExtremity)
        {
            if (_eventSystem == null)
            {
                Debug.LogWarning("[EUOSAInputBridge] 请先调用 SetEventSystem()");
                return;
            }
            if (_osa == null)
            {
                Debug.LogWarning("[EUOSAInputBridge] OSA 组件未找到，请确认 Adapter 与 Bridge 在同一 GameObject 上");
                return;
            }
            if (!_osa.IsInitialized || _osa.GetItemsCount() == 0) return;

            _activeLoopAtExtremity = loopAtExtremity;

            int total      = _osa.GetItemsCount();
            int startIndex = (restoreLastIndex && _lastIndex > 0)
                ? Mathf.Clamp(_lastIndex, 0, total - 1)
                : 0;

            _isActive     = true;
            _currentIndex = startIndex;
            _osa.BringToView(startIndex);
            OnItemFocused?.Invoke(startIndex);
            _eventSystem.SetSelectedGameObject(gameObject);
        }

        // ── UI 事件接口（由 InputSystemUIInputModule → EventSystem 派发）──────

        public void OnMove(AxisEventData eventData)
        {
            if (!_isActive) return;

            int delta = 0;
            if (_osa.IsVertical)
            {
                if (eventData.moveDir == MoveDirection.Up)   delta = -1;
                if (eventData.moveDir == MoveDirection.Down) delta =  1;
            }
            else
            {
                if (eventData.moveDir == MoveDirection.Left)  delta = -1;
                if (eventData.moveDir == MoveDirection.Right) delta =  1;
            }

            if (delta == 0) return; // 横向/纵向不属于列表滚动方向，不消费

            int next  = _currentIndex + delta;
            int total = _osa.GetItemsCount();

            if (next < 0 || next >= total)
            {
                if (_activeLoopAtExtremity)
                    next = next < 0 ? total - 1 : 0;
                else
                {
                    ExitList();
                    return;
                }
            }

            _currentIndex = next;
            _osa.BringToView(_currentIndex);
            _osa.BringToView(_currentIndex); // 可变大小 item 需调用两次
            OnItemFocused?.Invoke(_currentIndex);

            eventData.Use();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!_isActive) return;
            OnItemSubmitted?.Invoke(_currentIndex);
        }

        public void OnCancel(BaseEventData eventData)
        {
            if (!_isActive) return;
            ExitList();
        }

        // ── 内部 ────────────────────────────────────────────────────────────

        void ForceExit()
        {
            _lastIndex             = _currentIndex;    // 缓存当前位置供下次 EnterList 恢复
            _activeLoopAtExtremity = _loopAtExtremity; // 还原为 Inspector 默认值
            _isActive              = false;
            _eventSystem?.SetSelectedGameObject(null);
        }
    }
}
