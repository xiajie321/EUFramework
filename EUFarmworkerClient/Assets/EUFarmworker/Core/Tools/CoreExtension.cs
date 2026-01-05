using System;
using System.Runtime.CompilerServices;
using EUFarmworker.Core.Interfaces;
using EUFarmworker.Core.Interfaces.Can;

namespace EUFarmworker.Core.Tools
{
    /// <summary>
    /// 核心扩展类，提供基于接口的扩展方法，简化架构使用
    /// </summary>
    public static partial class CoreExtension
    {
        private static IArchitecture _architecture;

        /// <summary>
        /// 设置当前的架构实例
        /// </summary>
        /// <param name="architecture">架构实例</param>
        public static void SetArchitecture(IArchitecture architecture)
        {
            if (_architecture == architecture) return;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetModel<T>(this ICanGetModel canGetModel) 
        where T : class, IModel
        {
            return _architecture.GetModel<T>();
        }
        public static T GetModel<TIModel, T>(ref this TIModel canGetModel)
            where TIModel : struct, ICanGetModel
            where T : class, IModel
        {
            return _architecture.GetModel<T>();
        }

        /// <summary>
        /// 扩展方法：获取系统
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetSystem<T>(this ICanGetSystem canGetSystem) 
            where T : class, ISystem
        {
            return _architecture.GetSystem<T>();
        }
        public static T GetSystem<TISystem, T>(ref this TISystem canGetSystem)
            where TISystem : struct, ICanGetSystem
            where T : class, ISystem
        {
            return _architecture.GetSystem<T>();
        }

        /// <summary>
        /// 扩展方法：获取工具
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetUtility<T>(this ICanGetUtility canGetUtility) where T : class, IUtility
        {
            return _architecture.GetUtility<T>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetUtility<TIUitlity,T>(ref this TIUitlity canGetUtility) 
            where T : class, IUtility
            where TIUitlity : struct,ICanGetUtility
        {
            return _architecture.GetUtility<T>();
        }


        /// <summary>
        /// 扩展方法：发送命令（无返回值）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendCommand<T>(this ICanSendCommand canSendCommand, in T command) 
            where T : struct, ICommand
        {
            _architecture.SendCommand(command);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendCommand<TICommand,T>(this TICommand canSendCommand, in T command) 
            where TICommand : struct,ICanSendCommand
            where T : struct, ICommand
        {
            _architecture.SendCommand(command);
        }

        /// <summary>
        /// 扩展方法：发送命令（有返回值）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SendCommand<TCommand, T>(this ICanSendCommand canSendCommand, in TCommand command)
            where TCommand : struct, ICommand<T>
        {
            return _architecture.SendCommand<TCommand, T>(command);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SendCommand<TICommand, TCommand, T>(ref this TICommand canSendCommand, in TCommand command)
        where TICommand : struct,ICanSendCommand
        where TCommand : struct,ICommand<T>
        {
            return _architecture.SendCommand<TCommand, T>(command);
        }
        
        
        /// <summary>
        /// 扩展方法：发送查询
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SendQuery<TQuery, T>(this ICanSendQuery canSendQuery, in TQuery query)
            where TQuery : struct, IQuery<T>
        {
            return _architecture.SendQuery<TQuery, T>(query);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SendQuery<TIQuery, TQuery, T>(ref this TIQuery canSendQuery, in TQuery query)
        where TQuery : struct, IQuery<T>
        where TIQuery :struct,ICanSendQuery
        {
            return _architecture.SendQuery<TQuery, T>(query);
        }
        

        /// <summary>
        /// 扩展方法：发送事件
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendEvent<T>(this ICanSendEvent canSendEvent,in T Tevent) 
            where T : struct
        {
            _architecture.SendEvent(Tevent);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendEvent<TIEvent,T>(ref this TIEvent canSendEvent, in T Tevent) 
            where T : struct
            where TIEvent : struct,ICanSendEvent
        {
            _architecture.SendEvent(Tevent);
        }

        /// <summary>
        /// 扩展方法：注册事件
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterEvent<T>(this ICanRegisterEvent canRegisterEvent, Action<T> onEvent) 
            where T : struct
        {
            _architecture.RegisterEvent(onEvent);
        }


        /// <summary>
        /// 扩展方法：注销事件
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnRegisterEvent<T>(this ICanRegisterEvent canRegisterEvent, Action<T> onEvent)
            where T : struct
        {
            _architecture.UnRegisterEvent(onEvent);
        }
    }
}