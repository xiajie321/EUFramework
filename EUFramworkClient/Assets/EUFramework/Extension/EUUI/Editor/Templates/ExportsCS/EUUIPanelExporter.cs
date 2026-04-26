#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using EUFramework.Extension.EUUI;

namespace EUFramework.Extension.EUUI.Editor.Templates
{
    /// <summary>
    /// EUUI Panel 动态导出器 - 处理 WithData/EUUIPanel.Generated.sbn
    /// 负责从 Unity 场景采集 UI 节点数据，生成 Panel 代码并导出 Prefab
    /// 流程：校验 → 代码生成 → 编译后绑定 → 导出 Prefab
    /// </summary>
    public static class EUUIPanelExporter
    {
        private const string k_AutoBindKey = "EUUI_AutoBind_Pending";
        private const string k_PendingSceneKey = "EUUI_Pending_Scene";
        
        /// <summary>
        /// 获取模板与代码生成配置（公开方法，供其他编辑器类使用）
        /// </summary>
        public static EUUITemplateConfig GetConfig()
        {
            return EUUITemplateLocator.GetTemplateConfig();
        }

        /// <summary>
        /// 获取场景/资源制作配置（Prefab 路径、UIRoot 名称等）
        /// </summary>
        private static EUUIEditorConfig GetEditorConfig()
        {
            return AssetDatabase.LoadAssetAtPath<EUUIEditorConfig>(EUUISceneEditor.GetEditorConfigPath());
        }
        
        #region 变量名校验与路径辅助（参考 Doc UIEditorHelper）

        private static readonly HashSet<string> CSharpKeywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
            "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
            "using", "virtual", "void", "volatile", "while"
        };

