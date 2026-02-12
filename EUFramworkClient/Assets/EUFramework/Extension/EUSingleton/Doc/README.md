# EU 单例模式 (EU Singleton)

提供健全的 C# 和 MonoBehaviour 单例基类，支持懒加载和生命周期管理，简化单例模式的实现。

## 功能特点

- **泛型基类**：提供 `EUSingleton<T>` 和 `EUSingletonMono<T>` 两个泛型基类，方便快速实现单例。
- **懒加载**：实例在首次访问时创建，避免不必要的资源消耗。
- **健全性**：
    - **EUSingleton**：支持私有构造函数，确保单例的封装性。
    - **EUSingletonMono**：自动处理 GameObject 的创建和挂载，支持 `DontDestroyOnLoad` 跨场景持久化，并处理应用程序退出时的实例销毁，防止意外错误。
- **无锁设计**：针对 Unity 主线程环境优化，未引入锁机制，提高性能。

## 使用方法

### 1. 普通 C# 类单例

继承 `EUSingleton<T>`，并将自身类型作为泛型参数。

```csharp
using EUFramework;

public class GameManager : EUSingleton<GameManager>
{
    // 可选：重写初始化方法
    protected override void OnCreate()
    {
        // 初始化逻辑
    }

    public void StartGame()
    {
        // ...
    }
}

// 使用
GameManager.Instance.StartGame();
```

### 2. MonoBehaviour 单例

继承 `EUSingletonMono<T>`，并将自身类型作为泛型参数。

```csharp
using EUFramework;
using UnityEngine;

public class AudioManager : EUSingletonMono<AudioManager>
{
    // 可选：重写初始化方法
    protected override void OnCreate()
    {
        // 初始化逻辑
    }

    public void PlaySound(string clipName)
    {
        // ...
    }
}

// 使用
AudioManager.Instance.PlaySound("BGM");
```

## 文件结构

- `Script/`: 包含核心逻辑代码。
- `Doc/`: 文档说明。
- `extension.json`: 扩展描述文件。

## 依赖

- Unity 2021.3 或更高版本
