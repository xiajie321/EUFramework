#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using EUFramework.Extension.EUUI;

namespace EUFramework.Extension.EUUI.Editor.Templates
{
    /// <summary>
    /// Generates panel binding, business, and optional architecture integration code.
    /// </summary>
    internal static class EUUIPanelCodeGenerator
    {
        public static bool GenerateCode(
            string className,
            List<object> members,
            EUUIPanelDescription desc,
            EUUITemplateConfig config)
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

                if (config.useArchitecture)
                    GenerateMVCIntegration(config, ns, className, bindDir);

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

        private static void GenerateMVCIntegration(EUUITemplateConfig config, string ns, string className, string bindDir)
        {
            try
            {
                bool needGetArchitecture = !string.IsNullOrWhiteSpace(config.architectureName);
                bool hasArchitectureNamespace = !string.IsNullOrWhiteSpace(config.architectureNamespace);
                string customController = config.mvcControllerInterfaceQualifiedName?.Trim();
                string controllerBaseType;
                string architectureInterfaceType;
                if (string.IsNullOrEmpty(customController))
                {
                    controllerBaseType = "EUFramework.Core.MVC.Interface.IController";
                    architectureInterfaceType = "EUFramework.Core.MVC.Interface.IArchitecture";
                }
                else
                {
                    controllerBaseType = customController;
                    int lastDot = customController.LastIndexOf('.');
                    architectureInterfaceType = lastDot > 0
                        ? customController.Substring(0, lastDot) + ".IArchitecture"
                        : "EUFramework.Core.IArchitecture";
                }

                var controllerContext = new
                {
                    namespace_name = ns,
                    class_name = className,
                    need_get_architecture = needGetArchitecture,
                    architecture_name = config.architectureName?.Trim(),
                    has_architecture_namespace = hasArchitectureNamespace,
                    architecture_namespace = config.architectureNamespace?.Trim(),
                    controller_base_type = controllerBaseType,
                    architecture_interface_type = architectureInterfaceType
                };

                string controllerPath = Path.Combine(bindDir, className + ".IController.Generated.cs").Replace("\\", "/");
                if (!File.Exists(controllerPath))
                {
                    string result = EUUIBaseExporter.RenderTemplate("MVCArchitecture", controllerContext);
                    File.WriteAllText(controllerPath, result, System.Text.Encoding.UTF8);
                    Debug.Log($"[EUUI] IController partial 已生成: {controllerPath}");
                }
            }
            catch (KeyNotFoundException)
            {
                Debug.LogWarning("[EUUI] 注册表中未找到 MVCArchitecture 模板，跳过 MVC 集成代码生成。");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUUI] MVC 集成代码生成失败: {e.Message}");
            }
        }

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
    }
}
#endif
