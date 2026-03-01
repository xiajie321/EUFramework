using System;

namespace EUFramework.Extension.EUFSMKit
{
    /// <summary>
    /// 状态基类
    /// </summary>
    /// <typeparam name="TStateId">对应的状态枚举</typeparam>
    /// <typeparam name="TOwner">对应的状态机所有者</typeparam>
    public class EUAbsStateBase<TStateId,TOwner>:IState where TStateId : struct, Enum
    {
        private EUFSM<TStateId> _fsm;
        private TOwner _owner;
        protected EUFSM<TStateId> FSM => _fsm;
        protected TOwner Owner => _owner;

        public EUAbsStateBase(EUFSM<TStateId> fsm, TOwner owner)
        {
            _fsm = fsm;
            _owner = owner;
        }
        public virtual bool OnCondition()
        {
            return true;
        }

        public virtual void OnEnter()
        {

        }

        public virtual void OnExit()
        {

        }

        public virtual void OnUpdate()
        {

        }

        public virtual void OnFixedUpdate()
        {
           
        }
    }
}