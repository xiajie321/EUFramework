using System.IO;
using UnityEditor;
using UnityEngine;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// 模板目录迁移工具（一次性使用）
    /// 将旧的目录结构迁移到新的按生成类型分类的结构
    /// </summary>
    public static class EUUITemplateMigrationTool
    {
        public static void MigrateTemplateStructure()
        {
            if (!EditorUtility.DisplayDialog(
                "迁移模板目录",
                "将模板目录从旧结构迁移到新结构（按生成类型分类）\n\n" +
                "旧结构: Core/, Architecture/, ResourceLoader/\n" +
                "新结构: Panel/, Extensions/\n\n" +
                "此操作会移动文件，是否继续？",
                "确定", "取消"))
            {
                return;
            }

            try
            {
                string templatesDir = EUUITemplateManager.GetTemplatesDirectory();
                string baseDir = Path.GetDirectoryName(Application.dataPath);
                string fullTemplatesPath = Path.Combine(baseDir, templatesDir).Replace("\\", "/");

                // 1. 创建新目录结构
                CreateNewDirectoryStructure(templatesDir);

                // 2. 移动文件
                MoveTemplateFiles(templatesDir);

                // 3. 删除旧目录
                CleanupOldDirectories(templatesDir);

                // 4. 刷新资产数据库
                AssetDatabase.Refresh();

                // 5. 重新生成注册表
                EUUITemplateRegistryGenerator.RefreshRegistry();

                EditorUtility.DisplayDialog(
                    "迁移完成",
                    "模板目录结构已成功迁移！\n\n" +
                    "新结构:\n" +
                    "- Panel/ (数据驱动模板)\n" +
                    "- Extensions/ (自包含扩展)\n\n" +
                    "模板注册表已自动刷新。",
                    "确定");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EUUI] 迁移失败: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("迁移失败", $"迁移过程中出错:\n{e.Message}", "确定");
            }
        }

        private static void CreateNewDirectoryStructure(string templatesDir)
        {
            Debug.Log("[EUUI] 创建新目录结构...");

            // Panel/ - 数据驱动模板
            CreateDirectory(Path.Combine(templatesDir, "Panel"));

            // Extensions/ - 自包含扩展
            CreateDirectory(Path.Combine(templatesDir, "Extensions"));
            CreateDirectory(Path.Combine(templatesDir, "Extensions/PanelBase"));
            CreateDirectory(Path.Combine(templatesDir, "Extensions/UIKit"));
            CreateDirectory(Path.Combine(templatesDir, "Extensions/Architecture"));
        }

        private static void MoveTemplateFiles(string templatesDir)
        {
            Debug.Log("[EUUI] 移动模板文件...");

            // Panel.Generated: Core/ -> Panel/
            MoveFile(
                Path.Combine(templatesDir, "Core/EUUIPanel.Generated.sbn"),
                Path.Combine(templatesDir, "Panel/EUUIPanel.Generated.sbn"));

            // PanelBase.EURes: ResourceLoader/ -> Extensions/PanelBase/
            MoveFile(
                Path.Combine(templatesDir, "ResourceLoader/EUUIPanelBase.EURes.sbn"),
                Path.Combine(templatesDir, "Extensions/PanelBase/EURes.sbn"));

            // UIKit.EURes: ResourceLoader/ -> Extensions/UIKit/
            MoveFile(
                Path.Combine(templatesDir, "ResourceLoader/EUUIKit.EURes.sbn"),
                Path.Combine(templatesDir, "Extensions/UIKit/EURes.sbn"));

            // UIKit.Custom: ResourceLoader/ -> Extensions/UIKit/
            MoveFile(
                Path.Combine(templatesDir, "ResourceLoader/EUUIKit.Custom.sbn"),
                Path.Combine(templatesDir, "Extensions/UIKit/Custom.sbn"));

            // MVC: Architecture/ -> Extensions/Architecture/
            MoveFile(
                Path.Combine(templatesDir, "Architecture/EUUI.MVC.sbn"),
                Path.Combine(templatesDir, "Extensions/Architecture/MVC.sbn"));

            // Samples: Samples/ -> Extensions/UIKit/
            MoveFile(
                Path.Combine(templatesDir, "Samples/EUUIKit.Addressables.sbn"),
                Path.Combine(templatesDir, "Extensions/UIKit/Addressables.sbn"));

            MoveFile(
                Path.Combine(templatesDir, "Samples/EUUIKit.Resources.sbn"),
                Path.Combine(templatesDir, "Extensions/UIKit/Resources.sbn"));
        }

        private static void CleanupOldDirectories(string templatesDir)
        {
            Debug.Log("[EUUI] 清理旧目录...");

            // 删除旧目录（如果为空）
            DeleteDirectoryIfEmpty(Path.Combine(templatesDir, "Core"));
            DeleteDirectoryIfEmpty(Path.Combine(templatesDir, "Architecture"));
            DeleteDirectoryIfEmpty(Path.Combine(templatesDir, "ResourceLoader"));
            DeleteDirectoryIfEmpty(Path.Combine(templatesDir, "Samples"));
        }

        private static void CreateDirectory(string path)
        {
            string fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), path);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                Debug.Log($"[EUUI] 创建目录: {path}");
            }
        }

        private static void MoveFile(string oldPath, string newPath)
        {
            if (File.Exists(Path.Combine(Path.GetDirectoryName(Application.dataPath), oldPath)))
            {
                string error = AssetDatabase.MoveAsset(oldPath, newPath);
                if (string.IsNullOrEmpty(error))
                {
                    Debug.Log($"[EUUI] 移动: {Path.GetFileName(oldPath)} -> {newPath}");
                }
                else
                {
                    Debug.LogWarning($"[EUUI] 移动失败: {oldPath} -> {newPath}\n错误: {error}");
                }
            }
            else
            {
                Debug.LogWarning($"[EUUI] 文件不存在，跳过: {oldPath}");
            }
        }

        private static void DeleteDirectoryIfEmpty(string path)
        {
            string fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), path);
            if (Directory.Exists(fullPath))
            {
                var files = Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories);
                // 过滤掉 .meta 文件
                var nonMetaFiles = System.Array.FindAll(files, f => !f.EndsWith(".meta"));
                
                if (nonMetaFiles.Length == 0)
                {
                    AssetDatabase.DeleteAsset(path);
                    Debug.Log($"[EUUI] 删除空目录: {path}");
                }
                else
                {
                    Debug.LogWarning($"[EUUI] 目录不为空，保留: {path}");
                }
            }
        }
    }
}
