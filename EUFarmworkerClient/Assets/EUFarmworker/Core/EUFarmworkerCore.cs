using System;
using System.Collections.Generic;
using UnityEngine;

namespace EUFarmworker.Core
{
    // ==================================================================================
    // 1. 核心架构接口 (Architecture Interface)
    // ==================================================================================

    /// <summary>
    /// 架构接口，定义了模块注册、获取、命令、查询和事件系统的基本功能。
    /// </summary>
    public interface IArchitecture
    {
        /// <summary>
        /// 注册系统 (System)。
        /// </summary>
        void RegisterSystem<T>(T system) where T : class, ISystem;
        
        /// <summary>
        /// 注册模型 (Model)。
        /// </summary>
        void RegisterModel<T>(T model) where T : class, IModel;
        
        /// <summary>
        /// 注册工具 (Utility)。
        /// </summary>
        void RegisterUtility<T>(T utility) where T : class, IUtility;

        /// <summary>
        /// 获取系统 (System)。
        /// </summary>
        T GetSystem<T>() where T : class, ISystem;
        
        /// <summary>
        /// 获取模型 (Model)。
        /// </summary>
        T GetModel<T>() where T : class, IModel;
        
        /// <summary>
        /// 获取工具 (Utility)。
        /// </summary>
        T GetUtility<T>() where T : class, IUtility;

        /// <summary>
        /// 发送命令 (Command) - 写操作。
        /// </summary>
        void SendCommand<T>(T command) where T : struct, ICommand;
        
        /// <summary>
        /// 发送查询 (Query) - 读操作。
        /// </summary>
        TResult SendQuery<T, TResult>(T query) where T : struct, IQuery<TResult>;

        /// <summary>
        /// 发送事件 (Event)。
        /// </summary>
        void SendEvent<T>() where T : new();
        
        /// <summary>
        /// 发送事件 (Event)。
        /// </summary>
        void SendEvent<T>(T e);
        
        /// <summary>
        /// 注册事件监听。
        /// </summary>
        IUnRegister RegisterEvent<T>(Action<T> onEvent);
        
        /// <summary>
        /// 注销事件监听。
        /// </summary>
        void UnRegisterEvent<T>(Action<T> onEvent);
    }

    // ==================================================================================
    // 2. 模块与规则接口
    // ==================================================================================

    /// <summary>
    /// 属于架构的接口，用于设置和获取架构引用。
    /// </summary>
    public interface IBelongToArchitecture
    {
        IArchitecture GetArchitecture();
        void SetArchitecture(IArchitecture architecture);
    }

    /// <summary>
    /// 系统接口，负责业务逻辑。
    /// </summary>
    public interface ISystem : IBelongToArchitecture, ICanSetArchitecture, ICanGetModel, ICanGetUtility, ICanRegisterEvent, ICanSendEvent, ICanGetSystem
    {
        void Init();
    }

    /// <summary>
    /// 模型接口，负责数据管理。
    /// </summary>
    public interface IModel : IBelongToArchitecture, ICanSetArchitecture, ICanGetUtility, ICanSendEvent
    {
        void Init();
    }

    /// <summary>
    /// 工具接口，负责基础设施。
    /// </summary>
    public interface IUtility : IBelongToArchitecture, ICanSetArchitecture
    {
    }

    // 规则接口 Mixins
    public interface ICanSetArchitecture { }
    public interface ICanGetModel { }
    public interface ICanGetSystem { }
    public interface ICanGetUtility { }
    public interface ICanRegisterEvent { }
    public interface ICanSendEvent { }
    public interface ICanSendCommand { }
    public interface ICanSendQuery { }

    // ==================================================================================
    // 3. Command & Query (Struct Optimized)
    // ==================================================================================

    /// <summary>
    /// 命令接口，用于修改数据的写操作。
    /// </summary>
    public interface ICommand
    {
        void Execute(IArchitecture architecture);
    }

    /// <summary>
    /// 查询接口，用于获取数据的读操作。
    /// </summary>
    public interface IQuery<TResult>
    {
        TResult Do(IArchitecture architecture);
    }

    // ==================================================================================
    // 4. 扩展方法 (Extensions)
    // ==================================================================================

    /// <summary>
    /// 架构扩展方法，提供便捷的 API 调用。
    /// </summary>
    public static class ArchitectureExtensions
    {
        /// <summary>
        /// 获取模型 (Model)。
        /// </summary>
        public static T GetModel<T>(this ICanGetModel self) where T : class, IModel
        {
            return ((IBelongToArchitecture)self).GetArchitecture().GetModel<T>();
        }

