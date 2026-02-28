using Cysharp.Threading.Tasks;
using YooAsset;
using UnityEngine;

namespace EUFramework.Extension.EURes
{
    internal class FsmDownloadPackageOver : IStateNode
    {
        private StateMachine _machine;

        public void OnCreate(StateMachine machine)
        {
            _machine = machine;
        }

        public void OnEnter()
        {
            Debug.Log("[Fsm] FsmDownloadPackageOver OnEnter，即将 ChangeState FsmClearCacheBundle");
            // 下载完成，进入启动游戏状态
              _machine.ChangeState<FsmClearCacheBundle>();
        }

        public void OnUpdate()
        {
            
        }

        public void OnExit()
        {
            
        }
    }
}
