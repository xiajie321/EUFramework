using Cysharp.Threading.Tasks;
using YooAsset;
using UnityEngine;

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
            Debug.Log("[Fsm] FsmDownloadPackageFiles OnEnter");
            UniTaskBeginDownloadAsync().Forget();
        }
        private async UniTask UniTaskBeginDownloadAsync()
        {
            var downloader = (ResourceDownloaderOperation)_machine.GetBlackboardValue("Downloader");
            downloader.DownloadErrorCallback = (_machine.Owner as EUResKitPatchOperation).SendDownloadErrorEventMessage;
            downloader.DownloadUpdateCallback = (_machine.Owner as EUResKitPatchOperation).SendDownloadUpdateDataEventMessage;
            downloader.BeginDownload();
            Debug.Log("[Fsm] FsmDownloadPackageFiles 即将 await downloader");
            await downloader;
            Debug.Log($"[Fsm] FsmDownloadPackageFiles await 返回 Status={downloader.Status}");

            // 检测下载结果
            if (downloader.Status != EOperationStatus.Succeed)
            {
                Debug.Log("[Fsm] FsmDownloadPackageFiles 下载未成功，不切换状态");
                return;
            }

            Debug.Log("[Fsm] FsmDownloadPackageFiles 成功，即将 ChangeState FsmDownloadPackageOver");
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
