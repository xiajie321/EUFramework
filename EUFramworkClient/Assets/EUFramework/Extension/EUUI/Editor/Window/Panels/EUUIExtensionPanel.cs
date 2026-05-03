#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Scriban.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using EUFramework.Extension.EUUI.Editor.Templates;

namespace EUFramework.Extension.EUUI.Editor
{
    internal class EUUIExtensionPanel : IEUUIEditorPanel
    {
        // 模板管理 Tab 的滚动位置
        private Vector2 _scrollPos;

        // ── 行数据（内联替代原 EUUIStaticExporter.ManageableRow）──────────────
        private struct ExtRow
        {
            public string                  TemplateId;
            public string                  DisplayName;
            public string                  OutputAssetPath;
            public string                  ExtensionName;
            public bool                    IsCore;
            public bool                    Enabled;
            public EUUIAdditionalExtension ManualExt;
        }

        public void Build(VisualElement contentArea)
        {
            contentArea.Clear();
            contentArea.style.alignItems     = Align.Stretch;
            contentArea.style.justifyContent = Justify.FlexStart;

            contentArea.Add(EUUIEditorWindowHelper.CreateContentHeader(
                "拓展管理", "管理 .sbn 模板文件和 ExportsCS 导出器"));

            var tabBar       = EUUIEditorWindowHelper.CreateTabBar();
            var tabTemplates = EUUIEditorWindowHelper.CreateTabButton("模板管理", true);
            var tabGenerate  = EUUIEditorWindowHelper.CreateTabButton("生成绑定模板", false);
            tabBar.Add(tabTemplates);
            tabBar.Add(tabGenerate);
            contentArea.Add(tabBar);

            var tabContent = EUUIEditorWindowHelper.CreateTabContentContainer();
            contentArea.Add(tabContent);

            ShowTemplatesManagementTab(tabContent);

            tabTemplates.clicked += () =>
            {
                EUUIEditorWindowHelper.SetActiveTab(tabTemplates, tabGenerate);
                ShowTemplatesManagementTab(tabContent);
            };
            tabGenerate.clicked += () =>
            {
                EUUIEditorWindowHelper.SetActiveTab(tabGenerate, tabTemplates);
                ShowExtensionsTab(tabContent);
            };
        }

        // ── Tab：模板管理 ─────────────────────────────────────────────────────────

