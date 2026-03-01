# EUFramework Core MVC

## 目录
- [概述](#概述)
- [设计理念](#设计理念)
- [核心特性](#核心特性)
- [核心概念](#核心概念)
- [快速开始](#快速开始)
  - [1. 定义模型 (Model)](#1-定义模型-model)
  - [2. 定义系统 (System)](#2-定义系统-system)
  - [3. 定义架构 (Architecture)](#3-定义架构-architecture)
  - [4. 定义命令与事件 (Command & Event)](#4-定义命令与事件-command--event)
  - [5. 表现层使用 (Controller)](#5-表现层使用-controller)
- [进阶使用](#进阶使用)
  - [查询 (Query)](#查询-query)
  - [性能优化提示](#性能优化提示)
- [文档说明](#文档说明)

---

## 概述

EUFramework Core MVC 是一个基于 Unity 的轻量级架构框架，旨在提供清晰的代码结构和高效的开发体验。它深受 QFramework 的启发，并在此基础上进行了针对性的优化和改进，特别是在性能和类型安全方面。

## 设计理念

- **分层架构**：将应用程序分为表现层 (Controller)、系统层 (System)、数据层 (Model) 和工具层 (Utility)，实现关注点分离。
- **面向接口编程**：通过接口定义模块间的交互，降低耦合度，提高代码的可测试性和可维护性。
- **类型安全**：充分利用 C# 的泛型和强类型特性，在编译期捕获潜在错误，减少运行时异常。
- **极致性能**：在关键路径（如事件系统、命令执行）上广泛使用 **Struct** 和无装箱操作，大幅优化内存分配和执行效率。

## 核心特性

- **极简主义设计**：基于 IOC (控制反转) + MVC 的分层架构，结构清晰，学习曲线平缓，上手简单。
- **高性能事件系统**：内置 `TypeEventSystem`，采用静态泛型缓存 + 数组存储。特别针对 Unity 的 **Mono (JIT)** 版本进行了优化，利用泛型静态字段的直接地址访问特性，消除了字典查找开销，实现**零 GC (Zero-GC)** 的事件发送与监听。
- **严格的生命周期管理**：提供规范的模块初始化 (Init) 与销毁 (Dispose) 顺序管理，确保依赖关系正确无误。
  - **初始化顺序**：Utility -> Model -> System (基础设施 -> 数据 -> 逻辑)
  - **销毁顺序**：System -> Model -> Utility (逻辑 -> 数据 -> 基础设施)
- **泛型与扩展支持**：通过 `CoreExtension` 提供丰富的扩展方法（如 `this.GetModel<T>()`），使编码过程流畅且类型安全。

## 核心概念

### Architecture (架构)
整个应用的顶层容器，负责管理所有的 Model、System 和 Utility。它是单例的，作为访问所有底层模块的唯一入口。
> **注意**：必须使用 `EUCore.SetArchitecture(...)` 来激活当前架构，否则表现层无法正常工作。

### Model (数据层)
负责数据的存储和状态管理。Model 应该是纯粹的数据容器，不应包含复杂的业务逻辑。
- 继承自 `AbsModelBase` 或实现 `IModel` 接口。
- 通常作为类 (Class) 实现。

### System (系统层)
负责处理核心业务逻辑。System 可以访问 Model，也可以监听和发送事件。它是连接数据层和表现层的桥梁。
- 继承自 `AbsSystemBase` 或实现 `ISystem` 接口。
- 通常作为类 (Class) 实现。

### Utility (工具层)
提供通用的工具方法或基础设施服务，如本地存储、网络请求、通用算法等。
- 继承自 `AbsUtilityBase` 或实现 `IUtility` 接口。

### Command (命令)
用于执行状态变更的操作。Command 可以访问 Model 和 System，是修改数据的唯一推荐方式。
- 实现 `ICommand` (无返回值) 或 `ICommand<T>` (有返回值)。
- **强烈建议使用 struct 实现**，以充分利用框架的零 GC 优化特性。

### Query (查询)
用于获取数据。Query 可以访问 Model 和 System，但严格禁止修改它们的状态。
- 实现 `IQuery<T>`。
- **强烈建议使用 struct 实现**。

### Event (事件)
用于模块间的解耦通信。通过发布/订阅模式，不同模块可以在不知道彼此存在的情况下进行交互。
- 任意 struct 类型均可作为事件载体。

---

## 快速开始

以下是一个简单的计分系统示例，展示如何使用该框架。

### 1. 定义模型 (Model)

模型负责存储分数数据。

```csharp
using EUFramework.Core.MVC.Abstract;

public class GameModel : AbsModelBase
{
    public int Score { get; set; } = 0;

    protected override void OnInit()
    {
        Score = 0;
    }
}
```

### 2. 定义系统 (System)

系统负责处理游戏开始时的逻辑（重置分数）。

```csharp
using UnityEngine;
using EUFramework.Core.MVC.Abstract;
using EUFramework.Core.MVC.CoreTool;

public class ScoreSystem : AbsSystemBase
{
    protected override void OnInit()
    {
        // 监听游戏开始事件
        this.RegisterEvent<GameStartedEvent>(OnGameStarted);
    }

    private void OnGameStarted(GameStartedEvent e)
    {
        // 获取模型并修改数据
        var model = this.GetModel<GameModel>();
        model.Score = 0;
        Debug.Log("Game Started, Score Reset.");
    }
}
```

### 3. 定义架构 (Architecture)

架构负责注册所有的模型和系统。

```csharp
using EUFramework.Core.MVC.Abstract;

public class GameArchitecture : AbsArchitectureBase<GameArchitecture>
{
    protected override void Init()
    {
        // 注册模块
        RegisterModel(new GameModel());
        RegisterSystem(new ScoreSystem());
    }
}
```

### 4. 定义命令与事件 (Command & Event)

定义用于交互的事件和命令。推荐使用 `struct`。

```csharp
using UnityEngine;
using EUFramework.Core.MVC.Interface;
using EUFramework.Core.MVC.CoreTool;

// 事件：游戏开始
public struct GameStartedEvent { }

// 命令：增加分数
public struct AddScoreCommand : ICommand
{
    private readonly int _amount;
    
    public AddScoreCommand(int amount) 
    {
        _amount = amount;
    }

    public void Execute()
    {
        var model = this.GetModel<GameModel>();
        model.Score += _amount;
        Debug.Log($"Score Added: {_amount}, Total: {model.Score}");
    }
}
```

### 5. 表现层使用 (Controller)

在 Unity 的 MonoBehaviour 中使用框架。

```csharp
using UnityEngine;
using EUFramework.Core.MVC;
using EUFramework.Core.MVC.Interface;
using EUFramework.Core.MVC.CoreTool;

public class GamePanel : MonoBehaviour, IController
{
    private void Awake()
    {
        // 必须在最开始设置架构，否则后续的扩展方法无法找到目标
        EUCore.SetArchitecture<GameArchitecture>();
    }

    private void Start()
    {
        // 监听事件
        this.RegisterEvent<GameStartedEvent>(OnGameStarted);

        // 发送命令
        this.SendCommand(new AddScoreCommand(10));
    }

    private void OnGameStarted(GameStartedEvent e)
    {
        Debug.Log("UI received GameStartedEvent");
    }

    private void OnDestroy()
    {
        // 注意：MonoBehaviour 销毁时必须手动注销事件，防止回调引用丢失或内存泄漏
        this.UnRegisterEvent<GameStartedEvent>(OnGameStarted);
    }
}
```

---

## 进阶使用

### 查询 (Query)

当表现层需要获取数据，但不应该修改数据时，使用 Query。

```csharp
using EUFramework.Core.MVC.Interface;
using EUFramework.Core.MVC.CoreTool;

public struct GetScoreQuery : IQuery<int>
{
    public int Execute()
    {
        return this.GetModel<GameModel>().Score;
    }
}

// 在 Controller 或 System 中使用
int currentScore = this.SendQuery(new GetScoreQuery());
```

### 性能优化提示

框架内部针对 `struct` 类型的 Command、Query 和 Event 进行了专门的底层优化，避免了装箱（Boxing）和垃圾回收（GC）。
因此，**强烈建议始终使用 `struct` 来定义这些交互对象**，以获得最佳的运行性能。

---

## 文档说明

- **API文档**：请查阅 [API.md](API.md) 获取详细的接口和类说明。
- **更新日志**：请查阅 [Update.md](Update.md) 获取版本更新历史。
