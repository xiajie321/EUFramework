#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using EUFramework.Extension.EUUI;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// 编辑器启动时扫描所有程序集，收集带 [EUHotboxEntry] 特性的无参静态方法
    /// 以及实现 IEUHotboxAction 接口的类型，缓存为可供面板使用的 DiscoveredEntry 列表。
    /// </summary>
    [InitializeOnLoad]
    public static class EUHotboxEntryScanner
    {
        // ── 发现的条目记录 ─────────────────────────────────────────────────────

        public class DiscoveredEntry
        {
            public string EntryId;
            public string Label;
            public string Group;
            public string Tooltip;
            public Action Invoke;
        }

        private static List<DiscoveredEntry>           _entries;
        private static Dictionary<string, DiscoveredEntry> _entryMap;

        public static IReadOnlyList<DiscoveredEntry> AllEntries
        {
            get
            {
                if (_entries == null) Scan();
                return _entries;
            }
        }

        static EUHotboxEntryScanner()
        {
            EditorApplication.delayCall += () =>
            {
                _entries  = null;
                _entryMap = null;
                _ = AllEntries;
            };
        }

        // ── 扫描 ─────────────────────────────────────────────────────────────

        private static void Scan()
        {
            var result     = new List<DiscoveredEntry>();
            var map        = new Dictionary<string, DiscoveredEntry>(StringComparer.Ordinal);
            var attrType   = typeof(EUHotboxEntryAttribute);
            var actionType = typeof(IEUHotboxAction);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (ShouldSkipAssembly(assembly)) continue;

                Type[] types;
                try   { types = assembly.GetTypes(); }
                catch { continue; }

                foreach (var type in types)
                {
                    // ── 静态方法 + [EUHotboxEntry] ────────────────────────────
                    foreach (var method in type.GetMethods(
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (!(method.GetCustomAttribute(attrType) is EUHotboxEntryAttribute attr)) continue;
                        if (method.GetParameters().Length != 0) continue;
                        if (method.ContainsGenericParameters)   continue;

                        string id = MakeMethodId(type, method);
                        if (map.ContainsKey(id)) continue;

                        var capturedMethod = method;
                        var entry = new DiscoveredEntry
                        {
                            EntryId = id,
                            Label   = attr.Label,
                            Group   = attr.Group,
                            Tooltip = attr.Tooltip,
                            Invoke  = () =>
                            {
                                try   { capturedMethod.Invoke(null, null); }
                                catch (TargetInvocationException ex)
                                {
                                    Debug.LogError(
                                        $"[EUHotbox] 执行 {id} 失败: {ex.InnerException?.Message ?? ex.Message}");
                                }
                            }
                        };
                        result.Add(entry);
                        map[id] = entry;
                    }

                    // ── IEUHotboxAction 实现类 ────────────────────────────────
                    if (!actionType.IsAssignableFrom(type) || type.IsAbstract || type.IsInterface) continue;
                    if (type.GetConstructor(Type.EmptyTypes) == null) continue;

                    string actionId = type.FullName ?? type.Name;
                    if (map.ContainsKey(actionId)) continue;

                    try
                    {
                        var instance = (IEUHotboxAction)Activator.CreateInstance(type);
                        var actionEntry = new DiscoveredEntry
                        {
                            EntryId = actionId,
                            Label   = instance.Label,
                            Group   = string.IsNullOrEmpty(instance.Group) ? "通用" : instance.Group,
                            Tooltip = instance.Tooltip,
                            Invoke  = instance.Execute
                        };
                        result.Add(actionEntry);
                        map[actionId] = actionEntry;
                    }
                    catch { }
                }
            }

            _entries  = result.OrderBy(e => e.Group).ThenBy(e => e.Label).ToList();
            _entryMap = map;
        }

        // ── 公共 API ──────────────────────────────────────────────────────────

        public static DiscoveredEntry FindById(string id)
        {
            _ = AllEntries;
            return (_entryMap != null && _entryMap.TryGetValue(id, out var e)) ? e : null;
        }

        /// <summary>强制重新扫描（条目增减后手动刷新）</summary>
        public static void Refresh()
        {
            _entries  = null;
            _entryMap = null;
            _ = AllEntries;
        }

        // ── Config SO 查找 / 创建 ─────────────────────────────────────────────

        private const string k_FallbackPath =
            "Assets/EUFramework/Extension/EUUI/Editor/EditorSO/EUHotboxConfig.asset";

        public static EUHotboxConfigSO GetOrCreateConfig()
        {
            var so = AssetDatabase.LoadAssetAtPath<EUHotboxConfigSO>(k_FallbackPath);
            if (so != null) return so;

            var guids = AssetDatabase.FindAssets("t:EUHotboxConfigSO");
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<EUHotboxConfigSO>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));

            // 自动创建
            string dir = Path.GetDirectoryName(k_FallbackPath);
            EUUIAsmdefHelper.EnsureDirectory(dir);
            var newSO = ScriptableObject.CreateInstance<EUHotboxConfigSO>();
            AssetDatabase.CreateAsset(newSO, k_FallbackPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[EUUI] 已自动创建功能编排配置: {k_FallbackPath}");
            return newSO;
        }

        // ── 辅助 ──────────────────────────────────────────────────────────────

        private static string MakeMethodId(Type type, MethodInfo method) =>
            $"{type.FullName}::{method.Name}";

        private static bool ShouldSkipAssembly(Assembly asm)
        {
            string name = asm.GetName().Name;
            return name == "UnityEditor"
                || name == "UnityEngine"
                || name.StartsWith("UnityEngine.")
                || name.StartsWith("Unity.")
                || name.StartsWith("System")
                || name == "mscorlib"
                || name.StartsWith("Mono.")
                || name.StartsWith("nunit.")
                || name.StartsWith("JetBrains.")
                || name == "netstandard"
                || name.StartsWith("Microsoft.");
        }
    }
}
#endif
