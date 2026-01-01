# EUFarmworker Core 框架使用说明

## 目录
1. [简介](#简介)
2. [设计理念](#设计理念)
3. [与 QFramework 的对比](#与-qframework-的对比)
4. [核心概念](#核心概念)
5. [使用指南](#使用指南)
    - [架构定义](#架构定义)
    - [Model (数据层)](#model-数据层)
    - [System (系统层)](#system-系统层)
    - [Utility (工具层)](#utility-工具层)
    - [Command (命令)](#command-命令)
    - [Query (查询)](#query-查询)
    - [Event (事件)](#event-事件)
    - [Controller (表现层)](#controller-表现层)
6. [示例代码](#示例代码)

## 简介
EUFarmworker Core 是一个基于 Unity 的轻量级架构框架，旨在提供清晰的代码结构和高效的开发体验。它深受 QFramework 的启发，并在此基础上进行了针对性的优化和改进，特别是在性能和类型安全方面。

## 设计理念
本框架遵循以下核心设计原则：
- **分层架构**：将应用程序分为表现层、系统层、数据层和工具层，实现关注点分离。
- **面向接口编程**：通过接口定义模块间的交互，降低耦合度。
- **类型安全**：利用 C# 的泛型和强类型特性，减少运行时错误。
- **高性能**：在关键路径（如事件系统）上使用结构体和无装箱操作，优化内存分配和执行效率。

## 与 QFramework 的对比
虽然本框架的设计灵感来源于 QFramework，但在实现细节上有一些关键的区别：

1.  **事件系统优化**：
    -   **QFramework**：通常使用对象或接口作为事件载体。
    -   **EUFarmworker**：强制使用 `struct` 作为事件载体。这利用了值类型的特性，避免了引用类型的垃圾回收（GC）开销，显著提高了高频事件发送时的性能。

2.  **精简核心**：
    -   去除了部分在特定项目中不常用或过于复杂的功能，保持核心的轻量化。
    -   专注于核心架构（Architecture, Model, System, Utility, Command, Query, Event）的稳健实现。

3.  **明确的泛型约束**：
    -   在 `RegisterEvent`、`SendEvent` 等方法中增加了 `where T : struct` 约束，从编译层面强制执行最佳实践。

## 核心概念

### Architecture (架构)
整个应用的容器，负责管理所有的 Model、System 和 Utility。它是单例的，作为访问所有模块的入口。

### Model (数据层)
负责数据的存储和状态管理。Model 应该是纯粹的数据容器，不包含复杂的业务逻辑。

### System (系统层)
负责处理业务逻辑。System 可以访问 Model，也可以监听和发送事件。它是连接数据和表现层的桥梁。

### Utility (工具层)
提供通用的工具方法或基础设施服务，如存储、网络、算法等。Utility 应该是无状态的或仅维护自身状态，不依赖于具体的业务逻辑。

### Command (命令)
用于执行状态变更的操作。Command 可以访问 Model 和 System，是修改数据的唯一推荐方式。

### Query (查询)
用于获取数据。Query 可以访问 Model 和 System，但不能修改它们。它负责将数据转换为表现层需要的格式。

### Event (事件)
用于模块间的解耦通信。通过发布/订阅模式，不同模块可以在不知道彼此存在的情况下进行交互。

## 使用指南

### 架构定义
首先，你需要定义你的架构类，继承自 `Architecture<T>`。

```csharp
public class GameArchitecture : Architecture<GameArchitecture>
{
    protected override void Init()
    {
        // 注册模块
        RegisterModel(new GameModel());
        RegisterSystem(new ScoreSystem());
        RegisterUtility(new StorageUtility());
    }
}
```

### Model (数据层)
继承自 `AbstractModel`。

```csharp
public class GameModel : AbstractModel
{
    public int Score { get; set; }

    public override void Init()
    {
        Score = 0;
    }
}
```

### System (系统层)
继承自 `AbstractSystem`。

```csharp
public class ScoreSystem : AbstractSystem
{
    public override void Init()
    {
        // 初始化逻辑
    }

    public void AddScore(int amount)
    {
        var model = this.GetModel<GameModel>();
        model.Score += amount;
        
        // 发送分数变更事件
        this.SendEvent(new ScoreChangedEvent { NewScore = model.Score });
    }
}
```

### Utility (工具层)
继承自 `AbstractUtility`。

```csharp
public class StorageUtility : AbstractUtility
{
    public override void Init()
    {
    }

    public void Save(string key, string value)
    {
        // 保存逻辑
    }
}
```

### Command (命令)
实现 `ICommand` 接口。

```csharp
public struct AddScoreCommand : ICommand
{
    public int Amount;

    public void Execute()
    {
        var system = CoreExtension.GetArchitecture().GetSystem<ScoreSystem>();
        system.AddScore(Amount);
    }
}
```

### Query (查询)
实现 `IQuery<T>` 接口。

```csharp
public struct GetScoreQuery : IQuery<int>
{
    public int Execute()
    {
        var model = CoreExtension.GetArchitecture().GetModel<GameModel>();
        return model.Score;
    }
}
```

### Event (事件)
定义为 `struct`。

```csharp
public struct ScoreChangedEvent
{
    public int NewScore;
}
```

### Controller (表现层)
通常是 `MonoBehaviour`，实现 `IController` 接口。

```csharp
public class GamePanel : MonoBehaviour, IController
{
    void Start()
    {
        // 注册架构（通常在入口处做一次）
        EUCore.SetArchitecture(GameArchitecture.Instance);

        // 监听事件
        this.RegisterEvent<ScoreChangedEvent>(OnScoreChanged);
    }

    void OnDestroy()
    {
        // 注销事件
        this.UnRegisterEvent<ScoreChangedEvent>(OnScoreChanged);
    }

    private void OnScoreChanged(ScoreChangedEvent e)
    {
        Debug.Log($"Score: {e.NewScore}");
    }

    public void OnClickAddButton()
    {
        // 发送命令
        this.SendCommand(new AddScoreCommand { Amount = 10 });
    }
}
```

## 示例代码
完整的测试示例可以在 `EUFarmworker/Core/Test/TestCore.cs` 中找到。
