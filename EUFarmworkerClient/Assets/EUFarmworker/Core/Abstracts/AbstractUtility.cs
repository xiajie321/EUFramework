using EUFarmworker.Core.Interfaces;

namespace EUFarmworker.Core.Abstracts
{
    /// <summary>
    /// 工具抽象基类
    /// </summary>
    public abstract class AbstractUtility :IUtility
    {
        /// <summary>
        /// 初始化工具，可按需重写
        /// </summary>
        public virtual void Init()
        {
            
        }

        /// <summary>
        /// 销毁工具，可按需重写
        /// </summary>
        public virtual void Dispose()
        {
            
        }
    }
}
