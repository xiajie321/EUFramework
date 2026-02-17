using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EUFramework.Extension.EUUI.Editor;

namespace EUFramework.Extension.EUUI.Editor.Templates
{
    /// <summary>
    /// EUUI 静态模板导出器 - 处理所有 Static/ 目录下的模板
    /// 特点: 无需额外数据采集，直接读取模板渲染输出
    /// 适用于: PanelBase 扩展、UIKit 扩展、Architecture 集成等静态模板
    /// </summary>
    public static class EUUIStaticExporter
    {
        /// <summary>
        /// 导出所有静态模板扩展
        /// </summary>
        public static void ExportAll()
        {
            var config = GetConfig();
            if (config == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到配置文件！", "确定");
                return;
            }

            try
            {
                // 0. 先删除所有旧的生成文件，避免重复定义
                DeleteAllGeneratedFilesSilent();
                
                // 1. 生成 PanelBase 扩展
                ExportPanelBaseExtensions(config);
                
                // 2. 生成 UIKit 扩展
                ExportUIKitExtensions(config);
                
                AssetDatabase.Refresh();
                SetExtensionsGeneratedDefine(true);
                EditorUtility.DisplayDialog("完成", "所有扩展代码已生成", "确定");
                Debug.Log("[EUUI] 所有扩展代码生成完成");
            }
            catch (Exception e)
            {
                string errorMsg = $"扩展代码生成失败:\n{e.Message}";
                Debug.LogError($"[EUUI] {errorMsg}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("生成失败", errorMsg, "确定");
            }
        }

        /// <summary>
        /// 静默删除所有生成的扩展代码文件（不显示对话框）
        /// </summary>
        private static void DeleteAllGeneratedFilesSilent()
        {
            try
            {
                int deletedCount = 0;

                // 删除 PanelBase 生成文件
                string panelBaseDir = GetPanelBaseOutputDirectory();
                if (!string.IsNullOrEmpty(panelBaseDir) && Directory.Exists(panelBaseDir))
                {
                    string[] panelBaseFiles = Directory.GetFiles(panelBaseDir, "*.Generated.cs", SearchOption.TopDirectoryOnly);
                    foreach (var file in panelBaseFiles)
                    {
                        string assetPath = file.Replace("\\", "/");
                        if (assetPath.StartsWith(Application.dataPath))
                        {
                            assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);
                        }
                        
                        AssetDatabase.DeleteAsset(assetPath);
                        deletedCount++;
                    }
                }

                // 删除 UIKit 生成文件
                string uikitDir = GetUIKitOutputDirectory();
                if (!string.IsNullOrEmpty(uikitDir) && Directory.Exists(uikitDir))
                {
                    string[] uikitFiles = Directory.GetFiles(uikitDir, "*.Generated.cs", SearchOption.TopDirectoryOnly);
                    foreach (var file in uikitFiles)
                    {
                        string assetPath = file.Replace("\\", "/");
                        if (assetPath.StartsWith(Application.dataPath))
                        {
                            assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);
                        }
                        
                        AssetDatabase.DeleteAsset(assetPath);
                        deletedCount++;
                    }
                }

                if (deletedCount > 0)
                {
                    AssetDatabase.Refresh();
                    Debug.Log($"[EUUI] 已删除 {deletedCount} 个旧的生成文件");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[EUUI] 删除旧生成文件时出现警告: {e.Message}");
                // 不抛出异常，继续生成流程
            }
        }

        /// <summary>
        /// 单个可生成项（与 .sbn 一一对应，用于列表显示 未生成→创建 / 已生成→删除）
        /// </summary>
        public class TemplateGenerateItem
        {
            public string TemplatePath;      // .sbn 模板路径（相对或绝对）
            public string OutputAssetPath;   // 生成后的 .cs 在 Assets 下的路径
            public string DisplayName;       // 显示名，如 "PanelBase.EURes"
            public string ExtensionName;     // 扩展名，用于模板 context
            public bool Exists;              // 输出文件是否已存在
        }

        /// <summary>
        /// 可管理行：用于「生成绑定模板」中显示全部项（含未启用），并支持在此勾选是否加入管理。
        /// </summary>
        public struct ManageableRow
        {
            public TemplateGenerateItem Item;
            public bool Enabled;             // 是否在生成面板中管理（可创建/删除）
            public EUUIAdditionalExtension ManualExt; // 非空表示该项来自 manualExtensions，可在此切换 Enabled
        }

