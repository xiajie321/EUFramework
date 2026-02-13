using UnityEngine;

namespace Framework
{
    public enum UIType
    {
        Panel,    // 普通面板 (UIPanelBase)
        Popup,    // 弹窗 (UIPopupPanelBase)
        Bar,      // 状态栏
        Other
    }

    public enum UIPackageType
    {
        Builtin, // 首包
        Remote,  // 远程包
    }

    [DisallowMultipleComponent]
    public class UIPanelDescription : MonoBehaviour
    {
        [Header("资源归属")]
        [Tooltip("对应的目录名，如 Login, Battle, Main")]
        public string PackageName = "";
        
        [Header("资源归属")]
        [Tooltip("资源存放类型（首包或远程）")]
        public UIPackageType PackageType=UIPackageType.Remote; // 资源存放类型（首包或远程）

        [Tooltip("UI 的逻辑类型，决定生成的基类")]
        public UIType PanelType = UIType.Panel;

        [Header("自动生成信息")]
        public string Namespace = "Game.UI";

    }
}
