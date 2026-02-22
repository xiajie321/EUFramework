#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// EUUI asmdef 统一管理工具
    /// 负责向 EUUI.asmdef / EUUI.Editor.asmdef 添加或移除程序集引用，
    /// 以及根据已生成文件重算两个 asmdef 的 references 列表。
    /// </summary>
    public static class EUUIAsmdefHelper
    {
        // ── 常量：各 asmdef 的基础引用（永远保留）────────────────────────────────
        private static readonly string[] k_RuntimeBaseRefs = { "UniTask" };
        private static readonly string[] k_EditorBaseRefs  = { "EUUI", "UniTask" };

        // ── 脚本宏 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 设置或移除 EUUI_EXTENSIONS_GENERATED 脚本宏。
        /// 使用 delayCall 延迟到下一编辑器帧执行，避免在 asset 变更（删除/生成文件）
        /// 完成之前调用 SetScriptingDefineSymbols 导致 Unity 内部 Assembly 图中存在
        /// 残留空项，触发 Dictionary.ContainsKey(null) 崩溃。
        /// </summary>
        public static void SetExtensionsGeneratedDefine(bool add)
        {
            // 延迟到下一帧，确保 Unity 已完成对被删除/新增 asset 的 assembly 重建
            EditorApplication.delayCall += () => ApplyDefineImmediate(add);
        }

        private static void ApplyDefineImmediate(bool add)
        {
            const string define = "EUUI_EXTENSIONS_GENERATED";

            var targets = new[]
            {
                UnityEditor.Build.NamedBuildTarget.Standalone,
                UnityEditor.Build.NamedBuildTarget.Android,
                UnityEditor.Build.NamedBuildTarget.iOS,
                UnityEditor.Build.NamedBuildTarget.WebGL,
                UnityEditor.Build.NamedBuildTarget.WindowsStoreApps,
                UnityEditor.Build.NamedBuildTarget.tvOS,
                UnityEditor.Build.NamedBuildTarget.NintendoSwitch,
                UnityEditor.Build.NamedBuildTarget.Server,
            };

            foreach (var target in targets)
            {
                try
                {
                    string defines = PlayerSettings.GetScriptingDefineSymbols(target);
                    if (add)
                    {
                        if (defines.IndexOf(define, StringComparison.Ordinal) >= 0) continue;
                        if (defines.Length > 0) defines += ";";
                        defines += define;
                    }
                    else
                    {
                        var list = new List<string>(
                            defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                        if (!list.Remove(define)) continue;
                        defines = string.Join(";", list);
                    }
                    PlayerSettings.SetScriptingDefineSymbols(target, defines);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[EUUI] 设置脚本宏 {define} 失败 ({target}): {e.Message}");
                }
            }
        }

        // ── 单条 reference 增/删 ─────────────────────────────────────────────────

        /// <summary>
        /// 向指定 asmdef 文件的 references 中添加或移除单个程序集引用（幂等）。
        /// asmdefFileName 支持 "EUUI.asmdef" 或 "EUUI.Editor.asmdef"。
        /// </summary>
        public static void SetAssembly(string asmdefFileName, string assemblyName, bool add)
        {
            if (string.IsNullOrEmpty(assemblyName)) return;

            string asmdefPath = GetAsmdefPath(asmdefFileName);
            if (string.IsNullOrEmpty(asmdefPath))
            {
                Debug.LogError($"[EUUI] 无法找到 {asmdefFileName}");
                return;
            }

            string fullPath = Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(Application.dataPath), asmdefPath));
            string json = File.ReadAllText(fullPath, System.Text.Encoding.UTF8);

            // 只在 "references":[...] 块内检测，避免 name/rootNamespace 等字段误判
            bool hasRef = IsAssemblyInReferencesArray(json, assemblyName);

            if (add && hasRef)   return;
            if (!add && !hasRef) return;

            if (add)
            {
                json = AddReferenceToJson(json, assemblyName);
                Debug.Log($"[EUUI] 已向 {asmdefFileName} 添加引用: {assemblyName}");
            }
            else
            {
                json = RemoveReferenceFromJson(json, assemblyName);
                Debug.Log($"[EUUI] 已从 {asmdefFileName} 移除引用: {assemblyName}");
            }

            File.WriteAllText(fullPath, json, System.Text.Encoding.UTF8);
            // 不调用 ImportAsset：避免同步触发 Cursor IDE 插件 SyncAll → null-key 崩溃
            // Unity 文件监听器会在下一帧自动检测到变更
        }

        // ── 批量重算 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 扫描所有已生成文件对应的 .sbn 伴生 JSON，重新计算并同时写入：
        /// - EUUI.asmdef（运行时引用）
        /// - EUUI.Editor.asmdef（编辑器引用）
        /// 基础引用（UniTask / EUUI）始终保留，额外引用完全由当前存在的生成文件决定。
        /// 写入后延迟触发 AssetDatabase.Refresh，避免同步崩溃。
        /// </summary>
        public static void RecalculateFromGeneratedFiles()
        {
            var sbnPaths = CollectActiveSbnPaths();

            var runtimeRequired = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var editorRequired  = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var sbn in sbnPaths)
            {
                foreach (var asm in ReadSidecarRuntimeAssemblies(sbn))
                    runtimeRequired.Add(asm);
                foreach (var asm in ReadSidecarEditorAssemblies(sbn))
                    editorRequired.Add(asm);
            }

            // 直接写文件，不调用 ImportAsset；延迟一帧再 Refresh 让 Unity 重新编译
            RewriteAsmdefReferences("EUUI.asmdef",        k_RuntimeBaseRefs, runtimeRequired);
            RewriteAsmdefReferences("EUUI.Editor.asmdef", k_EditorBaseRefs,  editorRequired);
            EditorApplication.delayCall += AssetDatabase.Refresh;
        }

        /// <summary>
        /// 批量向两个 asmdef 添加程序集引用（模块安装时使用）。
        /// </summary>
        public static void AddModuleAssemblies(string[] runtimeAssemblies, string[] editorAssemblies)
        {
            foreach (var asm in runtimeAssemblies ?? Array.Empty<string>())
                SetAssembly("EUUI.asmdef", asm, true);
            foreach (var asm in editorAssemblies ?? Array.Empty<string>())
                SetAssembly("EUUI.Editor.asmdef", asm, true);
            EditorApplication.delayCall += AssetDatabase.Refresh;
        }

        // ── 程序集可用性检测 ─────────────────────────────────────────────────────

        /// <summary>
        /// 检查项目中是否存在指定程序集。
        /// 同时匹配 asmdef 文件名（去掉空格/扩展名）和 asmdef JSON 中的 "name" 字段，
        /// 避免文件名带空格（如 "EU Res.asmdef"）而程序集名为 "EURes" 时检测失败。
        /// </summary>
        public static bool IsAssemblyAvailable(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName)) return false;

            // 宽松搜索：去掉空格后与 assemblyName 比较文件名
            string[] guids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset");
            foreach (string g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                // 1. 文件名（去空格）匹配
                string fileBaseName = Path.GetFileNameWithoutExtension(p)
                    .Replace(" ", "");
                if (fileBaseName.Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
                    return true;

                // 2. 读 asmdef JSON 中的 "name" 字段匹配
                try
                {
                    string fullPath = Path.GetFullPath(
                        Path.Combine(Path.GetDirectoryName(Application.dataPath), p));
                    if (!File.Exists(fullPath)) continue;
                    string json = File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
                    var m = System.Text.RegularExpressions.Regex.Match(
                        json, "\"name\"\\s*:\\s*\"([^\"]+)\"");
                    if (m.Success && m.Groups[1].Value.Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                catch { /* 读取失败时跳过 */ }
            }
            return false;
        }

        // ── asmdef 路径定位 ──────────────────────────────────────────────────────

        /// <summary>
        /// 通过文件名查找 asmdef 的 Assets 相对路径。
        /// asmdefFileName 须为完整文件名，如 "EUUI.asmdef" 或 "EUUI.Editor.asmdef"。
        /// </summary>
        public static string GetAsmdefPath(string asmdefFileName)
        {
            string searchName = Path.GetFileNameWithoutExtension(asmdefFileName);
            string[] guids = AssetDatabase.FindAssets($"{searchName} t:AssemblyDefinitionAsset");
            foreach (string g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileName(p).Equals(asmdefFileName, StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            return null;
        }

        // ── 内部工具 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 收集当前所有已生成 .cs 文件对应的 .sbn Asset 路径集合
        /// </summary>
        internal static HashSet<string> CollectActiveSbnPaths()
        {
            var sbnPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string editorDir = EUUITemplateLocator.GetEditorDirectory();
            if (string.IsNullOrEmpty(editorDir)) return sbnPaths;

            string staticDir = Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(Application.dataPath),
                    $"{editorDir}/Templates/Sbn/Static"));

            foreach (var outDir in new[] { GetPanelBaseOutputDirectory(), GetUIKitOutputDirectory() })
            {
                if (string.IsNullOrEmpty(outDir)) continue;
                string outFull = Path.GetFullPath(
                    Path.Combine(Path.GetDirectoryName(Application.dataPath), outDir));
                if (!Directory.Exists(outFull)) continue;

                foreach (var genFile in Directory.GetFiles(outFull, "*.Generated.cs", SearchOption.TopDirectoryOnly))
                {
                    string baseName = Path.GetFileName(genFile)
                        .Replace(".Generated.cs", ".sbn", StringComparison.OrdinalIgnoreCase);

                    if (Directory.Exists(staticDir))
                    {
                        foreach (var sbn in Directory.GetFiles(staticDir, baseName, SearchOption.AllDirectories))
                            sbnPaths.Add(ToAssetPath(sbn));
                    }
                }
            }
            return sbnPaths;
        }

        /// <summary>读取 .sbn 伴生 .json 中声明的 requiredAssemblies（运行时）</summary>
        public static string[] ReadSidecarRuntimeAssemblies(string sbnAssetPath)
            => ReadSidecarField(sbnAssetPath, "requiredAssemblies");

        /// <summary>读取 .sbn 伴生 .json 中声明的 editorAssemblies（编辑器）</summary>
        public static string[] ReadSidecarEditorAssemblies(string sbnAssetPath)
            => ReadSidecarField(sbnAssetPath, "editorAssemblies");

        /// <summary>
        /// 读取 .sbn 伴生 .json 中 namespaceVariables 对象，
        /// 返回 { 模板变量名 → 程序集名 } 的映射，供导出时动态注入 rootNamespace。
        /// </summary>
        public static Dictionary<string, string> ReadSidecarNamespaceVariables(string sbnAssetPath)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(sbnAssetPath)) return result;

            string sbnFull  = Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(Application.dataPath), sbnAssetPath));
            string jsonFull = Path.ChangeExtension(sbnFull, ".json");
            if (!File.Exists(jsonFull)) return result;

            try
            {
                string content    = File.ReadAllText(jsonFull, System.Text.Encoding.UTF8);
                var blockMatch    = Regex.Match(content,
                    @"""namespaceVariables""\s*:\s*\{([^}]*)\}", RegexOptions.Singleline);
                if (!blockMatch.Success) return result;

                var pairs = Regex.Matches(blockMatch.Groups[1].Value,
                    @"""([^""]+)""\s*:\s*""([^""]*)""");
                foreach (Match m in pairs)
                    result[m.Groups[1].Value] = m.Groups[2].Value;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[EUUI] 读取 namespaceVariables 失败 ({jsonFull}): {e.Message}");
            }
            return result;
        }

        /// <summary>
        /// 通过程序集名找到对应 .asmdef 文件并读取 rootNamespace。
        /// 若 asmdef 不存在或未声明 rootNamespace，返回空字符串。
        /// </summary>
        public static string GetAssemblyRootNamespace(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName)) return string.Empty;

            string asmdefPath = GetAsmdefPath(assemblyName + ".asmdef");
            if (string.IsNullOrEmpty(asmdefPath)) return string.Empty;

            string fullPath = Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(Application.dataPath), asmdefPath));
            if (!File.Exists(fullPath)) return string.Empty;

            string json = File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
            var m = Regex.Match(json, @"""rootNamespace""\s*:\s*""([^""]*)""");
            return m.Success ? m.Groups[1].Value : string.Empty;
        }

        private static string[] ReadSidecarField(string sbnAssetPath, string fieldName)
        {
            if (string.IsNullOrEmpty(sbnAssetPath)) return Array.Empty<string>();

            string sbnFull = Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(Application.dataPath), sbnAssetPath));
            string jsonFull = Path.ChangeExtension(sbnFull, ".json");
            if (!File.Exists(jsonFull)) return Array.Empty<string>();

            try
            {
                string content = File.ReadAllText(jsonFull, System.Text.Encoding.UTF8);
                var matches = Regex.Matches(
                    content, $"\"{Regex.Escape(fieldName)}\"\\s*:\\s*\\[([^\\]]*?)\\]",
                    RegexOptions.Singleline);
                if (matches.Count == 0) return Array.Empty<string>();

                var names = new List<string>();
                var entries = Regex.Matches(matches[0].Groups[1].Value, "\"([^\"]+)\"");
                foreach (Match m in entries)
                    names.Add(m.Groups[1].Value);
                return names.ToArray();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[EUUI] 读取伴生配置字段 {fieldName} 失败 ({jsonFull}): {e.Message}");
                return Array.Empty<string>();
            }
        }

        private static void RewriteAsmdefReferences(
            string asmdefFileName,
            string[] baseRefs,
            HashSet<string> required)
        {
            string asmdefPath = GetAsmdefPath(asmdefFileName);
            if (string.IsNullOrEmpty(asmdefPath))
            {
                Debug.LogWarning($"[EUUI] 未找到 {asmdefFileName}，跳过引用重算");
                return;
            }

            string fullPath = Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(Application.dataPath), asmdefPath));
            string json = File.ReadAllText(fullPath, System.Text.Encoding.UTF8);

            var allRefs = new List<string>(baseRefs);
            foreach (var r in required)
                if (!allRefs.Contains(r, StringComparer.OrdinalIgnoreCase))
                    allRefs.Add(r);

            string newRefsBlock = "\"references\": [\n"
                + string.Join(",\n", allRefs.Select(r => $"        \"{r}\""))
                + "\n    ]";
            json = Regex.Replace(
                json,
                @"""references""\s*:\s*\[[^\]]*\]",
                newRefsBlock,
                RegexOptions.Singleline);

            File.WriteAllText(fullPath, json, System.Text.Encoding.UTF8);
            // 不调用 ImportAsset：避免同步触发 Cursor IDE 插件 SyncAll → null-key 崩溃
            Debug.Log($"[EUUI] {asmdefFileName} references 已重算: [{string.Join(", ", allRefs)}]");
        }

        /// <summary>检测 assemblyName 是否已在 "references":[...] 数组内（精确范围匹配）</summary>
        private static bool IsAssemblyInReferencesArray(string json, string assemblyName)
        {
            var m = Regex.Match(json, @"""references""\s*:\s*\[([^\]]*)\]", RegexOptions.Singleline);
            if (!m.Success) return false;
            string inner = m.Groups[1].Value;
            // 在 references 内容中找到完整的 "assemblyName" 字符串
            return Regex.IsMatch(inner, $"\"{ Regex.Escape(assemblyName) }\"");
        }

        private static string AddReferenceToJson(string json, string assemblyName)
        {
            // 精确匹配 "references": [...] 块，避免 LastIndexOf(']') 命中
            // includePlatforms/versionDefines 等其他数组的 ] 导致 JSON 写坏
            var refMatch = Regex.Match(json, @"""references""\s*:\s*\[([^\]]*)\]", RegexOptions.Singleline);
            if (!refMatch.Success)
            {
                Debug.LogError("[EUUI] asmdef 格式异常：未找到 \"references\" 数组");
                return json;
            }

            // closingIdx = references 数组闭合 ] 的位置
            int closingIdx   = refMatch.Index + refMatch.Length - 1;
            string beforeClose = json.Substring(0, closingIdx).TrimEnd();
            // 若数组当前为空（前面紧跟 [），不加逗号
            string comma = beforeClose.EndsWith("[") ? "" : ",\n        ";

            return json.Substring(0, closingIdx)
                 + comma + $"\"{assemblyName}\"\n    "
                 + json.Substring(closingIdx);
        }

        private static string RemoveReferenceFromJson(string json, string assemblyName)
        {
            // 只在 "references":[...] 范围内执行移除，防止误删其他字段
            var refMatch = Regex.Match(json, @"""references""\s*:\s*\[([^\]]*)\]", RegexOptions.Singleline);
            if (!refMatch.Success) return json;

            string inner    = refMatch.Groups[1].Value;
            string newInner = Regex.Replace(
                inner,
                $@",?\s*""{Regex.Escape(assemblyName)}""\s*,?",
                m =>
                {
                    bool hadLeading  = m.Value.TrimStart().StartsWith(",");
                    bool hadTrailing = m.Value.TrimEnd().EndsWith(",");
                    return (hadLeading && hadTrailing) ? "," : "";
                });
            newInner = Regex.Replace(newInner, @",(\s*,)+", ",");
            newInner = Regex.Replace(newInner, @"^\s*,", "");   // 移除开头多余逗号
            newInner = Regex.Replace(newInner, @",\s*$", "");   // 移除结尾多余逗号

            // 重建整个 references 块
            string oldBlock = refMatch.Value;
            string newBlock = Regex.Replace(oldBlock, @"(?<=\[)[^\]]*(?=\])", newInner, RegexOptions.Singleline);
            return json.Substring(0, refMatch.Index) + newBlock + json.Substring(refMatch.Index + refMatch.Length);
        }

        // ── 输出目录定位（与 EUUIExtensionPanel 共享逻辑）──────────────────────

        internal static string GetPanelBaseOutputDirectory()
        {
            string[] guids = AssetDatabase.FindAssets("EUUIPanelBase t:MonoScript");
            if (guids == null || guids.Length == 0) return null;
            string scriptDir   = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(guids[0]))?.Replace("\\", "/");
            string generateDir = Path.Combine(scriptDir, "Generate", "PanelBase").Replace("\\", "/");
            EnsureDirectory(generateDir);
            return generateDir;
        }

        internal static string GetUIKitOutputDirectory()
        {
            string[] guids = AssetDatabase.FindAssets("EUUIKit t:MonoScript");
            if (guids == null || guids.Length == 0) return null;
            string scriptDir   = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(guids[0]))?.Replace("\\", "/");
            string generateDir = Path.Combine(scriptDir, "Generate", "UIKit").Replace("\\", "/");
            EnsureDirectory(generateDir);
            return generateDir;
        }

        internal static void EnsureDirectory(string assetRelDir)
        {
            string full = Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(Application.dataPath), assetRelDir));
            if (!Directory.Exists(full))
                Directory.CreateDirectory(full);
        }

        internal static string ToAssetPath(string fullPath)
        {
            string ap = fullPath.Replace("\\", "/");
            string dp = Application.dataPath.Replace("\\", "/");
            return ap.StartsWith(dp) ? "Assets" + ap.Substring(dp.Length) : ap;
        }
    }
}
#endif
