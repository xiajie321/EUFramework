using System;
using System.Collections.Generic;

namespace EUFarmworker.Core.Tools
{
    /// <summary>
    /// 基于类型的事件系统，用于事件的注册、注销和分发
    /// </summary>
    public class TypeEventSystem
    {
        private readonly Dictionary<Type, IRegisterations> _eventRegisterations = new Dictionary<Type, IRegisterations>();

        /// <summary>
        /// 注册事件监听
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="onEvent">事件回调</param>
        public void Register<T>(Action<T> onEvent)where T:struct
        {
            var type = typeof(T);
            if (_eventRegisterations.TryGetValue(type, out var registerations))
            {
                var reg = (Registerations<T>)registerations;
                reg.OnEvent += onEvent;
            }
            else
            {
                var reg = new Registerations<T>();
                reg.OnEvent += onEvent;
                _eventRegisterations.Add(type, reg);
            }
        }

        /// <summary>
        /// 注销事件监听
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="onEvent">事件回调</param>
        public void UnRegister<T>(Action<T> onEvent)where T:struct
        {
            var type = typeof(T);
            if (_eventRegisterations.TryGetValue(type, out var registerations))
            {
                var reg = (Registerations<T>)registerations;
                reg.OnEvent -= onEvent;
            }
        }

        /// <summary>
        /// 发送事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="tEvent">事件数据</param>
        public void Send<T>(T tEvent) where T:struct
        {
            var type = typeof(T);
            if (_eventRegisterations.TryGetValue(type, out var registerations))
            {
                var reg = (Registerations<T>)registerations;
                reg.OnEvent?.Invoke(tEvent);
            }
        }

        /// <summary>
        /// 清空所有事件注册
        /// </summary>
        public void Clear()
        {
            _eventRegisterations.Clear();
        }

        private interface IRegisterations
        {
            
        }

        private class Registerations<T> : IRegisterations
        {
            public Action<T> OnEvent = obj => { };
        }
    }
}
