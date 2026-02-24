#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Scriban.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using EUFramework.Extension.EUUI.Editor.Templates;

namespace EUFramework.Extension.EUUI.Editor
{
    internal class EUUIExtensionPanel : IEUUIPanel
    {
        // ── 扩展创建 Tab 的持久状态 ──────────────────────────────────────────────
        private EUUIExtensionTemplateCreator.ExtensionType  _extensionType        = EUUIExtensionTemplateCreator.ExtensionType.KitExtension;
        private EUUIExtensionTemplateCreator.TemplatePreset _templatePreset       = EUUIExtensionTemplateCreator.TemplatePreset.ResourceLoader;
        private string _extensionName        = "";
        private string _requiredAssemblies   = "";

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
            var tabExtension = EUUIEditorWindowHelper.CreateTabButton("模板拓展", false);
            tabBar.Add(tabTemplates);
            tabBar.Add(tabGenerate);
            tabBar.Add(tabExtension);
            contentArea.Add(tabBar);

            var tabContent = EUUIEditorWindowHelper.CreateTabContentContainer();
            contentArea.Add(tabContent);

            ShowTemplatesManagementTab(tabContent);

            tabTemplates.clicked += () =>
            {
                EUUIEditorWindowHelper.SetActiveTab(tabTemplates, tabGenerate, tabExtension);
                ShowTemplatesManagementTab(tabContent);
            };
            tabGenerate.clicked += () =>
            {
                EUUIEditorWindowHelper.SetActiveTab(tabGenerate, tabTemplates, tabExtension);
                ShowExtensionsTab(tabContent);
            };
            tabExtension.clicked += () =>
            {
                EUUIEditorWindowHelper.SetActiveTab(tabExtension, tabTemplates, tabGenerate);
                ShowCreateExtensionTab(tabContent);
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
                    "Static/PanelBase/ 和 Static/UIKit/ 下的所有 .sbn 均可在「生成绑定模板」面板中启用/禁用和导出。\n" +
                    "勾选状态在此处预览；实际启用操作请前往「生成绑定模板」Tab。",
                    MessageType.Info);
                GUILayout.Space(5);

                if (customFiles.Count == 0)
                {
                    EditorGUILayout.HelpBox("暂无 Static 扩展模板，可在「模板拓展」Tab 中创建。", MessageType.Warning);
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
                            if (!HasAnyUIKitGeneratedFile())
                                EUUIAsmdefHelper.SetExtensionsGeneratedDefine(false);
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
            string panelDir = GetPanelBaseOutputDirectory();
            string uikitDir = GetUIKitOutputDirectory();

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
                        if (!IsPanelBaseTemplate(ap) && !IsUIKitTemplate(ap)) continue;

                        string fileName  = Path.GetFileNameWithoutExtension(ap);
                        string outputDir = IsPanelBaseTemplate(ap) ? panelDir : uikitDir;
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
                string outputDir = IsPanelBaseTemplate(ext.templatePath) ? panelDir : uikitDir;
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

            // 只要有任何 UIKit 扩展生成，就设置项目宏
            if (IsUIKitTemplate(sbnPath))
                EUUIAsmdefHelper.SetExtensionsGeneratedDefine(true);
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
                EUUIAsmdefHelper.SetExtensionsGeneratedDefine(true);
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

                // 只有当 UIKit 生成目录下已无任何 .Generated.cs 时才移除宏
                if (!HasAnyUIKitGeneratedFile())
                    EUUIAsmdefHelper.SetExtensionsGeneratedDefine(false);

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

        /// <summary>
        /// 检查 UIKit 生成目录下是否还存在任何 .Generated.cs 文件
        /// </summary>
        private static bool HasAnyUIKitGeneratedFile()
        {
            string uikitDir = GetUIKitOutputDirectory();
            if (string.IsNullOrEmpty(uikitDir)) return false;
            string full = Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(Application.dataPath), uikitDir));
            if (!Directory.Exists(full)) return false;
            return Directory.GetFiles(full, "*.Generated.cs", SearchOption.TopDirectoryOnly).Length > 0;
        }

        // ── 私有辅助方法 ──────────────────────────────────────────────────────────

        private static string GetPanelBaseOutputDirectory() => EUUIAsmdefHelper.GetPanelBaseOutputDirectory();
        private static string GetUIKitOutputDirectory()     => EUUIAsmdefHelper.GetUIKitOutputDirectory();

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

        private static bool IsPanelBaseTemplate(string path) =>
            path.Contains("/PanelBase/") || path.Contains("\\PanelBase\\");

        private static bool IsUIKitTemplate(string path) =>
            path.Contains("/UIKit/") || path.Contains("\\UIKit\\");

        /// <summary>
        /// 判断该 .sbn 是否由框架其他机制管理，不应出现在本面板列表中。
        /// 目前仅 WithData/ 模板（由 EUUIPanelExporter / 资源制作面板负责）属于此类。
        /// Static/ 下所有 .sbn（框架内置 + 用户扩展）均在本面板统一管理。
        /// </summary>
        private static bool IsManagedByFramework(string sbnPath) =>
            sbnPath.Contains("/WithData/") || sbnPath.Contains("\\WithData\\");

