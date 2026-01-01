using System;
using System.Collections.Generic;

namespace EUFarmworker.Core.Tools
{
    public class TypeEventSystem
    {
        private readonly Dictionary<Type, IRegisterations> _eventRegisterations = new Dictionary<Type, IRegisterations>();

        public void Register<T>(Action<T> onEvent)
        {
            var type = typeof(T);
            if (_eventRegisterations.TryGetValue(type, out var registerations))
            {
                var reg = registerations as Registerations<T>;
                reg.OnEvent += onEvent;
            }
            else
            {
                var reg = new Registerations<T>();
                reg.OnEvent += onEvent;
                _eventRegisterations.Add(type, reg);
            }
        }

        public void UnRegister<T>(Action<T> onEvent)
        {
            var type = typeof(T);
            if (_eventRegisterations.TryGetValue(type, out var registerations))
            {
                var reg = registerations as Registerations<T>;
                reg.OnEvent -= onEvent;
            }
        }

        public void Send<T>(T tEvent)
        {
            var type = typeof(T);
            if (_eventRegisterations.TryGetValue(type, out var registerations))
            {
                var reg = registerations as Registerations<T>;
                reg.OnEvent?.Invoke(tEvent);
            }
        }

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
