#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UI;
using EUFramework.Extension.EUUI;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// EUUI 场景创建编辑器：根据 UIRoot / ExcludedBottom / ExcludedTop 在 UISceneSavePath 下创建 UI 场景
    /// </summary>
    public static class EUUISceneEditor
    {
        /// <summary>
        /// 动态查找 EUUIEditorConfig 资源路径
        /// </summary>
        internal static string GetEditorConfigPath()
        {
            string[] guids = AssetDatabase.FindAssets("EUUIEditorConfig t:EUUIEditorConfig");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path != null && path.EndsWith("EUUIEditorConfig.asset", StringComparison.OrdinalIgnoreCase))
                    return path;
            }
            return "Assets/EUFramework/Extension/EUUI/Editor/EditorSO/EUUIEditorConfig.asset"; // 兜底路径
        }

        private static EUUIEditorConfig GetConfig()
        {
            return AssetDatabase.LoadAssetAtPath<EUUIEditorConfig>(GetEditorConfigPath());
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
        /// 执行 UI 场景模板的创建（仅从默认配置表 EUUIEditorConfig 读取路径与层级名）
        /// </summary>
        public static void ExecuteCreateUIScene(string panelName, EUUIPanelDescription template)
        {
            var config = GetConfig();
            if (config == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到 EUUIEditorConfig，请先通过「EUUI 配置工具」创建 UI 配置。", "确定");
                return;
            }

            string sceneSavePath = config.uiSceneSavePath;
            string nameUIRoot = config.exportRootName;
            string nameExcludedBottom = config.notExportBottomName;
            string nameExcludedTop = config.notExportTopName;

            string saveDir = Path.Combine(sceneSavePath, template.PackageName);
            EnsureDirectory(saveDir);

            string scenePath = $"{saveDir}/{panelName}.unity".Replace("\\", "/");
            if (File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog("提示", $"UI 界面 [{panelName}] 已存在！\n请检查是否重名或先手动删除旧场景。", "确定");
                return;
            }

            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. 根节点 + 元数据
            GameObject uiRoot = new GameObject(panelName);
            var desc = uiRoot.AddComponent<EUUIPanelDescription>();
            EditorUtility.CopySerialized(template, desc);

            // 2. 环境节点
            GameObject mainCam = new GameObject("Main Camera", typeof(Camera));
            mainCam.transform.SetParent(uiRoot.transform);
            var cam = mainCam.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;

            GameObject light = new GameObject("Directional Light", typeof(Light));
            light.transform.SetParent(uiRoot.transform);
            light.GetComponent<Light>().type = LightType.Directional;

            // 3. Canvas + 分辨率约定 + 三层子节点（ExcludedBottom, UIRoot, ExcludedTop）
            GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(uiRoot.transform);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = config.referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = config.matchWidthOrHeight;
            scaler.referencePixelsPerUnit = config.referencePixelsPerUnit;

            CreateSubLayer(canvasGO.transform, nameExcludedBottom);
            CreateSubLayer(canvasGO.transform, nameUIRoot);
            CreateSubLayer(canvasGO.transform, nameExcludedTop);

            // 4. EventSystem（自动适配 Input System / Input Module）
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.transform.SetParent(uiRoot.transform);

            bool hasInputSystem = false;
            try
            {
                var inputSystemType = System.Type.GetType(
                    "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputSystemType != null)
                {
                    eventSystem.AddComponent(inputSystemType);
                    hasInputSystem = true;
                }
            }
            catch (System.Exception) { }

            if (!hasInputSystem)
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            if (EditorSceneManager.SaveScene(newScene, scenePath))
            {
                AssetDatabase.Refresh();
                Debug.Log($"[EUUI] UI 场景创建成功: {scenePath}");
            }

            Selection.activeGameObject = uiRoot;
        }

        private static void CreateSubLayer(Transform parent, string layerName)
        {
            GameObject go = new GameObject(layerName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        [EUHotboxEntry("创建 UI 场景", "UI 制作", "弹出场景名称输入窗口，创建标准 UI 场景结构")]
        [Shortcut("EUUI/创建 UI 场景", KeyCode.U, ShortcutModifiers.Alt)]
        public static void ShowCreateSceneWindow()
        {
            EUUISceneCreateWindow.ShowWindow((name, template) => ExecuteCreateUIScene(name, template));
        }

        /// <summary>
        /// 定位到当前场景的 UIRoot 节点（聚焦并展开 Hierarchy）
        /// </summary>
        [EUHotboxEntry("定位 UIRoot", "UI 制作", "在 Hierarchy 中定位并展开 UIRoot 节点")]
        // [MenuItem("EUFramework/拓展/EUUI/定位 UIRoot &f", false, 103)]
        public static void LocateUIRoot()
        {
            var config = GetConfig();
            if (config == null)
            {
                EditorUtility.DisplayDialog("提示", "未找到 EUUIEditorConfig，无法获取 UIRoot 名称。", "确定");
                return;
            }

            var desc = UnityEngine.Object.FindFirstObjectByType<EUUIPanelDescription>();
            if (desc == null)
            {
                EditorUtility.DisplayDialog("提示", "当前场景未找到 EUUIPanelDescription 组件。", "确定");
                return;
            }

            GameObject uiRoot = GameObject.Find(config.exportRootName);
            if (uiRoot == null)
            {
                EditorUtility.DisplayDialog("提示", $"当前场景未找到 [{config.exportRootName}] 节点。", "确定");
                return;
            }

            Selection.activeGameObject = uiRoot;
            EditorGUIUtility.PingObject(uiRoot);
            ExpandHierarchyToObject(uiRoot);
            Debug.Log($"[EUUI] 已定位到: {desc.PackageName}/{UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name}");
        }

        private static void ExpandHierarchyToObject(GameObject target)
        {
            var hierarchyType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            if (hierarchyType == null) return;

            var window = EditorWindow.GetWindow(hierarchyType);
            if (window == null) return;

            var sceneHierarchy = hierarchyType.GetProperty("sceneHierarchy",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(window);

            if (sceneHierarchy != null)
            {
                var setExpandedMethod = sceneHierarchy.GetType().GetMethod("SetExpanded",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                if (setExpandedMethod != null)
                {
                    Transform current = target.transform;
                    while (current != null)
                    {
                        setExpandedMethod.Invoke(sceneHierarchy, new object[] { current.gameObject.GetInstanceID(), true });
                        current = current.parent;
                    }
                }
            }

            window.Repaint();
        }
    }

    /// <summary>
    /// 创建 UI 场景时的输入窗口（面板名 + EUUIPanelDescription）
    /// </summary>
    public class EUUISceneCreateWindow : EditorWindow
    {
        private string _panelName = "";
        private GameObject _tempGO;
        private EUUIPanelDescription _tempDesc;
        private SerializedObject _serializedObject;
        private Action<string, EUUIPanelDescription> _onConfirm;
        private bool _hasFocusedPanelName;
        private bool _pendingFocusPanelName;

        public static void ShowWindow(Action<string, EUUIPanelDescription> onConfirm)
        {
            var window = GetWindow<EUUISceneCreateWindow>(true, "创建 UI 场景", true);
            window._onConfirm = onConfirm;
            window.minSize = new Vector2(520, 320);
            window.CenterOnMainWin();
            window.Focus();
        }

        private void OnEnable()
        {
            _tempGO = new GameObject("TempDesc") { hideFlags = HideFlags.DontSave };
            _tempDesc = _tempGO.AddComponent<EUUIPanelDescription>();
            if (_tempDesc == null)
            {
                Debug.LogError("[EUUI] 无法添加 EUUIPanelDescription，请确保该脚本位于非 Editor 程序集中以便挂载。");
                return;
            }
            var templateConfig = EUUITemplateLocator.GetTemplateConfig();
            if (templateConfig != null && !string.IsNullOrEmpty(templateConfig.namespaceName))
            {
                _tempDesc.Namespace = templateConfig.namespaceName;
            }
            _serializedObject = new SerializedObject(_tempDesc);
        }

        private void OnDisable()
        {
            if (_tempGO) DestroyImmediate(_tempGO);
        }

        private void OnGUI()
        {
            GUI.enabled = true;

            EditorGUILayout.Space(10);

            GUI.SetNextControlName("PanelNameField");
            _panelName = EditorGUILayout.TextField("面板名称 (Name):", _panelName);
            if (!_hasFocusedPanelName)
            {
                _hasFocusedPanelName = true;
                EditorApplication.delayCall += () =>
                {
                    if (this != null)
                    {
                        Focus();
                        _pendingFocusPanelName = true;
                        Repaint();
                    }
                };
            }
            if (_pendingFocusPanelName)
            {
                _pendingFocusPanelName = false;
                GUI.FocusControl("PanelNameField");
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            if (_serializedObject != null)
            {
                _serializedObject.Update();
                EditorGUI.BeginChangeCheck();

                SerializedProperty iterator = _serializedObject.GetIterator();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (iterator.name == "m_Script") continue;
                    EditorGUILayout.PropertyField(iterator, true);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    _serializedObject.ApplyModifiedProperties();
                    Repaint();
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("确认创建", GUILayout.Height(35)))
            {
                ConfirmAndClose();
            }
            EditorGUILayout.Space(10);
        }

        private void ConfirmAndClose()
        {
            if (string.IsNullOrEmpty(_panelName))
            {
                EditorUtility.DisplayDialog("错误", "面板名称不能为空！", "确定");
                return;
            }

            string finalName = _panelName.StartsWith("Wnd") ? _panelName : "Wnd" + _panelName;
            _onConfirm?.Invoke(finalName, _tempDesc);
            Close();
        }
    }

    internal static class EUUIEditorWindowExtensions
    {
        public static void CenterOnMainWin(this EditorWindow window)
        {
            var main = EditorGUIUtility.GetMainWindowPosition();
            var pos = window.position;
            pos.x = main.x + (main.width - pos.width) * 0.5f;
            pos.y = main.y + (main.height - pos.height) * 0.5f;
            window.position = pos;
        }
    }
}
#endif
