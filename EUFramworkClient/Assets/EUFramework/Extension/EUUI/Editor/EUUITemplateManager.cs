using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// EUUI 模板路径统一管理器
    /// 负责所有 .sbn 模板文件的路径解析
    /// 基于程序集定义文件 (EUUI.Editor.asmdef) 进行路径定位，确保跨环境兼容
    /// </summary>
    public static class EUUITemplateManager
    {
        /// <summary>
        /// 模板类型枚举
        /// </summary>
        public enum TemplateType
        {
            // Panel 相关
            PanelGenerated,         // 面板生成模板
            MVCArchitecture,        // MVC 架构集成模板
            
            // PanelBase 扩展相关
            PanelBaseEURes,         // PanelBase EURes 扩展模板
            
            // UIKit 扩展相关
            KitEURes,               // UIKit EURes 扩展模板
            KitCustom               // UIKit Custom 默认模板
        }

        private static string _cachedEditorDirectory;
        private static EUUIEditorConfig _cachedConfig;
        private static EUUITemplateRegistryAsset _cachedRegistry;

        /// <summary>
        /// 通过 EUUI.Editor.asmdef 程序集定义文件获取编辑器目录
        /// 相比查找具体脚本文件更稳定，支持源码、Package、DLL 等多种部署方式
        /// </summary>
        public static string GetEditorDirectory()
        {
            if (!string.IsNullOrEmpty(_cachedEditorDirectory))
                return _cachedEditorDirectory;

            // 优先查找 EUUI.Editor.asmdef
            string[] guids = AssetDatabase.FindAssets("EUUI.Editor t:AssemblyDefinitionAsset");
            if (guids != null && guids.Length > 0)
            {
                string asmdefPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                _cachedEditorDirectory = Path.GetDirectoryName(asmdefPath)?.Replace("\\", "/");
                return _cachedEditorDirectory;
            }

            // Fallback: 通过运行时程序集 EUUI.asmdef 推断 Editor 目录
            guids = AssetDatabase.FindAssets("EUUI t:AssemblyDefinitionAsset");
            if (guids != null && guids.Length > 0)
            {
                // 过滤掉 EUUI.Editor.asmdef（名称包含 "Editor"）
                foreach (string guid in guids)
                {
                    string asmdefPath = AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = Path.GetFileName(asmdefPath);
                    
                    // 只取运行时程序集（EUUI.asmdef）
                    if (fileName == "EUUI.asmdef")
                    {
                        string euuiRootDir = Path.GetDirectoryName(asmdefPath)?.Replace("\\", "/");
                        _cachedEditorDirectory = Path.Combine(euuiRootDir, "Editor").Replace("\\", "/");
                        Debug.LogWarning($"[EUUI] 未找到 EUUI.Editor.asmdef，通过运行时程序集推断路径: {_cachedEditorDirectory}");
                        return _cachedEditorDirectory;
                    }
                }
            }

            Debug.LogError("[EUUI] 无法找到 EUUI 程序集定义文件 (EUUI.Editor.asmdef 或 EUUI.asmdef)");
            return null;
        }

        /// <summary>
        /// 获取模板根目录路径（Sbn/ 目录）
        /// </summary>
        public static string GetTemplatesDirectory()
        {
            string editorDir = GetEditorDirectory();
            if (string.IsNullOrEmpty(editorDir))
                return null;
                
            return Path.Combine(editorDir, "Templates", "Sbn").Replace("\\", "/");
        }

        /// <summary>
        /// 获取标准模板的完整路径
        /// </summary>
        /// <param name="type">模板类型</param>
        /// <param name="config">编辑器配置（为空时自动查找）</param>
        /// <returns>模板文件的完整路径</returns>
        /// <exception cref="ArgumentException">未知的模板类型</exception>
        /// <exception cref="FileNotFoundException">模板文件不存在</exception>
        public static string GetTemplatePath(TemplateType type, EUUIEditorConfig config = null)
        {
            config ??= GetConfig();
            if (config == null)
            {
                throw new InvalidOperationException("无法找到 EUUIEditorConfig 配置文件");
            }

            string templateId = type.ToString();

            // 1. 检查用户是否覆盖了这个模板
            var customOverride = config.templateOverrides?
                .FirstOrDefault(o => o.enabled && o.templateId == templateId);
            
            if (customOverride != null && !string.IsNullOrEmpty(customOverride.customPath))
            {
                Debug.Log($"[EUUI] 使用自定义模板覆盖: {templateId} -> {customOverride.customPath}");
                return ValidateAbsolutePath(customOverride.customPath);
            }

            // 2. 从模板注册表获取默认路径
            var registry = GetTemplateRegistry();
            if (registry == null)
            {
                throw new InvalidOperationException("无法找到模板注册表！将自动生成...");
            }

            string relativePath = registry.GetTemplatePath(templateId);
            
            if (string.IsNullOrEmpty(relativePath))
            {
                throw new ArgumentException($"模板 '{templateId}' 未在注册表中找到！\n" +
                    $"可用模板: {string.Join(", ", registry.templates.Select(t => t.id))}\n" +
                    $"请通过 EUUI 配置工具刷新模板注册表");
            }

            // 3. 解析为完整路径
            return ResolveTemplatePath(relativePath);
        }

        /// <summary>
        /// 获取模板注册表
        /// </summary>
        private static EUUITemplateRegistryAsset GetTemplateRegistry()
        {
            if (_cachedRegistry != null)
                return _cachedRegistry;

            string editorDir = GetEditorDirectory();
            if (string.IsNullOrEmpty(editorDir))
                return null;

            string registryPath = Path.Combine(editorDir, "EUUITemplateRegistry.asset").Replace("\\", "/");
            _cachedRegistry = AssetDatabase.LoadAssetAtPath<EUUITemplateRegistryAsset>(registryPath);
            
            if (_cachedRegistry == null)
            {
                Debug.LogWarning($"[EUUI] 未找到模板注册表: {registryPath}\n将自动生成...");
                EUUITemplateRegistryGenerator.RefreshRegistry();
                _cachedRegistry = AssetDatabase.LoadAssetAtPath<EUUITemplateRegistryAsset>(registryPath);
            }
            
            return _cachedRegistry;
        }

        /// <summary>
        /// 判断路径是否为相对路径
        /// </summary>
        private static bool IsRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // 相对路径：不以 "Assets/" 或 "Packages/" 开头，且不是根路径
            return !path.StartsWith("Assets/") && 
                   !path.StartsWith("Packages/") &&
                   !Path.IsPathRooted(path);
        }

        /// <summary>
        /// 验证并获取绝对路径的完整路径
        /// </summary>
        private static string ValidateAbsolutePath(string assetPath)
        {
            string fullPath = Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath));

            // 添加 .sbn 扩展名（如果没有）
            if (!fullPath.EndsWith(".sbn"))
            {
                fullPath += ".sbn";
            }

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"自定义模板文件不存在: {fullPath}");

            return fullPath;
        }

        /// <summary>
        /// 获取配置文件
        /// </summary>
        private static EUUIEditorConfig GetConfig()
        {
            if (_cachedConfig != null)
                return _cachedConfig;

            string[] guids = AssetDatabase.FindAssets("t:EUUIEditorConfig");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path != null && path.EndsWith("EUUIEditorConfig.asset", StringComparison.OrdinalIgnoreCase))
                {
                    _cachedConfig = AssetDatabase.LoadAssetAtPath<EUUIEditorConfig>(path);
                    return _cachedConfig;
                }
            }
            
            Debug.LogError("[EUUI] 未找到 EUUIEditorConfig 配置文件");
            return null;
        }

        /// <summary>
        /// 获取自定义模板的完整路径（用户指定的 .sbn 文件）
        /// </summary>
        /// <param name="assetRelativePath">相对于 Assets 的路径，如 "Assets/Game/UI/MyTemplate.sbn"</param>
        /// <returns>模板文件的完整路径</returns>
        /// <exception cref="ArgumentException">模板路径为空</exception>
        /// <exception cref="FileNotFoundException">模板文件不存在</exception>
        public static string GetCustomTemplatePath(string assetRelativePath)
        {
            if (string.IsNullOrEmpty(assetRelativePath))
                throw new ArgumentException("模板路径不能为空");

            string fullPath = Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(Application.dataPath), assetRelativePath));

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"自定义模板文件不存在: {fullPath}");

            return fullPath;
        }

        /// <summary>
        /// 解析模板相对路径为完整路径（相对路径基于 Templates/Sbn/ 下的子路径，如 Static/PanelBase/EURes）
        /// </summary>
        private static string ResolveTemplatePath(string relativePath)
        {
            string editorDir = GetEditorDirectory();
            if (string.IsNullOrEmpty(editorDir))
                throw new InvalidOperationException("无法定位编辑器脚本目录");

            // 模板实际存放在 Templates/Sbn/ 下，相对路径不含 Sbn 前缀
            string templatePath = Path.Combine(editorDir, "Templates", "Sbn", $"{relativePath}.sbn").Replace("\\", "/");
            string fullPath = Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(Application.dataPath), templatePath));

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"模板文件不存在: {fullPath}\n相对路径: {relativePath}");

            return fullPath;
        }

        /// <summary>
        /// 获取模板类型的相对路径（从注册表读取）
        /// </summary>
        public static string GetTemplateRelativePath(TemplateType type)
        {
            var registry = GetTemplateRegistry();
            if (registry == null)
            {
                throw new InvalidOperationException("无法找到模板注册表");
            }
            
            string templateId = type.ToString();
            string relativePath = registry.GetTemplatePath(templateId);
            
            if (string.IsNullOrEmpty(relativePath))
            {
                throw new ArgumentException($"模板 '{templateId}' 未在注册表中找到");
            }
            
            return relativePath;
        }

        /// <summary>
        /// 获取模板注册表资产（用于编辑器 UI 显示）
        /// </summary>
        public static EUUITemplateRegistryAsset GetRegistryAsset()
        {
            return GetTemplateRegistry();
        }

        /// <summary>
        /// 获取所有模板类型（枚举所有值）
        /// </summary>
        public static IEnumerable<TemplateType> GetAllTemplateTypes()
        {
            return Enum.GetValues(typeof(TemplateType)).Cast<TemplateType>();
        }

        /// <summary>
        /// 验证模板文件是否存在
        /// </summary>
        public static bool TemplateExists(TemplateType type, EUUIEditorConfig config = null)
        {
            try
            {
                GetTemplatePath(type, config);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 验证自定义模板文件是否存在
        /// </summary>
        public static bool CustomTemplateExists(string assetRelativePath)
        {
            try
            {
                GetCustomTemplatePath(assetRelativePath);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
