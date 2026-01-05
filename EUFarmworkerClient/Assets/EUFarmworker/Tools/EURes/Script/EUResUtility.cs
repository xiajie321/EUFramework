using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EUFarmworker.Core.Abstracts;
using UnityEngine;
using YooAsset;

namespace EUFarmworker.Tools.EURes.Script
{
    /// <summary>
    /// 资源加载模式
    /// </summary>
    public enum ResLoadMode
    {
        /// <summary>
        /// 使用 UnityEngine.Resources 加载
        /// </summary>
        Resources,
        
        /// <summary>
        /// 使用 YooAsset 加载
        /// </summary>
        YooAsset
    }

    /// <summary>
    /// 资源管理模块
    /// 负责提供资源加载器的创建与管理，作为架构中的 Utility 存在。
    /// 同时也负责管理资源包（Package）的生命周期。
    /// </summary>
    public class EUResUtility : AbstractUtility
    {
        /// <summary>
        /// 当前资源加载模式
        /// 默认为 YooAsset
        /// </summary>
        public static ResLoadMode LoadMode { get; set; } = ResLoadMode.YooAsset;

        /// <summary>
        /// 单例实例 (方便访问)
        /// </summary>
        public static EUResUtility Instance { get; private set; }

        /// <summary>
        /// 全局配置
        /// </summary>
        private EUResConfig _config;

        /// <summary>
        /// 管理的所有资源包
        /// </summary>
        private readonly Dictionary<string, EUResPackage> _packages = new Dictionary<string, EUResPackage>();

        /// <summary>
        /// 初始化
        /// </summary>
        public override void Init()
        {
            Instance = this;

            // 加载配置
            _config = Resources.Load<EUResConfig>("EUResConfig");
            if (_config == null)
            {
                Debug.LogWarning("[EURes] EUResConfig not found in Resources. Using default settings.");
                _config = ScriptableObject.CreateInstance<EUResConfig>();
            }

            // 初始化 YooAsset
            if (!YooAssets.Initialized)
            {
                YooAssets.Initialize();
            }
        }

        /// <summary>
        /// 启动初始化流程 (UniTask)
        /// 建议在游戏启动时调用
        /// </summary>
        public async UniTask InitializePackagesAsync()
        {
            if (LoadMode == ResLoadMode.Resources)
                return;

            // 1. 初始化默认包
            var defaultPackage = new EUResPackage(_config.DefaultPackageName, _config.DefaultHostServer, _config.PlayMode);
            await defaultPackage.InitializeAsync();
            
            if (defaultPackage.IsInitialized)
            {
                _packages[_config.DefaultPackageName] = defaultPackage;
                YooAssets.SetDefaultPackage(defaultPackage.Package);
            }

            // 2. 初始化额外包
            foreach (var pkgDef in _config.AdditionalPackages)
            {
                string host = string.IsNullOrEmpty(pkgDef.HostServer) ? _config.DefaultHostServer : pkgDef.HostServer;
                string localPath = pkgDef.LoadFromLocal ? pkgDef.LocalPath : null;
                
                var package = new EUResPackage(pkgDef.PackageName, host, _config.PlayMode, localPath);
                await package.InitializeAsync();
                
                if (package.IsInitialized)
                {
                    _packages[pkgDef.PackageName] = package;
                }
            }
        }

        /// <summary>
        /// 获取资源包包装器
        /// </summary>
        public EUResPackage GetPackage(string packageName)
        {
            if (_packages.TryGetValue(packageName, out var package))
            {
                return package;
            }
            Debug.LogError($"[EURes] Package '{packageName}' not found or not initialized.");
            return null;
        }

        /// <summary>
        /// 动态添加/加载一个资源包 (用于 DLC 或 Mod)
        /// </summary>
        public async UniTask LoadPackageAsync(string packageName, string hostServer = null, string localPath = null)
        {
            if (_packages.ContainsKey(packageName))
            {
                return;
            }

            string host = string.IsNullOrEmpty(hostServer) ? _config.DefaultHostServer : hostServer;
            var package = new EUResPackage(packageName, host, _config.PlayMode, localPath);
            await package.InitializeAsync();

            if (package.IsInitialized)
            {
                _packages[packageName] = package;
            }
        }

        /// <summary>
        /// 创建一个资源加载器
        /// </summary>
        /// <param name="mode">指定加载模式，如果不指定则使用全局默认配置 LoadMode</param>
        /// <param name="packageName">指定从哪个包加载 (仅 YooAsset 模式有效)，默认为 DefaultPackage</param>
        /// <returns>资源加载器实例</returns>
        public static ResLoader CreateLoader(ResLoadMode? mode = null, string packageName = null)
        {
            return ResLoader.Allocate(mode, packageName);
        }
    }
}
