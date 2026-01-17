using System;
using System.Collections.Generic;

namespace EUFarmworker.Core.MVC.CoreTool
{
    //不使用typeof(T)与字典的原因:
    //1、typeof(T)在JIT编译时会由编译时确定推迟变为运行时确定。
    //2、typeof(T)的结果本身是一个Type类型的实例对象。
    //3、字典属于逻辑查询,会涉及到大量的逻辑操作,而静态泛型会在生成机器码时将静态字段的内存嵌入到指令里可以直接访问。
    //4、静态泛型中的方法很有可能被内联,这会导致方法体直接嵌入到调用点,避免了传统的方法调用开销。
    public class TypeEventSystem
    {
        private static int _globalSystemIdCounter = 0;
        private static readonly Queue<int> _availableSystemIds = new Queue<int>();
        private readonly int _systemId;

        public TypeEventSystem()
        {
            if (_availableSystemIds.Count > 0)
            {
                _systemId = _availableSystemIds.Dequeue();
            }
            else
            {
                _systemId = _globalSystemIdCounter++;
            }
        }

        ~TypeEventSystem()
        {
            Clear();
            _availableSystemIds.Enqueue(_systemId);
        }

        private interface IRegistration
        {
            void UnRegister(int systemId);
        }

        private class EventCache<T> : IRegistration where T : struct
        {
            public static readonly EventCache<T> Instance = new EventCache<T>();
            
            // 使用数组存储，通过 systemId 直接索引
            public Action<T>[] SystemActions = new Action<T>[1];

            public void UnRegister(int systemId)
            {
                if (systemId < SystemActions.Length)
                {
                    SystemActions[systemId] = null;
                }
            }

            public void EnsureCapacity(int index)
            {
                if (index >= SystemActions.Length)
                {
                    int newSize = Math.Max(index + 1, SystemActions.Length * 2);
                    Array.Resize(ref SystemActions, newSize);
                }
            }
        }

        private readonly HashSet<IRegistration> _registeredTypes = new HashSet<IRegistration>();

        public void Register<T>(Action<T> onEvent) where T : struct
        {
            var cache = EventCache<T>.Instance;
            cache.EnsureCapacity(_systemId);
            
            if (cache.SystemActions[_systemId] == null)
            {
                cache.SystemActions[_systemId] = _ => { };
                _registeredTypes.Add(cache);
            }
            cache.SystemActions[_systemId] += onEvent;
        }

        public void UnRegister<T>(Action<T> onEvent) where T : struct
        {
            var cache = EventCache<T>.Instance;
            if (_systemId < cache.SystemActions.Length && cache.SystemActions[_systemId] != null)
            {
                cache.SystemActions[_systemId] -= onEvent;
            }
        }

        public void Send<T>(T tEvent) where T : struct
        {
            var actions = EventCache<T>.Instance.SystemActions;
            // 消除哈希计算，通过 ID 直接索引
            if (_systemId < actions.Length)
            {
                actions[_systemId]?.Invoke(tEvent);
            }
        }

        public void Clear()
        {
            foreach (var registration in _registeredTypes)
            {
                registration.UnRegister(_systemId);
            }
            _registeredTypes.Clear();
        }
    }
}
