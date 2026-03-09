using UnityEngine;

namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// 多人 UI 输入 Asset 来源模式
    /// </summary>
    public enum MultiplayerInputMode
    {
        /// <summary>
        /// 使用独立的 MultiplayerUIEvent.inputactions（4 个 Map，PlayerUI_1～4）。
        /// 支持键盘区域隔离（P1=WASD / P2=方向键），适合含键盘的多人场景。
        /// 设备管理由 EUUI 通过 AssignKeyboardPlayer / AssignGamepadPlayer 负责。
        /// </summary>
        MultiplayerUIEvent,

        /// <summary>
        /// 直接使用 EUInputController 每个 PlayerInputController 自带的 InputController Asset（UI Map）。
        /// 设备隔离由 PlayerInputController.BindGamepad() 自动维护，EUUI 不做额外设备管理。
        /// 适合纯手柄多人，或不需要键盘区域隔离的场景。
        /// </summary>
        InputControllerAsset,
    }

    /// <summary>
    /// EUUI 运行时配置（从 Resources 加载）
    /// </summary>
    [CreateAssetMenu(fileName = "EUUIKitConfig", menuName = "EUFramework/EUUI/Kit Config", order = 1)]
    public class EUUIKitConfig : ScriptableObject
    {
        [Header("分辨率配置")]
        [Tooltip("参考分辨率")]
        public Vector2 referenceResolution = new Vector2(1920, 1080);

        [Tooltip("屏幕匹配模式：0=以宽为准，1=以高为准，0.5=宽高折中")]
        [Range(0f, 1f)]
        public float matchWidthOrHeight = 0.5f;

        [Tooltip("参考像素每单位")]
        public float referencePixelsPerUnit = 100f;

        [Header("UI 资源路径")]
        [Tooltip("首包 UI Prefab 路径前缀")]
        public string builtinPrefabPath = "Assets/EUResources/Builtin/UI/Prefabs";

        [Tooltip("远程 UI Prefab 路径前缀")]
        public string remotePrefabPath = "Assets/EUResources/Remote/UI/Prefabs";

        [Tooltip("首包图集路径前缀")]
        public string builtinAtlasPath = "Assets/EUResources/Builtin/UI/Atlases";

        [Tooltip("远程图集路径前缀")]
        public string remoteAtlasPath = "Assets/EUResources/Remote/UI/Atlases";

        [Header("UI 相机配置（ScreenSpaceCamera 模式）")]
        [Tooltip("UI 相机渲染深度")]
        public int uiCameraDepth = 100;

        [Tooltip("UI 相机渲染层")]
        public LayerMask uiCullingMask = 1 << 5;

        [Tooltip("UI 相机清除标志")]
        public CameraClearFlags uiCameraClearFlags = CameraClearFlags.Depth;

        [Header("面板缓存（LRU）")]
        [Tooltip("最多缓存多少个已关闭的面板，0 表示不缓存（立即销毁），默认 5")]
        public int panelCacheCapacity = 5;

        [Header("层级排序")]
        [Tooltip("各层级的 Canvas SortingOrder 基础值")]
        public int baseSortingOrder = 0;

        [Header("多人输入模式")]
        [Tooltip("MultiplayerUIEvent：独立 inputactions，支持键盘区域隔离（默认）\n" +
                 "InputControllerAsset：复用 PlayerInputController 自带 Asset，设备由 EUInputController 管理")]
        public MultiplayerInputMode multiplayerInputMode = MultiplayerInputMode.MultiplayerUIEvent;

        /// <summary>
        /// 获取 UI Prefab 完整路径
        /// </summary>
        public string GetPrefabPath(string panelName, EUUIPackageType packageType)
        {
            string prefix = packageType == EUUIPackageType.Builtin ? builtinPrefabPath : remotePrefabPath;
            return $"{prefix}/{panelName}.prefab";
        }

        /// <summary>
        /// 获取图集路径（用于拼接）
        /// </summary>
        public string GetAtlasPath(string atlasName, bool isBuiltin)
        {
            string prefix = isBuiltin ? builtinAtlasPath : remoteAtlasPath;
            return $"{prefix}/{atlasName}.spriteatlas";
        }
    }
}
