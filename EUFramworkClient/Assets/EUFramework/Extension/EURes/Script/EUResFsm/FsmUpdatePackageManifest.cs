using Cysharp.Threading.Tasks;
using YooAsset;
using UnityEngine;

namespace EUFramework.Extension.EURes
{
    internal class FsmUpdatePackageManifest : IStateNode
    {
        private StateMachine _machine;

        public void OnCreate(StateMachine machine)
        {
            _machine = machine;
        }

        public void OnEnter()
        {
            Debug.Log("[Fsm] FsmUpdatePackageManifest OnEnter");
            UpdateManifestAsync().Forget();
        }

        private async UniTask UpdateManifestAsync()
        {
            var packageName = (string)_machine.GetBlackboardValue("PackageName");
            var packageVersion = (string)_machine.GetBlackboardValue("PackageVersion");
            Debug.Log($"[Fsm] FsmUpdatePackageManifest 即将 UpdatePackageManifestAsync package={packageName} version={packageVersion}");
            var package = YooAssets.GetPackage(packageName);
            var operation = package.UpdatePackageManifestAsync(packageVersion);
            await operation;
            Debug.Log($"[Fsm] FsmUpdatePackageManifest await 返回 Status={operation.Status}");
            if (operation.Status != EOperationStatus.Succeed)
            {
                Debug.Log("[Fsm] FsmUpdatePackageManifest 清单更新失败，触发 OnUpdatePackageManifestFailed");
                (_machine.Owner as EUResKitPatchOperation)?.OnUpdatePackageManifestFailed?.Invoke();
                return;
            }
            else
            {
                // 清单更新成功，清零重试计数器
                (_machine.Owner as EUResKitPatchOperation)?.ResetManifestRetryCount();
                Debug.Log("[Fsm] FsmUpdatePackageManifest 成功，即将 ChangeState FsmCreateDownloader");
                _machine.ChangeState<FsmCreateDownloader>();
            }
        }


        public void OnUpdate()
        {

        }

        public void OnExit()
        {

        }
    }
}
