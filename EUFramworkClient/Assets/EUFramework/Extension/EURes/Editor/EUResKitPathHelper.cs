#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EUFramework.Extension.EURes.Editor
{
    /// <summary>
    /// EUResKit 路径管理工具
    /// 提供动态路径查找和命名空间计算功能
    /// </summary>
    public static class EUResKitPathHelper
    {
        private static string _moduleRoot;
        private static string _namespace;
        
        /// <summary>
        /// 获取模块根目录（通过查找 EURes.asmdef 文件位置）
        /// </summary>
        public static string GetModuleRoot()
        {
            if (!string.IsNullOrEmpty(_moduleRoot))
                return _moduleRoot;
            
            // 查找 EURes.asmdef 文件
            string[] asmdefGuids = AssetDatabase.FindAssets("EURes t:asmdef");
            foreach (var guid in asmdefGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(path);
                
                // 确保是 EURes.asmdef 而不是 EURes.Editor.asmdef
                if (fileName == "EURes.asmdef")
                {
                    _moduleRoot = Path.GetDirectoryName(path).Replace("\\", "/");
                    break;
                }
            }
            
            if (string.IsNullOrEmpty(_moduleRoot))
            {
                Debug.LogError("[EUResKit] 无法找到 EURes.asmdef 文件，请确保模块结构完整");
            }
            
            return _moduleRoot;
        }
        
        /// <summary>
        /// 获取命名空间（排除 Assets 后的路径，用点号连接）
        /// 例如：Assets/EUFramework/Extension/EURes -> EUFramework.Extension.EURes
        /// </summary>
        public static string GetNamespace()
        {
            if (!string.IsNullOrEmpty(_namespace))
                return _namespace;
            
            string root = GetModuleRoot();
            if (string.IsNullOrEmpty(root))
                return "EUResKit"; // 默认命名空间
            
            // 移除 Assets/ 前缀
            string withoutAssets = root.Replace("Assets/", "").Replace("Assets\\", "");
            
            // 将路径分隔符替换为点号
            _namespace = withoutAssets.Replace("/", ".").Replace("\\", ".");
            
            return _namespace;
        }
        
        /// <summary>
        /// 获取 Resources 文件夹路径
        /// </summary>
        public static string GetResourcesPath()
        {
            string root = GetModuleRoot();
            if (string.IsNullOrEmpty(root))
                return "Assets/Resources";
            
            return Path.Combine(root, "Resources").Replace("\\", "/");
        }
        
        /// <summary>
        /// 获取配置文件存储路径
        /// </summary>
        public static string GetSettingsPath()
        {
            return Path.Combine(GetResourcesPath(), "EUResKitSettings").Replace("\\", "/");
        }
        
        /// <summary>
        /// 获取 Editor 文件夹路径
        /// </summary>
        public static string GetEditorPath()
        {
            string root = GetModuleRoot();
            if (string.IsNullOrEmpty(root))
                return "Assets/Editor";
            
            return Path.Combine(root, "Editor").Replace("\\", "/");
        }
        
        /// <summary>
        /// 获取 Script 文件夹路径
        /// </summary>
        public static string GetScriptPath()
        {
            string root = GetModuleRoot();
            if (string.IsNullOrEmpty(root))
                return "Assets/Scripts";
            
            return Path.Combine(root, "Script").Replace("\\", "/");
        }
        
        /// <summary>
        /// 获取模板文件夹路径
        /// </summary>
        public static string GetTemplatesPath()
        {
            return Path.Combine(GetEditorPath(), "Templates").Replace("\\", "/");
        }
        
        /// <summary>
        /// 清除缓存（当模块位置改变时调用）
        /// </summary>
        public static void ClearCache()
        {
            _moduleRoot = null;
            _namespace = null;
        }
        
        /// <summary>
        /// 验证模块结构是否完整
        /// </summary>
        public static bool ValidateModuleStructure(out string errorMessage)
        {
            errorMessage = string.Empty;
            
            string root = GetModuleRoot();
            if (string.IsNullOrEmpty(root))
            {
                errorMessage = "无法找到模块根目录（EURes.asmdef）";
                return false;
            }
            
            // 检查必要的文件夹
            string[] requiredFolders = new[]
            {
                "Editor",
                "Script",
                "Resources"
            };
            
            foreach (var folder in requiredFolders)
            {
                string path = Path.Combine(root, folder);
                if (!Directory.Exists(path))
                {
                    errorMessage = $"缺少必要的文件夹: {folder}";
                    return false;
                }
            }
            
            return true;
        }
    }
}
#endif
