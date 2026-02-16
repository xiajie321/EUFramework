namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// EUUI 使用的层级枚举
    /// </summary>
    public enum EUUILayerEnum
    {
        Background = 0,    // UI背景层（最底层）
        Normal = 1,        // 普通面板层
        Bar = 2,          // 顶部栏/底部栏层（用于topbar/bottombar）
        Popup = 3,        // 弹出窗口层
        Top = 4,          // 顶层提示层
        System = 5        // 系统层（最顶层）
    }

    /// <summary>
    /// UI 面板类型（决定生成基类）
    /// </summary>
    public enum EUUIType
    {
        Panel,    // 普通面板 (EUUIPanelBase)
        Popup,    // 弹窗 (EUUIPopupPanelBase)
        Bar,      // 状态栏
        Other
    }

    /// <summary>
    /// UI 资源包类型
    /// </summary>
    public enum EUUIPackageType
    {
        Builtin, // 首包
        Remote,  // 远程包
    }
}
