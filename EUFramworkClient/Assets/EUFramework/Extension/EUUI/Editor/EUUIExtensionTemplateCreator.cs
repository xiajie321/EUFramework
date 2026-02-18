using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using EUFramework.Extension.EUUI.Editor.Templates;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// EUUI 扩展模板创建向导
    /// </summary>
    public class EUUIExtensionTemplateCreator : EditorWindow
    {
        /// <summary>
        /// 扩展类型（决定目标目录与文件名前缀）
        /// </summary>
        public enum ExtensionType
        {
            PanelExtension,    // 面板扩展（EUUIPanelBase） → Static/PanelBase/
            KitExtension       // UIKit 功能扩展            → Static/UIKit/
        }

        /// <summary>
        /// 模板预设
        /// </summary>
        public enum TemplatePreset
        {
            Empty,             // 空模板（仅基础结构）
            ResourceLoader,    // 资源加载器（包含加载/释放方法）
            StaticExtension,   // 静态扩展方法
        }

        private ExtensionType extensionType = ExtensionType.KitExtension;
        private TemplatePreset templatePreset = TemplatePreset.ResourceLoader;
        private string extensionName = "";
        private string savePath = "Assets/Script/Game/UI/Extensions";
        private bool autoAddToConfig = true;
        private Vector2 scrollPos;

        // 该窗口已集成到 EUUIEditorWindow 中的"扩展开发"选项卡
        // 如需使用，请打开 EUFramework > EUUI 配置工具

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("创建新扩展模板", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // 扩展类型
            EditorGUILayout.LabelField("扩展类型", EditorStyles.boldLabel);
            extensionType = (ExtensionType)EditorGUILayout.EnumPopup(extensionType);
            
            string typeHint = extensionType switch
            {
                ExtensionType.PanelExtension => "为 EUUIPanelBase 添加静态扩展方法（如 OSA、DoTween）→ Static/PanelBase/",
                ExtensionType.KitExtension   => "为 EUUIKit 添加功能扩展（如资源加载、分析统计）→ Static/UIKit/",
                _ => ""
            };
            EditorGUILayout.HelpBox(typeHint, MessageType.Info);
            EditorGUILayout.Space(10);

            // 扩展名称
            EditorGUILayout.LabelField("扩展名称", EditorStyles.boldLabel);
            extensionName = EditorGUILayout.TextField(extensionName);
            if (string.IsNullOrEmpty(extensionName))
            {
                EditorGUILayout.HelpBox("请输入扩展名称（如：MyLoader、OSA、DoTween）", MessageType.Warning);
            }
            else if (!IsValidExtensionName(extensionName))
            {
                EditorGUILayout.HelpBox("名称只能包含字母、数字和下划线", MessageType.Error);
            }
            EditorGUILayout.Space(10);

            // 模板预设
            EditorGUILayout.LabelField("模板预设", EditorStyles.boldLabel);
            templatePreset = (TemplatePreset)EditorGUILayout.EnumPopup(templatePreset);
            
            string presetHint = templatePreset switch
            {
                TemplatePreset.Empty           => "仅包含基础结构和 TODO 注释",
                TemplatePreset.ResourceLoader  => "包含完整的资源加载/释放方法框架",
                TemplatePreset.StaticExtension => "包含静态扩展方法示例",
                _ => ""
            };
            EditorGUILayout.HelpBox(presetHint, MessageType.Info);
            EditorGUILayout.Space(10);

            // 保存位置
            EditorGUILayout.LabelField("保存位置", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            savePath = EditorGUILayout.TextField(savePath);
            if (GUILayout.Button("浏览...", GUILayout.Width(80)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("选择保存位置", savePath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // 转换为相对路径
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        savePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (!Directory.Exists(savePath))
            {
                EditorGUILayout.HelpBox($"目录不存在，将自动创建：{savePath}", MessageType.Info);
            }
            EditorGUILayout.Space(10);

            // 自动添加到配置
            autoAddToConfig = EditorGUILayout.Toggle("自动添加到配置", autoAddToConfig);
            if (autoAddToConfig)
            {
                EditorGUILayout.HelpBox("将自动添加到 EUUIEditorConfig 的 manualExtensions 列表", MessageType.Info);
            }
            EditorGUILayout.Space(15);

            // 预览生成的文件名
            if (!string.IsNullOrEmpty(extensionName) && IsValidExtensionName(extensionName))
            {
                string fileName = GetFileName();
                EditorGUILayout.LabelField("将生成文件", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(Path.Combine(savePath, fileName), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUILayout.Space(10);
            }

            // 创建按钮
            GUI.enabled = CanCreate();
            if (GUILayout.Button("创建模板", GUILayout.Height(40)))
            {
                CreateTemplate();
            }
            GUI.enabled = true;

            EditorGUILayout.Space(10);
            EditorGUILayout.EndScrollView();
        }

        private bool CanCreate()
        {
            return !string.IsNullOrEmpty(extensionName) && IsValidExtensionName(extensionName);
        }

        private bool IsValidExtensionName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            
            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }
            
            return char.IsLetter(name[0]); // 必须以字母开头
        }

        private string GetFileName()
        {
            return extensionType == ExtensionType.PanelExtension
                ? $"EUUIPanelBase.{extensionName}.sbn"
                : $"EUUIKit.{extensionName}.sbn";
        }

        private void CreateTemplate()
        {
            try
            {
                // 1. 确保目录存在
                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }

                // 2. 生成文件路径
                string fileName = GetFileName();
                string fullPath = Path.Combine(savePath, fileName);

                // 3. 检查文件是否已存在
                if (File.Exists(fullPath))
                {
                    if (!EditorUtility.DisplayDialog("文件已存在", 
                        $"文件 {fileName} 已存在，是否覆盖？", 
                        "覆盖", "取消"))
                    {
                        return;
                    }
                }

                // 4. 生成模板内容
                string templateContent = GenerateTemplateContent(extensionType, templatePreset, extensionName);

                // 5. 写入文件
                File.WriteAllText(fullPath, templateContent, System.Text.Encoding.UTF8);
                AssetDatabase.Refresh();

                // 6. 自动添加到配置
                if (autoAddToConfig)
                {
                    AddToConfig(fullPath);
                }

                // 7. 在 Project 窗口中高亮显示
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fullPath);
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;

                // 8. 显示成功消息
                EditorUtility.DisplayDialog("创建成功", 
                    $"扩展模板已创建：\n{fullPath}\n\n" +
                    (autoAddToConfig ? "已添加到配置列表。\n" : "") +
                    "请在模板中实现 TODO 标记的部分。", 
                    "确定");

                Debug.Log($"[EUUI] 扩展模板已创建: {fullPath}");
                
                // 关闭窗口
                Close();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("创建失败", 
                    $"创建扩展模板失败：\n{e.Message}", 
                    "确定");
                Debug.LogError($"[EUUI] 创建扩展模板失败: {e}");
            }
        }

        private void AddToConfig(string templatePath)
        {
            try
            {
                var config = EUUIPanelExporter.GetConfig();
                if (config == null)
                {
                    Debug.LogWarning("[EUUI] 未找到配置文件，无法自动添加到配置");
                    return;
                }

                if (config.manualExtensions == null)
                {
                    config.manualExtensions = new System.Collections.Generic.List<EUUIAdditionalExtension>();
                }

                var ext = new EUUIAdditionalExtension
                {
                    templatePath = templatePath,
                    enabled      = false
                };

                config.manualExtensions.Add(ext);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();

                Debug.Log($"[EUUI] 扩展已添加到配置: {extensionName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUUI] 添加到配置失败: {e.Message}");
            }
        }

        // ========== 模板内容生成方法 ==========
        
        /// <summary>
        /// 生成模板内容（静态方法，供外部调用）
        /// </summary>
        public static string GenerateTemplateContent(ExtensionType extensionType, TemplatePreset templatePreset, string extensionName)
        {
            string content = templatePreset switch
            {
                TemplatePreset.ResourceLoader  => GetResourceLoaderTemplate(extensionType),
                TemplatePreset.StaticExtension => GetStaticExtensionTemplate(extensionType),
                _                              => GetEmptyTemplate(extensionType)
            };

            // 替换占位符
            content = content
                .Replace("{{extension_name}}", extensionName)
                .Replace("{{creation_time}}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                .Replace("{{extension_type}}", extensionType.ToString());

            return content;
        }
        
        private string GenerateTemplateContent()
        {
            return GenerateTemplateContent(extensionType, templatePreset, extensionName);
        }

        private static string GetEmptyTemplate(ExtensionType extensionType)
        {
            if (extensionType == ExtensionType.PanelExtension)
            {
                return @"//------------------------------------------------------------------------------
// <auto-generated>
//     扩展名称：{{extension_name}}
//     创建时间：{{creation_time}}
//     扩展类型：{{extension_type}}
// </auto-generated>
//------------------------------------------------------------------------------
#define EUUI_EXTENSIONS_GENERATED

using UnityEngine;

namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// EUUIPanelBase 的 {{extension_name}} 扩展
    /// TODO: 在此描述扩展功能
    /// </summary>
    public static class EUUIPanelBase{{extension_name}}Extensions
    {
        /// <summary>
        /// TODO: 添加扩展方法
        /// </summary>
        public static void DoSomething<T>(this EUUIPanelBase<T> panel)
            where T : EUUIPanelBase<T>, new()
        {
            // TODO: 实现扩展功能
        }
    }
}
";
            }
            else
            {
                return @"//------------------------------------------------------------------------------
// <auto-generated>
//     扩展名称：{{extension_name}}
//     创建时间：{{creation_time}}
//     扩展类型：{{extension_type}}
// </auto-generated>
//------------------------------------------------------------------------------
#define EUUI_EXTENSIONS_GENERATED

using UnityEngine;

namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// EUUIKit 的 {{extension_name}} 扩展
    /// TODO: 在此描述扩展功能
    /// </summary>
    public static partial class EUUIKit
    {
        /// <summary>
        /// TODO: 添加扩展方法
        /// </summary>
        public static void {{extension_name}}_DoSomething()
        {
            // TODO: 实现扩展功能
        }
    }
}
";
            }
        }

        private static string GetResourceLoaderTemplate(ExtensionType extensionType)
        {
            if (extensionType == ExtensionType.PanelExtension)
            {
                return @"//------------------------------------------------------------------------------
// <auto-generated>
//     扩展名称：{{extension_name}}
//     创建时间：{{creation_time}}
//     用途：自定义面板资源扩展方法（对标 EUUIPanelBase.EURes）
// </auto-generated>
//------------------------------------------------------------------------------
#define EUUI_EXTENSIONS_GENERATED

using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// EUUIPanelBase 的 {{extension_name}} 资源扩展（静态扩展方法）
    /// TODO: 在此描述您的资源加载方案
    /// </summary>
    public static class EUUIPanelBase{{extension_name}}Extensions
    {
        /// <summary>
        /// 设置 Image 图片
        /// TODO: url 格式由您的资源系统决定（如 atlasName/spriteName）
        /// </summary>
        public static void SetImage<T>(this EUUIPanelBase<T> panel, Image image, string url, bool isSetNativeSize = true)
            where T : EUUIPanelBase<T>, new()
        {
            if (image == null || string.IsNullOrEmpty(url)) return;
            var sprite = panel.LoadSprite(url);
            if (sprite != null)
            {
                image.sprite = sprite;
                if (isSetNativeSize) image.SetNativeSize();
            }
        }

        /// <summary>
        /// 同步加载 Sprite
        /// </summary>
        public static Sprite LoadSprite<T>(this EUUIPanelBase<T> panel, string url)
            where T : EUUIPanelBase<T>, new()
        {
            if (string.IsNullOrEmpty(url)) return null;
            // TODO: 实现 Sprite 加载逻辑
            // 提示：解析 url，调用您的资源系统（如 Addressables / AssetBundle / YooAsset）同步加载
            Debug.LogWarning($""[{{extension_name}}] 请实现 LoadSprite 逻辑: {url}"");
            return null;
        }

        /// <summary>
        /// 异步加载 Prefab
        /// </summary>
        public static async UniTask<GameObject> LoadPrefabAsync<T>(this EUUIPanelBase<T> panel, string path)
            where T : EUUIPanelBase<T>, new()
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning(""[{{extension_name}}] Prefab path 为空"");
                return null;
            }
            // TODO: 实现异步 Prefab 加载逻辑
            await UniTask.Yield();
            return null;
        }

        /// <summary>
        /// 同步加载 Prefab
        /// </summary>
        public static GameObject LoadPrefab<T>(this EUUIPanelBase<T> panel, string path)
            where T : EUUIPanelBase<T>, new()
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning(""[{{extension_name}}] Prefab path 为空"");
                return null;
            }
            // TODO: 实现同步 Prefab 加载逻辑
            return null;
        }
    }
}
";
            }

            // KitExtension：UIKit 侧资源加载核心（LoadPanelPrefabAsync / OnPanelClosed）
            return @"//------------------------------------------------------------------------------
