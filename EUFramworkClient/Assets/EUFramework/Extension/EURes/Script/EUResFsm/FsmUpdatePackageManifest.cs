using Cysharp.Threading.Tasks;
using YooAsset;

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
            UpdateManifestAsync().Forget();
        }

        private async UniTask UpdateManifestAsync()
        {
            var packageName = (string)_machine.GetBlackboardValue("PackageName");
            var packageVersion = (string)_machine.GetBlackboardValue("PackageVersion");
            var package = YooAssets.GetPackage(packageName);
            var operation = package.UpdatePackageManifestAsync(packageVersion);
            await operation;

            if (operation.Status != EOperationStatus.Succeed)
            {
                (_machine.Owner as EUResKitPatchOperation)?.OnUpdatePackageManifestFailed?.Invoke();
                return;
            }
            else
            {
                // 清单更新成功，清零重试计数器
                (_machine.Owner as EUResKitPatchOperation)?.ResetManifestRetryCount();
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
