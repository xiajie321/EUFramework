using System;

namespace EUFarmworker.Core.Interfaces
{
    public interface IArchitecture:IDisposable
    {
        /// <summary>
        /// 注册系统
        /// </summary>
        void RegisterSystem<T>(T system) where T : ISystem;
        /// <summary>
        /// 注册数据
        /// </summary>
        void RegisterModel<T>(T model) where T : IModel;
        /// <summary>
        /// 注册工具
        /// </summary>
        void RegisterUtility<T>(T utility) where T : IUtility;
        
        T GetSystem<T>() where T : class,ISystem;
        
        T GetModel<T>() where T : class,IModel; 
        
        T GetUtility<T>() where T : class,IUtility;
        
        void SendCommand<T>(T command) where T : struct,ICommand;//无返回值命令
        
        T SendCommand<TCommand,T>(TCommand command) where TCommand : struct,ICommand<T>;//有返回值命令
        
        T SendQuery<T>(T query) where T : struct,IQuery<T>;//查询
        
        void SendEvent<T>(T tEvent) where T : struct;//发送事件

        void RegisterEvent<T>(Action<T> onEvent) where T : struct;

        void UnRegisterEvent<T>(Action<T> onEvent) where T : struct;
        
    }
}
