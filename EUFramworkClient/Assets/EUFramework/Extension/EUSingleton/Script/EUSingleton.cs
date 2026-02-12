using System;

namespace EUFramework.Extension.SingletonKit
{
    public abstract class EUSingleton<T> where T : EUSingleton<T>
    {
        private static T mInstance;

        public static T Instance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = Activator.CreateInstance(typeof(T), true) as T;
                    mInstance?.OnCreate();
                }
                return mInstance;
            }
        }

        protected virtual void OnCreate() { }
    }
}
