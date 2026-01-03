using System.Collections.Generic;
using UnityEngine;

namespace EUFarmworker.Tools.EUFSM
{
    public class FSM<TId, TTarget> : AbstractState<TId, TTarget>
    {
        private Dictionary<TId, AbstractState<TId, TTarget>> mStates;
        private AbstractState<TId, TTarget> mCurrentState;
        private TId mCurrentStateId;
        private AbstractState<TId, TTarget> mPreviousState;
        private TId mPreviousStateId;
        
        public AbstractState<TId, TTarget> CurrentState => mCurrentState;
        public TId CurrentStateId => mCurrentStateId;
        public AbstractState<TId, TTarget> PreviousState => mPreviousState;
        public TId PreviousStateId => mPreviousStateId;

        public FSM(TTarget target, int capacity = 16)
        {
            mTarget = target;
            mStates = new Dictionary<TId, AbstractState<TId, TTarget>>(capacity);
        }

        public void AddState(TId id, AbstractState<TId, TTarget> state)
        {
            if (mStates.ContainsKey(id))
            {
                Debug.LogError($"State {id} already exists in FSM.");
                return;
            }
            mStates.Add(id, state);
            state.Init(this, mTarget);
        }

        public void RemoveState(TId id)
        {
            if (mStates.ContainsKey(id))
            {
                mStates.Remove(id);
            }
        }

        public bool HasState(TId id)
        {
            return mStates.ContainsKey(id);
        }

        public AbstractState<TId, TTarget> GetState(TId id)
        {
            if (mStates.TryGetValue(id, out var state))
            {
                return state;
            }
            return null;
        }

        public new void ChangeState(TId id)
        {
            if (!mStates.TryGetValue(id, out var state))
            {
                Debug.LogError($"State {id} does not exist in FSM.");
                return;
            }

            if (mCurrentState != null)
            {
                mPreviousState = mCurrentState;
                mPreviousStateId = mCurrentStateId;
                mCurrentState.OnExit();
            }

            mCurrentState = state;
            mCurrentStateId = id;
            mCurrentState.OnEnter();
        }

        public override void OnEnter()
        {
            if (mCurrentState != null)
            {
                mCurrentState.OnEnter();
            }
        }

        public override void OnUpdate()
        {
            if (mCurrentState != null)
            {
                mCurrentState.OnUpdate();
            }
        }

        public override void OnFixedUpdate()
        {
            if (mCurrentState != null)
            {
                mCurrentState.OnFixedUpdate();
            }
        }

        public override void OnExit()
        {
            if (mCurrentState != null)
            {
                mCurrentState.OnExit();
            }
        }
        
        public void Clear()
        {
            if (mCurrentState != null)
            {
                mCurrentState.OnExit();
                mCurrentState = null;
            }
            mStates.Clear();
        }
    }
}
