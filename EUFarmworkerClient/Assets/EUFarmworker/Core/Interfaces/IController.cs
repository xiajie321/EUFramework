using EUFarmworker.Core.Interfaces.Can;

namespace EUFarmworker.Core.Interfaces
{
    /// <summary>
    /// 控制器接口，通常用于表现层（View）。
    /// 可以获取模型、工具、系统，发送事件和注册事件。
    /// </summary>
    public interface IController:ICanGetModel, ICanGetUtility, ICanSendEvent, ICanGetSystem,ICanRegisterEvent,ICanSendCommand
    {
        
    }
}
