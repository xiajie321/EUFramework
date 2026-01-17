using EUFarmworker.Core.MVC.Interfaces;

namespace EUFarmworker.Core.MVC.Abstracts
{
    /// <summary>
    /// 系统抽象基类
    /// </summary>
    public abstract class AbstractSystem:ISystem
    {
        /// <summary>
        /// 初始化系统，需在子类实现
        /// </summary>
        public abstract void Init();

        /// <summary>
        /// 销毁系统，可按需重写
        /// </summary>
        public virtual void Dispose()
        {
            
        }
    }
}
