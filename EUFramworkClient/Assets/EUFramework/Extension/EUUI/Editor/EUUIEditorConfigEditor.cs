#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using Framework;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// EUUIEditorConfig 编辑器扩展：创建/打开配置
    /// </summary>
    public static class EUUIEditorConfigEditor
    {
        private const string DefaultPath = "Assets/EUFramework/Extension/EUUI/Editor";
        private const string DefaultFilename = "EUUIEditorConfig.asset";

        [MenuItem("EUFramework/拓展/EUUI/创建 UI 配置", false, 100)]
        public static void CreateConfig()
        {
            if (!Directory.Exists(DefaultPath))
            {
                Directory.CreateDirectory(DefaultPath);
                AssetDatabase.Refresh();
            }

            string fullPath = Path.Combine(DefaultPath, DefaultFilename).Replace("\\", "/");

            var existingAsset = AssetDatabase.LoadAssetAtPath<EUUIEditorConfig>(fullPath);
            if (existingAsset != null)
            {
                bool select = EditorUtility.DisplayDialog(
                    "配置已存在",
                    $"配置文件已存在于:\n{fullPath}\n\n是否选中现有配置？",
                    "选中现有配置",
                    "取消"
                );
                if (select)
                {
                    EditorGUIUtility.PingObject(existingAsset);
                    Selection.activeObject = existingAsset;
                }
                return;
            }

            var config = ScriptableObject.CreateInstance<EUUIEditorConfig>();
            AssetDatabase.CreateAsset(config, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(config);
            Selection.activeObject = config;
            Debug.Log($"[EUUI] EUUI 配置文件创建成功: {fullPath}");
        }

        [MenuItem("EUFramework/拓展/EUUI/打开 UI 配置", false, 101)]
        public static void OpenConfig()
        {
            string fullPath = Path.Combine(DefaultPath, DefaultFilename).Replace("\\", "/");
            var config = AssetDatabase.LoadAssetAtPath<EUUIEditorConfig>(fullPath);

            if (config != null)
            {
                EditorGUIUtility.PingObject(config);
                Selection.activeObject = config;
            }
            else
            {
                bool create = EditorUtility.DisplayDialog(
                    "配置不存在",
                    "EUUIEditorConfig 不存在，是否创建？",
                    "创建",
                    "取消"
                );
                if (create)
                    CreateConfig();
            }
        }
    }

    [CustomEditor(typeof(EUUIEditorConfig))]
    public class EUUIEditorConfigInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }
}
#endif