        // ── Tab：模板拓展（创建扩展） ────────────────────────────────────────────

        private void ShowCreateExtensionTab(VisualElement container)
        {
            container.Clear();
            var template = EUUIEditorWindowHelper.LoadUXMLTemplate("CreateExtensionTab.uxml");
            if (template == null) return;

            var tab = template.Instantiate();

            var typeField          = tab.Q<EnumField>("extension-type");
            var nameField          = tab.Q<TextField>("extension-name");
            var presetField        = tab.Q<EnumField>("template-preset");
            var assembliesField    = tab.Q<TextField>("required-assemblies");
            var createBtn          = tab.Q<Button>("btn-create");

            var typeHint           = tab.Q<HelpBox>("type-hint");
            var nameValidation     = tab.Q<HelpBox>("name-validation");
            var presetHint         = tab.Q<HelpBox>("preset-hint");
            var previewLabel       = tab.Q<Label>("preview-label");
            var previewFilename    = tab.Q<TextField>("preview-filename");
            var existsHint         = tab.Q<HelpBox>("exists-hint");

            typeField.Init(_extensionType);
            presetField.Init(_templatePreset);
            nameField.value       = _extensionName;
            if (assembliesField != null)
                assembliesField.value = _requiredAssemblies;

            UpdateTypeHint(typeHint, _extensionType);
            UpdatePresetHint(presetHint, _templatePreset);
            UpdatePreview(previewLabel, previewFilename, existsHint, createBtn, _extensionName);

            typeField.RegisterValueChangedCallback(evt =>
            {
                _extensionType = (EUUIExtensionTemplateCreator.ExtensionType)evt.newValue;
                UpdateTypeHint(typeHint, _extensionType);
                UpdatePreview(previewLabel, previewFilename, existsHint, createBtn, nameField.value);
            });
            nameField.RegisterValueChangedCallback(evt =>
            {
                _extensionName = evt.newValue;
                ValidateExtensionName(nameValidation, evt.newValue);
                UpdatePreview(previewLabel, previewFilename, existsHint, createBtn, evt.newValue);
            });
            presetField.RegisterValueChangedCallback(evt =>
            {
                _templatePreset = (EUUIExtensionTemplateCreator.TemplatePreset)evt.newValue;
                UpdatePresetHint(presetHint, _templatePreset);
            });
            assembliesField?.RegisterValueChangedCallback(evt =>
            {
                _requiredAssemblies = evt.newValue;
            });

            createBtn.clicked += () => CreateExtensionTemplate(container);

            ValidateExtensionName(nameValidation, _extensionName);

            container.Add(tab);
        }

        // ── 扩展创建辅助方法 ──────────────────────────────────────────────────────

        private void UpdateTypeHint(HelpBox helpBox, EUUIExtensionTemplateCreator.ExtensionType type)
        {
            string targetDir = GetExtensionTargetDirectory(type);
            helpBox.text = type switch
            {
                EUUIExtensionTemplateCreator.ExtensionType.PanelExtension =>
                    $"为 EUUIPanelBase 添加静态扩展方法（如 OSA、DoTween）\n目标目录：{targetDir}",
                EUUIExtensionTemplateCreator.ExtensionType.KitExtension =>
                    $"为 EUUIKit 添加功能扩展（如资源加载、分析统计、日志）\n目标目录：{targetDir}\n" +
                    $"提示：选择 ResourceLoader 预设可快速生成含加载/释放框架的模板",
                _ => ""
            };
        }

        private void UpdatePresetHint(HelpBox helpBox, EUUIExtensionTemplateCreator.TemplatePreset preset)
        {
            helpBox.text = preset switch
            {
                EUUIExtensionTemplateCreator.TemplatePreset.Empty           => "仅包含基础结构和 TODO 注释",
                EUUIExtensionTemplateCreator.TemplatePreset.ResourceLoader  => "包含完整的资源加载/释放方法框架",
                EUUIExtensionTemplateCreator.TemplatePreset.StaticExtension => "包含静态扩展方法示例",
                _ => ""
            };
        }

