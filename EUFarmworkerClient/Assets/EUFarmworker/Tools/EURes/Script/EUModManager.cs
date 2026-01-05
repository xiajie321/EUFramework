using System.IO;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EUFarmworker.Tools.EURes.Script
{
    /// <summary>
    /// 简单的 Mod 管理器
    /// 负责加载本地 Mod 目录下的资源和代码
    /// </summary>
    public static class EUModManager
    {
        /// <summary>
        /// 加载指定目录下的所有 Mod
        /// </summary>
        /// <param name="modsRootPath">Mods 根目录</param>
        public static async UniTask LoadAllModsAsync(string modsRootPath)
        {
            if (!Directory.Exists(modsRootPath))
            {
                Debug.LogWarning($"[EUMod] Mods 根目录不存在: {modsRootPath}");
                return;
            }

            string[] modDirs = Directory.GetDirectories(modsRootPath);
            foreach (var modDir in modDirs)
            {
                await LoadModAsync(modDir);
            }
        }

        /// <summary>
        /// 加载单个 Mod
        /// </summary>
        /// <param name="modPath">Mod 目录路径</param>
        public static async UniTask LoadModAsync(string modPath)
        {
            string modName = Path.GetFileName(modPath);
            Debug.Log($"[EUMod] 开始加载 Mod: {modName} ({modPath})");

            // 1. 加载资源包
            // 假设 Mod 目录本身就是一个 YooAsset 构建输出目录 (包含 PackageManifest 等)
            // 我们使用 Mod 目录名作为包名
            if (EUResUtility.Instance != null)
            {
                await EUResUtility.Instance.LoadPackageAsync(modName, null, modPath);
            }
            else
            {
                Debug.LogError("[EUMod] EUResUtility 未初始化，无法加载 Mod 资源。");
                return;
            }

            // 2. 加载代码 (DLL)
            // 扫描 Mod 目录下的所有 .dll 文件
            // 注意：这里假设 DLL 是兼容的 (例如使用 HybridCLR 编译的热更 DLL)
            string[] dllFiles = Directory.GetFiles(modPath, "*.dll");
            foreach (var dllPath in dllFiles)
            {
                try
                {
                    byte[] dllBytes = File.ReadAllBytes(dllPath);
                    Assembly.Load(dllBytes);
                    Debug.Log($"[EUMod] 加载 DLL 成功: {Path.GetFileName(dllPath)}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[EUMod] 加载 DLL 失败: {dllPath}, Error: {e.Message}");
                }
            }
            
            Debug.Log($"[EUMod] Mod 加载完成: {modName}");
        }
    }
}
