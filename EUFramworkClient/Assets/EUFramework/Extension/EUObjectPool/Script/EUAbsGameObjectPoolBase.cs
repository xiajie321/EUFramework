using System.Collections.Generic;
using UnityEngine;

namespace EUFramework.Extension.EUObjectPoolKit
{
    /// <summary>
    /// Mono的C#对象池(仅允许使用Mono对象作为泛型参数)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>Use [EUObjectPool] attribute to automatically register in EUObjectPoolManager.</remarks>
    public abstract class EUAbsGameObjectPoolBase<T>:IObjectPool<T> where T : MonoBehaviour
    {
        private Stack<T> _pool;
        private GameObject _root;
        private bool _isInit;
       
        public GameObject Root => _root;
        /// <summary>
        /// 对象池初始大小
        /// </summary>
        public virtual Stack<T> Pool => _pool;
        /// <summary>
        /// 初始对象数量
        /// </summary>
        public virtual int StartObjectQuantity => 10;
        /// <summary>
        /// 最大池对象数量
        /// </summary>
        public virtual int MaxObjectQuantity => 100;

        private bool _lsP;
        private T _object;
        /// <summary>
        /// 用于实例化预制体
        /// </summary>
        protected virtual T ObjectPrefab
        {
            get
            {
                if (!_lsP)
                {
                    _lsP = true;
                    _object = OnLoadObject();
                }
                return _object;
            }
        }
        /// <summary>
        /// 从资源管理器加载对象(用于实例化对象),仅在第一次从资源管理器获取对象的时候执行一次,不会频繁调用。
        /// </summary>
        /// <returns>资源管理器加载到内存中的对应的对象引用</returns>
        public abstract T OnLoadObject();
        public abstract void OnInit();
        public abstract void OnCreate(T obj);
        public abstract void OnGet(T obj);
        public abstract void OnRelease(T obj);
        public virtual void Init()
        {
            _isInit = true;
            _root =  new GameObject(typeof(T).Name);
            Object.DontDestroyOnLoad(_root);
            _pool = new Stack<T>(MaxObjectQuantity>=0 ? MaxObjectQuantity:100);
            for (int i = 0; i < StartObjectQuantity; i++)
            {
                T obj = Create();
                obj.gameObject.SetActive(false);  // 初始为禁用状态
                OnRelease(obj);  // 调用释放时的初始化逻辑
                _pool.Push(obj);
            }
            OnInit();
        }

        public T Create()
        {
            T obj =  Object.Instantiate(ObjectPrefab.gameObject).GetComponent<T>();
            obj.transform.SetParent(_root.transform);
            OnCreate(obj);
            return obj;
        }

        public virtual T Get()
        {
            if(!_isInit) Init();
            T obj = null;
            if (_pool.Count == 0)
            {
                obj = Create();
                OnGet(obj);
                return obj;
            }
            obj = _pool.Pop();
            OnGet(obj);
            obj.gameObject.SetActive(true);
            return obj;
        }
        public virtual void Release(T obj)
        {
            if(!_isInit) Init();
            if (Pool.Count >= MaxObjectQuantity && MaxObjectQuantity >= 0)
            {
                Object.Destroy(obj.gameObject);
                return;
            }
            OnRelease(obj);
            obj.gameObject.SetActive(false);
            _pool.Push(obj);
        }
        public virtual void Clear()
        {
            while (_pool.Count > 0)
            {
                T obj = _pool.Pop();
                if (obj != null)
                    Object.Destroy(obj.gameObject);
            }
            _pool.Clear();
        }
    }
}