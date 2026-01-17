using EUFarmworker.Core.MVC.Interface;

namespace EUFarmworker.Core.MVC.Abstract
{
    /// <summary>
    /// 数据模型抽象基类
    /// </summary>
    public abstract class AbsModelBase:IModel
    {
        /// <summary>
        /// 初始化模型，需在子类实现
        /// </summary>
        public abstract void Init();

        /// <summary>
        /// 销毁模型，可按需重写
        /// </summary>
        public virtual void Dispose()
        {
            
        }
    }
}
