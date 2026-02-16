using Cysharp.Threading.Tasks;

namespace EUFramework.Extension.EUUI
{
    public interface IEUUIPanelData
    {
    }

    public interface IEUUIPanel
    {
        bool CanOpen();
        UniTask OpenAsync(IEUUIPanelData data);
        void Show();
        void Hide();
        void Close();

        bool EnableClose { get; }
    }
}
