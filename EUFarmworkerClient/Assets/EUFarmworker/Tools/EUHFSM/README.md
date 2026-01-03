# EUFSM 分层状态机使用文档

EUFSM 是一个高效、轻量级且支持分层的状态机实现。它使用泛型设计以避免装箱拆箱，从而提供高性能的状态管理。

## 特性

- **高性能**：使用泛型 嵌套，实现复杂的分层状态逻辑。
- **易于使用**：提供简洁的 API 用于状态的添加、移除和切换。
- **生命周期管理**：支持 `OnEnter`, `OnUpdate`, `OnFixedUpdate`, `OnExit` 等标准生命周期回调。
- **历史状态追踪**：支持获取上一次的状态，方便实现“返回上一状态”等逻辑。

## 快速开始

### 1. 定义状态 ID 和目标对象

首先，定义用于标识状态的 ID（`TId` 和 `TTarget`，避免了值类型 ID（如枚举）的装箱拆箱。
- **分层支持**：`FSM` 类本身也是一个 `AbstractState`，允许状态机通常是枚举）和状态机将要控制的目标对象。

```csharp
public enum PlayerStateId
{
    Idle,
    Run,
    Jump
}

public class PlayerController : MonoBehaviour
{
    // ... 玩家控制逻辑
}
```

### 2. 实现具体状态

继承 `AbstractState<TId, TTarget>` 来创建具体的状态类。

```csharp
using EUFarmworker.Tools.EUFSM;
using UnityEngine;

public class IdleState : AbstractState<PlayerStateId, PlayerController>
{
    public override void OnEnter()
    {
        Debug.Log("进入 Idle 状态");
        // 播放待机动画等
    }

    public override void OnUpdate()
    {
        // 检查转换条件
        if (Input.GetKeyDown(KeyCode.Space))
        {
("退出 Idle 状态");
    }
}

public class RunState : AbstractState<PlayerStateId, PlayerController>
{
    public override void OnEnter()
    {
        Debug.Log("进入 Run 状态");
    }

    public override void OnUpdate()
                ChangeState(PlayerStateId.Jump);
        }
        else if (Input.GetAxis("Horizontal") != 0)
        {
            ChangeState(PlayerStateId.Run);
        }
    }

    public override void OnExit()
    {
        Debug.Log{
        if (Input.GetAxis("Horizontal") == 0)
        {
            ChangeState(PlayerStateId.Idle);
        }
    }
}
```

### 3. 初始化和使用状态机

在目标对象（如 `PlayerController`）中初始化并驱动状态机。

```csharp
using EUFarmworker.Tools.EUFSM;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private FSM<PlayerStateId, PlayerController> mFSM;

    void Start()
    {
        // 初始化状态机，传入目标对象 (this)
        // 可选参数 capacity 指定状态字典的初始容量
        mFSM = new FSM<PlayerStateId, PlayerController>(this);

        // 添加状态
        mFSM.AddState(PlayerStateId.Idle, new IdleState());
        mFSM.AddState(PlayerStateId.Run, new RunState());
        // ... 添加其他状态

        // 启动状态机，进入默认状态
        mFSM.ChangeState(PlayerStateId.Idle);
    }

    void Update()
    {
        // 驱动状态机更新
        mFSM.OnUpdate();
    }

    void FixedUpdate()
    {
        // 如果需要物理更新
        mFSM.OnFixedUpdate();
    }
    
    void OnDestroy()
    {
        // 清理状态机```
        mFSM.Clear();
    }
}
非常有用。

```csharp
public class JumpState : AbstractState<PlayerStateId, PlayerController>
{
    public override void OnEnter()
    {
        // 检查我们是从哪个状态进入跳跃的
        if (mFSM.PreviousStateId == PlayerStateId

## 获取上一次状态

在任何状态中，你可以通过 `mFSM.PreviousStateId` 或 `mFSM.PreviousState` 访问上一次的状态。这在需要根据前一个状态执行不同逻辑时```.Run)
        {
            Debug.Log("冲刺跳跃！");
        }
        else
        {
            Debug.Log("原地跳跃");
        }
    }
}


## 完整示例：敌人 AI (巡逻与攻击)

以下是一个完整的敌人 AI 示例，展示了如何实现巡逻和攻击状态，并在它们之间切换。

