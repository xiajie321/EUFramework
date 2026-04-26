#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// Loads or creates the Hotbox workspace configuration asset.
    /// </summary>
    internal static class EUHotboxConfigProvider
    {
        internal static EUHotboxConfigSO GetOrCreateConfig()
        {
            string assetPath = EUUIEditorSOPaths.HotboxConfigAssetPath;
            var so = AssetDatabase.LoadAssetAtPath<EUHotboxConfigSO>(assetPath);
            if (so != null) return so;

            var guids = AssetDatabase.FindAssets("t:EUHotboxConfigSO");
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<EUHotboxConfigSO>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));

            string dir = Path.GetDirectoryName(assetPath);
            EUUIAsmdefHelper.EnsureDirectory(dir);
            var newSO = ScriptableObject.CreateInstance<EUHotboxConfigSO>();
            AssetDatabase.CreateAsset(newSO, assetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[EUUI] 已自动创建功能编排配置: {assetPath}");
            return newSO;
        }
    }
}
#endif
