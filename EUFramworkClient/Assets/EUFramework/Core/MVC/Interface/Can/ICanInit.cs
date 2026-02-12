using System;

namespace EUFramework.Core.MVC.Interface.Can
{
    /// <summary>
    /// 赋予对象初始化和销毁的能力
    /// </summary>
    public interface ICanInit:IDisposable
    {
        /// <summary>
        /// 初始化方法
        /// </summary>
        void Init();
    }
}
