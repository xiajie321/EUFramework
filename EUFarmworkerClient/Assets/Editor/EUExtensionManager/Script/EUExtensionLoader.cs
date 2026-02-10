#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;

using UnityEditor;
using UnityEngine.Networking;

namespace EUFarmworker.Extension.ExtensionManager
{
    public static class EUExtensionLoader
    {
        public const string ExtensionMarkerFile = "extension.json";
        
        // 缓存远程列表
        private static List<EUExtensionInfo> _cachedRemoteExtensions;
        private static DateTime _lastFetchTime;
        private const float CacheDurationSeconds = 300; // 5分钟内存缓存
        private const string CacheFilePath = "Library/EUExtensionCache.json";

        private const string PrefsKey_CommunityUrl = "EUExtensionManager_CommunityUrl";
        private const string PrefsKey_ExtensionRootPath = "EUExtensionManager_ExtensionRootPath";
        private const string PrefsKey_CoreInstallPath = "EUExtensionManager_CoreInstallPath";
        private const string DefaultCommunityUrl = "https://github.com/xiajie321/EUFramworkerCommunity";
        private const string DefaultExtensionRootPath = "Assets/EUFarmworker/Extension";
        private const string DefaultCoreInstallPath = "Assets/EUFarmworker/Core";

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

        public static string GetFullPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            if (path.StartsWith("Assets"))
                return Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            return path;
        }

        public static void MigrateExtensions(string oldPath, string newPath)
        {
            if (string.IsNullOrEmpty(oldPath) || string.IsNullOrEmpty(newPath) || oldPath == newPath) return;

            string oldFullPath = GetFullPath(oldPath);
            string newFullPath = GetFullPath(newPath);

            if (!Directory.Exists(oldFullPath)) return;
            if (!Directory.Exists(newFullPath)) Directory.CreateDirectory(newFullPath);

            string[] directories = Directory.GetDirectories(oldFullPath);
            bool movedAny = false;

            foreach (string dir in directories)
            {
                string jsonPath = Path.Combine(dir, ExtensionMarkerFile);
                if (File.Exists(jsonPath))
                {
                    string dirName = Path.GetFileName(dir);
                    string destDir = Path.Combine(newFullPath, dirName);

                    if (Directory.Exists(destDir))
                    {
                        Debug.LogWarning($"[EUExtensionManager] 目标路径已存在，跳过迁移: {dirName}");
                        continue;
                    }

                    try
                    {
                        string oldAssetPath = GetRelativePath(dir);
                        string newAssetPath = GetRelativePath(destDir);

                        // 只有当两个路径都在 Assets 下时才使用 AssetDatabase.MoveAsset
                        if (oldAssetPath.StartsWith("Assets") && newAssetPath.StartsWith("Assets"))
                        {
                            string error = AssetDatabase.MoveAsset(oldAssetPath, newAssetPath);
                            if (!string.IsNullOrEmpty(error))
                            {
                                Debug.LogError($"[EUExtensionManager] AssetDatabase 移动失败: {error}。尝试文件系统移动。");
                                // Fallback to IO
                                Directory.Move(dir, destDir);
                                string meta = dir + ".meta";
                                if (File.Exists(meta)) File.Move(meta, destDir + ".meta");
                            }
                        }
                        else
                        {
                            Directory.Move(dir, destDir);
                            string meta = dir + ".meta";
                            if (File.Exists(meta)) File.Move(meta, destDir + ".meta");
                        }
                        movedAny = true;
                        Debug.Log($"[EUExtensionManager] 已迁移: {dirName}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[EUExtensionManager] 迁移失败 {dirName}: {e.Message}");
                    }
                }
            }

            if (movedAny)
            {
                AssetDatabase.Refresh();
            }
        }
        
        /// <summary>
        /// 获取用于下载原始文件的URL (GitHub Raw)
        /// </summary>
        private static string GetRawFileUrl(string dirName, string fileName, string branch = "main")
        {
            string url = CommunityUrl.TrimEnd('/');
            // 添加时间戳防止缓存
            string baseUrl = url.Replace("github.com", "raw.githubusercontent.com") + $"/{branch}/{dirName}/{fileName}";
            return $"{baseUrl}?t={DateTime.Now.Ticks}";
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

            // 扫描 Packages 目录
            ScanPackagesDirectory(extensions);

            // 扫描 EUExtensionManager 自身（无论它在哪里）
            ScanSelf(extensions);
            
            return extensions;
        }

