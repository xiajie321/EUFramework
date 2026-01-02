namespace EUFarmworker.Tools.EUFSM.Script.Interfaces
{
    /// <summary>
    /// 状态接口
    /// </summary>
    public interface IState
    {
        bool Condition();
        void Enter();
        void Update();
        void FixedUpdate();
        void OnGUI();
        void Exit();
    }
}