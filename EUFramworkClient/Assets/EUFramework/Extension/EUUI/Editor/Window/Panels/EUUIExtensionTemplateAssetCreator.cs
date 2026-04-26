#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// File-system side of creating Static .sbn extension templates from the Extensions panel.
    /// </summary>
    internal static class EUUIExtensionTemplateAssetCreator
    {
        public static string GetExtensionTargetDirectory(EUUIExtensionTemplateCreator.ExtensionType type)
        {
            string editorDir = EUUITemplateLocator.GetEditorDirectory();
            string sub = type == EUUIExtensionTemplateCreator.ExtensionType.PanelExtension
                ? "PanelBase"
                : "UIKit";
            return string.IsNullOrEmpty(editorDir)
                ? $"Assets/EUFramework/Extension/EUUI/Editor/Templates/Sbn/Static/{sub}"
                : $"{editorDir}/Templates/Sbn/Static/{sub}";
        }

        public static string GetExtensionFileName(
            EUUIExtensionTemplateCreator.ExtensionType extensionType,
            string extensionName)
        {
            return extensionType == EUUIExtensionTemplateCreator.ExtensionType.PanelExtension
                ? $"EUUIPanelBase.{extensionName}.sbn"
                : $"EUUIKit.{extensionName}.sbn";
        }

        public static bool IsValidExtensionName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            foreach (char c in name)
                if (!char.IsLetterOrDigit(c) && c != '_') return false;
            return char.IsLetter(name[0]);
        }

        public static bool CreateTemplateAsset(
            EUUIExtensionTemplateCreator.ExtensionType extensionType,
            EUUIExtensionTemplateCreator.TemplatePreset templatePreset,
            string extensionName,
            string requiredAssemblies)
        {
            try
            {
                string targetDir = GetExtensionTargetDirectory(extensionType);
                string fileName = GetExtensionFileName(extensionType, extensionName);
                string assetPath = $"{targetDir}/{fileName}";
                string fullPath = Path.GetFullPath(
                    Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath));

                if (File.Exists(fullPath))
                {
                    EditorUtility.DisplayDialog("文件已存在",
                        $"模板文件已存在：\n{assetPath}\n\n请直接编辑该文件。", "确定");
                    return false;
                }

                string dirFull = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dirFull))
                    Directory.CreateDirectory(dirFull);

                string content = EUUIExtensionTemplateCreator.GenerateTemplateContent(
                    extensionType, templatePreset, extensionName);

                File.WriteAllText(fullPath, content, System.Text.Encoding.UTF8);

                string sidecarNote = CreateSidecarJson(fullPath, requiredAssemblies);

                AssetDatabase.Refresh();

                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (asset != null)
                {
                    EditorGUIUtility.PingObject(asset);
                    Selection.activeObject = asset;
                }

                EditorUtility.DisplayDialog("创建成功",
                    $"扩展模板已创建：\n{assetPath}\n\n" +
                    "模板注册表将自动更新，请在模板中实现 TODO 标记的部分。"
                    + sidecarNote, "确定");

                Debug.Log($"[EUUI] 扩展模板已创建: {assetPath}");
                return true;
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("创建失败", $"创建扩展模板失败：\n{e.Message}", "确定");
                Debug.LogError($"[EUUI] 创建扩展模板失败: {e}");
                return false;
            }
        }

        private static string CreateSidecarJson(string templateFullPath, string requiredAssemblies)
        {
            string[] parsedAssemblies = ParseAssembliesInput(requiredAssemblies);
            if (parsedAssemblies.Length == 0)
                return "";

            string jsonContent = "{\n    \"requiredAssemblies\": ["
                + string.Join(", ", parsedAssemblies.Select(a => $"\"{a}\""))
                + "]\n}\n";
            string jsonPath = Path.ChangeExtension(templateFullPath, ".json");
            File.WriteAllText(jsonPath, jsonContent, System.Text.Encoding.UTF8);

            Debug.Log($"[EUUI] 伴生配置已创建: {jsonPath}");
            return $"\n已生成伴生配置：{Path.GetFileName(jsonPath)}\n程序集：{string.Join(", ", parsedAssemblies)}";
        }

        private static string[] ParseAssembliesInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Array.Empty<string>();
            return input
                .Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
#endif
