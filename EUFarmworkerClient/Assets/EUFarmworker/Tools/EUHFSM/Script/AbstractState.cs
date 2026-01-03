namespace EUFarmworker.Tools.EUFSM
{
    public abstract class AbstractState<TId, TTarget> : IState
    {
        protected FSM<TId, TTarget> mFSM;
        protected TTarget mTarget;

        public void Init(FSM<TId, TTarget> fsm, TTarget target)
        {
            mFSM = fsm;
            mTarget = target;
            OnInit();
        }

        protected virtual void OnInit() { }
        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
        public virtual void OnExit() { }

        protected void ChangeState(TId id)
        {
            mFSM.ChangeState(id);
        }
    }
}
