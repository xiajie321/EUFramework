using EUFarmworker.Core.Interfaces;

namespace EUFarmworker.Core.Abstracts
{
    public abstract class AbstractSystem:ISystem
    {
        public abstract void Init();
        public virtual void Dispose()
        {
            
        }
    }
}