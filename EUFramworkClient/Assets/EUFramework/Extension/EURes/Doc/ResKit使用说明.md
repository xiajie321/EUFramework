# ResKit 使用说明

## 📖 简介

**ResKit** 是 EUFramework 的资源管理扩展模块，基于 YooAsset 构建，提供资源加载和热更新功能。

### 🔌 插拔式设计

ResKit 采用完全独立的插拔式设计：
- ✅ **零耦合**: 不依赖 EUFramework 其他模块
- ✅ **易集成**: 复制文件夹到项目即可使用
- ✅ **易移除**: 删除文件夹不影响其他系统
- ✅ **自包含**: 配置、代码、资源完全独立

## 🔧 ResKit 编辑器工具

打开方式：`菜单栏 → EUFramework → 拓展 → ResKit 配置工具`

### 1. 配置文件面板

**作用**：管理资源相关的配置文件

- **AssetBundleCollectorSetting**: 配置哪些资源需要打包
- **ResKitPackageConfig**: 配置资源包的运行模式（编辑器模拟/离线/联机/WebGL）
- **ResServerConfig**: 配置资源服务器地址（用于热更新）
- **YooAssetSettings**: YooAsset 的全局设置

**使用步骤**：
1. 点击 "创建配置文件" 创建所需配置
2. 点击 "配置资源收集" 添加要打包的资源
3. 点击 "同步 Packages" 同步包配置
4. 选择每个包的运行模式并保存

### 2. ResFacade 面板

**作用**：生成资源管理所需的代码和 UI

- **生成 UI Prefab**: 生成下载进度和用户交互界面
- **生成 ResKit 分部类**: 生成资源管理 API 代码
- **刷新程序集引用**: 修复编译错误

**使用步骤**：
1. 点击 "生成 UI Prefab 和脚本"
2. 点击 "生成 ResKit 分部类（同时生成两个文件）"

## 💻 代码使用

### 初始化资源系统

在游戏启动时调用初始化方法：

```csharp
using EUFramework.Extension.EURes;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameLauncher : MonoBehaviour
{
    private async void Start()
    {
        // 初始化所有资源包
        bool success = await ResKit.InitializeAllPackagesAsync();
        
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
    await ResKit.InitializeAllPackagesAsync(
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
        ResKit.SetDownloadProgressCallback(OnDownloadProgress);
        
        // 开始初始化
        await ResKit.InitializeAllPackagesAsync(
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
var handle = ResKit.GetPackage().LoadAssetAsync<GameObject>("Assets/Prefabs/Player.prefab");
await handle.ToUniTask();
GameObject player = handle.AssetObject as GameObject;
Instantiate(player);

// 使用完毕释放资源
handle.Release();
```

## 🎮 运行模式说明

在 ResKit 配置工具中可以为每个包选择运行模式：

| 模式 | 说明 | 适用场景 |
|------|------|----------|
| **EditorSimulateMode** | 编辑器模拟模式，直接从 Assets 加载 | 开发测试 |
| **OfflinePlayMode** | 离线模式，资源打包在应用内 | 单机游戏 |
| **HostPlayMode** | 联机模式，支持热更新 | 线上游戏 |
| **WebPlayMode** | WebGL 模式 | 网页游戏 |

> 💡 使用 **HostPlayMode** 或 **WebPlayMode** 时，需要在 ResServerConfig 中配置 CDN 地址。

## ❓ 常见问题

### Q: 编译错误找不到 YooAsset 或 UniTask？

**A**: 在 ResKit 配置工具的 "ResFacade" 面板，点击 "刷新程序集引用"

### Q: 如何添加新的资源包？

**A**: 
1. 在 ResKit 配置工具点击 "配置资源收集"
2. 在 YooAsset 收集器中添加新的 Package
3. 回到 ResKit 配置工具，点击 "同步 Packages"

### Q: 资源加载失败？

**A**: 
1. 检查资源路径是否正确（需要使用完整路径，如 `Assets/...`）
2. 确认资源已添加到 AssetBundleCollector
3. 确认包已正确初始化

---


更多详细信息请参考 YooAsset 官方文档：https://www.yooasset.com/
