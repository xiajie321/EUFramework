using UnityEditor;
using UnityEngine;
using System.IO;

namespace EUFramework.Extension.EURes.Editor
{
    /// <summary>
    /// EUResServerConfig 编辑器扩展
    /// </summary>
    public static class EUResServerConfigEditor
    {
        // 使用动态路径
        private static string DEFAULT_PATH => EUResKitPathHelper.GetSettingsPath();
        private const string DEFAULT_FILENAME = "EUResServerConfig.asset";

        [MenuItem("YooAsset/Create ResServer Config", false, 100)]
        public static void CreateResServerConfig()
        {
            // 创建配置目录
            if (!Directory.Exists(DEFAULT_PATH))
            {
                Directory.CreateDirectory(DEFAULT_PATH);
                AssetDatabase.Refresh();
            }

            string fullPath = Path.Combine(DEFAULT_PATH, DEFAULT_FILENAME);

            // 检查是否已存在
            var existingAsset = AssetDatabase.LoadAssetAtPath<EUResServerConfig>(fullPath);
            if (existingAsset != null)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "配置已存在",
                    $"配置文件已存在于:\n{fullPath}\n\n是否要选中现有配置？",
                    "选中现有配置",
                    "取消"
                );

                if (overwrite)
                {
                    EditorGUIUtility.PingObject(existingAsset);
                    Selection.activeObject = existingAsset;
                }
                return;
            }

            // 创建新配置
            EUResServerConfig config = ScriptableObject.CreateInstance<EUResServerConfig>();
            config.protocol = ServerProtocol.HTTP;
            config.hostServer = "127.0.0.1";
            config.port = 80;
            config.appVersion = "1.0.0";

            // 保存资源
            AssetDatabase.CreateAsset(config, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 选中并高亮显示
            EditorGUIUtility.PingObject(config);
            Selection.activeObject = config;

            Debug.Log($"[EUResServerConfig] 配置文件创建成功: {fullPath}");
        }

        [MenuItem("YooAsset/Open ResServer Config", false, 101)]
        public static void OpenResServerConfig()
        {
            string fullPath = Path.Combine(DEFAULT_PATH, DEFAULT_FILENAME);
            var config = AssetDatabase.LoadAssetAtPath<EUResServerConfig>(fullPath);

            if (config != null)
            {
                EditorGUIUtility.PingObject(config);
                Selection.activeObject = config;
            }
            else
            {
                bool create = EditorUtility.DisplayDialog(
                    "配置不存在",
                    "EUResServerConfig 配置文件不存在，是否创建？",
                    "创建",
                    "取消"
                );

                if (create)
                {
                    CreateResServerConfig();
                }
            }
        }
    }

    /// <summary>
    /// EUResServerConfig 自定义 Inspector
    /// </summary>
    [CustomEditor(typeof(EUResServerConfig))]
    public class EUResServerConfigInspector : UnityEditor.Editor
    {
        private SerializedProperty protocolProp;
        private SerializedProperty hostServerProp;
        private SerializedProperty portProp;
        private SerializedProperty customUrlProp;
        private SerializedProperty appVersionProp;

        private void OnEnable()
        {
            protocolProp = serializedObject.FindProperty("protocol");
            hostServerProp = serializedObject.FindProperty("hostServer");
            portProp = serializedObject.FindProperty("port");
            customUrlProp = serializedObject.FindProperty("customUrl");
            appVersionProp = serializedObject.FindProperty("appVersion");
        }

        public override void OnInspectorGUI()
        {
            EUResServerConfig config = (EUResServerConfig)target;

            serializedObject.Update();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("资源服务器配置", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // 服务器配置
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(protocolProp, new GUIContent("协议类型"));
            
            // 根据协议类型显示不同的字段
            ServerProtocol protocol = (ServerProtocol)protocolProp.enumValueIndex;
            
            if (protocol == ServerProtocol.Custom)
            {
                EditorGUILayout.PropertyField(customUrlProp, new GUIContent("自定义完整URL"));
                EditorGUILayout.HelpBox("示例: https://cdn.example.com:8080", MessageType.Info);
            }
            else
            {
                EditorGUILayout.PropertyField(hostServerProp, new GUIContent("主机地址 (IP或域名)"));
                EditorGUILayout.PropertyField(portProp, new GUIContent("端口号"));
                
                // 显示协议说明
                string protocolName = protocol == ServerProtocol.HTTPS ? "HTTPS" : "HTTP";
                EditorGUILayout.HelpBox($"协议: {protocolName}", MessageType.None);
            }
            
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("完整地址:", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(config.GetServerUrl(), EditorStyles.textField, GUILayout.Height(20));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // 应用版本
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(appVersionProp, new GUIContent("应用版本"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 验证状态
            if (config.IsValid())
            {
                EditorGUILayout.HelpBox("✓ 配置有效", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("✗ 配置无效，请检查必填项", MessageType.Warning);
            }

            EditorGUILayout.Space(5);

            // 快捷操作按钮
            if (GUILayout.Button("重置为默认值", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("确认重置", "确定要重置为默认配置吗？", "确定", "取消"))
                {
                    config.protocol = ServerProtocol.HTTP;
                    config.hostServer = "127.0.0.1";
                    config.port = 80;
                    config.customUrl = "";
                    config.appVersion = "1.0.0";
                    EditorUtility.SetDirty(config);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
