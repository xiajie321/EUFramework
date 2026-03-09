using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// EUUIKit 输入与导航分部类
    /// 负责统一处理 UI 焦点、方向导航与提交/返回行为（兼容新旧输入系统）
    /// 由 EUUIKit.Initialize() 自动初始化，无需手动调用
    /// </summary>
    public static partial class EUUIKit
    {
        /// <summary>
        /// 设置当前焦点（由 EUUIPanelBase.Show 自动调用，通常无需手动调用）。
        /// 多人模式下此方法无效（请使用 MultiplayerEventSystem 直接设置焦点）。
        /// </summary>
        public static void SetDefaultSelection(Selectable selectable)
        {
            if (_isMultiplayer) return;
            if (selectable == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }

        /// <summary>
        /// 清除当前焦点。
        /// 多人模式下此方法无效（避免错误地操作某个玩家的 MultiplayerEventSystem）。
        /// </summary>
        public static void ClearSelection()
        {
            if (_isMultiplayer) return;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        /// <summary>
        /// 为指定玩家设置焦点（多人模式专用）。
        /// </summary>
        /// <param name="playerIndex">玩家槽位 0～3</param>
        /// <param name="selectable">要聚焦的 Selectable，传 null 则清除焦点</param>
        public static void SetPlayerSelection(int playerIndex, Selectable selectable)
        {
            var es = GetMultiplayerEventSystem(playerIndex);
            if (es == null) return;
            es.SetSelectedGameObject(selectable != null ? selectable.gameObject : null);
        }

        /// <summary>
        /// 清除指定玩家的焦点（多人模式专用）。
        /// </summary>
        /// <param name="playerIndex">玩家槽位 0～3</param>
        public static void ClearPlayerSelection(int playerIndex)
        {
            SetPlayerSelection(playerIndex, null);
        }

        /// <summary>
        /// 程序化方向导航（方向键 / 摇杆，沿已配置的导航链移动）
        /// </summary>
        public static void Navigate(Vector2 direction)
        {
            if (EventSystem.current?.currentSelectedGameObject == null) return;

            var cur = EventSystem.current.currentSelectedGameObject
                                         .GetComponent<Selectable>();
            if (cur == null) return;

            Selectable next = null;
            if (direction.y > 0.5f) next = cur.FindSelectableOnUp();
            else if (direction.y < -0.5f) next = cur.FindSelectableOnDown();
            else if (direction.x > 0.5f) next = cur.FindSelectableOnRight();
            else if (direction.x < -0.5f) next = cur.FindSelectableOnLeft();

            if (next != null)
                EventSystem.current.SetSelectedGameObject(next.gameObject);
        }

        /// <summary>
        /// 触发确认或取消
        /// isSubmit=true  → 对当前选中元素执行 Submit 事件
        /// isSubmit=false → 触发 EUUIKit.BackAsync()（返回上一个面板）
        /// </summary>
        public static void SubmitOrCancel(bool isSubmit)
        {
            if (EventSystem.current == null) return;

            if (isSubmit)
            {
                var selected = EventSystem.current.currentSelectedGameObject;
                if (selected == null) return;
                ExecuteEvents.Execute(
                    selected,
                    new BaseEventData(EventSystem.current),
                    ExecuteEvents.submitHandler);
            }
            else
            {
                BackAsync().Forget();
            }
        }
    }
}
