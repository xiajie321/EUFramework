#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        // ── JSON 数据模型 ────────────────────────────────────────────────────────

        [Serializable]
        private class StringPair
        {
            public string key;
            public string value;
        }

        [Serializable]
        private class SidecarConfig
        {
            public List<string> requiredAssemblies = new List<string>();
            public List<string> editorAssemblies = new List<string>();
            public List<StringPair> namespaceVariables = new List<StringPair>();

            public Dictionary<string, string> GetNamespaceVariables()
            {
                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in namespaceVariables ?? new List<StringPair>())
                {
                    if (!string.IsNullOrEmpty(pair?.key))
                        result[pair.key] = pair.value;
                }

                return result;
            }
        }

        [Serializable]
        private class AsmdefVersionDefine
        {
            public string name;
            public string expression;
            public string define;
        }

        [Serializable]
        private class AsmdefData
        {
            public string name;
            public string rootNamespace;
            public List<string> references = new List<string>();
            public List<string> includePlatforms = new List<string>();
            public List<string> excludePlatforms = new List<string>();
            public bool allowUnsafeCode;
            public bool overrideReferences;
            public List<string> precompiledReferences = new List<string>();
            public bool autoReferenced = true;
            public List<string> defineConstraints = new List<string>();
            public List<AsmdefVersionDefine> versionDefines = new List<AsmdefVersionDefine>();
            public bool noEngineReferences;
        }

        // ── 常量：各 asmdef 的基础引用（永远保留）────────────────────────────────
        private static readonly string[] k_RuntimeBaseRefs =
        {
            "UniTask",
            "Unity.InputSystem",
            "Unity.InputSystem.ForUI",
            "Unity.RenderPipelines.Universal.Runtime"
        };

        private static readonly string[] k_EditorBaseRefs = { "EUUI", "UniTask" };

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
            var asmdef = ReadAsmdef(fullPath);
            var refs = asmdef.references ?? new List<string>();
            bool hasRef = refs.Any(r => string.Equals(r, assemblyName, StringComparison.OrdinalIgnoreCase));

            if (add && hasRef) return;
            if (!add && !hasRef) return;

            if (add)
            {
                refs.Add(assemblyName);
                Debug.Log($"[EUUI] 已向 {asmdefFileName} 添加引用: {assemblyName}");
            }
            else
            {
                refs.RemoveAll(r => string.Equals(r, assemblyName, StringComparison.OrdinalIgnoreCase));
                Debug.Log($"[EUUI] 已从 {asmdefFileName} 移除引用: {assemblyName}");
            }

            asmdef.references = refs;
            WriteAsmdef(fullPath, asmdef);
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
            var editorRequired = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var sbn in sbnPaths)
            {
                foreach (var asm in ReadSidecarRuntimeAssemblies(sbn))
                    runtimeRequired.Add(asm);
                foreach (var asm in ReadSidecarEditorAssemblies(sbn))
                    editorRequired.Add(asm);
            }

            // 直接写文件，不调用 ImportAsset；延迟一帧再 Refresh 让 Unity 重新编译
            RewriteAsmdefReferences("EUUI.asmdef", k_RuntimeBaseRefs, runtimeRequired);
            RewriteAsmdefReferences("EUUI.Editor.asmdef", k_EditorBaseRefs, editorRequired);
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

            string[] guids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset");
            foreach (string g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                // 1. 文件名（去空格）匹配
                string fileBaseName = Path.GetFileNameWithoutExtension(p).Replace(" ", "");
                if (fileBaseName.Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
                    return true;

                // 2. 读 asmdef JSON 中的 "name" 字段匹配
                try
                {
                    string fullPath = Path.GetFullPath(
                        Path.Combine(Path.GetDirectoryName(Application.dataPath), p));
                    if (!File.Exists(fullPath)) continue;
                    string name = ReadAsmdef(fullPath).name;
                    if (!string.IsNullOrEmpty(name) && name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                catch
                {
                    /* 读取失败时跳过 */
                }
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

            string outDir = GetStaticGeneratedOutputDirectory();
            if (!string.IsNullOrEmpty(outDir))
            {
                string outFull = Path.GetFullPath(
                    Path.Combine(Path.GetDirectoryName(Application.dataPath), outDir));
                if (Directory.Exists(outFull))
                {
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
            }

            return sbnPaths;
        }

        /// <summary>读取 .sbn 伴生 .json 中声明的 requiredAssemblies（运行时）</summary>
        public static string[] ReadSidecarRuntimeAssemblies(string sbnAssetPath)
        {
            var config = ReadSidecarConfig(sbnAssetPath);
            return config?.requiredAssemblies?.ToArray() ?? Array.Empty<string>();
        }

        /// <summary>读取 .sbn 伴生 .json 中声明的 editorAssemblies（编辑器）</summary>
        public static string[] ReadSidecarEditorAssemblies(string sbnAssetPath)
        {
            var config = ReadSidecarConfig(sbnAssetPath);
            return config?.editorAssemblies?.ToArray() ?? Array.Empty<string>();
        }

        /// <summary>
        /// 读取 .sbn 伴生 .json 中 namespaceVariables 列表，
        /// 返回 { 模板变量名 → 程序集名 } 的映射，供导出时动态注入 rootNamespace。
        /// </summary>
        public static Dictionary<string, string> ReadSidecarNamespaceVariables(string sbnAssetPath)
        {
            var config = ReadSidecarConfig(sbnAssetPath);
            return config?.GetNamespaceVariables()
                   ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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

            try
            {
                return ReadAsmdef(fullPath).rootNamespace ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static SidecarConfig ReadSidecarConfig(string sbnAssetPath)
        {
            if (string.IsNullOrEmpty(sbnAssetPath)) return null;

            string sbnFull = Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(Application.dataPath), sbnAssetPath));
            string jsonFull = Path.ChangeExtension(sbnFull, ".json");
            if (!File.Exists(jsonFull)) return null;

            try
            {
                return JsonUtility.FromJson<SidecarConfig>(
                    File.ReadAllText(jsonFull, System.Text.Encoding.UTF8));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[EUUI] 读取伴生配置失败 ({jsonFull}): {e.Message}");
                return null;
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
            var asmdef = ReadAsmdef(fullPath);

            var allRefs = new List<string>(baseRefs);
            foreach (var r in required)
                if (!allRefs.Contains(r, StringComparer.OrdinalIgnoreCase))
                    allRefs.Add(r);

            asmdef.references = allRefs;
            WriteAsmdef(fullPath, asmdef);
            // 不调用 ImportAsset：避免同步触发 Cursor IDE 插件 SyncAll → null-key 崩溃
            Debug.Log($"[EUUI] {asmdefFileName} references 已重算: [{string.Join(", ", allRefs)}]");
        }

        // ── 输出目录定位（与 EUUIExtensionPanel 共享逻辑）──────────────────────

        internal static string GetStaticGeneratedOutputDirectory()
        {
            string scriptPath = FindScriptExact("EUUIInterface");
            if (string.IsNullOrEmpty(scriptPath)) return null;
            string scriptDir = Path.GetDirectoryName(scriptPath)?.Replace("\\", "/");
            string scriptRoot = Path.GetDirectoryName(scriptDir)?.Replace("\\", "/");
            string generateDir = Path.Combine(scriptRoot, "Generate", "Static").Replace("\\", "/");
            EnsureDirectory(generateDir);
            return generateDir;
        }

        /// <summary>
        /// 通过精确文件名（不含扩展名）定位脚本资源路径
        /// 避免 FindAssets 子字符串匹配导致命中同名前缀的其他脚本（如 EUUIPanelBaseListExtension）
        /// </summary>
        private static string FindScriptExact(string scriptName)
        {
            string[] guids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
            if (guids == null) return null;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == scriptName)
                    return path;
            }

            return null;
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

        private static AsmdefData ReadAsmdef(string fullPath)
        {
            if (!File.Exists(fullPath))
                return new AsmdefData();

            var data = JsonUtility.FromJson<AsmdefData>(
                File.ReadAllText(fullPath, System.Text.Encoding.UTF8));
            return data ?? new AsmdefData();
        }

        private static void WriteAsmdef(string fullPath, AsmdefData asmdef)
        {
            File.WriteAllText(fullPath, JsonUtility.ToJson(asmdef, true), System.Text.Encoding.UTF8);
        }
    }
}
#endif