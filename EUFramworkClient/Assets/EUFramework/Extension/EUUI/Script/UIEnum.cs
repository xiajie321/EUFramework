// UI Kit 使用的枚举
namespace Framework
{
    public enum UILayerEnum
    {
        Background = 0,    // UI背景层（最底层）
        Normal = 1,        // 普通面板层
        Bar = 2,          // 顶部栏/底部栏层（用于topbar/bottombar）
        Popup = 3,         // 弹出窗口层
        Top = 4,           // 顶层提示层
        System = 5         // 系统层（最顶层）
    }
}