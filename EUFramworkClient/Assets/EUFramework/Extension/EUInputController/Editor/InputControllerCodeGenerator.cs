using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Scriban;
using Scriban.Runtime;
using UnityEditor;
using UnityEngine;

namespace EUFramework.Extension.EUInputController.Editor
{
    /// <summary>
    /// 输入控制器代码生成器
    /// 根据 InputController.inputactions 文件自动生成对应的 C# 代码
    /// </summary>
    public class InputControllerCodeGenerator : AssetPostprocessor
    {
        private const string InputActionsFileName = "InputController.inputactions";
        private const string GeneratorScriptName = "InputControllerCodeGenerator.cs";

        #region 数据模型（用于解析 .inputactions JSON）

        [Serializable]
        private class InputActionsData
        {
            public string name;
            public List<ActionMapData> maps;
        }

        [Serializable]
        private class ActionMapData
        {
            public string name;
            public List<ActionData> actions;
        }

        [Serializable]
        private class ActionData
        {
            public string name;
            public string type;
        }

        #endregion

        #region 每个 ActionMap 的生成配置

        private class MapGenerationConfig
        {
            /// <summary>ActionMap 名称（如 "Player", "UI"）</summary>
            public string MapName;

            /// <summary>公开事件类名（如 "PlayerInputEvent"）</summary>
            public string EventClassName;

            /// <summary>内部回调类名（如 "PlayerInputControllerEvent"）</summary>
            public string CallbackClassName;

            /// <summary>接口名（如 "IPlayerActions"）</summary>
            public string InterfaceName;

            /// <summary>输出文件名（如 "PlayerInputControllerEvent.cs"）</summary>
            public string OutputFileName;

            /// <summary>在 PlayerInputController 中的字段名（如 "playerInputEvent"）</summary>
            public string EventFieldName;

            /// <summary>在 PlayerInputController 中的属性名（如 "PlayerInputControllerEvent"）</summary>
            public string EventPropertyName;

            /// <summary>Add 方法是否使用 = 赋值（true）还是 += 追加（false）</summary>
            public bool UseAssign;

            /// <summary>该 Map 下的所有 Action</summary>
            public List<ActionData> Actions;
        }

        #endregion

        #region 路径工具

        /// <summary>
        /// 通过查找自身脚本文件的位置来确定 Editor 目录的绝对路径
        /// </summary>
        private static string GetEditorDirectory()
        {
            // 查找本脚本的 GUID
            string[] guids = AssetDatabase.FindAssets("InputControllerCodeGenerator t:MonoScript");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(GeneratorScriptName))
                {
                    // 返回 Editor 目录的绝对路径
                    string relativeDirPath = Path.GetDirectoryName(path);
                    return Path.GetFullPath(relativeDirPath).Replace('\\', '/');
                }
            }

