using EUFramework.Extension.EUUI;

namespace EUUI.Extension
{
    /// <summary>
    /// EUUIPanelBase 的列表扩展方法
    /// 将面板作用域的 SpriteLoader 注入 Adapter，handle 生命周期跟随面板
    /// </summary>
    public static class EUUIPanelBaseListExtension
    {
        /// <summary>
        /// 将面板的 SpriteLoader 注入 ListAdapter
        /// 在面板 OnOpen/OnShow 前调用，VH 内直接使用 SpriteLoader 加载图集
        /// </summary>
        public static void BindSpriteLoader<TPanel, TData, TVH>(
            this EUUIPanelBase<TPanel> panel,
            FrameworkListAdapter<TData, TVH> adapter)
            where TPanel : EUUIPanelBase<TPanel>
            where TData : class
            where TVH : FrameworkListViewsHolder<TData>, new()
        {
            if (panel is IEUSpriteProvider provider)
                adapter.SpriteLoader = (url) => provider.GetSprite(url);
        }

        /// <summary>
        /// 将面板的 SpriteLoader 注入 GridAdapter
        /// 在面板 OnOpen/OnShow 前调用，VH 内直接使用 SpriteLoader 加载图集
        /// </summary>
        public static void BindSpriteLoader<TPanel, TData, TCellVH>(
            this EUUIPanelBase<TPanel> panel,
            FrameworkGridAdapter<TData, TCellVH> adapter)
            where TPanel : EUUIPanelBase<TPanel>
            where TData : class
            where TCellVH : FrameworkGridViewsHolder<TData>, new()
        {
            if (panel is IEUSpriteProvider provider)
                adapter.SpriteLoader = (url) => provider.GetSprite(url);
        }
    }
}
