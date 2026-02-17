using System;
using System.Collections.Generic;
using UnityEngine;
using EUFramework.Extension.EUUI;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// 资源加载器类型
    /// </summary>
    public enum ResourceLoaderType
    {
        [Tooltip("使用 EURes（框架默认，依赖 YooAsset）")]
        EURes = 0,
        
        [Tooltip("使用自定义模板")]
        Custom = 1,
        
        [Tooltip("不生成（用户完全手动实现分部类）")]
        None = 2
    }
    
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
        
        [Tooltip("导出脚本名称（ExportsCS 目录下的导出脚本类名，如：EUUIStaticExporter）")]
        public string exporterScript;
        
        [Tooltip("是否在「生成绑定模板」面板中管理（勾选后可在该面板中单独创建/删除）")]
        public bool enabled = false;
    }
    
    /// <summary>
    /// EUUI 核心模板覆盖配置
    /// </summary>
    [Serializable]
    public class EUUITemplateOverride
    {
        [Tooltip("要覆盖的模板 ID（如: PanelGenerated, MVCArchitecture）")]
        public string templateId;
        
        [Tooltip("自定义模板路径（绝对路径，如: Assets/MyGame/Templates/MyPanel.sbn）")]
        public string customPath;
        
        [Tooltip("是否启用此覆盖")]
        public bool enabled = true;
    }
    
    /// <summary>
    /// EUUI 编辑器配置（ScriptableObject）
    /// 与 EURes 连用时的默认路径基于 Assets/EUResources（Builtin/Remote/Excluded）
    /// </summary>
    [CreateAssetMenu(fileName = "EUUIEditorConfig", menuName = "EUFramework/EUUI/Editor Config", order = 0)]
    public class EUUIEditorConfig : ScriptableObject
    {
        [Header("分辨率")]
        [Tooltip("参考分辨率")]
        public Vector2 referenceResolution = new Vector2(1920, 1080);

        [Tooltip("屏幕匹配模式：0=以宽为准，1=以高为准，0.5=宽高折中")]
        [Range(0f, 1f)]
        public float matchWidthOrHeight = 0.5f;

        [Tooltip("参考像素每单位（与 Sprite 的 Pixels Per Unit 一致）")]
        public float referencePixelsPerUnit = 100f;
        
        [Header("场景层级名称")]
        [Tooltip("UI 根节点名称（导出 Prefab 的根）")]
        public string exportRootName = "UIRoot";

        [Tooltip("底层排除节点名称（不参与导出）")]
        public string notExportBottomName = "Excluded_Bottom";

        [Tooltip("顶层排除节点名称（不参与导出）")]
        public string notExportTopName = "Excluded_Top";

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

        [Header("核心模板管理（自动检测）")]
        [Space(5)]
        [Tooltip("覆盖框架默认模板（可选）\n" +
                 "• templateId: 要覆盖的模板 ID（PanelGenerated/MVCArchitecture 等）\n" +
                 "• customPath: 自定义模板的绝对路径\n" +
                 "• enabled: 是否启用此覆盖\n" +
                 "留空则使用框架自动检测的默认模板")]
        public List<EUUITemplateOverride> templateOverrides = new List<EUUITemplateOverride>();

        [Header("资源加载扩展（核心）")]
        [Tooltip("资源加载器类型：\n" +
                 "• EURes - 框架默认（依赖 YooAsset）\n" +
                 "• Custom - 使用自定义模板\n" +
                 "• None - 不生成（手动实现分部类）")]
        public ResourceLoaderType resourceLoader = ResourceLoaderType.EURes;

        [Tooltip("自定义资源加载模板路径（resourceLoader = Custom 时使用）\n" +
                 "示例：Assets/Script/Game/Templates/MyLoader.sbn\n" +
                 "留空则使用框架提供的 Custom 模板")]
        public string customLoaderTemplate = "";

        [Header("附加扩展模块（可选）")]
        [Tooltip("自动发现模式：扫描指定目录下的所有 .sbn 模板")]
        public bool autoDiscoverExtensions = false;

        [Tooltip("扩展模板目录（autoDiscover = true 时使用）")]
        public string extensionsDirectory = "Assets/Script/Game/UI/Extensions";

        [Tooltip("手动配置的扩展列表（精确控制）")]
        public List<EUUIAdditionalExtension> manualExtensions = new List<EUUIAdditionalExtension>();

        [Header("UI 资源路径")]
        [Tooltip("UI 源场景保存路径，不参与资源导出")]
        public string uiSceneSavePath = "Assets/EUResources/Excluded/CreateUIScenes";

        [Tooltip("首包 UI Prefab 路径")]
        public string uiPrefabBuiltinPath = "Assets/EUResources/Builtin/UI/Prefabs";

        [Tooltip("远程 UI Prefab 路径")]
        public string uiPrefabRemotePath = "Assets/EUResources/Remote/UI/Prefabs";

        [Tooltip("首包图集路径")]
        public string atlasBuiltinPath = "Assets/EUResources/Builtin/UI/Atlases";

        [Tooltip("远程图集路径")]
        public string atlasRemotePath = "Assets/EUResources/Remote/UI/Atlases";

        [Header("代码生成路径（自动绑定时使用）")]
        [Tooltip("绑定代码（Generated.cs）输出目录，与 Luban/服务器等生成代码并列于 Generate/UI")]
        public string uiBindScriptsPath = "Assets/Script/Generate/UI";

        [Tooltip("业务逻辑代码（.cs）输出目录，按 PackageName 分子目录")]
        public string uiLogicScriptsPath = "Assets/Script/Game/UI";

        public string GetUIPrefabDir(EUUIPackageType type)
        {
            return type switch
            {
                EUUIPackageType.Builtin => uiPrefabBuiltinPath,
                EUUIPackageType.Remote => uiPrefabRemotePath,
                _ => uiPrefabRemotePath
            };
        }

        public string GetAtlasPath(bool isBuiltin)
        {
            return isBuiltin ? atlasBuiltinPath : atlasRemotePath;
        }
    }
}
