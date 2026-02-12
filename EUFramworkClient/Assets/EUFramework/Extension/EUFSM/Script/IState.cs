namespace EUFramwork.Extension.EUFSMKit
{
    /// <summary>
    /// 状态接口
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// 是否进入状态的逻辑
        /// </summary>
        /// <returns>是否进入当前状态</returns>
        public bool OnCondition();
        /// <summary>
        /// 进入状态时的逻辑
        /// </summary>
        public void OnEnter();
        /// <summary>
        /// 退出状态时的逻辑
        /// </summary>
        public void OnExit();
        /// <summary>
        /// 该状态每帧执行的逻辑
        /// </summary>
        public void OnUpdate();
        /// <summary>
        /// 该状态每物理帧执行的逻辑
        /// </summary>
        public void OnFixedUpdate();
    }
}