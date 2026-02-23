using System.IO;
using UnityEditor;
using UnityEngine;
using EUFramework.Extension.EUUI.Editor.Templates;

namespace Framework.Editor
{
    /// <summary>
    /// OSA ListView 代码生成路径配置
    /// 逻辑代码根目录复用 EUUITemplateConfig.uiLogicScriptsPath，此处仅维护 OSA 专属路径
    /// </summary>
    [CreateAssetMenu(
        fileName = "OSAListViewConfig",
        menuName = "EUFramework/OSAExtension/ListView Config")]
    public class OSAListViewConfig : ScriptableObject
    {
        [Header("代码生成路径")]
        [Tooltip("ViewsHolder.Generated.cs 输出目录")]
        public string holderGeneratedOutputPath = "Assets/Script/Generate/ListHolder";

        [Tooltip("逻辑代码在 PackageName 下的子目录名")]
        public string listViewSubFolder = "Comp";

        /// <summary>
        /// 逻辑代码根路径：优先读取 EUUITemplateConfig.uiLogicScriptsPath，保持单一数据源
        /// </summary>
        public static string GetLogicScriptsRoot()
        {
            var euuiConfig = EUUIPanelExporter.GetConfig();
            if (euuiConfig != null && !string.IsNullOrEmpty(euuiConfig.uiLogicScriptsPath))
                return euuiConfig.uiLogicScriptsPath;
            return "Assets/Script/Game/UI";
        }

        /// <summary>
        /// 获取现有的 SO，若不存在则自动创建
        /// </summary>
        public static OSAListViewConfig GetOrCreate()
        {
            var guids = AssetDatabase.FindAssets("t:OSAListViewConfig");
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<OSAListViewConfig>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));

            return CreateDefault();
        }

        private static OSAListViewConfig CreateDefault()
        {
            var scriptGuids = AssetDatabase.FindAssets("OSAListViewConfig t:MonoScript");
            string dir;
            if (scriptGuids.Length > 0)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuids[0]);
                dir = Path.GetDirectoryName(scriptPath)?.Replace("\\", "/");
            }
            else
            {
                dir = "Assets/EUFramework/Extension/EUUI/Extension/OSAExtension/Editor/EditorSO";
            }

            string fullDir = Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(Application.dataPath), dir));
            if (!Directory.Exists(fullDir))
                Directory.CreateDirectory(fullDir);

            AssetDatabase.Refresh();

            var config = CreateInstance<OSAListViewConfig>();
            string assetPath = $"{dir}/OSAListViewConfig.asset";
            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[OSAExtension] OSAListViewConfig 已自动创建于 {assetPath}");
            return config;
        }
    }
}
