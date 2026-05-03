#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EUFramework.Extension.EUUI.Editor
{
    internal class EUUISOConfigPanel : IEUUIEditorPanel
    {
        public void Build(VisualElement contentArea)
        {
            contentArea.Clear();
            contentArea.style.alignItems = Align.Stretch;
            contentArea.style.justifyContent = Justify.FlexStart;

            contentArea.Add(EUUIEditorWindowHelper.CreateContentHeader(
                "SO 配置管理", "管理 EUUI 所有 ScriptableObject 配置文件"));

            var tabBar = EUUIEditorWindowHelper.CreateTabBar();
            var tabEditorConfig      = EUUIEditorWindowHelper.CreateTabButton("EUUIEditorConfig", true);
            var tabTemplateConfig    = EUUIEditorWindowHelper.CreateTabButton("EUUITemplateConfig", false);
            var tabTemplateRegistry  = EUUIEditorWindowHelper.CreateTabButton("EUUITemplateRegistry", false);
            var tabKitConfig         = EUUIEditorWindowHelper.CreateTabButton("EUUIKitConfig", false);
            tabBar.Add(tabEditorConfig);
            tabBar.Add(tabTemplateConfig);
            tabBar.Add(tabTemplateRegistry);
            tabBar.Add(tabKitConfig);
            contentArea.Add(tabBar);

            var tabContent = EUUIEditorWindowHelper.CreateTabContentContainer();
            contentArea.Add(tabContent);

            ShowEditorConfigTab(tabContent);

            tabEditorConfig.clicked += () =>
            {
                EUUIEditorWindowHelper.SetActiveTab(tabEditorConfig, tabTemplateConfig, tabTemplateRegistry, tabKitConfig);
                ShowEditorConfigTab(tabContent);
            };
            tabTemplateConfig.clicked += () =>
            {
                EUUIEditorWindowHelper.SetActiveTab(tabTemplateConfig, tabEditorConfig, tabTemplateRegistry, tabKitConfig);
                ShowTemplateConfigTab(tabContent);
            };
            tabTemplateRegistry.clicked += () =>
            {
                EUUIEditorWindowHelper.SetActiveTab(tabTemplateRegistry, tabEditorConfig, tabTemplateConfig, tabKitConfig);
                ShowTemplateRegistryTab(tabContent);
            };
            tabKitConfig.clicked += () =>
            {
                EUUIEditorWindowHelper.SetActiveTab(tabKitConfig, tabEditorConfig, tabTemplateConfig, tabTemplateRegistry);
                ShowKitConfigTab(tabContent);
            };
        }

        // ── 路径查询 ────────────────────────────────────────────────────────────

        private static string GetConfigAssetPath()
        {
            string[] guids = AssetDatabase.FindAssets("EUUIEditorConfig t:EUUIEditorConfig");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path != null && path.EndsWith("EUUIEditorConfig.asset", StringComparison.OrdinalIgnoreCase))
                    return path;
            }
            return EUUIEditorSOPaths.EditorConfigAssetPath;
        }

        private static string GetTemplateConfigAssetPath()
        {
            string[] guids = AssetDatabase.FindAssets("EUUITemplateConfig t:EUUITemplateConfig");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path != null && path.EndsWith("EUUITemplateConfig.asset", StringComparison.OrdinalIgnoreCase))
                    return path;
            }
            return EUUIEditorSOPaths.TemplateConfigAssetPath;
        }

        private static string GetTemplateRegistryAssetPath()
        {
            return EUUIEditorSOPaths.TemplateRegistryAssetPath;
        }

        private static string GetKitConfigAssetPath()
        {
            string resourcesPath = EUUIEditorConfigEditor.GetResourcesPath();
            return Path.Combine(resourcesPath, "EUUIKitConfig.asset").Replace("\\", "/");
        }

        // ── Tab 内容 ─────────────────────────────────────────────────────────────

        private void ShowEditorConfigTab(VisualElement container)
        {
            container.Clear();
            var path   = GetConfigAssetPath();
            var config = AssetDatabase.LoadAssetAtPath<EUUIEditorConfig>(path);
            ShowInlineSOEditor(
                container,
                label:       "EUUIEditorConfig",
                assetPath:   path,
                target:      config,
                onCreate:    () => { EUUIEditorConfigEditor.CreateConfig(); ShowEditorConfigTab(container); });
        }

        private void ShowTemplateConfigTab(VisualElement container)
        {
            container.Clear();
            var path   = GetTemplateConfigAssetPath();
            var config = AssetDatabase.LoadAssetAtPath<EUUITemplateConfig>(path);
            ShowInlineSOEditor(
                container,
                label:       "EUUITemplateConfig",
                assetPath:   path,
                target:      config,
                onCreate:    () => { EUUIEditorConfigEditor.CreateTemplateConfig(); ShowTemplateConfigTab(container); });
        }

        /// <summary>
        /// 通用内嵌 SO 编辑器：SO 存在时显示可编辑的 InspectorElement，不存在时显示创建入口。
        /// <para><paramref name="extraContent"/> 可选：在 InspectorElement 下方追加额外控件（仅 SO 存在时调用）。</para>
        /// </summary>
        private static void ShowInlineSOEditor(
            VisualElement container,
            string label,
            string assetPath,
            UnityEngine.Object target,
            Action onCreate,
            Action<ScrollView> extraContent = null)
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow  = 1;
            scroll.style.alignSelf = Align.Stretch;

            var statusBox = new HelpBox();

            if (target != null)
            {
                statusBox.text        = assetPath;
                statusBox.messageType = HelpBoxMessageType.Info;
                scroll.Add(statusBox);

                var pingBtn = new Button(() => { Selection.activeObject = target; EditorGUIUtility.PingObject(target); })
                {
                    text = $"在 Project 中定位 {label}"
                };
                pingBtn.style.marginBottom = 6;
                scroll.Add(pingBtn);

                var inspector = new InspectorElement(target);
                scroll.Add(inspector);

                extraContent?.Invoke(scroll);
            }
            else
            {
                statusBox.text        = $"未找到 {label}，点击下方按钮创建";
                statusBox.messageType = HelpBoxMessageType.Warning;
                scroll.Add(statusBox);

                var createBtn = new Button(onCreate) { text = $"创建 {label}" };
                scroll.Add(createBtn);
            }

            container.Add(scroll);
        }

        private void ShowTemplateRegistryTab(VisualElement container)
        {
            container.Clear();
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow  = 1;
            scroll.style.alignSelf = Align.Stretch;

            var box  = new HelpBox { messageType = HelpBoxMessageType.Info };
            var path = GetTemplateRegistryAssetPath();
            var registry = path != null ? AssetDatabase.LoadAssetAtPath<EUUITemplateRegistryAsset>(path) : null;

            if (registry != null)
            {
                box.text = $"当前注册表：{path}";
                var openBtn    = new Button(() => { Selection.activeObject = registry; EditorGUIUtility.PingObject(registry); }) { text = "打开注册表" };
                var refreshBtn = new Button(() => { EUUITemplateRegistryGenerator.RefreshRegistry(); box.text = $"已刷新。当前注册表：{path}"; }) { text = "刷新注册表" };
                scroll.Add(box);
                scroll.Add(openBtn);
                scroll.Add(refreshBtn);
            }
            else
            {
                box.text          = "未找到模板注册表，点击下方按钮生成。";
                box.messageType   = HelpBoxMessageType.Warning;
                var refreshBtn    = new Button(() => { EUUITemplateRegistryGenerator.RefreshRegistry(); ShowTemplateRegistryTab(container); }) { text = "生成注册表" };
                scroll.Add(box);
                scroll.Add(refreshBtn);
            }

            container.Add(scroll);
        }

        // 仅在面板中显示的字段（分辨率 / 路径已由 EditorConfig 同步，不在此处展示）
        private static readonly string[] _kitConfigEditableFields =
        {
            "uiCameraDepth", "uiCullingMask", "uiCameraClearFlags",
            "panelCacheCapacity", "baseSortingOrder", "multiplayerInputMode"
        };

        private void ShowKitConfigTab(VisualElement container)
        {
            container.Clear();

            var path      = GetKitConfigAssetPath();
            var kitConfig = AssetDatabase.LoadAssetAtPath<EUUIKitConfig>(path);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow  = 1;
            scroll.style.alignSelf = Align.Stretch;

            if (kitConfig == null)
            {
                var warnBox = new HelpBox($"未找到 EUUIKitConfig，点击下方按钮创建", HelpBoxMessageType.Warning);
                scroll.Add(warnBox);

                var createBtn = new Button(() =>
                {
                    var editorConfig = AssetDatabase.LoadAssetAtPath<EUUIEditorConfig>(GetConfigAssetPath());
                    if (editorConfig == null) { EditorUtility.DisplayDialog("提示", "请先创建 EUUIEditorConfig。", "确定"); return; }
                    EUUIEditorConfigEditorSync.SyncEditorConfigToKitConfig(editorConfig);
                    EditorUtility.DisplayDialog("完成", $"已创建并同步：{path}", "确定");
                    ShowKitConfigTab(container);
                }) { text = "创建 EUUIKitConfig" };
                scroll.Add(createBtn);
            }
            else
            {
                var infoBox = new HelpBox(path, HelpBoxMessageType.Info);
                scroll.Add(infoBox);

                var pingBtn = new Button(() => { Selection.activeObject = kitConfig; EditorGUIUtility.PingObject(kitConfig); })
                    { text = "在 Project 中定位 EUUIKitConfig" };
                pingBtn.style.marginBottom = 6;
                scroll.Add(pingBtn);

                var syncHint = new HelpBox(
                    "分辨率、UI Prefab 路径等字段由 EUUIEditorConfig 统一管理，请在 EUUIEditorConfig 中修改后点击同步。",
                    HelpBoxMessageType.None);
                syncHint.style.marginBottom = 4;
                scroll.Add(syncHint);

                // 只显示不由 EditorConfig 同步的独有字段
                var so = new SerializedObject(kitConfig);
                foreach (var fieldName in _kitConfigEditableFields)
                {
                    var prop = so.FindProperty(fieldName);
                    if (prop == null) continue;
                    var propField = new PropertyField(prop);
                    propField.Bind(so);
                    scroll.Add(propField);
                }

                var separator = new VisualElement();
                separator.style.height          = 1;
                separator.style.marginTop        = 8;
                separator.style.marginBottom     = 4;
                separator.style.backgroundColor  = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 0.5f));
                scroll.Add(separator);

                var syncBtn = new Button(() =>
                {
                    var editorConfig = AssetDatabase.LoadAssetAtPath<EUUIEditorConfig>(GetConfigAssetPath());
                    if (editorConfig == null) { EditorUtility.DisplayDialog("提示", "请先创建或打开 EUUIEditorConfig。", "确定"); return; }
                    EUUIEditorConfigEditorSync.SyncEditorConfigToKitConfig(editorConfig);
                    EditorUtility.DisplayDialog("完成", $"已同步到：{path}", "确定");
                    ShowKitConfigTab(container);
                }) { text = "从 EditorConfig 同步（覆盖分辨率 / UI Prefab 路径等字段）" };
                syncBtn.style.marginTop = 4;
                scroll.Add(syncBtn);
            }

            container.Add(scroll);
        }
    }
}
#endif