        private static void ScanPackagesDirectory(List<EUExtensionInfo> extensions)
        {
            string packagesPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages"));
            if (Directory.Exists(packagesPath))
            {
                string[] directories = Directory.GetDirectories(packagesPath);
                foreach (string dir in directories)
                {
                    TryLoadExtensionInfo(dir, extensions);
                }
            }
        }

        private static void ScanSelf(List<EUExtensionInfo> extensions)
        {
            // 通过脚本定位自身目录
            string[] guids = AssetDatabase.FindAssets("EUExtensionLoader t:Script");
            if (guids.Length == 0) return;

            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            if (string.IsNullOrEmpty(assetPath)) return;

            // 假设结构: .../EUExtensionManager/Script/EUExtensionLoader.cs
            //我们需要拿到 .../EUExtensionManager
            string scriptDir = Path.GetDirectoryName(assetPath);
            string managerDir = Path.GetDirectoryName(scriptDir);
            
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", managerDir));

            TryLoadExtensionInfo(fullPath, extensions);
        }

        private static void ScanDirectoryForExtensions(string rootPath, List<EUExtensionInfo> extensions)
        {
            string fullPath = GetFullPath(rootPath);
            
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
            string fullPath = GetFullPath(corePath);

            if (Directory.Exists(fullPath))
            {
                // Core 路径可能包含多个子模块（如 MVC），也可能直接就是核心包
                // 策略：扫描 Core 路径下的直接子目录，以及 Core 路径本身
                
                // 1. 检查 Core 路径本身
                TryLoadExtensionInfo(fullPath, extensions, isCore: true);

                // 2. 检查 Core 路径下的子目录
                string[] directories = Directory.GetDirectories(fullPath);
                foreach (string dir in directories)
                {
                    TryLoadExtensionInfo(dir, extensions, isCore: true);
                }
            }
        }

        private static EUExtensionInfo LoadExtensionInfo(string dir)
        {
            string jsonPath = Path.Combine(dir, ExtensionMarkerFile);
            if (!File.Exists(jsonPath)) return null;

            try
            {
                string jsonContent = File.ReadAllText(jsonPath);
                EUExtensionInfo info = JsonUtility.FromJson<EUExtensionInfo>(jsonContent);
                if (info != null)
                {
                    info.folderPath = dir.Replace("\\", "/");
                    info.isInstalled = true;
                }
                return info;
            }
            catch
            {
                return null;
            }
        }

