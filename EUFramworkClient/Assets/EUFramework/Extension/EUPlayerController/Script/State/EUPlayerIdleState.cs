using EUFramework.Extension.EUFSMKit;
using UnityEngine;

namespace EUFramework.Extension.EUPlayerControllerKit.State
{
    public class EUPlayerIdleState:EUAbsStateBase<EUPlayerState,EUPlayerController>
    {
        public EUPlayerIdleState(EUFSM<EUPlayerState> fsm, EUPlayerController owner) : base(fsm, owner)
        {
        }

        public override bool OnCondition()
        {
            return base.OnCondition();
        }

        public override void OnEnter()
        {
            
        }

        public override void OnUpdate()
        {
            if (Owner.Pos.x != 0)
            {
                Owner.Rigidbody.velocity = new Vector2(Owner.Pos.x, Owner.Rigidbody.velocity.y);
            }
        }

        public override void OnExit()
        {
            
        }
    }
}