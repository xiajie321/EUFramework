using EUFarmworker.Core.Abstracts;

namespace EUFarmworker.Tools.EURes.Script
{
    /// <summary>
    /// 资源加载模式
    /// </summary>
    public enum ResLoadMode
    {
        /// <summary>
        /// 使用 UnityEngine.Resources 加载
        /// </summary>
        Resources,
        
        /// <summary>
        /// 使用 YooAsset 加载
        /// </summary>
        YooAsset
    }

    /// <summary>
    /// 资源管理模块
    /// 负责提供资源加载器的创建与管理，作为架构中的 Utility 存在。
    /// </summary>
    public class EUResUtility : AbstractUtility
    {
        /// <summary>
        /// 当前资源加载模式
        /// 默认为 YooAsset
        /// </summary>
        public static ResLoadMode LoadMode { get; set; } = ResLoadMode.YooAsset;

        /// <summary>
        /// 初始化
        /// </summary>
        public override void Init()
        {
            // 可以在此处进行全局配置或初始化
            // 注意：YooAsset 的初始化通常在游戏启动流程中进行，此处仅做模块内的初始化
        }

        /// <summary>
        /// 创建一个资源加载器
        /// </summary>
        /// <param name="mode">指定加载模式，如果不指定则使用全局默认配置 LoadMode</param>
        /// <returns>资源加载器实例</returns>
        public static ResLoader CreateLoader(ResLoadMode? mode = null)
        {
            return ResLoader.Allocate(mode);
        }
    }
}
