#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// EUUI 节点绑定编辑器：批量为选中对象添加 EUUINodeBind 组件，自动匹配 UI 类型，并在 Hierarchy 显示绑定标识
    /// </summary>
    [InitializeOnLoad]
    public static class EUUINodeBindEditor
    {
        private static Type _tmpType;

        static EUUINodeBindEditor()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        /// <summary>
        /// 在 Hierarchy 面板展示已绑定节点的类型标识
        /// </summary>
        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj == null) return;

            var bind = obj.GetComponent<EUUINodeBind>();
            if (bind == null) return;

            Rect labelRect = new Rect(selectionRect.xMax - 95, selectionRect.y, 95, selectionRect.height);
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.4f, 0.8f, 1f) }
            };
            GUI.Label(labelRect, $"[{bind.ComponentType}]", style);
        }

        private static Type GetTMPTextType()
        {
            if (_tmpType != null) return _tmpType;
            _tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            return _tmpType;
        }

        /// <summary>
        /// 根据 GameObject 已有组件自动匹配 EUUINodeBindType（优先级：Button → Image → Text → TMP → RectTransform）
        /// </summary>
        public static EUUINodeBindType DetectComponentType(GameObject go)
        {
            if (go.GetComponent<Button>() != null) return EUUINodeBindType.Button;
            if (go.GetComponent<Image>() != null) return EUUINodeBindType.Image;
            if (go.GetComponent<Text>() != null) return EUUINodeBindType.Text;
            var tmpType = GetTMPTextType();
            if (tmpType != null && go.GetComponent(tmpType) != null) return EUUINodeBindType.TextMeshProUGUI;
            return EUUINodeBindType.RectTransform;
        }

        /// <summary>
        /// 批量为选中的对象添加 EUUINodeBind 脚本，并自动匹配 UI 类型
        /// </summary>
        // [MenuItem("EUFramework/拓展/EUUI/绑定 NodeBind &b", false, 104)]
        public static void AddBindComponent()
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go.GetComponent<EUUINodeBind>() != null) continue;

                var bind = Undo.AddComponent<EUUINodeBind>(go);
                EUUINodeBindType detected = DetectComponentType(go);
                if (bind.ComponentType != detected)
                {
                    Undo.RecordObject(bind, "EUUI NodeBind Auto Type");
                    bind.ComponentType = detected;
                }
            }
        }
    }
}
#endif
