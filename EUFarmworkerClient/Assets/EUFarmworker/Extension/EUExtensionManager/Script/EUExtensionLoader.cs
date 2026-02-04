#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;

using UnityEditor;
using UnityEngine.Networking;

namespace EUFarmworker.ExtensionManager
{
    public static class EUExtensionLoader
    {
        public const string ExtensionMarkerFile = "extension.json";
        
        private const string PrefsKey_CommunityUrl = "EUExtensionManager_CommunityUrl";
        private const string PrefsKey_ExtensionRootPath = "EUExtensionManager_ExtensionRootPath";
        private const string PrefsKey_CoreInstallPath = "EUExtensionManager_CoreInstallPath";
        private const string DefaultCommunityUrl = "https://github.com/xiajie321/EUFramworkerCommunity";
        private const string DefaultExtensionRootPath = "Assets/EUFarmworker/Extension";
        private const string DefaultCoreInstallPath = "Assets/EUFarmworker/Core/MVC";

        public static string ExtensionRootPath
        {
            get => EditorPrefs.GetString(PrefsKey_ExtensionRootPath, DefaultExtensionRootPath);
            set => EditorPrefs.SetString(PrefsKey_ExtensionRootPath, value);
        }

        public static string CoreInstallPath
        {
            get => EditorPrefs.GetString(PrefsKey_CoreInstallPath, DefaultCoreInstallPath);
            set => EditorPrefs.SetString(PrefsKey_CoreInstallPath, value);
        }

        public static string CommunityUrl
        {
            get => EditorPrefs.GetString(PrefsKey_CommunityUrl, DefaultCommunityUrl);
            set => EditorPrefs.SetString(PrefsKey_CommunityUrl, value);
        }
        
        /// <summary>
        /// 获取用于下载原始文件的URL (GitHub Raw)
        /// </summary>
        private static string GetRawFileUrl(string dirName, string fileName)
        {
            string url = CommunityUrl.TrimEnd('/');
            return url.Replace("github.com", "raw.githubusercontent.com") + $"/main/{dirName}/{fileName}";
        }
        
        /// <summary>
        /// 获取仓库ZIP下载URL
        /// </summary>
        private static string GetZipDownloadUrl(string branch = "main")
        {
            string url = CommunityUrl.TrimEnd('/');
            return url + $"/archive/refs/heads/{branch}.zip";
        }

        public static void ResetSettings()
        {
            EditorPrefs.DeleteKey(PrefsKey_CommunityUrl);
            EditorPrefs.DeleteKey(PrefsKey_ExtensionRootPath);
            EditorPrefs.DeleteKey(PrefsKey_CoreInstallPath);
        }

        public static List<EUExtensionInfo> GetAllLocalExtensions()
        {
            List<EUExtensionInfo> extensions = new List<EUExtensionInfo>();
            
            // 扫描扩展目录
            ScanDirectoryForExtensions(ExtensionRootPath, extensions);

            // 扫描核心目录（核心本身作为一个特殊的扩展）
            ScanCoreDirectory(extensions);
            
            return extensions;
        }

        private static void ScanDirectoryForExtensions(string rootPath, List<EUExtensionInfo> extensions)
        {
            string fullPath;
            if (rootPath.StartsWith("Assets"))
                fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", rootPath));
            else
                fullPath = rootPath;
            
            if (!Directory.Exists(fullPath)) 
            {
                try { Directory.CreateDirectory(fullPath); }
                catch { return; }
            }

            string[] directories = Directory.GetDirectories(fullPath);
            foreach (string dir in directories)
            {
                TryLoadExtensionInfo(dir, extensions);
            }
        }

        private static void ScanCoreDirectory(List<EUExtensionInfo> extensions)
        {
            string corePath = CoreInstallPath;
            string fullPath;
            if (corePath.StartsWith("Assets"))
                fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", corePath));
            else
                fullPath = corePath;

            if (Directory.Exists(fullPath))
            {
                // 检查是否包含 Doc 文件夹
                string docPath = Path.Combine(fullPath, "Doc");
                if (Directory.Exists(docPath))
                {
                    TryLoadExtensionInfo(fullPath, extensions, isCore: true);
                }
            }
        }

