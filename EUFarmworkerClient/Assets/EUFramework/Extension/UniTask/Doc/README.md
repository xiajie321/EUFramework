# UniTask

为 Unity 提供高效的、无分配的 async/await 集成。

## 简介

UniTask 提供了一个基于结构体的 `UniTask<T>` 和自定义的 `AsyncMethodBuilder`，以实现零分配的异步操作。它使所有 Unity 的 `AsyncOperations` 和 `Coroutines` 都可以被等待（awaitable）。

## 为什么选择 UniTask

*   **零分配**: 使用基于结构体的 `UniTask` 类型，避免了原生 `Task` 类型的堆分配。
*   **高性能**: 针对 Unity 进行了深度优化，避免了上下文切换的开销。
*   **Unity 集成**: 完美支持 Unity 的 PlayerLoop（Update, FixedUpdate 等），支持 WebGL。
*   **功能丰富**: 提供了丰富的 API，如 `WhenAll`, `WhenAny`, `Delay`, `Yield` 等，以及对 uGUI 和 MonoBehaviour 事件的支持。
*   **调试友好**: 提供了 UniTask Tracker 窗口，方便监控任务状态，防止内存泄漏。

## 安装

### 通过 Unity Package Manager (UPM)

你可以通过 git URL 安装 UniTask。打开 Unity 的 Package Manager，点击 "+" 号，选择 "Add package from git URL..."，然后输入：

`https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`

或者在 `Packages/manifest.json` 中添加：

```json
{
  "dependencies": {
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  }
}
```

## 基础用法

在使用 UniTask 时，需要在脚本头部添加命名空间引用：

```csharp
using Cysharp.Threading.Tasks;
```

### 替换 Coroutine

UniTask 可以完全替代 Unity 的 Coroutine，并且语法更加简洁清晰。

**旧的 Coroutine 方式：**

```csharp
IEnumerator Coroutine()
{
    yield return new WaitForSeconds(1);
    Debug.Log("Done");
}
```

**UniTask 方式：**

```csharp
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

// 网络请求
var request = await UnityWebRequest.Get("https://google.com").SendWebRequest();
```

## 核心 API 详解

### UniTask.Delay

等待指定的时间。

```csharp
// 等待 1000 毫秒
await UniTask.Delay(1000);

// 等待 1 秒，使用 TimeSpan
await UniTask.Delay(TimeSpan.FromSeconds(1));

// 使用 Unscaled Time
await UniTask.Delay(1000, ignoreTimeScale: true);

// 在 Delay 期间支持取消
await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy());
```

### UniTask.Yield

等待一帧，类似于协程中的 `yield return null`。可以指定在 PlayerLoop 的哪个阶段恢复执行。

```csharp
// 等待到下一帧的 Update 阶段（默认）
await UniTask.Yield();

// 等待到 FixedUpdate 阶段
await UniTask.Yield(PlayerLoopTiming.FixedUpdate);

// 等待到 PostLateUpdate 阶段
await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
```

### UniTask.NextFrame / WaitForEndOfFrame / WaitForFixedUpdate

提供了更明确的帧等待方法。

```csharp
await UniTask.NextFrame();
await UniTask.WaitForEndOfFrame();
await UniTask.WaitForFixedUpdate();
```

### WhenAll / WhenAny

组合多个任务。

```csharp
var task1 = UniTask.Delay(1000);
var task2 = UniTask.Delay(2000);

// 等待所有任务完成
await UniTask.WhenAll(task1, task2);

// 等待任意一个任务完成
await UniTask.WhenAny(task1, task2);
```

## 取消处理 (Cancellation)

在 Unity 中，GameObject 可能会在异步操作完成之前被销毁。为了防止在对象销毁后继续执行代码（可能导致空引用异常），必须正确处理取消。

### CancellationToken

UniTask 广泛支持 `CancellationToken`。

```csharp
async UniTask ExampleAsync(CancellationToken cancellationToken)
{
    await UniTask.Delay(1000, cancellationToken: cancellationToken);
    // 如果在 Delay 期间被取消，后续代码不会执行，并抛出 OperationCanceledException
}
```

### GetCancellationTokenOnDestroy

MonoBehaviour 扩展方法，获取一个与 GameObject 生命周期绑定的 Token。

```csharp
// 在 GameObject 销毁时自动取消
await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy());
```

### 处理取消异常

当任务被取消时，会抛出 `OperationCanceledException`。

```csharp
try
{
    await UniTask.Delay(1000, cancellationToken: cancellationToken);
}
catch (OperationCanceledException)
{
    // 任务被取消
}
finally
{
    // 清理工作
}
```