        private static void TryLoadExtensionInfo(string dir, List<EUExtensionInfo> extensions, bool isCore = false)
        {
            var info = LoadExtensionInfo(dir);
            if (info != null)
            {
                // 如果是核心，可以在这里标记，或者通过 category 区分
                if (isCore && string.IsNullOrEmpty(info.category))
                {
                    info.category = "Core";
                }

                // 检查是否已存在同名扩展
                var existing = extensions.FirstOrDefault(e => e.name == info.name);
                if (existing != null)
                {
                    // 如果路径不同，说明有重复安装
                    if (!string.Equals(Path.GetFullPath(existing.folderPath).TrimEnd('/', '\\'), 
                                       Path.GetFullPath(info.folderPath).TrimEnd('/', '\\'), 
                                       StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.LogWarning($"[EUExtensionManager] 检测到重复扩展: {info.name}\n" +
                                         $"位置 1: {existing.folderPath}\n" +
                                         $"位置 2: {info.folderPath}\n" +
                                         $"将保留第一个加载的版本。");
                    }
                    return;
                }

                extensions.Add(info);
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
        public static void FetchRemoteRegistry(Action<List<EUExtensionInfo>> callback, bool forceRefresh = false)
        {
            if (!forceRefresh && _cachedRemoteExtensions != null && (DateTime.Now - _lastFetchTime).TotalSeconds < CacheDurationSeconds)
            {
                callback?.Invoke(_cachedRemoteExtensions);
                return;
            }

            FetchRemoteRegistryViaGitHubApi(list => 
            {
                if (list != null && list.Count > 0)
                {
                    _cachedRemoteExtensions = list;
                    _lastFetchTime = DateTime.Now;
                }
                callback?.Invoke(list);
            });
        }
        
        [Serializable]
        private class ExtensionCacheItem
        {
            public string path;
            public string sha;
            public EUExtensionInfo info;
        }

        [Serializable]
        private class ExtensionCacheData
        {
            public List<ExtensionCacheItem> items = new List<ExtensionCacheItem>();
            public long lastUpdateTime;
        }

        [Serializable]
        private class GitHubTreeResponse
        {
            public GitHubTreeItem[] tree;
            public bool truncated;
        }

        [Serializable]
        private class GitHubTreeItem
        {
            public string path;
            public string type;
            public string sha;
        }

        private static ExtensionCacheData LoadCache()
        {
            if (File.Exists(CacheFilePath))
            {
                try
                {
                    string json = File.ReadAllText(CacheFilePath);
                    return JsonUtility.FromJson<ExtensionCacheData>(json);
                }
                catch { }
            }
            return new ExtensionCacheData();
        }

        private static void SaveCache(ExtensionCacheData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(CacheFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[EUExtensionManager] 保存缓存失败: {e.Message}");
            }
        }

        /// <summary>
        /// 通过 GitHub Trees API 获取文件列表并增量更新
        /// </summary>
        private static void FetchRemoteRegistryViaGitHubApi(Action<List<EUExtensionInfo>> callback)
        {
            // 1. 先尝试加载本地缓存并立即回调（如果内存缓存为空）
            var cache = LoadCache();
            if (_cachedRemoteExtensions == null && cache.items.Count > 0)
            {
                var cachedList = cache.items.Select(i => i.info).ToList();
                // 恢复非序列化字段
                foreach (var item in cache.items)
                {
                    if (item.info != null)
                    {
                        string dirName = item.path.Split('/')[0];
                        item.info.downloadUrl = $"{CommunityUrl}/tree/main/{dirName}";
                        item.info.isInstalled = false;
                        item.info.remoteFolderName = dirName;
                    }
                }
                callback?.Invoke(cachedList);
            }

            // 2. 请求 GitHub Trees API
            string url = CommunityUrl.TrimEnd('/');
            // 默认使用 main 分支，如果需要支持 master，可能需要先检测默认分支
            string apiUrl = url.Replace("github.com", "api.github.com/repos") + "/git/trees/main?recursive=1";
            
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
                        var treeResponse = JsonUtility.FromJson<GitHubTreeResponse>(json);
                        
                        if (treeResponse == null || treeResponse.tree == null)
                        {
                            Debug.LogError("[EUExtensionManager] 解析 Trees API 响应失败");
                            return;
                        }

                        // 筛选出所有 extension.json
                        var extensionFiles = treeResponse.tree
                            .Where(t => t.path.EndsWith(ExtensionMarkerFile, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        if (extensionFiles.Count == 0)
                        {
                            callback?.Invoke(new List<EUExtensionInfo>());
                            return;
                        }

                        // 3. 对比缓存，找出需要更新的文件
                        var newCacheItems = new List<ExtensionCacheItem>();
                        var downloadQueue = new List<GitHubTreeItem>();

                        foreach (var file in extensionFiles)
                        {
                            var cachedItem = cache.items.FirstOrDefault(i => i.path == file.path);
                            if (cachedItem != null && cachedItem.sha == file.sha && cachedItem.info != null)
                            {
                                // 缓存命中且 SHA 一致，直接使用
                                newCacheItems.Add(cachedItem);
                            }
                            else
                            {
                                // 需要下载
                                downloadQueue.Add(file);
                            }
                        }

                        // 4. 并发下载更新
                        if (downloadQueue.Count == 0)
                        {
                            // 没有更新，直接使用缓存
                            UpdateCacheAndCallback(newCacheItems, callback);
                        }
                        else
                        {
                            int pending = downloadQueue.Count;
                            foreach (var file in downloadQueue)
                            {
                                string dirName = file.path.Split('/')[0];
                                string rawUrl = GetRawFileUrl(dirName, ExtensionMarkerFile, "main"); // Trees API 基于 main

                                var dlRequest = CreateRequest(rawUrl);
                                var dlOp = dlRequest.SendWebRequest();
                                dlOp.completed += __ =>
                                {
                                    if (dlRequest.result == UnityWebRequest.Result.Success)
                                    {
                                        try
                                        {
                                            var info = JsonUtility.FromJson<EUExtensionInfo>(dlRequest.downloadHandler.text);
                                            if (info != null)
                                            {
                                                newCacheItems.Add(new ExtensionCacheItem
                                                {
                                                    path = file.path,
                                                    sha = file.sha,
                                                    info = info
                                                });
                                            }
                                        }
                                        catch { }
                                    }
                                    
                                    pending--;
                                    if (pending == 0)
                                    {
                                        UpdateCacheAndCallback(newCacheItems, callback);
                                    }
                                };
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[EUExtensionManager] 处理 Trees API 响应错误: {e.Message}");
                    }
                }
                else
                {
                    // 如果 main 分支失败，尝试 master 分支 (简单的回退机制)
                    if (apiUrl.Contains("/main?"))
                    {
                        string masterUrl = apiUrl.Replace("/main?", "/master?");
                        var masterReq = CreateRequest(masterUrl);
                        var masterOp = masterReq.SendWebRequest();
                        masterOp.completed += __ => 
                        {
                            if (masterReq.result != UnityWebRequest.Result.Success)
                            {
                                Debug.LogError($"[EUExtensionManager] API 请求失败: {request.error}");
                            }
                            // 这里为了简化代码，不再递归重试解析逻辑，实际项目中可以提取公共逻辑
                        };
                    }
                    else
                    {
                        Debug.LogError($"[EUExtensionManager] API 请求失败: {request.error}");
                    }
                }
            };
        }

        private static void UpdateCacheAndCallback(List<ExtensionCacheItem> items, Action<List<EUExtensionInfo>> callback)
        {
            // 更新缓存文件
            var cacheData = new ExtensionCacheData
            {
                items = items,
                lastUpdateTime = DateTime.Now.Ticks
            };
            SaveCache(cacheData);

            // 准备结果列表
            var result = new List<EUExtensionInfo>();
            foreach (var item in items)
            {
                if (item.info != null)
                {
                    string dirName = item.path.Split('/')[0];
                    item.info.downloadUrl = $"{CommunityUrl}/tree/main/{dirName}";
                    item.info.isInstalled = false;
                    item.info.remoteFolderName = dirName;
                    result.Add(item.info);
                }
            }

            // 更新内存缓存
            _cachedRemoteExtensions = result;
            _lastFetchTime = DateTime.Now;

            callback?.Invoke(result);
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
            // 1. 初步检查直接依赖
            var localExtensions = GetAllLocalExtensions();
            bool hasMissing = false;
            
            if (info.dependencies != null)
            {
                foreach (var dep in info.dependencies)
                {
                    if (!IsDependencySatisfied(dep, localExtensions))
                    {
                        hasMissing = true;
                        break;
                    }
                }
            }

            if (!hasMissing)
            {
                onComplete?.Invoke(true);
                return;
            }

            // 2. 如果有缺失依赖，我们需要完整的解析。
            EditorUtility.DisplayProgressBar("依赖管理", "正在解析依赖信息...", 0.2f);

            FetchRemoteRegistry(remoteExtensions => 
            {
                EditorUtility.ClearProgressBar();
                
                // 3. 解析依赖树
                var installPlan = ResolveDependencyGraph(info, localExtensions, remoteExtensions);
                
                if (installPlan.Count > 0)
                {
                    string depNames = string.Join(", ", installPlan.Select(d => $"{d.DisplayName} (v{d.Version ?? "any"})"));
                    if (EditorUtility.DisplayDialog("安装依赖", 
                        $"扩展 {info.displayName} 需要安装以下依赖:\n{depNames}\n是否继续?", "安装", "取消"))
                    {
                        // 4. 执行批量安装
                        ExecuteInstallPlan(installPlan, 0, onComplete);
                    }
                    else
                    {
                        onComplete?.Invoke(false); // 用户取消
                    }
                }
                else
                {
                    onComplete?.Invoke(true);
                }
            });
        }

        private static bool IsDependencySatisfied(EUDependency dep, List<EUExtensionInfo> localExtensions)
        {
            var installedExt = localExtensions.FirstOrDefault(e => e.name == dep.name);
            if (installedExt == null)
            {
                // 检查路径
                if (!string.IsNullOrEmpty(dep.installPath))
                {
                    string path = dep.installPath;
                    if (path.StartsWith("Assets") || path.StartsWith("Packages")) 
                        path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
                    
                    if (Directory.Exists(path) && File.Exists(Path.Combine(path, ExtensionMarkerFile)))
                    {
                        // 尝试加载版本信息
                        var extInfo = LoadExtensionInfo(path);
                        if (extInfo != null) installedExt = extInfo;
                    }
                }
            }

            if (installedExt == null) return false;

            // 检查版本
            if (!string.IsNullOrEmpty(dep.version) && CompareVersion(installedExt.version, dep.version) < 0)
            {
                return false;
            }

            return true;
        }

        private class InstallItem
        {
            public string Name;
            public string Version;
            public string GitUrl;
            public string DisplayName;
            public bool IsUpgrade;
            public EUExtensionInfo RemoteInfo;
        }

        private static List<InstallItem> ResolveDependencyGraph(EUExtensionInfo root, List<EUExtensionInfo> local, List<EUExtensionInfo> remote)
        {
            var plan = new Dictionary<string, InstallItem>();
            var queue = new Queue<EUDependency>();
            var visited = new HashSet<string>();

            if (root.dependencies != null)
                foreach (var dep in root.dependencies) queue.Enqueue(dep);

            visited.Add(root.name);

            while (queue.Count > 0)
            {
                var dep = queue.Dequeue();
                if (visited.Contains(dep.name)) continue;
                visited.Add(dep.name);

                // 检查本地
                var localExt = local.FirstOrDefault(e => e.name == dep.name);
                bool needsInstall = false;
                bool isUpgrade = false;

                if (localExt == null)
                {
                    needsInstall = true;
                }
                else if (!string.IsNullOrEmpty(dep.version) && CompareVersion(localExt.version, dep.version) < 0)
                {
                    needsInstall = true;
                    isUpgrade = true;
                }

                if (!needsInstall) continue;

                // 查找远程信息
                var remoteExt = remote?.FirstOrDefault(e => e.name == dep.name);
                
                string displayName = dep.name;
                string version = dep.version;
                string gitUrl = dep.gitUrl;

                if (remoteExt != null)
                {
                    displayName = remoteExt.displayName;
                    // 如果远程有依赖信息，加入队列
                    if (remoteExt.dependencies != null)
                    {
                        foreach (var subDep in remoteExt.dependencies) queue.Enqueue(subDep);
                    }
                }
                else if (string.IsNullOrEmpty(gitUrl))
                {
                    Debug.LogWarning($"[EUExtensionManager] 无法解析依赖: {dep.name}，未找到远程信息且无 Git URL");
                    continue;
                }

                if (!plan.ContainsKey(dep.name))
                {
                    plan.Add(dep.name, new InstallItem 
                    {
                        Name = dep.name,
                        Version = version,
                        GitUrl = gitUrl,
                        DisplayName = displayName,
                        IsUpgrade = isUpgrade,
                        RemoteInfo = remoteExt
                    });
                }
            }

            return plan.Values.ToList();
        }

        private static void ExecuteInstallPlan(List<InstallItem> plan, int index, Action<bool> onComplete)
        {
            if (index >= plan.Count)
            {
                onComplete?.Invoke(true);
                return;
            }

            var item = plan[index];
            Action<bool> next = (success) => 
            {
                if (!success)
                {
                    Debug.LogError($"[EUExtensionManager] 安装依赖失败: {item.DisplayName}");
                    onComplete?.Invoke(false);
                }
                else
                {
                    ExecuteInstallPlan(plan, index + 1, onComplete);
                }
            };

            if (item.RemoteInfo != null)
            {
                // 使用远程信息安装
                DownloadExtensionViaZip(item.RemoteInfo, item.RemoteInfo.remoteFolderName, next);
            }
            else if (!string.IsNullOrEmpty(item.GitUrl))
            {
                // 使用 Git URL 安装
                var dep = new EUDependency { name = item.Name, gitUrl = item.GitUrl, version = item.Version };
                DownloadDependency(dep, next);
            }
            else
            {
                Debug.LogError($"[EUExtensionManager] 无法安装依赖 {item.Name}: 缺少下载源");
                next(false);
            }
        }

        private static string NormalizeUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return "";
            return url.Replace("https://", "").Replace("http://", "").Replace(".git", "").TrimEnd('/');
        }

        private static int CompareVersion(string v1, string v2)
        {
            if (string.IsNullOrEmpty(v1)) return -1;
            if (string.IsNullOrEmpty(v2)) return 1;
            
            try 
            {
                Version ver1 = new Version(v1);
                Version ver2 = new Version(v2);
                return ver1.CompareTo(ver2);
            }
            catch
            {
                return string.Compare(v1, v2, StringComparison.Ordinal);
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
                InstallExtensionFromSource(extensionSourceDir, dirName, null, info.downloadUrl);
                
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
                InstallExtensionFromSource(repoRoot, dep.name, dep.installPath, dep.gitUrl);
                
            }, onComplete);
        }

        private static void InstallExtensionFromSource(string sourceDir, string targetDirName, string explicitInstallPath, string sourceUrl = null)
        {
            string targetBase;
            string finalTargetDir = null;

            // 1. 如果显式指定了安装路径，优先使用
            if (!string.IsNullOrEmpty(explicitInstallPath))
            {
                string installPath = explicitInstallPath.Replace("\\", "/");
                if (installPath.StartsWith("Assets") || installPath.StartsWith("Packages"))
                {
                    targetBase = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                }
                else
                {
                    targetBase = Application.dataPath;
                }
                finalTargetDir = Path.Combine(targetBase, installPath);
            }
            else
            {
                // 2. 检查 extension.json 获取信息
                string jsonPath = Path.Combine(sourceDir, ExtensionMarkerFile);
                string category = "";
                string extensionName = targetDirName;

                if (File.Exists(jsonPath))
                {
                    try
                    {
                        string jsonContent = File.ReadAllText(jsonPath);
                        EUExtensionInfo info = JsonUtility.FromJson<EUExtensionInfo>(jsonContent);
                        if (info != null) 
                        {
                            category = info.category;
                            if (!string.IsNullOrEmpty(info.name)) extensionName = info.name;
                        }
                    }
                    catch { /* Ignore error, default path will be used */ }
                }

                // 检查是否已安装，如果已安装则覆盖原路径
                var localExtensions = GetAllLocalExtensions();
                var installedExt = localExtensions.FirstOrDefault(e => e.name == extensionName);
                
                if (installedExt != null && !string.IsNullOrEmpty(installedExt.folderPath) && Directory.Exists(installedExt.folderPath))
                {
                    finalTargetDir = installedExt.folderPath;
                }
                else
                {
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

                    finalTargetDir = Path.Combine(targetBase, targetDirName);
                }
            }

            InstallDirectory(sourceDir, finalTargetDir);

            // 4. 更新 sourceUrl
            if (!string.IsNullOrEmpty(sourceUrl))
            {
                UpdateExtensionSourceUrl(finalTargetDir, sourceUrl);
            }
        }

        private static void UpdateExtensionSourceUrl(string dir, string url)
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
                        info.sourceUrl = url;
                        string newJson = JsonUtility.ToJson(info, true);
                        File.WriteAllText(jsonPath, newJson);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EUExtensionManager] 更新 sourceUrl 失败: {e.Message}");
                }
            }
        }

        private static void InstallDirectory(string sourceDir, string targetDir)
        {
            // 安全覆盖安装逻辑：不直接删除整个目录，而是同步文件内容
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // 1. 复制/覆盖所有新文件
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, true);
            }

            // 2. 递归处理子目录
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(dir);
                string destDir = Path.Combine(targetDir, dirName);
                InstallDirectory(dir, destDir);
            }

            // 3. 清理旧文件（目标有但源没有的文件）
            // 注意：不要删除 .meta 文件，除非对应的源文件也没有
            var sourceFiles = new HashSet<string>(Directory.GetFiles(sourceDir).Select(Path.GetFileName));
            var sourceDirs = new HashSet<string>(Directory.GetDirectories(sourceDir).Select(Path.GetFileName));

            foreach (var file in Directory.GetFiles(targetDir))
            {
                string fileName = Path.GetFileName(file);
                if (fileName.EndsWith(".meta")) continue; // 跳过 meta 文件

                if (!sourceFiles.Contains(fileName))
                {
                    try { File.Delete(file); } catch { /* Ignore lock errors */ }
                    // 顺便尝试删除对应的 meta
                    string meta = file + ".meta";
                    if (File.Exists(meta)) try { File.Delete(meta); } catch { }
                }
            }

            foreach (var dir in Directory.GetDirectories(targetDir))
            {
                string dirName = Path.GetFileName(dir);
                if (!sourceDirs.Contains(dirName))
                {
                    try { Directory.Delete(dir, true); } catch { /* Ignore lock errors */ }
                    string meta = dir + ".meta";
                    if (File.Exists(meta)) try { File.Delete(meta); } catch { }
                }
            }
        }

        private static void DownloadAndExtractZip(string url, string progressTitle, Action<string> onExtracted, Action<bool> onComplete)
        {
            EditorUtility.DisplayProgressBar("下载中", progressTitle, 0.1f);
            
            var request = CreateRequest(url);
            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    EditorUtility.ClearProgressBar();
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