        /// <summary>
        /// 获取所有可生成项（与配置和 .sbn 一一对应）
        /// </summary>
        public static List<TemplateGenerateItem> GetGeneratableItems(EUUIEditorConfig config)
        {
            var list = new List<TemplateGenerateItem>();
            if (config == null) return list;

            string panelBaseDir = GetPanelBaseOutputDirectory();
            string uikitDir = GetUIKitOutputDirectory();

            string ToAssetPath(string dir, string fileName)
            {
                string p = Path.Combine(dir, fileName).Replace("\\", "/");
                return p;
            }
            bool OutputExists(string assetPath)
            {
                if (string.IsNullOrEmpty(assetPath)) return false;
                string full = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath));
                return File.Exists(full);
            }

            // PanelBase 核心 EURes
            if (config.resourceLoader == ResourceLoaderType.EURes && !string.IsNullOrEmpty(panelBaseDir))
            {
                try
                {
                    string tp = EUUITemplateManager.GetTemplatePath(EUUITemplateManager.TemplateType.PanelBaseEURes, config);
                    string outPath = ToAssetPath(panelBaseDir, "EUUIPanelBaseEUResExtensions.Generated.cs");
                    list.Add(new TemplateGenerateItem
                    {
                        TemplatePath = tp,
                        OutputAssetPath = outPath,
                        DisplayName = "PanelBase.EURes",
                        ExtensionName = "EURes",
                        Exists = OutputExists(outPath)
                    });
                }
                catch { /* 模板未找到时忽略 */ }
            }

            // PanelBase 附加扩展
            var panelExts = config.manualExtensions?.Where(e => e.enabled && IsPanelBaseTemplate(e.templatePath) && !IsPanelBaseEUResTemplate(e.templatePath)).ToList();
            if (panelExts != null && !string.IsNullOrEmpty(panelBaseDir))
                foreach (var e in panelExts)
                {
                    string name = GetExtensionNameFromPath(e.templatePath);
                    string outPath = ToAssetPath(panelBaseDir, $"EUUIPanelBase.{name}.Generated.cs");
                    list.Add(new TemplateGenerateItem
                    {
                        TemplatePath = e.templatePath,
                        OutputAssetPath = outPath,
                        DisplayName = "PanelBase." + name,
                        ExtensionName = name,
                        Exists = OutputExists(outPath)
                    });
                }

            // UIKit 核心（EURes 或 Custom）
            if (config.resourceLoader == ResourceLoaderType.EURes && !string.IsNullOrEmpty(uikitDir))
            {
                try
                {
                    string tp = EUUITemplateManager.GetTemplatePath(EUUITemplateManager.TemplateType.KitEURes, config);
                    string outPath = ToAssetPath(uikitDir, "EUUIKit.EURes.Generated.cs");
                    list.Add(new TemplateGenerateItem
                    {
                        TemplatePath = tp,
                        OutputAssetPath = outPath,
                        DisplayName = "UIKit.EURes",
                        ExtensionName = "EURes",
                        Exists = OutputExists(outPath)
                    });
                }
                catch { }
            }
            else if (config.resourceLoader == ResourceLoaderType.Custom && !string.IsNullOrEmpty(uikitDir))
            {
                string outPath = ToAssetPath(uikitDir, "EUUIKit.Custom.Generated.cs");
                list.Add(new TemplateGenerateItem
                {
                    TemplatePath = "",
                    OutputAssetPath = outPath,
                    DisplayName = "UIKit.Custom",
                    ExtensionName = "Custom",
                    Exists = OutputExists(outPath)
                });
            }

            // UIKit 附加扩展
            var uikitExts = config.manualExtensions?.Where(e => e.enabled && IsUIKitTemplate(e.templatePath) && !IsResourceLoaderTemplate(e.templatePath)).ToList();
            if (uikitExts != null && !string.IsNullOrEmpty(uikitDir))
                foreach (var e in uikitExts)
                {
                    string name = GetExtensionNameFromPath(e.templatePath);
                    string outPath = ToAssetPath(uikitDir, $"EUUIKit.{name}.Generated.cs");
                    list.Add(new TemplateGenerateItem
                    {
                        TemplatePath = e.templatePath,
                        OutputAssetPath = outPath,
                        DisplayName = "UIKit." + name,
                        ExtensionName = name,
                        Exists = OutputExists(outPath)
                    });
                }

