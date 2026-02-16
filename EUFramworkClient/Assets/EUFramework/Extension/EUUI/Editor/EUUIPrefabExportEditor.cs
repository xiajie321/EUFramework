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

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// EUUI Prefab 导出与自动绑定：校验 → 代码生成 → 编译后绑定 → 导出 Prefab（参考 Doc/UIEditor）
    /// </summary>
    public static class EUUIPrefabExportEditor
    {
        private const string k_AutoBindKey = "EUUI_AutoBind_Pending";
        private const string k_PendingSceneKey = "EUUI_Pending_Scene";

        /// <summary>
        /// 通过 AssetDatabase 解析配置资源路径（不写死 EUUI 目录），便于扩展移动
        /// </summary>
        private static string GetEditorConfigAssetPath()
        {
            string[] guids = AssetDatabase.FindAssets("EUUIEditorConfig");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path != null && path.EndsWith("EUUIEditorConfig.asset", StringComparison.OrdinalIgnoreCase))
                    return path;
            }
            return EUUISceneEditor.GetEditorConfigPath();
        }

        internal static EUUIEditorConfig GetConfig()
        {
            return AssetDatabase.LoadAssetAtPath<EUUIEditorConfig>(GetEditorConfigAssetPath());
        }

        /// <summary>
        /// 解析模板文件路径：优先按资源名查找 EUUIPanel.Generated.sbn，否则相对当前脚本目录
        /// </summary>
        private static string GetTemplateFullPath()
        {
            string[] guids = AssetDatabase.FindAssets("EUUIPanel.Generated");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path != null && path.EndsWith("EUUIPanel.Generated.sbn", StringComparison.OrdinalIgnoreCase))
                    return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), path));
            }
            var scriptGuid = AssetDatabase.FindAssets("EUUIPrefabExportEditor t:MonoScript");
            if (scriptGuid != null && scriptGuid.Length > 0)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuid[0]);
                string scriptDir = Path.GetDirectoryName(scriptPath).Replace("\\", "/");
                string relativeTemplate = scriptDir + "/Templates/EUUIPanel.Generated.sbn";
                return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), relativeTemplate));
            }
            return null;
        }

        /// <summary>
        /// 解析 MVC 模板文件路径
        /// </summary>
        private static string GetMVCTemplateFullPath()
        {
            return GetTemplateFullPathByName("EUUI.MVC");
        }

        /// <summary>
        /// 解析 EUUIPanelBase.EURes 模板文件路径
        /// </summary>
        private static string GetPanelBaseEUResTemplateFullPath()
        {
            return GetTemplateFullPathByName("EUUIPanelBase.EURes");
        }

        /// <summary>
        /// 解析 EUUIKit.EURes 模板文件路径
        /// </summary>
        private static string GetKitEUResTemplateFullPath()
        {
            return GetTemplateFullPathByName("EUUIKit.EURes");
        }

        /// <summary>
        /// 通用模板路径解析方法
        /// </summary>
        private static string GetTemplateFullPathByName(string templateNameWithoutExt)
        {
            string[] guids = AssetDatabase.FindAssets(templateNameWithoutExt);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path != null && path.EndsWith($"{templateNameWithoutExt}.sbn", StringComparison.OrdinalIgnoreCase))
                    return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), path));
            }
            var scriptGuid = AssetDatabase.FindAssets("EUUIPrefabExportEditor t:MonoScript");
            if (scriptGuid != null && scriptGuid.Length > 0)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuid[0]);
                string scriptDir = Path.GetDirectoryName(scriptPath).Replace("\\", "/");
                string relativeTemplate = scriptDir + $"/Templates/{templateNameWithoutExt}.sbn";
                return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), relativeTemplate));
            }
            return null;
        }

        private static void EnsureDirectory(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
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
        // [MenuItem("EUFramework/拓展/EUUI/导出 Prefab", false, 105)]
        public static void ExportCurrentPanelToPrefab()
        {
            var config = GetConfig();
            if (config == null)
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

            GameObject exportRoot = GameObject.Find(config.exportRootName);
            if (exportRoot == null)
            {
                EditorUtility.DisplayDialog("错误", $"场景中未找到 [{config.exportRootName}] 节点，请先创建 UI 场景。", "确定");
                return;
            }

            string folderPath = config.GetUIPrefabDir(desc.PackageType);
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
        // [MenuItem("EUFramework/拓展/EUUI/自动绑定并导出 Prefab", false, 106)]
        public static void StartExportProcess()
        {
            var config = GetConfig();
            if (config == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到 EUUIEditorConfig，请先创建 UI 配置。", "确定");
                return;
            }

            var desc = UnityEngine.Object.FindFirstObjectByType<EUUIPanelDescription>();
            if (desc == null)
            {
                EditorUtility.DisplayDialog("错误", "场景中未发现 EUUIPanelDescription，无法导出。", "确定");
                return;
            }

            GameObject exportRoot = GameObject.Find(config.exportRootName);
            if (exportRoot == null)
            {
                Debug.LogError($"[EUUI] 未找到 [{config.exportRootName}]，请先创建模板。");
                EditorUtility.DisplayDialog("错误", $"未找到 [{config.exportRootName}]，请先创建 UI 场景。", "确定");
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
        /// 生成 EURes 扩展代码（一次性生成，位于 Script 目录）
        /// </summary>
        public static void GenerateEUResExtensions()
        {
            var config = GetConfig();
            if (config == null || !config.enableEUResExtension)
            {
                Debug.Log("[EUUI] EURes 扩展未启用，跳过生成。");
                return;
            }

            // 获取 Script 目录（与 EUUIPanelBase.cs 同级）
            var scriptGuids = AssetDatabase.FindAssets("EUUIPanelBase t:MonoScript");
            string scriptDir = "Assets/EUFramework/Extension/EUUI/Script"; // 兜底
            if (scriptGuids != null && scriptGuids.Length > 0)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuids[0]);
                scriptDir = Path.GetDirectoryName(scriptPath).Replace("\\", "/");
            }

            try
            {
                // 1. 生成 EUUIPanelBase.EURes 扩展
                string panelBaseTemplatePath = GetPanelBaseEUResTemplateFullPath();
                if (!string.IsNullOrEmpty(panelBaseTemplatePath) && File.Exists(panelBaseTemplatePath))
                {
                    string templateStr = File.ReadAllText(panelBaseTemplatePath);
                    var template = Scriban.Template.Parse(templateStr);
                    string result = template.Render(new { });
                    
                    string outputPath = Path.Combine(scriptDir, "EUUIPanelBaseEUResExtensions.Generated.cs").Replace("\\", "/");
                    File.WriteAllText(outputPath, result, System.Text.Encoding.UTF8);
                    Debug.Log($"[EUUI] EUUIPanelBase.EURes 扩展已生成: {outputPath}");
                }

                // 2. 生成 EUUIKit.EURes 分部类
                string kitTemplatePath = GetKitEUResTemplateFullPath();
                if (!string.IsNullOrEmpty(kitTemplatePath) && File.Exists(kitTemplatePath))
                {
                    string templateStr = File.ReadAllText(kitTemplatePath);
                    var template = Scriban.Template.Parse(templateStr);
                    string result = template.Render(new { });
                    
                    string outputPath = Path.Combine(scriptDir, "EUUIKit.EURes.Generated.cs").Replace("\\", "/");
                    File.WriteAllText(outputPath, result, System.Text.Encoding.UTF8);
                    Debug.Log($"[EUUI] EUUIKit.EURes 扩展已生成: {outputPath}");
                }

                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("完成", "EURes 扩展代码生成成功！", "确定");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUUI] EURes 扩展生成失败: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"EURes 扩展生成失败:\n{e.Message}", "确定");
            }
        }

        /// <summary>
        /// 生成 MVC 架构集成代码（为面板生成 IController 分部类）
        /// </summary>
        private static void GenerateMVCIntegration(EUUIEditorConfig config, string ns, string className, string bindDir)
        {
            string mvcTemplatePath = GetMVCTemplateFullPath();
            if (string.IsNullOrEmpty(mvcTemplatePath) || !File.Exists(mvcTemplatePath))
            {
                Debug.LogWarning($"[EUUI] 未找到 EUUI.MVC.sbn 模板，跳过 MVC 集成代码生成。");
                return;
            }

            try
            {
                string mvcTemplateStr = File.ReadAllText(mvcTemplatePath);
                var mvcTemplate = Scriban.Template.Parse(mvcTemplateStr);
                bool needGetArchitecture = !string.IsNullOrWhiteSpace(config.architectureName);

                // 为每个面板生成 IController partial
                string controllerPath = Path.Combine(bindDir, className + ".IController.Generated.cs").Replace("\\", "/");
                if (!File.Exists(controllerPath))
                {
                    var controllerContext = new
                    {
                        namespace_name = ns,
                        class_name = className,
                        need_get_architecture = needGetArchitecture,
                        architecture_name = config.architectureName?.Trim(),
                        architecture_namespace = config.architectureNamespace?.Trim()
                    };
                    string controllerResult = mvcTemplate.Render(controllerContext);
                    File.WriteAllText(controllerPath, controllerResult, System.Text.Encoding.UTF8);
                    Debug.Log($"[EUUI] IController partial 已生成: {controllerPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUUI] MVC 集成代码生成失败: {e.Message}");
            }
        }

        private static bool GenerateCode(string className, List<object> members, EUUIPanelDescription desc, EUUIEditorConfig config)
        {
            // 根据面板类型确定基类
            string baseClassName = desc.PanelType switch
            {
                EUUIType.Popup => "EUUIPopupPanelBase",
                EUUIType.Bar => "EUUIBarBase",
                _ => "EUUIPanelBase"
            };
            string fullBaseClass = $"{baseClassName}<{className}>";

            string templatePath = GetTemplateFullPath();
            if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
            {
                Debug.LogError($"[EUUI] 无法找到 Scriban 模板 (EUUIPanel.Generated.sbn)。");
                EditorUtility.DisplayDialog("错误", "未找到 EUUIPanel.Generated.sbn 模板。", "确定");
                return false;
            }

            try
            {
                string templateStr = File.ReadAllText(templatePath);
                var template = Scriban.Template.Parse(templateStr);

                string ns = string.IsNullOrEmpty(desc.Namespace) ? config.namespaceName : desc.Namespace;
                string bindDir = string.IsNullOrEmpty(config.uiBindScriptsPath) ? "Assets/Script/Generate/UI" : config.uiBindScriptsPath;
                string logicDirBase = string.IsNullOrEmpty(config.uiLogicScriptsPath) ? "Assets/Script/Game/UI" : config.uiLogicScriptsPath;

                // 1. 生成 .Generated.cs（带绑定的 partial）
                var genContext = new 
                { 
                    is_gen = true, 
                    namespace_name = ns, 
                    class_name = className, 
                    members = members 
                };
                string genResult = template.Render(genContext);
                EnsureDirectory(bindDir);
                string genPath = Path.Combine(bindDir, className + ".Generated.cs").Replace("\\", "/");
                File.WriteAllText(genPath, genResult, System.Text.Encoding.UTF8);
                Debug.Log($"[EUUI] 代码生成: {className}.Generated.cs");

                // 2. 若启用架构，生成 MVC 集成代码
                // - 有中间层：生成中间层基类（一次性）
                // - 无中间层：为每个面板生成 IController partial
                if (config.useArchitecture)
                {
                    GenerateMVCIntegration(config, ns, className, bindDir);
                }

                // 3. 若不存在则生成业务逻辑 .cs
                string logicDir = Path.Combine(logicDirBase, desc.PackageName).Replace("\\", "/");
                EnsureDirectory(logicDir);
                string logicPath = Path.Combine(logicDir, className + ".cs").Replace("\\", "/");
                if (!File.Exists(logicPath))
                {
                    var logicContext = new 
                    { 
                        is_gen = false, 
                        namespace_name = ns, 
                        class_name = className, 
                        base_class = fullBaseClass, 
                        package_name = desc.PackageName,
                        use_architecture = config.useArchitecture
                    };
                    string logicResult = template.Render(logicContext);
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
            if (config == null)
            {
                Debug.LogError("[EUUI] 绑定失败：未找到 EUUIEditorConfig");
                return;
            }

            GameObject exportRoot = GameObject.Find(config.exportRootName);
            if (exportRoot == null)
            {
                Debug.LogError($"[EUUI] 绑定失败：场景中找不到 [{config.exportRootName}]");
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

            FinalizePrefab(exportRoot, panelName, config);
        }

        private static void FinalizePrefab(GameObject exportRoot, string panelName, EUUIEditorConfig config)
        {
            var desc = exportRoot.GetComponentInParent<EUUIPanelDescription>() ?? UnityEngine.Object.FindFirstObjectByType<EUUIPanelDescription>();
            var pkgType = desc != null ? desc.PackageType : EUUIPackageType.Remote;
            string folderPath = config.GetUIPrefabDir(pkgType);
            EnsureDirectory(folderPath);
            string prefabPath = $"{folderPath}/{panelName}.prefab".Replace("\\", "/");

            PrefabUtility.SaveAsPrefabAsset(exportRoot, prefabPath);

            if (AssetDatabase.LoadMainAssetAtPath(prefabPath) == null)
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
