#if UNITY_EDITOR
using System.IO;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// Centralized EditorSO asset paths.
    /// Config = user-maintained settings, Cache = generated tool data, Workspace = editor workflow layout.
    /// </summary>
    internal static class EUUIEditorSOPaths
    {
        internal static string GetRootDirectory()
        {
            string editorDir = EUUITemplateLocator.GetEditorDirectory();
            return string.IsNullOrEmpty(editorDir)
                ? "Assets/EUFramework/Extension/EUUI/Editor/EditorSO"
                : Path.Combine(editorDir, "EditorSO").Replace("\\", "/");
        }

        internal static string GetConfigDirectory() => Path.Combine(GetRootDirectory(), "Config").Replace("\\", "/");
        internal static string GetCacheDirectory() => Path.Combine(GetRootDirectory(), "Cache").Replace("\\", "/");
        internal static string GetWorkspaceDirectory() => Path.Combine(GetRootDirectory(), "Workspace").Replace("\\", "/");

        internal static string EditorConfigAssetPath => Path.Combine(GetConfigDirectory(), "EUUIEditorConfig.asset").Replace("\\", "/");
        internal static string TemplateConfigAssetPath => Path.Combine(GetConfigDirectory(), "EUUITemplateConfig.asset").Replace("\\", "/");
        internal static string TemplateRegistryAssetPath => Path.Combine(GetCacheDirectory(), "EUUITemplateRegistry.asset").Replace("\\", "/");
        internal static string HotboxConfigAssetPath => Path.Combine(GetWorkspaceDirectory(), "EUHotboxConfig.asset").Replace("\\", "/");
    }
}
#endif
