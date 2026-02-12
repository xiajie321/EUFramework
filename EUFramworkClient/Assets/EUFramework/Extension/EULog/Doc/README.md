# EU 日志工具 (EULog)

## 简介

EULog 是一个高性能的日志工具类，旨在解决 Unity 原生 `Debug.Log` 在禁用日志后依然产生字符串拼接开销的问题。

## 核心原理

使用 C# 的 `[Conditional("SYMBOL")]` 属性。当指定的宏（如 `UNITY_EDITOR` 或 `DEVELOPMENT_BUILD`）未定义时，编译器会直接移除所有对该方法的调用指令。这意味着连同参数的计算（包括字符串拼接）都会在编译阶段被移除，从而完全避免运行时的任何开销。

## 使用方法

### 1. 自动开启

EULog 默认在以下情况下自动开启：

- **Unity Editor**：在编辑器中运行时始终开启。
- **Development Build**：打包时勾选 "Development Build" 选项时开启。

在正式发布的 Release 版本（未勾选 Development Build）中，所有日志调用及其开销将被自动移除。

### 2. 代码示例

```csharp
using EUFramework;

public class Example : MonoBehaviour
{
    void Start()
    {
        // 普通日志
        EUDebug.Log("Hello World");

        // 带上下文的日志（点击日志可跳转到对应物体）
        EUDebug.Log("Hello Object", this);

        // 格式化日志
        // 如果在 Release 版本中，字符串拼接 "Value: " + 100 不会执行
        EUDebug.LogFormat("Value: {0}", 100);

        // 警告
        EUDebug.LogWarning("This is a warning");

        // 错误
        EUDebug.LogError("This is an error");
    }
}
```

## 优势

- **零开销**：在正式发布版本中，日志调用完全消失，不占用任何 CPU 和内存。
- **智能控制**：自动适配 Editor 和 Development Build，无需手动配置宏定义。
- **无缝替换**：API 设计与 `UnityEngine.Debug` 保持一致，易于上手和替换。
- **支持上下文**：支持传入 `UnityEngine.Object` 作为上下文，方便在 Editor 中定位对象。
