#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using EUFramework.Extension.EUUI;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// 创建 UI 场景时的输入窗口（面板名 + EUUIPanelDescription）。
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
}
#endif
