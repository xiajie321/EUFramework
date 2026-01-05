using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
using Object = UnityEngine.Object;

namespace EUFarmworker.Tools.EURes.Script
{
    /// <summary>
    /// 资源加载器
    /// 支持 YooAsset 和 Resources 两种加载模式。
    /// 使用 ResLoader 加载的资源，会在 ResLoader 销毁或调用 ReleaseAll 时自动释放引用（仅限 YooAsset 模式）。
    /// </summary>
    public class ResLoader : IDisposable
    {
        // 已加载的资源句柄列表，用于统一管理释放 (YooAsset)
        private readonly List<AssetHandle> _handles = new List<AssetHandle>();
        
        // 正在进行的异步操作列表 (Resources)
        private readonly List<AsyncOperation> _asyncOperations = new List<AsyncOperation>();
        
        // 对象池，避免频繁创建 ResLoader 造成 GC
        private static readonly Stack<ResLoader> _pool = new Stack<ResLoader>();

        /// <summary>
        /// 当前加载器的资源加载模式
        /// </summary>
        public ResLoadMode LoadMode { get; private set; }

        /// <summary>
        /// 当前加载器使用的包名 (仅 YooAsset 模式有效)
        /// 如果为空，则使用 YooAsset 的默认包
        /// </summary>
        public string PackageName { get; private set; }

        /// <summary>
        /// 私有构造函数，强制使用 Allocate 获取实例
        /// </summary>
        private ResLoader() { }

        /// <summary>
        /// 从对象池分配一个加载器
        /// </summary>
        /// <param name="mode">指定加载模式，如果不指定则使用全局默认配置 EUResUtility.LoadMode</param>
        /// <param name="packageName">指定从哪个包加载 (仅 YooAsset 模式有效)</param>
        /// <returns>ResLoader 实例</returns>
        public static ResLoader Allocate(ResLoadMode? mode = null, string packageName = null)
        {
            ResLoader loader = _pool.Count > 0 ? _pool.Pop() : new ResLoader();
            loader.LoadMode = mode ?? EUResUtility.LoadMode;
            loader.PackageName = packageName;
            return loader;
        }

        /// <summary>
        /// 回收加载器到对象池
        /// 会自动释放当前加载器持有的所有资源
        /// </summary>
        public void Recycle()
        {
            ReleaseAll();
            _pool.Push(this);
        }

        /// <summary>
        /// 销毁/释放资源 (IDisposable 接口实现)
        /// 等同于 Recycle()
        /// </summary>
        public void Dispose()
        {
            Recycle();
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="address">资源地址（YooAsset Address 或 Resources Path）</param>
        /// <returns>资源对象，如果加载失败返回 null</returns>
        public T LoadSync<T>(string address) where T : Object
        {
            if (LoadMode == ResLoadMode.Resources)
            {
                return Resources.Load<T>(address);
            }
            else
            {
                AssetHandle handle;
                if (string.IsNullOrEmpty(PackageName))
                {
                    handle = YooAssets.LoadAssetSync<T>(address);
                }
                else
                {
                    var package = YooAssets.GetPackage(PackageName);
                    handle = package.LoadAssetSync<T>(address);
                }

                _handles.Add(handle);

                if (handle.Status == EOperationStatus.Succeed)
                {
                    return handle.AssetObject as T;
                }
                
                Debug.LogError($"[ResLoader] 加载资源失败: {address}, 状态: {handle.Status}, 错误: {handle.LastError}");
                return null;
            }
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="address">资源地址</param>
        /// <param name="onCompleted">加载完成回调</param>
        /// <param name="onProgress">加载进度回调</param>
        public void LoadAsync<T>(string address, Action<T> onCompleted, Action<float> onProgress = null) where T : Object
        {
            if (LoadMode == ResLoadMode.Resources)
            {
                var request = Resources.LoadAsync<T>(address);
                _asyncOperations.Add(request);

                if (onProgress != null)
                {
                    // Resources.LoadAsync 不支持每帧回调进度，只能通过协程或Update轮询
                    // 这里简化处理，仅在开始和结束时回调
                    // 如果需要精确进度，需要外部配合协程轮询 request.progress
                    // 为了不引入 MonoBehavior，这里暂时无法实现轮询进度
                    // 但我们可以利用 UniTask 或者类似的机制，不过为了保持 pure C#，暂不引入
                    // 实际上 Resources.LoadAsync 非常快，通常不需要进度条，这里仅做接口兼容
                    onProgress.Invoke(0);
                }
                
                request.completed += (op) =>
                {
                    onProgress?.Invoke(1);
                    onCompleted?.Invoke(request.asset as T);
                };
            }
            else
            {
                AssetHandle handle;
                if (string.IsNullOrEmpty(PackageName))
                {
                    handle = YooAssets.LoadAssetAsync<T>(address);
                }
                else
                {
                    var package = YooAssets.GetPackage(PackageName);
                    handle = package.LoadAssetAsync<T>(address);
                }

                _handles.Add(handle);

                handle.Completed += (h) =>
                {
                    if (h.Status == EOperationStatus.Succeed)
                    {
                        onCompleted?.Invoke(h.AssetObject as T);
                    }
                    else
                    {
                        Debug.LogError($"[ResLoader] 异步加载资源失败: {address}, 状态: {h.Status}, 错误: {h.LastError}");
                        onCompleted?.Invoke(null);
                    }
                };
            }
        }
        
        /// <summary>
        /// 加载场景
        /// </summary>
        /// <param name="address">场景地址</param>
        /// <param name="sceneMode">加载模式 (Single/Additive)</param>
        /// <param name="suspendLoad">是否挂起加载 (仅 YooAsset 支持)</param>
        /// <returns>场景句柄 (仅 YooAsset 模式返回有效句柄，Resources 模式返回 null)</returns>
        public SceneHandle LoadScene(string address, LoadSceneMode sceneMode = LoadSceneMode.Single, bool suspendLoad = false)
        {
            if (LoadMode == ResLoadMode.Resources)
            {
                // Resources 模式下使用 SceneManager 加载
                // 注意：Resources.Load 无法加载场景，场景必须在 Build Settings 中
                // 这里假设 address 是场景名称
                SceneManager.LoadScene(address, sceneMode);
                return null; 
            }
            else
            {
                // 场景加载通常由 YooAsset 内部管理，ResLoader 不持有 SceneHandle 的引用计数
                // 因为场景卸载通常是通过 UnloadSceneAsync 显式调用的
                SceneHandle handle;
                if (string.IsNullOrEmpty(PackageName))
                {
                    handle = YooAssets.LoadSceneAsync(address, sceneMode, LocalPhysicsMode.None, suspendLoad);
                }
                else
                {
                    var package = YooAssets.GetPackage(PackageName);
                    handle = package.LoadSceneAsync(address, sceneMode, LocalPhysicsMode.None, suspendLoad);
                }
                return handle;
            }
        }

        /// <summary>
        /// 释放所有已加载的资源
        /// </summary>
        public void ReleaseAll()
        {
            if (LoadMode == ResLoadMode.YooAsset)
            {
                foreach (var handle in _handles)
                {
                    if (handle != null)
                    {
                        handle.Release();
                    }
                }
                _handles.Clear();
            }
            else
            {
                // Resources 模式下，通常不需要手动释放 Load 加载的资源引用
                // 如果需要释放未使用的资源，可以调用 Resources.UnloadUnusedAssets()
                // 但这通常由全局管理，而不是单个 Loader 管理
                _asyncOperations.Clear();
            }
        }
    }
}
