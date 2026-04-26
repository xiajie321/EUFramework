#if UNITY_EDITOR
using UnityEditor;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// Keeps EUUI_EXTENSIONS_GENERATED in sync when UIKit generated files are changed manually.
    /// </summary>
    internal sealed class EUUIGeneratedFileWatcher : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!TouchesUIKitGeneratedFiles(importedAssets)
                && !TouchesUIKitGeneratedFiles(deletedAssets)
                && !TouchesUIKitGeneratedFiles(movedAssets)
                && !TouchesUIKitGeneratedFiles(movedFromAssetPaths))
            {
                return;
            }

            EUUIAsmdefHelper.SyncExtensionsGeneratedDefine();
            EUUIAsmdefHelper.RecalculateFromGeneratedFiles();
        }

        private static bool TouchesUIKitGeneratedFiles(string[] paths)
        {
            if (paths == null) return false;

            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                string normalized = path.Replace("\\", "/");
                if (normalized.Contains("/Script/Kit/Generate/UIKit/")
                    && normalized.EndsWith(".Generated.cs", System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