        private static void TryLoadExtensionInfo(string dir, List<EUExtensionInfo> extensions, bool isCore = false)
        {
            string jsonPath = Path.Combine(dir, ExtensionMarkerFile);
            if (File.Exists(jsonPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    EUExtensionInfo info = JsonUtility.FromJson<EUExtensionInfo>(jsonContent);
                    if (info != null)
                    {
                        info.folderPath = dir.Replace("\\", "/");
                        info.isInstalled = true;
                        // 如果是核心，可以在这里标记，或者通过 category 区分
                        if (isCore && string.IsNullOrEmpty(info.category))
                        {
                            info.category = "Core";
                        }
                        extensions.Add(info);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"加载本地插件失败 {dir}: {e.Message}");
                }
            }
        }

        public static void Uninstall(EUExtensionInfo info)
        {
            if (string.IsNullOrEmpty(info.folderPath) || !Directory.Exists(info.folderPath)) return;
            try
            {
                Directory.Delete(info.folderPath, true);
                string meta = info.folderPath + ".meta";
                if (File.Exists(meta)) File.Delete(meta);
                AssetDatabase.Refresh();
            }
            catch (Exception e) { Debug.LogError("卸载失败: " + e.Message); }
        }

        public static void OpenDocumentation(EUExtensionInfo info)
        {
            if (string.IsNullOrEmpty(info.folderPath)) return;
            
            string docPath = Path.Combine(info.folderPath, "Doc");
            if (Directory.Exists(docPath))
            {
                string[] mdFiles = Directory.GetFiles(docPath, "*.md");
                if (mdFiles.Length > 0)
                {
                    string readme = mdFiles.FirstOrDefault(f => Path.GetFileName(f).Equals("README.md", StringComparison.OrdinalIgnoreCase));
                    string target = readme ?? mdFiles[0];
                    
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GetRelativePath(target));
                    if (obj != null) AssetDatabase.OpenAsset(obj);
                    else System.Diagnostics.Process.Start(target);
                }
                else
                {
                    EditorUtility.RevealInFinder(docPath);
                }
            }
            else
            {
                Debug.LogWarning($"未找到文档目录: {docPath}");
            }
        }

        private static string GetRelativePath(string fullPath)
        {
            string projectPath = Path.GetFullPath(Application.dataPath).Replace("\\", "/");
            fullPath = fullPath.Replace("\\", "/");
            if (fullPath.StartsWith(projectPath))
            {
                return "Assets" + fullPath.Substring(projectPath.Length);
            }
            return fullPath;
        }

        /// <summary>
        /// 创建带有请求头的 UnityWebRequest
        /// </summary>
        private static UnityWebRequest CreateRequest(string url)
        {
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            request.SetRequestHeader("Accept", "*/*");
            return request;
        }
        
        /// <summary>
        /// 从远程仓库获取扩展列表
        /// </summary>
        public static void FetchRemoteRegistry(Action<List<EUExtensionInfo>> callback)
        {
            Debug.Log("[EUExtensionManager] 从 GitHub 获取扩展列表...");
            FetchRemoteRegistryViaGitHubApi(callback);
        }
        
        /// <summary>
        /// 通过 GitHub API 获取目录列表
        /// </summary>
        private static void FetchRemoteRegistryViaGitHubApi(Action<List<EUExtensionInfo>> callback)
        {
            string url = CommunityUrl.TrimEnd('/');
            string apiUrl = url.Replace("github.com", "api.github.com/repos") + "/contents";
            
            Debug.Log($"[EUExtensionManager] API URL: {apiUrl}");
            
            var request = CreateRequest(apiUrl);
            request.SetRequestHeader("Accept", "application/vnd.github.v3+json");
            var operation = request.SendWebRequest();
            
            operation.completed += _ =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string json = request.downloadHandler.text;
                        List<string> directories = ParseGitHubDirectories(json);
                        
                        Debug.Log($"[EUExtensionManager] 找到 {directories.Count} 个目录");
                        
                        if (directories.Count == 0)
                        {
                            callback?.Invoke(new List<EUExtensionInfo>());
                            return;
                        }
                        
                        List<EUExtensionInfo> remoteExtensions = new List<EUExtensionInfo>();
                        int pending = directories.Count;
                        
                        foreach (var dirName in directories)
                        {
                            FetchRemoteExtensionJson(dirName, info =>
                            {
                                if (info != null)
                                {
                                    info.downloadUrl = $"{CommunityUrl}/tree/main/{dirName}";
                                    info.isInstalled = false;
                                    info.remoteFolderName = dirName;
                                    remoteExtensions.Add(info);
                                    Debug.Log($"[EUExtensionManager] 加载扩展: {info.displayName}");
                                }
                                pending--;
                                if (pending == 0) callback?.Invoke(remoteExtensions);
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[EUExtensionManager] 解析错误: {e.Message}");
                        callback?.Invoke(new List<EUExtensionInfo>());
                    }
                }
                else
                {
                    Debug.LogError($"[EUExtensionManager] API 请求失败: {request.error}");
                    callback?.Invoke(new List<EUExtensionInfo>());
                }
            };
        }
        
