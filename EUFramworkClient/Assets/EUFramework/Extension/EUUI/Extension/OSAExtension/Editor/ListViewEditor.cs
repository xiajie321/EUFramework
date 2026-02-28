using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Scriban;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Com.ForbiddenByte.OSA.Core;
using Com.ForbiddenByte.OSA.CustomParams;
using Com.ForbiddenByte.OSA.CustomAdapters.GridView;
using EUFramework.Extension.EUUI;

namespace Framework.Editor
{
    /// <summary>
    /// ListView 编辑器工具
    /// 负责：Adapter 生成、ViewsHolder 生成、自动绑定、废弃 Generated 清理
    /// 外围 ScrollView 结构创建请使用 OSA 原生 Wizard
    /// </summary>
    public static class ListViewEditor
    {
        #region 编译回调 - 自动挂载 Adapter

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += CheckPendingAdapterMount;
        }

        private static void CheckPendingAdapterMount()
        {
            string typeName = EditorPrefs.GetString("ListView_PendingAdapter_Type", "");
            int instanceID = EditorPrefs.GetInt("ListView_PendingAdapter_InstanceID", 0);

            if (string.IsNullOrEmpty(typeName) || instanceID == 0) return;

            bool isGrid = EditorPrefs.GetBool("ListView_PendingAdapter_IsGrid", false);
            int itemPrefabID = EditorPrefs.GetInt("ListView_PendingAdapter_ItemPrefab", 0);

            EditorPrefs.DeleteKey("ListView_PendingAdapter_Type");
            EditorPrefs.DeleteKey("ListView_PendingAdapter_InstanceID");
            EditorPrefs.DeleteKey("ListView_PendingAdapter_IsGrid");
            EditorPrefs.DeleteKey("ListView_PendingAdapter_ItemPrefab");

            Type adapterType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                adapterType = asm.GetType(typeName);
                if (adapterType != null) break;
            }