如果你不想使用 try-catch，可以使用 `SuppressCancellationThrow`。

```csharp
bool canceled = await UniTask.Delay(1000, cancellationToken: cancellationToken).SuppressCancellationThrow();
if (canceled)
{
    return;
}
```

## 超时处理 (Timeout)

为异步操作添加超时限制。

```csharp
// 如果 1 秒内未完成，抛出 TimeoutException
await SomeAsyncOperation().Timeout(TimeSpan.FromSeconds(1));

// 如果超时，返回 false，不抛出异常
bool success = await SomeAsyncOperation().TimeoutWithoutException(TimeSpan.FromSeconds(1));
```

## 进度报告 (Progress)

支持 `IProgress<T>` 接口。

```csharp
var progress = Progress.Create<float>(x => Debug.Log($"Progress: {x}"));
await UniTask.Delay(1000).ToUniTask(progress: progress);
```

## 切换上下文 (Context Switching)

UniTask 允许你在主线程和线程池之间切换。

```csharp
// 在主线程开始
await UniTask.SwitchToThreadPool();

// 现在在线程池上运行（后台线程）
// 执行繁重的计算...

await UniTask.SwitchToMainThread();
// 回到主线程，可以访问 Unity API
```

## 事件等待

UniTask 支持等待 uGUI 事件和 MonoBehaviour 消息。

### uGUI 事件

```csharp
// 等待按钮点击
await button.OnClickAsync();

// 等待输入框结束编辑
await inputField.OnEndEditAsync();
```

### MonoBehaviour 消息

```csharp
// 等待触发器进入
await this.GetAsyncTriggerEnterTrigger().OnTriggerEnterAsync();

// 等待碰撞开始
await this.GetAsyncCollisionEnterTrigger().OnCollisionEnterAsync();
```

## 异步 LINQ (UniTask.Linq)

`Cysharp.Threading.Tasks.Linq` 命名空间提供了对异步流的操作能力。

```csharp
using Cysharp.Threading.Tasks.Linq;

// 每秒生成一个数字，取前 5 个，并在 Update 中处理
await UniTaskAsyncEnumerable.Timer(TimeSpan.FromSeconds(1))
    .Select((_, i) => i)
    .Take(5)
    .ForEachAsync(x => Debug.Log(x));
```

## Fire and Forget (UniTaskVoid)

对于不需要等待其完成的异步方法（例如从 UI 事件调用的方法），应使用 `UniTaskVoid` 返回类型。

```csharp
public async UniTaskVoid FireAndForgetMethod()
{
    await UniTask.Delay(1000);
    Debug.Log("Finished");
}

// 调用时
FireAndForgetMethod().Forget();
```

**注意**: 永远不要使用 `async void`，除非是为了兼容某些特定的事件系统。`async void` 发生的异常会导致程序崩溃，而 `UniTaskVoid` 允许通过 `UniTaskScheduler.UnobservedTaskException` 处理未捕获的异常。

## UniTask Tracker

UniTask 提供了一个强大的调试工具 `UniTask Tracker`。
通过菜单 `Window > UniTask Tracker` 打开。
它可以显示当前所有正在运行的 UniTask，帮助你发现悬挂的任务和潜在的内存泄漏。
确保开启 `Enable Tracking` 选项以开始监控。

## 最佳实践

1.  **始终传递 CancellationToken**: 特别是在 MonoBehaviour 中，使用 `GetCancellationTokenOnDestroy()` 防止对象销毁后的空引用。
2.  **避免 async void**: 使用 `async UniTaskVoid` 代替。
3.  **使用 UniTask 而不是 Task**: 在 Unity 中，`UniTask` 比原生 `Task` 更轻量、更高效。
4.  **注意死锁**: 虽然 UniTask 主要在主线程运行，但在与线程池交互时仍需注意同步上下文。
5.  **利用对象池**: 对于极高频调用的异步方法，UniTask 内部已经做了很多优化，但尽量减少闭包捕获可以进一步减少 GC。

## 外部库支持

UniTask 支持多种外部库的异步扩展（通常通过定义预编译宏或导入额外的包）：
*   **DOTween**: `await transform.DOMove(...)`
*   **Addressables**: `await Addressables.LoadAssetAsync(...)`
*   **TextMeshPro**: 文本打字机效果等。

---
*本文档旨在提供 UniTask 的全面指南。更多高级用法和源码细节，请参考 [UniTask GitHub 仓库](https://github.com/Cysharp/UniTask)。*
