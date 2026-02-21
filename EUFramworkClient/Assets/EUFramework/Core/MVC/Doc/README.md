# EUFramework Core MVC

## 概述

EUFramework Core MVC 是一个基于 Unity 的轻量级架构框架，旨在提供清晰的代码结构和高效的开发体验。它深受 QFramework 的启发，并在此基础上进行了针对性的优化和改进，特别是在性能和类型安全方面。

## 设计理念

- **分层架构**：将应用程序分为表现层、系统层、数据层和工具层，实现关注点分离。
- **面向接口编程**：通过接口定义模块间的交互，降低耦合度。
- **类型安全**：利用 C# 的泛型和强类型特性，减少运行时错误。
- **高性能**：在关键路径（如事件系统）上使用结构体和无装箱操作，优化内存分配和执行效率。

## 核心概念

### Architecture (架构)
整个应用的容器，负责管理所有的 Model、System 和 Utility。它是单例的，作为访问所有模块的入口。

### Model (数据层)
负责数据的存储和状态管理。Model 应该是纯粹的数据容器，不包含复杂的业务逻辑。

### System (系统层)
负责处理业务逻辑。System 可以访问 Model，也可以监听和发送事件。它是连接数据和表现层的桥梁。

### Utility (工具层)
提供通用的工具方法或基础设施服务，如存储、网络、算法等。

### Command (命令)
用于执行状态变更的操作。Command 可以访问 Model 和 System，是修改数据的唯一推荐方式。

### Query (查询)
用于获取数据。Query 可以访问 Model 和 System，但不能修改它们。

### Event (事件)
用于模块间的解耦通信。通过发布/订阅模式，不同模块可以在不知道彼此存在的情况下进行交互。

## 快速开始

### 1. 架构定义

```csharp
public class GameArchitecture : AbsArchitectureBase<GameArchitecture>
{
    protected override void Init()
    {
        RegisterModel(new GameModel());
        RegisterSystem(new ScoreSystem());
        RegisterUtility(new StorageUtility());
    }
}
```

### 2. 表现层使用

```csharp
public class GamePanel : MonoBehaviour, IController
{
    private void Awake()
    {
        EUCore.SetArchitecture(GameArchitecture.Instance);
    }

    void Start()
    {
        this.RegisterEvent<ScoreChangedEvent>(OnScoreChanged);
    }
    
    // ...
}
```

## 文档说明

- **API文档**：请查阅 [API.md](API.md) 获取详细的接口说明。
- **更新日志**：请查阅 [Update.md](Update.md) 获取版本更新历史。
