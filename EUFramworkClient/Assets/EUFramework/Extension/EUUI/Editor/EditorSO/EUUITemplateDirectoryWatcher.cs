using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// 模板目录监听器
    /// 自动检测 Templates 目录中 .sbn 文件的变化并更新注册表
    /// </summary>
    public class EUUITemplateDirectoryWatcher : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool needRefresh = false;
            
            // 检查导入的资产
            foreach (var asset in importedAssets)
            {
                if (IsTemplateFile(asset))
                {
                    Debug.Log($"[EUUI] 检测到新模板文件: {asset}");
                    needRefresh = true;
                    break;
                }
            }
            
            // 检查删除的资产
            if (!needRefresh)
            {
                foreach (var asset in deletedAssets)
                {
                    if (IsTemplateFile(asset))
                    {
                        Debug.Log($"[EUUI] 检测到模板文件删除: {asset}");
                        needRefresh = true;
                        break;
                    }
                }
            }
            
            // 检查移动的资产
            if (!needRefresh)
            {
                foreach (var asset in movedAssets.Concat(movedFromAssetPaths))
                {
                    if (IsTemplateFile(asset))
                    {
                        Debug.Log($"[EUUI] 检测到模板文件移动: {asset}");
                        needRefresh = true;
                        break;
                    }
                }
            }
            
            // 如果检测到变化，刷新注册表
            if (needRefresh)
            {
                // 延迟执行，避免在资产导入过程中刷新
                EditorApplication.delayCall += () =>
                {
                    if (EUUITemplateRegistryGenerator.NeedsUpdate())
                    {
                        Debug.Log("[EUUI] 🔄 模板文件已变化，自动刷新注册表...");
                        EUUITemplateRegistryGenerator.RefreshRegistry();
                    }
                };
            }
        }

        /// <summary>
        /// 判断是否为模板文件
        /// </summary>
        private static bool IsTemplateFile(string assetPath)
        {
            // 检查是否在 Templates 目录下且是 .sbn 文件
            return assetPath.Contains("/Templates/") && assetPath.EndsWith(".sbn");
        }
    }
}
