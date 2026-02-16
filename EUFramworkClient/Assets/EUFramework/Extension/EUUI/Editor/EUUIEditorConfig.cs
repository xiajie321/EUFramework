using UnityEngine;
using EUFramework.Extension.EUUI;

namespace EUFramework.Extension.EUUI.Editor
{
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

        [Header("扩展模块")]
        [Tooltip("启用 EURes 资源加载扩展（生成 EUUIPanelBase 和 EUUIKit 的 EURes 扩展方法）")]
        public bool enableEUResExtension = true;

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
