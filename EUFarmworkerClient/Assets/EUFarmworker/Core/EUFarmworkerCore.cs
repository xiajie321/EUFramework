using System;
using System.Collections.Generic;

namespace EUFarmworker.Core
{
    // ==================================================================================
    // 1. 核心架构接口 (Architecture Interface)
    // ==================================================================================

    public interface IArchitecture
    {
        // 注册模块
        void RegisterSystem<T>(T system) where T : class, ISystem;
        void RegisterModel<T>(T model) where T : class, IModel;
        void RegisterUtility<T>(T utility) where T : class, IUtility;

        // 获取模块
        T GetSystem<T>() where T : class, ISystem;
        T GetModel<T>() where T : class, IModel;
        T GetUtility<T>() where T : class, IUtility;

        // 命令与查询
        void SendCommand<T>(T command) where T : struct, ICommand;
        TResult SendQuery<T, TResult>(T query) where T : struct, IQuery<TResult>;

        // 事件系统
        void SendEvent<T>() where T : new();
        void SendEvent<T>(T e);
        IUnRegister RegisterEvent<T>(Action<T> onEvent);
        void UnRegisterEvent<T>(Action<T> onEvent);
    }

    // ==================================================================================
    // 2. 模块与规则接口
    // ==================================================================================

    public interface IBelongToArchitecture
    {
        IArchitecture GetArchitecture();
        void SetArchitecture(IArchitecture architecture);
    }

    public interface ISystem : IBelongToArchitecture, ICanSetArchitecture, ICanGetModel, ICanGetUtility, ICanRegisterEvent, ICanSendEvent, ICanGetSystem
    {
        void Init();
    }

    public interface IModel : IBelongToArchitecture, ICanSetArchitecture, ICanGetUtility, ICanSendEvent
    {
        void Init();
    }

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

    public interface ICommand
    {
        void Execute(IArchitecture architecture);
    }

    public interface IQuery<TResult>
    {
        TResult Do(IArchitecture architecture);
    }

    // ==================================================================================
    // 4. 扩展方法 (Extensions)
    // ==================================================================================

    public static class ArchitectureExtensions
    {
        public static T GetModel<T>(this ICanGetModel self) where T : class, IModel
        {
            return ((IBelongToArchitecture)self).GetArchitecture().GetModel<T>();
        }

        public static T GetSystem<T>(this ICanGetSystem self) where T : class, ISystem
        {
            return ((IBelongToArchitecture)self).GetArchitecture().GetSystem<T>();
        }

        public static T GetUtility<T>(this ICanGetUtility self) where T : class, IUtility
        {
            return ((IBelongToArchitecture)self).GetArchitecture().GetUtility<T>();
        }

        public static void SendCommand<T>(this ICanSendCommand self, T command) where T : struct, ICommand
        {
            ((IBelongToArchitecture)self).GetArchitecture().SendCommand(command);
        }
        
        // 支持非 ICanSendCommand 的对象直接使用 (如 MonoBehaviour)
        public static void SendCommand<T>(this IBelongToArchitecture self, T command) where T : struct, ICommand
        {
            self.GetArchitecture().SendCommand(command);
        }

        public static TResult SendQuery<T, TResult>(this ICanSendQuery self, T query) where T : struct, IQuery<TResult>
        {
            return ((IBelongToArchitecture)self).GetArchitecture().SendQuery<T, TResult>(query);
        }

        public static void SendEvent<T>(this ICanSendEvent self) where T : new()
        {
            ((IBelongToArchitecture)self).GetArchitecture().SendEvent<T>();
        }

        public static void SendEvent<T>(this ICanSendEvent self, T e)
        {
            ((IBelongToArchitecture)self).GetArchitecture().SendEvent(e);
        }

        public static IUnRegister RegisterEvent<T>(this ICanRegisterEvent self, Action<T> onEvent)
        {
            return ((IBelongToArchitecture)self).GetArchitecture().RegisterEvent(onEvent);
        }
    }

    // ==================================================================================
    // 5. 优化后的 IOC 容器 (IOC Container)
    // ==================================================================================

    public class IOCContainer
    {
        // 使用 Dictionary 存储实例
        private Dictionary<Type, object> mInstances = new Dictionary<Type, object>();

