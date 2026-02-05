using System;

namespace EUFarmworker.Core.MVC.Interface
{
    /// <summary>
    /// 架构接口，定义了架构的基本功能，包括注册、获取模块以及发送命令、查询和事件。
    /// </summary>
    public interface IArchitecture:IDisposable
    {
        /// <summary>
        /// 注册系统
        /// </summary>
        /// <typeparam name="T">系统类型</typeparam>
        /// <param name="system">系统实例</param>
        void RegisterSystem<T>(T system) where T : ISystem;

        /// <summary>
        /// 注册数据模型
        /// </summary>
        /// <typeparam name="T">模型类型</typeparam>
        /// <param name="model">模型实例</param>
        void RegisterModel<T>(T model) where T : IModel;

        /// <summary>
        /// 注册工具
        /// </summary>
        /// <typeparam name="T">工具类型</typeparam>
        /// <param name="utility">工具实例</param>
        void RegisterUtility<T>(T utility) where T : IUtility;
        
        /// <summary>
        /// 获取系统
        /// </summary>
        /// <typeparam name="T">系统类型</typeparam>
        /// <returns>系统实例</returns>
        T GetSystem<T>() where T : class,ISystem;
        
        /// <summary>
        /// 获取数据模型
        /// </summary>
        /// <typeparam name="T">模型类型</typeparam>
        /// <returns>模型实例</returns>
        T GetModel<T>() where T : class,IModel; 
        
        /// <summary>
        /// 获取工具
        /// </summary>
        /// <typeparam name="T">工具类型</typeparam>
        /// <returns>工具实例</returns>
        T GetUtility<T>() where T : class,IUtility;
        
        /// <summary>
        /// 发送无返回值命令
        /// </summary>
        /// <typeparam name="T">命令类型</typeparam>
        /// <param name="command">命令实例</param>
        void SendCommand<T>(T command) where T : struct,ICommand;
        
        /// <summary>
        /// 发送有返回值命令
        /// </summary>
        /// <typeparam name="TCommand">命令类型</typeparam>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="command">命令实例</param>
        /// <returns>命令执行结果</returns>
        T SendCommand<TCommand,T>(TCommand command) where TCommand : struct,ICommand<T>;

        /// <summary>
        /// 发送查询
        /// </summary>
        /// <typeparam name="T">查询类型</typeparam>
        /// <typeparam name="TQuery">查询事件</typeparam>
        /// <param name="query">查询实例</param>
        /// <returns>查询结果</returns>
        T SendQuery<TQuery,T>(TQuery query) where TQuery : struct,IQuery<T>;
        
        /// <summary>
        /// 发送事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="tEvent">事件实例</param>
        void SendEvent<T>(in T tEvent) where T : struct;

        /// <summary>
        /// 注册事件监听
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="onEvent">事件回调</param>
        void RegisterEvent<T>(Action<T> onEvent) where T : struct;

        /// <summary>
        /// 注销事件监听
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="onEvent">事件回调</param>
        void UnRegisterEvent<T>(Action<T> onEvent) where T : struct;
        
    }
}
