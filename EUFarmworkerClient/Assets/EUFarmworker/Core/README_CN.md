# EUFarmworker Core 架构文档

## 1. 设计来源

本架构的核心设计思想深受 **QFramework** 的启发。QFramework 以其简洁的 API 设计（如 `this.GetSystem`、`this.SendCommand`）和清晰的分层架构（MVC/DDD 混合体）在 Unity 开发者中广受欢迎。

**EUFarmworker Core** 旨在继承 QFramework **"易于上手、代码整洁"** 的优良基因，同时针对高性能场景（如移动端游戏、高频逻辑循环）进行了底层的重构与优化。

## 2. 设计思路与架构分层

本架构遵循经典的 **四层架构** 设计：

1.  **表现层 (View/Controller)**：
    *   负责处理用户输入和界面显示。
    *   **只做**：发送 Command、发送 Query、监听 Event。
    *   **不做**：直接修改 Model、直接处理复杂业务逻辑。
2.  **系统层 (System)**：
    *   负责处理业务逻辑（Business Logic）。
    *   管理多个 Model 的状态变更。
    *   响应 Command，发送 Event。
3.  **模型层 (Model)**：
    *   负责管理数据（Data）。
    *   使用 `BindableProperty` 提供响应式数据。
    *   **只做**：存储数据、数据的序列化/反序列化。
4.  **工具层 (Utility)**：
    *   负责提供通用的基础设施（如存储、网络、算法）。
    *   无状态，纯功能性支持。

### 核心交互规则

*   **Command (命令)**：用于**修改**数据。View -> System/Model。
*   **Query (查询)**：用于**获取**数据。View <- System/Model。
*   **Event (事件)**：用于**通知**变化。Model/System -> View。

## 3. 核心优势：为什么这样设计？

相较于原版 QFramework 或传统的 OOP 架构，EUFarmworker Core 最大的改进在于 **"零 GC (Zero Garbage Collection)"**。

### 3.1 传统架构的痛点

在传统的命令模式实现中，每次操作通常需要创建一个对象：

```csharp
// 传统方式：每次调用都会在堆(Heap)上分配一个新的对象
this.SendCommand(new AttackCommand(target)); 
// 结果：产生 GC Garbage，导致内存碎片，增加 GC 触发频率，引起卡顿。
```

### 3.2 EUFarmworker 的解决方案

我们利用 C# 的 **Struct (结构体)** 和 **泛型约束** 彻底解决了这个问题：

1.  **Struct 代替 Class**：Command、Query 和 Event 全部定义为 `struct`。结构体是值类型，分配在栈(Stack)上，随作用域结束自动销毁，不经过 GC。
2.  **泛型约束避免装箱**：
    ```csharp
    // 架构层定义
    void SendCommand<T>(T command) where T : struct, ICommand;
    ```
    通过 `where T : struct` 约束，编译器会生成专门的泛型代码路径，避免将 struct 装箱(Boxing)成接口对象（装箱会导致堆分配）。

### 3.3 性能对比

| 特性 | 传统 QFramework (Class) | EUFarmworker Core (Struct) |
| :--- | :--- | :--- |
| **内存分配** | 每次调用都在堆上分配 | **0 分配** (栈上分配) |
| **GC 压力** | 高 (高频调用时) | **无** |
| **调用开销** | 虚方法调用 | 直接调用 (泛型特化) |
| **数据局部性** | 差 (分散在堆中) | 好 (连续在栈上) |

## 4. 详细使用说明

### 4.1 定义架构 (Architecture)

首先，定义你的游戏架构入口。

```csharp
using EUFarmworker.Core;

public class GameApp : Architecture<GameApp>
{
    protected override void Init()
    {
        // 注册模块顺序：Utility -> Model -> System
        RegisterModel(new ScoreModel());
        RegisterSystem(new ScoreSystem());
    }
}
```

### 4.2 定义模型 (Model)

使用 `BindableProperty` 来管理需要响应式更新的数据。

```csharp
public interface IScoreModel : IModel
{
    BindableProperty<int> Score { get; }
}

public class ScoreModel : AbstractModel, IScoreModel
{
    public BindableProperty<int> Score { get; } = new BindableProperty<int>(0);

    protected override void Init()
    {
        // 初始化数据，例如从本地加载
        Score.Value = 0;
    }
}
```

### 4.3 定义系统 (System)

系统负责具体的逻辑实现。

```csharp
public interface IScoreSystem : ISystem
{
    void AddScore(int amount);
}

public class ScoreSystem : AbstractSystem, IScoreSystem
{
    protected override void Init() { }

    public void AddScore(int amount)
    {
        // System 可以直接访问 Model
        var scoreModel = this.GetModel<IScoreModel>();
        scoreModel.Score.Value += amount;
        
        // 逻辑完成后，可以发送事件（如果 BindableProperty 不够用）
        if (scoreModel.Score.Value > 100)
        {
            this.SendEvent(new GamePassEvent());
        }
    }
}
```

### 4.4 定义 Command (写操作)

**关键：使用 `struct`**。

```csharp
public struct AddScoreCommand : ICommand
{
    private readonly int mAmount;

    public AddScoreCommand(int amount)
    {
        mAmount = amount;
    }

    public void Execute(IArchitecture architecture)
    {
        // Command 调用 System
        architecture.GetSystem<IScoreSystem>().AddScore(mAmount);
    }
}
```

### 4.5 定义 Query (读操作)

**关键：使用 `struct`**。

```csharp
public struct GetScoreQuery : IQuery<int>
{
    public int Do(IArchitecture architecture)
    {
        return architecture.GetModel<IScoreModel>().Score.Value;
    }
}
```

### 4.6 定义 Event (事件)

**关键：使用 `struct`**。

```csharp
public struct GamePassEvent { }
```

### 4.7 在 View (MonoBehaviour) 中使用

```csharp
using UnityEngine;
using EUFarmworker.Core;

public class GamePanel : MonoBehaviour, IBelongToArchitecture
{
    // 1. 连接架构
    public IArchitecture GetArchitecture() => GameApp.Interface;

    void Start()
    {
        // 2. 注册事件 (监听 Model 变化)
        var scoreModel = this.GetModel<IScoreModel>();
        scoreModel.Score.RegisterWithInitValue(OnScoreChanged).UnRegisterWhenGameObjectDestroyed(gameObject);
        
        // 3. 监听架构事件
        this.RegisterEvent<GamePassEvent>(e => 
        {
            Debug.Log("游戏通关！");
        }).UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void OnScoreChanged(int score)
    {
        Debug.Log($"当前分数: {score}");
    }

    // UI 按钮点击回调
    public void OnClickAddButton()
    {
        // 4. 发送命令 (交互)
        this.SendCommand(new AddScoreCommand(10));
    }
    
    // 获取数据
    public void PrintScore()
    {
        // 5. 发送查询
        var score = this.SendQuery(new GetScoreQuery());
        Debug.Log(score);
    }
}
```

*(注：`UnRegisterWhenGameObjectDestroyed` 是一个推荐的扩展方法，需自行实现以自动管理生命周期，核心库中提供了基础的 `UnRegister` 接口)*

## 5. 最佳实践

1.  **始终使用 Struct**：对于 Command、Query 和 Event，永远不要使用 `class`，否则将失去本框架的核心优势。
2.  **保持 Model 纯净**：Model 中不应包含复杂的业务逻辑，只包含数据定义和基础的数据操作。
3.  **System 处理逻辑**：所有的 `if/else`、状态判断、计算逻辑都应放在 System 中。
4.  **View 只负责表现**：View 层不应直接修改 Model，必须通过 Command 或 System 方法来改变状态。