        private void ShowTemplatesManagementTab(VisualElement container)
        {
            container.Clear();
            container.style.paddingLeft  = 20;
            container.style.paddingRight = 20;
            container.style.paddingTop   = 10;
            container.style.alignSelf    = Align.Stretch;

            var imgui = new IMGUIContainer(() =>
            {
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                GUILayout.Space(10);
                GUILayout.Label("模板管理", EditorStyles.boldLabel);
                GUILayout.Space(10);

                var config = EUUIPanelExporter.GetConfig();
                if (config == null)
                {
                    EditorGUILayout.HelpBox("未找到模板配置文件！可在下方点击按钮创建 EUUITemplateConfig。", MessageType.Error);
                    GUILayout.Space(8);
                    if (GUILayout.Button("创建模板配置文件", GUILayout.Height(28)))
                    {
                        EUUIEditorConfigEditor.CreateTemplateConfig();
                        GUIUtility.ExitGUI();
                    }
                    GUILayout.EndScrollView();
                    GUILayout.EndVertical();
                    return;
                }

                string templatesDir = EUUITemplateLocator.GetTemplatesDirectory();
                if (string.IsNullOrEmpty(templatesDir))
                {
                    EditorGUILayout.HelpBox("无法找到模板目录！", MessageType.Error);
                    GUILayout.EndScrollView();
                    GUILayout.EndVertical();
                    return;
                }

                if (config.manualExtensions == null)
                    config.manualExtensions = new List<EUUIAdditionalExtension>();

                // 清理 manualExtensions 中模板文件已被删除的失效条目
                int removedCount = config.manualExtensions.RemoveAll(e =>
                {
                    if (string.IsNullOrEmpty(e.templatePath)) return true;
                    string fp = Path.GetFullPath(
                        Path.Combine(Path.GetDirectoryName(Application.dataPath), e.templatePath));
                    return !File.Exists(fp);
                });
                if (removedCount > 0)
                    EditorUtility.SetDirty(config);

                // 扫描 .sbn 文件：WithData 由资源制作面板管理；Static/ 全部可在「生成绑定模板」Tab 管理
                var coreFiles   = new List<string>(); // WithData/ 模板
                var customFiles = new List<string>(); // Static/ 模板
                if (Directory.Exists(templatesDir))
                {
                    foreach (var file in Directory.GetFiles(templatesDir, "*.sbn", SearchOption.AllDirectories))
                    {
                        string rel = file.Replace("\\", "/");
                        if (rel.StartsWith(Application.dataPath))
                            rel = "Assets" + rel.Substring(Application.dataPath.Length);
                        if (IsManagedByFramework(rel))
                            coreFiles.Add(rel);
                        else
                            customFiles.Add(rel);
                    }
                }

                // ── WithData 模板（只读，由资源制作面板导出）─────────────────
                GUILayout.Space(5);
                EditorGUILayout.LabelField("WithData 模板（资源制作面板管理）", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "WithData 模板（PanelGenerated / MVCArchitecture）由「资源制作」面板在绑定时自动导出，无需在此管理。",
                    MessageType.Info);
                GUILayout.Space(5);

                if (coreFiles.Count == 0)
                {
                    EditorGUILayout.HelpBox("未找到核心模板文件。", MessageType.Warning);
                }
                else
                {
                    var prevColor = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, 0.5f);
                    foreach (var sbnPath in coreFiles)
                    {
                        string fileName = Path.GetFileName(sbnPath);
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("🔒", GUILayout.Width(18));
                        EditorGUILayout.LabelField("框架管理", GUILayout.Width(72));
                        EditorGUILayout.LabelField(fileName, EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("定位", GUILayout.Width(46)))
                            PingAsset(sbnPath);
                        if (GUILayout.Button("打开", GUILayout.Width(46)))
                        {
                            if (File.Exists(sbnPath))
                                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(sbnPath, 1);
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.LabelField(sbnPath, EditorStyles.miniLabel);
                        EditorGUILayout.EndVertical();
                        GUILayout.Space(3);
                    }
                    GUI.color = prevColor;
                }

                // ── Static/ 扩展模板（在「生成绑定模板」Tab 统一管理）──────────
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Static 扩展模板（生成绑定模板 Tab 管理）", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Static/ 下的所有 .sbn 均可在「生成绑定模板」面板中启用/禁用和导出。\n" +
                    "勾选状态在此处预览；实际启用操作请前往「生成绑定模板」Tab。",
                    MessageType.Info);
                GUILayout.Space(5);

                if (customFiles.Count == 0)
                {
                    EditorGUILayout.HelpBox("暂无 Static 扩展模板，可直接在 Templates/Sbn/Static 下新增 .sbn。", MessageType.Warning);
                }
                else
                {
                    foreach (var sbnPath in customFiles)
                    {
                        var ext = config.manualExtensions.Find(e => e.templatePath == sbnPath);
                        if (ext == null)
                        {
                            ext = new EUUIAdditionalExtension { templatePath = sbnPath, enabled = true };
                            config.manualExtensions.Add(ext);
                            EditorUtility.SetDirty(config);
                        }

                        string fileName = Path.GetFileName(sbnPath);
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        EditorGUILayout.BeginHorizontal();
                        bool newEnabled = EditorGUILayout.Toggle(ext.enabled, GUILayout.Width(18));
                        if (newEnabled != ext.enabled) { ext.enabled = newEnabled; EditorUtility.SetDirty(config); }
                        EditorGUILayout.LabelField("生成面板管理", GUILayout.Width(72));
                        EditorGUILayout.LabelField(fileName, EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("定位", GUILayout.Width(46)))
                            PingAsset(sbnPath);
                        if (GUILayout.Button("打开", GUILayout.Width(46)))
                        {
                            if (File.Exists(sbnPath))
                                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(sbnPath, 1);
                            else
                                EditorUtility.DisplayDialog("文件不存在", $"模板文件不存在：\n{sbnPath}", "确定");
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.LabelField(sbnPath, EditorStyles.miniLabel);

                        EditorGUILayout.EndVertical();
                        GUILayout.Space(5);
                    }
                }

                GUILayout.Space(15);
                EditorGUILayout.LabelField("快捷操作", EditorStyles.boldLabel);
                GUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("打开配置文件"))  { Selection.activeObject = config; EditorGUIUtility.PingObject(config); }
                if (GUILayout.Button("刷新模板列表"))  { AssetDatabase.Refresh(); }
                if (GUILayout.Button("打开模板目录"))
                {
                    if (Directory.Exists(templatesDir))
                        EditorUtility.RevealInFinder(templatesDir);
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            });

            imgui.style.flexGrow  = 1;
            imgui.style.alignSelf = Align.Stretch;
            container.Add(imgui);
        }

        // ── Tab：生成绑定模板 ──────────────────────────────────────────────────────

