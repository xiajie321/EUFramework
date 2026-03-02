using System;
using System.Runtime.CompilerServices;
using EUFramework.Core.MVC.Interface;
using EUFramework.Core.MVC.Interface.Can;

namespace EUFramework.Core.MVC.CoreTool
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
        /// <summary>
        /// 扩展方法：获取数据模型 (避免 struct 装箱)
        /// </summary>
        /// <typeparam name="TCaller">调用者的类型 (必须是 struct)</typeparam>
        /// <typeparam name="T">要获取的 Model 类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetModel<TCaller, T>(ref this TCaller caller)
            where TCaller : struct, ICanGetModel
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

        /// <summary>
        /// 扩展方法：获取系统 (避免 struct 装箱)
        /// </summary>
        /// <typeparam name="TCaller">调用者的类型 (必须是 struct)</typeparam>
        /// <typeparam name="T">要获取的 System 类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetSystem<TCaller, T>(ref this TCaller caller)
            where TCaller : struct, ICanGetSystem
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

        /// <summary>
        /// 扩展方法：获取工具 (避免 struct 装箱)
        /// </summary>
        /// <typeparam name="TCaller">调用者的类型 (必须是 struct)</typeparam>
        /// <typeparam name="T">要获取的 Utility 类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetUtility<TCaller, T>(ref this TCaller caller)
            where T : class, IUtility
            where TCaller : struct, ICanGetUtility
        {
            return _architecture.GetUtility<T>();
        }


        /// <summary>
        /// 扩展方法：发送命令（无返回值）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendCommand<T>(this ICanSendCommand canSendCommand, T command)
            where T : struct, ICommand
        {
            _architecture.SendCommand(command);
        }

        /// <summary>
        /// 扩展方法：发送命令（无返回值，避免 struct 装箱）
        /// </summary>
        /// <typeparam name="TCaller">调用者的类型 (必须是 struct)</typeparam>
        /// <typeparam name="T">要发送的 Command 类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendCommand<TCaller, T>(this TCaller caller, T command)
            where TCaller : struct, ICanSendCommand
            where T : struct, ICommand
        {
            _architecture.SendCommand(command);
        }

        /// <summary>
        /// 扩展方法：发送命令（有返回值）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SendCommand<TCommand, T>(this ICanSendCommand canSendCommand, TCommand command)
            where TCommand : struct, ICommand<T>
        {
            return _architecture.SendCommand<TCommand, T>(command);
        }

        /// <summary>
        /// 扩展方法：发送命令（有返回值，避免 struct 装箱）
        /// </summary>
        /// <typeparam name="TCaller">调用者的类型 (必须是 struct)</typeparam>
        /// <typeparam name="TCommand">要发送的 Command 类型</typeparam>
        /// <typeparam name="T">返回值的类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SendCommand<TCaller, TCommand, T>(ref this TCaller caller, TCommand command)
        where TCaller : struct, ICanSendCommand
        where TCommand : struct, ICommand<T>
        {
            return _architecture.SendCommand<TCommand, T>(command);
        }


        /// <summary>
        /// 扩展方法：发送查询
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SendQuery<TQuery, T>(this ICanSendQuery canSendQuery, TQuery query)
            where TQuery : struct, IQuery<T>
        {
            return _architecture.SendQuery<TQuery, T>(query);
        }

        /// <summary>
        /// 扩展方法：发送查询 (避免 struct 装箱)
        /// </summary>
        /// <typeparam name="TCaller">调用者的类型 (必须是 struct)</typeparam>
        /// <typeparam name="TQuery">要发送的 Query 类型</typeparam>
        /// <typeparam name="T">返回值的类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SendQuery<TCaller, TQuery, T>(ref this TCaller caller, TQuery query)
        where TQuery : struct, IQuery<T>
        where TCaller : struct, ICanSendQuery
        {
            return _architecture.SendQuery<TQuery, T>(query);
        }


        /// <summary>
        /// 扩展方法：发送事件
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendEvent<T>(this ICanSendEvent canSendEvent, ref T Tevent)
            where T : struct
        {
            _architecture.SendEvent(Tevent);
        }

        /// <summary>
        /// 扩展方法：发送事件 (避免 struct 装箱)
        /// </summary>
        /// <typeparam name="TCaller">调用者的类型 (必须是 struct)</typeparam>
        /// <typeparam name="T">要发送的 Event 类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendEvent<TCaller, T>(ref this TCaller caller, ref T Tevent)
            where T : struct
            where TCaller : struct, ICanSendEvent
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