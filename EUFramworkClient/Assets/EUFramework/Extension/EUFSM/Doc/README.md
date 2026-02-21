# EU FSM 有限状态机

## 概述

EU FSM 是一个轻量级、高性能的通用有限状态机 (FSM) 系统，专为 Unity 开发设计。它使用泛型枚举作为状态标识，避免了字符串比较的开销，并提供了完整的状态生命周期管理。

## 功能特点

- **泛型枚举 ID**：使用 `enum` 作为状态标识，类型安全且高效。
- **高性能**：内部使用 `List` 结合 `Unsafe.As` 进行索引映射，避免了字典查找 (Dictionary) 的开销。
- **完整生命周期**：支持 `OnEnter`, `OnExit`, `OnUpdate`, `OnFixedUpdate` 四个关键生命周期回调。
- **状态基类**：提供 `EUAbsStateBase<TStateId, TOwner>`，方便状态类直接访问状态机和所有者 (`Owner`)。
- **状态所有者**：通过泛型绑定所有者类型，方便在状态中操作角色或对象。

## 快速开始

### 1. 定义状态枚举

首先定义一个枚举来标识所有的状态：

```csharp
public enum PlayerState
{
    Idle,
    Run,
    Jump,
    Attack
}
```

### 2. 实现状态类

继承 `EUAbsStateBase<TStateId, TOwner>` 并实现具体逻辑：

```csharp
using EUFramwork.Extension.FSM;
using UnityEngine;

// 泛型参数：<状态枚举, 所有者类型>
public class PlayerIdleState : EUAbsStateBase<PlayerState, PlayerController>
{
    public PlayerIdleState(EUFSM<PlayerState> fsm, PlayerController owner) : base(fsm, owner)
    {
    }

    public override void OnEnter()
    {
        Debug.Log("Enter Idle");
        // 播放待机动画
        Owner.PlayAnimation("Idle");
    }

    public override void OnUpdate()
    {
        // 检测输入切换状态
        if (Input.GetKey(KeyCode.W))
        {
            // 切换到跑步状态
            // 注意：这里需要通过 Owner 或其他方式访问 FSM 进行切换
            // 或者在构造函数中保存 FSM 引用（基类已保存 _fsm 但为私有，通常通过 Owner 暴露方法切换）
            _fsm.ChangeState(PlayerState.Run);
        }
    }

    public override void OnExit()
    {
        Debug.Log("Exit Idle");
    }
}
```

### 3. 在 MonoBehaviour 中驱动状态机

在你的控制器脚本中初始化并驱动状态机：

```csharp
using EUFramwork.Extension.FSM;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // 声明状态机
    public EUFSM<PlayerState> Fsm { get; private set; }

    void Start()
    {
        // 1. 初始化状态机
        Fsm = new EUFSM<PlayerState>();

        // 2. 添加状态
        // 注意：构造函数中传入 fsm 和 owner (this)
        Fsm.AddState(PlayerState.Idle, new PlayerIdleState(Fsm, this));
        Fsm.AddState(PlayerState.Run, new PlayerRunState(Fsm, this));
        // ... 添加其他状态

        // 3. 启动状态机，进入初始状态
        Fsm.StartState(PlayerState.Idle);
    }

    void Update()
    {
        // 4. 驱动状态机 Update
        Fsm.Update();
    }

    void FixedUpdate()
    {
        // 5. 驱动状态机 FixedUpdate
        Fsm.FixedUpdate();
    }
    
    // 示例方法供状态调用
    public void PlayAnimation(string animName)
    {
        // ...
    }
}
```

## 文档说明

- **API文档**：请查阅 [API.md](API.md) 获取详细的接口说明。
- **更新日志**：请查阅 [Update.md](Update.md) 获取版本更新历史。

## 注意事项

1. **状态 ID 必须是枚举**：泛型 `TKey` 约束为 `struct, Enum`。
2. **列表扩容**：内部使用 `List` 存储状态，索引为枚举值的整数值。建议枚举值从 0 开始连续定义，以避免 List 空间浪费。
3. **初始化顺序**：必须先 `AddState` 添加状态，才能调用 `StartState` 或 `ChangeState`。