        private void ShowExtensionsTab(VisualElement container)
        {
            container.Clear();
            var template = EUUIEditorWindowHelper.LoadUXMLTemplate("ExtensionsTab.uxml");
            if (template == null) return;

            var tab         = template.Instantiate();
            var itemsList   = tab.Q<ScrollView>("generatable-items-list");
            var statusArea  = tab.Q<VisualElement>("extensions-status");
            var generateBtn = tab.Q<Button>("btn-generate");
            var deleteBtn   = tab.Q<Button>("btn-delete");

            var config = EUUIPanelExporter.GetConfig();
            if (config == null)
            {
                statusArea?.Add(new Label("未找到配置文件！") { style = { color = new Color(1f, 0.5f, 0.5f) } });
                generateBtn.SetEnabled(false);
                container.Add(tab);
                return;
            }

            // ── 构建行数据 ─────────────────────────────────────────────────────
            var rows    = BuildExtRows(config);
            var content = itemsList?.contentContainer;
            content?.Clear();

            foreach (var row in rows)
            {
                var r     = row;
                var rowEl = new VisualElement();
                rowEl.style.flexDirection   = FlexDirection.Row;
                rowEl.style.alignItems      = Align.Center;
                rowEl.style.marginBottom    = 4;
                rowEl.style.paddingLeft     = 4;
                rowEl.style.paddingRight    = 4;
                rowEl.style.paddingTop      = 2;
                rowEl.style.paddingBottom   = 2;
                rowEl.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);

                var toggle = new Toggle { value = r.Enabled };
                toggle.style.width = 18;
                toggle.RegisterValueChangedCallback(evt =>
                {
                    r.ManualExt.enabled = evt.newValue;
                    EditorUtility.SetDirty(config);
                    AssetDatabase.SaveAssets();
                    ShowExtensionsTab(container);
                });
                rowEl.Add(toggle);
                rowEl.Add(new Label("管理") { style = { minWidth = 28, fontSize = 11 } });

                var nameLabel = new Label(r.DisplayName)
                    { style = { minWidth = 120, unityFontStyleAndWeight = FontStyle.Bold } };
                rowEl.Add(nameLabel);

                string statusText  = r.Enabled ? (OutputFileExists(r.OutputAssetPath) ? "已生成" : "未生成") : "未加入管理";
                var    statusLabel = new Label(statusText);
                statusLabel.style.minWidth = 56;
                statusLabel.style.color    = r.Enabled
                    ? (OutputFileExists(r.OutputAssetPath) ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.9f, 0.7f, 0.3f))
                    : new Color(0.6f, 0.6f, 0.6f);
                rowEl.Add(statusLabel);
                rowEl.Add(new VisualElement { style = { flexGrow = 1 } });

                // 定位按钮（始终显示，定位 .sbn 模板文件）
                if (!string.IsNullOrEmpty(r.ManualExt?.templatePath))
                {
                    string sbnPath = r.ManualExt.templatePath;
                    var pingBtn = new Button(() =>
                    {
                        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(sbnPath);
                        if (obj != null) { EditorGUIUtility.PingObject(obj); Selection.activeObject = obj; }
                    }) { text = "定位" };
                    pingBtn.style.minWidth = 40;
                    rowEl.Add(pingBtn);
                }

                if (r.Enabled && !string.IsNullOrEmpty(r.OutputAssetPath))
                {
                    bool exists = OutputFileExists(r.OutputAssetPath);
                    var  btn    = new Button();
                    if (exists)
                    {
                        btn.text     = "删除";
                        btn.clicked += () =>
                        {
                            AssetDatabase.DeleteAsset(r.OutputAssetPath);
                            AssetDatabase.Refresh();
                            EUUIAsmdefHelper.RecalculateFromGeneratedFiles();
                            ShowExtensionsTab(container);
                        };
                    }
                    else
                    {
                        btn.text     = "创建";
                        btn.clicked += () =>
                        {
                            try
                            {
                                ExportRow(r);
                                AssetDatabase.Refresh();
                                ShowExtensionsTab(container);
                            }
                            catch (Exception ex) { EditorUtility.DisplayDialog("生成失败", ex.Message, "确定"); }
                        };
                    }
                    btn.style.minWidth = 46;
                    rowEl.Add(btn);
                }

                content?.Add(rowEl);
            }

            // ── 状态统计 ───────────────────────────────────────────────────────
            if (statusArea != null)
            {
                int total   = rows.Count;
                int enabled = rows.FindAll(r => r.Enabled).Count;
                int done    = rows.FindAll(r => r.Enabled && OutputFileExists(r.OutputAssetPath)).Count;
                statusArea.Add(new Label($"共 {total} 个模板，{enabled} 个已启用，{done} 个已生成")
                    { style = { fontSize = 11, color = new Color(0.7f, 0.7f, 0.7f) } });

            }

