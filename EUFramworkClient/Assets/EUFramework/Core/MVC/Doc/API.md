# EUFramework Core MVC API 文档

## 目录
- [核心类与接口](#核心类与接口)
  - [EUCore](#eucore)
  - [IArchitecture](#iarchitecture)
  - [IController](#icontroller)
  - [ISystem](#isystem)
  - [IModel](#imodel)
  - [IUtility](#iutility)
  - [ICommand / ICommand<TResult>](#icommand--icommandtresult)
  - [IQuery<TResult>](#iquerytresult)
- [基类](#基类)
  - [AbsArchitectureBase<T>](#absarchitecturebaset)
  - [AbsModelBase / AbsSystemBase / AbsUtilityBase](#absmodelbase--abssystembase--absutilitybase)
- [扩展能力接口 (Can Interfaces)](#扩展能力接口-can-interfaces)
- [扩展方法 (CoreExtension)](#扩展方法-coreextension)
  - [常用扩展方法](#常用扩展方法)
  - [零 GC 优化](#零-gc-优化)

---

## 核心类与接口

### EUCore
框架的核心入口静态类，用于设置和管理当前激活的架构。

- `static void SetArchitecture(IArchitecture architecture)`
  - **说明**：设置运行时的架构实例。调用此方法会自动释放上一次设置的架构。
- `static void SetArchitecture<T>() where T : AbsArchitectureBase<T>, new()`
  - **说明**：泛型版本，设置架构实例。

### IArchitecture
架构接口，定义了模块注册和交互的核心方法。通常不需要直接实现此接口，而是通过继承 `AbsArchitectureBase<T>` 来实现。

#### 模块管理
- `void RegisterModel<T>(T model) where T : IModel`：注册 Model。
- `void RegisterSystem<T>(T system) where T : ISystem`：注册 System。
- `void RegisterUtility<T>(T utility) where T : IUtility`：注册 Utility。
- `T GetModel<T>() where T : class, IModel`：获取已注册的 Model。
- `T GetSystem<T>() where T : class, ISystem`：获取已注册的 System。
- `T GetUtility<T>() where T : class, IUtility`：获取已注册的 Utility。

#### 交互方法
- `void SendCommand<T>(T command) where T : ICommand`：发送无返回值命令。
- `TResult SendCommand<TCommand, TResult>(TCommand command) where TCommand : ICommand<TResult>`：发送有返回值命令。
- `TResult SendQuery<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>`：发送查询。
- `void SendEvent<T>() where T : new()`：发送无参事件。
- `void SendEvent<T>(T e)`：发送带参事件。
- `void RegisterEvent<T>(Action<T> onEvent)`：注册事件监听。
- `void UnRegisterEvent<T>(Action<T> onEvent)`：注销事件监听。

### IController
表现层接口。实现此接口的类（通常是 `MonoBehaviour`）可以获得架构的扩展方法。

> **重要提示**：
> 1. `IController` 本身是一个组合接口，继承了 `ICanGetModel`, `ICanGetSystem`, `ICanGetUtility`, `ICanSendCommand`, `ICanSendQuery`, `ICanSendEvent`, `ICanRegisterEvent`。
> 2. **使用前置**：必须先调用 `EUCore.SetArchitecture(...)` 设置当前架构，`IController` 的扩展方法才能正常工作。

### ISystem
系统层接口。用于处理业务逻辑。
- `void Init()`：初始化方法。通常在此处注册事件监听。
- `void Dispose()`：销毁方法。用于清理资源。

### IModel
数据层接口。用于存储状态。
- `void Init()`：初始化方法。通常在此处初始化默认数据。
- `void Dispose()`：销毁方法。用于清理资源。

### IUtility
工具层接口。用于提供通用服务。
- `void Init()`：初始化方法。

### ICommand / ICommand<TResult>
命令接口。用于执行状态变更。
- `void Execute()`：执行命令逻辑（无返回值）。
- `TResult Execute()`：执行命令并返回结果。

### IQuery<TResult>
查询接口。用于获取数据。
- `TResult Execute()`：执行查询逻辑并返回结果。

---

## 基类

### AbsArchitectureBase<T>
架构抽象基类，实现了单例模式 (`Instance` 属性) 和 `IArchitecture` 接口。

- `protected abstract void Init()`：子类必须实现此方法，在其中调用 `RegisterModel`, `RegisterSystem`, `RegisterUtility` 进行模块注册。
- `protected virtual void OnDispose()`：子类可重写此方法，在架构销毁时执行自定义清理逻辑。

**生命周期顺序**：
- **初始化**：
  1. 调用 `Init()` 进行模块注册。
  2. 依次初始化 Utility -> Model -> System。
- **销毁**：
  1. 调用 `OnDispose()`。
  2. 依次销毁 System -> Model -> Utility。
  3. 清理事件系统 (`TypeEventSystem.Clear()`)。

### AbsModelBase / AbsSystemBase / AbsUtilityBase
各层级的抽象基类，提供了基础的 `Init` 和 `Dispose` 虚方法。
- `protected virtual void OnInit()`：供子类重写的初始化逻辑。
- `protected virtual void OnDispose()`：供子类重写的销毁逻辑。

> **建议**：在实际开发中，建议继承这些基类而非直接实现 `IModel`、`ISystem` 等接口。

---

## 扩展能力接口 (Can Interfaces)

框架将能力拆分为细粒度的接口，`IController`、`AbsSystemBase` 等类组合了这些接口以获得相应能力。

- `ICanGetModel`：获取 Model 的能力。
- `ICanGetSystem`：获取 System 的能力。
- `ICanGetUtility`：获取 Utility 的能力。
- `ICanSendCommand`：发送 Command 的能力。
- `ICanSendQuery`：发送 Query 的能力。
- `ICanSendEvent`：发送 Event 的能力。
- `ICanRegisterEvent`：注册/注销 Event 的能力。
- `ICanInit`：初始化能力。

---

## 扩展方法 (CoreExtension)

框架为实现了上述 `ICan...` 接口的对象提供了丰富的扩展方法。这意味着在 `System`, `Command`, `Query` 以及实现了 `IController` 的类中，可以直接调用这些方法。

> **机制说明**：扩展方法内部通过 `CoreExtension` 持有的静态架构实例来转发调用。因此，务必确保在使用前调用了 `EUCore.SetArchitecture`。

### 常用扩展方法

- `this.GetModel<T>()`
- `this.GetSystem<T>()`
- `this.GetUtility<T>()`
- `this.SendCommand<T>(new T())`
- `this.SendQuery<T>(new T())`
- `this.RegisterEvent<T>(OnEvent)`
- `this.UnRegisterEvent<T>(OnEvent)`
- `this.SendEvent<T>()` 或 `this.SendEvent<T>(new T())`

### 零 GC 优化

为了极致性能，框架提供了带 `ref` 参数的扩展方法，专门配合 `struct` 使用，避免装箱 (Boxing)。同时，内部的事件系统利用了 **静态泛型缓存 (Static Generic Cache)** 技术。

#### 静态泛型缓存原理
在 C# 中，泛型类的静态字段是针对每个具体的泛型参数类型独立存储的。
- **Mono (JIT)**：在运行时（JIT编译阶段），运行时环境会直接确定静态字段的内存地址。访问 `EventCache<T>.Handlers` 等同于直接访问内存地址，无需任何查表或哈希计算。这种优化在 Unity 的 Mono 打包版本中尤为显著。
- **IL2CPP (AOT)**：编译器会为每个泛型实例生成对应的 C++ 代码，静态字段被编译为全局变量或类静态成员，访问速度同样极快（O(1)）。

```csharp
// 标准写法 (可能产生装箱，取决于实现)
this.GetModel<MyModel>();

// 零 GC 写法 (针对 struct 调用者)
this.GetModel<TCaller, MyModel>(ref this);
```

> **注意**：通常情况下使用标准写法即可。只有在极高频调用的热路径中，且调用者本身是 `struct` 时，才需要考虑手动使用零 GC 扩展。框架内部对 Command/Query 的处理已经默认进行了优化。
