using EUFramework.Core.MVC.Interface.Can;

namespace EUFramework.Core.MVC.Interface
{
    /// <summary>
    /// 数据模型接口，用于存储数据状态。
    /// 可以初始化，获取工具，发送事件。
    /// </summary>
    public interface IModel:ICanInit, ICanGetUtility, ICanSendEvent
    {
    }
}