        private static bool IsValidVariableName(string name, out string errorMessage)
        {
            errorMessage = "";
            if (string.IsNullOrEmpty(name)) { errorMessage = "名称不能为空"; return false; }
            if (CSharpKeywords.Contains(name)) { errorMessage = $"'{name}' 是 C# 关键字"; return false; }
            if (char.IsDigit(name[0])) { errorMessage = "不能以数字开头"; return false; }
            if (!Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$")) { errorMessage = "只能包含字母数字下划线"; return false; }
            return true;
        }

        private static string GetRelativePath(Transform child, Transform root)
        {
            if (child == root) return string.Empty;
            string path = child.name;
            Transform parent = child.parent;
            while (parent != null && parent != root)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        #endregion

        #region EUUINodeBindType → 类型名 / Type（用于代码生成与运行时绑定）

        private static string GetMemberTypeName(EUUINodeBindType bindType)
        {
            return bindType switch
            {
                EUUINodeBindType.RectTransform => "UnityEngine.RectTransform",
                EUUINodeBindType.Image => "UnityEngine.UI.Image",
                EUUINodeBindType.Text => "UnityEngine.UI.Text",
                EUUINodeBindType.Button => "UnityEngine.UI.Button",
                EUUINodeBindType.TextMeshProUGUI => "TMPro.TextMeshProUGUI",
                _ => "UnityEngine.RectTransform"
            };
        }

        #endregion

        /// <summary>
        /// 导出当前场景的 UIRoot 为 Prefab：保存到配置路径，并移除 Prefab 内的 EUUINodeBind 组件
        /// </summary>
        [EUHotboxEntry("导出 Prefab", "UI 制作", "将当前场景的 UIRoot 导出为干净的 Prefab")]
        // [MenuItem("EUFramework/拓展/EUUI/导出 Prefab", false, 105)]
        public static void ExportCurrentPanelToPrefab()
        {
            var editorConfig = GetEditorConfig();
            if (editorConfig == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到 EUUIEditorConfig，请先创建 UI 配置。", "确定");
                return;
            }

            var desc = UnityEngine.Object.FindFirstObjectByType<EUUIPanelDescription>();
            if (desc == null)
            {
                EditorUtility.DisplayDialog("错误", "场景中未找到 EUUIPanelDescription，无法导出。", "确定");
                return;
            }

            GameObject exportRoot = GameObject.Find(editorConfig.exportRootName);
            if (exportRoot == null)
            {
                EditorUtility.DisplayDialog("错误", $"场景中未找到 [{editorConfig.exportRootName}] 节点，请先创建 UI 场景。", "确定");
                return;
            }

            string panelName = EditorSceneManager.GetActiveScene().name;
            if (EUUIPrefabExporter.FinalizePrefab(exportRoot, panelName, editorConfig))
                EditorUtility.DisplayDialog("完成", "Prefab 已导出并清理 EUUINodeBind。", "确定");
        }

        #region 自动绑定流程：开始导出 → 代码生成 → 编译后绑定 → 导出 Prefab

        /// <summary>
        /// 开始自动绑定流程：校验命名 → 生成 Generated/逻辑代码 → 刷新后编译，编译完成后自动执行绑定并导出 Prefab
        /// </summary>
        [EUHotboxEntry("自动绑定导出", "UI 制作", "代码生成 + 字段绑定 + 导出 Prefab 完整自动流程")]
        // [MenuItem("EUFramework/拓展/EUUI/自动绑定并导出 Prefab", false, 106)]
        public static void StartExportProcess()
        {
            var config = GetConfig();
            var editorConfig = GetEditorConfig();
            if (config == null || editorConfig == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到配置文件，请先创建 EUUITemplateConfig 与 EUUIEditorConfig。", "确定");
                return;
            }

            var desc = UnityEngine.Object.FindFirstObjectByType<EUUIPanelDescription>();
            if (desc == null)
            {
                EditorUtility.DisplayDialog("错误", "场景中未发现 EUUIPanelDescription，无法导出。", "确定");
                return;
            }

            GameObject exportRoot = GameObject.Find(editorConfig.exportRootName);
            if (exportRoot == null)
            {
                Debug.LogError($"[EUUI] 未找到 [{editorConfig.exportRootName}]，请先创建模板。");
                EditorUtility.DisplayDialog("错误", $"未找到 [{editorConfig.exportRootName}]，请先创建 UI 场景。", "确定");
                return;
            }

            string panelName = EditorSceneManager.GetActiveScene().name;
            var bindNodes = exportRoot.GetComponentsInChildren<EUUINodeBind>(true);
            var members = new List<object>();
            var usedNames = new HashSet<string>();

            foreach (var node in bindNodes)
            {
                string finalName = node.GetFinalMemberName();
                if (!IsValidVariableName(finalName, out string errorMsg))
                {
                    string path = GetRelativePath(node.transform, exportRoot.transform);
                    Debug.LogError($"[EUUI] 导出失败：节点 [{node.name}] 命名非法: {errorMsg}\n路径: {path}");
                    EditorUtility.DisplayDialog("非法命名", $"节点 [{node.name}] 变量名非法：\n{errorMsg}", "确定");
                    return;
                }
                if (usedNames.Contains(finalName))
                {
                    string path = GetRelativePath(node.transform, exportRoot.transform);
                    Debug.LogError($"[EUUI] 导出失败：重复的变量名 [{finalName}]\n路径: {path}");
                    EditorUtility.DisplayDialog("命名冲突", $"发现重复的变量名: {finalName}", "确定");
                    return;
                }
                usedNames.Add(finalName);
                members.Add(new { name = finalName, type = GetMemberTypeName(node.GetFinalComponentType()) });
            }

            if (!EUUIPanelCodeGenerator.GenerateCode(panelName, members, desc, config))
                return;

            EditorPrefs.SetBool(k_AutoBindKey, true);
            EditorPrefs.SetString(k_PendingSceneKey, panelName);
            AssetDatabase.Refresh();

            if (!EditorApplication.isCompiling)
                OnScriptsReloaded();
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (!EditorPrefs.GetBool(k_AutoBindKey, false)) return;
            EditorPrefs.SetBool(k_AutoBindKey, false);
            string panelName = EditorPrefs.GetString(k_PendingSceneKey, "");
            if (string.IsNullOrEmpty(panelName)) return;
            SchedulePrefabBinding(panelName);
        }

        private static void SchedulePrefabBinding(string panelName)
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    SchedulePrefabBinding(panelName);
                    return;
                }

                EUUIPrefabBinder.PerformBinding(panelName);
            };
        }

        #endregion
    }
}
#endif
