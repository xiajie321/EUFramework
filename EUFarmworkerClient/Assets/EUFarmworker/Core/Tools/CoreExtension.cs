using System;
using EUFarmworker.Core.Interfaces;
using EUFarmworker.Core.Interfaces.Can;

namespace EUFarmworker.Core.Tools
{
    public static class CoreExtension
    {
        private static IArchitecture _architecture;

        public static void SetArchitecture(IArchitecture architecture)
        {
            if(_architecture == architecture) return;
            _architecture?.Dispose();
            _architecture = architecture;
        }

        public static IArchitecture GetArchitecture() => _architecture;
        
        public static T GetModel<T>(this ICanGetModel canGetModel) where T: class,IModel
        {
            return _architecture.GetModel<T>();
        }

        public static T GetSystem<T>(this ICanGetSystem canGetSystem) where T : class, ISystem
        {
            return _architecture.GetSystem<T>();
        }

        public static T GetUtility<T>(this ICanGetUtility canGetUtility) where T : class, IUtility
        {
            return _architecture.GetUtility<T>();
        }

        public static void SendCommand<T>(this ICanSendCommand canSendCommand,T command) where T : struct,ICommand
        {
            _architecture.SendCommand(command);
        }

        public static T SendCommand<TCommand, T>(this ICanSendCommand sendCommand, TCommand command)where TCommand : struct, ICommand<T>
        {
            return _architecture.SendCommand<TCommand,T>(command);
        }

        public static T SendQuery<T>(this ICanSendQuery canSendQuery,T query) where T : struct, IQuery<T>
        {
            return _architecture.SendQuery(query);
        }

        public static void SendEvent<T>(this ICanSendEvent canSendEvent, T Tevent) where T : struct
        {
            _architecture.SendEvent(Tevent);
        }

        public static void RegisterEvent<T>(this ICanRegisterEvent canRegisterEvent, Action<T> onEvent) where T : struct
        {
            _architecture.RegisterEvent(onEvent);
        }

        public static void UnRegisterEvent<T>(this ICanRegisterEvent canRegisterEvent, Action<T> onEvent) where T : struct
        {
            _architecture.UnRegisterEvent(onEvent);
        }
    }
}
