
using Cysharp.Threading.Tasks;
using YooAsset;

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
            throw new System.NotImplementedException();
        }

        private async UniTask UpdatePackageVersionAsync()
        {
            var packageName = (string)_machine.GetBlackboardValue("PackageName");
            var package = YooAssets.GetPackage(packageName);
            var operation = package.RequestPackageVersionAsync();
            await operation;
            if (operation.Status != EOperationStatus.Succeed)
            {
                (_machine.Owner as EUResKitPatchOperation)?.OnPackageVersionRequestFailed?.Invoke();
            }
            else
            {
                // 版本请求成功，清零重试计数器
                (_machine.Owner as EUResKitPatchOperation)?.ResetVersionRetryCount();
                _machine.SetBlackboardValue("PackageVersion", operation.PackageVersion);
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
