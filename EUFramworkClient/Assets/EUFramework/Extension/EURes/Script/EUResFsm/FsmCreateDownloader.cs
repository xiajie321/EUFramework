using Cysharp.Threading.Tasks;
using YooAsset;

namespace EUFramework.Extension.EURes
{
    internal class FsmCreateDownloader : IStateNode
    {
        private StateMachine _machine;

        public void OnCreate(StateMachine machine)
        {
            _machine = machine;
        }

        public void OnEnter()
        {
            CreateDownloader();
        }

        void CreateDownloader()
        {
            var packageName = (string)_machine.GetBlackboardValue("PackageName");
            var package = YooAssets.GetPackage(packageName);
            int downloadingMaxNum = 10;
            int failedTryAgain = 3;
            var downloader = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);
            _machine.SetBlackboardValue("Downloader", downloader);
            if (downloader.TotalDownloadCount == 0)
            {
                _machine.ChangeState<FsmStartGame>();
            }
            else
            {
                int totalDownloadCount = downloader.TotalDownloadCount;
                long totalDownloadBytes = downloader.TotalDownloadBytes;
                (_machine.Owner as EUResKitPatchOperation)?.OnFoundUpdateFiles?.Invoke(totalDownloadCount, totalDownloadBytes);
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
