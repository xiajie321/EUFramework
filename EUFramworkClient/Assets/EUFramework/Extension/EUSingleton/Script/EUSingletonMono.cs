using UnityEngine;

namespace EUFramework.Extension.SingletonKit
{
    public abstract class EUSingletonMono<T> : MonoBehaviour where T : EUSingletonMono<T>
    {
        private static T _instance;
        private static bool _isApplicationQuitting = false;
        private static bool _isInit = false;
        public static T Instance
        {
            get
            {
                if (_isApplicationQuitting)
                {
                    return null;
                }

                if (_isInit)
                    return _instance;
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject(typeof(T).Name);
                        _instance = go.AddComponent<T>();
                    }
                }
                _isInit = true;
                return _instance;
            }
        }
        public static bool IsInit => _isInit;

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                Init();
            }
            else if (_instance == this)
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
            _isApplicationQuitting = true;
            _isInit = false;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _isInit = false;
                _instance = null;
            }
        }

        protected virtual void OnCreate() { }
    }
}
