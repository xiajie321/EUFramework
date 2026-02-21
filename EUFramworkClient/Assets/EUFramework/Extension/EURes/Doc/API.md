# EUResKit API 文档

## EUResKit 类

`EUFramework.Extension.EURes.EUResKit`

资源管理核心入口类。

### 属性

- `static bool IsInitialized`: 资源系统是否已初始化。

### 方法

#### 初始化
- `static UniTask<bool> InitializeAllPackagesAsync(Action<string, bool> onPackageInitialized = null, Action<bool> onAllCompleted = null)`: 初始化所有资源包。
- `static void SetDownloadProgressCallback(Action<string, int, int, long, long> callback)`: 设置下载进度回调。

#### 资源包获取
- `static ResourcePackage GetPackage(string packageName = null)`: 获取指定的资源包。如果不传参数，默认获取第一个包。

#### 工具方法
- `static string GetPackageVersion(string packageName)`: 获取包版本。

## 回调定义

### 下载进度回调
`Action<string packageName, int totalCount, int currentCount, long totalBytes, long currentBytes>`

- `packageName`: 包名
- `totalCount`: 总文件数
- `currentCount`: 当前文件数
- `totalBytes`: 总字节数
- `currentBytes`: 当前字节数
