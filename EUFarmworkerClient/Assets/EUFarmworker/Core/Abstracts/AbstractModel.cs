using EUFarmworker.Core.Interfaces;

namespace EUFarmworker.Core.Abstracts
{
    public abstract class AbstractModel:IModel
    {
        public abstract void Init();
        public virtual void Dispose()
        {
            
        }
    }
}