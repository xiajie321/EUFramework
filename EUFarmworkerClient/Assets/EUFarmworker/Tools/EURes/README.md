# EURes 资源加载模块 - 保姆级使用文档

EURes 是一个基于 YooAsset (v2.3+) 封装的资源加载模块，旨在提供统一的资源加载接口，支持 UniTask 异步流，并提供可视化的配置管理。

本文档将手把手教你如何使用 EURes，从最简单的单机模式到完整的热更新环境搭建，以及 Mod 系统的使用。

---

## 目录

1.  [简介](#1-简介)
2.  [快速上手：配置面板](#2-快速上手配置面板)
3.  [场景一：单机/开发模式 (无需服务器)](#3-场景一单机开发模式-无需服务器)
4.  [场景二：保姆级热更教程 (含服务器搭建)](#4-场景二保姆级热更教程-含服务器搭建)
5.  [场景三：本地加载与 Mod 系统](#5-场景三本地加载与-mod-系统)
    *   [Mod 制作流程](#mod-制作流程)
    *   [加载 Mod](#加载-mod)
6.  [进阶：清单与版本管理](#6-进阶清单与版本管理)
7.  [API 参考](#7-api-参考)

---

## 1. 简介

EURes 解决了 Unity 原生 Resources 加载与 AssetBundle 加载割裂的问题。
*   **开发时**：使用 `EditorSimulateMode`，无需打包，直接运行，修改资源即时生效。
*   **发布时**：使用 `OfflinePlayMode` (单机) 或 `HostPlayMode` (热更)，享受 AssetBundle 的高性能和热更能力。

---

## 2. 快速上手：配置面板

所有配置都在一个可视化的面板中完成。

1.  在 Unity 菜单栏点击：`EUFarmworker` -> `EURes` -> `Config Panel`。
2.  如果提示“未找到配置文件”，点击“创建配置文件”按钮。
3.  你将看到以下设置：
    *   **运行模式**：决定游戏如何加载资源。
    *   **默认资源服务器**：热更模式下下载资源的地址。
    *   **资源包设置**：管理主包 (DefaultPackage) 和 DLC/Mod 包。

---

## 3. 场景一：单机/开发模式 (无需服务器)

适用于：
*   刚开始开发游戏。
*   做完全单机的游戏（所有资源打进包体）。

### 设置步骤

1.  **开发阶段**：
    *   打开 Config Panel。
    *   将 `运行模式` 设置为 **EditorSimulateMode**。
    *   无需关心服务器地址。
    *   直接运行游戏即可。

2.  **打真机包 (单机)**：
    *   打开 Config Panel。
    *   将 `运行模式` 设置为 **OfflinePlayMode**。
    *   **重要**：你需要先使用 YooAsset 构建资源，并将构建结果复制到 `StreamingAssets` 目录（YooAsset 提供了“内置构建管线”来自动处理这个，详见 YooAsset 文档）。
    *   打包 APK/IPA。

---

## 4. 场景二：保姆级热更教程 (含服务器搭建)

适用于：
*   需要在线更新资源的游戏。
*   需要分包下载 (DLC) 的游戏。
*   WebGL 游戏。

### 第一步：资源打包

在使用热更模式前，必须先将资源打包成 AssetBundle。

1.  **打开 YooAsset 收集器**：`YooAsset` -> `AssetBundle Collector`。
2.  **配置包**：
    *   默认有一个 `DefaultPackage`。
    *   在 `Groups` 下添加一个组，例如 `GameRes`。
    *   将你的资源文件夹（如 `Assets/Art/Prefabs`）拖入收集器。
3.  **打开 YooAsset 构建器**：`YooAsset` -> `AssetBundle Builder`。
4.  **构建设置**：
    *   **Build Pipeline**: `BuiltinBuildPipeline` (或 ScriptableBuildPipeline)。
    *   **Build Mode**: `ForceRebuild` (第一次) 或 `IncrementalBuild` (后续更新)。
    *   **Build Version**: 输入一个版本号，例如 `1.0`。
    *   点击 **Build**。
5.  **构建结果**：
    *   构建完成后，会在项目目录下的 `Bundles` 文件夹生成资源包。
    *   路径通常是：`Bundles/StandaloneWindows64/DefaultPackage/1.0/` (取决于你的平台和版本)。

### 第二步：搭建本地资源服务器

为了测试热更，我们需要一个简单的 HTTP 服务器。推荐使用 **HFS (HTTP File Server)**，它只有一个 exe 文件，无需安装，即点即用。

1.  **下载 HFS**：[官网下载](https://www.rejetto.com/hfs/) (Windows)。
2.  **运行 HFS**。
3.  **创建根目录**：
    *   在你的电脑任意位置创建一个文件夹，命名为 `CDN`。
    *   将这个 `CDN` 文件夹拖入 HFS 的左侧窗口（Virtual File System）。
    *   选择 **Real folder** (实文件夹)。
4.  **获取地址**：
    *   HFS 顶部地址栏会显示你的本地 IP，例如 `http://192.168.1.100/`。
    *   你的 CDN 地址就是 `http://192.168.1.100/CDN`。

### 第三步：部署资源

将打包好的资源放到服务器上。

1.  找到刚才 YooAsset 构建生成的文件夹：`Bundles/StandaloneWindows64/DefaultPackage/1.0/`。
2.  将 `StandaloneWindows64` 整个文件夹复制到你的 `CDN` 文件夹中。
3.  现在的目录结构应该是：
    ```
    CDN/
      └── StandaloneWindows64/
            └── DefaultPackage/
                  ├── 1.0/
                  │     ├── OutputCache.js
                  │     ├── PackageManifest_1.0.hash
                  │     ├── PackageManifest_1.0.json
                  │     └── ... (AssetBundles)
                  └── ...
    ```

### 第四步：客户端配置

1.  回到 Unity，打开 `EURes Config Panel`。
2.  **运行模式**：选择 **HostPlayMode**。
3.  **默认资源服务器**：
    *   填写你的 HFS 地址加上平台目录。
    *   例如：`http://192.168.1.100/CDN/StandaloneWindows64`。

### 第五步：代码实现与测试

在游戏启动脚本中编写更新逻辑。

```csharp
using Cysharp.Threading.Tasks;
using EUFarmworker.Tools.EURes.Script;
using UnityEngine;

public class GameInit : MonoBehaviour
{
    async void Start()
    {
        // 1. 初始化
        var resUtility = new EUResUtility();
        resUtility.Init();
        
        // 2. 初始化包 (HostPlayMode 下这步只是准备环境)
        await resUtility.InitializePackagesAsync();

        // 3. 开始更新流程
        await CheckUpdate();
        
        // 4. 加载资源
        var loader = ResLoader.Allocate();
        var prefab = await loader.LoadAsync<GameObject>("Assets/Art/Prefabs/MyCube.prefab"); // 使用全路径
        Instantiate(prefab);
    }

    private async UniTask CheckUpdate()
    {
        var package = EUResUtility.Instance.GetPackage("DefaultPackage");
        
        // A. 获取远端最新版本
        var (success, version) = await package.UpdatePackageVersionAsync();
        if (!success) 
        {
            Debug.LogError("无法连接服务器或获取版本失败！");
            return;
        }
        Debug.Log($"发现新版本: {version}");

        // B. 更新资源清单
        bool manifestUpdated = await package.UpdatePackageManifestAsync(version);
        if (!manifestUpdated)
        {
            Debug.LogError("更新清单失败！");
            return;
        }

        // C. 创建下载器
        var downloader = package.CreateResourceDownloader();
        
        // D. 检查下载量
        if (downloader.TotalDownloadCount > 0)
        {
            Debug.Log($"需要下载: {downloader.TotalDownloadCount} 个文件, 总大小: {downloader.TotalDownloadBytes / 1024.0f / 1024.0f:F2} MB");
            
            // E. 开始下载
            downloader.BeginDownload();
            await downloader.ToUniTask(); // 等待下载完成
            
            if (downloader.Status != YooAsset.EOperationStatus.Succeed)
            {
                Debug.LogError($"下载失败: {downloader.LastError}");
                return;
            }
        }
        
        Debug.Log("更新完成，进入游戏！");
    }
}
```

---

## 5. 场景三：本地加载与 Mod 系统

适用于：
*   单机游戏加载本地指定文件夹的资源。
*   支持玩家自制 Mod（包含资源和代码）。

### Mod 制作流程

1.  **创建 Mod 包**：
    *   在 YooAsset AssetBundle Collector 中新建一个 Package，例如 `MyMod`。
    *   将 Mod 的资源放入该 Package。
    *   构建资源包。
2.  **编写 Mod 代码 (可选)**：
    *   创建一个新的 C# 类库项目。
    *   编写 Mod 逻辑。
    *   编译生成 DLL 文件。
3.  **组装 Mod**：
    *   创建一个文件夹，例如 `MyMod_v1.0`。
    *   将构建好的 AssetBundle 文件（包含清单文件）复制到该文件夹。
    *   将编译好的 DLL 文件也复制到该文件夹。

### 加载 Mod

你可以使用 `EUModManager` 来加载指定目录下的 Mod。

```csharp
using Cysharp.Threading.Tasks;
using EUFarmworker.Tools.EURes.Script;
using UnityEngine;

public class ModLoader : MonoBehaviour
{
    async void Start()
    {
        // 假设 Mod 放在 persistentDataPath/Mods 下
        string modsRoot = System.IO.Path.Combine(Application.persistentDataPath, "Mods");
        
        // 加载所有 Mod
        await EUModManager.LoadAllModsAsync(modsRoot);
        
        Debug.Log("所有 Mod 加载完成");
        
        // 现在你可以像加载普通资源一样加载 Mod 资源了
        // 注意：需要指定 Mod 的包名 (通常是 Mod 文件夹名)
        var loader = ResLoader.Allocate(ResLoadMode.YooAsset, "MyMod");
        var modPrefab = await loader.LoadAsync<GameObject>("Assets/Mods/MyMod/Hero.prefab");
        Instantiate(modPrefab);
    }
}
```

### 配置面板设置 (可选)

如果你只是想在开发时测试本地加载功能，也可以在 `Config Panel` 中配置：
1.  在 `额外资源包列表` 中添加一个包。
2.  勾选 `LoadFromLocal`。
3.  在 `LocalPath` 中填写本地资源的绝对路径。

---

## 6. 进阶：清单与版本管理

### 什么是清单 (Manifest)?
清单文件（通常是 `.json` 或二进制文件）就像一个快递单，它记录了：
*   当前版本有哪些资源文件。
*   每个文件的大小、哈希值（用于校验）。
*   文件之间的依赖关系。

### 清单怎么看？
在构建输出目录中，你会看到类似 `PackageManifest_1.0.json` 的文件。打开它，你可以看到：
*   `AssetList`: 包含所有资源的列表。
*   `BundleList`: 包含所有 AssetBundle 的列表。

### 版本更新原理
1.  **UpdatePackageVersionAsync**: 客户端向服务器请求“最新版本号是多少？”。服务器通常通过一个静态文件（如 `version.txt` 或通过列出目录）告诉客户端最新版本是 `1.1`。
2.  **UpdatePackageManifestAsync**: 客户端下载 `1.1` 版本的清单文件。
3.  **CreateResourceDownloader**: YooAsset 对比本地清单（比如 `1.0`）和新清单（`1.1`）。
    *   如果 `1.1` 里有个文件 Hash 变了，说明资源更新了 -> 加入下载列表。
    *   如果 `1.1` 里新增了文件 -> 加入下载列表。
    *   如果 `1.1` 里删除了文件 -> 标记为可清理。

---

## 7. API 参考

### EUResUtility
*   `InitializePackagesAsync()`: 初始化所有配置的包。
*   `LoadPackageAsync(name, server, localPath)`: 动态加载额外的包（DLC/Mod）。
*   `GetPackage(name)`: 获取已初始化的包对象。

### EUModManager
*   `LoadAllModsAsync(rootPath)`: 加载指定目录下的所有 Mod。
*   `LoadModAsync(modPath)`: 加载单个 Mod。

### EUResPackage
*   `UpdatePackageVersionAsync()`: 获取远端版本。
*   `UpdatePackageManifestAsync(version)`: 更新清单。
*   `CreateResourceDownloader()`: 创建下载器。
*   `ClearCacheFilesAsync()`: 清理缓存。

### ResLoader
*   `Allocate()`: 获取加载器实例。
*   `LoadSync<T>(path)`: 同步加载。
*   `LoadAsync<T>(path, callback)`: 异步加载（回调）。
*   `LoadScene(path)`: 加载场景。
*   `ReleaseAll()`: 释放资源。

---

## 常见问题 (FAQ)

**Q: 为什么 HostPlayMode 下提示下载失败？**
A: 
1. 检查 HFS 是否开启。
2. 检查浏览器能否直接访问你的资源文件 URL。
3. 检查 `EUResConfig` 中的 `DefaultHostServer` 是否正确拼接了路径。
4. 检查 Unity 控制台报错，通常会有 404 (找不到文件) 或 500 (服务器错误)。

**Q: 为什么修改了资源，EditorSimulateMode 下没变化？**
A: EditorSimulateMode 默认模拟构建结果。如果你新增了文件但没有在 AssetBundle Collector 中配置，或者没有重新模拟构建（YooAsset 菜单里有 Simulate Build），可能无法加载。通常建议直接使用 EditorSimulateMode，它会自动处理大部分情况。

**Q: 如何做 DLC？**
A: 
1. 在 AssetBundle Collector 中新建一个 Package，例如 `DLC1`。
2. 构建 `DLC1`。
3. 在 `EURes Config Panel` 的 `额外资源包列表` 中添加 `DLC1`。
4. 代码中使用 `EUResUtility.Instance.LoadPackageAsync("DLC1")` 加载它。
