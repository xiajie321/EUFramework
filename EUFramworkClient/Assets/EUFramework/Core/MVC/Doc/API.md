# EU MVC Core API 文档

## 核心接口

### IArchitecture
架构接口，定义了模块注册和交互的核心方法。
- `RegisterModel/System/Utility<T>(T instance)`
- `GetModel/System/Utility<T>()`
- `SendCommand<T>(T command)`
- `SendQuery<T>(T query)`
- `SendEvent<T>(T event)`
- `RegisterEvent<T>(Action<T> onEvent)`
- `UnRegisterEvent<T>(Action<T> onEvent)`

### IController
表现层接口。实现此接口的类（通常是 MonoBehaviour）可以获得架构的扩展方法。
- `GetArchitecture()`: 获取架构实例。

### ISystem
系统层接口。
- `Init()`: 初始化。

### IModel
数据层接口。
- `Init()`: 初始化。

### IUtility
工具层接口。
- `Init()`: 初始化。

### ICommand / ICommand<TResult>
命令接口。
- `Execute()`: 执行命令。

### IQuery<TResult>
查询接口。
- `Execute()`: 执行查询并返回结果。

## 基类

### AbsArchitectureBase<T>
架构基类，实现了单例模式和 IArchitecture 接口。

### AbsModelBase
Model 基类。

### AbsSystemBase
System 基类。

### AbsUtilityBase
Utility 基类。

## 扩展方法

框架为 `ICanGetModel`, `ICanGetSystem`, `ICanSendCommand` 等接口提供了丰富的扩展方法，使得在 Controller, System, Command 中调用架构功能变得非常简便。

### 性能优化 API
针对 `struct` 类型的 Command/Query/Event，提供了带 `TCaller` 泛型参数的扩展方法，以避免装箱开销。

```csharp
// 推荐写法 (无 GC)
this.GetModel<TCaller, TModel>();
```
