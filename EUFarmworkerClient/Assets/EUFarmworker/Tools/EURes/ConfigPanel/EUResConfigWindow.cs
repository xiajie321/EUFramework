using System.IO;
using EUFarmworker.Tools.EURes.Script;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EUFarmworker.Tools.EURes.ConfigPanel
{
    public class EUResConfigWindow : EditorWindow
    {
        private const string ConfigPath = "Assets/Resources/EUResConfig.asset";
        private const string UxmlPath = "Assets/EUFarmworker/Tools/EURes/ConfigPanel/EUResConfigPanel.uxml";
        private const string UssPath = "Assets/EUFarmworker/Tools/EURes/ConfigPanel/EUResConfigPanel.uss";

        private EUResConfig _config;
        private SerializedObject _serializedConfig;

        [MenuItem("EUFarmworker/EURes/Config Panel")]
        public static void ShowWindow()
        {
            EUResConfigWindow wnd = GetWindow<EUResConfigWindow>();
            wnd.titleContent = new GUIContent("EURes Config");
            wnd.minSize = new Vector2(400, 500);
        }

        public void CreateGUI()
        {
            // 加载 UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (visualTree == null)
            {
                var label = new Label($"Error: Could not load UXML at {UxmlPath}");
                label.style.color = Color.red;
                rootVisualElement.Add(label);
                return;
            }
            visualTree.CloneTree(rootVisualElement);

            // 加载 USS
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            // 刷新 UI 状态
            RefreshUI();
        }

        private void RefreshUI()
        {
            var warningContainer = rootVisualElement.Q<VisualElement>("warning-container");
            var configContainer = rootVisualElement.Q<VisualElement>("config-container");
            var createBtn = rootVisualElement.Q<Button>("create-btn");

            // 尝试加载配置
            _config = Resources.Load<EUResConfig>("EUResConfig");
            
            // 如果 Resources.Load 失败，尝试通过 AssetDatabase 加载（以防不在 Resources 根目录下）
            if (_config == null)
            {
                _config = AssetDatabase.LoadAssetAtPath<EUResConfig>(ConfigPath);
            }

            if (_config != null)
            {
                // 配置存在，显示配置面板
                warningContainer.style.display = DisplayStyle.None;
                configContainer.style.display = DisplayStyle.Flex;

                _serializedConfig = new SerializedObject(_config);
                configContainer.Bind(_serializedConfig);
            }
            else
            {
                // 配置不存在，显示警告和创建按钮
                warningContainer.style.display = DisplayStyle.Flex;
                configContainer.style.display = DisplayStyle.None;

                if (createBtn != null)
                {
                    createBtn.clicked -= OnCreateConfig;
                    createBtn.clicked += OnCreateConfig;
                }
            }
        }

        private void OnCreateConfig()
        {
            // 确保 Resources 目录存在
            string resourcesPath = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // 创建配置资源
            var newConfig = ScriptableObject.CreateInstance<EUResConfig>();
            AssetDatabase.CreateAsset(newConfig, ConfigPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[EURes] Config created at {ConfigPath}");

            // 刷新 UI
            RefreshUI();
        }

        private void OnFocus()
        {
            // 当窗口获得焦点时刷新，以防配置在外部被删除或创建
            RefreshUI();
        }
    }
}