// <auto-generated>
//     扩展名称：{{extension_name}}
//     创建时间：{{creation_time}}
//     用途：自定义资源加载实现（UIKit 核心入口）
// </auto-generated>
//------------------------------------------------------------------------------
#define EUUI_EXTENSIONS_GENERATED

using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// {{extension_name}} 资源加载扩展（EUUIKit 核心入口）
    /// TODO: 在此描述您的资源加载方案
    /// </summary>
    public static partial class EUUIKit
    {
        // ========== 资源缓存（可选） ==========
        // private static Dictionary<string, object> _{{extension_name}}_cache = new Dictionary<string, object>();
        
        /// <summary>
        /// 加载面板 Prefab（框架核心入口，由 EUUIKit.Open 调用）
        /// </summary>
        private static async UniTask<GameObject> LoadPanelPrefabAsync<T>() 
            where T : EUUIPanelBase<T>
        {
            string panelName = typeof(T).Name;
            
            // TODO: 实现资源加载逻辑
            // 提示：
            // 1. 根据 panelName 确定资源路径
            // 2. 调用您的资源系统加载资源（如 Addressables / AssetBundle / YooAsset）
            // 3. 可选：缓存已加载的资源
            // 4. 返回加载的 GameObject
            
            Debug.LogWarning($""[{{extension_name}}] 请实现资源加载逻辑: {panelName}"");
            await UniTask.Yield();
            return null;
        }
        
        /// <summary>
        /// 释放面板资源（面板关闭时由框架调用）
        /// </summary>
        static partial void OnPanelClosed(string panelName)
        {
            // TODO: 实现资源释放逻辑
            // 提示：
            // 1. 从缓存中移除资源
            // 2. 调用您的资源系统释放资源
        }
        
        // ========== 辅助方法（可选） ==========
        
        /// <summary>
        /// 预加载面板资源
        /// </summary>
        public static async UniTask<bool> {{extension_name}}_PreloadPanel(string panelName)
        {
            // TODO: 实现预加载逻辑
            await UniTask.Yield();
            return false;
        }
        
        /// <summary>
        /// 清理所有资源缓存
        /// </summary>
        public static void {{extension_name}}_ClearCache()
        {
            // TODO: 实现缓存清理
        }
    }
}
";
        }

        private static string GetStaticExtensionTemplate(ExtensionType extensionType)
        {
            if (extensionType == ExtensionType.PanelExtension)
            {
                return @"//------------------------------------------------------------------------------
// <auto-generated>
//     扩展名称：{{extension_name}}
//     创建时间：{{creation_time}}
//     用途：面板静态扩展方法
// </auto-generated>
//------------------------------------------------------------------------------
#define EUUI_EXTENSIONS_GENERATED

using UnityEngine;
using Cysharp.Threading.Tasks;

namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// EUUIPanelBase 的 {{extension_name}} 扩展
    /// TODO: 描述此扩展提供的功能
    /// </summary>
    public static class EUUIPanelBase{{extension_name}}Extensions
    {
        /// <summary>
        /// TODO: 扩展方法示例
        /// </summary>
        public static void DoSomething<T>(this EUUIPanelBase<T> panel, string param)
            where T : EUUIPanelBase<T>, new()
        {
            // TODO: 实现扩展功能
            Debug.Log($""[{{extension_name}}] 扩展方法被调用: {typeof(T).Name}, 参数: {param}"");
        }
        
        /// <summary>
        /// TODO: 异步扩展方法示例
        /// </summary>
        public static async UniTask DoSomethingAsync<T>(this EUUIPanelBase<T> panel)
            where T : EUUIPanelBase<T>, new()
        {
            // TODO: 实现异步扩展功能
            Debug.Log($""[{{extension_name}}] 异步扩展方法开始: {typeof(T).Name}"");
            await UniTask.Yield();
            Debug.Log($""[{{extension_name}}] 异步扩展方法完成"");
        }
        
        /// <summary>
        /// TODO: 获取/设置扩展数据示例
        /// </summary>
        public static T GetExtensionData<T>(this EUUIPanelBase panel, string key) where T : class
        {
            // TODO: 实现扩展数据获取逻辑
            return null;
        }
    }
}
";
            }
            else
            {
                return @"//------------------------------------------------------------------------------
// <auto-generated>
//     扩展名称：{{extension_name}}
//     创建时间：{{creation_time}}
//     用途：UIKit 功能扩展
// </auto-generated>
//------------------------------------------------------------------------------
#define EUUI_EXTENSIONS_GENERATED

using UnityEngine;
using Cysharp.Threading.Tasks;

namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// EUUIKit 的 {{extension_name}} 功能扩展
    /// TODO: 描述此扩展提供的功能
    /// </summary>
    public static partial class EUUIKit
    {
        // ========== 私有字段 ==========
        // TODO: 添加需要的字段
        // private static bool _{{extension_name}}_initialized = false;
        
        /// <summary>
        /// TODO: 初始化方法示例
        /// </summary>
        public static void {{extension_name}}_Initialize()
        {
            // TODO: 实现初始化逻辑
            Debug.Log(""[{{extension_name}}] 已初始化"");
        }
        
        /// <summary>
        /// TODO: 公开方法示例
        /// </summary>
        public static void {{extension_name}}_DoSomething(string param)
        {
            // TODO: 实现功能
            Debug.Log($""[{{extension_name}}] DoSomething: {param}"");
        }
        
        /// <summary>
        /// TODO: 异步方法示例
        /// </summary>
        public static async UniTask {{extension_name}}_DoSomethingAsync()
        {
            // TODO: 实现异步功能
            Debug.Log(""[{{extension_name}}] 异步操作开始"");
            await UniTask.Yield();
            Debug.Log(""[{{extension_name}}] 异步操作完成"");
        }
    }
}
";
            }
        }

    }
}
