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
        
        // 对象池，避免频繁创建 ResLoader 造成 GC
        private static readonly Stack<ResLoader> _pool = new Stack<ResLoader>();

        /// <summary>
        /// 当前加载器的资源加载模式
        /// </summary>
        public ResLoadMode LoadMode { get; private set; }

        /// <summary>
        /// 私有构造函数，强制使用 Allocate 获取实例
        /// </summary>
        private ResLoader() { }

        /// <summary>
        /// 从对象池分配一个加载器
        /// </summary>
        /// <param name="mode">指定加载模式，如果不指定则使用全局默认配置 EUResUtility.LoadMode</param>
        /// <returns>ResLoader 实例</returns>
        public static ResLoader Allocate(ResLoadMode? mode = null)
        {
            ResLoader loader = _pool.Count > 0 ? _pool.Pop() : new ResLoader();
            loader.LoadMode = mode ?? EUResUtility.LoadMode;
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
                var handle = YooAssets.LoadAssetSync<T>(address);
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
        public void LoadAsync<T>(string address, Action<T> onCompleted) where T : Object
        {
            if (EUFarmworker.Tools.EURes.Script.EUResUtility.LoadMode == EUFarmworker.Tools.EURes.Script.ResLoadMode.Resources)
            {
                var request = Resources.LoadAsync<T>(address);
                request.completed += (op) =>
                {
                    onCompleted?.Invoke(request.asset as T);
                };
            }
            else
            {
                var handle = YooAssets.LoadAssetAsync<T>(address);
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
            if (EUFarmworker.Tools.EURes.Script.EUResUtility.LoadMode == EUFarmworker.Tools.EURes.Script.ResLoadMode.Resources)
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
                var handle = YooAssets.LoadSceneAsync(address, sceneMode, LocalPhysicsMode.None, suspendLoad);
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
            }
        }
    }
}
