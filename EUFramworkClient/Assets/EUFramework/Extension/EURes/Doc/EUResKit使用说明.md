# EUResKit 使用说明

## 简介

**EUResKit** 是基于 YooAsset 构建的资源管理模块，提供资源加载和热更新功能。

### 插拔式设计

EUResKit 采用独立的插拔式设计，真正做到"复制即用"：
- **零模块耦合**: 不依赖其他业务模块
- **路径自适应**: 可放置在项目任意位置
- **命名空间自动**: 根据文件夹路径自动生成命名空间
- **易集成易移除**: 复制文件夹到项目即可使用，删除不影响其他系统

### 依赖说明

**必需依赖**（需要自行安装）：
- Unity 2021.3 或更高版本
- YooAsset（资源管理框架）
- UniTask（异步任务库）

**文件结构**：
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

## EUResKit 编辑器工具

打开方式：`菜单栏 → EUFramework → 拓展 → EUResKit 配置工具`

### 1. 资源配置面板

**作用**：管理资源目录结构和配置文件

#### 资源目录管理
- **一键生成目录结构**：自动创建标准的 `Builtin`、`Excluded`、`Remote` 三层目录
- **自动创建 Package**：在 YooAsset Collector 中创建 Builtin 和 Remote 两个 Package
- **实时状态显示**：显示目录和 Package 的创建状态

#### 配置文件管理
- **AssetBundleCollectorSetting**：配置哪些资源需要打包
- **EUResKitPackageConfig**：配置资源包的运行模式（编辑器模拟/离线/联机/WebGL）
- **EUResServerConfig**：配置资源服务器地址（用于热更新）
- **YooAssetSettings**：YooAsset 的全局设置

**首次使用步骤**：
1. 点击 "一键生成目录结构与配置" 创建标准目录和 Package
2. 点击 "创建配置文件" 创建所需的 ScriptableObject 配置
3. 打开 YooAsset Collector 窗口，为 Package 添加 Group 和 Collector
4. 点击 "同步 Packages" 同步包配置到 EUResKitPackageConfig
5. 选择每个包的运行模式并保存

### 2. 代码生成面板

**作用**：生成资源管理代码和开发工具

#### UI Prefab 和脚本
- **生成 UI Prefab**: 生成下载进度和用户交互界面
- **EUResKitUserOpePopUp**: 用户可自定义的交互弹窗

#### EUResKit 分部类（Partial Class）
- **EUResKit.Generated.cs**: 自动生成的基础工具类（可重新生成）
- **EUResKit.cs**: 用户编辑的业务逻辑类（请勿覆盖）

#### 程序集引用管理
- **刷新程序集引用**: 解决 YooAsset 和 UniTask 引用丢失问题

#### 模块管理工具（新功能）
- **刷新命名空间**: 当模块位置改变时，自动更新命名空间和 asmdef
- **删除所有生成的文件**: 清理所有生成的代码和资源文件

**首次使用步骤**：
1. 点击 "生成 UI Prefab 和脚本"
2. 点击 "生成 EUResKit 分部类（同时生成两个文件）"
3. 查看底部显示的当前模块位置和命名空间

## 代码使用

**注意**：示例代码中的命名空间 `EUFramework.Extension.EURes` 仅为示例。实际命名空间根据您的模块位置自动生成（排除 Assets 后的文件夹路径）。例如：`Assets/Plugins/EUResKit/` → `Plugins.EUResKit`

### 初始化资源系统

在游戏启动时调用初始化方法：

```csharp
using EUFramework.Extension.EURes; // 根据实际命名空间修改
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameLauncher : MonoBehaviour
{
    private async void Start()
    {
        // 初始化所有资源包
        bool success = await EUResKit.InitializeAllPackagesAsync();
        
        if (success)
        {
            Debug.Log("资源初始化成功");
            // 进入游戏
            StartGame();
        }
        else
        {
            Debug.LogError("资源初始化失败");
        }
    }
}
```

### 带回调的初始化

如果需要监听初始化过程，可以使用回调参数：

```csharp
private async void Start()
{
    await EUResKit.InitializeAllPackagesAsync(
        // 回调1：每个包初始化完成时触发
        onPackageInitialized: (packageName, isSuccess) =>
        {
            Debug.Log($"包 {packageName} 初始化: {(isSuccess ? "成功" : "失败")}");
        },
        // 回调2：所有包初始化完成时触发
        onAllCompleted: (allSuccess) =>
        {
            if (allSuccess)
            {
                Debug.Log("所有包初始化完成");
                StartGame();
            }
        }
    );
}
```

### 监听下载进度

如果需要显示下载进度条，可以设置进度回调：

