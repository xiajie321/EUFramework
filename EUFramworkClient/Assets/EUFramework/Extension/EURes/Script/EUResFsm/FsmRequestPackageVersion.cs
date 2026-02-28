
using Cysharp.Threading.Tasks;
using YooAsset;
using UnityEngine;

namespace EUFramework.Extension.EURes
{
    internal class FsmRequestPackageVersion : IStateNode
    {
        private StateMachine _machine;

        public void OnCreate(StateMachine machine)
        {
            _machine = machine;
        }


        public void OnEnter()
        {
            Debug.Log("[Fsm] FsmRequestPackageVersion OnEnter");
            UpdatePackageVersionAsync().Forget();
        }

        private async UniTask UpdatePackageVersionAsync()
        {
            var packageName = (string)_machine.GetBlackboardValue("PackageName");
            Debug.Log($"[Fsm] FsmRequestPackageVersion 即将 RequestPackageVersionAsync package={packageName}");
            var package = YooAssets.GetPackage(packageName);
            var operation = package.RequestPackageVersionAsync();
            await operation;
            Debug.Log($"[Fsm] FsmRequestPackageVersion await 返回 Status={operation.Status}");
            if (operation.Status != EOperationStatus.Succeed)
            {
                Debug.Log("[Fsm] FsmRequestPackageVersion 版本请求失败，触发 OnPackageVersionRequestFailed");
                (_machine.Owner as EUResKitPatchOperation)?.OnPackageVersionRequestFailed?.Invoke();
            }
            else
            {
                // 版本请求成功，清零重试计数器
                (_machine.Owner as EUResKitPatchOperation)?.ResetVersionRetryCount();
                _machine.SetBlackboardValue("PackageVersion", operation.PackageVersion);
                Debug.Log("[Fsm] FsmRequestPackageVersion 成功，即将 ChangeState FsmUpdatePackageManifest");
                _machine.ChangeState<FsmUpdatePackageManifest>();
            }
        }
        public void OnExit()
        {

        }

        public void OnUpdate()
        {

        }

    }
}
