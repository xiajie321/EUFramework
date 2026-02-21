# EU 对象池 (EU Object Pool)

## 概述

一个轻量级、高性能的通用对象池系统，旨在简化 Unity 项目中的对象复用管理。支持纯 C# 对象和 Unity GameObject，并提供生命周期回调与自动代码生成功能。

## 功能特点

- **双模式支持**：
  - `EUAbsObjectPoolBase<T>`: 用于纯 C# 对象 (Non-MonoBehaviour)。
  - `EUAbsGameObjectPoolBase<T>`: 用于 Unity GameObject (MonoBehaviour)。
- **生命周期管理**：提供 `OnInit`, `OnCreate`, `OnGet`, `OnRelease` 四个关键生命周期回调。
- **自动管理**：
  - GameObject 池自动处理 `SetActive` 状态。
  - 自动创建根节点并挂载 `DontDestroyOnLoad`。
  - 支持设置初始容量和最大容量。
- **便捷集成**：通过 `[EUObjectPool]` 特性标记，配合代码生成器自动注册到 `EUObjectPoolManager`。

## 快速开始

### 1. 纯 C# 对象池

继承 `EUAbsObjectPoolBase<T>` 并实现抽象方法：

```csharp
using EUFramework.Extension.EUObjectPool;

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
using EUFramework.Extension.EUObjectPool;
using UnityEngine;

[EUObjectPool]
public class MyEffectPool : EUAbsGameObjectPoolBase<MyEffectController>
{
    // 加载预制体的方法
    public override MyEffectController OnLoadObject()
    {
        // 示例：从 Resources 加载
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
`EUFramework -> EU对象池 -> 生成注册信息`

系统会自动更新 `EUObjectPoolManager.cs` 文件，生成静态访问入口。

### 4. 调用方式

```csharp
// 获取对象
var data = EUObjectPoolManager.MyDataPool.Get();
var effect = EUObjectPoolManager.MyEffectPool.Get();

// 回收对象
EUObjectPoolManager.MyDataPool.Release(data);
EUObjectPoolManager.MyEffectPool.Release(effect);
```

## 文档说明

- **API文档**：请查阅 [API.md](API.md) 获取详细的接口说明。
- **更新日志**：请查阅 [Update.md](Update.md) 获取版本更新历史。
