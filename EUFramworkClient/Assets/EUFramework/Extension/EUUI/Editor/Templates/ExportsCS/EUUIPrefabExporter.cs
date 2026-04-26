#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using EUFramework.Extension.EUUI;

namespace EUFramework.Extension.EUUI.Editor.Templates
{
    /// <summary>
    /// Saves exported UI roots as prefabs and strips editor-only bind markers.
    /// </summary>
    internal static class EUUIPrefabExporter
    {
        public static bool FinalizePrefab(GameObject exportRoot, string panelName, EUUIEditorConfig config)
        {
            var desc = exportRoot.GetComponentInParent<EUUIPanelDescription>() ?? UnityEngine.Object.FindFirstObjectByType<EUUIPanelDescription>();
            var pkgType = desc != null ? desc.PackageType : EUUIPackageType.Remote;
            string folderPath = config.GetUIPrefabDir(pkgType);
            EnsureDirectory(folderPath);
            string prefabPath = $"{folderPath}/{panelName}.prefab".Replace("\\", "/");

            GameObject savedRoot = PrefabUtility.SaveAsPrefabAsset(exportRoot, prefabPath, out bool saveSuccess);
            if (!saveSuccess || savedRoot == null)
            {
                Debug.LogError($"[EUUI] Prefab 保存失败（路径: {prefabPath}）。Unity 可能仍在导入新生成脚本，或 UIRoot 上存在不可保存组件。请等待编译完成后重试导出。");
                return false;
            }

            GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
            var nodes = prefabContents.GetComponentsInChildren<EUUINodeBind>(true);
            for (int i = nodes.Length - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(nodes[i]);
            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath, out bool cleanupSuccess);
            PrefabUtility.UnloadPrefabContents(prefabContents);

            if (!cleanupSuccess)
            {
                Debug.LogWarning($"[EUUI] Prefab 已保存，但清理 EUUINodeBind 后的二次保存失败: {prefabPath}");
                return false;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[EUUI] Prefab 已导出: {prefabPath}");
            return true;
        }

        private static void EnsureDirectory(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            string fullPath = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), path));
            if (!System.IO.Directory.Exists(fullPath))
            {
                System.IO.Directory.CreateDirectory(fullPath);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
