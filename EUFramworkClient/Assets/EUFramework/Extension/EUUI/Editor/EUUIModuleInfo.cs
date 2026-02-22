#if UNITY_EDITOR
using System.Collections.Generic;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// EUUI 扩展模块描述
    /// 一个模块可包含多个 .sbn 模板（templateIds），安装时同时生成所有模板并写入 asmdef 引用。
    /// </summary>
    public class EUUIModuleInfo
    {
        /// <summary>模块唯一标识（英文，无空格）</summary>
        public string Id;

        /// <summary>模块显示名称</summary>
        public string DisplayName;

        /// <summary>模块描述</summary>
        public string Description;

        /// <summary>
        /// 对应的 .sbn 模板注册表 ID 列表
        /// 例如 ["EUUIKit.EURes", "EUUIPanelBase.EURes"]
        /// </summary>
        public string[] TemplateIds;

        /// <summary>
        /// 需要写入 EUUI.asmdef references 的程序集名称
        /// 例如 ["EURes", "YooAsset"]
        /// </summary>
        public string[] RuntimeAssemblies;

        /// <summary>
        /// 需要写入 EUUI.Editor.asmdef references 的程序集名称
        /// 例如 ["EURes", "YooAsset", "YooAsset.Editor"]
        /// </summary>
        public string[] EditorAssemblies;

        /// <summary>
        /// 安装该模块时额外需要检测的程序集（用于判断模块是否可安装）。
        /// 留空时取 RuntimeAssemblies 第一项。
        /// </summary>
        public string[] RequiredAssemblies;
    }

    /// <summary>
    /// EUUI 内置模块注册表
    /// </summary>
    public static class EUUIBuiltinModules
    {
        public static readonly EUUIModuleInfo EURes = new EUUIModuleInfo
        {
            Id                 = "EURes",
            DisplayName        = "EURes 资源加载",
            Description        = "基于 YooAsset 的资源加载扩展。安装后 EUUIKit 和 EUUIPanelBase 均获得同步/异步加载图集、Prefab 的能力。",
            TemplateIds        = new[] { "EUUIKit.EURes", "EUUIPanelBase.EURes" },
            RuntimeAssemblies  = new[] { "EURes", "YooAsset" },
            EditorAssemblies   = new[] { "EURes", "YooAsset", "YooAsset.Editor" },
            RequiredAssemblies = new[] { "EURes", "YooAsset" }
        };

        /// <summary>返回所有内置模块列表</summary>
        public static IReadOnlyList<EUUIModuleInfo> GetAll() => new[] { EURes };
    }
}
#endif
