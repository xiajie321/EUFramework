# EU MVC Core API 文档

## 核心接口

### IArchitecture
架构接口，定义了模块注册和交互的核心方法。通常通过继承 `AbsArchitectureBase<T>` 来实现。

#### 模块管理
- `void RegisterModel<T>(T model)`: 注册 Model。`T` 必须实现 `IModel`。
- `void RegisterSystem<T>(T system)`: 注册 System。`T` 必须实现 `ISystem`。
- `void RegisterUtility<T>(T utility)`: 注册 Utility。`T` 必须实现 `IUtility`。
- `T GetModel<T>()`: 获取 Model。
- `T GetSystem<T>()`: 获取 System。
- `T GetUtility<T>()`: 获取 Utility。

#### 交互方法
- `void SendCommand<T>(T command)`: 发送无返回值命令。`T` 建议为 `struct` 并实现 `ICommand`。
- `TResult SendCommand<TCommand, TResult>(TCommand command)`: 发送有返回值命令。`TCommand` 建议为 `struct` 并实现 `ICommand<TResult>`。
- `TResult SendQuery<TQuery, TResult>(TQuery query)`: 发送查询。`TQuery` 建议为 `struct` 并实现 `IQuery<TResult>`。
- `void SendEvent<T>(T event)`: 发送事件。`T` 建议为 `struct`。
- `void RegisterEvent<T>(Action<T> onEvent)`: 注册事件监听。
- `void UnRegisterEvent<T>(Action<T> onEvent)`: 注销事件监听。

### IController
表现层接口。实现此接口的类（通常是 MonoBehaviour）可以获得架构的扩展方法。
- `IArchitecture GetArchitecture()`: 获取架构实例。必须显式实现。

### ISystem
系统层接口。用于处理业务逻辑。
- `void Init()`: 初始化方法。在此处注册事件监听。
- `void Dispose()`: 销毁方法（可选）。

### IModel
数据层接口。用于存储状态。
- `void Init()`: 初始化方法。在此处初始化数据。
- `void Dispose()`: 销毁方法（可选）。

### IUtility
工具层接口。用于提供通用服务。
- `void Init()`: 初始化方法。

### ICommand / ICommand<TResult>
命令接口。
- `void Execute()`: 执行命令逻辑。
- `TResult Execute()`: 执行命令并返回结果。

### IQuery<TResult>
查询接口。
- `TResult Execute()`: 执行查询逻辑。

## 扩展方法 (CoreExtension)

框架为实现了 `ICanGetModel`, `ICanGetSystem`, `ICanSendCommand` 等接口的对象提供了丰富的扩展方法。
这意味着在 `System`, `Command`, `Query` 以及实现了 `IController` 的类中，可以直接调用：

- `this.GetModel<T>()`
- `this.GetSystem<T>()`
- `this.GetUtility<T>()`
- `this.SendCommand<T>(...)`
- `this.SendQuery<T>(...)`
- `this.RegisterEvent<T>(...)`
- `this.SendEvent<T>(...)`

### 零 GC 优化

为了极致性能，框架提供了带 `ref` 参数的扩展方法，专门配合 `struct` 使用，避免装箱 (Boxing)。

```csharp
// 标准写法 (可能产生装箱，取决于实现)
this.GetModel<MyModel>();

// 零 GC 写法 (针对 struct 调用者)
this.GetModel<TCaller, MyModel>(ref this);
```

> **注意**：通常情况下使用标准写法即可。只有在极高频调用的热路径中才需要考虑手动使用零 GC 扩展。框架内部对 Command/Query 的处理已经默认进行了优化。

## 基类

### AbsArchitectureBase<T>
架构抽象基类，实现了单例模式 (`Instance` 属性) 和 `IArchitecture` 接口。
- `protected abstract void Init()`: 子类必须实现此方法来注册模块。

### AbsModelBase / AbsSystemBase / AbsUtilityBase
各层级的抽象基类，提供了基础的 `Init` 和 `Dispose` 虚方法。建议继承这些基类而非直接实现接口。
