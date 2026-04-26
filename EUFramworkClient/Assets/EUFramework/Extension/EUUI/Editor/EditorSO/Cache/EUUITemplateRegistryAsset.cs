using System;
using System.Collections.Generic;
using UnityEngine;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// EUUI 模板信息
    /// </summary>
    [Serializable]
    public class EUUITemplateInfo
    {
        [Tooltip("模板唯一标识符")]
        public string id;

        [Tooltip("相对于 Templates/Sbn/ 的路径（不含 .sbn 扩展名）")]
        public string path;
    }

    /// <summary>
    /// EUUI 模板注册表 ScriptableObject
    /// 自动从 Templates/Sbn/ 目录扫描生成，记录所有可用 .sbn 模板的 ID 与路径
    /// </summary>
    [CreateAssetMenu(fileName = "EUUITemplateRegistry", menuName = "EUFramework/EUUI/Template Registry", order = 1)]
    public class EUUITemplateRegistryAsset : ScriptableObject
    {
        [Tooltip("已注册的模板列表")]
        public List<EUUITemplateInfo> templates = new List<EUUITemplateInfo>();

        /// <summary>
        /// 获取指定 ID 对应的模板相对路径（相对于 Templates/Sbn/，不含 .sbn）
        /// </summary>
        public string GetTemplatePath(string id)
        {
            var template = templates.Find(t => t.id == id);
            return template?.path;
        }

        /// <summary>
        /// 检查指定 ID 的模板是否已注册
        /// </summary>
        public bool HasTemplate(string id)
        {
            return templates.Exists(t => t.id == id);
        }

        /// <summary>
        /// 通过任意格式的路径反向查找模板 ID
        /// 优先按 path 后缀精确匹配，其次按文件名兜底匹配
        /// </summary>
        public string FindIdByPath(string templatePath)
        {
            if (string.IsNullOrEmpty(templatePath)) return null;

            string normalized = templatePath.Replace("\\", "/");
            if (normalized.EndsWith(".sbn", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(0, normalized.Length - 4);

            foreach (var t in templates)
            {
                if (normalized.EndsWith(t.path, StringComparison.OrdinalIgnoreCase))
                    return t.id;
            }

            string fileName = System.IO.Path.GetFileName(normalized);
            foreach (var t in templates)
            {
                if (string.Equals(System.IO.Path.GetFileName(t.path), fileName, StringComparison.OrdinalIgnoreCase))
                    return t.id;
            }

            return null;
        }
    }
}
