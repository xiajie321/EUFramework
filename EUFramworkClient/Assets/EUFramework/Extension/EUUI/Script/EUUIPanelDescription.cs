using UnityEngine;

namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// EUUI 面板描述（挂在场景根节点，用于编辑期配置；导出 Prefab 时导出的是 UIRoot，不包含此节点）
    /// </summary>
    [DisallowMultipleComponent]
    public class EUUIPanelDescription : MonoBehaviour
    {
        [Header("资源归属")]
        [Tooltip("对应的目录名，如 Login, Battle, Main")]
        public string PackageName = "";

        [Header("资源归属")]
        [Tooltip("资源存放类型（首包或远程）")]
        public EUUIPackageType PackageType = EUUIPackageType.Remote;

        [Tooltip("UI 的逻辑类型，决定生成的基类")]
        public EUUIType PanelType = EUUIType.Panel;

        [Header("自动生成信息")]
        public string Namespace = "Game.UI";
    }
}
