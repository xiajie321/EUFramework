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
        
        /// <summary>
        /// 移除根节点及所有子节点上的缺失脚本，避免保存 Prefab 时报错
        /// </summary>
        private static void RemoveMissingScripts(GameObject root)
        {
            if (root == null) return;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            int totalRemoved = 0;
            foreach (var t in transforms)
            {
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                totalRemoved += removed;
            }
            if (totalRemoved > 0)
                Debug.Log($"[EUUI] 已移除 {totalRemoved} 个缺失脚本（{root.name} 及其子节点）。");
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        private static void EnsureDirectory(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            
            string fullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), path));
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                AssetDatabase.Refresh();
            }
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

        private static Type GetComponentType(EUUINodeBindType bindType)
        {
            switch (bindType)
            {
                case EUUINodeBindType.RectTransform: return typeof(RectTransform);
                case EUUINodeBindType.Image: return typeof(UnityEngine.UI.Image);
                case EUUINodeBindType.Text: return typeof(UnityEngine.UI.Text);
                case EUUINodeBindType.Button: return typeof(UnityEngine.UI.Button);
                case EUUINodeBindType.TextMeshProUGUI:
                    var t = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
                    return t ?? typeof(Component);
                default: return typeof(RectTransform);
            }
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

            string folderPath = editorConfig.GetUIPrefabDir(desc.PackageType);
            EnsureDirectory(folderPath);

            string panelName = EditorSceneManager.GetActiveScene().name;
            string prefabPath = $"{folderPath}/{panelName}.prefab".Replace("\\", "/");

            PrefabUtility.SaveAsPrefabAsset(exportRoot, prefabPath);
            GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
            var nodes = prefabContents.GetComponentsInChildren<EUUINodeBind>(true);
            for (int i = nodes.Length - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(nodes[i]);
            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabContents);

            AssetDatabase.Refresh();
            Debug.Log($"[EUUI] Prefab 导出成功: {prefabPath}");
            EditorUtility.DisplayDialog("完成", $"Prefab 已导出至:\n{prefabPath}", "确定");
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

            if (!GenerateCode(panelName, members, desc, config))
                return;

            EditorPrefs.SetBool(k_AutoBindKey, true);
            EditorPrefs.SetString(k_PendingSceneKey, panelName);
            AssetDatabase.Refresh();

            if (!EditorApplication.isCompiling)
                OnScriptsReloaded();
        }

        /// <summary>
        /// 生成 MVC 架构集成代码（为面板生成 IController 分部类）
        /// </summary>
        private static void GenerateMVCIntegration(EUUITemplateConfig config, string ns, string className, string bindDir)
        {
            try
            {
                bool needGetArchitecture = !string.IsNullOrWhiteSpace(config.architectureName);
                bool hasArchitectureNamespace = !string.IsNullOrWhiteSpace(config.architectureNamespace);
                var controllerContext = new
                {
                    namespace_name = ns,
                    class_name = className,
                    need_get_architecture = needGetArchitecture,
                    architecture_name = config.architectureName?.Trim(),
                    has_architecture_namespace = hasArchitectureNamespace,
                    architecture_namespace = config.architectureNamespace?.Trim()
                };

                string controllerPath = Path.Combine(bindDir, className + ".IController.Generated.cs").Replace("\\", "/");
                if (!File.Exists(controllerPath))
                {
                    string result = EUUIBaseExporter.RenderTemplate("MVCArchitecture", controllerContext);
                    File.WriteAllText(controllerPath, result, System.Text.Encoding.UTF8);
                    Debug.Log($"[EUUI] IController partial 已生成: {controllerPath}");
                }
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                Debug.LogWarning("[EUUI] 注册表中未找到 MVCArchitecture 模板，跳过 MVC 集成代码生成。");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUUI] MVC 集成代码生成失败: {e.Message}");
            }
        }

        private static bool GenerateCode(string className, List<object> members, EUUIPanelDescription desc, EUUITemplateConfig config)
        {
            string baseClassName = desc.PanelType switch
            {
                EUUIType.Popup => "EUUIPopupPanelBase",
                EUUIType.Bar => "EUUIBarBase",
                _ => "EUUIPanelBase"
            };
            string fullBaseClass = $"{baseClassName}<{className}>";

            try
            {
                string ns = string.IsNullOrEmpty(desc.Namespace) ? config.namespaceName : desc.Namespace;
                string bindDir = string.IsNullOrEmpty(config.uiBindScriptsPath) ? "Assets/Script/Generate/UI" : config.uiBindScriptsPath;
                string logicDirBase = string.IsNullOrEmpty(config.uiLogicScriptsPath) ? "Assets/Script/Game/UI" : config.uiLogicScriptsPath;

                // 1. 生成 .Generated.cs（带绑定的 partial）
                string genResult = EUUIBaseExporter.RenderTemplate("PanelGenerated", new
                {
                    is_gen = true,
                    namespace_name = ns,
                    class_name = className,
                    members = members,
                    package_type = desc.PackageType.ToString()
                });
                EnsureDirectory(bindDir);
                string genPath = Path.Combine(bindDir, className + ".Generated.cs").Replace("\\", "/");
                File.WriteAllText(genPath, genResult, System.Text.Encoding.UTF8);
                Debug.Log($"[EUUI] 代码生成: {className}.Generated.cs");

                // 2. 若启用架构，生成 MVC 集成代码
                if (config.useArchitecture)
                    GenerateMVCIntegration(config, ns, className, bindDir);

                // 3. 若不存在则生成业务逻辑 .cs
                string logicDir = Path.Combine(logicDirBase, desc.PackageName).Replace("\\", "/");
                EnsureDirectory(logicDir);
                string logicPath = Path.Combine(logicDir, className + ".cs").Replace("\\", "/");
                if (!File.Exists(logicPath))
                {
                    string logicResult = EUUIBaseExporter.RenderTemplate("PanelGenerated", new
                    {
                        is_gen = false,
                        namespace_name = ns,
                        class_name = className,
                        base_class = fullBaseClass,
                        package_name = desc.PackageName,
                        use_architecture = config.useArchitecture
                    });
                    File.WriteAllText(logicPath, logicResult, System.Text.Encoding.UTF8);
                    Debug.Log($"[EUUI] 初始业务逻辑已生成: {logicPath}");
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUUI] 代码生成失败: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"代码生成失败: {e.Message}", "确定");
                return false;
            }
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (!EditorPrefs.GetBool(k_AutoBindKey, false)) return;
            EditorPrefs.SetBool(k_AutoBindKey, false);
            string panelName = EditorPrefs.GetString(k_PendingSceneKey, "");
            if (string.IsNullOrEmpty(panelName)) return;
            PerformBinding(panelName);
        }

        private static void PerformBinding(string panelName)
        {
            var config = GetConfig();
            var editorConfig = GetEditorConfig();
            if (config == null || editorConfig == null)
            {
                Debug.LogError("[EUUI] 绑定失败：未找到配置文件（EUUITemplateConfig / EUUIEditorConfig）");
                return;
            }

            GameObject exportRoot = GameObject.Find(editorConfig.exportRootName);
            if (exportRoot == null)
            {
                Debug.LogError($"[EUUI] 绑定失败：场景中找不到 [{editorConfig.exportRootName}]");
                return;
            }

            // 导出前移除 UIRoot 及子节点上的缺失脚本，避免保存 Prefab 时报错
            RemoveMissingScripts(exportRoot);

            var desc = exportRoot.GetComponentInParent<EUUIPanelDescription>() ?? UnityEngine.Object.FindFirstObjectByType<EUUIPanelDescription>();
            string ns = desc != null && !string.IsNullOrEmpty(desc.Namespace) ? desc.Namespace : config.namespaceName;

            Type type = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic) continue;
                string fullName = ns + "." + panelName;
                type = asm.GetType(fullName);
                if (type != null) break;
            }
            if (type == null)
            {
                Debug.LogError($"[EUUI] 绑定失败：找不到类型 {ns}.{panelName}，请检查编译是否通过。");
                return;
            }

            var comp = exportRoot.GetComponent(type) ?? exportRoot.AddComponent(type);
            var nodes = exportRoot.GetComponentsInChildren<EUUINodeBind>(true);
            foreach (var node in nodes)
            {
                var field = type.GetField(node.GetFinalMemberName(), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    Type compType = GetComponentType(node.GetFinalComponentType());
                    var targetComp = node.GetComponent(compType);
                    if (targetComp != null)
                        field.SetValue(comp, targetComp);
                }
            }

            FinalizePrefab(exportRoot, panelName, editorConfig);
        }

        private static void FinalizePrefab(GameObject exportRoot, string panelName, EUUIEditorConfig config)
        {
            var desc = exportRoot.GetComponentInParent<EUUIPanelDescription>() ?? UnityEngine.Object.FindFirstObjectByType<EUUIPanelDescription>();
            var pkgType = desc != null ? desc.PackageType : EUUIPackageType.Remote;
            string folderPath = config.GetUIPrefabDir(pkgType);
            EnsureDirectory(folderPath);
            string prefabPath = $"{folderPath}/{panelName}.prefab".Replace("\\", "/");

            GameObject savedRoot = PrefabUtility.SaveAsPrefabAsset(exportRoot, prefabPath);
            if (savedRoot == null)
            {
                Debug.LogError($"[EUUI] Prefab 保存失败（路径: {prefabPath}）。若控制台提示「缺失脚本」，请先移除 UIRoot 上的缺失组件、确保生成脚本已编译，再重新执行「自动绑定并导出 Prefab」。");
                return;
            }

            GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
            var nodes = prefabContents.GetComponentsInChildren<EUUINodeBind>(true);
            for (int i = nodes.Length - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(nodes[i]);
            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabContents);

            AssetDatabase.Refresh();
            Debug.Log($"[EUUI] 自动绑定完成，Prefab 已导出: {prefabPath}");
        }

        #endregion
    }
}
#endif
