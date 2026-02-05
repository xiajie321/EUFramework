using System;
using System.Collections.Generic;
using EUFarmworker.Core.MVC.CoreTool;
using EUFarmworker.Core.MVC.Interface;
using UnityEngine;

namespace EUFarmworker.Core.MVC.Abstract
{
    /// <summary>
    /// 架构抽象基类，实现了 IArchitecture 接口。
    /// 管理系统、模型、工具的注册、获取和销毁，以及事件系统。
    /// </summary>
    /// <typeparam name="T">具体的架构类型</typeparam>
    public abstract class AbsArchitectureBase<T>:IArchitecture where T: AbsArchitectureBase<T>,new()
    {
        private static T _instance;
        private static HashSet<Type> _hashSet = new();//用于检查重复注册
        private static List<ISystem> _systems = new();
        private static List<IUtility> _utilities = new();
        private static List<IModel> _models = new();
        private static TypeEventSystem _typeEventSystem = new TypeEventSystem();
        private static Action _dispose;

        /// <summary>
        /// 获取架构实例（单例）
        /// </summary>
        public static IArchitecture Instance
        {
            get
            {
                if (_instance == null) InitArchitecture();
                return _instance;
            }
        }

        /// <summary>
        /// 初始化架构
        /// </summary>
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

        /// <summary>
        /// 销毁架构及其所有模块
        /// </summary>
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
            _typeEventSystem.Clear();
            _dispose = null;
            _instance = null;
        }

        /// <summary>
        /// 架构初始化方法，由子类实现，用于注册模块
        /// </summary>
        protected abstract void Init();

        /// <summary>
        /// 架构销毁回调，可由子类重写
        /// </summary>
        protected virtual void OnDispose()
        {
            
        }
        
        /// <summary>
        /// 注册系统
        /// </summary>
        /// <typeparam name="T1">系统类型</typeparam>
        /// <param name="system">系统实例</param>
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

        /// <summary>
        /// 注册数据模型
        /// </summary>
        /// <typeparam name="T1">模型类型</typeparam>
        /// <param name="model">模型实例</param>
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

        /// <summary>
        /// 注册工具
        /// </summary>
        /// <typeparam name="T1">工具类型</typeparam>
        /// <param name="utility">工具实例</param>
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

        /// <summary>
        /// 注册事件
        /// </summary>
        /// <typeparam name="T1">事件类型</typeparam>
        /// <param name="onEvent">事件回调</param>
        public void RegisterEvent<T1>(Action<T1> onEvent) where T1 : struct
        {
            _typeEventSystem.Register(onEvent);
        }

        /// <summary>
        /// 注销事件
        /// </summary>
        /// <typeparam name="T1">事件类型</typeparam>
        /// <param name="onEvent">事件回调</param>
        public void UnRegisterEvent<T1>(Action<T1> onEvent) where T1 : struct
        {
            _typeEventSystem.UnRegister(onEvent);
        }

        /// <summary>
        /// 获取系统
        /// </summary>
        /// <typeparam name="T1">系统类型</typeparam>
        /// <returns>系统实例</returns>
        public T1 GetSystem<T1>() where T1 : class, ISystem
        {
            return CacheContainer<T1>.Value;
        }

        /// <summary>
        /// 获取数据模型
        /// </summary>
        /// <typeparam name="T1">模型类型</typeparam>
        /// <returns>模型实例</returns>
        public T1 GetModel<T1>() where T1 : class, IModel
        {
            return CacheContainer<T1>.Value;
        }

        /// <summary>
        /// 获取工具
        /// </summary>
        /// <typeparam name="T1">工具类型</typeparam>
        /// <returns>工具实例</returns>
        public T1 GetUtility<T1>() where T1 : class, IUtility
        {
            return CacheContainer<T1>.Value;
        }

        /// <summary>
        /// 发送命令（无返回值）
        /// </summary>
        /// <typeparam name="T1">命令类型</typeparam>
        /// <param name="command">命令实例</param>
        public void SendCommand<T1>(T1 command) where T1 : struct, ICommand
        {
            command.Execute();
        }

        /// <summary>
        /// 发送命令（有返回值）
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        /// <typeparam name="T1">返回值类型</typeparam>
        /// <param name="command">命令实例</param>
        /// <returns>执行结果</returns>
        public T1 SendCommand<TCommand, T1>(TCommand command) where TCommand : struct, ICommand<T1>
        {
            return command.Execute();
        }

        /// <summary>
        /// 发送查询
        /// </summary>
        /// <typeparam name="T1">查询类型</typeparam>
        /// <typeparam name="TQuery">具体查询类型</typeparam>
        /// <param name="query">查询实例</param>
        /// <returns>查询结果</returns>
        public T1 SendQuery<TQuery, T1>(TQuery query) where TQuery : struct, IQuery<T1>
        {
            return query.Execute();
        }

        /// <summary>
        /// 发送事件
        /// </summary>
        /// <typeparam name="T1">事件类型</typeparam>
        /// <param name="tEvent">事件实例</param>
        public void SendEvent<T1>(in T1 tEvent) where T1 : struct
        {
            _typeEventSystem.Send(tEvent);
        }
    }
}
