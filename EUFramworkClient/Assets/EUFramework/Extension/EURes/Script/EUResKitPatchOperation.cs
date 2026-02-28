using System;
using UnityEngine;
using YooAsset;

namespace EUFramework.Extension.EURes
{
    /// <summary>
    /// 资源热更新异步操作
    /// 基于状态机实现的资源包热更新流程，支持初始化、版本检查、清单更新、资源下载等完整流程
    /// </summary>
    internal class EUResKitPatchOperation : GameAsyncOperation
    {
        private enum ESteps
        {
            None,//尚未启动
            Update,//补丁流程运行态。驱动内部 StateMachine
            Done,//补丁流程完成态。停止状态机更新
        }
        private readonly string _packageName;
        private ESteps _steps = ESteps.None;
        private readonly StateMachine _machine;

        #region 重试计数器
        /// <summary>初始化重试计数</summary>
        private int _initRetryCount = 0;
        /// <summary>版本请求重试计数</summary>
        private int _versionRetryCount = 0;
        /// <summary>清单更新重试计数</summary>
        private int _manifestRetryCount = 0;
        /// <summary>最大重试次数</summary>
        private const int MAX_RETRY_COUNT = 3;
        #endregion

        #region 外部回调 - 由外部业务层绑定
        /// <summary>
        /// 初始化包失败回调
        /// </summary>
        /// <remarks>外部绑定后可显示弹框，用户确认后调用 <see cref="UserRetryInitialize"/></remarks>
        public Action OnInitializePackageFailed;

        /// <summary>
        /// 请求包版本失败回调
        /// </summary>
        /// <remarks>外部绑定后可显示弹框，用户确认后调用 <see cref="UserRetryRequestVersion"/></remarks>
        public Action OnPackageVersionRequestFailed;

        /// <summary>
        /// 更新包清单失败回调
        /// </summary>
        /// <remarks>外部绑定后可显示弹框，用户确认后调用 <see cref="UserRetryUpdateManifest"/></remarks>
        public Action OnUpdatePackageManifestFailed;

        /// <summary>
        /// 发现需要更新的文件回调
        /// </summary>
        /// <param name="totalCount">需要下载的文件数量</param>
        /// <param name="totalBytes">需要下载的总字节数</param>
        /// <remarks>外部绑定后可显示下载确认弹框，用户确认后调用 <see cref="UserBeginDownloadWebFiles"/></remarks>
        public Action<int, long> OnFoundUpdateFiles;

        /// <summary>
        /// 下载进度更新回调
        /// </summary>
        /// <param name="totalDownloadCount">总下载文件数量</param>
        /// <param name="currentDownloadCount">当前已下载文件数量</param>
        /// <param name="totalDownloadBytes">总下载字节数</param>
        /// <param name="currentDownloadBytes">当前已下载字节数</param>
        public Action<int, int, long, long> OnDownloadUpdate;

        /// <summary>
        /// 下载发生错误回调
        /// </summary>
        /// <param name="fileName">出错的文件名</param>
        /// <param name="errorInfo">错误信息</param>
        public Action<string, string> OnDownloadError;
        #endregion

        /// <summary>
        /// 构造函数 - 创建资源热更新操作
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <param name="playMode">运行模式（编辑器模式、离线模式、联机模式等）</param>
        public EUResKitPatchOperation(string packageName, EPlayMode playMode)
        {
            _packageName = packageName;

            // 注意：失败回调（OnXxxFailed）由外部绑定
            // 外部可以显示弹框让用户选择是否重试，然后调用 UserRetryXxx() 方法

            // 创建状态机
            _machine = new StateMachine(this);
            _machine.AddNode<FsmInitializePackage>();
            _machine.AddNode<FsmRequestPackageVersion>();
            _machine.AddNode<FsmUpdatePackageManifest>();
            _machine.AddNode<FsmCreateDownloader>();
            _machine.AddNode<FsmStartGame>();
            _machine.AddNode<FsmDownloadPackageFiles>();
            _machine.AddNode<FsmDownloadPackageOver>();
            _machine.AddNode<FsmClearCacheBundle>();

            // 设置黑板数据
            _machine.SetBlackboardValue("PackageName", packageName);
            _machine.SetBlackboardValue("PlayMode", playMode);
        }

        #region 重试计数器清零方法
        /// <summary>清零初始化重试计数器</summary>
        public void ResetInitRetryCount() => _initRetryCount = 0;
        /// <summary>清零版本请求重试计数器</summary>
        public void ResetVersionRetryCount() => _versionRetryCount = 0;
        /// <summary>清零清单更新重试计数器</summary>
        public void ResetManifestRetryCount() => _manifestRetryCount = 0;
        #endregion

