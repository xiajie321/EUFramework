# EUSingleton API 文档

## EUSingleton<T> 类

`EUFramework.EUSingleton<T>`

普通 C# 对象的单例基类。`T` 必须是类且具有无参构造函数。

### 属性

- `static T Instance`: 获取单例实例。首次调用时会创建实例。

### 方法 (Override)

- `protected virtual void OnCreate()`: 实例创建时调用，用于初始化。

## EUSingletonMono<T> 类

`EUFramework.EUSingletonMono<T>`

MonoBehaviour 的单例基类。`T` 必须继承自 `MonoBehaviour`。

### 属性

- `static T Instance`: 获取单例实例。
  - 如果场景中已存在该类型的对象，则直接使用。
  - 如果不存在，会自动创建一个名为 `[Singleton] TypeName` 的 GameObject 并挂载该脚本。
  - 自动调用 `DontDestroyOnLoad`。

### 方法 (Override)

- `protected virtual void OnCreate()`: 实例创建（Awake）时调用，用于初始化。
