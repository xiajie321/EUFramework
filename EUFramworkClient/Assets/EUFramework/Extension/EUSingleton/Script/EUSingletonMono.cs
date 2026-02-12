using UnityEngine;

namespace EUFramework.Extension.SingletonKit
{
    public abstract class EUSingletonMono<T> : MonoBehaviour where T : EUSingletonMono<T>
    {
        private static T mInstance;
        private static bool mIsApplicationQuitting = false;

        public static T Instance
        {
            get
            {
                if (mIsApplicationQuitting)
                {
                    return null;
                }

                if (mInstance == null)
                {
                    mInstance = FindObjectOfType<T>();
                    if (mInstance == null)
                    {
                        GameObject go = new GameObject(typeof(T).Name);
                        mInstance = go.AddComponent<T>();
                    }
                }
                return mInstance;
            }
        }

        protected virtual void Awake()
        {
            if (mInstance == null)
            {
                mInstance = this as T;
                Init();
            }
            else if (mInstance == this)
            {
                Init();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Init()
        {
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
            OnCreate();
        }

        protected virtual void OnApplicationQuit()
        {
            mIsApplicationQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (mInstance == this)
            {
                mInstance = null;
            }
        }

        protected virtual void OnCreate() { }
    }
}
