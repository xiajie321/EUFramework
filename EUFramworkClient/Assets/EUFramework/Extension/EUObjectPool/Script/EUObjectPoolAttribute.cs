using System;
using System.Diagnostics;

namespace EUFramework.Extension.EUObjectPoolKit
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false),Conditional("UNITY_EDITOR")]
    public class EUObjectPoolAttribute : Attribute
    {
        public EUObjectPoolAttribute()
        {
        }
    }
}
