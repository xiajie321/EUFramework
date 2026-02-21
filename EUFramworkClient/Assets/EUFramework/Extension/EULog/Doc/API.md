# EULog API 文档

## EUDebug 类

`EUFramework.EUDebug`

高性能日志工具类。所有方法均带有 `[Conditional]` 特性。

### 静态方法

#### Log
- `void Log(object message)`: 打印普通日志。
- `void Log(object message, Object context)`: 打印带上下文的普通日志。

#### LogWarning
- `void LogWarning(object message)`: 打印警告日志。
- `void LogWarning(object message, Object context)`: 打印带上下文的警告日志。

#### LogError
- `void LogError(object message)`: 打印错误日志。
- `void LogError(object message, Object context)`: 打印带上下文的错误日志。

#### Format
- `void LogFormat(string format, params object[] args)`: 打印格式化普通日志。
- `void LogWarningFormat(string format, params object[] args)`: 打印格式化警告日志。
- `void LogErrorFormat(string format, params object[] args)`: 打印格式化错误日志。
