#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using EUFramework.Extension.EUUI;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// Synchronizes editor-only EUUI settings into the runtime EUUIKitConfig asset.
    /// </summary>
    public static class EUUIEditorConfigEditorSync
    {
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

            EditorUtility.SetDirty(kitConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[EUUI] 配置已同步到 EUUIKitConfig");
        }
    }
}
#endif
