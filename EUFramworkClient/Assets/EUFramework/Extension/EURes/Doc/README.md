# EUResKit 使用说明

## 简介

**EUResKit** 是基于 YooAsset 构建的资源管理模块，提供资源加载和热更新功能。

### 插拔式设计

EUResKit 采用独立的插拔式设计，真正做到"复制即用"：
- **零模块耦合**: 不依赖其他业务模块
- **路径自适应**: 可放置在项目任意位置
- **命名空间自动**: 根据文件夹路径自动生成命名空间
- **易集成易移除**: 复制文件夹到项目即可使用，删除不影响其他系统

## 依赖项

- **YooAsset**: 核心底层依赖 (需通过 UPM 或 Package 安装)
- **UniTask**: 异步处理依赖

## 目录结构

```
EURes/                   # 模块根目录（可以放在任意位置）
├── Editor/              # 编辑器工具
├── Script/              # 运行时代码
├── Resources/           # 配置文件和 UI 资源
└── Doc/                 # 文档

EUResources/             # 资源根目录（固定在 Assets/EUResources）
├── Builtin/             # 内置资源
├── Excluded/            # 不打包资源
└── Remote/              # 热更新资源
```

## 快速开始

### 1. 初始化资源系统

在游戏启动时调用初始化方法。系统会自动检测更新并弹出内置的更新 UI（如果需要）。

```csharp
using EUFramework.Extension.EURes; // 根据实际命名空间修改
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameLauncher : MonoBehaviour
{
    private async void Start()
    {
        // 可选：设置下载进度回调（用于自定义 Loading 条）
        EUResKit.SetDownloadProgressCallback((packageName, totalCount, currentCount, totalBytes, currentBytes) =>
        {
             float progress = (float)currentBytes / totalBytes;
             Debug.Log($"[{packageName}] 下载进度: {progress * 100:F1}%");
        });

        // 初始化所有资源包
        // 如果检测到更新，会自动弹出内置 UI 提示用户下载
        bool success = await EUResKit.InitializeAllPackagesAsync();
        
        if (success)
        {
            Debug.Log("资源初始化成功");
            StartGame();
        }
        else
        {
            Debug.LogError("资源初始化失败");
        }
    }
}
```

### 2. 加载资源

初始化完成后，就可以加载资源了：

```csharp
using YooAsset;

// 异步加载预制体
var handle = EUResKit.GetPackage().LoadAssetAsync<GameObject>("Assets/Prefabs/Player.prefab");
await handle.ToUniTask();

if (handle.Status == EOperationStatus.Succeed)
{
    GameObject player = handle.AssetObject as GameObject;
    Instantiate(player);
}

// 使用完毕释放资源
handle.Release();
```

## 功能特性

### 内置热更新 UI
模块内置了 `EUResKitUserOpePopUp` 预制体，能够自动处理：
- 初始化失败提示与重试
- 版本检查失败提示与重试
- 清单更新失败提示与重试
- 发现新版本提示下载（显示大小）
- 下载错误提示与重试

### 进度回调
通过 `SetDownloadProgressCallback` 可以获取详细的下载进度信息，方便对接自定义的 Loading 界面。

## 配置工具

打开方式：`菜单栏 -> EUFramework -> 拓展 -> EUResKit 配置工具`

### 资源配置面板
管理资源目录结构和配置文件。
- **一键生成目录结构**：自动创建标准的目录结构。
- **配置文件管理**：管理 YooAsset 相关配置。

### 代码生成面板
生成资源管理代码和开发工具。
- **刷新命名空间**：当模块位置改变时，自动更新命名空间和 asmdef。
- **生成 UI Prefab**：生成下载进度界面。
- **刷新程序集引用**：解决 YooAsset 或 UniTask 引用丢失问题。

## 文档说明

- **API文档**：请查阅 [API.md](API.md) 获取详细的接口说明。
- **更新日志**：请查阅 [Update.md](Update.md) 获取版本更新历史。

## 常见问题

**Q: 编译错误找不到 YooAsset 或 UniTask？**
A: 在 EUResKit 配置工具的 "代码生成" 面板，点击 "刷新程序集引用"。确保项目中已安装这两个库。

**Q: 如何添加新的资源包？**
A: 在配置工具中点击 "配置资源收集"，在 YooAsset Collector 中添加，然后点击 "同步 Packages"。
