using System;

namespace EUFarmworker.Core.Interfaces.Can
{
    public interface ICanInit:IDisposable
    {
        void Init();
    }
}