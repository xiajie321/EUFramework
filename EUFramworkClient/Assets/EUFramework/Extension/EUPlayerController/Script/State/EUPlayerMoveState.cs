using EUFramework.Extension.EUFSMKit;

namespace EUFramework.Extension.EUPlayerControllerKit.State
{
    public class EUPlayerMoveState:EUAbsStateBase<EUPlayerState,EUPlayerController>
    {
        public EUPlayerMoveState(EUFSM<EUPlayerState> fsm, EUPlayerController owner) : base(fsm, owner)
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
            
        }

        public override void OnExit()
        {
            
        }
    }
}