        /// <summary>
        /// 获取系统 (System)。
        /// </summary>
        public static T GetSystem<T>(this ICanGetSystem self) where T : class, ISystem
        {
            return ((IBelongToArchitecture)self).GetArchitecture().GetSystem<T>();
        }

        /// <summary>
        /// 获取工具 (Utility)。
        /// </summary>
        public static T GetUtility<T>(this ICanGetUtility self) where T : class, IUtility
        {
            return ((IBelongToArchitecture)self).GetArchitecture().GetUtility<T>();
        }

        /// <summary>
        /// 发送命令 (Command)。
        /// </summary>
        public static void SendCommand<T>(this ICanSendCommand self, T command) where T : struct, ICommand
        {
            ((IBelongToArchitecture)self).GetArchitecture().SendCommand(command);
        }
        
        /// <summary>
        /// 发送命令 (Command) - 支持非 ICanSendCommand 的对象直接使用 (如 MonoBehaviour)。
        /// </summary>
        public static void SendCommand<T>(this IBelongToArchitecture self, T command) where T : struct, ICommand
        {
            self.GetArchitecture().SendCommand(command);
        }

        /// <summary>
        /// 发送查询 (Query)。
        /// </summary>
        public static TResult SendQuery<T, TResult>(this ICanSendQuery self, T query) where T : struct, IQuery<TResult>
        {
            return ((IBelongToArchitecture)self).GetArchitecture().SendQuery<T, TResult>(query);
        }

        /// <summary>
        /// 发送事件 (Event)。
        /// </summary>
        public static void SendEvent<T>(this ICanSendEvent self) where T : new()
        {
            ((IBelongToArchitecture)self).GetArchitecture().SendEvent<T>();
        }

        /// <summary>
        /// 发送事件 (Event)。
        /// </summary>
        public static void SendEvent<T>(this ICanSendEvent self, T e)
        {
            ((IBelongToArchitecture)self).GetArchitecture().SendEvent(e);
        }

        /// <summary>
        /// 注册事件监听。
        /// </summary>
        public static IUnRegister RegisterEvent<T>(this ICanRegisterEvent self, Action<T> onEvent)
        {
            return ((IBelongToArchitecture)self).GetArchitecture().RegisterEvent(onEvent);
        }
    }

    // ==================================================================================
    // 5. 优化后的 IOC 容器 (IOC Container)
    // ==================================================================================

    /// <summary>
    /// 简单的 IOC 容器，用于存储和获取模块实例。
    /// </summary>
    public class IOCContainer
    {
        // 使用 Dictionary 存储实例
        private Dictionary<Type, object> mInstances = new Dictionary<Type, object>();

        /// <summary>
        /// 注册具体实例。
        /// </summary>
        public void Register<T>(T instance)
        {
            var key = typeof(T);

            if (mInstances.ContainsKey(key))
            {
                mInstances[key] = instance; // 覆盖模式
            }
            else
            {
                mInstances.Add(key, instance);
            }
        }

        /// <summary>
        /// 获取实例。
        /// </summary>
        public T Get<T>() where T : class
        {
            var key = typeof(T);
            if (mInstances.TryGetValue(key, out var retInstance))
            {
                return retInstance as T;
            }
            return null;
        }

        /// <summary>
        /// 清空容器。
        /// </summary>
        public void Clear()
        {
            mInstances.Clear();
        }
    }

    // ==================================================================================
    // 6. 极速事件系统 (TypeEventSystem Optimized)
    // ==================================================================================

    /// <summary>
    /// 注销接口，用于取消注册。
    /// </summary>
    public interface IUnRegister
    {
        void UnRegister();
    }

    /// <summary>
    /// 优化后的事件系统。
    /// 1. 支持基于 Type 的事件分发。
    /// 2. 使用委托链实现广播。
    /// 3. 提供 UnRegister 句柄。
    /// </summary>
    public class TypeEventSystem
    {
        // 定义一个通用的事件容器接口
        interface IEventContainer { }

        // 具体的泛型事件容器
        class EventContainer<T> : IEventContainer
        {
            public Action<T> OnEvent;
        }

        // 存储所有类型的事件容器
        private Dictionary<Type, IEventContainer> mEvents = new Dictionary<Type, IEventContainer>();

