using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// EUUI 模板路径基础定位器
    /// 通过 EUUI.Editor.asmdef 程序集定义文件确定 Editor 目录，
    /// 并提供 Templates/Sbn/ 目录与注册表资产的访问入口。
    /// 路径解析（ID → 完整路径）由 EUUIBaseExporter 负责。
    /// </summary>
    public static class EUUITemplateLocator
    {
        private static string _cachedEditorDirectory;
        private static EUUITemplateRegistryAsset _cachedRegistry;
        private static EUUITemplateConfig _cachedTemplateConfig;

        /// <summary>
        /// 通过 EUUI.Editor.asmdef 程序集定义文件获取 Editor 根目录（Assets 相对路径）
        /// </summary>
        public static string GetEditorDirectory()
        {
            if (!string.IsNullOrEmpty(_cachedEditorDirectory))
                return _cachedEditorDirectory;

            string[] guids = AssetDatabase.FindAssets("EUUI.Editor t:AssemblyDefinitionAsset");
            if (guids != null && guids.Length > 0)
            {
                string asmdefPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                _cachedEditorDirectory = Path.GetDirectoryName(asmdefPath)?.Replace("\\", "/");
                return _cachedEditorDirectory;
            }

            // Fallback：通过运行时 EUUI.asmdef 推断 Editor 子目录
            guids = AssetDatabase.FindAssets("EUUI t:AssemblyDefinitionAsset");
            if (guids != null && guids.Length > 0)
            {
                foreach (string guid in guids)
                {
                    string asmdefPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (Path.GetFileName(asmdefPath) == "EUUI.asmdef")
                    {
                        string euuiRoot = Path.GetDirectoryName(asmdefPath)?.Replace("\\", "/");
                        _cachedEditorDirectory = Path.Combine(euuiRoot, "Editor").Replace("\\", "/");
                        Debug.LogWarning($"[EUUI] 未找到 EUUI.Editor.asmdef，通过运行时程序集推断路径: {_cachedEditorDirectory}");
                        return _cachedEditorDirectory;
                    }
                }
            }

            Debug.LogError("[EUUI] 无法找到程序集定义文件（EUUI.Editor.asmdef 或 EUUI.asmdef）");
            return null;
        }

        /// <summary>
        /// 返回用户自定义扩展模板的备用存放目录（Templates/Extensions/）
        /// 框架内置扩展直接放在 Templates/Sbn/Static/PanelBase 或 Static/UIKit 下；
        /// 此目录供 autoDiscoverExtensions 模式扫描使用。
        /// </summary>
        public static string GetExtensionsDirectory()
        {
            string editorDir = GetEditorDirectory();
            return string.IsNullOrEmpty(editorDir)
                ? "Assets/EUFramework/Extension/EUUI/Editor/Templates/Extensions"
                : $"{editorDir}/Templates/Extensions";
        }

        /// <summary>
        /// 返回 .sbn 模板根目录（Assets 相对路径：Editor/Templates/Sbn/）
        /// </summary>
        public static string GetTemplatesDirectory()
        {
            string editorDir = GetEditorDirectory();
            if (string.IsNullOrEmpty(editorDir))
                return null;

            return Path.Combine(editorDir, "Templates", "Sbn").Replace("\\", "/");
        }

        /// <summary>
        /// 返回模板与代码生成配置（EUUITemplateConfig.asset），未找到时返回 null
        /// </summary>
        public static EUUITemplateConfig GetTemplateConfig()
        {
            if (_cachedTemplateConfig != null) return _cachedTemplateConfig;

            string[] guids = AssetDatabase.FindAssets("EUUITemplateConfig t:EUUITemplateConfig");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path != null && path.EndsWith("EUUITemplateConfig.asset", StringComparison.OrdinalIgnoreCase))
                {
                    _cachedTemplateConfig = AssetDatabase.LoadAssetAtPath<EUUITemplateConfig>(path);
                    return _cachedTemplateConfig;
                }
            }

            string editorDir = GetEditorDirectory();
            if (!string.IsNullOrEmpty(editorDir))
            {
                string fallback = Path.Combine(editorDir, "EditorSO", "EUUITemplateConfig.asset").Replace("\\", "/");
                _cachedTemplateConfig = AssetDatabase.LoadAssetAtPath<EUUITemplateConfig>(fallback);
            }
            return _cachedTemplateConfig;
        }

        /// <summary>
        /// 返回模板注册表资产，首次访问时自动触发生成
        /// </summary>
        public static EUUITemplateRegistryAsset GetRegistryAsset()
        {
            if (_cachedRegistry != null)
                return _cachedRegistry;

            string editorDir = GetEditorDirectory();
            if (string.IsNullOrEmpty(editorDir))
                return null;

            string registryPath = Path.Combine(editorDir, "EditorSO", "EUUITemplateRegistry.asset").Replace("\\", "/");
            _cachedRegistry = AssetDatabase.LoadAssetAtPath<EUUITemplateRegistryAsset>(registryPath);

            if (_cachedRegistry == null)
            {
                Debug.LogWarning($"[EUUI] 未找到模板注册表，尝试自动生成: {registryPath}");
                EUUITemplateRegistryGenerator.RefreshRegistry();
                _cachedRegistry = AssetDatabase.LoadAssetAtPath<EUUITemplateRegistryAsset>(registryPath);
            }

            return _cachedRegistry;
        }
    }
}
