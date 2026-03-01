using Cysharp.Threading.Tasks;
using UnityEngine;

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

    /// <summary>
    /// 面板图集 Sprite 加载能力接口
    /// 由 EUUIPanelBase.EURes.Generated.cs 在生成后实现
    /// EUUIPanelBaseListExtension 通过此接口解耦对 EUResLoader 的直接依赖
    /// </summary>
    public interface IEUSpriteProvider
    {
        /// <summary>url 格式：atlasName/spriteName</summary>
        Sprite GetSprite(string url, bool isRemote = true);
    }
}