        /// <summary>
        /// 发送事件。
        /// </summary>
        public void Send<T>(T e)
        {
            var type = typeof(T);
            if (mEvents.TryGetValue(type, out var container))
            {
                // 无 GC 调用
                (container as EventContainer<T>)?.OnEvent?.Invoke(e);
            }
        }

        /// <summary>
        /// 发送事件 (无参)。
        /// </summary>
        public void Send<T>() where T : new()
        {
            // 对于无参数 struct 事件，推荐使用 Send(new T())
            // 这里为了方便，如果 T 是 struct，会产生一次栈分配，无堆 GC
            Send(new T());
        }

        /// <summary>
        /// 注册事件监听。
        /// </summary>
        public IUnRegister Register<T>(Action<T> onEvent)
        {
            var type = typeof(T);
            EventContainer<T> container;

            if (mEvents.TryGetValue(type, out var existingContainer))
            {
                container = existingContainer as EventContainer<T>;
            }
            else
            {
                container = new EventContainer<T>();
                mEvents.Add(type, container);
            }

            // C# 委托多播是线程安全的加法
            container.OnEvent += onEvent;

            return new TypeEventSystemUnRegister<T>()
            {
                EventSystem = this,
                OnEvent = onEvent
            };
        }

        /// <summary>
        /// 注销事件监听。
        /// </summary>
        public void UnRegister<T>(Action<T> onEvent)
        {
            var type = typeof(T);
            if (mEvents.TryGetValue(type, out var container))
            {
                (container as EventContainer<T>).OnEvent -= onEvent;
            }
        }

        // 注销辅助类，避免闭包捕获
        class TypeEventSystemUnRegister<T> : IUnRegister
        {
            public TypeEventSystem EventSystem;
            public Action<T> OnEvent;

            public void UnRegister()
            {
                EventSystem.UnRegister(OnEvent);
                EventSystem = null;
                OnEvent = null;
            }
        }
        
        public void Clear()
        {
            mEvents.Clear();
        }
    }

    // ==================================================================================
    // 7. 架构抽象基类 (Base Architecture)
    // ==================================================================================

    /// <summary>
    /// 架构抽象基类，实现单例和模块管理。
    /// </summary>
    public abstract class Architecture<T> : IArchitecture where T : Architecture<T>, new()
    {
        // --- 单例管理 ---
        private static T mArchitecture;
        
        /// <summary>
        /// 获取架构的单例实例。
        /// </summary>
        public static IArchitecture Interface
        {
            get
            {
                if (mArchitecture == null) MakeSureArchitecture();
                return mArchitecture;
            }
        }

        /// <summary>
        /// 确保架构已初始化。
        /// </summary>
        public static void MakeSureArchitecture()
        {
            if (mArchitecture == null)
            {
                mArchitecture = new T();
                mArchitecture.Init();
                
                // 执行模块初始化
                foreach (var model in mArchitecture.mModels) model.Init();
                mArchitecture.mModels.Clear();

                foreach (var system in mArchitecture.mSystems) system.Init();
                mArchitecture.mSystems.Clear();
            }
        }

        // --- 极速缓存 (Zero-Cost Access) ---
        // 利用静态泛型类的特性，为每个 T (Architecture) 下的每个模块类型提供极速访问
        private static class InstanceCache<TInstance>
        {
            public static TInstance Instance;
        }

        // 用于在架构销毁时重置所有静态缓存
        private static List<Action> mResetActions = new List<Action>();

        // --- 内部状态 ---
        private IOCContainer mContainer = new IOCContainer();
        private TypeEventSystem mEventSystem = new TypeEventSystem();
        
        private List<IModel> mModels = new List<IModel>();
        private List<ISystem> mSystems = new List<ISystem>();

        // --- 抽象接口 ---
        protected abstract void Init();

        // --- IArchitecture 实现 ---

        /// <summary>
        /// 注册系统 (System)。
        /// </summary>
        public void RegisterSystem<TSystem>(TSystem system) where TSystem : class, ISystem
        {
            system.SetArchitecture(this);
            mContainer.Register<TSystem>(system);

            // 写入静态缓存
            if (InstanceCache<TSystem>.Instance == null)
            {
                mResetActions.Add(() => InstanceCache<TSystem>.Instance = null);
            }
            InstanceCache<TSystem>.Instance = system;

            if (mArchitecture == null) // 还在 Init 阶段
            {
                mSystems.Add(system);
            }
            else // 运行时动态注册
            {
                system.Init();
            }
        }

