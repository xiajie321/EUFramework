# EU FSM API 文档

## EUFSM<TKey> 类

`EUFramwork.Extension.FSM.EUFSM<TKey>`

有限状态机核心类。`TKey` 必须为枚举类型。

### 属性

- `IState CurrentState`: 获取当前状态对象。
- `TKey CurrentId`: 获取当前状态 ID。
- `IState PreviousState`: 获取上一个状态对象。
- `TKey PreviousId`: 获取上一个状态 ID。

### 方法

- `void StartState(TKey id)`: 启动状态机并进入初始状态。
- `void ChangeState(TKey id)`: 切换到指定状态（触发旧状态 `OnExit` 和新状态 `OnEnter`）。
- `void AddState(TKey id, IState state)`: 注册状态。
- `void RemoveState(TKey id)`: 移除状态。
- `void Update()`: 轮询当前状态的 `OnUpdate`，需在 `MonoBehaviour.Update` 中调用。
- `void FixedUpdate()`: 轮询当前状态的 `OnFixedUpdate`，需在 `MonoBehaviour.FixedUpdate` 中调用。
- `void Clear()`: 清空所有状态并重置状态机。

## EUAbsStateBase<TStateId, TOwner> 类

`EUFramwork.Extension.FSM.EUAbsStateBase<TStateId, TOwner>`

推荐继承的状态基类。

### 构造函数

- `EUAbsStateBase(EUFSM<TStateId> fsm, TOwner owner)`: 初始化基类，绑定 FSM 和 Owner。

### 属性

- `TOwner Owner`: 获取状态的所有者对象（通常是 MonoBehaviour）。

### 虚拟方法 (Override)

- `virtual void OnEnter()`: 进入状态时调用。
- `virtual void OnExit()`: 退出状态时调用。
- `virtual void OnUpdate()`: 每帧调用 (由 `EUFSM.Update` 驱动)。
- `virtual void OnFixedUpdate()`: 每物理帧调用 (由 `EUFSM.FixedUpdate` 驱动)。
- `virtual void OnCondition()`: (可选) 状态跳转条件检查，建议在 `OnUpdate` 中手动调用。

## IState 接口

`EUFramwork.Extension.FSM.IState`

状态接口定义。

### 方法

- `void OnEnter()`
- `void OnExit()`
- `void OnUpdate()`
- `void OnFixedUpdate()`
- `void OnCondition()`
