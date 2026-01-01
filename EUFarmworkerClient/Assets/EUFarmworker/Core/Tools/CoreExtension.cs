using System;
using EUFarmworker.Core.Interfaces;
using EUFarmworker.Core.Interfaces.Can;

namespace EUFarmworker.Core.Tools
{
    /// <summary>
    /// 核心扩展类，提供基于接口的扩展方法，简化架构使用
    /// </summary>
    public static class CoreExtension
    {
        private static IArchitecture _architecture;

        /// <summary>
        /// 设置当前的架构实例
        /// </summary>
        /// <param name="architecture">架构实例</param>
        public static void SetArchitecture(IArchitecture architecture)
        {
            if(_architecture == architecture) return;
            _architecture?.Dispose();
            _architecture = architecture;
        }

        /// <summary>
        /// 获取当前的架构实例
        /// </summary>
        /// <returns>架构实例</returns>
        public static IArchitecture GetArchitecture() => _architecture;
        
        /// <summary>
        /// 扩展方法：获取数据模型
        /// </summary>
        public static T GetModel<T>(this ICanGetModel canGetModel) where T: class,IModel
        {
            return _architecture.GetModel<T>();
        }

        /// <summary>
        /// 扩展方法：获取系统
        /// </summary>
        public static T GetSystem<T>(this ICanGetSystem canGetSystem) where T : class, ISystem
        {
            return _architecture.GetSystem<T>();
        }

        /// <summary>
        /// 扩展方法：获取工具
        /// </summary>
        public static T GetUtility<T>(this ICanGetUtility canGetUtility) where T : class, IUtility
        {
            return _architecture.GetUtility<T>();
        }

        /// <summary>
        /// 扩展方法：发送命令（无返回值）
        /// </summary>
        public static void SendCommand<T>(this ICanSendCommand canSendCommand,T command) where T : struct,ICommand
        {
            _architecture.SendCommand(command);
        }

        /// <summary>
        /// 扩展方法：发送命令（有返回值）
        /// </summary>
        public static T SendCommand<TCommand, T>(this ICanSendCommand sendCommand, TCommand command)where TCommand : struct, ICommand<T>
        {
            return _architecture.SendCommand<TCommand,T>(command);
        }

        /// <summary>
        /// 扩展方法：发送查询
        /// </summary>
        public static T SendQuery<T>(this ICanSendQuery canSendQuery,T query) where T : struct, IQuery<T>
        {
            return _architecture.SendQuery(query);
        }

        /// <summary>
        /// 扩展方法：发送事件
        /// </summary>
        public static void SendEvent<T>(this ICanSendEvent canSendEvent, T Tevent) where T : struct
        {
            _architecture.SendEvent(Tevent);
        }

        /// <summary>
        /// 扩展方法：注册事件
        /// </summary>
        public static void RegisterEvent<T>(this ICanRegisterEvent canRegisterEvent, Action<T> onEvent) where T : struct
        {
            _architecture.RegisterEvent(onEvent);
        }

        /// <summary>
        /// 扩展方法：注销事件
        /// </summary>
        public static void UnRegisterEvent<T>(this ICanRegisterEvent canRegisterEvent, Action<T> onEvent) where T : struct
        {
            _architecture.UnRegisterEvent(onEvent);
        }
    }
}
