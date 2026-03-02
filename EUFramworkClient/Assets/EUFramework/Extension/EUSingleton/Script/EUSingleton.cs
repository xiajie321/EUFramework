using System;

namespace EUFramework.Extension.SingletonKit
{
    public abstract class EUSingleton<T> where T : EUSingleton<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Activator.CreateInstance(typeof(T), true) as T;
                    _instance?.OnCreate();
                }
                return _instance;
            }
        }

        protected virtual void OnCreate() { }
    }
}