            // ── 批量按钮 ───────────────────────────────────────────────────────
            generateBtn.clicked += () => { ExportAllEnabled(config, rows); ShowExtensionsTab(container); };
            if (deleteBtn != null)
                deleteBtn.clicked += () => { DeleteAllEnabled(rows); ShowExtensionsTab(container); };

            container.Add(tab);
        }

        // ── 行数据构建 ────────────────────────────────────────────────────────────

        private static List<ExtRow> BuildExtRows(EUUITemplateConfig config)
        {
            var rows        = new List<ExtRow>();
            string outputDir = GetStaticGeneratedOutputDirectory();

            if (config.manualExtensions == null)
                config.manualExtensions = new List<EUUIAdditionalExtension>();

            bool dirty     = false;
            var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // ── Step 1：扫描框架 Static/ 目录的所有 .sbn（含用户放进来的扩展）──────────
            string editorDir = EUUITemplateLocator.GetEditorDirectory();
            if (!string.IsNullOrEmpty(editorDir))
            {
                string staticDir = $"{editorDir}/Templates/Sbn/Static";
                if (Directory.Exists(staticDir))
                {
                    foreach (var file in Directory.GetFiles(staticDir, "*.sbn", SearchOption.AllDirectories))
                    {
                        string ap = ToAssetPath(file);

                        string fileName  = Path.GetFileNameWithoutExtension(ap);
                        string outPath   = string.IsNullOrEmpty(outputDir) ? "" : $"{outputDir}/{fileName}.Generated.cs";

                        var ext = config.manualExtensions.Find(e => e.templatePath == ap);
                        if (ext == null)
                        {
                            // 框架内置模板：首次出现默认启用
                            ext = new EUUIAdditionalExtension { templatePath = ap, enabled = true };
                            config.manualExtensions.Add(ext);
                            dirty = true;
                        }

                        addedPaths.Add(ap);
                        rows.Add(new ExtRow
                        {
                            TemplateId      = GetTemplateIdFromPath(ap),
                            DisplayName     = fileName,
                            OutputAssetPath = outPath,
                            ExtensionName   = ExtractExtensionName(fileName),
                            IsCore          = false,
                            Enabled         = ext.enabled,
                            ManualExt       = ext
                        });
                    }
                }
            }

            // ── Step 2：manualExtensions 中用户手动加入但不在 Static/ 目录的条目 ──────────────
            foreach (var ext in config.manualExtensions)
            {
                if (string.IsNullOrEmpty(ext.templatePath)) continue;
                if (addedPaths.Contains(ext.templatePath)) continue;
                if (IsManagedByFramework(ext.templatePath)) continue;

                string fp = Path.GetFullPath(
                    Path.Combine(Path.GetDirectoryName(Application.dataPath), ext.templatePath));
                if (!File.Exists(fp)) continue;

                addedPaths.Add(ext.templatePath);
                string fileName  = Path.GetFileNameWithoutExtension(ext.templatePath);
                string outPath   = string.IsNullOrEmpty(outputDir) ? "" : $"{outputDir}/{fileName}.Generated.cs";
                rows.Add(new ExtRow
                {
                    TemplateId      = GetTemplateIdFromPath(ext.templatePath),
                    DisplayName     = fileName,
                    OutputAssetPath = outPath,
                    ExtensionName   = ExtractExtensionName(fileName),
                    IsCore          = false,
                    Enabled         = ext.enabled,
                    ManualExt       = ext
                });
            }

            if (dirty) EditorUtility.SetDirty(config);

            return rows;
        }

        /// <summary>将系统绝对路径转为 Assets/ 相对路径（委托给 EUUIAsmdefHelper）</summary>
        private static string ToAssetPath(string fullPath) => EUUIAsmdefHelper.ToAssetPath(fullPath);

        /// <summary>从 .sbn 文件名中提取扩展名部分（去掉类前缀）</summary>
        private static string ExtractExtensionName(string filename)
        {
            if (filename.StartsWith("EUUIKit."))       return filename.Substring("EUUIKit.".Length);
            if (filename.StartsWith("EUUIPanelBase.")) return filename.Substring("EUUIPanelBase.".Length);
            return filename;
        }

        /// <summary>通过注册表或文件名获取模板 ID</summary>
        private static string GetTemplateIdFromPath(string assetPath)
        {
            var registry = EUUITemplateLocator.GetRegistryAsset();
            if (registry != null)
            {
                string id = registry.FindIdByPath(assetPath);
                if (!string.IsNullOrEmpty(id)) return id;
            }
            return Path.GetFileNameWithoutExtension(assetPath);
        }

        // ── 导出 / 删除 ───────────────────────────────────────────────────────────

        private static void ExportRow(ExtRow row)
        {
            if (string.IsNullOrEmpty(row.TemplateId) || string.IsNullOrEmpty(row.OutputAssetPath)) return;

            // 构建 Scriban 上下文：先注入 extension_name，再从伴生 JSON 读取 namespaceVariables 并解析 rootNamespace
            var scriptObject = new ScriptObject();
            if (!string.IsNullOrEmpty(row.ExtensionName))
                scriptObject["extension_name"] = row.ExtensionName;

            string sbnPath = row.ManualExt?.templatePath ?? "";
            var nsVarMap = EUUIAsmdefHelper.ReadSidecarNamespaceVariables(sbnPath);
            foreach (var kv in nsVarMap)
            {
                string ns = EUUIAsmdefHelper.GetAssemblyRootNamespace(kv.Value);
                scriptObject[kv.Key] = !string.IsNullOrEmpty(ns) ? ns : kv.Value;
            }

            EUUIBaseExporter.Export(row.TemplateId, row.OutputAssetPath, scriptObject, row.DisplayName);

            // 读取伴生 JSON，将运行时所需程序集加入 EUUI.asmdef，编辑器所需程序集加入 EUUI.Editor.asmdef
            var runtimeAsms = EUUIAsmdefHelper.ReadSidecarRuntimeAssemblies(sbnPath);
            foreach (var asm in runtimeAsms)
                EUUIAsmdefHelper.SetAssembly("EUUI.asmdef", asm, true);
            var editorAsms = EUUIAsmdefHelper.ReadSidecarEditorAssemblies(sbnPath);
            foreach (var asm in editorAsms)
                EUUIAsmdefHelper.SetAssembly("EUUI.Editor.asmdef", asm, true);
        }

        private static void ExportAllEnabled(EUUITemplateConfig config, List<ExtRow> rows)
        {
            try
            {
                foreach (var row in rows)
                    if (row.Enabled && !string.IsNullOrEmpty(row.OutputAssetPath))
                        ExportRow(row);

                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("完成", "所有扩展代码已生成", "确定");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUUI] 扩展代码生成失败: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("生成失败", e.Message, "确定");
            }
        }

        private static void DeleteAllEnabled(List<ExtRow> rows)
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "确认删除",
                "确定要删除所有已启用的扩展生成文件吗？\n此操作不可撤销。",
                "删除", "取消");
            if (!confirmed) return;

            try
            {
                int count = 0;
                foreach (var row in rows)
                {
                    if (!row.Enabled || string.IsNullOrEmpty(row.OutputAssetPath)) continue;
                    if (!OutputFileExists(row.OutputAssetPath)) continue;
                    AssetDatabase.DeleteAsset(row.OutputAssetPath);
                    count++;
                }
                AssetDatabase.Refresh();

                // 根据剩余生成文件重新计算两个 asmdef 所需的程序集引用
                EUUIAsmdefHelper.RecalculateFromGeneratedFiles();

                EditorUtility.DisplayDialog("完成", $"已删除 {count} 个生成文件", "确定");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUUI] 删除生成文件失败: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("删除失败", e.Message, "确定");
            }
        }

        // ── 私有辅助方法 ──────────────────────────────────────────────────────────

        private static string GetStaticGeneratedOutputDirectory() => EUUIAsmdefHelper.GetStaticGeneratedOutputDirectory();

        private static void EnsureDirectory(string assetRelDir) => EUUIAsmdefHelper.EnsureDirectory(assetRelDir);

        private static void PingAsset(string assetPath)
        {
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (obj != null)
            {
                EditorGUIUtility.PingObject(obj);
                Selection.activeObject = obj;
            }
            else
            {
                EditorUtility.DisplayDialog("文件不存在", $"无法定位文件：\n{assetPath}", "确定");
            }
        }

        private static bool OutputFileExists(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            string full = Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath));
            return File.Exists(full);
        }

        /// <summary>
        /// 判断该 .sbn 是否由框架其他机制管理，不应出现在本面板列表中。
        /// 目前仅 WithData/ 模板（由 EUUIPanelExporter / 资源制作面板负责）属于此类。
        /// Static/ 下所有 .sbn（框架内置 + 用户扩展）均在本面板统一管理。
        /// </summary>
        private static bool IsManagedByFramework(string sbnPath) =>
            sbnPath.Contains("/WithData/") || sbnPath.Contains("\\WithData\\");
    }
}
#endif
