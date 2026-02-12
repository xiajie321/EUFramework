using System.Collections.Generic;
using UnityEngine;

namespace EUFramework.Extension.EUObjectPoolKit
{
    /// <summary>
    /// 非Mono的C#对象池(不允许使用Mono对象作为泛型参数)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>Use [EUObjectPool] attribute to automatically register in EUObjectPoolManager.</remarks>
    public abstract class EUAbsObjectPoolBase<T>:IObjectPool<T> where T : class,new()
    {
        private bool _isInit;
        private Stack<T> _pool;
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
        public abstract void OnInit();
        public abstract void OnCreate(T obj);
        public abstract void OnGet(T obj);
        public abstract void OnRelease(T obj);
        public virtual void Init()
        {
            _isInit = true;
            _pool = new Stack<T>(MaxObjectQuantity>=0 ? MaxObjectQuantity:100);
            for (int i = 0; i < StartObjectQuantity; i++)
            {
                _pool.Push(Create());
            }
            OnInit();
        }

        public T Create()
        {
            var obj = new T();
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
            return obj;
        }
        public virtual void Release(T obj)
        {
            if(!_isInit) Init();
            if (Pool.Count >= MaxObjectQuantity && MaxObjectQuantity >= 0) return;
            OnRelease(obj);
            _pool.Push(obj);
            
        }
        public virtual void Clear()
        {
            Pool.Clear();
        }
    }
}