            if (adapterType == null)
            {
                Debug.LogWarning($"[ListView] 未找到类型: {typeName}，请手动挂载 Adapter");
                return;
            }

            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null)
            {
                Debug.LogWarning("[ListView] 未找到目标 GameObject，请手动挂载 Adapter");
                return;
            }

            if (go.GetComponent(adapterType) != null)
            {
                Debug.Log($"<color=yellow>[ListView] Adapter 已存在于 {go.name}</color>");
                return;
            }

            var adapter = Undo.AddComponent(go, adapterType);

            if (adapter is IOSA iAdapter)
            {
                var scrollRect = go.GetComponent<ScrollRect>();
                var baseParams = iAdapter.BaseParameters;
                if (baseParams != null && scrollRect != null)
                {
                    baseParams.Viewport = scrollRect.viewport;
                    baseParams.Content = scrollRect.content;
                    baseParams.ContentPadding = new RectOffset(10, 10, 10, 10);
                    baseParams.ContentSpacing = 10f;

                    RectTransform itemPrefab = itemPrefabID != 0
                        ? EditorUtility.InstanceIDToObject(itemPrefabID) as RectTransform
                        : null;

                    if (itemPrefab != null)
                    {
                        if (baseParams is BaseParamsWithPrefab listParams)
                            listParams.ItemPrefab = itemPrefab;
                        else if (baseParams is GridParams gridParams)
                            gridParams.Grid.CellPrefab = itemPrefab;
                    }

                    Undo.DestroyObjectImmediate(scrollRect);
                }
            }

            Debug.Log($"<color=green>[ListView] Adapter 已自动挂载到 {go.name}</color>");
            Selection.activeGameObject = go;
            EditorUtility.SetDirty(go);
        }

        #endregion

        #region 菜单入口

        [EUHotboxEntry("生成 ViewsHolder", "ListView", "从选中 Item Prefab 生成 ViewsHolder 绑定代码")]
        public static void GenerateViewsHolder()
        {
            GameObject prefab = Selection.activeGameObject;
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("错误", "请先在 Hierarchy 或 Project 中选中一个 Item Prefab", "确定");
                return;
            }
            ViewsHolderGeneratorWindow.ShowWindow(prefab);
        }

        [EUHotboxEntry("生成 Adapter", "ListView", "弹出 Adapter 生成器窗口")]
        public static void GenerateAdapter()
        {
            AdapterGeneratorWindow.ShowWindow();
        }

        [EUHotboxEntry("清理废弃 VH", "ListView", "删除没有对应逻辑脚本的 ViewsHolder.Generated 文件")]
        public static void CleanupOrphanedViewsHolders()
        {
            var config = OSAListViewConfig.GetOrCreate();
            string genDir = ToFullPath(config.holderGeneratedOutputPath);

            if (!Directory.Exists(genDir))
            {
                EditorUtility.DisplayDialog("提示", "Generated 目录不存在", "确定");
                return;
            }

            var validNames = new HashSet<string>();
            string logicRoot = ToFullPath(OSAListViewConfig.GetLogicScriptsRoot());

            if (Directory.Exists(logicRoot))
            {
                foreach (var dir in Directory.GetDirectories(logicRoot, "*", SearchOption.AllDirectories))
                {
                    if (Path.GetFileName(dir) == config.listViewSubFolder)
                    {
                        foreach (var file in Directory.GetFiles(dir, "*.cs"))
                            validNames.Add(Path.GetFileNameWithoutExtension(file));
                    }
                }
            }

            string[] genFiles = Directory.GetFiles(genDir, "*.Generated.cs");
            var orphaned = new List<string>();

            foreach (var file in genFiles)
            {
                string name = Path.GetFileName(file).Replace(".Generated.cs", "");
                if (!validNames.Contains(name))
                    orphaned.Add(file);
            }

            if (orphaned.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "未发现废弃脚本", "确定");
                return;
            }

            if (EditorUtility.DisplayDialog("清理",
                $"发现 {orphaned.Count} 个废弃脚本，是否删除？\n\n{string.Join("\n", orphaned.Select(Path.GetFileName))}",
                "删除", "取消"))
            {
                foreach (var file in orphaned)
                    AssetDatabase.DeleteAsset(ToAssetPath(file));
                AssetDatabase.Refresh();
                Debug.Log($"[ListView] 已清理 {orphaned.Count} 个废弃 Generated 文件");
            }
        }

        #endregion

        #region 辅助方法

        public static List<UIComponentInfo> AnalyzePrefab(GameObject prefab)
        {
            var result = new List<UIComponentInfo>();
            foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (t == prefab.transform) continue;
                string typeName = GetUIComponentType(t.gameObject);
                if (!string.IsNullOrEmpty(typeName))
                {
                    result.Add(new UIComponentInfo
                    {
                        name = t.name,
                        type = typeName,
                        path = GetRelativePath(t, prefab.transform)
                    });
                }
            }
            return result;
        }

        private static string GetUIComponentType(GameObject go)
        {
            if (go.TryGetComponent<Button>(out _)) return "Button";
            if (go.TryGetComponent<Toggle>(out _)) return "Toggle";
            if (go.TryGetComponent<Slider>(out _)) return "Slider";
            if (go.TryGetComponent<InputField>(out _)) return "InputField";
            if (go.TryGetComponent<TMPro.TMP_InputField>(out _)) return "TMP_InputField";
            if (go.TryGetComponent<TMPro.TextMeshProUGUI>(out _)) return "TextMeshProUGUI";
            if (go.TryGetComponent<Text>(out _)) return "Text";
            if (go.TryGetComponent<RawImage>(out _)) return "RawImage";
            if (go.TryGetComponent<Image>(out _)) return "Image";
            return null;
        }

        private static string GetRelativePath(Transform child, Transform root)
        {
            if (child.parent == root) return child.name;
            return GetRelativePath(child.parent, root) + "/" + child.name;
        }

        /// <summary>
        /// 动态定位模板文件路径（相对于本脚本所在目录的 Templates 子目录）
        /// </summary>
        internal static string GetTemplatePath(string filename)
        {
            var guids = AssetDatabase.FindAssets("ListViewEditor t:MonoScript");
            if (guids.Length > 0)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                string dir = Path.GetDirectoryName(scriptPath)?.Replace("\\", "/");
                return Path.Combine(dir, "Templates", filename).Replace("\\", "/");
            }
            return $"Assets/EUFramework/Extension/EUUI/Extension/OSAExtension/Editor/Templates/{filename}";
        }

        internal static string ToAssetPath(string fullPath)
        {
            string dataPath = Application.dataPath.Replace("\\", "/");
            fullPath = fullPath.Replace("\\", "/");
            if (fullPath.StartsWith(dataPath))
                return "Assets" + fullPath.Substring(dataPath.Length);
            return fullPath;
        }

        internal static string ToFullPath(string assetPath)
        {
            return Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath));
        }

        internal static void EnsureDirectory(string assetOrFullPath)
        {
            string full = assetOrFullPath.StartsWith("Assets")
                ? ToFullPath(assetOrFullPath)
                : assetOrFullPath;
            if (!Directory.Exists(full))
                Directory.CreateDirectory(full);
        }

        public class UIComponentInfo
        {
            public string name;
            public string type;
            public string path;
        }

        #endregion
    }

    /// <summary>
    /// Adapter 生成器弹窗 - 同时生成 Adapter、ViewsHolder、ViewsHolder.Generated 文件
    /// </summary>
    public class AdapterGeneratorWindow : EditorWindow
    {
        private string _className = "MyListAdapter";
        private string _dataClassName = "MyItemData";
        private string _vhClassName = "MyItemVH";
        private string _namespace = "Game.UI";
        private string _packageName = "Common";
        private bool _isGrid = false;

        private ScrollRect _scrollRect;
        private RectTransform _itemPrefab;

        public static void ShowWindow(bool isGrid = false)
        {
            string packageName = "Common";
            string namespaceName = "Game.UI";

            EUUIPanelDescription desc = Selection.activeGameObject?.GetComponentInParent<EUUIPanelDescription>();
            if (desc == null)
                desc = UnityEngine.Object.FindObjectOfType<EUUIPanelDescription>();

            if (desc != null)
            {
                if (!string.IsNullOrEmpty(desc.PackageName)) packageName = desc.PackageName;
                if (!string.IsNullOrEmpty(desc.Namespace)) namespaceName = desc.Namespace;
            }

            ShowWindow(isGrid, packageName, namespaceName, null, null);
        }

        public static void ShowWindow(bool isGrid, string packageName, string namespaceName,
            ScrollRect scrollRect, RectTransform itemPrefab)
        {
            var window = GetWindow<AdapterGeneratorWindow>(true, "生成 ListView 脚本");
            window._isGrid = isGrid;
            window._packageName = packageName ?? "Common";
            window._namespace = namespaceName ?? "Game.UI";
            window._scrollRect = scrollRect;
            window._itemPrefab = itemPrefab;
            window.minSize = new Vector2(460, 380);
            window.CenterOnMainWin();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("ListView 脚本生成器", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical("box");
            _className = EditorGUILayout.TextField("Adapter 类名:", _className);
            _dataClassName = EditorGUILayout.TextField("Data 类名:", _dataClassName);
            _vhClassName = EditorGUILayout.TextField("ViewsHolder 类名:", _vhClassName);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("生成位置", EditorStyles.boldLabel);
            _namespace = EditorGUILayout.TextField("命名空间:", _namespace);
            _packageName = EditorGUILayout.TextField("PackageName:", _packageName);
            _isGrid = EditorGUILayout.Toggle("Grid 模式:", _isGrid);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            var config = OSAListViewConfig.GetOrCreate();
            string baseClass = _isGrid ? "FrameworkGridAdapter" : "FrameworkListAdapter";
            string vhBaseClass = _isGrid ? "FrameworkGridViewsHolder" : "FrameworkListViewsHolder";
            string outputDir = $"{OSAListViewConfig.GetLogicScriptsRoot()}/{_packageName}/{config.listViewSubFolder}/";

            EditorGUILayout.HelpBox(
                $"将生成以下文件到 {outputDir}:\n\n" +
                $"• {_className}.cs  →  {baseClass}<{_dataClassName}, {_vhClassName}>\n" +
                $"• {_vhClassName}.cs  →  {vhBaseClass}<{_dataClassName}>\n" +
                $"• {_vhClassName}.Generated.cs  →  {config.holderGeneratedOutputPath}/",
                MessageType.Info);

            if (_scrollRect != null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    $"生成后将自动挂载 Adapter 到: {_scrollRect.name}" +
                    (_itemPrefab != null ? $"\n配置 ItemPrefab: {_itemPrefab.name}" : ""),
                    MessageType.None);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("生成全部脚本", GUILayout.Height(35)))
                GenerateAllCode();

            EditorGUILayout.Space(10);
        }

        private void GenerateAllCode()
        {
            if (string.IsNullOrEmpty(_className) || string.IsNullOrEmpty(_dataClassName) || string.IsNullOrEmpty(_vhClassName))
            {
                EditorUtility.DisplayDialog("错误", "类名不能为空！", "确定");
                return;
            }

            string adapterTplPath = ListViewEditor.GetTemplatePath("Adapter.sbn");
            string vhTplPath = ListViewEditor.GetTemplatePath("ViewsHolder.sbn");
            string vhGenTplPath = ListViewEditor.GetTemplatePath("ViewsHolder.Generated.sbn");

            if (!File.Exists(adapterTplPath) || !File.Exists(vhTplPath))
            {
                EditorUtility.DisplayDialog("错误", $"模板文件不存在！\n{adapterTplPath}", "确定");
                return;
            }

            try
            {
                var config = OSAListViewConfig.GetOrCreate();
                string logicDir = Path.Combine(OSAListViewConfig.GetLogicScriptsRoot(), _packageName, config.listViewSubFolder);
                ListViewEditor.EnsureDirectory(logicDir);

                var generatedFiles = new List<string>();

                // 1. Adapter
                string adapterPath = Path.Combine(logicDir, $"{_className}.cs");
                if (!File.Exists(adapterPath) ||
                    EditorUtility.DisplayDialog("文件已存在", $"{_className}.cs 已存在，是否覆盖？", "覆盖", "跳过"))
                {
                    var ctx = new
                    {
                        namespace_name = _namespace,
                        class_name = _className,
                        data_class_name = _dataClassName,
                        vh_class_name = _vhClassName,
                        base_class = _isGrid ? "FrameworkGridAdapter" : "FrameworkListAdapter",
                        adapter_type = _isGrid ? "Grid" : "List",
                        is_grid = _isGrid
                    };
                    File.WriteAllText(adapterPath, Template.Parse(File.ReadAllText(adapterTplPath)).Render(ctx), Encoding.UTF8);
                    generatedFiles.Add($"Adapter: {adapterPath}");
                }

                // 2. ViewsHolder
                string vhPath = Path.Combine(logicDir, $"{_vhClassName}.cs");
                if (!File.Exists(vhPath))
                {
                    var ctx = new
                    {
                        namespace_name = _namespace,
                        class_name = _vhClassName,
                        data_class_name = _dataClassName,
                        base_class = _isGrid ? "FrameworkGridViewsHolder" : "FrameworkListViewsHolder",
                        views_property = _isGrid ? "views" : "root"
                    };
                    File.WriteAllText(vhPath, Template.Parse(File.ReadAllText(vhTplPath)).Render(ctx), Encoding.UTF8);
                    generatedFiles.Add($"ViewsHolder: {vhPath}");
                }

                // 3. ViewsHolder.Generated（需要 ItemPrefab）
                if (_itemPrefab != null && File.Exists(vhGenTplPath))
                {
                    string genDir = config.holderGeneratedOutputPath;
                    ListViewEditor.EnsureDirectory(genDir);
                    string genPath = Path.Combine(genDir, $"{_vhClassName}.Generated.cs");
                    var ctx = new
                    {
                        namespace_name = _namespace,
                        class_name = _vhClassName,
                        views_property = _isGrid ? "views" : "root",
                        members = ListViewEditor.AnalyzePrefab(_itemPrefab.gameObject)
                    };
                    File.WriteAllText(genPath, Template.Parse(File.ReadAllText(vhGenTplPath)).Render(ctx), Encoding.UTF8);
                    generatedFiles.Add($"Generated: {genPath}");
                }

                AssetDatabase.Refresh();

                // 记录待挂载信息，编译完成后由 [DidReloadScripts] 自动执行
                if (_scrollRect != null)
                {
                    EditorPrefs.SetString("ListView_PendingAdapter_Type", $"{_namespace}.{_className}");
                    EditorPrefs.SetInt("ListView_PendingAdapter_InstanceID", _scrollRect.gameObject.GetInstanceID());
                    EditorPrefs.SetBool("ListView_PendingAdapter_IsGrid", _isGrid);
                    if (_itemPrefab != null)
                        EditorPrefs.SetInt("ListView_PendingAdapter_ItemPrefab", _itemPrefab.GetInstanceID());
                }

                string message = $"脚本生成完成！共 {generatedFiles.Count} 个文件\n\n{string.Join("\n", generatedFiles)}";
                if (_scrollRect != null) message += "\n\n编译完成后将自动挂载 Adapter。";
                EditorUtility.DisplayDialog("成功", message, "确定");
                Close();
            }
            catch (Exception e)
            {
                Debug.LogError($"[ListView] 生成失败: {e.Message}");
                EditorUtility.DisplayDialog("错误", e.Message, "确定");
            }
        }

        private void CenterOnMainWin()
        {
            var main = EditorGUIUtility.GetMainWindowPosition();
            var pos = position;
            pos.x = main.x + (main.width - pos.width) * 0.5f;
            pos.y = main.y + (main.height - pos.height) * 0.5f;
            position = pos;
        }
    }

    /// <summary>
    /// ViewsHolder 绑定生成器 - 扫描已有 VH 逻辑文件，仅生成 .Generated.cs 绑定部分
    /// </summary>
    public class ViewsHolderGeneratorWindow : EditorWindow
    {
        private GameObject _prefab;
        private string _namespace = "Game.UI";
        private string _packageName = "Common";
        private bool _isGrid = false;
        private List<ListViewEditor.UIComponentInfo> _components;
        private Vector2 _scrollPos;

        // VH 类名选择
        private List<string> _vhClassNames = new List<string>();
        private int _selectedVhIndex = 0;
        private string _manualClassName = "";

        private string SelectedClassName =>
            _vhClassNames.Count > 0 ? _vhClassNames[_selectedVhIndex] : _manualClassName;

        public static void ShowWindow(GameObject prefab, bool isGrid = false)
        {
            var window = GetWindow<ViewsHolderGeneratorWindow>(true, "生成 VH 绑定");
            window._prefab = prefab;
            window._components = ListViewEditor.AnalyzePrefab(prefab);

            EUUIPanelDescription desc = prefab.GetComponentInParent<EUUIPanelDescription>();
            if (desc == null)
                desc = UnityEngine.Object.FindObjectOfType<EUUIPanelDescription>();
            if (desc != null)
            {
                if (!string.IsNullOrEmpty(desc.PackageName)) window._packageName = desc.PackageName;
                if (!string.IsNullOrEmpty(desc.Namespace)) window._namespace = desc.Namespace;
            }

            window.ScanVHFiles(prefab.name);
            window.minSize = new Vector2(420, 380);
            window.CenterOnMainWin();
        }

        private void ScanVHFiles(string prefabName)
        {
            _vhClassNames.Clear();
            _selectedVhIndex = 0;

            var config = OSAListViewConfig.GetOrCreate();
            string logicDir = ListViewEditor.ToFullPath(
                Path.Combine(OSAListViewConfig.GetLogicScriptsRoot(), _packageName, config.listViewSubFolder));

            if (!Directory.Exists(logicDir))
            {
                _manualClassName = prefabName + "VH";
                return;
            }

            foreach (var file in Directory.GetFiles(logicDir, "*VH.cs"))
                _vhClassNames.Add(Path.GetFileNameWithoutExtension(file));

            if (_vhClassNames.Count == 0)
            {
                _manualClassName = prefabName + "VH";
                return;
            }

            // 按 Prefab 名前缀自动匹配：去掉 Item 后缀再做前缀比较
            string prefix = System.Text.RegularExpressions.Regex.Replace(prefabName, "(?i)item$", "").ToLower();
            int bestIdx = 0, bestScore = -1;
            for (int i = 0; i < _vhClassNames.Count; i++)
            {
                string lower = _vhClassNames[i].ToLower();
                if (lower.StartsWith(prefix) && prefix.Length > bestScore)
                {
                    bestScore = prefix.Length;
                    bestIdx = i;
                }
            }
            _selectedVhIndex = bestIdx;

            DetectIsGrid(logicDir);
        }

        private void DetectIsGrid(string logicDir)
        {
            if (_vhClassNames.Count == 0) return;
            string file = Path.Combine(logicDir, $"{_vhClassNames[_selectedVhIndex]}.cs");
            if (!File.Exists(file)) return;
            _isGrid = File.ReadAllText(file).Contains("FrameworkGridViewsHolder");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("VH 绑定生成器", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical("box");

            if (_vhClassNames.Count > 0)
            {
                int newIdx = EditorGUILayout.Popup("ViewsHolder 类名:", _selectedVhIndex, _vhClassNames.ToArray());
                if (newIdx != _selectedVhIndex)
                {
                    _selectedVhIndex = newIdx;
                    var config = OSAListViewConfig.GetOrCreate();
                    string logicDir = ListViewEditor.ToFullPath(
                        Path.Combine(OSAListViewConfig.GetLogicScriptsRoot(), _packageName, config.listViewSubFolder));
                    DetectIsGrid(logicDir);
                }
            }
            else
            {
                _manualClassName = EditorGUILayout.TextField("ViewsHolder 类名:", _manualClassName);
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("命名空间:", _namespace);
            EditorGUILayout.Toggle("Grid 模式:", _isGrid);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"检测到 {_components?.Count ?? 0} 个 UI 组件:", EditorStyles.boldLabel);

            if (_components != null)
            {
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(130));
                foreach (var comp in _components)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"  {comp.type}", GUILayout.Width(140));
                    EditorGUILayout.LabelField(comp.name, EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新组件", GUILayout.Height(30)))
                _components = ListViewEditor.AnalyzePrefab(_prefab);
            if (GUILayout.Button("生成绑定", GUILayout.Height(30)))
                GenerateCode();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
        }

        private void GenerateCode()
        {
            string className = SelectedClassName;
            if (string.IsNullOrEmpty(className))
            {
                EditorUtility.DisplayDialog("错误", "类名不能为空！", "确定");
                return;
            }

            string genTplPath = ListViewEditor.GetTemplatePath("ViewsHolder.Generated.sbn");
            if (!File.Exists(genTplPath))
            {
                EditorUtility.DisplayDialog("错误", $"模板文件不存在！\n{genTplPath}", "确定");
                return;
            }

            try
            {
                var config = OSAListViewConfig.GetOrCreate();
                var ctx = new
                {
                    namespace_name = _namespace,
                    class_name = className,
                    views_property = _isGrid ? "views" : "root",
                    members = _components
                };

                string genDir = config.holderGeneratedOutputPath;
                ListViewEditor.EnsureDirectory(genDir);
                string genPath = Path.Combine(genDir, $"{className}.Generated.cs");
                File.WriteAllText(genPath, Template.Parse(File.ReadAllText(genTplPath)).Render(ctx), Encoding.UTF8);

                AssetDatabase.Refresh();
                Debug.Log($"<color=white>[ListView] VH 绑定已生成: {genPath}</color>");
                EditorUtility.DisplayDialog("成功", $"VH 绑定生成完成！\n\n{genPath}", "确定");
                Close();
            }
            catch (Exception e)
            {
                Debug.LogError($"[ListView] 生成失败: {e.Message}");
                EditorUtility.DisplayDialog("错误", e.Message, "确定");
            }
        }

        private void CenterOnMainWin()
        {
            var main = EditorGUIUtility.GetMainWindowPosition();
            var pos = position;
            pos.x = main.x + (main.width - pos.width) * 0.5f;
            pos.y = main.y + (main.height - pos.height) * 0.5f;
            position = pos;
        }
    }
}
