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
                string templatesDir = EUUITemplateManager.GetTemplatesDirectory();
                if (string.IsNullOrEmpty(templatesDir))
                {
                    Debug.LogError("[EUUI] 无法定位 Templates 目录");
                    return;
                }

                // 先获取或创建注册表资产（用于保留已有模板信息）
                var registry = GetOrCreateRegistry();
                
                // 扫描所有模板（传入现有注册表以保留已有信息）
                var templates = ScanTemplates(templatesDir, registry);
                
                // 更新注册表
                registry.version = "1.0.0";
                registry.lastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                registry.templatesDirectory = templatesDir;
                registry.templates = templates;

                // 保存资产
                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[EUUI] ✅ 模板注册表已更新: {templates.Count} 个模板\n路径: {GetRegistryAssetPath()}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUUI] 刷新模板注册表失败: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 扫描 Templates 目录下的所有 .sbn 文件
        /// </summary>
        /// <param name="templatesDir">模板目录</param>
        /// <param name="existingRegistry">现有注册表（用于保留已有模板信息，如名称、描述等）</param>
        private static List<EUUITemplateInfo> ScanTemplates(string templatesDir, EUUITemplateRegistryAsset existingRegistry = null)
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
            
            // 构建现有模板的查找字典（按 ID），用于保留用户已修改的信息
            var existingTemplatesMap = new Dictionary<string, EUUITemplateInfo>();
            if (existingRegistry != null && existingRegistry.templates != null)
            {
                foreach (var existing in existingRegistry.templates)
                {
                    existingTemplatesMap[existing.id] = existing;
                }
            }
            
            foreach (var filePath in sbnFiles)
            {
                // 获取相对于 Templates 目录的路径（不含扩展名）
                string relativePath = Path.GetRelativePath(fullTemplatesDir, filePath)
                    .Replace("\\", "/")
                    .Replace(".sbn", "");

                // 生成模板 ID
                string id = GenerateTemplateId(relativePath);
                
                // 获取分类（目录名）
                string category = Path.GetDirectoryName(relativePath)?.Replace("\\", "/") ?? "";

                // 创建模板信息
                EUUITemplateInfo template;
                
                // 如果注册表中已有此模板，保留已有信息（用户可能已修改名称、描述等）
                if (existingTemplatesMap.TryGetValue(id, out var existing))
                {
                    template = new EUUITemplateInfo
                    {
                        id = id,
                        name = existing.name,           // 保留已有名称
                        category = category,            // 更新分类（路径可能变化）
                        path = relativePath,            // 更新路径
                        description = existing.description,  // 保留已有描述
                        required = existing.required    // 保留已有必需标记
                    };
                }
                else
                {
                    // 新模板，使用默认值
                    string fileName = Path.GetFileName(relativePath);
                    template = new EUUITemplateInfo
                    {
                        id = id,
                        name = fileName,      // 使用文件名作为默认名称
                        category = category,
                        path = relativePath,
                        description = "",      // 默认空描述
                        required = false       // 默认非必需
                    };
                }

                templates.Add(template);
            }

            return templates.OrderBy(t => t.category).ThenBy(t => t.name).ToList();
        }

        /// <summary>
        /// 根据相对路径生成模板 ID
        /// </summary>
        private static string GenerateTemplateId(string relativePath)
        {
            // 移除路径前缀，只保留文件名（无扩展名）
            string fileName = Path.GetFileName(relativePath);
            
            // 特殊处理已知的核心模板（新目录结构：Sbn/WithData/, Sbn/Static/）
            if (fileName == "EUUIPanel.Generated") return "PanelGenerated";
            if (fileName == "MVC") return "MVCArchitecture";
            if (fileName == "EURes" && relativePath.Contains("PanelBase")) return "PanelBaseEURes";
            if (fileName == "EURes" && relativePath.Contains("UIKit")) return "KitEURes";
            if (fileName == "Custom") return "KitCustom";
            
            // 其他模板使用文件名作为 ID（移除特殊字符）
            return fileName.Replace(".", "").Replace("-", "").Replace("_", "");
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
            string editorDir = EUUITemplateManager.GetEditorDirectory();
            return Path.Combine(editorDir, RegistryAssetName).Replace("\\", "/");
        }

        /// <summary>
        /// 验证注册表是否需要更新
        /// </summary>
        public static bool NeedsUpdate()
        {
            string assetPath = GetRegistryAssetPath();
            var registry = AssetDatabase.LoadAssetAtPath<EUUITemplateRegistryAsset>(assetPath);
            
            if (registry == null)
                return true;

            // 检查模板文件是否有变化
            string templatesDir = EUUITemplateManager.GetTemplatesDirectory();
            var currentTemplates = ScanTemplates(templatesDir, registry);
            
            // 简单对比：数量不同或 ID 不匹配
            if (registry.templates.Count != currentTemplates.Count)
                return true;
            
            var registryIds = registry.templates.Select(t => t.id).OrderBy(id => id);
            var currentIds = currentTemplates.Select(t => t.id).OrderBy(id => id);
            
            return !registryIds.SequenceEqual(currentIds);
        }
    }
}
