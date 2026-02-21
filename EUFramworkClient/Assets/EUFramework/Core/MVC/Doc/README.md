# EUFramework Core MVC

## 概述

EUFramework Core MVC 是一个基于 Unity 的轻量级架构框架，旨在提供清晰的代码结构和高效的开发体验。它深受 QFramework 的启发，并在此基础上进行了针对性的优化和改进，特别是在性能和类型安全方面。

## 设计理念

- **分层架构**：将应用程序分为表现层 (Controller)、系统层 (System)、数据层 (Model) 和工具层 (Utility)，实现关注点分离。
- **面向接口编程**：通过接口定义模块间的交互，降低耦合度。
- **类型安全**：利用 C# 的泛型和强类型特性，减少运行时错误。
- **高性能**：在关键路径（如事件系统、命令执行）上使用 **Struct** 和无装箱操作，优化内存分配和执行效率。

## 核心概念

### Architecture (架构)
整个应用的容器，负责管理所有的 Model、System 和 Utility。它是单例的，作为访问所有模块的入口。

### Model (数据层)
负责数据的存储和状态管理。Model 应该是纯粹的数据容器，不包含复杂的业务逻辑。
- 继承自 `AbsModelBase` 或实现 `IModel`。
- 通常作为类 (Class) 实现。

### System (系统层)
负责处理业务逻辑。System 可以访问 Model，也可以监听和发送事件。它是连接数据和表现层的桥梁。
- 继承自 `AbsSystemBase` 或实现 `ISystem`。
- 通常作为类 (Class) 实现。

### Utility (工具层)
提供通用的工具方法或基础设施服务，如存储、网络、算法等。
- 继承自 `AbsUtilityBase` 或实现 `IUtility`。

### Command (命令)
用于执行状态变更的操作。Command 可以访问 Model 和 System，是修改数据的唯一推荐方式。
- 实现 `ICommand` (无返回值) 或 `ICommand<T>` (有返回值)。
- **强烈建议使用 struct 实现**，以利用框架的零GC优化。

### Query (查询)
用于获取数据。Query 可以访问 Model 和 System，但不能修改它们。
- 实现 `IQuery<T>`。
- **强烈建议使用 struct 实现**。

### Event (事件)
用于模块间的解耦通信。通过发布/订阅模式，不同模块可以在不知道彼此存在的情况下进行交互。
- 任意 struct 类型均可作为事件。

## 快速开始

### 1. 定义模型 (Model)

```csharp
public class GameModel : AbsModelBase
{
    public int Score { get; set; } = 0;

    public override void Init()
    {
        Score = 0;
    }
}
```

### 2. 定义系统 (System)

```csharp
public class ScoreSystem : AbsSystemBase
{
    public override void Init()
    {
        // 监听事件
        this.RegisterEvent<GameStartedEvent>(OnGameStarted);
    }

    private void OnGameStarted(GameStartedEvent e)
    {
        // 获取模型并修改
        var model = this.GetModel<GameModel>();
        model.Score = 0;
        Debug.Log("Game Started, Score Reset.");
    }
}
```

### 3. 定义架构 (Architecture)

```csharp
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

```csharp
// 事件 (推荐 struct)
public struct GameStartedEvent { }

// 命令 (推荐 struct)
public struct AddScoreCommand : ICommand
{
    private readonly int _amount;
    public AddScoreCommand(int amount) => _amount = amount;

    public void Execute()
    {
        var model = this.GetModel<GameModel>();
        model.Score += _amount;
        Debug.Log($"Score Added: {_amount}, Total: {model.Score}");
    }
}
```

### 5. 表现层使用 (Controller)

```csharp
using UnityEngine;
using EUFramework.Core.MVC;
using EUFramework.Core.MVC.Interface;

public class GamePanel : MonoBehaviour, IController
{
    // 初始化架构
    private void Awake()
    {
        EUCore.SetArchitecture(GameArchitecture.Instance);
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
        // ...
    }
    
    private void OnDestroy()
    {
        // Controller 销毁时会自动注销事件吗？
        // 注意：EUFramework Core 目前需要手动注销，或者使用基于 MonoBehaviour 的封装基类（如果有）
        this.UnRegisterEvent<GameStartedEvent>(OnGameStarted);
    }

    // 实现 IController 接口以获得架构能力扩展方法
    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Instance;
    }
}
```

> **注意**：`IController` 接口通常需要显式实现 `GetArchitecture()`，或者继承自框架提供的 `AbsController` (如果存在) 或自己封装一个基类。

## 进阶使用

### 查询 (Query)

```csharp
public struct GetScoreQuery : IQuery<int>
{
    public int Execute()
    {
        return this.GetModel<GameModel>().Score;
    }
}

// 使用
int score = this.SendQuery(new GetScoreQuery());
```

### 性能优化提示

框架内部针对 `struct` 类型的 Command、Query 和 Event 进行了专门的优化，避免了装箱（Boxing）和垃圾回收（GC）。因此，建议始终使用 `struct` 来定义这些交互对象。

## 文档说明

- **API文档**：请查阅 [API.md](API.md) 获取详细的接口说明。
- **更新日志**：请查阅 [Update.md](Update.md) 获取版本更新历史。
