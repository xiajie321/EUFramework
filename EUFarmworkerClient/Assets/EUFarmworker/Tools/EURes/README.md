# EURes 资源加载模块

EURes 是一个基于 YooAsset 封装的资源加载模块，旨在提供统一的资源加载接口，并支持在开发阶段快速切换到 Resources 加载模式。

## 功能特性

*   **统一接口**：无论底层使用 YooAsset 还是 Resources，上层调用接口保持一致。
*   **引用计数**：基于 YooAsset 的引用计数管理，自动释放未使用的资源。
*   **对象池**：内置 `ResLoader` 对象池，减少 GC。
*   **双模式支持**：支持 `YooAsset` 和 `Resources` 两种加载模式，方便开发调试。

## 使用说明

### 1. 初始化

EURes 作为一个 Utility 模块，通常在架构初始化时注册。

```csharp
// 在你的架构定义中
public class MyArchitecture : Architecture<MyArchitecture>
{
    protected override void Init()
    {
        // 注册 EUResUtility
        RegisterUtility(new EUResUtility());
    }
}
```

### 2. 设置加载模式

可以通过 `EUResUtility.LoadMode` 设置全局默认加载模式，也可以在创建 `ResLoader` 时指定特定的加载模式。

```csharp
using EUFarmworker.Tools.EURes.Script;

// 1. 设置全局默认模式
EUResUtility.LoadMode = ResLoadMode.Resources;

// 2. 创建 Loader 时指定模式 (覆盖全局默认值)
// 强制使用 YooAsset 模式
var yooLoader = ResLoader.Allocate(ResLoadMode.YooAsset);

// 强制使用 Resources 模式
var resLoader = ResLoader.Allocate(ResLoadMode.Resources);

// 使用全局默认模式
var defaultLoader = ResLoader.Allocate(); 
```

### 3. 加载资源

使用 `ResLoader` 加载资源。推荐使用 `Allocate` 方法获取实例，使用完毕后调用 `Recycle` 或 `Dispose` 回收。

```csharp
using EUFarmworker.Tools.EURes.Script;
using UnityEngine;
using UnityEngine.SceneManagement;

// 获取 Loader
var loader = ResLoader.Allocate();

// 同步加载
var prefab = loader.LoadSync<GameObject>("Prefabs/MyCube");
if (prefab != null)
{
    Instantiate(prefab);
}

// 异步加载
loader.LoadAsync<GameObject>("Prefabs/MySphere", (obj) =>
{
    if (obj != null)
    {
        Instantiate(obj);
    }
});

// 加载场景
// 在 YooAsset 模式下返回 SceneHandle，在 Resources 模式下返回 null
loader.LoadScene("MyScene", LoadSceneMode.Single);

// 释放资源 (回收 Loader)
loader.Recycle();
// 或者
// loader.Dispose();
```

### 4. 注意事项

*   **Resources 模式**：
    *   资源必须放在 `Resources` 目录下。
    *   `LoadScene` 使用 `EUResUtility.LoadScene`，场景必须添加到 Build Settings 中。
    *   `ReleaseAll` 不会主动卸载 Resources 资源（因为 Resources 机制限制），建议仅在开发阶段使用。

*   **YooAsset 模式**：
    *   资源必须按照 YooAsset 流程打包。
    *   `LoadScene` 使用 YooAsset 的场景加载接口，返回 `SceneHandle`。
    *   `ReleaseAll` 会释放所有加载的 AssetHandle，引用计数归零时资源会被卸载。