```csharp
using UnityEngine;
using EUFarmworker.Tools.EUFSM;

// 1. 定义状态 ID
public enum EnemyStateId
{
    Patrol, // 巡逻
    Attack, // 攻击
    Chase   // 追逐
}

// 2. 定义敌人控制器 (Target)
public class EnemyController : MonoBehaviour
{
    public Transform[] PatrolPoints; // 巡逻点
    public float MoveSpeed = 3f;
    public float AttackRange = 1.5f;
    public float ChaseRange = 5f;
    public Transform Player; // 玩家目标

    private FSM<EnemyStateId, EnemyController> mFSM;

    void Start()
    {
        mFSM = new FSM<EnemyStateId, EnemyController>(this);

        // 添加状态
        mFSM.AddState(EnemyStateId.Patrol, new PatrolState());
        mFSM.AddState(EnemyStateId.Chase, new ChaseState());
        mFSM.AddState(EnemyStateId.Attack, new AttackState());

        // 初始状态为巡逻
        mFSM.ChangeState(EnemyStateId.Patrol);
    }

    void Update()
    {
        mFSM.OnUpdate();
    }
    
    // 辅助方法：获取到玩家的距离
    public float GetDistanceToPlayer()
    {
        if (Player == null) return float.MaxValue;
        return Vector3.Distance(transform.position, Player.position);
    }
}

// 3. 实现巡逻状态
public class PatrolState : AbstractState<EnemyStateId, EnemyController>
{
    private int mCurrentPointIndex = 0;

    public override void OnEnter()
    {
        Debug.Log("开始巡逻");
    }

    public override void OnUpdate()
    {
        // 检查是否发现玩家
        if (mTarget.GetDistanceToPlayer() < mTarget.ChaseRange)
        {
            ChangeState(EnemyStateId.Chase);
            return;
        }

        // 巡逻逻辑
        if (mTarget.PatrolPoints.Length == 0) return;

        Transform targetPoint = mTarget.PatrolPoints[mCurrentPointIndex];
        mTarget.transform.position = Vector3.MoveTowards(
            mTarget.transform.position, 
            targetPoint.position, 
            mTarget.MoveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(mTarget.transform.position, targetPoint.position) < 0.1f)
        {
            mCurrentPointIndex = (mCurrentPointIndex + 1) % mTarget.PatrolPoints.Length;
        }
    }
}

// 4. 实现追逐状态
public class ChaseState : AbstractState<EnemyStateId, EnemyController>
{
    public override void OnEnter()
    {
        Debug.Log("发现玩家，开始追逐！");
    }

    public override void OnUpdate()
    {
        float distance = mTarget.GetDistanceToPlayer();

        // 如果玩家跑远了，回去巡逻
        if (distance > mTarget.ChaseRange * 1.5f) // 增加一点缓冲距离防止频繁切换
        {
            ChangeState(EnemyStateId.Patrol);
            return;
        }

        // 如果进入攻击范围，开始攻击
        if (distance < mTarget.AttackRange)
        {
            ChangeState(EnemyStateId.Attack);
            return;
        }

        // 追逐逻辑
        if (mTarget.Player != null)
        {
            mTarget.transform.position = Vector3.MoveTowards(
                mTarget.transform.position,
                mTarget.Player.position,
                mTarget.MoveSpeed * Time.deltaTime
            );
        }
    }
}

// 5. 实现攻击状态
public class AttackState : AbstractState<EnemyStateId, EnemyController>
{
    private float mAttackTimer;
    private float mAttackCooldown = 1.0f;

    public override void OnEnter()
    {
        Debug.Log("进入攻击状态");
        mAttackTimer = 0; // 立即攻击一次
    }

    public override void OnUpdate()
    {
        // 如果玩家离开攻击范围，转回追逐
        if (mTarget.GetDistanceToPlayer() > mTarget.AttackRange)
        {
            ChangeState(EnemyStateId.Chase);
            return;
        }

        // 攻击逻辑
        mAttackTimer -= Time.deltaTime;
        if (mAttackTimer <= 0)
        {
            PerformAttack();
            mAttackTimer = mAttackCooldown;
        }
    }

    private void PerformAttack()
    {
        Debug.Log($"攻击玩家！上一次状态是: {mFSM.PreviousStateId}");
        // 这里可以添加造成伤害的逻辑
    }
}
```

## 高级用法：分层状态机 (HFSM)

由于 `FSM` 类继承自 `AbstractState`，你可以将一个状态机作为另一个状态机的状态，从而实现分层结构。

例如，你可以有一个 `GroundedState`（在地面上），它内部包含一个子状态机来管理 `Idle` 和 `Run` 状态。

```csharp
public class GroundedState : FSM<PlayerStateId, PlayerController>
{
    public GroundedState(PlayerController target) : base(target)
    {
        // 在构造函数中添加子状态
        AddState(PlayerStateId.Idle, new IdleState());
        AddState(PlayerStateId.Run, new RunState());
    }

    public override void OnEnter()
    {
        base.OnEnter();
        // 进入 GroundedState 时，默认进入子状态机的 Idle 状态
        ChangeState(PlayerStateId.Idle);
    }
    
    // OnUpdate, OnFixedUpdate, OnExit 会自动由基类 FSM 处理并传递给当前子状态
}
```

然后在主状态机中使用：

```csharp
// 主状态机
mMainFSM = new FSM<MainStateId, PlayerController>(this);
mMainFSM.AddState(MainStateId.Grounded, new GroundedState(this));
mMainFSM.AddState(MainStateId.Airborne, new AirborneState(this));
```

## API 参考

### `FSM<TId, TTarget>`

- `FSM(TTarget target, int capacity = 16)`: 构造函数。
- `void AddState(TId id, AbstractState<TId, TTarget> state)`: 添加状态。
- `void RemoveState(TId id)`: 移除状态。
- `void ChangeState(TId id)`: 切换到指定状态。
- `AbstractState<TId, TTarget> GetState(TId id)`: 获取指定 ID 的状态实例。
- `bool HasState(TId id)`: 检查是否存在指定 ID 的状态。
- `void Clear()`: 退出当前状态并清除所有状态。
- `TId CurrentStateId`: 获取当前状态 ID。
- `AbstractState<TId, TTarget> CurrentState`: 获取当前状态实例。
- `TId PreviousStateId`: 获取上一个状态 ID。
- `AbstractState<TId, TTarget> PreviousState`: 获取上一个状态实例。

### `AbstractState<TId, TTarget>`

- `void Init(FSM<TId, TTarget> fsm, TTarget target)`: 初始化状态（由 FSM 自动调用）。
- `virtual void OnInit()`: 初始化回调，子类可重写以进行一次性初始化。
- `virtual void OnEnter()`: 进入状态时调用。
- `virtual void OnUpdate()`: 每帧调用。
- `virtual void OnFixedUpdate()`: 固定时间步调用。
- `virtual void OnExit()`: 退出状态时调用。
- `void ChangeState(TId id)`: 切换状态的快捷方法。
