#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.U2D;
using EUFramework.Extension.EUUI;

namespace EUFramework.Extension.EUUI.Editor
{
    /// <summary>
    /// UI 资源编辑器工具：负责图片类型自动转换、图集一键生成等
    /// </summary>
    [InitializeOnLoad]
    public static class EUUISpriteEditor
    {
        static EUUISpriteEditor()
        {
            Debug.Log("<color=cyan>[EUUISpriteEditor] 脚本已加载并初始化成功。</color>");
        }

        [EUHotboxEntry("生成图集", "图集", "从选中文件夹一键生成 SpriteAtlas")]
        [Shortcut("EUUI/生成图集", KeyCode.G, ShortcutModifiers.Control | ShortcutModifiers.Alt)]
        public static void GenerateAtlasFromFolder()
        {
            Debug.Log("[EUUISpriteEditor] ---> 开始执行图集生成流程 <---");

            var config = GetConfig();
            if (config == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到 EUUIEditorConfig，请先通过「EUUI 配置工具」创建 UI 配置。", "确定");
                return;
            }

            string folderPath = GetSelectedFolderPath();
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogWarning("[EUUISpriteEditor] 失败：未识别到选中的文件夹。");
                EditorUtility.DisplayDialog("提示", "请先在 Project 窗口选中一个【存放图片的文件夹】！", "确定");
                return;
            }

            Debug.Log($"[EUUISpriteEditor] 识别到文件夹路径: {folderPath}");

            string folderName = Path.GetFileName(folderPath);

            // 弹窗让用户选择图集保存到 Builtin 还是 Remote
            int choice = EditorUtility.DisplayDialogComplex(
                "选择图集保存位置",
                $"请选择 [{folderName}] 图集的打包类型：\n\n" +
                $"• 首包(Builtin)：{config.atlasBuiltinPath}\n" +
                $"• 远程(Remote)：{config.atlasRemotePath}",
                "首包 Builtin",
                "取消",
                "远程 Remote"
            );

            if (choice == 1)
            {
                Debug.Log("[EUUISpriteEditor] 用户取消操作。");
                return;
            }

            string saveDir = choice == 0 ? config.atlasBuiltinPath : config.atlasRemotePath;
            string typeLabel = choice == 0 ? "首包(Builtin)" : "远程(Remote)";
            Debug.Log($"[EUUISpriteEditor] 选择类型：【{typeLabel}】，保存目录: {saveDir}");

            try
            {
                ProcessTexturesInFolder(folderPath);

                EnsureDirectory(saveDir);
                string atlasPath = $"{saveDir}/{folderName}.spriteatlas";
                Debug.Log($"[EUUISpriteEditor] 图集保存路径: {atlasPath}");

                SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                bool isNew = atlas == null;
                if (isNew)
                {
                    atlas = new SpriteAtlas();
                    Debug.Log("[EUUISpriteEditor] 创建新图集。");
                }
                else
                {
                    Debug.Log("[EUUISpriteEditor] 更新现有图集。");
                }

                SetAtlasSettings(atlas);

                UnityEngine.Object folderObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);
                if (folderObj == null)
                {
                    Debug.LogError("[EUUISpriteEditor] 无法加载文件夹对象。");
                    return;
                }

                SpriteAtlasExtensions.Add(atlas, new UnityEngine.Object[] { folderObj });
                Debug.Log("[EUUISpriteEditor] 已关联目标文件夹。");

                if (isNew)
                    AssetDatabase.CreateAsset(atlas, atlasPath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"<color=green>[EUUISpriteEditor] 处理成功: {atlasPath}</color>");

                Selection.activeObject = atlas;
                EditorUtility.DisplayDialog(
                    "成功",
                    $"图集 [{folderName}] 处理完成！\n类型：{typeLabel}\n保存位置: {saveDir}\n请在 Inspector 面板确认并点击 'Pack Preview'。",
                    "确定"
                );
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EUUISpriteEditor] 运行异常: {e.Message}\n{e.StackTrace}");
            }
        }

        private static EUUIEditorConfig GetConfig()
        {
            return AssetDatabase.LoadAssetAtPath<EUUIEditorConfig>(EUUISceneEditor.GetEditorConfigPath());
        }

        private static void EnsureDirectory(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        private static string GetSelectedFolderPath()
        {
            var objects = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets);
            foreach (var obj in objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (AssetDatabase.IsValidFolder(path)) return path;

                if (File.Exists(path))
                {
                    string dir = Path.GetDirectoryName(path)?.Replace("\\", "/");
                    if (AssetDatabase.IsValidFolder(dir)) return dir;
                }
            }
            return null;
        }

        private static void ProcessTexturesInFolder(string folderPath)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { folderPath });
            int count = 0;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled))
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.SaveAndReimport();
                    count++;
                }
            }
            if (count > 0)
                Debug.Log($"[EUUISpriteEditor] 自动转换了 {count} 张贴图为 Sprite 类型。");
        }

        private static void SetAtlasSettings(SpriteAtlas atlas)
        {
            atlas.SetPackingSettings(new SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                enableRotation = false,
                enableTightPacking = false,
                padding = 4
            });

            atlas.SetTextureSettings(new SpriteAtlasTextureSettings
            {
                readable = false,
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear
            });
        }
    }
}
#endif
