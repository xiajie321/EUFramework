using UnityEngine;

namespace Framework
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
        [Tooltip("UI 命名空间")]
        public string namespaceName = "Game.UI";

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