        /// <summary>
        /// 解析 GitHub API 返回的目录列表
        /// </summary>
        private static List<string> ParseGitHubDirectories(string json)
        {
            var directories = new List<string>();
            
            // 简单解析 JSON 数组，查找 type 为 dir 的项
            int idx = 0;
            while (idx < json.Length)
            {
                int objStart = json.IndexOf('{', idx);
                if (objStart < 0) break;
                
                int objEnd = json.IndexOf('}', objStart);
                if (objEnd < 0) break;
                
                string obj = json.Substring(objStart, objEnd - objStart + 1);
                
                // 检查是否是目录
                if (obj.Contains("\"type\":\"dir\"") || obj.Contains("\"type\": \"dir\""))
                {
                    // 提取 name
                    string name = ExtractJsonValue(obj, "name");
                    if (!string.IsNullOrEmpty(name) && !name.StartsWith("."))
                    {
                        directories.Add(name);
                    }
                }
                
                idx = objEnd + 1;
            }
            
            return directories;
        }
        
        private static string ExtractJsonValue(string json, string key)
        {
            string pattern = $"\"{key}\":";
            int startIndex = json.IndexOf(pattern);
            if (startIndex < 0) return null;
            startIndex += pattern.Length;
            
            while (startIndex < json.Length && char.IsWhiteSpace(json[startIndex])) startIndex++;
            
            if (startIndex >= json.Length) return null;
            
            if (json[startIndex] == '"')
            {
                startIndex++;
                int endIndex = json.IndexOf('"', startIndex);
                if (endIndex < 0) return null;
                return json.Substring(startIndex, endIndex - startIndex);
            }
            
            return null;
        }

