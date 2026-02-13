using System.IO;
using UnityEditor;

namespace Framework.Editor
{
    public static class UIEditorConstants
    {
        //UI Scene 中对应的层级
        public const string ExportRoot = "ExportRoot";
        public const string NotExportBottom = "NotExportRoot_Bottom";
        public const string NotExportTop = "NotExportRoot_Top";

        //相关的路径
        public const string UISceneSavePath = "Assets/Res/CreateUIScenes";
        public const string UIPrefabSaveBuiltinPath = "Assets/Res/Builtin/UI/Prefabs";  //首包资源
        public const string UIPrefabSavePath = "Assets/Res/Remote/UI/Prefabs";  //远程资源
        public const string UIAtlasSavePath = "Assets/Res/Remote/UI/Atlases";   //图集资源
        public const string UIAtlasBuiltinSavePath = "Assets/Res/Builtin/UI/Atlases"; //首包图集资源
        public const string UIBindScripts = "Assets/Scripts/Generated/UI";  //绑定脚本  
        public const string UILogicScripts = "Assets/Scripts/Game/UI";  //业务逻辑脚本
        
        // ListView 相关路径
        public const string ListViewBindScripts = "Assets/Scripts/Generated/ListView";  // ViewsHolder 生成脚本
        public const string ListViewCompFolder = "Comp";  // 组件子目录名

        public static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        public static string GetUIPrefabDir(UIPackageType type)
        {
            return type switch
            {
                UIPackageType.Builtin => UIPrefabSaveBuiltinPath,
                UIPackageType.Remote => UIPrefabSavePath,
                _ => UIPrefabSavePath // 默认保底
            };
        }
        //UI命名空间
        public const string UINameSpace = "Game.UI";  //UI命名空间
    }
}