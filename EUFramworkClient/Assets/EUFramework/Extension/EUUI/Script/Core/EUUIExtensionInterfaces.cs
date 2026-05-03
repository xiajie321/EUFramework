using UnityEngine;

namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// 面板图集 Sprite 加载能力接口。
    /// 由 EUUIPanelBase.EURes.Generated.cs 在生成后实现，
    /// 让 OSA 等扩展不直接依赖具体资源加载器。
    /// </summary>
    public interface IEUSpriteProvider
    {
        /// <summary>url 格式：atlasName/spriteName</summary>
        Sprite GetSprite(string url, bool isRemote = true);
    }
}
