# EUObjectPool API 文档

## EUObjectPoolManager 类

`EUFramework.Extension.EUObjectPool.EUObjectPoolManager`

对象池管理器，包含所有自动生成的对象池静态访问入口。
代码生成器会自动向此类添加属性。

## EUAbsObjectPoolBase<T> 类

`EUFramework.Extension.EUObjectPool.EUAbsObjectPoolBase<T>`

纯 C# 对象池基类。

### 属性 (Override)
- `virtual int StartObjectQuantity`: 初始对象数量。
- `virtual int MaxObjectQuantity`: 最大对象数量（负数表示不限制）。

### 方法 (Override)
- `abstract void OnInit()`: 池初始化时调用。
- `abstract void OnCreate(T obj)`: 对象创建时调用。
- `abstract void OnGet(T obj)`: 对象从池中获取时调用。
- `abstract void OnRelease(T obj)`: 对象回收回池时调用。
- `T Get()`: 获取对象。
- `void Release(T obj)`: 回收对象。
- `void Clear()`: 清空池。

## EUAbsGameObjectPoolBase<T> 类

`EUFramework.Extension.EUObjectPool.EUAbsGameObjectPoolBase<T>`

GameObject 对象池基类。`T` 必须继承自 `MonoBehaviour`。

### 方法 (Override)
- `abstract T OnLoadObject()`: **(必需)** 加载并返回一个新的对象实例（通常是 Prefab 的实例化）。
- `abstract void OnInit()`: 池初始化时调用。
- `abstract void OnCreate(T obj)`: 对象创建时调用。
- `abstract void OnGet(T obj)`: 对象获取时调用（自动 SetActive(true)）。
- `abstract void OnRelease(T obj)`: 对象回收时调用（自动 SetActive(false)）。

## 特性

### [EUObjectPool]

标记一个类为对象池，使其能够被代码生成器识别并注册到 `EUObjectPoolManager`。
