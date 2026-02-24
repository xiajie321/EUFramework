using System;

namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// 标记一个无参静态方法为 Hotbox 功能条目。
    /// 被标记的方法将自动出现在 EUUI 功能编排面板的条目发现列表中，
    /// 用户可将其拖入区域，在 Scene 视图按住 Space 键时快速调用。
    /// </summary>
    /// <example>
    /// [EUHotboxEntry("创建UI场景", "UI制作", "创建标准 UI 场景结构")]
    /// public static void ShowCreateSceneWindow() { ... }
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class EUHotboxEntryAttribute : Attribute
    {
        /// <summary>在面板和弹出层中显示的标签</summary>
        public string Label { get; }

        /// <summary>左侧条目列表的分组名（可选，默认"通用"）</summary>
        public string Group { get; }

        /// <summary>鼠标悬停时显示的描述（可选）</summary>
        public string Tooltip { get; }

        public EUHotboxEntryAttribute(string label, string group = "通用", string tooltip = "")
        {
            Label   = label;
            Group   = string.IsNullOrEmpty(group) ? "通用" : group;
            Tooltip = tooltip;
        }
    }
}