        /// <summary>
        /// 注册模型 (Model)。
        /// </summary>
        public void RegisterModel<TModel>(TModel model) where TModel : class, IModel
        {
            model.SetArchitecture(this);
            mContainer.Register<TModel>(model);

            // 写入静态缓存
            if (InstanceCache<TModel>.Instance == null)
            {
                mResetActions.Add(() => InstanceCache<TModel>.Instance = null);
            }
            InstanceCache<TModel>.Instance = model;

            if (mArchitecture == null)
            {
                mModels.Add(model);
            }
            else
            {
                model.Init();
            }
        }

        /// <summary>
        /// 注册工具 (Utility)。
        /// </summary>
        public void RegisterUtility<TUtility>(TUtility utility) where TUtility : class, IUtility
        {
            utility.SetArchitecture(this);
            mContainer.Register<TUtility>(utility);

            // 写入静态缓存
            if (InstanceCache<TUtility>.Instance == null)
            {
                mResetActions.Add(() => InstanceCache<TUtility>.Instance = null);
            }
            InstanceCache<TUtility>.Instance = utility;
        }

        /// <summary>
        /// 获取系统 (System)。
        /// </summary>
        public TSystem GetSystem<TSystem>() where TSystem : class, ISystem
        {
            // 优先读取静态缓存 (极速路径)
            if (InstanceCache<TSystem>.Instance != null)
            {
                return InstanceCache<TSystem>.Instance;
            }
            return mContainer.Get<TSystem>();
        }

        /// <summary>
        /// 获取模型 (Model)。
        /// </summary>
        public TModel GetModel<TModel>() where TModel : class, IModel
        {
            // 优先读取静态缓存 (极速路径)
            if (InstanceCache<TModel>.Instance != null)
            {
                return InstanceCache<TModel>.Instance;
            }
            return mContainer.Get<TModel>();
        }

        /// <summary>
        /// 获取工具 (Utility)。
        /// </summary>
        public TUtility GetUtility<TUtility>() where TUtility : class, IUtility
        {
            // 优先读取静态缓存 (极速路径)
            if (InstanceCache<TUtility>.Instance != null)
            {
                return InstanceCache<TUtility>.Instance;
            }
            return mContainer.Get<TUtility>();
        }

        /// <summary>
        /// 发送命令 (Command)。
        /// </summary>
        public void SendCommand<TCommand>(TCommand command) where TCommand : struct, ICommand
        {
            command.Execute(this);
        }

        /// <summary>
        /// 发送查询 (Query)。
        /// </summary>
        public TResult SendQuery<TQuery, TResult>(TQuery query) where TQuery : struct, IQuery<TResult>
        {
            return query.Do(this);
        }

        /// <summary>
        /// 发送事件 (Event)。
        /// </summary>
        public void SendEvent<TEvent>() where TEvent : new()
        {
            mEventSystem.Send<TEvent>();
        }

        /// <summary>
        /// 发送事件 (Event)。
        /// </summary>
        public void SendEvent<TEvent>(TEvent e)
        {
            mEventSystem.Send(e);
        }

        /// <summary>
        /// 注册事件监听。
        /// </summary>
        public IUnRegister RegisterEvent<TEvent>(Action<TEvent> onEvent)
        {
            return mEventSystem.Register(onEvent);
        }

        /// <summary>
        /// 注销事件监听。
        /// </summary>
        public void UnRegisterEvent<TEvent>(Action<TEvent> onEvent)
        {
            mEventSystem.UnRegister(onEvent);
        }
        
        /// <summary>
        /// 销毁架构，重置状态。
        /// </summary>
        public static void DestroyArchitecture()
        {
            if (mArchitecture != null)
            {
                mArchitecture.OnDestroy();
                mArchitecture = null;
            }
        }
        
        protected virtual void OnDestroy()
        {
            // 清空静态缓存
            foreach (var action in mResetActions)
            {
                action();
            }
            mResetActions.Clear();

            mContainer.Clear();
            mEventSystem.Clear();
        }
    }

    // ==================================================================================
    // 8. 抽象基类实现 (Base Implementation Helpers)
    // ==================================================================================

    public abstract class AbstractSystem : ISystem
    {
        private IArchitecture mArchitecture;
        public IArchitecture GetArchitecture() => mArchitecture;

        public void SetArchitecture(IArchitecture architecture)
        {
            mArchitecture = architecture;
        }

        public virtual void Init() { }
    }

