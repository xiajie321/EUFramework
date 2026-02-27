using Cysharp.Threading.Tasks;
using YooAsset;
using UnityEngine;

namespace EUFramework.Extension.EURes
{
    internal class FsmClearCacheBundle : IStateNode
    {
        private StateMachine _machine;

        public void OnCreate(StateMachine machine)
        {
            _machine = machine;
        }

        public void OnEnter()
        {
            var packageName = (string)_machine.GetBlackboardValue("PackageName");
            Debug.Log($"[Fsm] FsmClearCacheBundle OnEnter package={packageName} 即将 ClearCacheFilesAsync");
            var package = YooAssets.GetPackage(packageName);
            var operation = package.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles);
            operation.Completed += Operation_Completed;

        }


        private void Operation_Completed(YooAsset.AsyncOperationBase obj)
        {
            Debug.Log("[Fsm] FsmClearCacheBundle Operation_Completed，即将 ChangeState FsmStartGame");
            _machine.ChangeState<FsmStartGame>();
        }


        public void OnUpdate()
        {

        }

        public void OnExit()
        {

        }
    }
}
