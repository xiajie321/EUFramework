using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EUFramework.Extension.EUUI.Editor;
using Scriban.Runtime;
using UnityEditor;
using UnityEngine;

namespace EUFramework.Extension.EUUI.Editor.Templates
{
    /// <summary>
    /// EUUI 基础静态模板导出器 
    /// 处理逻辑为传入.sbn路径 导出路径
    /// 模板渲染 输出.cs文件
    /// </summary>
    public static class EUUIBaseExporter
    {
        #region 获取.sbn文件的方法

        /// <summary>
        /// 通过注册表 ID 获取 .sbn 模板文件的完整路径
        /// 路径拼接规则：
        ///   前缀 = EUUI.Editor.asmdef 所在目录（由 EUUITemplateLocator.GetEditorDirectory() 提供）
        ///   后缀 = 注册表 EUUITemplateInfo.path 字段（如 "Static/PanelBase/EURes"）
        ///   完整 = {editorDir}/Templates/Sbn/{registryPath}.sbn
        /// 常见 ID：PanelGenerated / MVCArchitecture / EUUIPanelBase.EURes / EUUIKit.EURes
        /// 以及所有在 Templates/Sbn/ 下扫描到的自定义模板 ID
        /// </summary>
        /// <param name="templateId">注册表中的模板 ID</param>
        /// <returns>模板文件的完整路径</returns>
        /// <exception cref="ArgumentException">ID 为空</exception>
        /// <exception cref="InvalidOperationException">注册表不可用</exception>
        /// <exception cref="KeyNotFoundException">ID 未在注册表中找到</exception>
        /// <exception cref="FileNotFoundException">模板文件不存在</exception>
        public static string GetTemplatePath(string templateId)
        {
            if (string.IsNullOrEmpty(templateId))
                throw new ArgumentException("模板 ID 不能为空");

            // 1. 获取 Editor 目录（通过 EUUI.Editor.asmdef 定位，与框架部署位置无关）
            string editorDir = EUUITemplateLocator.GetEditorDirectory();
            if (string.IsNullOrEmpty(editorDir))
                throw new InvalidOperationException("无法通过 EUUI.Editor.asmdef 定位 Editor 目录");

            // 2. 从注册表获取该 ID 对应的相对路径（如 "Static/PanelBase/EURes"）
            var registry = EUUITemplateLocator.GetRegistryAsset();
            if (registry == null)
                throw new InvalidOperationException("模板注册表不可用，请执行菜单 EUFramework/EUUI/刷新模板注册表");

            string relativePath = registry.GetTemplatePath(templateId);
            if (string.IsNullOrEmpty(relativePath))
            {
                string available = string.Join(", ", registry.templates.Select(t => t.id));
                throw new KeyNotFoundException(
                    $"模板 ID '{templateId}' 未在注册表中找到\n" +
                    $"可用 ID：{available}\n" +
                    $"如有新增模板请执行：EUFramework/EUUI/刷新模板注册表");
            }

            // 3. 拼接完整路径：editorDir + Templates/Sbn/ + relativePath + .sbn
            string fullPath = Path.Combine(editorDir, "Templates", "Sbn", relativePath + ".sbn")
                .Replace("\\", "/");

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"模板文件不存在: {fullPath}");

            return fullPath;
        }

