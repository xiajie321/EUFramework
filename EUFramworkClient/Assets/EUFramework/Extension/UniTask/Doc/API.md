# UniTask API 文档

由于 UniTask 是第三方开源库，详细的 API 文档请参考官方仓库。

**官方文档**: [https://github.com/Cysharp/UniTask](https://github.com/Cysharp/UniTask)

## 核心类

### Cysharp.Threading.Tasks.UniTask
核心结构体，替代 `System.Threading.Tasks.Task`。

### Cysharp.Threading.Tasks.UniTask<T>
带返回值的核心结构体，替代 `System.Threading.Tasks.Task<T>`。

### Cysharp.Threading.Tasks.UniTaskVoid
用于 Fire-and-Forget 的异步方法返回类型。

## 常用静态方法

- `UniTask.Delay`
- `UniTask.Yield`
- `UniTask.NextFrame`
- `UniTask.WaitForEndOfFrame`
- `UniTask.WaitForFixedUpdate`
- `UniTask.WhenAll`
- `UniTask.WhenAny`
- `UniTask.SwitchToMainThread`
- `UniTask.SwitchToThreadPool`

## 常用扩展方法

- `GetCancellationTokenOnDestroy()` (MonoBehaviour)
- `OnClickAsync()` (Button)
- `OnValueChangedAsync()` (Toggle/Slider/InputField)
- `ToUniTask()` (AsyncOperation/Task)
