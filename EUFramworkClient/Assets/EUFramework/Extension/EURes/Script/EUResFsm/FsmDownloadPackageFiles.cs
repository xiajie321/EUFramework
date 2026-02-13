using Cysharp.Threading.Tasks;
using YooAsset;

namespace EUFramework.Extension.EURes
{
    internal class FsmDownloadPackageFiles : IStateNode
    {
        private StateMachine _machine;

        public void OnCreate(StateMachine machine)
        {
            _machine = machine;
        }

        public void OnEnter()
        {
            UniTaskBeginDownloadAsync().Forget();
        }
        private async UniTask UniTaskBeginDownloadAsync()
        {
            var downloader = (ResourceDownloaderOperation)_machine.GetBlackboardValue("Downloader");
            downloader.DownloadErrorCallback = (_machine.Owner as EUResKitPatchOperation).SendDownloadErrorEventMessage;
            downloader.DownloadUpdateCallback = (_machine.Owner as EUResKitPatchOperation).SendDownloadUpdateDataEventMessage;
            downloader.BeginDownload();
            await downloader;

            // 检测下载结果
            if (downloader.Status != EOperationStatus.Succeed)
                return;

            _machine.ChangeState<FsmDownloadPackageOver>();
        }

        public void OnUpdate()
        {
            
        }

        public void OnExit()
        {
           
        }
    }
}
