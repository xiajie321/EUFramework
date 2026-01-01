using EUFarmworker.Core.Interfaces;
using EUFarmworker.Core.Tools;

namespace EUFarmworker.Core
{
    public static class EUCore
    {
        /// <summary>
        /// 该方法用于设置框架(会自动释放上一次的框架的注册信息)
        /// </summary>
        /// <param name="architecture"></param>
        public static void SetArchitecture(IArchitecture architecture)
        {
            CoreExtension.SetArchitecture(architecture);
        }
    }
}