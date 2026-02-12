# EUFramework Core MVC使用说明

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
6. [使用注意事项](#使用注意事项)
7. [API 介绍](#api-介绍)
8. [进阶指南：性能优化与最佳实践](#进阶指南性能优化与最佳实践)
9. [示例代码](#示例代码)

## 简介

EUFramework Core MVC 是一个基于 Unity 的轻量级架构框架，旨在提供清晰的代码结构和高效的开发体验。它深受 QFramework 的启发，并在此基础上进行了针对性的优化和改进，特别是在性能和类型安全方面。

## 设计理念

本框架遵循以下核心设计原则：

- **分层架构**：将应用程序分为表现层、系统层、数据层和工具层，实现关注点分离。
- **面向接口编程**：通过接口定义模块间的交互，降低耦合度。
- **类型安全**：利用 C# 的泛型和强类型特性，减少运行时错误。
- **高性能**：在关键路径（如事件系统）上使用结构体和无装箱操作，优化内存分配和执行效率。

## 与 QFramework 的对比

虽然本框架的设计灵感来源于 QFramework，但在实现细节上有一些关键的区别：

1. **事件系统优化**：
   
   - **QFramework**：通常使用对象或接口作为事件载体。
   - **EUFramework**：强制使用 `struct` 作为事件载体。这利用了值类型的特性，避免了引用类型的垃圾回收（GC）开销，显著提高了高频事件发送时的性能。

2. **精简核心**：
   
   - 去除了部分在特定项目中不常用或过于复杂的功能，保持核心的轻量化。
   - 专注于核心架构（Architecture, Model, System, Utility, Command, Query, Event）的稳健实现。

3. **明确的泛型约束**：
   
   - 在 `RegisterEvent`、`SendEvent` 等方法中增加了 `where T : struct` 约束，从编译层面强制执行最佳实践。

## 核心概念

### Architecture (架构)

整个应用的容器，负责管理所有的 Model、System 和 Utility。它是单例的，作为访问所有模块的入口。

> **重要提示**：由于 `Architecture` 使用静态泛型缓存（[CacheContainer.cs](file:///d%3A/Unity/UnityProject/EUFramworker/EUFrameworkClient/Assets/EUFramework/Core/MVC/CoreTool/CacheContainer.cs)）来提升性能，它**不会**在对象销毁时自动清理。你**必须**在合适的时机显式调用 `YourArchitecture.Instance.Dispose()`。

### EUCore.SetArchitecture (游戏运行时的核心框架设置)

使用EUCore.SetArchitecture可以设置和切换当前游戏运行时用到的唯一架构,会自动的去调用上次的架构Dispose()方法,即：`YourArchitecture.Instance.Dispose()`。
调用顺序：`LastArchitecture.Instance.Dispose()` ->`CurrentArchitecture.Instance.Dispose()`。

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

首先，你需要定义你的架构类，继承自 `AbsArchitectureBase<T>`。

```csharp
public class GameArchitecture : AbsArchitectureBase<GameArchitecture>
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

继承自 `AbsModelBase`。

```csharp
public class GameModel : AbsModelBase
{
    public int Score { get; set; }

    public override void Init()
    {
        Score = 0;
    }
}
```

### System (系统层)

继承自 `AbsSystemBase`。

```csharp
public class ScoreSystem : AbsSystemBase
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

继承自 `AbsUtilityBase`。

```csharp
public class StorageUtility : AbsUtilityBase
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

实现 `ICommand` 接口（无返回值）或 `ICommand<TResult>` 接口（有返回值）。

#### 无返回值命令

```csharp
public struct AddScoreCommand : ICommand
{
    public int Amount;

    public void Execute()
    {
        // 在 struct 中调用 GetSystem 建议使用带 TCaller 的泛型版本以避免装箱
        var system = this.GetSystem<AddScoreCommand, ScoreSystem>();
        system.AddScore(Amount);
    }
}
```

#### 有返回值命令

```csharp
public struct GetScoreCommand : ICommand<int>
{
    public int Execute()
    {
        var model = this.GetModel<GetScoreCommand, GameModel>();
        return model.Score;
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
        // 建议使用带 TCaller 的泛型版本
        var model = this.GetModel<GetScoreQuery, GameModel>();
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
        private void Awake()
        {
            // 初始化架构
            EUCore.SetArchitecture(TestArchitecture.Instance);
        }
        void Start()
        {
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

## 使用注意事项

### 1. 生命周期与内存管理

- **手动释放**：`Architecture` 及其管理的模块使用静态缓存提升性能。这意味着即便 `Architecture` 实例被置空，静态变量仍会持有引用。
- **必须调用 Dispose**：在对应架构确定以后不会使用时，务必显式调用 `YourArchitecture.Instance.Dispose()`去释放掉内存避免长时间的去占用内存导致对应内存长时间不被释放。

### 2. 线程安全

- **主线程限制**：本框架**非线程安全**。
- **风险**：所有的事件发送（`SendEvent`）、模块注册和 ID 分配都没有加锁。请确保所有架构操作均在 Unity 主线程中进行。

### 3. 初始化顺序依赖

- **固定顺序**：框架按 `Model -> System -> Utility` 的顺序调用 `Init()`。
- **避免交叉引用**：在 `Init` 阶段，尽量避免跨层级调用（例如 Model 初始化时去获取 System），这可能导致 `NullReferenceException` 或初始化不完全。

### 4. 事件注册注销成对

- **防止空指针**：在 `Controller` (MonoBehaviour) 中注册事件后，务必在 `OnDestroy` 中注销。否则当物体销毁后，事件系统仍会尝试调用已销毁物体的回调。

## API 介绍

### IArchitecture (架构接口)

所有架构类都应实现此接口，它定义了框架的核心操作。

- **模块注册**：
  - `void RegisterModel<T>(T model)`: 注册一个 Model 实例。
  - `void RegisterSystem<T>(T system)`: 注册一个 System 实例。
  - `void RegisterUtility<T>(T utility)`: 注册一个 Utility 实例。
- **模块获取**：
  - `T GetModel<T>()`: 获取已注册的 Model。
  - `T GetSystem<T>()`: 获取已注册的 System。
  - `T GetUtility<T>()`: 获取已注册的 Utility。
- **交互操作**：
  - `void SendCommand<T>(T command)`: 发送一个无返回值的命令。
  - `T SendCommand<TCommand, T>(TCommand command)`: 发送一个带返回值的命令。
  - `T SendQuery<TQuery, T>(TQuery query)`: 执行一个查询并获取结果。
  - `void SendEvent<T>(T tEvent)`: 发布一个事件。
- **事件管理**：
  - `void RegisterEvent<T>(Action<T> onEvent)`: 订阅特定类型的事件。
  - `void UnRegisterEvent<T>(Action<T> onEvent)`: 取消订阅。
- **生命周期**：
  - `void Dispose()`: 销毁架构，释放所有模块并清理事件系统。

### 扩展方法 (ICan 接口族)

通过实现 `IController`, `ISystem`, `IModel`, `ICommand` 等接口，你的类可以获得便捷的扩展方法。

- **ICanGetModel**: 获得 `this.GetModel<T>()` 能力。
- **ICanGetSystem**: 获得 `this.GetSystem<T>()` 能力。
- **ICanGetUtility**: 获得 `this.GetUtility<T>()` 能力。
- **ICanSendCommand**: 获得 `this.SendCommand(...)` 能力。
- **ICanSendQuery**: 获得 `this.SendQuery(...)` 能力。
- **ICanSendEvent**: 获得 `this.SendEvent(...)` 能力。
- **ICanRegisterEvent**: 获得 `this.RegisterEvent<T>(...)` 和 `this.UnRegisterEvent<T>(...)` 能力。

> **提示**：对于 `struct` 类型的调用者，请优先使用带 `TCaller` 的扩展方法版本（如 `this.GetModel<TCaller, TModel>(ref this)`）以获得最佳性能。

## 进阶指南：性能优化与最佳实践

EUFramework Core MVC 的一大特性是极致的性能优化，特别是在 Struct 类型的 Command、Query 和 Event 中。为了避免 Struct 在调用接口方法时产生装箱（Boxing）操作（即 `this` 指针从值类型转换为引用类型接口），框架提供了一套特定的泛型扩展方法。

### 在 Struct 中调用架构方法

当你在 `struct` (如 Command 或 Query) 内部调用 `GetModel`、`GetSystem`、`SendCommand` 等方法时，**强烈建议**使用包含 `TCaller` (调用者类型) 的重载版本。

#### 推荐写法 (无 GC)

通过泛型显式传入当前结构体的类型，编译器会生成专门的代码路径，避免装箱。

```csharp
public struct TestCommand : ICommand
{
    public void Execute()
    {
        // 获取 Model/System/Utility
        // 格式: this.GetModel<TCaller, TModel>()
        var model = this.GetModel<TestCommand, GameModel>();

        // 发送 Command
        // 格式: this.SendCommand<TCaller, TCommand>(command)
        this.SendCommand<TestCommand, OtherCommand>(new OtherCommand());

        // 发送有返回值的 Command
        // 格式: this.SendCommand<TCaller, TCommand, TResult>(command)
        int result = this.SendCommand<TestCommand, CommandWithResult, int>(new CommandWithResult());

        // 发送 Query
        // 格式: this.SendQuery<TCaller, TQuery, TResult>(query)
        int score = this.SendQuery<TestCommand, GetScoreQuery, int>(new GetScoreQuery());

        // 发送 Event
        // 格式: this.SendEvent<TCaller, TEvent>(event)
        this.SendEvent<TestCommand, GameStartEvent>(new GameStartEvent());
    }
}
```

#### 不推荐写法 (产生 GC)

直接调用接口方法会导致 `struct` 被装箱为接口对象，产生不必要的内存分配。

```csharp
public struct TestCommand : ICommand
{
    public void Execute()
    {
        // ⚠️ 以下写法在 struct 中会产生装箱，不建议使用

        // this.GetModel<GameModel>(); 
        // this.SendCommand(new OtherCommand());
        // this.SendQuery<GetScoreQuery, int>(new GetScoreQuery());
        // this.SendEvent(new GameStartEvent());
    }
}
```

> **注意**：在 `class` (如 System, Model, MonoBehaviour Controller) 中，由于本身就是引用类型，直接使用 `this.GetModel<T>()` 等简化写法即可，不会有装箱问题。

## 示例代码

完整的测试示例可以在 [TestCore.cs](file:///d%3A/Unity/UnityProject/EUFramworker/EUFrameworkClient/Assets/EUFramework/Core/MVC/Example/Script/TestCore.cs) 中找到。