```csharp
using UnityEngine.UI;

public class LoadingUI : MonoBehaviour
{
    public Slider progressBar;
    public Text progressText;
    
    private async void Start()
    {
        // 设置下载进度回调
        EUResKit.SetDownloadProgressCallback(OnDownloadProgress);
        
        // 开始初始化
        await EUResKit.InitializeAllPackagesAsync(
            onAllCompleted: (success) =>
            {
                if (success)
                {
                    progressText.text = "加载完成";
                }
            }
        );
    }
    
    private void OnDownloadProgress(string packageName, int totalCount, int currentCount, long totalBytes, long currentBytes)
    {
        // 计算进度百分比
        float progress = (float)currentBytes / totalBytes;
        
        // 更新进度条
        progressBar.value = progress;
        progressText.text = $"下载中: {progress:P0}";
    }
}
```

**回调参数说明**：
- `packageName`: 当前下载的资源包名称
- `totalCount`: 总文件数
- `currentCount`: 当前已下载文件数
- `totalBytes`: 总字节数
- `currentBytes`: 当前已下载字节数

### 加载资源

初始化完成后，就可以加载资源了：

```csharp
using YooAsset;

// 异步加载预制体
var handle = EUResKit.GetPackage().LoadAssetAsync<GameObject>("Assets/Prefabs/Player.prefab");
await handle.ToUniTask();
GameObject player = handle.AssetObject as GameObject;
Instantiate(player);

// 使用完毕释放资源
handle.Release();
```

## 运行模式说明

在 EUResKit 配置工具中可以为每个包选择运行模式：

| 模式 | 说明 | 适用场景 |
|------|------|----------|
| **EditorSimulateMode** | 编辑器模拟模式，直接从 Assets 加载 | 开发测试 |
| **OfflinePlayMode** | 离线模式，资源打包在应用内 | 单机游戏 |
| **HostPlayMode** | 联机模式，支持热更新 | 线上游戏 |
| **WebPlayMode** | WebGL 模式 | 网页游戏 |

**注意**：使用 **HostPlayMode** 或 **WebPlayMode** 时，需要在 EUResServerConfig 中配置 CDN 地址。

## 迁移和部署指南

### 将 EUResKit 迁移到新项目

1. **复制模块文件夹**
   - 将整个 `EURes` 文件夹复制到新项目的任意位置
   - 例如：`Assets/Plugins/EUResKit/`、`Assets/Tools/ResourceManager/` 等

2. **刷新命名空间**
   - 打开 EUResKit 配置工具
   - 进入 "代码生成" 面板
   - 点击 "刷新命名空间"
   - 选择是否重新生成代码文件

3. **安装依赖**
   - 确保项目已安装 YooAsset 和 UniTask

4. **完成**
   - 查看配置工具底部显示的当前命名空间
   - 在代码中使用新的命名空间即可

### 清理和重置

如果需要清理所有生成的文件：

1. 打开 EUResKit 配置工具 → 代码生成面板
2. 点击 "删除所有生成的文件"
3. 选择删除范围：
   - **仅删除代码和UI**：保留配置文件（推荐）
   - **完全清理**：删除包括配置在内的所有生成内容
4. 确认删除

## 常见问题

### Q: 编译错误找不到 YooAsset 或 UniTask？

**A**: 在 EUResKit 配置工具的 "代码生成" 面板，点击 "刷新程序集引用"

### Q: 如何添加新的资源包？

**A**: 
1. 在 EUResKit 配置工具点击 "配置资源收集"
2. 在 YooAsset 收集器中添加新的 Package
3. 回到 EUResKit 配置工具，点击 "同步 Packages"

### Q: 资源加载失败？

**A**: 
1. 检查资源路径是否正确（需要使用完整路径，如 `Assets/...`）
2. 确认资源已添加到 AssetBundleCollector
3. 确认包已正确初始化

### Q: 将模块移动到新位置后出现编译错误？

**A**: 
1. 打开 EUResKit 配置工具
2. 进入 "代码生成" 面板
3. 点击 "刷新命名空间"，会自动更新 asmdef 和重新生成代码
4. 等待编译完成

### Q: 当前使用的命名空间是什么？

**A**: 
- 打开 EUResKit 配置工具 → 代码生成面板
- 查看底部的"当前命名空间"信息
- 命名空间根据模块路径自动生成（排除 Assets 的文件夹路径）

### Q: 如何自定义弹窗 UI？

**A**: 
1. 生成 UI 后，编辑 `Script/EUResKitUserOpePopUp.cs` 脚本
2. 修改 `Resources/EUResKitUI/EUResKitUserOpePopUp.prefab` 预制体
3. 这两个文件都是用户可编辑的，不会被覆盖

---

## 更多资源

- YooAsset 官方文档：https://www.yooasset.com/
- UniTask 文档：https://github.com/Cysharp/UniTask
