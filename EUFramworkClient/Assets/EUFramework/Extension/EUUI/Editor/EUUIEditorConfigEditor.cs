#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using EUFramework.Extension.EUUI;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// EUUIEditorConfig 编辑器扩展：创建/打开配置
    /// </summary>
    public static class EUUIEditorConfigEditor
    {
        private const string DefaultFilename = "EUUIEditorConfig.asset";

        /// <summary>
        /// 动态解析 Editor 目录路径（基于当前脚本位置）
        /// </summary>
        private static string GetDefaultPath()
        {
            var scriptGuids = AssetDatabase.FindAssets("EUUIEditorConfigEditor t:MonoScript");
            if (scriptGuids != null && scriptGuids.Length > 0)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuids[0]);
                return Path.GetDirectoryName(scriptPath).Replace("\\", "/");
            }
            return "Assets/EUFramework/Extension/EUUI/Editor"; // 兜底路径
        }

        /// <summary>
        /// 动态解析 Resources 目录路径（基于脚本所在 Extension/EUUI 下的 Resources）
        /// </summary>
        internal static string GetResourcesPath()
        {
            var scriptGuids = AssetDatabase.FindAssets("EUUIEditorConfigEditor t:MonoScript");
            if (scriptGuids != null && scriptGuids.Length > 0)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuids[0]);
                string editorDir = Path.GetDirectoryName(scriptPath).Replace("\\", "/");
                string euuiDir = Path.GetDirectoryName(editorDir).Replace("\\", "/");
                return Path.Combine(euuiDir, "Resources").Replace("\\", "/");
            }
            return "Assets/EUFramework/Extension/EUUI/Resources"; // 兜底路径
        }

        // [MenuItem("EUFramework/拓展/EUUI/创建 UI 配置", false, 100)]
        public static void CreateConfig()
        {
            string defaultPath = GetDefaultPath();
            if (!Directory.Exists(defaultPath))
            {
                Directory.CreateDirectory(defaultPath);
                AssetDatabase.Refresh();
            }

            string fullPath = Path.Combine(defaultPath, DefaultFilename).Replace("\\", "/");

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

        // [MenuItem("EUFramework/拓展/EUUI/打开 UI 配置", false, 101)]
        public static void OpenConfig()
        {
            string defaultPath = GetDefaultPath();
            string fullPath = Path.Combine(defaultPath, DefaultFilename).Replace("\\", "/");
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

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("运行时配置同步：将上述配置同步到 EUUIKitConfig（运行时使用）", MessageType.Info);

            if (GUILayout.Button("同步到 EUUIKitConfig", GUILayout.Height(30)))
            {
                var editorConfig = target as EUUIEditorConfig;
                if (editorConfig != null)
                    EUUIEditorConfigEditorSync.SyncEditorConfigToKitConfig(editorConfig);
            }
        }
    }

    /// <summary>
    /// 供 EUUI 配置工具等调用的静态同步方法
    /// </summary>
    public static class EUUIEditorConfigEditorSync
    {
        /// <summary>
        /// 将 EUUIEditorConfig 同步到 EUUIKitConfig（运行时配置）
        /// </summary>
        public static void SyncEditorConfigToKitConfig(EUUIEditorConfig editorConfig)
        {
            if (editorConfig == null) return;

            string resourcesPath = EUUIEditorConfigEditor.GetResourcesPath();
            if (!Directory.Exists(resourcesPath))
            {
                Directory.CreateDirectory(resourcesPath);
                AssetDatabase.Refresh();
            }

            string kitConfigPath = Path.Combine(resourcesPath, "EUUIKitConfig.asset").Replace("\\", "/");
            var kitConfig = AssetDatabase.LoadAssetAtPath<EUUIKitConfig>(kitConfigPath);

            if (kitConfig == null)
            {
                kitConfig = ScriptableObject.CreateInstance<EUUIKitConfig>();
                AssetDatabase.CreateAsset(kitConfig, kitConfigPath);
                Debug.Log($"[EUUI] 创建运行时配置: {kitConfigPath}");
            }

            kitConfig.referenceResolution = editorConfig.referenceResolution;
            kitConfig.matchWidthOrHeight = editorConfig.matchWidthOrHeight;
            kitConfig.referencePixelsPerUnit = editorConfig.referencePixelsPerUnit;
            kitConfig.builtinPrefabPath = editorConfig.uiPrefabBuiltinPath;
            kitConfig.remotePrefabPath = editorConfig.uiPrefabRemotePath;
            kitConfig.builtinAtlasPath = editorConfig.atlasBuiltinPath;
            kitConfig.remoteAtlasPath = editorConfig.atlasRemotePath;

            EditorUtility.SetDirty(kitConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[EUUI] 配置已同步到 EUUIKitConfig");
        }
    }
}
#endif
