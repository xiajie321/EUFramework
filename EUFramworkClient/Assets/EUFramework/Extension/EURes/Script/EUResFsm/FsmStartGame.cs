using Cysharp.Threading.Tasks;
using YooAsset;
using UnityEngine;

namespace EUFramework.Extension.EURes
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
            Debug.Log("[Fsm] FsmStartGame OnEnter, SetFinish");
            (_machine.Owner as EUResKitPatchOperation)?.SetFinish();
        }

        public void OnUpdate()
        {
            
        }

        public void OnExit()
        {
            
        }
    }
}
