using System;
using System.Collections.Generic;

namespace EUFarmworker.Core.Tools
{
    //不使用typeof(T)与字典的原因:
    //1、typeof(T)在JIT编译时会由编译时确定推迟变为运行时确定,运行时环境必须去编译器编译后的一个运行时字典中查找,而不是像(typeof(int))这样的运行时确定。
    //2、typeof(T)的结果本身是一个Type类型的实例对象。
    //3、字典属于逻辑查询,会涉及到大量的逻辑操作,而静态泛型会在生成机器码时将静态字段的内存嵌入到指令里可以直接访问。
    //4、静态泛型中的方法很有可能被内联,这会导致方法体直接嵌入到调用点,避免了传统的方法调用开销。
    public class TypeEventSystem
    {
        private interface IRegistration
        {
            void UnRegister(TypeEventSystem system);
        }

        private class EventCache<T> : IRegistration where T : struct
        {
            // 优化 1：使用单例实例，避免 Register 时重复 new 包装对象
            public static readonly EventCache<T> Instance = new EventCache<T>();
            
            // 优化 2：静态字典存储实例映射
            public static readonly Dictionary<TypeEventSystem, Action<T>> SystemToActions = new Dictionary<TypeEventSystem, Action<T>>();

            public void UnRegister(TypeEventSystem system)
            {
                SystemToActions.Remove(system);
            }
        }

        private readonly HashSet<IRegistration> _registeredTypes = new HashSet<IRegistration>();

        public void Register<T>(Action<T> onEvent) where T : struct
        {
                if (!EventCache<T>.SystemToActions.ContainsKey(this))
                {
                    EventCache<T>.SystemToActions[this] = obj => { };
                    // 使用静态单例，彻底消除 Register 时的堆内存分配
                    _registeredTypes.Add(EventCache<T>.Instance);
                }
                EventCache<T>.SystemToActions[this] += onEvent;
        }

        public void UnRegister<T>(Action<T> onEvent) where T : struct
        {
                if (EventCache<T>.SystemToActions.TryGetValue(this, out var actions))
                {
                    EventCache<T>.SystemToActions[this] -= onEvent;
                }
        }

        public void Send<T>(T tEvent) where T : struct
        {
            // Send 通常在主线程高频触发，TryGetValue 在“一写多读”环境下是线程安全的，
            // 且 Key 只有 1 个时速度极快，无需加锁
            if (EventCache<T>.SystemToActions.TryGetValue(this, out var action))
            {
                action.Invoke(tEvent);
            }
        }

        public void Clear()
        {
            foreach (var registration in _registeredTypes)
            {
                registration.UnRegister(this);
            }
            _registeredTypes.Clear();
        }
    }
}