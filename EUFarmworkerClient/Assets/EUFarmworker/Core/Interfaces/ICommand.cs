using EUFarmworker.Core.Interfaces.Can;

namespace EUFarmworker.Core.Interfaces
{
    public interface ICommand:ICanGetModel,ICanGetSystem,ICanGetUtility,ICanSendCommand,ICanSendQuery,ICanSendEvent
    {
        void Execute();
    }

    public interface ICommand<out T>:ICanGetModel,ICanGetSystem,ICanGetUtility,ICanSendCommand,ICanSendQuery,ICanSendEvent
    {
        T Execute();
    }
}