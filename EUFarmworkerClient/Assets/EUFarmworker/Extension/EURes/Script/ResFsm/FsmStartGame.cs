using Cysharp.Threading.Tasks;
using YooAsset;

namespace EUFarmworker.Extension.EURes
{
    internal class FsmStartGame : IStateNode
    {
        private StateMachine _machine;

        public void OnCreate(StateMachine machine)
        {
            _machine = machine;
        }

        public void OnEnter()
        {
            (_machine.Owner as ResKitPatchOperation)?.SetFinish();
        }

        public void OnUpdate()
        {
            
        }

        public void OnExit()
        {
            
        }
    }
}
