#if UNITY_EDITOR
using System;
using System.IO;
using Scriban.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using EUFramework.Extension.EUUI.Editor.Templates;

namespace EUFramework.Extension.EUUI.Editor
{
    internal class EUUIModulePanel : IEUUIPanel
    {
        public void Build(VisualElement contentArea)
        {
            contentArea.Clear();
            contentArea.style.alignItems     = Align.Stretch;
            contentArea.style.justifyContent = Justify.FlexStart;

            contentArea.Add(EUUIEditorWindowHelper.CreateContentHeader(
                "模块管理", "管理程序集引用"));

            var template = EUUIEditorWindowHelper.LoadUXMLTemplate("ModulesTab.uxml");
            if (template == null) return;

            var tab                  = template.Instantiate();
            var btnDefaultConfig   = tab.Q<Button>("btn-generate-default-config");
            var btnRefresh         = tab.Q<Button>("btn-refresh-assemblies");
            var btnRegenerateAll   = tab.Q<Button>("btn-regenerate-all");
            var btnDeleteAllWithSO = tab.Q<Button>("btn-delete-all-with-so");

            if (btnDefaultConfig != null)
                btnDefaultConfig.clicked += GenerateDefaultSOConfigs;

            if (btnRefresh != null)
                btnRefresh.clicked += () =>
                {
                    EUUIAsmdefHelper.RecalculateFromGeneratedFiles();
                    EditorUtility.DisplayDialog("完成", "程序集引用已根据已生成文件重算完毕。", "确定");
                };

            if (btnRegenerateAll != null)
                btnRegenerateAll.clicked += RegenerateAllGeneratedFiles;

            if (btnDeleteAllWithSO != null)
                btnDeleteAllWithSO.clicked += DeleteAllWithSO;

            contentArea.Add(tab);
        }

        // ── 一键生成默认配置 ──────────────────────────────────────────────────────

        private static void GenerateDefaultSOConfigs()
        {
            try
            {
                int created = 0;
                string editorSoDir = GetEditorSODirectory();
                EnsureDirectoryExists(editorSoDir);

                // EUUIEditorConfig
                string editorConfigPath = $"{editorSoDir}/EUUIEditorConfig.asset";
                var editorConfig = AssetDatabase.LoadAssetAtPath<EUUIEditorConfig>(editorConfigPath);
                if (editorConfig == null)
                {
                    editorConfig = ScriptableObject.CreateInstance<EUUIEditorConfig>();
                    AssetDatabase.CreateAsset(editorConfig, editorConfigPath);
                    created++;
                    Debug.Log($"[EUUI] 已创建 EUUIEditorConfig: {editorConfigPath}");
                }

                // EUUITemplateConfig
                string templateConfigPath = $"{editorSoDir}/EUUITemplateConfig.asset";
                if (AssetDatabase.LoadAssetAtPath<EUUITemplateConfig>(templateConfigPath) == null)
                {
                    AssetDatabase.CreateAsset(
                        ScriptableObject.CreateInstance<EUUITemplateConfig>(), templateConfigPath);
                    created++;
                    Debug.Log($"[EUUI] 已创建 EUUITemplateConfig: {templateConfigPath}");
                }

                // EUUIKitConfig（从 EditorConfig 同步）
                EUUIEditorConfigEditorSync.SyncEditorConfigToKitConfig(editorConfig);

                // 模板注册表
                EUUITemplateRegistryGenerator.RefreshRegistry();

                AssetDatabase.SaveAssets();

                string msg = created > 0
                    ? $"已创建 {created} 个 SO 配置，并同步运行时配置、刷新模板注册表。"
                    : "所有 SO 配置已存在，已同步运行时配置并刷新模板注册表。";
                EditorUtility.DisplayDialog("完成", msg, "确定");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUUI] 生成默认配置失败: {e.Message}");
                EditorUtility.DisplayDialog("失败", e.Message, "确定");
            }
        }

        // ── 重新生成全部 ──────────────────────────────────────────────────────────

        private static void RegenerateAllGeneratedFiles()
        {
            var sbnPaths = EUUIAsmdefHelper.CollectActiveSbnPaths();
            if (sbnPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "当前没有已生成的扩展文件。", "确定");
                return;
            }

