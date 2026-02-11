# EU 对象池 (EU Object Pool)

一个轻量级、高性能的通用对象池系统，旨在简化 Unity 项目中的对象复用管理。支持纯 C# 对象和 Unity GameObject，并提供生命周期回调与自动代码生成功能。

## 功能特点

- **双模式支持**：
  - `EUAbsObjectPoolBase<T>`: 用于纯 C# 对象 (Non-MonoBehaviour)。
  - `EUAbsGameObjectPoolBase<T>`: 用于 Unity GameObject (MonoBehaviour)。
- **生命周期管理**：提供 `OnInit` (初始化), `OnCreate` (创建), `OnGet` (获取), `OnRelease` (回收) 四个关键生命周期回调。
- **自动管理**：
  - GameObject 池自动处理 `SetActive` 状态。
  - 自动创建根节点并挂载 `DontDestroyOnLoad`。
  - 支持设置初始容量 (`StartObjectQuantity`) 和最大容量 (`MaxObjectQuantity`)。
- **便捷集成**：通过 `[EUObjectPool]` 特性标记，配合代码生成器自动注册到 `EUObjectPoolManager`。

## 使用方法

### 1. 纯 C# 对象池

继承 `EUAbsObjectPoolBase<T>` 并实现抽象方法：

```csharp
using EUFarmworker.Extension.EUObjectPool;

[EUObjectPool] // 标记以自动生成访问代码
public class MyDataPool : EUAbsObjectPoolBase<MyData>
{
    // 配置池大小（可选）
    public override int StartObjectQuantity => 20;
    public override int MaxObjectQuantity => 200;

    public override void OnInit() { }
    public override void OnCreate(MyData obj) { }
    public override void OnGet(MyData obj) { }
    public override void OnRelease(MyData obj) { }
}
```

### 2. GameObject 对象池

继承 `EUAbsGameObjectPoolBase<T>`，`T` 必须是 `MonoBehaviour`：

```csharp
using EUFarmworker.Extension.EUObjectPool;
using UnityEngine;

[EUObjectPool]
public class MyEffectPool : EUAbsGameObjectPoolBase<MyEffectController>
{
    // 加载预制体的方法
    public override MyEffectController OnLoadObject()
    {
        // 示例：从 Resources 加载，也可以使用 Addressables 或其他方式
        var prefab = Resources.Load<GameObject>("Prefabs/MyEffect");
        return prefab.GetComponent<MyEffectController>();
    }

    public override void OnInit() { }
    public override void OnCreate(MyEffectController obj) { }
    public override void OnGet(MyEffectController obj) 
    {
        // 对象被获取时的逻辑，例如重置状态
    }
    public override void OnRelease(MyEffectController obj) 
    {
        // 对象回收时的逻辑
    }
}
```

### 3. 代码生成

编写完对象池类后，点击 Unity 菜单栏：
`EUFarmworker -> EU对象池 -> 生成注册信息`

系统会自动更新 `EUObjectPoolManager.cs` 文件，生成静态访问入口。

### 4. 调用方式

使用 `EUObjectPoolManager` 进行调用：

```csharp
// 获取对象
var data = EUObjectPoolManager.MyDataPool.Get();
var effect = EUObjectPoolManager.MyEffectPool.Get();

// 回收对象
EUObjectPoolManager.MyDataPool.Release(data);
EUObjectPoolManager.MyEffectPool.Release(effect);
```

## 核心 API

- `Get()`: 从池中获取一个对象。如果池为空，会自动创建新对象。
- `Release(T obj)`: 将对象归还给池。如果池已满，可能会销毁对象（对于 GameObject）。
- `Clear()`: 清空对象池。
- `Pool`: 获取内部 Stack 容器。

## 注意事项

- 请确保 GameObject 池的预制体上挂载了对应的 MonoBehaviour 脚本。
- `MaxObjectQuantity` 设置为负数时表示不限制容量。
- 自动生成的代码位于 `EUObjectPoolManager` 中，请勿手动修改该文件的生成部分。
