using EUFarmworker.Core.Interfaces.Can;

namespace EUFarmworker.Core.Interfaces
{
    public interface ISystem:ICanInit,ICanGetModel, ICanGetUtility,ICanRegisterEvent, ICanSendEvent, ICanGetSystem
    {
    }
}