            try
            {
                int count = 0;
                foreach (var sbnPath in sbnPaths)
                {
                    ExportTemplate(sbnPath);
                    count++;
                }
                EUUIAsmdefHelper.RecalculateFromGeneratedFiles();
                Debug.Log($"[EUUI] 重新生成完成，共 {count} 个文件");
                EditorUtility.DisplayDialog("完成", $"已重新生成 {count} 个扩展文件，程序集引用已同步。", "确定");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUUI] 重新生成失败: {e.Message}");
                EditorUtility.DisplayDialog("失败", e.Message, "确定");
            }
        }

        // ── 删除生成代码和SO配置 ──────────────────────────────────────────────────

        private static void DeleteAllWithSO()
        {
            if (!EditorUtility.DisplayDialog(
                "确认删除",
                "将删除：\n• 所有 .Generated.cs 扩展文件\n• EUUIEditorConfig\n• EUUITemplateConfig\n• EUUITemplateRegistry\n• EUUIKitConfig\n\n单独删除生成代码请使用「拓展」页面。\n此操作不可撤销。",
                "删除", "取消")) return;

            try
            {
                int count = 0;
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var dir in new[] {
                        EUUIAsmdefHelper.GetPanelBaseOutputDirectory(),
                        EUUIAsmdefHelper.GetUIKitOutputDirectory() })
                    {
                        if (string.IsNullOrEmpty(dir)) continue;
                        string full = Path.GetFullPath(
                            Path.Combine(Path.GetDirectoryName(Application.dataPath), dir));
                        if (!Directory.Exists(full)) continue;

                        foreach (var f in Directory.GetFiles(full, "*.Generated.cs", SearchOption.TopDirectoryOnly))
                        {
                            AssetDatabase.DeleteAsset(EUUIAsmdefHelper.ToAssetPath(f));
                            count++;
                        }
                    }

                    foreach (var soPath in GetAllSOAssetPaths())
                    {
                        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(soPath) != null)
                        {
                            AssetDatabase.DeleteAsset(soPath);
                            Debug.Log($"[EUUI] 已删除 SO: {soPath}");
                        }
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }

                EUUIAsmdefHelper.RecalculateFromGeneratedFiles();
                EUUIAsmdefHelper.SetExtensionsGeneratedDefine(false);

                EditorUtility.DisplayDialog("完成",
                    $"已删除 {count} 个生成文件及所有 EUUI SO 配置，程序集引用已重置。", "确定");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUUI] 删除操作失败: {e.Message}");
                EditorUtility.DisplayDialog("失败", e.Message, "确定");
            }
        }

        // ── 模板导出 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 重新渲染指定 .sbn 并写出 .Generated.cs。
        /// 通过 sidecar JSON 的 namespaceVariables 动态解析各程序集的 rootNamespace。
        /// </summary>
        private static void ExportTemplate(string sbnAssetPath)
        {
            string templateId = Path.GetFileNameWithoutExtension(Path.GetFileName(sbnAssetPath));

            string outDir = templateId.StartsWith("EUUIKit", StringComparison.OrdinalIgnoreCase)
                ? EUUIAsmdefHelper.GetUIKitOutputDirectory()
                : EUUIAsmdefHelper.GetPanelBaseOutputDirectory();

            if (string.IsNullOrEmpty(outDir))
                throw new InvalidOperationException($"无法确定模板 [{templateId}] 的输出目录");

            string sbnFull    = Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(Application.dataPath), sbnAssetPath));
            string sbnContent = File.ReadAllText(sbnFull, System.Text.Encoding.UTF8);

            var nsVarMap = EUUIAsmdefHelper.ReadSidecarNamespaceVariables(sbnAssetPath);
            var model    = new ScriptObject();
            foreach (var kv in nsVarMap)
            {
                string ns = EUUIAsmdefHelper.GetAssemblyRootNamespace(kv.Value);
                if (!string.IsNullOrEmpty(ns))
                    model[kv.Key] = ns;
                else
                    Debug.LogWarning(
                        $"[EUUI] 未能解析程序集 [{kv.Value}] 的 rootNamespace，变量 [{kv.Key}] 将使用空值");
            }

            var scribanTemplate = Scriban.Template.Parse(sbnContent);
            var templateContext = new Scriban.TemplateContext();
            templateContext.PushGlobal(model);
            string rendered = scribanTemplate.Render(templateContext);

            string outPath = $"{outDir}/{templateId}.Generated.cs";
            EUUIBaseExporter.EnsureOutputDirectory(outPath);
            string outFull = EUUIBaseExporter.NormalizeOutputPath(outPath);
            File.WriteAllText(outFull, rendered, System.Text.Encoding.UTF8);
            Debug.Log($"[EUUI] 模板已重新生成: {outPath}");
        }

        // ── 路径辅助 ─────────────────────────────────────────────────────────────

        private static string GetEditorSODirectory()
        {
            string editorDir = EUUITemplateLocator.GetEditorDirectory();
            return string.IsNullOrEmpty(editorDir)
                ? "Assets/EUFramework/Extension/EUUI/Editor/EditorSO"
                : $"{editorDir}/EditorSO";
        }

        private static string[] GetAllSOAssetPaths()
        {
            string editorSoDir    = GetEditorSODirectory();
            string resourcesPath  = EUUIEditorConfigEditor.GetResourcesPath();
            return new[]
            {
                $"{editorSoDir}/EUUIEditorConfig.asset",
                $"{editorSoDir}/EUUITemplateConfig.asset",
                $"{editorSoDir}/EUUITemplateRegistry.asset",
                $"{resourcesPath}/EUUIKitConfig.asset",
            };
        }

        private static void EnsureDirectoryExists(string assetRelativePath)
        {
            string full = Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(Application.dataPath), assetRelativePath));
            if (!Directory.Exists(full))
                Directory.CreateDirectory(full);
        }
    }
}
#endif
