using Cysharp.Threading.Tasks;
using YooAsset;

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
            var package = YooAssets.GetPackage(packageName);
            var operation = package.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles);
            operation.Completed += Operation_Completed;

        }


        private void Operation_Completed(YooAsset.AsyncOperationBase obj)
        {
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