        private void ValidateExtensionName(HelpBox helpBox, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                helpBox.text          = "请输入扩展名称（如：MyLoader、OSA、DoTween）";
                helpBox.messageType   = HelpBoxMessageType.Warning;
                helpBox.style.display = DisplayStyle.Flex;
            }
            else if (!IsValidExtensionName(name))
            {
                helpBox.text          = "名称只能包含字母、数字和下划线，且必须以字母开头";
                helpBox.messageType   = HelpBoxMessageType.Error;
                helpBox.style.display = DisplayStyle.Flex;
            }
            else
            {
                helpBox.style.display = DisplayStyle.None;
            }
        }

        private void UpdatePreview(Label label, TextField field, HelpBox existsHint, Button createBtn, string name)
        {
            bool validName = !string.IsNullOrEmpty(name) && IsValidExtensionName(name);
            if (!validName)
            {
                label.style.display      = DisplayStyle.None;
                field.style.display      = DisplayStyle.None;
                existsHint.style.display = DisplayStyle.None;
                createBtn.SetEnabled(false);
                return;
            }

            string targetDir = GetExtensionTargetDirectory(_extensionType);
            string assetPath = $"{targetDir}/{GetExtensionFileName()}";
            field.SetValueWithoutNotify(assetPath);
            label.style.display = DisplayStyle.Flex;
            field.style.display = DisplayStyle.Flex;

            string fullPath = Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath));
            bool fileExists = File.Exists(fullPath);

            if (fileExists)
            {
                existsHint.text          = "文件已存在，无需重复创建。可直接编辑已有模板。";
                existsHint.messageType   = HelpBoxMessageType.Warning;
                existsHint.style.display = DisplayStyle.Flex;
                createBtn.SetEnabled(false);
            }
            else
            {
                existsHint.style.display = DisplayStyle.None;
                createBtn.SetEnabled(true);
            }
        }

        /// <summary>根据扩展类型返回目标目录（Templates/Sbn/Static 子目录）</summary>
        private static string GetExtensionTargetDirectory(EUUIExtensionTemplateCreator.ExtensionType type)
        {
            string editorDir = EUUITemplateLocator.GetEditorDirectory();
            string sub = type == EUUIExtensionTemplateCreator.ExtensionType.PanelExtension
                ? "PanelBase"
                : "UIKit";
            return string.IsNullOrEmpty(editorDir)
                ? $"Assets/EUFramework/Extension/EUUI/Editor/Templates/Sbn/Static/{sub}"
                : $"{editorDir}/Templates/Sbn/Static/{sub}";
        }

        private bool IsValidExtensionName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            foreach (char c in name)
                if (!char.IsLetterOrDigit(c) && c != '_') return false;
            return char.IsLetter(name[0]);
        }

        private string GetExtensionFileName() =>
            _extensionType == EUUIExtensionTemplateCreator.ExtensionType.PanelExtension
                ? $"EUUIPanelBase.{_extensionName}.sbn"
                : $"EUUIKit.{_extensionName}.sbn";

        private bool CanCreateExtension() =>
            !string.IsNullOrEmpty(_extensionName) && IsValidExtensionName(_extensionName);

        /// <summary>
        /// 解析逗号分隔的程序集输入，返回去空白、去重后的数组
        /// </summary>
        private static string[] ParseAssembliesInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return System.Array.Empty<string>();
            return input
                .Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private void CreateExtensionTemplate(VisualElement container)
        {
            try
            {
                string targetDir = GetExtensionTargetDirectory(_extensionType);
                string fileName  = GetExtensionFileName();
                string assetPath = $"{targetDir}/{fileName}";
                string fullPath  = Path.GetFullPath(
                    Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath));

                if (File.Exists(fullPath))
                {
                    EditorUtility.DisplayDialog("文件已存在",
                        $"模板文件已存在：\n{assetPath}\n\n请直接编辑该文件。", "确定");
                    return;
                }

                string dirFull = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dirFull))
                    Directory.CreateDirectory(dirFull);

                string content = EUUIExtensionTemplateCreator.GenerateTemplateContent(
                    _extensionType, _templatePreset, _extensionName);

                File.WriteAllText(fullPath, content, System.Text.Encoding.UTF8);

                // 解析所需程序集，生成伴生 JSON
                string[] parsedAssemblies = ParseAssembliesInput(_requiredAssemblies);
                string sidecarNote = "";
                if (parsedAssemblies.Length > 0)
                {
                    string jsonContent = "{\n    \"requiredAssemblies\": ["
                        + string.Join(", ", parsedAssemblies.Select(a => $"\"{a}\""))
                        + "]\n}\n";
                    string jsonPath = Path.ChangeExtension(fullPath, ".json");
                    File.WriteAllText(jsonPath, jsonContent, System.Text.Encoding.UTF8);
                    sidecarNote = $"\n已生成伴生配置：{Path.GetFileName(jsonPath)}\n程序集：{string.Join(", ", parsedAssemblies)}";
                    Debug.Log($"[EUUI] 伴生配置已创建: {jsonPath}");
                }

                AssetDatabase.Refresh();

                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (asset != null)
                {
                    EditorGUIUtility.PingObject(asset);
                    Selection.activeObject = asset;
                }

                EditorUtility.DisplayDialog("创建成功",
                    $"扩展模板已创建：\n{assetPath}\n\n" +
                    "模板注册表将自动更新，请在模板中实现 TODO 标记的部分。"
                    + sidecarNote, "确定");

                Debug.Log($"[EUUI] 扩展模板已创建: {assetPath}");
                _extensionName      = "";
                _requiredAssemblies = "";
                ShowCreateExtensionTab(container);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("创建失败", $"创建扩展模板失败：\n{e.Message}", "确定");
                Debug.LogError($"[EUUI] 创建扩展模板失败: {e}");
            }
        }
    }
}
#endif