        /// <summary>
        /// 注册具体实例
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
        /// 获取实例
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

        public void Clear()
        {
            mInstances.Clear();
        }
    }

    // ==================================================================================
    // 6. 极速事件系统 (TypeEventSystem Optimized)
    // ==================================================================================

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

        public void Send<T>(T e)
        {
            var type = typeof(T);
            if (mEvents.TryGetValue(type, out var container))
            {
                // 无 GC 调用
                (container as EventContainer<T>)?.OnEvent?.Invoke(e);
            }
        }

        public void Send<T>() where T : new()
        {
            // 对于无参数 struct 事件，推荐使用 Send(new T())
            // 这里为了方便，如果 T 是 struct，会产生一次栈分配，无堆 GC
            Send(new T());
        }

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

    public abstract class Architecture<T> : IArchitecture where T : Architecture<T>, new()
    {
        // --- 单例管理 ---
        private static T mArchitecture;
        
        public static IArchitecture Interface
        {
            get
            {
                if (mArchitecture == null) MakeSureArchitecture();
                return mArchitecture;
            }
        }

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

        // --- 内部状态 ---
        private IOCContainer mContainer = new IOCContainer();
        private TypeEventSystem mEventSystem = new TypeEventSystem();
        
        private List<IModel> mModels = new List<IModel>();
        private List<ISystem> mSystems = new List<ISystem>();

        // --- 抽象接口 ---
        protected abstract void Init();

        // --- IArchitecture 实现 ---

        public void RegisterSystem<TSystem>(TSystem system) where TSystem : class, ISystem
        {
            system.SetArchitecture(this);
            mContainer.Register<TSystem>(system);

            if (mArchitecture == null) // 还在 Init 阶段
            {
                mSystems.Add(system);
            }
            else // 运行时动态注册
            {
                system.Init();
            }
        }

        public void RegisterModel<TModel>(TModel model) where TModel : class, IModel
        {
            model.SetArchitecture(this);
            mContainer.Register<TModel>(model);

            if (mArchitecture == null)
            {
                mModels.Add(model);
            }
            else
            {
                model.Init();
            }
        }

        public void RegisterUtility<TUtility>(TUtility utility) where TUtility : class, IUtility
        {
            utility.SetArchitecture(this);
            mContainer.Register<TUtility>(utility);
        }

        public TSystem GetSystem<TSystem>() where TSystem : class, ISystem
        {
            return mContainer.Get<TSystem>();
        }

        public TModel GetModel<TModel>() where TModel : class, IModel
        {
            return mContainer.Get<TModel>();
        }

        public TUtility GetUtility<TUtility>() where TUtility : class, IUtility
        {
            return mContainer.Get<TUtility>();
        }

        public void SendCommand<TCommand>(TCommand command) where TCommand : struct, ICommand
        {
            command.Execute(this);
        }

        public TResult SendQuery<TQuery, TResult>(TQuery query) where TQuery : struct, IQuery<TResult>
        {
            return query.Do(this);
        }

        public void SendEvent<TEvent>() where TEvent : new()
        {
            mEventSystem.Send<TEvent>();
        }

        public void SendEvent<TEvent>(TEvent e)
        {
            mEventSystem.Send(e);
        }

        public IUnRegister RegisterEvent<TEvent>(Action<TEvent> onEvent)
        {
            return mEventSystem.Register(onEvent);
        }

        public void UnRegisterEvent<TEvent>(Action<TEvent> onEvent)
        {
            mEventSystem.UnRegister(onEvent);
        }
        
        // 销毁架构（如果需要重置游戏状态）
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

    public class BindableProperty<T>
    {
        private T mValue;
        public T Value
        {
            get => mValue;
            set
            {
                if (!object.Equals(mValue, value))
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

        public IUnRegister Register(Action<T> onValueChanged)
        {
            OnValueChanged += onValueChanged;
            return new BindablePropertyUnRegister<T>()
            {
                BindableProperty = this,
                OnValueChanged = onValueChanged
            };
        }
        
        public IUnRegister RegisterWithInitValue(Action<T> onValueChanged)
        {
            onValueChanged(mValue);
            return Register(onValueChanged);
        }

        public void UnRegister(Action<T> onValueChanged)
        {
            OnValueChanged -= onValueChanged;
        }
        
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
}
