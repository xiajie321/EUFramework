using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// EUUIKit 导航分部类（兼容新旧输入系统）
    /// 由 EUUIKit.Initialize() 自动初始化，无需手动调用
    /// </summary>
    public static partial class EUUIKit
    {

        // ── 由 EUUIKit.Initialize() 调用 ──────────────────
        private static void InitNavigation()
        {

        }

        // ── 公开 API ──────────────────────────────────────

        /// <summary>
        /// 设置当前焦点（由 EUUIPanelBase.Show 自动调用，通常无需手动调用）
        /// </summary>
        public static void SetDefaultSelection(Selectable selectable)
        {
            if (selectable == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }

        /// <summary>
        /// 清除当前焦点
        /// </summary>
        public static void ClearSelection()
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
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
            if      (direction.y >  0.5f) next = cur.FindSelectableOnUp();
            else if (direction.y < -0.5f) next = cur.FindSelectableOnDown();
            else if (direction.x >  0.5f) next = cur.FindSelectableOnRight();
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