        private static void FetchRemoteExtensionJson(string dirName, Action<EUExtensionInfo> callback)
        {
            string rawUrl = GetRawFileUrl(dirName, ExtensionMarkerFile);
            Debug.Log($"[EUExtensionManager] 获取 extension.json: {rawUrl}");
            
            var request = CreateRequest(rawUrl);
            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string jsonText = request.downloadHandler.text;
                        EUExtensionInfo info = JsonUtility.FromJson<EUExtensionInfo>(jsonText);
                        if (info != null && !string.IsNullOrEmpty(info.name))
                        {
                            callback?.Invoke(info);
                        }
                        else
                        {
                            Debug.LogWarning($"[EUExtensionManager] 无效的 extension.json: {dirName}");
                            callback?.Invoke(null);
                        }
                    }
                    catch (Exception e) 
                    { 
                        Debug.LogError($"[EUExtensionManager] JSON 解析错误 {dirName}: {e.Message}");
                        callback?.Invoke(null); 
                    }
                }
                else 
                { 
                    Debug.LogWarning($"[EUExtensionManager] 获取 extension.json 失败 {dirName}: {request.error}");
                    callback?.Invoke(null); 
                }
            };
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string dest = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, dest, true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string dest = Path.Combine(destinationDir, Path.GetFileName(dir));
                CopyDirectory(dir, dest);
            }
        }

        /// <summary>
        /// 下载并安装扩展（仅支持 ZIP 下载），完成后检查并安装依赖
        /// </summary>
        public static void DownloadAndInstall(EUExtensionInfo info, string dirName, Action<bool> onComplete)
        {
            Debug.Log($"[EUExtensionManager] 开始下载扩展: {info.displayName} (目录: {dirName})");
            DownloadExtensionViaZip(info, dirName, (success) =>
            {
                if (success)
                {
                    // 安装成功后，检查依赖
                    CheckAndInstallDependencies(info, onComplete);
                }
                else
                {
                    onComplete?.Invoke(false);
                }
            });
        }

        private static void CheckAndInstallDependencies(EUExtensionInfo info, Action<bool> onComplete)
        {
            if (info.dependencies == null || info.dependencies.Length == 0)
            {
                onComplete?.Invoke(true);
                return;
            }

            List<EUDependency> missingDeps = new List<EUDependency>();
            var localExtensions = GetAllLocalExtensions();

            foreach (var dep in info.dependencies)
            {
                // 检查本地是否已安装
                bool installed = localExtensions.Any(e => e.name == dep.name);
                // 也可以检查具体路径是否存在
                if (!installed && !string.IsNullOrEmpty(dep.installPath))
                {
                     // 简单检查路径
                     string path = dep.installPath;
                     if (path.StartsWith("Assets")) 
                        path = Path.Combine(Application.dataPath, "..", path);
                     if (Directory.Exists(path) && File.Exists(Path.Combine(path, ExtensionMarkerFile)))
                        installed = true;
                }

                if (!installed)
                {
                    missingDeps.Add(dep);
                }
            }

            if (missingDeps.Count > 0)
            {
                string depNames = string.Join(", ", missingDeps.Select(d => d.name));
                if (EditorUtility.DisplayDialog("安装依赖", 
                    $"扩展 {info.displayName} 需要安装以下依赖:\n{depNames}\n是否立即安装?", "安装", "稍后"))
                {
                    InstallDependenciesRecursive(missingDeps, 0, onComplete);
                }
                else
                {
                    onComplete?.Invoke(true);
                }
            }
            else
            {
                onComplete?.Invoke(true);
            }
        }

        private static void InstallDependenciesRecursive(List<EUDependency> deps, int index, Action<bool> onComplete)
        {
            if (index >= deps.Count)
            {
                onComplete?.Invoke(true);
                return;
            }

            var dep = deps[index];
            Action<bool> next = (success) => InstallDependenciesRecursive(deps, index + 1, onComplete);

            if (!string.IsNullOrEmpty(dep.gitUrl))
            {
                DownloadDependency(dep, next);
            }
            else
            {
                // 尝试从社区仓库获取信息
                // 这里我们没有当前的远程列表，可能需要重新获取或者假设调用者有 context
                // 简单起见，如果通过 DownloadDependency 失败（没 url），则跳过
                Debug.LogWarning($"依赖 {dep.name} 没有配置 gitUrl，无法自动安装。");
                next(true); 
            }
        }

        public static void DownloadDependency(EUDependency dep, Action<bool> onComplete)
        {
            if (string.IsNullOrEmpty(dep.gitUrl))
            {
                Debug.LogError($"[EUExtensionManager] 依赖 {dep.name} 的 gitUrl 为空");
                onComplete?.Invoke(false);
                return;
            }

            Debug.Log($"[EUExtensionManager] 开始下载依赖: {dep.name} 来自 {dep.gitUrl}");
            
            // 构建 ZIP 下载 URL
            string repoUrl = dep.gitUrl.TrimEnd('/');
            if (repoUrl.EndsWith(".git")) repoUrl = repoUrl.Substring(0, repoUrl.Length - 4);
            
            // 尝试下载 main 或 master 分支
            TryDownloadDependencyZip(dep, repoUrl, "main", success =>
            {
                if (success) onComplete?.Invoke(true);
                else TryDownloadDependencyZip(dep, repoUrl, "master", onComplete);
            });
        }
        
        /// <summary>
        /// 通过下载整个仓库 ZIP 并解压指定目录
        /// </summary>
        private static void DownloadExtensionViaZip(EUExtensionInfo info, string dirName, Action<bool> onComplete)
        {
            // 首先尝试 main 分支，失败后尝试 master 分支
            TryDownloadZip(info, dirName, "main", success =>
            {
                if (success)
                {
                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.Log("[EUExtensionManager] main 分支下载失败，尝试 master 分支...");
                    TryDownloadZip(info, dirName, "master", onComplete);
                }
            });
        }
        
        private static void TryDownloadZip(EUExtensionInfo info, string dirName, string branch, Action<bool> onComplete)
        {
            string zipUrl = GetZipDownloadUrl(branch);
            DownloadAndExtractZip(zipUrl, $"下载 {info.displayName}...", (extractedDir) =>
            {
                // 查找目标扩展目录
                string[] extractedDirs = Directory.GetDirectories(extractedDir);
                if (extractedDirs.Length == 0) throw new Exception("ZIP 解压后未找到目录");
                
                string repoRoot = extractedDirs[0];
                string extensionSourceDir = Path.Combine(repoRoot, dirName);
                if (!Directory.Exists(extensionSourceDir)) throw new Exception($"在仓库中未找到扩展目录: {dirName}");

                // 使用通用安装逻辑
                InstallExtensionFromSource(extensionSourceDir, dirName, null);
                
                Debug.Log($"[EUExtensionManager] 安装完成: {info.displayName}");
            }, onComplete);
        }

        private static void TryDownloadDependencyZip(EUDependency dep, string repoUrl, string branch, Action<bool> onComplete)
        {
            string zipUrl = $"{repoUrl}/archive/refs/heads/{branch}.zip";
            DownloadAndExtractZip(zipUrl, $"下载依赖 {dep.name}...", (extractedDir) =>
            {
                string[] extractedDirs = Directory.GetDirectories(extractedDir);
                if (extractedDirs.Length == 0) throw new Exception("ZIP 解压后未找到目录");
                
                string repoRoot = extractedDirs[0];
                
                // 使用通用安装逻辑
                InstallExtensionFromSource(repoRoot, dep.name, dep.installPath);
                
                Debug.Log($"[EUExtensionManager] 依赖安装完成: {dep.name}");
            }, onComplete);
        }

        private static void InstallExtensionFromSource(string sourceDir, string targetDirName, string explicitInstallPath)
        {
            string targetBase;

            // 1. 如果显式指定了安装路径，优先使用
            if (!string.IsNullOrEmpty(explicitInstallPath))
            {
                string installPath = explicitInstallPath.Replace("\\", "/");
                if (installPath.StartsWith("Assets"))
                {
                    targetBase = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                }
                else
                {
                    targetBase = Application.dataPath;
                }
                string fullTarget = Path.Combine(targetBase, installPath);
                InstallDirectory(sourceDir, fullTarget);
                return;
            }

            // 2. 否则，检查 extension.json 确定 category
            string jsonPath = Path.Combine(sourceDir, ExtensionMarkerFile);
            string category = "";
            if (File.Exists(jsonPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    EUExtensionInfo info = JsonUtility.FromJson<EUExtensionInfo>(jsonContent);
                    if (info != null) category = info.category;
                }
                catch { /* Ignore error, default path will be used */ }
            }

            // 3. 根据 Category 决定根路径
            string rootPath;
            if (category == "框架")
            {
                rootPath = CoreInstallPath;
            }
            else
            {
                rootPath = ExtensionRootPath;
            }

            if (rootPath.StartsWith("Assets"))
                targetBase = Path.GetFullPath(Path.Combine(Application.dataPath, "..", rootPath));
            else
                targetBase = rootPath;

            string targetDir = Path.Combine(targetBase, targetDirName);
            InstallDirectory(sourceDir, targetDir);
        }

        private static void InstallDirectory(string sourceDir, string targetDir)
        {
            // 如果目标目录已存在，先删除
            if (Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, true);
                string metaPath = targetDir + ".meta";
                if (File.Exists(metaPath)) File.Delete(metaPath);
            }
            
            // 复制扩展目录到目标位置
            CopyDirectory(sourceDir, targetDir);
        }

        private static void DownloadAndExtractZip(string url, string progressTitle, Action<string> onExtracted, Action<bool> onComplete)
        {
            Debug.Log($"[EUExtensionManager] 下载 ZIP: {url}");
            EditorUtility.DisplayProgressBar("下载中", progressTitle, 0.1f);
            
            var request = CreateRequest(url);
            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    EditorUtility.ClearProgressBar();
                    Debug.LogWarning($"[EUExtensionManager] 下载失败: {request.error}");
                    onComplete?.Invoke(false);
                    return;
                }
                
                string tempDir = null;
                string zipPath = null;

                try
                {
                    byte[] zipData = request.downloadHandler.data;
                    if (zipData == null || zipData.Length == 0) throw new Exception("ZIP 数据为空");
                    
                    if (zipData.Length < 4 || zipData[0] != 0x50 || zipData[1] != 0x4B)
                    {
                        throw new Exception("下载的不是有效的 ZIP 文件");
                    }
                    
                    EditorUtility.DisplayProgressBar("下载中", "正在解压...", 0.5f);
                    
                    tempDir = Path.Combine(Application.temporaryCachePath, "eu_temp_" + Guid.NewGuid().ToString());
                    Directory.CreateDirectory(tempDir);
                    
                    zipPath = Path.Combine(tempDir, "download.zip");
                    File.WriteAllBytes(zipPath, zipData);
                    
                    ZipFile.ExtractToDirectory(zipPath, tempDir);
                    
                    // 回调处理解压后的内容
                    onExtracted?.Invoke(tempDir);
                    
                    onComplete?.Invoke(true);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EUExtensionManager] 处理失败: {e.Message}");
                    onComplete?.Invoke(false);
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                    AssetDatabase.Refresh();
                    
                    // 清理
                    try
                    {
                        if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir)) 
                            Directory.Delete(tempDir, true);
                    }
                    catch {}
                }
            };
        }
    }
}
#endif
