using System;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

namespace EUFramework.Extension.EURes
{
    /// <summary>
    /// EUResKit Package 配置
    /// 用于管理所有资源包的信息和加载方式
    /// </summary>
    [CreateAssetMenu(fileName = "EUResKitPackageConfig", menuName = "EUFramework/EUResKit Package Config", order = 0)]
    public class EUResKitPackageConfig : ScriptableObject
    {
        [Header("Package 配置列表")]
        [SerializeField]
        private List<PackageInfo> packages = new List<PackageInfo>();
        
        /// <summary>
        /// Package 信息
        /// </summary>
        [Serializable]
        public class PackageInfo
        {
            [Tooltip("资源包名称（需与 AssetBundleCollectorSetting 中的名称一致）")]
            public string packageName = "DefaultPackage";
            
            [Tooltip("运行模式")]
            public EPlayMode playMode = EPlayMode.EditorSimulateMode;
            
            [Tooltip("是否为默认包")]
            public bool isDefault = false;
            
            [Tooltip("包描述")]
            public string description = "";
        }
        
        /// <summary>
        /// 获取所有 Package 信息
        /// </summary>
        public List<PackageInfo> GetAllPackages()
        {
            return packages;
        }
        
        /// <summary>
        /// 获取默认 Package 名称
        /// </summary>
        public string GetDefaultPackageName()
        {
            // 优先返回标记为默认的 Package
            var defaultPkg = packages.Find(p => p.isDefault);
            if (defaultPkg != null)
                return defaultPkg.packageName;
            
            // 返回第一个 Package
            if (packages.Count > 0)
                return packages[0].packageName;
            
            return "DefaultPackage";
        }
        
        /// <summary>
        /// 获取 Package 信息
        /// </summary>
        public PackageInfo GetPackage(string packageName)
        {
            return packages.Find(p => p.packageName == packageName);
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// 添加 Package（仅供内部同步使用）
        /// 警告：请使用 SyncFromCollectorSettings 方法来管理 Package
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void AddPackage(string packageName, EPlayMode playMode = EPlayMode.EditorSimulateMode, bool isDefault = false)
        {
            if (packages.Exists(p => p.packageName == packageName))
            {
                Debug.LogWarning($"[EUResKitPackageConfig] Package '{packageName}' 已存在");
                return;
            }
            
            packages.Add(new PackageInfo
            {
                packageName = packageName,
                playMode = playMode,
                isDefault = isDefault
            });
        }
        
        /// <summary>
        /// 移除 Package（仅供内部同步使用）
        /// 警告：请使用 SyncFromCollectorSettings 方法来管理 Package
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void RemovePackage(string packageName)
        {
            packages.RemoveAll(p => p.packageName == packageName);
        }
        
#endif
        
        /// <summary>
        /// 设置默认 Package
        /// </summary>
        public void SetDefaultPackage(string packageName)
        {
            // 取消所有默认标记
            foreach (var pkg in packages)
            {
                pkg.isDefault = false;
            }
            
            // 设置新的默认
            var targetPkg = packages.Find(p => p.packageName == packageName);
            if (targetPkg != null)
            {
                targetPkg.isDefault = true;
            }
        }
        
        /// <summary>
        /// 验证配置
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            
            if (packages.Count == 0)
            {
                errorMessage = "至少需要配置一个 Package";
                return false;
            }
            
            // 检查是否有重复的 Package 名称
            var nameSet = new HashSet<string>();
            foreach (var pkg in packages)
            {
                if (string.IsNullOrEmpty(pkg.packageName))
                {
                    errorMessage = "Package 名称不能为空";
                    return false;
                }
                
                if (!nameSet.Add(pkg.packageName))
                {
                    errorMessage = $"存在重复的 Package 名称: {pkg.packageName}";
                    return false;
                }
            }
            
            return true;
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// 移除重复的 Package
        /// </summary>
        public void RemoveDuplicatePackages()
        {
            var uniquePackages = new List<PackageInfo>();
            var nameSet = new HashSet<string>();
            
            foreach (var pkg in packages)
            {
                if (!string.IsNullOrEmpty(pkg.packageName) && nameSet.Add(pkg.packageName))
                {
                    uniquePackages.Add(pkg);
                }
                else if (!string.IsNullOrEmpty(pkg.packageName))
                {
                    Debug.LogWarning($"[EUResKitPackageConfig] 移除重复的 Package: {pkg.packageName}");
                }
            }
            
            if (packages.Count != uniquePackages.Count)
            {
                packages.Clear();
                packages.AddRange(uniquePackages);
                Debug.Log($"[EUResKitPackageConfig] 已清理重复的 Package，当前共 {packages.Count} 个");
            }
        }
        
        private void OnValidate()
        {
            // 1. 移除重复的 Package
            RemoveDuplicatePackages();
            
            // 2. 确保只有一个默认 Package
            int defaultCount = 0;
            PackageInfo lastDefault = null;
            
            foreach (var pkg in packages)
            {
                if (pkg.isDefault)
                {
                    defaultCount++;
                    lastDefault = pkg;
                }
            }
            
            if (defaultCount > 1)
            {
                Debug.LogWarning("[EUResKitPackageConfig] 只能有一个默认 Package，已自动调整");
                foreach (var pkg in packages)
                {
                    pkg.isDefault = (pkg == lastDefault);
                }
            }
        }
#endif
    }
}
