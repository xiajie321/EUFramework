using Cysharp.Threading.Tasks;

namespace Framework
{
    public interface IUIPanelData
    {
    }
    public interface IUIPanel
    {
        // === 生命周期方法 ===
        bool CanOpen();              // 打开前检查
        UniTask OpenAsync(IUIPanelData data);               // 打开时 -- 加载资源
        void Show();               // 显示时 -- 打开面板
        void Hide();               // 隐藏时 -- 隐藏面板
        void Close();              // 关闭时 -- 清理资源
                                                  
        bool EnableClose { get; }  // 是否允许关闭
    }
}
