using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace EUFarmworker.Tools.EURes.Script
{
    /// <summary>
    /// YooAsset.ResourcePackage 的生命周期管理包装类
    /// 负责处理包的初始化、版本更新、清单更新和资源下载
    /// </summary>
    public class EUResPackage
    {
        /// <summary>
        /// 资源包名称
        /// </summary>
        public string PackageName { get; private set; }

        /// <summary>
        /// YooAsset 资源包实例
        /// </summary>
        public ResourcePackage Package { get; private set; }

        /// <summary>
        /// 是否已初始化完成
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 远端资源服务器地址
        /// </summary>
        private string _hostServer;

        /// <summary>
        /// 运行模式
        /// </summary>
        private EPlayMode _playMode;

        /// <summary>
        /// 本地加载路径 (如果不为空，则从该路径加载)
        /// </summary>
        private string _localPath;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="packageName">包名</param>
        /// <param name="hostServer">资源服务器地址</param>
        /// <param name="playMode">运行模式</param>
        /// <param name="localPath">本地加载路径 (可选)</param>
        public EUResPackage(string packageName, string hostServer, EPlayMode playMode, string localPath = null)
        {
            PackageName = packageName;
            _hostServer = hostServer;
            _playMode = playMode;
            _localPath = localPath;
        }

        /// <summary>
        /// 异步初始化资源包
        /// </summary>
        public async UniTask InitializeAsync()
        {
            // 1. 获取或创建资源包
            Package = YooAssets.TryGetPackage(PackageName);
            if (Package == null)
            {
                Package = YooAssets.CreatePackage(PackageName);
            }

            // 2. 初始化资源包
            InitializationOperation initOperation = null;
            if (_playMode == EPlayMode.EditorSimulateMode)
            {
#if UNITY_EDITOR
                // 编辑器模拟模式
                var buildResult = EditorSimulateModeHelper.SimulateBuild(PackageName);
                var packageRoot = buildResult.PackageRootDirectory;
                var createParameters = new EditorSimulateModeParameters();
                createParameters.EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
                initOperation = Package.InitializeAsync(createParameters);
#else
                Debug.LogError($"[EURes] EditorSimulate mode is only supported in Unity Editor. Fallback to OfflinePlay mode.");
                _playMode = EPlayMode.OfflinePlayMode;
                var createParameters = new OfflinePlayModeParameters();
                createParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                initOperation = Package.InitializeAsync(createParameters);
#endif
            }
            else if (_playMode == EPlayMode.OfflinePlayMode)
            {
                // 离线运行模式
                var createParameters = new OfflinePlayModeParameters();
                createParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                initOperation = Package.InitializeAsync(createParameters);
            }
            else if (_playMode == EPlayMode.HostPlayMode || !string.IsNullOrEmpty(_localPath))
            {
                // 联机运行模式 (或本地加载模式)
                var createParameters = new HostPlayModeParameters();
                createParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();

                if (!string.IsNullOrEmpty(_localPath))
                {
                    // 本地加载模式：将 Cache 根目录指向本地路径，Remote 也指向本地路径
                    string localUrl = $"file:///{_localPath}";
                    // 注意：Windows 下 file:///D:/...，其他平台可能不同，这里简单处理
                    if (Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.WindowsPlayer)
                    {
                        localUrl = $"file://{_localPath}";
                    }
                    
                    createParameters.CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(new RemoteServices(localUrl, localUrl), null, _localPath);
                }
                else
                {
                    // 标准联机模式
                    createParameters.CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(new RemoteServices(_hostServer, _hostServer));
                }
                
                initOperation = Package.InitializeAsync(createParameters);
            }
            // WebGL 运行模式 (YooAsset 2.1+ 新增)
            else if (_playMode == EPlayMode.WebPlayMode)
            {
                var createParameters = new WebPlayModeParameters();
                var remoteServices = new RemoteServices(_hostServer, _hostServer);
                createParameters.WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
                createParameters.WebRemoteFileSystemParameters = FileSystemParameters.CreateDefaultWebRemoteFileSystemParameters(remoteServices);
                initOperation = Package.InitializeAsync(createParameters);
            }

            await initOperation.ToUniTask();

            if (initOperation.Status == EOperationStatus.Succeed)
            {
                IsInitialized = true;
                Debug.Log($"[EURes] 资源包 '{PackageName}' 初始化成功。");
            }
            else
            {
                Debug.LogError($"[EURes] 资源包 '{PackageName}' 初始化失败: {initOperation.Error}");
            }
        }

        /// <summary>
        /// 异步更新资源包版本
        /// </summary>
        /// <returns>是否成功, 包版本号</returns>
        public async UniTask<(bool success, string version)> UpdatePackageVersionAsync()
        {
            if (!IsInitialized)
            {
                Debug.LogError($"[EURes] 资源包 '{PackageName}' 未初始化。");
                return (false, null);
            }

            var operation = Package.RequestPackageVersionAsync();
            await operation.ToUniTask();

            if (operation.Status == EOperationStatus.Succeed)
            {
                Debug.Log($"[EURes] 资源包 '{PackageName}' 版本更新成功: {operation.PackageVersion}");
                return (true, operation.PackageVersion);
            }
            else
            {
                Debug.LogError($"[EURes] 资源包 '{PackageName}' 版本更新失败: {operation.Error}");
                return (false, null);
            }
        }

        /// <summary>
        /// 异步更新资源包清单
        /// </summary>
        /// <param name="packageVersion">包版本号</param>
        /// <returns>是否成功</returns>
        public async UniTask<bool> UpdatePackageManifestAsync(string packageVersion)
        {
            if (!IsInitialized)
            {
                Debug.LogError($"[EURes] 资源包 '{PackageName}' 未初始化。");
                return false;
            }

            var operation = Package.UpdatePackageManifestAsync(packageVersion);
            await operation.ToUniTask();

            if (operation.Status == EOperationStatus.Succeed)
            {
                Debug.Log($"[EURes] 资源包 '{PackageName}' 清单更新成功。");
                return true;
            }
            else
            {
                Debug.LogError($"[EURes] 资源包 '{PackageName}' 清单更新失败: {operation.Error}");
                return false;
            }
        }
        
        /// <summary>
        /// 创建资源下载器 (下载所有资源)
        /// </summary>
        /// <param name="downloadingMaxNum">同时下载的最大文件数</param>
        /// <param name="failedTryAgain">失败重试次数</param>
        public ResourceDownloaderOperation CreateResourceDownloader(int downloadingMaxNum = 10, int failedTryAgain = 3)
        {
            if (!IsInitialized)
            {
                Debug.LogError($"[EURes] 资源包 '{PackageName}' 未初始化。");
                return null;
            }
            
            return Package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);
        }

        /// <summary>
        /// 创建资源下载器 (按标签下载)
        /// </summary>
        /// <param name="tag">资源标签</param>
        /// <param name="downloadingMaxNum">同时下载的最大文件数</param>
        /// <param name="failedTryAgain">失败重试次数</param>
        public ResourceDownloaderOperation CreateResourceDownloader(string tag, int downloadingMaxNum = 10, int failedTryAgain = 3)
        {
             if (!IsInitialized)
            {
                Debug.LogError($"[EURes] 资源包 '{PackageName}' 未初始化。");
                return null;
            }

            return Package.CreateResourceDownloader(tag, downloadingMaxNum, failedTryAgain);
        }

        /// <summary>
        /// 清理缓存文件
        /// </summary>
        public async UniTask ClearCacheFilesAsync()
        {
            if (!IsInitialized)
            {
                Debug.LogError($"[EURes] 资源包 '{PackageName}' 未初始化。");
                return;
            }
            
            var operation = Package.ClearCacheFilesAsync(EFileClearMode.ClearAllBundleFiles);
            await operation.ToUniTask();
            
            if (operation.Status == EOperationStatus.Succeed)
            {
                 Debug.Log($"[EURes] 资源包 '{PackageName}' 缓存清理完成。");
            }
            else
            {
                 Debug.LogError($"[EURes] 资源包 '{PackageName}' 缓存清理失败: {operation.Error}");
            }
        }

        /// <summary>
        /// 简单的远程服务实现类
        /// </summary>
        private class RemoteServices : IRemoteServices
        {
            private readonly string _defaultHostServer;
            private readonly string _fallbackHostServer;

            public RemoteServices(string defaultHostServer, string fallbackHostServer)
            {
                _defaultHostServer = defaultHostServer;
                _fallbackHostServer = fallbackHostServer;
            }

            public string GetRemoteMainURL(string fileName)
            {
                return $"{_defaultHostServer}/{fileName}";
            }

            public string GetRemoteFallbackURL(string fileName)
            {
                return $"{_fallbackHostServer}/{fileName}";
            }
        }
    }
}
