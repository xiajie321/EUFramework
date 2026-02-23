using UnityEditor;
using UnityEngine;

namespace Framework.Editor
{
    /// <summary>
    /// 编辑器启动时自动检查并创建 OSAListViewConfig
    /// </summary>
    [InitializeOnLoad]
    public static class OSAListViewConfigInitializer
    {
        static OSAListViewConfigInitializer()
        {
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            var guids = AssetDatabase.FindAssets("t:OSAListViewConfig");
            if (guids.Length > 0) return;

            OSAListViewConfig.GetOrCreate();
        }
    }
}
