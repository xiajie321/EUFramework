using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// EUUI 模板注册表生成器
    /// 自动扫描 Templates 目录并生成/更新注册表资产
    /// </summary>
    public static class EUUITemplateRegistryGenerator
    {
        private const string RegistryAssetName = "EUUITemplateRegistry.asset";

        public static void RefreshRegistry()
        {
            try
            {
                string templatesDir = EUUITemplateLocator.GetTemplatesDirectory();
                if (string.IsNullOrEmpty(templatesDir))
                {
                    Debug.LogError("[EUUI] 无法定位 Templates 目录");
                    return;
                }

                var registry = GetOrCreateRegistry();
                var templates = ScanTemplates(templatesDir);
                registry.templates = templates;

                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[EUUI] 模板注册表已更新：{templates.Count} 个模板\n路径: {GetRegistryAssetPath()}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUUI] 刷新模板注册表失败: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 扫描 Templates/Sbn/ 目录下的所有 .sbn 文件，生成 ID → 相对路径 列表
        /// </summary>
        private static List<EUUITemplateInfo> ScanTemplates(string templatesDir)
        {
            var templates = new List<EUUITemplateInfo>();

            string fullTemplatesDir = Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(Application.dataPath), templatesDir));

            if (!Directory.Exists(fullTemplatesDir))
            {
                Debug.LogWarning($"[EUUI] Templates 目录不存在: {fullTemplatesDir}");
                return templates;
            }

            var sbnFiles = Directory.GetFiles(fullTemplatesDir, "*.sbn", SearchOption.AllDirectories);

            foreach (var filePath in sbnFiles)
            {
                string relativePath = Path.GetRelativePath(fullTemplatesDir, filePath)
                    .Replace("\\", "/")
                    .Replace(".sbn", "");

                templates.Add(new EUUITemplateInfo
                {
                    id   = GenerateTemplateId(relativePath),
                    path = relativePath
                });
            }

            return templates.OrderBy(t => t.id).ToList();
        }

        /// <summary>
        /// 以文件名（无扩展名、无特殊字符）作为模板 ID
        /// </summary>
        private static string GenerateTemplateId(string relativePath)
        {
            return Path.GetFileName(relativePath).Replace(".", "").Replace("-", "").Replace("_", "");
        }

        /// <summary>
        /// 获取或创建注册表资产
        /// </summary>
        private static EUUITemplateRegistryAsset GetOrCreateRegistry()
        {
            string assetPath = GetRegistryAssetPath();
            
            // 尝试加载现有资产
            var registry = AssetDatabase.LoadAssetAtPath<EUUITemplateRegistryAsset>(assetPath);
            
            if (registry == null)
            {
                // 创建新资产
                registry = ScriptableObject.CreateInstance<EUUITemplateRegistryAsset>();
                
                // 确保目录存在
                string directory = Path.GetDirectoryName(assetPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                AssetDatabase.CreateAsset(registry, assetPath);
                Debug.Log($"[EUUI] 创建模板注册表资产: {assetPath}");
            }
            
            return registry;
        }

        /// <summary>
        /// 获取注册表资产路径
        /// </summary>
        private static string GetRegistryAssetPath()
        {
            string editorDir = EUUITemplateLocator.GetEditorDirectory();
            return Path.Combine(editorDir, "EditorSO", RegistryAssetName).Replace("\\", "/");
        }

        /// <summary>
        /// 检查注册表是否需要更新（ID 集合发生变化则需要）
        /// </summary>
        public static bool NeedsUpdate()
        {
            string assetPath = GetRegistryAssetPath();
            var registry = AssetDatabase.LoadAssetAtPath<EUUITemplateRegistryAsset>(assetPath);

            if (registry == null)
                return true;

            string templatesDir = EUUITemplateLocator.GetTemplatesDirectory();
            var currentTemplates = ScanTemplates(templatesDir);

            if (registry.templates.Count != currentTemplates.Count)
                return true;

            var registryIds = registry.templates.Select(t => t.id).OrderBy(id => id);
            var currentIds  = currentTemplates.Select(t => t.id).OrderBy(id => id);

            return !registryIds.SequenceEqual(currentIds);
        }
    }
}