            Debug.LogError("[EUInputController] 无法定位代码生成器脚本，请确保 InputControllerCodeGenerator.cs 存在于 Editor 目录中。");
            return null;
        }

        /// <summary>
        /// 获取模板目录路径
        /// </summary>
        private static string GetTemplatesDirectory(string editorDir)
        {
            return Path.Combine(editorDir, "Templates").Replace('\\', '/');
        }

        /// <summary>
        /// 获取 Script 输出目录路径（Editor 的兄弟目录 Script）
        /// </summary>
        private static string GetScriptOutputDirectory(string editorDir)
        {
            string parentDir = Directory.GetParent(editorDir)?.FullName;
            if (parentDir == null)
            {
                Debug.LogError("[EUInputController] 无法获取 Editor 目录的父目录。");
                return null;
            }

            return Path.Combine(parentDir, "Script").Replace('\\', '/');
        }

        /// <summary>
        /// 查找 InputController.inputactions 文件的绝对路径
        /// </summary>
        private static string FindInputActionsFile(string editorDir)
        {
            string scriptDir = GetScriptOutputDirectory(editorDir);
            if (scriptDir == null) return null;

            string inputActionsPath = Path.Combine(scriptDir, "InputSystem", InputActionsFileName).Replace('\\', '/');
            if (File.Exists(inputActionsPath))
            {
                return inputActionsPath;
            }

            Debug.LogError($"[EUInputController] 未找到 {InputActionsFileName} 文件，预期路径: {inputActionsPath}");
            return null;
        }

        #endregion

        #region JSON 解析

        /// <summary>
        /// 解析 .inputactions JSON 文件，提取 ActionMap 和 Action 信息
        /// 使用简易正则解析，避免对外部 JSON 库的依赖
        /// </summary>
        private static InputActionsData ParseInputActions(string jsonContent)
        {
            var data = new InputActionsData();
            data.maps = new List<ActionMapData>();

            // 匹配 "maps" 数组中的每个对象
            // 使用简单的层级解析
            try
            {
                int mapsStart = jsonContent.IndexOf("\"maps\"", StringComparison.Ordinal);
                if (mapsStart == -1)
                {
                    Debug.LogError("[EUInputController] JSON 中未找到 \"maps\" 字段。");
                    return null;
                }

                // 找到 maps 数组的开头 [
                int arrayStart = jsonContent.IndexOf('[', mapsStart);
                if (arrayStart == -1) return null;

                // 找到匹配的 ]
                int arrayEnd = FindMatchingBracket(jsonContent, arrayStart, '[', ']');
                if (arrayEnd == -1) return null;

                string mapsArrayContent = jsonContent.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);

                // 提取每个 map 对象
                int searchPos = 0;
                while (searchPos < mapsArrayContent.Length)
                {
                    int objStart = mapsArrayContent.IndexOf('{', searchPos);
                    if (objStart == -1) break;

                    int objEnd = FindMatchingBracket(mapsArrayContent, objStart, '{', '}');
                    if (objEnd == -1) break;

                    string mapObjStr = mapsArrayContent.Substring(objStart, objEnd - objStart + 1);
                    var mapData = ParseActionMap(mapObjStr);
                    if (mapData != null)
                    {
                        data.maps.Add(mapData);
                    }

                    searchPos = objEnd + 1;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUInputController] 解析 InputActions JSON 时出错: {e.Message}");
                return null;
            }

            return data;
        }

        private static ActionMapData ParseActionMap(string mapJson)
        {
            var map = new ActionMapData();
            map.actions = new List<ActionData>();

            // 提取 map 名称
            map.name = ExtractStringValue(mapJson, "name");
            if (string.IsNullOrEmpty(map.name)) return null;

            // 提取 actions 数组
            int actionsStart = mapJson.IndexOf("\"actions\"", StringComparison.Ordinal);
            if (actionsStart == -1) return map;

            int actionsArrayStart = mapJson.IndexOf('[', actionsStart);
            if (actionsArrayStart == -1) return map;

            int actionsArrayEnd = FindMatchingBracket(mapJson, actionsArrayStart, '[', ']');
            if (actionsArrayEnd == -1) return map;

            string actionsArrayContent = mapJson.Substring(actionsArrayStart + 1, actionsArrayEnd - actionsArrayStart - 1);

            // 提取每个 action 对象
            int searchPos = 0;
            while (searchPos < actionsArrayContent.Length)
            {
                int objStart = actionsArrayContent.IndexOf('{', searchPos);
                if (objStart == -1) break;

                int objEnd = FindMatchingBracket(actionsArrayContent, objStart, '{', '}');
                if (objEnd == -1) break;

                string actionObjStr = actionsArrayContent.Substring(objStart, objEnd - objStart + 1);
                string actionName = ExtractStringValue(actionObjStr, "name");
                string actionType = ExtractStringValue(actionObjStr, "type");

                if (!string.IsNullOrEmpty(actionName))
                {
                    map.actions.Add(new ActionData { name = actionName, type = actionType });
                }

                searchPos = objEnd + 1;
            }

            return map;
        }

        private static string ExtractStringValue(string json, string key)
        {
            string pattern = $"\"{Regex.Escape(key)}\"\\s*:\\s*\"([^\"]*?)\"";
            var match = Regex.Match(json, pattern);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static int FindMatchingBracket(string str, int startIndex, char open, char close)
        {
            int depth = 0;
            bool inString = false;
            for (int i = startIndex; i < str.Length; i++)
            {
                char c = str[i];
                if (c == '"' && (i == 0 || str[i - 1] != '\\'))
                {
                    inString = !inString;
                    continue;
                }

                if (inString) continue;

                if (c == open) depth++;
                else if (c == close)
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }

            return -1;
        }

        #endregion

        #region 名称转换工具

        /// <summary>
        /// 将首字母转为小写，如果全部是大写则全部转为小写
        /// "Player" → "player", "UI" → "ui"
        /// </summary>
        private static string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            // 如果全部是大写字母，则全部小写
            if (name.All(char.IsUpper))
            {
                return name.ToLower();
            }

            // 否则仅首字母小写
            return char.ToLower(name[0]) + name.Substring(1);
        }

        /// <summary>
        /// 根据 ActionMap 名称生成各类命名配置
        /// </summary>
        private static MapGenerationConfig CreateMapConfig(ActionMapData mapData, bool useAssign)
        {
            string mapName = mapData.name;
            return new MapGenerationConfig
            {
                MapName = mapName,
                EventClassName = $"{mapName}InputEvent",
                CallbackClassName = $"{mapName}InputControllerEvent",
                InterfaceName = $"I{mapName}Actions",
                OutputFileName = $"{mapName}InputControllerEvent.cs",
                EventFieldName = $"{ToCamelCase(mapName)}InputEvent",
                EventPropertyName = $"{mapName}InputControllerEvent",
                UseAssign = useAssign,
                Actions = mapData.actions
            };
        }

        #endregion

        #region 代码生成

        /// <summary>
        /// 从文件加载 Scriban 模板
        /// </summary>
        private static Template LoadTemplate(string templatePath)
        {
            if (!File.Exists(templatePath))
            {
                Debug.LogError($"[EUInputController] 模板文件未找到: {templatePath}");
                return null;
            }

            string templateContent = File.ReadAllText(templatePath);
            var template = Template.Parse(templateContent);
            if (template.HasErrors)
            {
                Debug.LogError($"[EUInputController] 模板解析错误 ({templatePath}):\n{string.Join("\n", template.Messages.Select(m => m.ToString()))}");
                return null;
            }

            return template;
        }

        /// <summary>
        /// 使用 Scriban 渲染模板并写入文件
        /// </summary>
        private static bool RenderAndWrite(Template template, ScriptObject scriptObject, string outputPath)
        {
            try
            {
                var context = new TemplateContext();
                context.PushGlobal(scriptObject);
                string result = template.Render(context);

                // 移除开头可能的空行
                result = result.TrimStart('\r', '\n');

                string existingContent = File.Exists(outputPath) ? File.ReadAllText(outputPath) : null;
                if (existingContent == result)
                {
                    Debug.Log($"[EUInputController] 文件内容未变化，跳过写入: {Path.GetFileName(outputPath)}");
                    return false;
                }

                File.WriteAllText(outputPath, result);
                Debug.Log($"[EUInputController] 成功生成文件: {Path.GetFileName(outputPath)}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUInputController] 渲染模板时出错: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 执行完整的代码生成流程
        /// </summary>
        private static void GenerateAll()
        {
            string editorDir = GetEditorDirectory();
            if (editorDir == null) return;

            string templatesDir = GetTemplatesDirectory(editorDir);
            string scriptOutputDir = GetScriptOutputDirectory(editorDir);
            string inputActionsPath = FindInputActionsFile(editorDir);

            if (scriptOutputDir == null || inputActionsPath == null) return;

            // 读取并解析 .inputactions 文件
            string jsonContent = File.ReadAllText(inputActionsPath);
            var inputActionsData = ParseInputActions(jsonContent);
            if (inputActionsData == null || inputActionsData.maps == null || inputActionsData.maps.Count == 0)
            {
                Debug.LogError("[EUInputController] 解析 InputActions 数据失败或无 ActionMap。");
                return;
            }

            Debug.Log($"[EUInputController] 开始代码生成，检测到 {inputActionsData.maps.Count} 个 ActionMap...");

            // 构建每个 ActionMap 的配置
            // 注意：第一个 map 通常是 "Player"，使用 = 赋值；其余使用 += 追加
            var mapConfigs = new List<MapGenerationConfig>();
            for (int i = 0; i < inputActionsData.maps.Count; i++)
            {
                bool useAssign = (i == 0); // 第一个 map 使用赋值模式
                mapConfigs.Add(CreateMapConfig(inputActionsData.maps[i], useAssign));
            }

            bool anyFileChanged = false;

            // 1. 生成 PlayerInputController.cs
            string controllerTemplatePath = Path.Combine(templatesDir, "PlayerInputController.scriban");
            var controllerTemplate = LoadTemplate(controllerTemplatePath);
            if (controllerTemplate != null)
            {
                var scriptObject = new ScriptObject();
                var mapsData = new List<Dictionary<string, object>>();
                foreach (var config in mapConfigs)
                {
                    mapsData.Add(new Dictionary<string, object>
                    {
                        { "name", config.MapName },
                        { "event_class_name", config.EventClassName },
                        { "event_field_name", config.EventFieldName },
                        { "event_property_name", config.EventPropertyName }
                    });
                }

                scriptObject.Add("maps", mapsData);

                string outputPath = Path.Combine(scriptOutputDir, "PlayerInputController.cs");
                if (RenderAndWrite(controllerTemplate, scriptObject, outputPath))
                    anyFileChanged = true;
            }

            // 2. 为每个 ActionMap 生成事件文件
            string eventTemplatePath = Path.Combine(templatesDir, "InputControllerEvent.scriban");
            var eventTemplate = LoadTemplate(eventTemplatePath);
            if (eventTemplate != null)
            {
                foreach (var config in mapConfigs)
                {
                    var scriptObject = new ScriptObject();
                    scriptObject.Add("event_class_name", config.EventClassName);
                    scriptObject.Add("callback_class_name", config.CallbackClassName);
                    scriptObject.Add("interface_name", config.InterfaceName);
                    scriptObject.Add("use_assign", config.UseAssign);

                    var actionsData = new List<Dictionary<string, object>>();
                    foreach (var action in config.Actions)
                    {
                        actionsData.Add(new Dictionary<string, object>
                        {
                            { "name", action.name }
                        });
                    }

                    scriptObject.Add("actions", actionsData);

                    string outputPath = Path.Combine(scriptOutputDir, config.OutputFileName);
                    if (RenderAndWrite(eventTemplate, scriptObject, outputPath))
                        anyFileChanged = true;
                }
            }

            if (anyFileChanged)
            {
                AssetDatabase.Refresh();
                Debug.Log("[EUInputController] 代码生成完成，已刷新资源数据库。");
            }
            else
            {
                Debug.Log("[EUInputController] 代码生成完成，所有文件均为最新状态。");
            }
        }

        #endregion

        #region 触发机制

        /// <summary>
        /// 菜单项：手动触发代码生成
        /// </summary>
        [MenuItem("Tools/EUInputController/生成输入控制器代码")]
        public static void GenerateFromMenu()
        {
            Debug.Log("[EUInputController] 手动触发代码生成...");
            GenerateAll();
        }

        /// <summary>
        /// 资源导入后处理器：监听 .inputactions 文件变化，自动触发代码生成
        /// </summary>
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool shouldGenerate = false;

            foreach (string assetPath in importedAssets)
            {
                if (assetPath.EndsWith(InputActionsFileName))
                {
                    shouldGenerate = true;
                    break;
                }
            }

            if (shouldGenerate)
            {
                Debug.Log($"[EUInputController] 检测到 {InputActionsFileName} 文件变化，自动触发代码生成...");
                // 延迟执行以确保文件已完全写入
                EditorApplication.delayCall += GenerateAll;
            }
        }

        #endregion
    }
}
