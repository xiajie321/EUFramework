using EUFarmworker.Core.Interfaces.Can;

namespace EUFarmworker.Core.Interfaces
{
    public interface IController:ICanGetModel, ICanGetUtility, ICanSendEvent, ICanGetSystem,ICanRegisterEvent
    {
        
    }
}