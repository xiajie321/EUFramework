# EUFarmworker Core 架构文档

## 目录 (API 导航)

*   [1. 项目概述](#1-项目概述)
*   [2. 架构设计原则](#2-架构设计原则)
*   [3. 性能优化与技术实现](#3-性能优化与技术实现)
*   [4. 详细使用说明](#4-详细使用说明)
    *   [4.1 定义架构 (Architecture)](#41-定义架构-architecture)
    *   [4.2 定义模型 (Model)](#42-定义模型-model)
    *   [4.3 定义系统 (System)](#43-定义系统-system)
    *   [4.4 定义 Command (写操作)](#44-定义-command-写操作)
    *   [4.5 定义 Query (读操作)](#45-定义-query-读操作)
    *   [4.6 定义 Event (事件)](#46-定义-event-事件)
    *   [4.7 在 View (MonoBehaviour) 中使用](#47-在-view-monobehaviour-中使用)
    *   [4.8 自动管理生命周期 (Extensions)](#48-自动管理生命周期-extensions)
*   [5. 最佳实践](#5-最佳实践)

## 1. 项目概述

**EUFarmworker Core** 是一套专为 Unity 开发的高性能、轻量级 **游戏架构框架 (Game Architecture Framework)**。
它基于 **IOC (控制反转)** 和 **CQRS (命令查询职责分离)** 思想设计，旨在解决中大型 Unity 项目中常见的代码耦合严重、逻辑混乱和维护困难等问题。

### 1.1 灵感来源与致敬

本架构的核心设计灵感主要来源于 **QFramework**。
QFramework 以其优雅的 API 设计（如 `this.GetSystem`、`this.SendCommand`）和清晰的架构分层，在 Unity 中文社区中树立了良好的标杆。

EUFarmworker Core 继承了 QFramework **"易于上手、代码整洁"** 的优良基因，保留了其广受好评的 API 风格和架构思想。
在此基础上，我们针对 **运行时性能 (Runtime Performance)** 进行了深度的底层重构。我们的目标是在维持 OOP (面向对象) 开发便利性的前提下，将框架层面的性能开销降至最低。

## 2. 架构设计原则

本框架遵循严格的分层设计与单向数据流原则，确保系统的可维护性与可扩展性。

### 2.1 四层架构体系

1.  **表现层 (Presentation Layer / View)**
    *   **定义**：负责图形渲染、UI 交互及用户输入捕获。
    *   **职责**：只做"表面功夫"。它通过发送 Command 修改数据，通过 Query 获取数据，通过监听 Event 响应变化。
    *   **禁忌**：严禁直接修改 Model，严禁包含复杂的业务逻辑算法。
    *   **组件**：`MonoBehaviour` 脚本、UI 面板、特效控制器。

2.  **系统层 (System Layer)**
    *   **定义**：承载核心业务逻辑 (Business Logic) 的容器。
    *   **职责**：维护系统的整体状态，协调多个 Model 的工作。响应 Command，触发 Event。
    *   **组件**：如 `AchievementSystem` (成就系统), `InventorySystem` (背包系统)。

3.  **模型层 (Model Layer)**
    *   **定义**：数据的持有者与管理者。
    *   **职责**：维护数据的持久化状态 (State)。使用 `BindableProperty<T>` 提供响应式数据能力。
    *   **禁忌**：不应包含复杂的业务流程逻辑，只负责数据的存取与基础校验。
    *   **组件**：如 `PlayerModel` (玩家数据), `SettingModel` (设置数据)。

4.  **工具层 (Utility Layer)**
    *   **定义**：提供通用的、无状态的基础设施支持。
    *   **职责**：封装底层技术细节，提供易用的 API。与具体业务逻辑完全解耦。
    *   **组件**：`Storage` (存储), `Network` (网络), `Math` (算法库)。

### 2.2 核心交互模式 (CQRS)

框架采用 **CQRS (Command Query Responsibility Segregation)** 模式来规范模块间的通信，清晰分离了"读"与"写"的关注点：

*   **Command (写操作)**：
    *   用于**修改**系统状态或数据。
    *   **特征**：无返回值，语义明确（如 `UpgradeSkillCommand`）。
    *   **流向**：View -> System/Model。
*   **Query (读操作)**：
    *   用于**获取**系统数据。
    *   **特征**：必须有返回值，无副作用（不修改任何状态）。
    *   **流向**：View <- System/Model。
*   **Event (事件通知)**：
    *   用于**广播**状态的变化。
    *   **特征**：发布/订阅模式，实现模块间的解耦。
    *   **流向**：Model/System -> View。

## 3. 性能优化与技术实现

本框架的核心竞争力在于对 **GC (Garbage Collection)** 的极致控制与运行时性能优化。

### 3.1 性能定位：OOP 架构中的极限

在讨论性能时，我们必须严谨地区分 **架构类型**。

*   **ECS (Entity Component System)**：如 Unity DOTS 或 Entitas。通过数据导向设计 (DOD) 和内存连续布局，极大提高了 CPU 缓存命中率，适合处理海量（10万+）同类实体。
*   **OOP (Object Oriented Programming)**：如传统的 MVC/IOC 框架。优势在于代码组织直观、开发效率高，但在处理海量对象时，因内存分散导致的 Cache Miss 是其天然劣势。

**EUFarmworker Core 的定位是：在 OOP 范畴内做到性能极致。**

我们不追求替代 ECS 去处理海量单位的物理运算。我们的目标是解决 **UI 系统、游戏流程控制、业务逻辑模块** 中的性能痛点——即在这些传统 OOP 领域中，消除因框架设计不当（如滥用装箱、频繁 new 对象）导致的额外 GC 开销。

### 3.2 零分配通信 (Zero-Allocation Communication)

在传统的 C# OOP 框架中，消息传递通常伴随着对象的创建（`new Command()`），这在高频逻辑循环中会产生大量的内存垃圾，导致 GC 峰值。

EUFarmworker Core 采用了以下策略彻底解决此问题：

*   **Struct over Class**：所有的 `ICommand`、`IQuery`、`IEvent` 实现均采用 `struct` (结构体)。结构体在栈 (Stack) 上分配，随作用域结束立即回收，**不产生任何 GC 压力**。
*   **泛型约束避免装箱 (No Boxing)**：
    框架接口采用严格的泛型约束（`where T : struct`），确保结构体在传递过程中**不会**被装箱为引用类型（Interface）。
    ```csharp
    // 编译器会生成专门的泛型代码，直接传递结构体，无堆内存分配
    void SendCommand<T>(T command) where T : struct, ICommand;
    ```

> **技术说明**：这意味着，如果您的业务逻辑（Command/Event 内部）不自行产生 GC，那么使用本框架进行高频的模块间通信将**不会引入任何额外的 GC 负担**。

### 3.2 静态泛型缓存 (Static Generic Caching)

为了解决 IOC 容器常见的性能瓶颈（字典查找开销），框架内部实现了 **静态泛型缓存**。

*   **机制**：利用 C# 泛型类的静态字段特性 (`static class InstanceCache<T>`)。
*   **效果**：首次获取模块时进行查找并缓存，后续所有的 `GetSystem<T>()` 或 `GetModel<T>()` 调用等同于直接访问静态变量。
*   **复杂度**：从 O(1) 的哈希查找优化为 **纯内存寻址**，极大地提升了模块获取速度。

### 3.3 响应式数据优化

内置的 `BindableProperty<T>` 针对值类型进行了特殊优化：
*   在 Setter 中使用 `EqualityComparer<T>.Default` 进行比对。
*   避免了传统 `object.Equals` 导致的装箱操作。
*   确保只有数据真正变化时才触发事件，减少不必要的逻辑执行与 UI 刷新。

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

**使用场景**：
*   定义游戏中的数据状态（如：分数、玩家生命值、背包物品列表）。
*   需要数据变更通知时（BindableProperty）。

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

**使用场景**：
*   实现具体的游戏规则和业务逻辑（如：计算得分、判断游戏胜负、处理技能释放）。
*   需要跨模型交互或管理复杂状态变更时。

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

**使用场景**：
*   **System 通知 View**：游戏结束、成就解锁、受到伤害。
*   **Model 通知 View**：金币变化、血量变化（也可使用 BindableProperty）。
*   **跨模块通信**：敌人死亡通知任务系统计数。

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

### 4.8 自动管理生命周期 (Extensions)

核心库内置了两个便捷的扩展方法来自动管理事件注销：

1.  `UnRegisterWhenGameObjectDestroyed(gameObject)`: 当 GameObject 销毁 (OnDestroy) 时自动注销。适用于 `Start` 或 `Awake` 中注册的事件。
2.  `UnRegisterWhenGameObjectDisabled(gameObject)`: 当 GameObject 禁用 (OnDisable) 时自动注销。适用于 `OnEnable` 中注册的事件。

#### 示例：在 OnEnable/OnDisable 中使用

```csharp
    void OnEnable()
    {
        // 在 OnEnable 中注册，并在 OnDisable 时自动注销
        this.RegisterEvent<GamePassEvent>(e => 
        {
            Debug.Log("游戏通关！");
        }).UnRegisterWhenGameObjectDisabled(gameObject);
    }
```

## 5. 最佳实践

1.  **始终使用 Struct**：对于 Command、Query 和 Event，永远不要使用 `class`，否则将失去本框架的核心优势。
2.  **保持 Model 纯净**：Model 中不应包含复杂的业务逻辑，只包含数据定义和基础的数据操作。
3.  **System 处理逻辑**：所有的 `if/else`、状态判断、计算逻辑都应放在 System 中。
4.  **View 只负责表现**：View 层不应直接修改 Model，必须通过 Command 或 System 方法来改变状态。