        #region 公开的用户操作方法 - 供外部调用
        /// <summary>
        /// 用户重试初始化包
        /// </summary>
        /// <remarks>
        /// 通常在 <see cref="OnInitializePackageFailed"/> 回调中，用户确认后调用此方法
        /// 最多重试 3 次，超过次数后自动结束操作
        /// </remarks>
        public void UserRetryInitialize()
        {
            _initRetryCount++;
            if (_initRetryCount > MAX_RETRY_COUNT)
            {
                Debug.LogError($"[EUResKitPatchOperation] 初始化重试次数已达上限({MAX_RETRY_COUNT}次)");
                SetFinish();
                return;
            }

            _machine.ChangeState<FsmInitializePackage>();
        }

        /// <summary>
        /// 用户重试请求包版本
        /// </summary>
        /// <remarks>
        /// 通常在 <see cref="OnPackageVersionRequestFailed"/> 回调中，用户确认后调用此方法
        /// 最多重试 3 次，超过次数后自动结束操作
        /// </remarks>
        public void UserRetryRequestVersion()
        {
            _versionRetryCount++;
            if (_versionRetryCount > MAX_RETRY_COUNT)
            {
                Debug.LogError($"[EUResKitPatchOperation] 版本请求重试次数已达上限({MAX_RETRY_COUNT}次)");
                SetFinish();
                return;
            }

            _machine.ChangeState<FsmRequestPackageVersion>();
        }

        /// <summary>
        /// 用户重试更新包清单
        /// </summary>
        /// <remarks>
        /// 通常在 <see cref="OnUpdatePackageManifestFailed"/> 回调中，用户确认后调用此方法
        /// 最多重试 3 次，超过次数后自动结束操作
        /// </remarks>
        public void UserRetryUpdateManifest()
        {
            _manifestRetryCount++;
            if (_manifestRetryCount > MAX_RETRY_COUNT)
            {
                Debug.LogError($"[EUResKitPatchOperation] 清单更新重试次数已达上限({MAX_RETRY_COUNT}次)");
                SetFinish();
                return;
            }

            _machine.ChangeState<FsmUpdatePackageManifest>();
        }

        /// <summary>
        /// 用户开始下载资源文件
        /// </summary>
        /// <remarks>
        /// 通常在 <see cref="OnFoundUpdateFiles"/> 回调中，用户确认下载后调用此方法
        /// 开始下载所有需要更新的资源文件
        /// </remarks>
        public void UserBeginDownloadWebFiles()
        {
            _machine.ChangeState<FsmDownloadPackageFiles>();
        }

        /// <summary>
        /// 用户取消当前流程，终止操作并使 await 返回（Status 为 Failed）
        /// </summary>
        /// <remarks>在弹窗“取消”按钮或无法弹窗时调用，避免 await patchOperation 永远不返回</remarks>
        public void UserCancel()
        {
            Abort();
        }

        #endregion

        #region 下载回调处理（内部使用）
        /// <summary>
        /// 处理下载错误事件
        /// </summary>
        /// <param name="errorData">YooAsset 提供的错误数据</param>
        /// <remarks>此方法由 YooAsset 下载器回调，内部转发到 <see cref="OnDownloadError"/></remarks>
        public void SendDownloadErrorEventMessage(DownloadErrorData errorData)
        {
            OnDownloadError?.Invoke(errorData.FileName, errorData.ErrorInfo);
        }

        /// <summary>
        /// 处理下载进度更新事件
        /// </summary>
        /// <param name="updateData">YooAsset 提供的更新数据</param>
        /// <remarks>此方法由 YooAsset 下载器回调，内部转发到 <see cref="OnDownloadUpdate"/></remarks>
        public void SendDownloadUpdateDataEventMessage(DownloadUpdateData updateData)
        {
            OnDownloadUpdate?.Invoke(
                updateData.TotalDownloadCount,
                updateData.CurrentDownloadCount,
                updateData.TotalDownloadBytes,
                updateData.CurrentDownloadBytes
            );
        }

        #endregion

        /// <summary>
        /// 设置热更新操作完成
        /// </summary>
        /// <remarks>清零所有重试计数器，标记操作为成功完成状态</remarks>
        public void SetFinish()
        {
            _steps = ESteps.Done;
            Status = EOperationStatus.Succeed;

            // 流程结束，清零所有计数器
            ResetInitRetryCount();
            ResetVersionRetryCount();
            ResetManifestRetryCount();
        }

        /// <summary>
        /// 中止操作（YooAsset 异步操作生命周期方法）
        /// </summary>
        protected override void OnAbort()
        {
            // 可在此处理资源清理逻辑
        }

        /// <summary>
        /// 启动操作（YooAsset 异步操作生命周期方法）
        /// </summary>
        /// <remarks>由 YooAssets.StartOperation() 调用，启动状态机</remarks>
        protected override void OnStart()
        {
            _steps = ESteps.Update;
            _machine.Run<FsmInitializePackage>();
        }

        /// <summary>
        /// 更新操作（YooAsset 异步操作生命周期方法）
        /// </summary>
        /// <remarks>每帧由 OperationSystem 调用，驱动状态机运行</remarks>
        protected override void OnUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.Update)
            {
                _machine.Update();
            }
        }
    }
}
