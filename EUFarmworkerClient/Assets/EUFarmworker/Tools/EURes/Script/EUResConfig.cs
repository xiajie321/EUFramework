using System.Collections.Generic;
using UnityEngine;
using YooAsset;

namespace EUFarmworker.Tools.EURes.Script
{
    [CreateAssetMenu(fileName = "EUResConfig", menuName = "EUFarmworker/EURes/Config")]
    public class EUResConfig : ScriptableObject
    {
        [Header("全局设置")]
        [Tooltip("运行模式 (编辑器模拟, 离线, 联机, WebGL)")]
        public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

        [Tooltip("默认资源服务器地址")]
        public string DefaultHostServer = "http://127.0.0.1/CDN";

        [Header("资源包设置")]
        [Tooltip("默认资源包名称 (通常为 'DefaultPackage')")]
        public string DefaultPackageName = "DefaultPackage";

        [Tooltip("额外资源包列表 (DLC, Mods 等)")]
        public List<PackageDefinition> AdditionalPackages = new List<PackageDefinition>();

        [System.Serializable]
        public class PackageDefinition
        {
            [Tooltip("包名")]
            public string PackageName;
            
            [Tooltip("包版本 (可选: 强制指定版本)")]
            public string PackageVersion; 
            
            [Tooltip("自动下载 (启动时下载)")]
            public bool AutoDownload = false; 
            
            [Tooltip("资源服务器地址 (可选: 覆盖默认地址)")]
            public string HostServer; 

            [Tooltip("是否从本地路径加载 (用于 Mod/本地测试)")]
            public bool LoadFromLocal = false;

            [Tooltip("本地资源路径 (绝对路径或相对于项目根目录)")]
            public string LocalPath;
        }
    }
}
