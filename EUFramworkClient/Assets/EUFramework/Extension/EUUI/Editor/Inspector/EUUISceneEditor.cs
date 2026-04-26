#if UNITY_EDITOR
using System;
using System.IO;
using EUFramework.Extension.EUUI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UI;

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
            return EUUIEditorSOPaths.EditorConfigAssetPath;
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

        /// <summary>
        /// 在 Project 窗口中定位当前场景对应的面板 Prefab。
        /// </summary>
        public static void LocateCurrentPrefab()
        {
            var config = GetConfig();
            if (config == null)
            {
                EditorUtility.DisplayDialog("提示", "未找到 EUUIEditorConfig，无法定位 Prefab。", "确定");
                return;
            }

            var desc = UnityEngine.Object.FindFirstObjectByType<EUUIPanelDescription>();
            if (desc == null)
            {
                EditorUtility.DisplayDialog("提示", "当前场景未找到 EUUIPanelDescription，无法确认包类型。", "确定");
                return;
            }

            string panelName = EditorSceneManager.GetActiveScene().name;
            string dir = config.GetUIPrefabDir(desc.PackageType);
            string prefabPath = $"{dir}/{panelName}.prefab".Replace("\\", "/");

            var prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(prefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("提示",
                    $"未找到 Prefab：\n{prefabPath}\n\n请先执行「自动流程」生成 Prefab。", "确定");
                return;
            }

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        /// <summary>
        /// 弹出 Area 设计参考框创建窗口。
        /// </summary>
        [EUHotboxEntry("创建 Area", "UI 制作", "在 UIRoot 下创建或更新多人分屏 Area 设计参考框")]
        public static void ShowCreateAreaWindow()
        {
            EUUIAreaCreateWindow.ShowWindow(CreateOrUpdateArea);
        }

        /// <summary>
        /// 在当前场景的 UIRoot 下创建或更新 Area 容器节点。
        /// 流程：先以 center 锚点 + 动态 sizeDelta 定位（让 Unity 用正确尺寸计算位置），
        /// 再立即转为全拉伸锚点（0,0 → 1,1），与运行时挂载到 PlayerRoot 后行为一致。
        /// Canvas 的 referenceResolution 不变，始终以 EUUIEditorConfig 配置为准。
        /// </summary>
        public static void CreateOrUpdateArea(int playerCount,
            MultiplayerLayoutMode layout, MultiplayerLayoutAxis axis)
        {
            var config = GetConfig();
            if (config == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到 EUUIEditorConfig，请先配置。", "确定");
                return;
            }

            GameObject exportRoot = GameObject.Find(config.exportRootName);
            if (exportRoot == null)
            {
                EditorUtility.DisplayDialog("错误",
                    $"场景中未找到 [{config.exportRootName}]，请先创建 UI 场景。", "确定");
                return;
            }

            // 计算玩家区域像素尺寸（同一布局下所有槽位相同，取槽位 0）
            var slotRect = EUUIKit.GetSlotRect(0, playerCount, layout, axis);
            float width = slotRect.width * config.referenceResolution.x;
            float height = slotRect.height * config.referenceResolution.y;

            // 找或创建 Area 节点
            Transform areaTrans = exportRoot.transform.Find("Area");
            GameObject areaGO;
            if (areaTrans != null)
            {
                areaGO = areaTrans.gameObject;
            }
            else
            {
                areaGO = new GameObject("Area", typeof(RectTransform));
                areaGO.layer = exportRoot.layer;
                areaGO.transform.SetParent(exportRoot.transform, false);
            }

            var rt = areaGO.GetComponent<RectTransform>();

            // 步骤一：初始设置（Center锚点）
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = Vector2.zero;

            // 关键：复刻编辑器的锚点切换逻辑
            SwitchAnchorToStretchWithoutSizeChange(rt);

            EditorUtility.SetDirty(areaGO);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Selection.activeGameObject = areaGO;
            EditorGUIUtility.PingObject(areaGO);

            string layoutDesc = layout == MultiplayerLayoutMode.Grid
                ? "Grid 2×2"
                : $"Linear {axis} {playerCount}人";
            Debug.Log($"[EUUI] Area 已更新：{layoutDesc} → {width}×{height} px，全拉伸填满 UIRoot");
        }
        private static void SwitchAnchorToStretchWithoutSizeChange(RectTransform targetRt)
        {
            // 1. 记录切换前的关键数据（世界空间的矩形）
            Rect rectBefore = targetRt.rect;
            Vector2 worldPosBefore = targetRt.TransformPoint(rectBefore.center);

            // 2. 切换锚点
            targetRt.anchorMin = Vector2.zero;
            targetRt.anchorMax = Vector2.one;

            // 3. 编辑器核心补偿步骤：还原位置和尺寸
            // 3.1 先把锚点位置归中（保证居中）
            targetRt.anchoredPosition = Vector2.zero;
            // 3.2 计算补偿后的sizeDelta（让尺寸回到切换前）
            Vector2 parentSize = Vector2.zero;
            if (targetRt.parent is RectTransform parentRt)
            {
                parentSize = parentRt.rect.size;
            }
            // 核心公式：sizeDelta = 目标尺寸 - 父节点尺寸（和编辑器一致）
            targetRt.sizeDelta = new Vector2(rectBefore.width - parentSize.x, rectBefore.height - parentSize.y);

            // 4. 可选：还原世界位置（确保位置完全不变，应对特殊布局）
            Vector2 localPos = targetRt.InverseTransformPoint(worldPosBefore);
            targetRt.anchoredPosition = localPos;
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

}
#endif
