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
6. [进阶指南：性能优化与最佳实践](#进阶指南性能优化与最佳实践)
7. [实战教程：制作一个贪吃蛇游戏](#实战教程制作一个贪吃蛇游戏)
8. [示例代码](#示例代码)

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

## 进阶指南：性能优化与最佳实践

EUFarmworker Core 的一大特性是极致的性能优化，特别是在 Struct 类型的 Command、Query 和 Event 中。为了避免 Struct 在调用接口方法时产生装箱（Boxing）操作（即 `this` 指针从值类型转换为引用类型接口），框架提供了一套特定的泛型扩展方法。

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

## 实战教程：制作一个贪吃蛇游戏

为了更好地理解框架的使用，我们将通过一个简单的贪吃蛇游戏来演示如何组织代码。

### 1. 架构定义 (SnakeApp)
首先定义游戏的架构入口。

```csharp
public class SnakeApp : Architecture<SnakeApp>
{
    protected override void Init()
    {
        RegisterModel(new SnakeModel());
        RegisterSystem(new SnakeSystem());
    }
}
```

### 2. 数据层 (SnakeModel)
定义游戏的数据：蛇身位置、食物位置、移动方向。

```csharp
public class SnakeModel : AbstractModel
{
    public List<Vector2Int> Body { get; private set; }
    public Vector2Int FoodPosition { get; set; }
    public Vector2Int Direction { get; set; }

    public override void Init()
    {
        Body = new List<Vector2Int> { new Vector2Int(0, 0) };
        Direction = Vector2Int.right;
        FoodPosition = new Vector2Int(5, 0);
    }
}
```

### 3. 事件定义 (Events)
定义游戏中发生的事件。

```csharp
// 游戏重置/开始事件
public struct GameStartEvent { }

// 食物被吃掉事件
public struct FoodEatenEvent { }

// 游戏结束事件
public struct GameOverEvent { }
```

### 4. 命令定义 (Commands)
定义改变游戏状态的操作。

```csharp
// 开始游戏命令
public struct StartGameCommand : ICommand
{
    public void Execute()
    {
        var model = this.GetModel<StartGameCommand, SnakeModel>();
        model.Body.Clear();
        model.Body.Add(new Vector2Int(0, 0));
        model.Direction = Vector2Int.right;
        
        // 发送游戏开始事件
        this.SendEvent<StartGameCommand, GameStartEvent>(new GameStartEvent());
    }
}

// 改变方向命令
public struct ChangeDirectionCommand : ICommand
{
    public Vector2Int NewDirection;
    
    public void Execute()
    {
        var model = this.GetModel<ChangeDirectionCommand, SnakeModel>();
        // 简单的逻辑：不能直接掉头
        if (model.Direction + NewDirection != Vector2Int.zero)
        {
            model.Direction = NewDirection;
        }
    }
}
```

### 5. 系统层 (SnakeSystem)
处理核心游戏逻辑：移动、碰撞检测。

```csharp
public class SnakeSystem : AbstractSystem
{
    private float _timer;
    private const float MoveInterval = 0.5f;

    public override void Init()
    {
        // 可以在这里监听事件或初始化其他资源
    }

    // 由 Controller 调用，驱动游戏逻辑
    public void OnUpdate()
    {
        _timer += Time.deltaTime;
        if (_timer >= MoveInterval)
        {
            _timer = 0;
            Move();
        }
    }

    private void Move()
    {
        var model = this.GetModel<SnakeModel>();
        var head = model.Body[0];
        var newHead = head + model.Direction;

        // 碰撞检测（墙壁或自身）省略...
        
        // 移动蛇身
        model.Body.Insert(0, newHead);

        // 吃食物检测
        if (newHead == model.FoodPosition)
        {
            // 生成新食物位置（简单逻辑）
            model.FoodPosition += Vector2Int.one; 
            this.SendEvent(new FoodEatenEvent());
        }
        else
        {
            model.Body.RemoveAt(model.Body.Count - 1);
        }
    }
}
```

### 6. 表现层 (SnakeController)
处理输入和渲染。

```csharp
public class SnakeController : MonoBehaviour, IController
{
    private void Awake()
    {
        EUCore.SetArchitecture(SnakeApp.Instance);
    }

    private void Start()
    {
        this.SendCommand(new StartGameCommand());
        this.RegisterEvent<FoodEatenEvent>(OnFoodEaten);
    }
    
    private void OnDestroy()
    {
        this.UnRegisterEvent<FoodEatenEvent>(OnFoodEaten);
    }

    private void Update()
    {
        // 处理输入
        if (Input.GetKeyDown(KeyCode.W)) 
            this.SendCommand(new ChangeDirectionCommand { NewDirection = Vector2Int.up });
        if (Input.GetKeyDown(KeyCode.S)) 
            this.SendCommand(new ChangeDirectionCommand { NewDirection = Vector2Int.down });
        if (Input.GetKeyDown(KeyCode.A)) 
            this.SendCommand(new ChangeDirectionCommand { NewDirection = Vector2Int.left });
        if (Input.GetKeyDown(KeyCode.D)) 
            this.SendCommand(new ChangeDirectionCommand { NewDirection = Vector2Int.right });
            
        // 驱动系统运行
        this.GetSystem<SnakeSystem>().OnUpdate();
    }

    private void OnFoodEaten(FoodEatenEvent e)
    {
        Debug.Log("Food Eaten!");
    }
}
```

通过这个简单的例子，我们可以看到 EUFarmworker Core 如何帮助我们将**数据** (Model)、**逻辑** (System/Command) 和 **表现** (Controller) 清晰地分离，并通过 **事件** (Event) 进行解耦。

## 示例代码
完整的测试示例可以在 `EUFarmworker/Core/Test/TestCore.cs` 中找到。