    public abstract class AbstractModel : IModel
    {
        private IArchitecture mArchitecture;
        public IArchitecture GetArchitecture() => mArchitecture;

        public void SetArchitecture(IArchitecture architecture)
        {
            mArchitecture = architecture;
        }

        public virtual void Init() { }
    }

    // ==================================================================================
    // 9. 可绑定属性 (Bindable Property)
    // ==================================================================================

    /// <summary>
    /// 可绑定属性，用于实现响应式数据。
    /// </summary>
    public class BindableProperty<T>
    {
        private T mValue;
        
        /// <summary>
        /// 获取或设置属性值，值变化时会触发事件。
        /// </summary>
        public T Value
        {
            get => mValue;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(mValue, value))
                {
                    mValue = value;
                    OnValueChanged?.Invoke(value);
                }
            }
        }

        public BindableProperty(T defaultValue = default)
        {
            mValue = defaultValue;
        }

        public event Action<T> OnValueChanged = _ => { };

        /// <summary>
        /// 注册值变化监听。
        /// </summary>
        public IUnRegister Register(Action<T> onValueChanged)
        {
            OnValueChanged += onValueChanged;
            return new BindablePropertyUnRegister<T>()
            {
                BindableProperty = this,
                OnValueChanged = onValueChanged
            };
        }
        
        /// <summary>
        /// 注册值变化监听，并立即调用一次当前值。
        /// </summary>
        public IUnRegister RegisterWithInitValue(Action<T> onValueChanged)
        {
            onValueChanged(mValue);
            return Register(onValueChanged);
        }

        /// <summary>
        /// 注销监听。
        /// </summary>
        public void UnRegister(Action<T> onValueChanged)
        {
            OnValueChanged -= onValueChanged;
        }
        
        /// <summary>
        /// 设置值但不触发事件。
        /// </summary>
        public void SetValueWithoutEvent(T value)
        {
            mValue = value;
        }
        
        public static implicit operator T(BindableProperty<T> property)
        {
            return property.Value;
        }

        public override string ToString()
        {
            return mValue.ToString();
        }
    }

    public class BindablePropertyUnRegister<T> : IUnRegister
    {
        public BindableProperty<T> BindableProperty;
        public Action<T> OnValueChanged;

        public void UnRegister()
        {
            BindableProperty.UnRegister(OnValueChanged);
            BindableProperty = null;
            OnValueChanged = null;
        }
    }

    // ==================================================================================
    // 10. Unity 扩展 (Unity Extensions)
    // ==================================================================================

    /// <summary>
    /// 自动注销扩展，用于管理事件生命周期。
    /// </summary>
    public static class UnRegisterExtension
    {
        /// <summary>
        /// 当 GameObject 销毁时自动注销。
        /// </summary>
        public static void UnRegisterWhenGameObjectDestroyed(this IUnRegister self, GameObject gameObject)
        {
            if (!gameObject.TryGetComponent<UnRegisterTrigger>(out var trigger))
            {
                trigger = gameObject.AddComponent<UnRegisterTrigger>();
            }

            trigger.Add(self);
        }

        /// <summary>
        /// 当 GameObject 禁用时自动注销。
        /// </summary>
        public static void UnRegisterWhenGameObjectDisabled(this IUnRegister self, GameObject gameObject)
        {
            if (!gameObject.TryGetComponent<UnRegisterOnDisableTrigger>(out var trigger))
            {
                trigger = gameObject.AddComponent<UnRegisterOnDisableTrigger>();
            }

            trigger.Add(self);
        }
    }

    public class UnRegisterTrigger : MonoBehaviour
    {
        private readonly HashSet<IUnRegister> mUnRegisters = new HashSet<IUnRegister>();

        public void Add(IUnRegister unRegister)
        {
            mUnRegisters.Add(unRegister);
        }

        private void OnDestroy()
        {
            foreach (var unRegister in mUnRegisters)
            {
                unRegister.UnRegister();
            }

            mUnRegisters.Clear();
        }
    }

    public class UnRegisterOnDisableTrigger : MonoBehaviour
    {
        private readonly HashSet<IUnRegister> mUnRegisters = new HashSet<IUnRegister>();

        public void Add(IUnRegister unRegister)
        {
            mUnRegisters.Add(unRegister);
        }

        private void OnDisable()
        {
            foreach (var unRegister in mUnRegisters)
            {
                unRegister.UnRegister();
            }

            mUnRegisters.Clear();
        }
    }
}