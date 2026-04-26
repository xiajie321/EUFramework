#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EUFramework.Extension.EUUI;

namespace EUFramework.Extension.EUUI.Editor.Templates
{
    /// <summary>
    /// Binds generated fields to scene nodes after scripts recompile, then exports the clean prefab.
    /// </summary>
    internal static class EUUIPrefabBinder
    {
        public static void PerformBinding(string panelName)
        {
            var config = EUUIPanelExporter.GetConfig();
            var editorConfig = AssetDatabase.LoadAssetAtPath<EUUIEditorConfig>(EUUISceneEditor.GetEditorConfigPath());
            if (config == null || editorConfig == null)
            {
                Debug.LogError("[EUUI] 绑定失败：未找到配置文件（EUUITemplateConfig / EUUIEditorConfig）");
                return;
            }

            GameObject exportRoot = GameObject.Find(editorConfig.exportRootName);
            if (exportRoot == null)
            {
                Debug.LogError($"[EUUI] 绑定失败：场景中找不到 [{editorConfig.exportRootName}]");
                return;
            }

            RemoveMissingScripts(exportRoot);

            var desc = exportRoot.GetComponentInParent<EUUIPanelDescription>() ?? UnityEngine.Object.FindFirstObjectByType<EUUIPanelDescription>();
            string ns = desc != null && !string.IsNullOrEmpty(desc.Namespace) ? desc.Namespace : config.namespaceName;

            Type type = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic) continue;
                string fullName = ns + "." + panelName;
                type = asm.GetType(fullName);
                if (type != null) break;
            }
            if (type == null)
            {
                Debug.LogError($"[EUUI] 绑定失败：找不到类型 {ns}.{panelName}，请检查编译是否通过。");
                return;
            }

            var comp = exportRoot.GetComponent(type) ?? exportRoot.AddComponent(type);
            var nodes = exportRoot.GetComponentsInChildren<EUUINodeBind>(true);
            foreach (var node in nodes)
            {
                var field = type.GetField(node.GetFinalMemberName(), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    Type compType = GetComponentType(node.GetFinalComponentType());
                    var targetComp = node.GetComponent(compType);
                    if (targetComp != null)
                        field.SetValue(comp, targetComp);
                }
            }

            EditorUtility.SetDirty(comp);
            EditorUtility.SetDirty(exportRoot);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EUUIPrefabExporter.FinalizePrefab(exportRoot, panelName, editorConfig);
        }

        private static void RemoveMissingScripts(GameObject root)
        {
            if (root == null) return;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            int totalRemoved = 0;
            foreach (var t in transforms)
            {
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                totalRemoved += removed;
            }
            if (totalRemoved > 0)
                Debug.Log($"[EUUI] 已移除 {totalRemoved} 个缺失脚本（{root.name} 及其子节点）。");
        }

        private static Type GetComponentType(EUUINodeBindType bindType)
        {
            switch (bindType)
            {
                case EUUINodeBindType.RectTransform: return typeof(RectTransform);
                case EUUINodeBindType.Image: return typeof(UnityEngine.UI.Image);
                case EUUINodeBindType.Text: return typeof(UnityEngine.UI.Text);
                case EUUINodeBindType.Button: return typeof(UnityEngine.UI.Button);
                case EUUINodeBindType.TextMeshProUGUI:
                    var t = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
                    return t ?? typeof(Component);
                default: return typeof(RectTransform);
            }
        }
    }
}
#endif
