#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// 扩展目标类型
    /// </summary>
    public enum ExtensionTarget
    {
        [Tooltip("EUUIPanelBase 扩展")]
        EUUIPanelBase,

        [Tooltip("EUUIKit 扩展")]
        EUUIKit
    }

    /// <summary>
    /// EUUI 附加扩展配置
    /// </summary>
    [Serializable]
    public class EUUIAdditionalExtension
    {
        [Tooltip("模板路径（.sbn 文件路径，相对于项目根目录）")]
        public string templatePath;

        [Tooltip("是否在「生成绑定模板」面板中管理（勾选后可在该面板中单独创建/删除）")]
        public bool enabled = false;
    }

    /// <summary>
    /// EUUI 模板与代码生成配置（ScriptableObject）
    /// 管理命名空间、架构集成、资源加载扩展和附加扩展模块等代码生成相关设置。
    /// 场景制作与 UI 资源路径配置请参见 EUUIEditorConfig。
    /// </summary>
    [CreateAssetMenu(fileName = "EUUITemplateConfig", menuName = "EUFramework/EUUI/Template Config", order = 1)]
    public class EUUITemplateConfig : ScriptableObject
    {
        [Header("命名空间")]
        [Tooltip("UI 命名空间（生成代码的 namespace，与业务程序集一致）")]
        public string namespaceName = "Game.UI";

        [Header("代码生成-架构集成")]
        [Tooltip("是否使用 MVC 架构（启用后生成代码会实现 IController）")]
        public bool useArchitecture = true;

        [Tooltip("架构名称（如 GameApp）：\n" +
                 "- 留空：使用 CoreExtension 全局静态架构（框架内部方式）\n" +
                 "- 填写：生成 GetArchitecture() 返回指定架构（QF 重构方式）")]
        public string architectureName = "";

        [Tooltip("架构命名空间（如 Game.Architecture）：\n" +
                 "- 仅当填写了 architectureName 时需要填写\n" +
                 "- 用于生成正确的 using 语句")]
        public string architectureNamespace = "";

        [Header("代码生成路径（自动绑定时使用）")]
        [Tooltip("绑定代码（Generated.cs）输出目录，与 Luban/服务器等生成代码并列于 Generate/UI")]
        public string uiBindScriptsPath = "Assets/Script/Generate/UI";

        [Tooltip("业务逻辑代码（.cs）输出目录，按 PackageName 分子目录")]
        public string uiLogicScriptsPath = "Assets/Script/Game/UI";

        [Header("附加扩展模块")]
        [Tooltip("扩展模板启用状态列表（由「生成绑定模板」面板自动维护，无需手动编辑）")]
        public List<EUUIAdditionalExtension> manualExtensions = new List<EUUIAdditionalExtension>();
    }
}
#endif
