using EUFramework.Core.MVC.Interface.Can;

namespace EUFramework.Core.MVC.Interface
{
    /// <summary>
    /// 系统接口，用于处理业务逻辑。
    /// 可以初始化，获取模型、工具、系统，以及注册和发送事件。
    /// </summary>
    public interface ISystem:ICanInit,ICanGetModel, ICanGetUtility,ICanRegisterEvent, ICanSendEvent, ICanGetSystem
    {
    }
}