        /// <summary>
        /// 验证指定 ID 的模板文件是否存在于注册表且文件在磁盘上可访问
        /// </summary>
        /// <param name="templateId">注册表中的模板 ID</param>
        /// <returns>模板是否可用</returns>
        public static bool ValidateTemplateExists(string templateId)
        {
            try
            {
                GetTemplatePath(templateId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 读取指定 ID 对应的模板内容
        /// </summary>
        /// <param name="templateId">注册表中的模板 ID</param>
        /// <returns>模板文件内容（UTF-8）</returns>
        /// <exception cref="KeyNotFoundException">ID 未在注册表中找到</exception>
        /// <exception cref="FileNotFoundException">模板文件不存在</exception>
        public static string ReadTemplateContent(string templateId)
        {
            string fullPath = GetTemplatePath(templateId);
            return File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
        }

        #endregion

        #region 导出方法

        /// <summary>
        /// 核心导出方法：通过注册表 ID 读取模板 → Scriban 渲染 → 输出 .cs 文件
        /// </summary>
        /// <param name="templateId">注册表中的模板 ID（如 "PanelGenerated"）</param>
        /// <param name="outputPath">输出文件路径（Assets相对路径或绝对路径均可）</param>
        /// <param name="context">模板上下文数据（可选，无数据时传 null）</param>
        /// <param name="displayName">日志显示名（可选，默认取文件名）</param>
        /// <exception cref="ArgumentException">参数无效</exception>
        /// <exception cref="KeyNotFoundException">模板 ID 未在注册表中找到</exception>
        /// <exception cref="FileNotFoundException">模板文件不存在</exception>
        /// <exception cref="IOException">文件操作失败</exception>
        public static void Export(
            string templateId,
            string outputPath,
            object context = null,
            string displayName = null)
        {
            if (!ValidateExport(templateId, outputPath, out string errorMessage))
                throw new ArgumentException(errorMessage);

            string normalizedOutputPath = NormalizeOutputPath(outputPath);
            EnsureOutputDirectory(normalizedOutputPath);

            string content = ReadTemplateContent(templateId);
            var template = Scriban.Template.Parse(content);
            string result = template.Render(context ?? new { });

            WriteFileWithRetry(normalizedOutputPath, result, displayName ?? Path.GetFileNameWithoutExtension(outputPath));
        }

        /// <summary>
        /// 核心导出方法（ScriptObject 版本）：支持动态键名的模板变量注入（如 eu_res_namespace）
        /// </summary>
        /// <param name="templateId">注册表中的模板 ID</param>
        /// <param name="outputPath">输出文件路径</param>
        /// <param name="scriptObject">Scriban ScriptObject 上下文，支持任意动态键</param>
        /// <param name="displayName">日志显示名（可选）</param>
        public static void Export(
            string templateId,
            string outputPath,
            ScriptObject scriptObject,
            string displayName = null)
        {
            if (!ValidateExport(templateId, outputPath, out string errorMessage))
                throw new ArgumentException(errorMessage);

            string normalizedOutputPath = NormalizeOutputPath(outputPath);
            EnsureOutputDirectory(normalizedOutputPath);

            string content = ReadTemplateContent(templateId);
            var template = Scriban.Template.Parse(content);
            var templateContext = new Scriban.TemplateContext();
            if (scriptObject != null)
                templateContext.PushGlobal(scriptObject);
            string result = template.Render(templateContext);

            WriteFileWithRetry(normalizedOutputPath, result, displayName ?? Path.GetFileNameWithoutExtension(outputPath));
        }

        /// <summary>
        /// 渲染模板并返回结果字符串（不写入文件）
        /// </summary>
        /// <param name="templateId">注册表中的模板 ID</param>
        /// <param name="context">模板上下文数据（可选）</param>
        /// <returns>渲染后的字符串</returns>
        /// <exception cref="KeyNotFoundException">模板 ID 未在注册表中找到</exception>
        /// <exception cref="FileNotFoundException">模板文件不存在</exception>
        public static string RenderTemplate(string templateId, object context = null)
        {
            string content = ReadTemplateContent(templateId);
            var template = Scriban.Template.Parse(content);
            return template.Render(context ?? new { });
        }

        #endregion

        #region 确认/验证方法

        /// <summary>
        /// 验证导出参数是否有效（模板 ID 是否存在 + 输出路径是否合法）
        /// </summary>
        /// <param name="templateId">注册表中的模板 ID</param>
        /// <param name="outputPath">输出文件路径</param>
        /// <param name="errorMessage">验证失败时的错误信息</param>
        /// <returns>验证是否通过</returns>
        public static bool ValidateExport(string templateId, string outputPath, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(templateId))
            {
                errorMessage = "模板 ID 不能为空";
                return false;
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                errorMessage = "输出路径不能为空";
                return false;
            }

            string extension = Path.GetExtension(outputPath);
            if (string.IsNullOrEmpty(extension) || !extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = $"输出文件必须是 .cs 文件，当前扩展名: {extension ?? "无"}";
                return false;
            }

            if (!ValidateTemplateExists(templateId))
            {
                errorMessage = $"模板 ID '{templateId}' 不存在或注册表中找不到，请刷新模板注册表";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 确保输出目录存在
        /// </summary>
        /// <param name="outputPath">输出文件路径</param>
        public static void EnsureOutputDirectory(string outputPath)
        {
            if (string.IsNullOrEmpty(outputPath))
                return;

            string outputDir = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrEmpty(outputDir))
                return;

            // 规范化路径
            string normalizedDir = NormalizePath(outputDir);
            
            // 转换为完整路径
            string fullPath;
            if (normalizedDir.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                fullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), normalizedDir));
            }
            else if (Path.IsPathRooted(normalizedDir))
            {
                fullPath = normalizedDir;
            }
            else
            {
                fullPath = Path.GetFullPath(normalizedDir);
            }

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                Debug.Log($"[EUUI] 创建输出目录: {fullPath}");
            }
        }

        /// <summary>
        /// 规范化输出路径（统一为绝对路径）
        /// </summary>
        /// <param name="outputPath">输出路径（可以是Assets相对路径或绝对路径）</param>
        /// <returns>规范化后的绝对路径</returns>
        public static string NormalizeOutputPath(string outputPath)
        {
            if (string.IsNullOrEmpty(outputPath))
                return outputPath;

            // 如果已经是绝对路径，直接返回
            if (Path.IsPathRooted(outputPath))
                return outputPath.Replace("\\", "/");

            // 如果是Assets相对路径，转换为绝对路径
            if (outputPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), outputPath))
                    .Replace("\\", "/");
            }

            // 其他情况，尝试作为相对路径处理
            return Path.GetFullPath(outputPath).Replace("\\", "/");
        }

        /// <summary>
        /// 规范化路径（统一格式）
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns>规范化后的路径</returns>
        public static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) 
                return path;
            return path.Replace("\\", "/").TrimEnd('/');
        }

        /// <summary>
        /// 将绝对路径转换为Assets相对路径
        /// </summary>
        /// <param name="absolutePath">绝对路径</param>
        /// <returns>Assets相对路径，如果无法转换则返回原路径</returns>
        public static string ToAssetsRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return absolutePath;

            string normalizedPath = NormalizePath(absolutePath);
            string dataPath = Application.dataPath.Replace("\\", "/");

            if (normalizedPath.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                return "Assets" + normalizedPath.Substring(dataPath.Length);
            }

            int idx = normalizedPath.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                return normalizedPath.Substring(idx);
            }

            return normalizedPath;
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 写入文件（带重试机制处理文件共享冲突）
        /// </summary>
        private static void WriteFileWithRetry(string outputPath, string content, string displayName)
        {
            // 处理文件共享冲突：如果文件已存在，先刷新资源数据库并尝试删除
            string assetPath = ToAssetsRelativePath(outputPath);
            
            // 如果文件已存在，先刷新并删除（解决文件被占用的问题）
            if (File.Exists(outputPath))
            {
                AssetDatabase.Refresh();
                if (!string.IsNullOrEmpty(assetPath) && assetPath.StartsWith("Assets/"))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
                AssetDatabase.Refresh();
                
                // 等待文件系统释放文件句柄
                System.Threading.Thread.Sleep(50);
            }
            
            // 写入文件（使用重试机制处理文件共享冲突）
            int maxRetries = 3;
            int retryDelay = 100; // 毫秒
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    File.WriteAllText(outputPath, content, System.Text.Encoding.UTF8);
                    AssetDatabase.Refresh();
                    Debug.Log($"[EUUI] {displayName} 已生成: {outputPath}");
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
    }
}