using EUFarmworker.Core.Interfaces.Can;

namespace EUFarmworker.Core.Interfaces
{
    public interface IModel:ICanInit, ICanGetUtility, ICanSendEvent
    {
    }
}