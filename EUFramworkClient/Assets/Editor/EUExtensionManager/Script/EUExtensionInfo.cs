#if UNITY_EDITOR
using System;

namespace EUFramework.Extension.ExtensionManagerKit.Editor
{
    [Serializable]
    public class EUDependency
    {
        public string name;
        public string gitUrl; // 可选，如果为空则尝试从社区仓库查找
        public string installPath;
        public string version; // 最低版本要求
    }

    [Serializable]
    public class EUExtensionInfo
    {
        public string name;
        public string displayName;
        public string version;
        public string description;
        public string author;
        public string category;
        public string downloadUrl; // 远程跳转地址
        public string sourceUrl; // 安装来源 URL (用于冲突检查)
        public EUDependency[] dependencies; // 依赖项
        
        // 非序列化字段
        [NonSerialized]
        public string folderPath; 
        [NonSerialized]
        public bool isInstalled;
        [NonSerialized]
        public string remoteVersion; // 记录对应的远程版本号用于对比
        [NonSerialized]
        public string remoteFolderName; // 远程仓库中的实际文件夹名称
    }
}
#endif
