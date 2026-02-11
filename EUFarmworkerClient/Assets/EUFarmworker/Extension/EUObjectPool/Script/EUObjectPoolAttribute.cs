using System;
using System.Diagnostics;

namespace EUFarmworker.Extension.EUObjectPool
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false),Conditional("UNITY_EDITOR")]
    public class EUObjectPoolAttribute : Attribute
    {
        public EUObjectPoolAttribute()
        {
        }
    }
}
