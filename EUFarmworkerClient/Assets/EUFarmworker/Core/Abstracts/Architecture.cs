using System;
using System.Collections.Generic;
using EUFarmworker.Core.Interfaces;
using EUFarmworker.Core.Tools;
using UnityEngine;

namespace EUFarmworker.Core.Abstracts
{
    public abstract class Architecture<T>:IArchitecture where T: Architecture<T>,new()
    {
        private static T _instance;
        private static HashSet<Type> _hashSet = new();//用于检查重复注册
        private static List<ISystem> _systems = new();
        private static List<IUtility> _utilities = new();
        private static List<IModel> _models = new();
        private static Action _dispose;

        public static IArchitecture Instance
        {
            get
            {
                if (_instance == null) InitArchitecture();
                return _instance;
            }
        }

        public static void InitArchitecture()
        {
            if (_instance != null) return;
            _instance = new T();
            CoreExtension.SetArchitecture(_instance);
            _instance.Init();
            int i;
            for (i = 0; i < _models.Count; i++)
            {
                _models[i].Init();
            }
            for (i = 0; i < _systems.Count; i++)
            {
                _systems[i].Init();
            }
            for (i = 0; i < _utilities.Count; i++)
            {
                _utilities[i].Init();
            }
        }
        public void Dispose()
        {
            OnDispose();
            int i;
            for (i = 0; i < _models.Count; i++)
            {
                _models[i].Dispose();
            }
            for (i = 0; i < _systems.Count; i++)
            {
                _systems[i].Dispose();
            }
            for (i = 0; i < _utilities.Count; i++)
            {
                _utilities[i].Dispose();
            }
            _dispose?.Invoke();
            _models.Clear();
            _systems.Clear();
            _utilities.Clear();
            _hashSet.Clear();
            _dispose = null;
            _instance = null;
        }
        protected abstract void Init();
        protected virtual void OnDispose()
        {
            
        }
        
        public void RegisterSystem<T1>(T1 system) where T1 : ISystem
        {
            if (!_hashSet.Add(typeof(T1)))
            {
                Debug.LogError("[RegisterSystem] 重复注册");
                return;
            }
            _systems.Add(system);
            _dispose += () =>
            {
                CacheContainer<T1>.Value = default;
            };
            CacheContainer<T1>.Value = system;
        }

        public void RegisterModel<T1>(T1 model) where T1 : IModel
        {
            if (!_hashSet.Add(typeof(T1)))
            {
                Debug.LogError("[RegisterModel] 重复注册");
                return;
            }
            _models.Add(model);
            _dispose += () =>
            {
                CacheContainer<T1>.Value = default;
            };
            CacheContainer<T1>.Value = model;
        }

        public void RegisterUtility<T1>(T1 utility) where T1 : IUtility
        {
            if (!_hashSet.Add(typeof(T1)))
            {
                Debug.LogError("[RegisterUtility] 重复注册");
                return;
            }
            _utilities.Add(utility);
            _dispose += () =>
            {
                CacheContainer<T1>.Value = default;
            };
            CacheContainer<T1>.Value = utility;
        }

        public void RegisterEvent()
        {
            
        }

        public T1 GetSystem<T1>() where T1 : class, ISystem
        {
            return CacheContainer<T1>.Value;
        }

        public T1 GetModel<T1>() where T1 : class, IModel
        {
            return CacheContainer<T1>.Value;
        }

        public T1 GetUtility<T1>() where T1 : class, IUtility
        {
            return CacheContainer<T1>.Value;
        }

        public void SendCommand<T1>(T1 command) where T1 : struct, ICommand
        {
            command.Execute();
        }

        public T1 SendCommand<TCommand, T1>(TCommand command) where TCommand : struct, ICommand<T1>
        {
            return command.Execute();
        }

        public T1 SendQuery<T1>(T1 query) where T1 : struct, IQuery<T1>
        {
            return query.Execute();
        }

        public void SendEvent<T1>(T1 tEvent) where T1 : struct
        {
            
        }
    }
}