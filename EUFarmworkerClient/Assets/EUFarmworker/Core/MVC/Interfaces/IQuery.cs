using EUFarmworker.Core.MVC.Interfaces.Can;

namespace EUFarmworker.Core.MVC.Interfaces
{
    /// <summary>
    /// 查询接口，用于获取数据或状态。
    /// 可以获取模型和工具。
    /// </summary>
    /// <typeparam name="T">查询结果类型</typeparam>
    public interface IQuery<out T>:ICanGetModel,ICanGetUtility,ICanSendQuery
    {
        /// <summary>
        /// 执行查询
        /// </summary>
        /// <returns>查询结果</returns>
        T Execute();
    }
}
