using EUFarmworker.Core.Interfaces.Can;

namespace EUFarmworker.Core.Interfaces
{
    public interface IQuery<out T>:ICanGetModel,ICanGetUtility
    {
        T Execute();
    }
}