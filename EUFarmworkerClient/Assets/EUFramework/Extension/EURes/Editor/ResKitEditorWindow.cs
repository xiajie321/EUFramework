#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using YooAsset.Editor;

namespace EUFramework.Extension.EURes.Editor
{
    public class ResKitEditorWindow : EditorWindow
    {
        [MenuItem("EUFramework/ResKit 配置工具", priority = 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<ResKitEditorWindow>();
            window.titleContent = new GUIContent("ResKit 配置工具");
            window.minSize = new Vector2(800, 600);
        }

        private void CreateGUI()
        {
            // 加载 UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/EUFramework/Extension/EURes/Editor/UI/ResKitEditorWindow.uxml");
            
            if (visualTree != null)
            {
                visualTree.CloneTree(rootVisualElement);
            }
            else
            {
                CreateFallbackUI();
                return;
            }

            // 加载样式
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/EUFramework/Extension/EURes/Editor/UI/ResKitEditorWindow.uss");
            
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            // 绑定按钮事件
            BindButtons();
        }

        private void BindButtons()
        {
            var btn1 = rootVisualElement.Q<Button>("btn-create-settings");
            var btn2 = rootVisualElement.Q<Button>("btn-create-prefab");
            var btn3 = rootVisualElement.Q<Button>("btn-generate-reskit");

            if (btn1 != null)
                btn1.clicked += OnCreateSettingsClicked;
            
            if (btn2 != null)
                btn2.clicked += OnCreatePrefabClicked;
            
            if (btn3 != null)
                btn3.clicked += OnGenerateResKitClicked;
        }

        #region 按钮事件处理

        private void OnCreateSettingsClicked()
        {
            string settingsPath = "Assets/EUFramework/Resources/ResKitSettings";
            
            if (!Directory.Exists(settingsPath))
            {
                Directory.CreateDirectory(settingsPath);
                AssetDatabase.Refresh();
            }

            // 1. 创建 AssetBundleCollectorSetting
            CreateAssetBundleCollectorSetting(settingsPath);

            // 2. 创建 YooAssetSettings
            CreateYooAssetSettings(settingsPath);

            // 3. 创建 ResServerConfig
            CreateResServerConfig(settingsPath);

            UpdateContentArea($"✓ 配置文件创建完成！\n\n路径: {settingsPath}\n\n已创建:\n- AssetBundleCollectorSetting.asset\n- YooAssetSettings.asset\n- ResServerConfig.asset");
            AssetDatabase.Refresh();
        }

        private void OnCreatePrefabClicked()
        {
            string prefabPath = "Assets/EUFramework/Extension/EURes/Prefabs";
            
            if (!Directory.Exists(prefabPath))
            {
                Directory.CreateDirectory(prefabPath);
                AssetDatabase.Refresh();
            }

            string fullPath = Path.Combine(prefabPath, "ResKitUserOpePopUp.prefab");

            // 检查是否已存在
            if (File.Exists(fullPath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "文件已存在",
                    $"预制体已存在:\n{fullPath}\n\n是否覆盖?",
                    "覆盖",
                    "取消"
                );

                if (!overwrite)
                {
                    UpdateContentArea("操作已取消");
                    return;
                }
            }

            // 创建默认的弹窗预制体
            GameObject popup = CreateDefaultPopupPrefab();
            
            // 保存为预制体
            PrefabUtility.SaveAsPrefabAsset(popup, fullPath);
            DestroyImmediate(popup);

            UpdateContentArea($"✓ UI Prefab 创建完成！\n\n路径: {fullPath}\n\n包含组件:\n- Canvas (ScreenSpaceOverlay)\n- Panel 背景面板\n- Title 标题文本\n- Content 内容文本\n- BtnConfirm 确认按钮\n- BtnCancel 取消按钮");
            AssetDatabase.Refresh();
            
            // 选中创建的预制体
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
            EditorGUIUtility.PingObject(prefab);
            Selection.activeObject = prefab;
        }

        private void OnGenerateResKitClicked()
        {
            string templatePath = "Assets/EUFramework/Extension/EURes/Editor/Templates/DefaultResKit.Generated.sbn";
            string outputPath = "Assets/EUFramework/Extension/EURes/Script/Generated/ResKit.Generated.cs";

            if (!File.Exists(templatePath))
            {
                UpdateContentArea($"✗ 错误：模板文件不存在！\n\n路径: {templatePath}");
                return;
            }

            // 读取模板
            string template = File.ReadAllText(templatePath);

            // 替换变量
            string generated = template
                .Replace("{{ namespace }}", "EUFramework.Extension.EURes")
                .Replace("{{ class_name }}", "ResKit");

            // 确保输出目录存在
            string outputDir = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // 保存生成的代码
            File.WriteAllText(outputPath, generated);
            AssetDatabase.Refresh();

            UpdateContentArea($"✓ ResKit 代码生成完成！\n\n路径: {outputPath}\n\n生成的方法:\n- InitPackageResAsync()\n- GetPackage()\n- SetDefaultPackage()\n- IsInitialized()");
            
            // 选中生成的文件
            var script = AssetDatabase.LoadAssetAtPath<TextAsset>(outputPath);
            EditorGUIUtility.PingObject(script);
            Selection.activeObject = script;
        }

        #endregion

        #region 创建配置文件

        private void CreateAssetBundleCollectorSetting(string basePath)
        {
            string path = Path.Combine(basePath, "AssetBundleCollectorSetting.asset");
            
            var existing = AssetDatabase.LoadAssetAtPath<AssetBundleCollectorSetting>(path);
            if (existing != null)
            {
                Debug.Log($"[ResKit] AssetBundleCollectorSetting 已存在: {path}");
                EditorGUIUtility.PingObject(existing);
                return;
            }

            var setting = ScriptableObject.CreateInstance<AssetBundleCollectorSetting>();
            AssetDatabase.CreateAsset(setting, path);
            Debug.Log($"[ResKit] AssetBundleCollectorSetting 创建成功: {path}");
        }

        private void CreateYooAssetSettings(string basePath)
        {
            string path = Path.Combine(basePath, "YooAssetSettings.asset");
            
            // YooAsset 的设置类型可能不同，这里使用通用方法
            var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (existing != null)
            {
                Debug.Log($"[ResKit] YooAssetSettings 已存在: {path}");
                EditorGUIUtility.PingObject(existing);
                return;
            }

            // 尝试创建 YooAsset 的构建设置
            try
            {
                var settingType = System.Type.GetType("YooAsset.Editor.AssetBundleBuilderSetting,YooAsset.Editor");
                if (settingType != null)
                {
                    var setting = ScriptableObject.CreateInstance(settingType);
                    AssetDatabase.CreateAsset(setting, path);
                    Debug.Log($"[ResKit] YooAssetSettings 创建成功: {path}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ResKit] YooAssetSettings 创建失败: {e.Message}");
            }
        }

        private void CreateResServerConfig(string basePath)
        {
            string path = Path.Combine(basePath, "ResServerConfig.asset");
            
            var existing = AssetDatabase.LoadAssetAtPath<EUFramework.Extension.EURes.ResServerConfig>(path);
            if (existing != null)
            {
                Debug.Log($"[ResKit] ResServerConfig 已存在: {path}");
                EditorGUIUtility.PingObject(existing);
                return;
            }

            var config = ScriptableObject.CreateInstance<EUFramework.Extension.EURes.ResServerConfig>();
            config.protocol = EUFramework.Extension.EURes.ServerProtocol.HTTP;
            config.hostServer = "127.0.0.1";
            config.port = 80;
            config.appVersion = "1.0.0";
            
            AssetDatabase.CreateAsset(config, path);
            Debug.Log($"[ResKit] ResServerConfig 创建成功: {path}");
        }

        #endregion

        #region 创建默认 UI Prefab

        private GameObject CreateDefaultPopupPrefab()
        {
            // 创建根对象
            GameObject root = new GameObject("ResKitUserOpePopUp");
            
            // 添加 Canvas 组件
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            var canvasScaler = root.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            
            root.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // 创建背景面板
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);
            
            var rectTransform = panel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(600, 400);
            
            var image = panel.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            // 创建标题
            CreateText(panel.transform, "Title", "提示标题-Text", new Vector2(0, 150), 24);

            // 创建内容
            CreateText(panel.transform, "Content", "context -Text 居中", new Vector2(0, 0), 18);

            // 创建按钮
            CreateButton(panel.transform, "BtnConfirm", "按钮 确认", new Vector2(-100, -120));
            CreateButton(panel.transform, "BtnCancel", "按钮取消", new Vector2(100, -120));

            return root;
        }

        private void CreateText(Transform parent, string name, string text, Vector2 position, int fontSize)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            
            var rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(500, 50);
            
            var textComponent = textObj.AddComponent<UnityEngine.UI.Text>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.white;
            
            // 使用 Unity 默认字体
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private void CreateButton(Transform parent, string name, string text, Vector2 position)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);
            
            var rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(150, 40);
            
            var image = buttonObj.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.8f, 0.4f, 0.2f, 1f);
            
            buttonObj.AddComponent<UnityEngine.UI.Button>();

            // 创建按钮文本
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            var textComponent = textObj.AddComponent<UnityEngine.UI.Text>();
            textComponent.text = text;
            textComponent.fontSize = 16;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.white;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        #endregion

        #region UI 更新

        private void UpdateContentArea(string message)
        {
            var contentLabel = rootVisualElement.Q<Label>("content-label");
            if (contentLabel != null)
            {
                contentLabel.text = message;
            }
        }

        private void CreateFallbackUI()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;
            container.style.justifyContent = Justify.Center;
            container.style.alignItems = Align.Center;
            
            var label = new Label("UXML 文件未找到！\n请确保文件存在:\nAssets/EUFramework/Extension/EURes/Editor/UI/ResKitEditorWindow.uxml");
            label.style.fontSize = 16;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.color = new Color(1f, 0.5f, 0.5f);
            
            container.Add(label);
            rootVisualElement.Add(container);
        }

        #endregion
    }
}
#endif
