using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// EUUI 模板信息
    /// </summary>
    [Serializable]
    public class EUUITemplateInfo
    {
        [Tooltip("模板唯一标识符（对应枚举值）")]
        public string id;
        
        [Tooltip("模板显示名称")]
        public string name;
        
        [Tooltip("模板分类（Core/Architecture/ResourceLoader）")]
        public string category;
        
        [Tooltip("相对于 Templates 目录的路径（不含 .sbn 扩展名）")]
        public string path;
        
        [Tooltip("模板描述")]
        public string description;
        
        [Tooltip("是否为必需模板")]
        public bool required;
    }

    /// <summary>
    /// EUUI 模板注册表 ScriptableObject
    /// 自动从 Templates 目录扫描生成，记录所有可用的模板
    /// </summary>
    [CreateAssetMenu(fileName = "EUUITemplateRegistry", menuName = "EUFramework/EUUI/Template Registry", order = 1)]
    public class EUUITemplateRegistryAsset : ScriptableObject
    {
        [Header("注册表信息")]
        [Tooltip("注册表版本")]
        public string version = "1.0.0";
        
        [Tooltip("最后更新时间")]
        public string lastUpdated;
        
        [Tooltip("Templates 目录路径")]
        public string templatesDirectory;

        [Header("已注册的模板")]
        [Tooltip("所有可用的模板列表")]
        public List<EUUITemplateInfo> templates = new List<EUUITemplateInfo>();

        /// <summary>
        /// 获取指定 ID 的模板路径
        /// </summary>
        public string GetTemplatePath(string id)
        {
            var template = templates.Find(t => t.id == id);
            return template?.path;
        }

        /// <summary>
        /// 检查模板是否存在
        /// </summary>
        public bool HasTemplate(string id)
        {
            return templates.Any(t => t.id == id);
        }

        /// <summary>
        /// 获取所有模板
        /// </summary>
        public IEnumerable<EUUITemplateInfo> GetAllTemplates()
        {
            return templates;
        }

        /// <summary>
        /// 获取指定分类的模板
        /// </summary>
        public IEnumerable<EUUITemplateInfo> GetTemplatesByCategory(string category)
        {
            return templates.Where(t => t.category == category);
        }

        /// <summary>
        /// 获取所有分类
        /// </summary>
        public IEnumerable<string> GetCategories()
        {
            return templates.Select(t => t.category).Distinct();
        }
    }
}
