using EUFarmworker.Core.MVC.CoreTool;
using EUFarmworker.Core.MVC.Interface;

namespace EUFarmworker.Core.MVC
{
    /// <summary>
    /// 核心入口类，用于设置和管理架构
    /// </summary>
    public static class EUCore
    {
        /// <summary>
        /// 该方法用于设置游戏运行时的框架(会自动释放上一次的框架的注册信息)
        /// </summary>
        /// <param name="architecture">架构实例</param>
        public static void SetArchitecture(IArchitecture architecture)
        {
            CoreExtension.SetArchitecture(architecture);
        }
    }
}