            return list;
        }

        /// <summary>
        /// 获取所有可管理行（含未启用的手动扩展），供「生成绑定模板」中显示并勾选是否加入管理。
        /// </summary>
        public static List<ManageableRow> GetManageableRows(EUUIEditorConfig config)
        {
            var rows = new List<ManageableRow>();
            if (config == null) return rows;

            string panelBaseDir = GetPanelBaseOutputDirectory();
            string uikitDir = GetUIKitOutputDirectory();
            string corePanelPath = null;
            string coreKitPath = null;

            string ToAssetPath(string dir, string fileName)
            {
                string p = Path.Combine(dir, fileName).Replace("\\", "/");
                return p;
            }
            bool OutputExists(string assetPath)
            {
                if (string.IsNullOrEmpty(assetPath)) return false;
                string full = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath));
                return File.Exists(full);
            }

            // 核心项：无 ManualExt，始终视为启用
            if (config.resourceLoader == ResourceLoaderType.EURes && !string.IsNullOrEmpty(panelBaseDir))
            {
                try
                {
                    string tp = EUUITemplateManager.GetTemplatePath(EUUITemplateManager.TemplateType.PanelBaseEURes, config);
                    corePanelPath = tp;
                    string outPath = ToAssetPath(panelBaseDir, "EUUIPanelBaseEUResExtensions.Generated.cs");
                    rows.Add(new ManageableRow
                    {
                        Item = new TemplateGenerateItem
                        {
                            TemplatePath = tp,
                            OutputAssetPath = outPath,
                            DisplayName = "PanelBase.EURes",
                            ExtensionName = "EURes",
                            Exists = OutputExists(outPath)
                        },
                        Enabled = true,
                        ManualExt = null
                    });
                }
                catch { }
            }

            // 手动扩展 - PanelBase（含未启用）
            var panelExts = config.manualExtensions?.Where(e => IsPanelBaseTemplate(e.templatePath) && !IsPanelBaseEUResTemplate(e.templatePath)).ToList();
            if (panelExts != null && !string.IsNullOrEmpty(panelBaseDir))
            {
                foreach (var e in panelExts)
                {
                    string name = GetExtensionNameFromPath(e.templatePath);
                    string outPath = ToAssetPath(panelBaseDir, $"EUUIPanelBase.{name}.Generated.cs");
                    rows.Add(new ManageableRow
                    {
                        Item = new TemplateGenerateItem
                        {
                            TemplatePath = e.templatePath,
                            OutputAssetPath = outPath,
                            DisplayName = "PanelBase." + name,
                            ExtensionName = name,
                            Exists = OutputExists(outPath)
                        },
                        Enabled = e.enabled,
                        ManualExt = e
                    });
                }
            }

            // 核心 UIKit
            if (config.resourceLoader == ResourceLoaderType.EURes && !string.IsNullOrEmpty(uikitDir))
            {
                try
                {
                    string tp = EUUITemplateManager.GetTemplatePath(EUUITemplateManager.TemplateType.KitEURes, config);
                    coreKitPath = tp;
                    string outPath = ToAssetPath(uikitDir, "EUUIKit.EURes.Generated.cs");
                    rows.Add(new ManageableRow
                    {
                        Item = new TemplateGenerateItem
                        {
                            TemplatePath = tp,
                            OutputAssetPath = outPath,
                            DisplayName = "UIKit.EURes",
                            ExtensionName = "EURes",
                            Exists = OutputExists(outPath)
                        },
                        Enabled = true,
                        ManualExt = null
                    });
                }
                catch { }
            }
            else if (config.resourceLoader == ResourceLoaderType.Custom && !string.IsNullOrEmpty(uikitDir))
            {
                coreKitPath = string.IsNullOrEmpty(config.customLoaderTemplate) ? null : EUUITemplateManager.GetCustomTemplatePath(config.customLoaderTemplate);
                string outPath = ToAssetPath(uikitDir, "EUUIKit.Custom.Generated.cs");
                rows.Add(new ManageableRow
                {
                    Item = new TemplateGenerateItem
                    {
                        TemplatePath = "",
                        OutputAssetPath = outPath,
                        DisplayName = "UIKit.Custom",
                        ExtensionName = "Custom",
                        Exists = OutputExists(outPath)
                    },
                    Enabled = true,
                    ManualExt = null
                });
            }

            // 手动扩展 - UIKit（含未启用，含 Resources/Addressables 等，与「模板管理」一致）
            var uikitExts = config.manualExtensions?.Where(e => IsUIKitTemplate(e.templatePath)).ToList();
            if (uikitExts != null && !string.IsNullOrEmpty(uikitDir))
            {
                foreach (var e in uikitExts)
                {
                    if (PathsEqual(e.templatePath, coreKitPath)) continue; // 与核心重复，已在上面列出
                    string name = GetExtensionNameFromPath(e.templatePath);
                    string outPath = ToAssetPath(uikitDir, $"EUUIKit.{name}.Generated.cs");
                    rows.Add(new ManageableRow
                    {
                        Item = new TemplateGenerateItem
                        {
                            TemplatePath = e.templatePath,
                            OutputAssetPath = outPath,
                            DisplayName = "UIKit." + name,
                            ExtensionName = name,
                            Exists = OutputExists(outPath)
                        },
                        Enabled = e.enabled,
                        ManualExt = e
                    });
                }
            }

            // 其余手动扩展（WithData、PanelBase 下非 EURes 等）：均列出，便于在「生成绑定模板」中勾选管理
            var alreadyAdded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
                if (!string.IsNullOrEmpty(r.Item.TemplatePath))
                    alreadyAdded.Add(ToAssetsRelativePath(r.Item.TemplatePath));
            if (!string.IsNullOrEmpty(corePanelPath)) alreadyAdded.Add(ToAssetsRelativePath(corePanelPath));
            if (!string.IsNullOrEmpty(coreKitPath)) alreadyAdded.Add(ToAssetsRelativePath(coreKitPath));
            if (config.manualExtensions != null)
            {
                foreach (var e in config.manualExtensions)
                {
                    if (string.IsNullOrEmpty(e.templatePath)) continue;
                    string norm = ToAssetsRelativePath(e.templatePath);
                    if (alreadyAdded.Contains(norm)) continue;
                    alreadyAdded.Add(norm);
                    string name = GetExtensionNameFromPath(e.templatePath);
                    string displayName = name;
                    string outPath = "";
                    if (IsPanelBaseTemplate(e.templatePath) && !IsPanelBaseEUResTemplate(e.templatePath) && !string.IsNullOrEmpty(panelBaseDir))
                    {
                        displayName = "PanelBase." + name;
                        outPath = ToAssetPath(panelBaseDir, $"EUUIPanelBase.{name}.Generated.cs");
                    }
                    else if (IsUIKitTemplate(e.templatePath))
                    {
                        displayName = "UIKit." + name;
                        if (!string.IsNullOrEmpty(uikitDir))
                            outPath = ToAssetPath(uikitDir, $"EUUIKit.{name}.Generated.cs");
                    }
                    rows.Add(new ManageableRow
                    {
                        Item = new TemplateGenerateItem
                        {
                            TemplatePath = e.templatePath,
                            OutputAssetPath = outPath,
                            DisplayName = displayName,
                            ExtensionName = name,
                            Exists = !string.IsNullOrEmpty(outPath) && OutputExists(outPath)
                        },
                        Enabled = e.enabled,
                        ManualExt = e
                    });
                }
            }

            return rows;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            return path.Replace("\\", "/").TrimEnd('/');
        }

        /// <summary>
        /// 统一为 Assets 相对路径再比较，避免核心路径（完整路径）与 manual 路径（Assets/...）不一致导致重复项。
        /// </summary>
        private static string ToAssetsRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            path = NormalizePath(path);
            string dataPath = Application.dataPath.Replace("\\", "/");
            if (path.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
                return "Assets" + path.Substring(dataPath.Length);
            int idx = path.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0) return path.Substring(idx);
            return path;
        }

        private static bool PathsEqual(string a, string b)
        {
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return true;
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            return string.Equals(ToAssetsRelativePath(a), ToAssetsRelativePath(b), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 仅生成指定一项（用于与 .sbn 一一对应的「创建」）
        /// </summary>
        public static void ExportSingleItem(EUUIEditorConfig config, TemplateGenerateItem item)
        {
            if (config == null || item == null) return;
            string templatePath = item.TemplatePath;
            if (string.IsNullOrEmpty(templatePath) && item.DisplayName == "UIKit.Custom")
            {
                ExportUIKitCustomLoader(config, GetUIKitOutputDirectory());
                AssetDatabase.Refresh();
                SetExtensionsGeneratedDefine(true);
                return;
            }
            if (string.IsNullOrEmpty(templatePath)) return;
            string fullOutputPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), item.OutputAssetPath));
            try
            {
                string resolvedTemplate = templatePath.Contains("Assets") || File.Exists(templatePath)
                    ? templatePath
                    : EUUITemplateManager.GetCustomTemplatePath(templatePath);
                ExportStaticTemplate(resolvedTemplate, fullOutputPath, item.DisplayName, new { extension_name = item.ExtensionName });
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUUI] 生成失败 {item.DisplayName}: {e.Message}");
                throw;
            }
            AssetDatabase.Refresh();
            if (item.OutputAssetPath.Contains("UIKit")) SetExtensionsGeneratedDefine(true);
        }

        /// <summary>
        /// 仅删除指定一项生成文件（用于与 .sbn 一一对应的「删除」）
        /// </summary>
        public static void DeleteSingleItem(TemplateGenerateItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.OutputAssetPath)) return;
            AssetDatabase.DeleteAsset(item.OutputAssetPath);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 删除所有生成的扩展代码文件
        /// </summary>
        public static void DeleteAllGeneratedFiles()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "确认删除",
                "确定要删除所有生成的扩展代码文件吗？\n\n此操作将删除以下目录中的所有 .Generated.cs 文件：\n" +
                "- Script/Generate/PanelBase/\n" +
                "- Script/Generate/UIKit/\n\n" +
                "此操作不可撤销！",
                "删除",
                "取消");

            if (!confirmed)
                return;

            try
            {
                int deletedCount = 0;

                // 删除 PanelBase 生成文件
                string panelBaseDir = GetPanelBaseOutputDirectory();
                if (!string.IsNullOrEmpty(panelBaseDir) && Directory.Exists(panelBaseDir))
                {
                    string[] panelBaseFiles = Directory.GetFiles(panelBaseDir, "*.Generated.cs", SearchOption.TopDirectoryOnly);
                    foreach (var file in panelBaseFiles)
                    {
                        string assetPath = file.Replace("\\", "/");
                        if (assetPath.StartsWith(Application.dataPath))
                        {
                            assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);
                        }
                        
                        AssetDatabase.DeleteAsset(assetPath);
                        deletedCount++;
                    }
                }

                // 删除 UIKit 生成文件
                string uikitDir = GetUIKitOutputDirectory();
                if (!string.IsNullOrEmpty(uikitDir) && Directory.Exists(uikitDir))
                {
                    string[] uikitFiles = Directory.GetFiles(uikitDir, "*.Generated.cs", SearchOption.TopDirectoryOnly);
                    foreach (var file in uikitFiles)
                    {
                        string assetPath = file.Replace("\\", "/");
                        if (assetPath.StartsWith(Application.dataPath))
                        {
                            assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);
                        }
                        
                        AssetDatabase.DeleteAsset(assetPath);
                        deletedCount++;
                    }
                }

                AssetDatabase.Refresh();
                SetExtensionsGeneratedDefine(false);
                EditorUtility.DisplayDialog("完成", $"已删除 {deletedCount} 个生成文件", "确定");
                Debug.Log($"[EUUI] 已删除 {deletedCount} 个生成的扩展代码文件");
            }
            catch (Exception e)
            {
                string errorMsg = $"删除生成文件失败:\n{e.Message}";
                Debug.LogError($"[EUUI] {errorMsg}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("删除失败", errorMsg, "确定");
            }
        }

        #region PanelBase 扩展

        /// <summary>
        /// 导出 PanelBase 扩展
        /// </summary>
        private static void ExportPanelBaseExtensions(EUUIEditorConfig config)
        {
            string outputDir = GetPanelBaseOutputDirectory();
            if (string.IsNullOrEmpty(outputDir))
            {
                Debug.LogError("[EUUI] 无法定位 PanelBase 输出目录");
                return;
            }

            Debug.Log($"[EUUI] 开始生成 PanelBase 扩展 -> {outputDir}");

            // 1. 生成核心扩展 (EURes)
            ExportPanelBaseCoreExtension(config, outputDir);
            
            // 2. 生成附加扩展 (用户自定义)
            ExportPanelBaseAdditionalExtensions(config, outputDir);
        }

        /// <summary>
        /// 导出 PanelBase 核心扩展
        /// </summary>
        private static void ExportPanelBaseCoreExtension(EUUIEditorConfig config, string outputDir)
        {
            // 只有使用 EURes 时才生成
            if (config.resourceLoader != ResourceLoaderType.EURes)
            {
                Debug.Log("[EUUI] PanelBase: 核心扩展跳过 (resourceLoader != EURes)");
                return;
            }

            try
            {
                string templatePath = EUUITemplateManager.GetTemplatePath(
                    EUUITemplateManager.TemplateType.PanelBaseEURes, config);
                
                ExportStaticTemplate(
                    templatePath, 
                    Path.Combine(outputDir, "EUUIPanelBaseEUResExtensions.Generated.cs"),
                    "PanelBase.EURes");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUUI] PanelBase 核心扩展生成失败: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 导出 PanelBase 附加扩展
        /// 注意：排除 EURes 模板（由核心扩展处理），避免重复生成
        /// </summary>
        private static void ExportPanelBaseAdditionalExtensions(EUUIEditorConfig config, string outputDir)
        {
            var extensions = config.manualExtensions?
                .Where(e => e.enabled && IsPanelBaseTemplate(e.templatePath) && !IsPanelBaseEUResTemplate(e.templatePath))
                .ToList();

            if (extensions == null || extensions.Count == 0)
            {
                Debug.Log("[EUUI] PanelBase: 没有启用的附加扩展（EURes 模板已排除）");
                return;
            }

            Debug.Log($"[EUUI] PanelBase: 开始生成 {extensions.Count} 个附加扩展");

            foreach (var ext in extensions)
            {
                try
                {
                    string templatePath = EUUITemplateManager.GetCustomTemplatePath(ext.templatePath);
                    string extensionName = GetExtensionNameFromPath(ext.templatePath);
                    string outputPath = Path.Combine(outputDir, $"EUUIPanelBase.{extensionName}.Generated.cs");
                    
                    ExportStaticTemplate(
                        templatePath, 
                        outputPath,
                        $"PanelBase.{extensionName}",
                        new { extension_name = extensionName });
                }
                catch (Exception e)
                {
                    string extensionName = GetExtensionNameFromPath(ext.templatePath);
                    Debug.LogError($"[EUUI] PanelBase 扩展 '{extensionName}' 生成失败: {e.Message}");
                    throw;
                }
            }
        }
        
        /// <summary>
        /// 判断模板是否为 PanelBase 扩展（根据路径判断）
        /// </summary>
        private static bool IsPanelBaseTemplate(string templatePath)
        {
            return templatePath.Contains("/PanelBase/") || templatePath.Contains("\\PanelBase\\");
        }

        /// <summary>
        /// 判断模板是否为 PanelBase EURes 模板（由核心扩展处理，不应作为附加扩展生成）
        /// </summary>
        private static bool IsPanelBaseEUResTemplate(string templatePath)
        {
            if (string.IsNullOrEmpty(templatePath))
                return false;

            string fileName = Path.GetFileNameWithoutExtension(templatePath).ToLowerInvariant();
            return fileName == "eures";
        }
        
        /// <summary>
        /// 从模板路径提取扩展名称（文件名，不含扩展名）
        /// </summary>
        private static string GetExtensionNameFromPath(string templatePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(templatePath);
            return fileName;
        }

        #endregion

        #region UIKit 扩展

        /// <summary>
        /// 导出 UIKit 扩展
        /// </summary>
        private static void ExportUIKitExtensions(EUUIEditorConfig config)
        {
            string outputDir = GetUIKitOutputDirectory();
            if (string.IsNullOrEmpty(outputDir))
            {
                Debug.LogError("[EUUI] 无法定位 UIKit 输出目录");
                return;
            }

            Debug.Log($"[EUUI] 开始生成 UIKit 扩展 -> {outputDir}");

            // 1. 生成资源加载器扩展
            ExportUIKitResourceLoader(config, outputDir);
            
            // 2. 生成附加扩展 (用户自定义)
            ExportUIKitAdditionalExtensions(config, outputDir);
        }

        /// <summary>
        /// 导出 UIKit 资源加载器
        /// </summary>
        private static void ExportUIKitResourceLoader(EUUIEditorConfig config, string outputDir)
        {
            switch (config.resourceLoader)
            {
                case ResourceLoaderType.EURes:
                    Debug.Log("[EUUI] UIKit: 生成 EURes 资源加载器");
                    ExportUIKitEUResLoader(config, outputDir);
                    break;
                    
                case ResourceLoaderType.Custom:
                    Debug.Log("[EUUI] UIKit: 生成自定义资源加载器");
                    ExportUIKitCustomLoader(config, outputDir);
                    break;
                    
                case ResourceLoaderType.None:
                    Debug.LogWarning("[EUUI] UIKit 资源加载设置为 None，需手动实现：\n" +
                                   "1. 创建 EUUIKit.*.cs 分部类\n" +
                                   "2. 添加 #define EUUI_EXTENSIONS_GENERATED\n" +
                                   "3. 实现 LoadPanelPrefabAsync<T>() 方法");
                    break;
            }
        }

        /// <summary>
        /// 导出 UIKit EURes 加载器
        /// </summary>
        private static void ExportUIKitEUResLoader(EUUIEditorConfig config, string outputDir)
        {
            try
            {
                string templatePath = EUUITemplateManager.GetTemplatePath(
                    EUUITemplateManager.TemplateType.KitEURes, config);
                
                ExportStaticTemplate(
                    templatePath,
                    Path.Combine(outputDir, "EUUIKit.EURes.Generated.cs"),
                    "UIKit.EURes");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUUI] UIKit EURes 扩展生成失败: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 导出 UIKit 自定义加载器
        /// </summary>
        private static void ExportUIKitCustomLoader(EUUIEditorConfig config, string outputDir)
        {
            try
            {
                string templatePath;
                
                if (string.IsNullOrEmpty(config.customLoaderTemplate))
                {
                    // 使用默认 Custom 模板
                    templatePath = EUUITemplateManager.GetTemplatePath(
                        EUUITemplateManager.TemplateType.KitCustom, config);
                    Debug.Log("[EUUI] 使用默认 Custom 模板");
                }
                else
                {
                    // 使用用户指定的模板
                    templatePath = EUUITemplateManager.GetCustomTemplatePath(config.customLoaderTemplate);
                    Debug.Log($"[EUUI] 使用自定义模板: {config.customLoaderTemplate}");
                }
                
                ExportStaticTemplate(
                    templatePath,
                    Path.Combine(outputDir, "EUUIKit.Custom.Generated.cs"),
                    "UIKit.Custom");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUUI] UIKit 自定义加载器生成失败: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 导出 UIKit 附加扩展
        /// 注意：排除资源加载器模板（EURes、Custom、Resources、Addressables），这些由核心扩展处理
        /// </summary>
        private static void ExportUIKitAdditionalExtensions(EUUIEditorConfig config, string outputDir)
        {
            var extensions = config.manualExtensions?
                .Where(e => e.enabled && IsUIKitTemplate(e.templatePath) && !IsResourceLoaderTemplate(e.templatePath))
                .ToList();

            if (extensions == null || extensions.Count == 0)
            {
                Debug.Log("[EUUI] UIKit: 没有启用的附加扩展（资源加载器模板已排除）");
                return;
            }

            Debug.Log($"[EUUI] UIKit: 开始生成 {extensions.Count} 个附加扩展");

            foreach (var ext in extensions)
            {
                try
                {
                    string templatePath = EUUITemplateManager.GetCustomTemplatePath(ext.templatePath);
                    string extensionName = GetExtensionNameFromPath(ext.templatePath);
                    string outputPath = Path.Combine(outputDir, $"EUUIKit.{extensionName}.Generated.cs");
                    
                    ExportStaticTemplate(
                        templatePath,
                        outputPath,
                        $"UIKit.{extensionName}",
                        new { extension_name = extensionName });
                }
                catch (Exception e)
                {
                    string extensionName = GetExtensionNameFromPath(ext.templatePath);
                    Debug.LogError($"[EUUI] UIKit 扩展 '{extensionName}' 生成失败: {e.Message}");
                    throw;
                }
            }
        }
        
        /// <summary>
        /// 判断模板是否为 UIKit 扩展（根据路径判断）
        /// </summary>
        private static bool IsUIKitTemplate(string templatePath)
        {
            return templatePath.Contains("/UIKit/") || templatePath.Contains("\\UIKit\\");
        }

        /// <summary>
        /// 判断模板是否为资源加载器模板（这些应该由核心扩展处理，不应作为附加扩展生成）
        /// </summary>
        private static bool IsResourceLoaderTemplate(string templatePath)
        {
            if (string.IsNullOrEmpty(templatePath))
                return false;

            string fileName = Path.GetFileNameWithoutExtension(templatePath).ToLowerInvariant();
            
            // 资源加载器模板名称：EURes、Custom、Resources、Addressables
            return fileName == "eures" || 
                   fileName == "custom" || 
                   fileName == "resources" || 
                   fileName == "addressables";
        }

        #endregion

        #region 核心导出逻辑

        /// <summary>
        /// 导出静态模板（通用方法）
        /// </summary>
        private static void ExportStaticTemplate(
            string templatePath, 
            string outputPath, 
            string displayName,
            object context = null)
        {
            // 1. 读取模板
            string templateContent = File.ReadAllText(templatePath);
            
            // 2. 渲染（静态模板无需或仅需简单数据）
            var template = Scriban.Template.Parse(templateContent);
            string result = template.Render(context ?? new { });
            
            // 3. 确保输出目录存在
            string outputDir = Path.GetDirectoryName(outputPath);
            EnsureDirectory(outputDir);
            
            // 4. 处理文件共享冲突：如果文件已存在，先刷新资源数据库并尝试删除
            string assetPath = outputPath.Replace("\\", "/");
            if (assetPath.StartsWith(Application.dataPath))
            {
                assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);
            }
            
            // 如果文件已存在，先刷新并删除（解决文件被占用的问题）
            if (File.Exists(outputPath))
            {
                AssetDatabase.Refresh();
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.Refresh();
                
                // 等待文件系统释放文件句柄
                System.Threading.Thread.Sleep(50);
            }
            
            // 5. 写入文件（使用重试机制处理文件共享冲突）
            int maxRetries = 3;
            int retryDelay = 100; // 毫秒
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    File.WriteAllText(outputPath, result, System.Text.Encoding.UTF8);
                    AssetDatabase.Refresh();
                    Debug.Log($"[EUUI] {displayName} 扩展已生成: {outputPath}");
                    return;
                }
                catch (System.IO.IOException ex) when (ex.Message.Contains("Sharing violation") || ex.Message.Contains("being used"))
                {
                    if (attempt < maxRetries - 1)
                    {
                        Debug.LogWarning($"[EUUI] 文件被占用，等待 {retryDelay}ms 后重试 ({attempt + 1}/{maxRetries})...");
                        System.Threading.Thread.Sleep(retryDelay);
                        retryDelay *= 2; // 指数退避
                        AssetDatabase.Refresh();
                    }
                    else
                    {
                        throw new System.IO.IOException(
                            $"无法写入文件（文件被占用）: {outputPath}\n" +
                            $"请关闭可能正在编辑此文件的程序（如 Visual Studio、Rider 等），然后重试。", ex);
                    }
                }
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取 PanelBase 扩展输出目录
        /// </summary>
        private static string GetPanelBaseOutputDirectory()
        {
            string[] guids = AssetDatabase.FindAssets("EUUIPanelBase t:MonoScript");
            if (guids == null || guids.Length == 0)
            {
                Debug.LogError("[EUUI] 无法找到 EUUIPanelBase 脚本");
                return null;
            }

            string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            string scriptDir = Path.GetDirectoryName(scriptPath)?.Replace("\\", "/");
            
            string generateDir = Path.Combine(scriptDir, "Generate", "PanelBase").Replace("\\", "/");
            EnsureDirectory(generateDir);
            
            return generateDir;
        }

        /// <summary>
        /// 获取 UIKit 扩展输出目录
        /// </summary>
        private static string GetUIKitOutputDirectory()
        {
            string[] guids = AssetDatabase.FindAssets("EUUIKit t:MonoScript");
            if (guids == null || guids.Length == 0)
            {
                Debug.LogError("[EUUI] 无法找到 EUUIKit 脚本");
                return null;
            }

            string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            string scriptDir = Path.GetDirectoryName(scriptPath)?.Replace("\\", "/");
            
            string generateDir = Path.Combine(scriptDir, "Generate", "UIKit").Replace("\\", "/");
            EnsureDirectory(generateDir);
            
            return generateDir;
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        private static void EnsureDirectory(string directory)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), directory));
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                Debug.Log($"[EUUI] 创建目录: {directory}");
            }
        }

        /// <summary>
        /// 设置或移除 EUUI_EXTENSIONS_GENERATED 宏（控制 EUUIKit.cs 中占位方法是否参与编译）
        /// 生成扩展代码后添加宏，占位不编译；删除生成文件后移除宏，占位参与编译
        /// </summary>
        private static void SetExtensionsGeneratedDefine(bool add)
        {
            const string define = "EUUI_EXTENSIONS_GENERATED";
            foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (group == BuildTargetGroup.Unknown) continue;
                try
                {
                    string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
                    if (add)
                    {
                        if (defines.IndexOf(define, StringComparison.Ordinal) >= 0) continue;
                        if (defines.Length > 0) defines += ";";
                        defines += define;
                    }
                    else
                    {
                        var list = new List<string>(defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                        if (!list.Remove(define)) continue;
                        defines = string.Join(";", list);
                    }
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[EUUI] 设置脚本宏 {define} 失败 (BuildTargetGroup.{group}): {e.Message}");
                }
            }
        }

        /// <summary>
        /// 获取配置文件
        /// </summary>
        private static EUUIEditorConfig GetConfig()
        {
            string[] guids = AssetDatabase.FindAssets("EUUIEditorConfig");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path != null && path.EndsWith("EUUIEditorConfig.asset", StringComparison.OrdinalIgnoreCase))
                {
                    return AssetDatabase.LoadAssetAtPath<EUUIEditorConfig>(path);
                }
            }
            return null;
        }

        #endregion
    }
}
