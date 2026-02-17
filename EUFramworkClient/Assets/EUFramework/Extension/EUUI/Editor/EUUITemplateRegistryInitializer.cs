using UnityEditor;
using UnityEngine;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// 模板注册表初始化器
    /// 在编辑器启动时自动检查并生成模板注册表
    /// </summary>
    [InitializeOnLoad]
    public static class EUUITemplateRegistryInitializer
    {
        static EUUITemplateRegistryInitializer()
        {
            // 延迟到 Unity 完全加载后执行
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            // 检查是否需要生成注册表
            if (EUUITemplateRegistryGenerator.NeedsUpdate())
            {
                Debug.Log("[EUUI] 🔄 首次启动或模板已更新，正在生成模板注册表...");
                EUUITemplateRegistryGenerator.RefreshRegistry();
            }
        }
    }
}
