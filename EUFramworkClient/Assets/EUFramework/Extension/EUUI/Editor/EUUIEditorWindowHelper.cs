#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// EUUIEditorWindow 各 Panel 共享的 UI 工具方法
    /// </summary>
    internal interface IEUUIPanel
    {
        void Build(VisualElement contentArea);
    }

    internal static class EUUIEditorWindowHelper
    {
        internal static string GetEditorUIPath()
        {
            var guids = AssetDatabase.FindAssets("EUUIEditorWindow t:MonoScript");
            if (guids != null && guids.Length > 0)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                string scriptDir = Path.GetDirectoryName(scriptPath)?.Replace("\\", "/");
                return Path.Combine(scriptDir, "UI").Replace("\\", "/");
            }
            return "Assets/EUFramework/Extension/EUUI/Editor/UI";
        }

        internal static VisualTreeAsset LoadUXMLTemplate(string filename)
        {
            string uxmlPath = Path.Combine(GetEditorUIPath(), filename).Replace("\\", "/");
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (template == null)
                Debug.LogError($"[EUUI] 无法加载 UXML 模板: {uxmlPath}");
            return template;
        }

        internal static VisualElement CreateContentHeader(string title, string subtitle)
        {
            var header = new VisualElement();
            header.AddToClassList("content-header");

            var titleLabel = new Label(title);
            titleLabel.AddToClassList("content-title");
            header.Add(titleLabel);

            var subtitleLabel = new Label(subtitle);
            subtitleLabel.AddToClassList("content-subtitle");
            header.Add(subtitleLabel);

            return header;
        }

        internal static VisualElement CreateTabBar()
        {
            var bar = new VisualElement();
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.marginTop = 15;
            bar.style.marginBottom = 10;
            bar.style.borderBottomWidth = 1;
            bar.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            bar.style.alignSelf = Align.Stretch;
            return bar;
        }

        internal static VisualElement CreateTabContentContainer()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;
            container.style.alignSelf = Align.Stretch;
            return container;
        }

        internal static Button CreateTabButton(string text, bool isActive)
        {
            var button = new Button { text = text };
            button.style.height = 32;
            button.style.paddingLeft = 15;
            button.style.paddingRight = 15;
            button.style.marginRight = 5;
            button.style.borderLeftWidth = 0;
            button.style.borderRightWidth = 0;
            button.style.borderTopWidth = 0;
            button.style.borderBottomWidth = 0;
            button.style.borderTopLeftRadius = 4;
            button.style.borderTopRightRadius = 4;
            button.style.backgroundColor = isActive ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.2f, 0.2f, 0.2f);
            button.style.color = isActive ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.6f, 0.6f, 0.6f);
            if (isActive)
                button.AddToClassList("tab-active");
            return button;
        }

        internal static void SetActiveTab(Button activeTab, params Button[] inactiveTabs)
        {
            activeTab.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            activeTab.style.color = new Color(0.9f, 0.9f, 0.9f);
            activeTab.AddToClassList("tab-active");

            foreach (var tab in inactiveTabs)
            {
                tab.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                tab.style.color = new Color(0.6f, 0.6f, 0.6f);
                tab.RemoveFromClassList("tab-active");
            }
        }
    }
}
#endif
