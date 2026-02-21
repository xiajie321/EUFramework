# EUResKit API 文档

## EUResKit 类

`EUFramework.Extension.EURes.EUResKit`

资源管理核心入口类，基于 YooAsset 封装。

### 属性

- `static bool IsInitialized`: 资源系统是否已初始化完成。

### 方法

#### 初始化

- `static UniTask<bool> InitializeAllPackagesAsync(Action<string, bool> onPackageInitialized = null, Action<bool> onAllCompleted = null)`
  - 初始化所有在配置文件中定义的资源包。
  - **自动热更新**：初始化过程中会自动检查资源版本、下载清单、并弹出内置 UI 提示用户下载更新（如果存在）。
  - `onPackageInitialized`: 单个包初始化完成的回调 `(packageName, isSuccess)`。
  - `onAllCompleted`: 所有包处理完成后的回调 `(allSuccess)`。

- `static void SetDownloadProgressCallback(Action<string, int, int, long, long> callback)`
  - 设置全局下载进度回调，用于自定义 Loading 界面。
  - 回调参数：
    - `packageName`: 当前下载的包名
    - `totalCount`: 总文件数
    - `currentCount`: 当前已下载文件数
    - `totalBytes`: 总字节数
    - `currentBytes`: 当前已下载字节数

#### 资源包获取

- `static ResourcePackage GetPackage(string packageName = null)`
  - 获取指定的资源包对象 (`YooAsset.ResourcePackage`)。
  - `packageName`: 包名。如果为 `null`，则返回默认包（通常是第一个初始化成功的包）。
  - **返回值**：返回 `ResourcePackage` 对象，可用于调用 `LoadAssetAsync` 等方法。

#### 工具方法

- `static string GetPackageVersion(string packageName)`
  - 获取指定包的当前版本号。

## 常见操作示例

### 加载资源

```csharp
// 1. 获取包
var package = EUResKit.GetPackage("DefaultPackage");

// 2. 加载资源 (异步)
var handle = package.LoadAssetAsync<GameObject>("Assets/Game/Prefabs/Player.prefab");
await handle.ToUniTask();

// 3. 实例化
if (handle.Status == EOperationStatus.Succeed)
{
    var prefab = handle.AssetObject as GameObject;
    Instantiate(prefab);
}

// 4. 释放
handle.Release();
```

### 切换场景

```csharp
var sceneHandle = package.LoadSceneAsync("Assets/Game/Scenes/GameScene");
await sceneHandle.ToUniTask();
// 注意：场景资源通常不需要手动释放，切换新场景时会自动释放
```

## 内置 UI (EUResKitUserOpePopUp)

系统内置了一个 `EUResKitUserOpePopUp` 预制体（位于 `Resources/EUResKitUI/`），用于处理热更新过程中的用户交互：
- **初始化失败**：提示重试。
- **版本检查失败**：提示检查网络并重试。
- **发现更新**：显示更新大小，询问用户是否下载。
- **下载失败**：提示文件下载失败并重试。

该 UI 会自动加载，无需手动调用。
