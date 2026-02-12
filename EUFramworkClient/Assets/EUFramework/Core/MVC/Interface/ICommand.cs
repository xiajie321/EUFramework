using EUFramework.Core.MVC.Interface.Can;

namespace EUFramework.Core.MVC.Interface
{
    /// <summary>
    /// 命令接口，用于执行特定的操作，无返回值。
    /// 可以获取模型、系统、工具，发送命令、查询和事件。
    /// </summary>
    public interface ICommand:ICanGetModel,ICanGetSystem,ICanGetUtility,ICanSendCommand,ICanSendQuery,ICanSendEvent
    {
        /// <summary>
        /// 执行命令
        /// </summary>
        void Execute();
    }

    /// <summary>
    /// 命令接口，用于执行特定的操作，有返回值。
    /// 可以获取模型、系统、工具，发送命令、查询和事件。
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    public interface ICommand<out T>:ICanGetModel,ICanGetSystem,ICanGetUtility,ICanSendCommand,ICanSendQuery,ICanSendEvent
    {
        /// <summary>
        /// 执行命令
        /// </summary>
        /// <returns>执行结果</returns>
        T Execute();
    }
}
