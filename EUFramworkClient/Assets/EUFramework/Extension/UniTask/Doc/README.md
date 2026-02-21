# UniTask

## 概述

为 Unity 提供高效的、无分配的 async/await 集成。

## 简介

UniTask 提供了一个基于结构体的 `UniTask<T>` 和自定义的 `AsyncMethodBuilder`，以实现零分配的异步操作。它使所有 Unity 的 `AsyncOperations` 和 `Coroutines` 都可以被等待（awaitable）。

## 为什么选择 UniTask

*   **零分配**: 使用基于结构体的 `UniTask` 类型，避免了原生 `Task` 类型的堆分配。
*   **高性能**: 针对 Unity 进行了深度优化，避免了上下文切换的开销。
*   **Unity 集成**: 完美支持 Unity 的 PlayerLoop（Update, FixedUpdate 等），支持 WebGL。
*   **功能丰富**: 提供了丰富的 API，如 `WhenAll`, `WhenAny`, `Delay`, `Yield` 等，以及对 uGUI 和 MonoBehaviour 事件的支持。
*   **调试友好**: 提供了 UniTask Tracker 窗口，方便监控任务状态，防止内存泄漏。

## 基础用法

在使用 UniTask 时，需要在脚本头部添加命名空间引用：

```csharp
using Cysharp.Threading.Tasks;
```

### 替换 Coroutine

UniTask 可以完全替代 Unity 的 Coroutine，并且语法更加简洁清晰。

```csharp
// UniTask 方式
async UniTask Task()
{
    await UniTask.Delay(1000);
    Debug.Log("Done");
}
```

### 等待 Unity 异步操作

UniTask 使得 Unity 的异步操作（如加载资源、场景跳转等）可以直接使用 `await` 关键字。

```csharp
// 加载资源
var asset = await Resources.LoadAsync<TextAsset>("foo");

// 加载场景
await SceneManager.LoadSceneAsync("SceneName");
```

## 核心 API 概览

### Delay & Yield
- `UniTask.Delay(1000)`: 等待指定时间。
- `UniTask.Yield()`: 等待一帧。
- `UniTask.NextFrame()`: 等待下一帧。
- `UniTask.WaitForFixedUpdate()`: 等待 FixedUpdate。

### 组合任务
- `UniTask.WhenAll`: 等待所有任务完成。
- `UniTask.WhenAny`: 等待任意一个任务完成。

### 取消处理
- 支持 `CancellationToken`。
- `GetCancellationTokenOnDestroy()`: 获取与 GameObject 生命周期绑定的 Token。

## 文档说明

- **API文档**：请查阅 [API.md](API.md) 获取详细的接口说明。
- **更新日志**：请查阅 [Update.md](Update.md) 获取版本更新历史。
