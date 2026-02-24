namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// 实现此接口的非抽象类将作为 Hotbox 功能条目，
    /// 可在 EUUI 功能编排面板中被发现和配置。
    /// 当无法在静态方法上添加 [EUHotboxEntry] 特性时（例如第三方类），
    /// 可用此接口作为备用方式注册功能。
    /// </summary>
    public interface IEUHotboxAction
    {
        string Label   { get; }
        string Group   { get; }
        string Tooltip { get; }

        void Execute();
    }
}
