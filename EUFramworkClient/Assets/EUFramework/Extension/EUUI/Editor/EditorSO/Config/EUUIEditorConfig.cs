using UnityEngine;
using EUFramework.Extension.EUUI;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// EUUI 编辑器配置（ScriptableObject）
    /// 管理场景制作、分辨率及 UI 资源路径等美术/资源制作相关设置。
    /// 代码生成与模板扩展配置请参见 EUUITemplateConfig。
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
