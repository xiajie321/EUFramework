# EUInputController API 参考文档

本文档详细列出了 `EUInputController` 模块中各类、属性及方法的说明。所有类均位于 `EUFramework.Extension.EUInputControllerKit` 命名空间下。

## 1. EUInputController (静态类)

核心管理类，负责全局的控制器管理、设备映射与热插拔逻辑。

### 属性

| 属性名 | 类型 | 读写 | 说明 |
| :--- | :--- | :--- | :--- |
| `MaxPlayerInputControllers` | `int` | get/set | 设置或获取系统支持的最大玩家控制器数量。默认为 4，最少为 1。设置较小值时会自动移除多余的控制器。 |
| `CurrentPlayerInputControllerCount` | `int` | get | 获取当前已创建并活跃的玩家控制器数量。 |
| `CurrentPlayerInputDeviceCount` | `int` | get | 获取当前已连接的输入设备（Gamepad）数量。 |
| `PlayerInputControllerMapId` | `Dictionary<PlayerInputController, int>` | internal | 控制器到 ID 的映射字典。 |
| `PlayerInputDeviceMap` | `Dictionary<int, InputDevice>` | internal | 设备 ID 到设备对象的映射字典。 |

### 控制器管理方法

| 方法签名 | 说明 |
| :--- | :--- |
| `int AddPlayerInputController()` | 创建一个新的玩家控制器，并返回其生成的唯一 ID。如果已达到最大数量限制，可能无法创建。 |
| `void RemovePlayerInputController(int playerId)` | 根据 ID 移除指定的玩家控制器。如果该控制器为主控制器，则不会被移除。 |
| `void RemovePlayerInputController(PlayerInputController playerInputController)` | 移除指定的玩家控制器实例。 |
| `PlayerInputController GetMainPlayerInputController()` | 获取当前的主玩家控制器（通常对应 ID 0，或者当前活跃的第一个控制器）。 |
| `void SetMainPlayerInputController(PlayerInputController playerInputController)` | 将指定的控制器实例设置为主控制器，并触发变更事件。 |
| `void SetMainPlayerInputController(int playerId)` | 将指定 ID 的控制器设置为主控制器。 |
| `PlayerInputController GetPlayerInputController(int playerId)` | 根据 ID 获取玩家控制器实例。 |
| `PlayerInputController[] GetPlayerInputControllerList()` | 获取所有玩家控制器的数组副本。注意：此方法会产生少量 GC，建议避免在 Update 中高频调用。 |
| `PlayerInputController[] GetIdlePlayerInputControllerList()` | 获取当前未绑定任何设备的空闲玩家控制器列表。 |

### 设备管理方法

| 方法签名 | 说明 |
| :--- | :--- |
| `void SetPlayerInputControllerOfDevice(PlayerInputController playerInputController, InputDevice inputDevice)` | 将指定设备绑定到指定控制器。如果设备已被其他控制器绑定，会先解除旧绑定。 |
| `void SetPlayerInputControllerOfDevice(int playerId, InputDevice inputDevice)` | 将指定设备绑定到指定 ID 的控制器。 |
| `PlayerInputController GetPlayerInputDeviceOfPlayerInputController(int deviceId)` | 获取指定设备 ID 当前所绑定的玩家控制器。如果没有绑定，返回 null。 |
| `PlayerInputController GetPlayerInputDeviceOfPlayerInputController(InputDevice inputDevice)` | 获取指定设备对象当前所绑定的玩家控制器。 |
| `int GetPlayerInputDeviceCount()` | 获取当前连接的设备总数。 |
| `InputDevice[] GetPlayerInputDeviceList()` | 获取所有已连接设备的数组副本。 |
| `InputDevice[] GetIdlePlayerInputDeviceList()` | 获取当前未被任何控制器绑定的空闲设备列表。 |
| `Dictionary<int, InputDevice> GetPlayerInputDeviceDictionary()` | 获取设备字典的副本。 |

### 事件监听方法

| 方法签名 | 说明 |
| :--- | :--- |
| `void AddMainPlayerInputControllerChangeListener(Action<EUMainInputControllerChangeData> action)` | 添加主控制器变更事件监听。 |
| `void RemoveMainPlayerInputControllerChangeListener(Action<EUMainInputControllerChangeData> action)` | 移除主控制器变更事件监听。 |
| `void RemoveAllMainPlayerInputControllerChangeListener()` | 移除所有主控制器变更事件监听。 |
| `void AddPlayerInputControllerOfDeviceChangeListener(Action<EUPlayerInputOfDeviceChangeData> action)` | 添加控制器设备绑定变更事件监听。 |
| `void RemovePlayerInputControllerOfDeviceChangeListener(Action<EUPlayerInputOfDeviceChangeData> action)` | 移除控制器设备绑定变更事件监听。 |
| `void RemoveAllPlayerInputControllerOfDeviceChangeListener()` | 移除所有控制器设备绑定变更事件监听。 |
| `void AddPlayerInputDeviceAddedListener(Action<InputDevice> action)` | 添加设备接入事件监听。 |
| `void RemovePlayerInputDeviceAddedListener(Action<InputDevice> action)` | 移除设备接入事件监听。 |
| `void RemoveAllPlayerInputDeviceAddedListener()` | 移除所有设备接入事件监听。 |
| `void AddPlayerInputDeviceRemovedListener(Action<InputDevice> action)` | 添加设备移除事件监听。 |
| `void RemovePlayerInputDeviceRemovedListener(Action<InputDevice> action)` | 移除设备移除事件监听。 |
| `void RemoveAllPlayerInputDeviceRemovedListener()` | 移除所有设备移除事件监听。 |

---

## 2. PlayerInputController

玩家控制器实例，是对 Unity `InputController` 的高级封装，管理单个玩家的输入状态和事件。

### 属性

| 属性名 | 类型 | 说明 |
| :--- | :--- | :--- |
| `Gamepad` | `Gamepad` | 获取当前绑定的手柄设备。如果为 `null`，表示当前使用键盘/鼠标作为输入源。 |
| `Controller` | `InputController` | 获取底层的 Input System 控制器实例（自动生成的类）。 |
| `PlayerInputControllerEvent` | `PlayerInputEvent` | 获取 Player Action Map (玩家操作) 的事件代理对象。 |
| `UIInputControllerEvent` | `UIInputEvent` | 获取 UI Action Map (界面操作) 的事件代理对象。 |

---

## 3. PlayerInputEvent

封装了 Player Action Map 中所有 Action 的事件订阅接口。

### 方法

所有方法均成对出现（Add/Remove/RemoveAll），以下以 `Move` 动作为例：

*   `void AddMoveListener(Action<InputAction.CallbackContext> action)`
*   `void RemoveMoveListener(Action<InputAction.CallbackContext> action)`
*   `void RemoveAllMoveListener()`

**支持的 Action 事件：**
*   `Move` (移动)
*   `Jump` (跳跃)
*   `Interaction` (交互)
*   `Raise` (举起)
*   `PickUp` (拾取)
*   `PushPull` (推拉)
*   `Discard` (丢弃)
*   `Disassemble` (分解)

*(注：具体 Action 取决于 Input Actions 配置文件，此处列出的是当前版本的默认配置)*

---

## 4. UIInputEvent

封装了 UI Action Map 中所有 Action 的事件订阅接口。

### 方法

所有方法均成对出现（Add/Remove/RemoveAll），以下以 `Submit` 动作为例：

*   `void AddSubmitListener(Action<InputAction.CallbackContext> action)`
*   `void RemoveSubmitListener(Action<InputAction.CallbackContext> action)`
*   `void RemoveAllSubmitListener()`

**支持的 Action 事件：**
*   `Navigate` (导航)
*   `Submit` (确认)
*   `Cancel` (取消)
*   `Point` (指针位置)
*   `Click` (点击)
*   `ScrollWheel` (滚轮)
*   `MiddleClick` (中键)
*   `RightClick` (右键)
*   `TrackedDevicePosition` (VR/AR 设备位置)
*   `TrackedDeviceOrientation` (VR/AR 设备旋转)

---

## 5. EUInputControllerExtension (扩展方法)

为 `PlayerInputController` 和 `InputDevice` 提供的便捷扩展方法。

### 方法

| 方法签名 | 扩展类型 | 说明 |
| :--- | :--- | :--- |
| `void SetPlayerInputControllerOfDevice(this PlayerInputController, InputDevice)` | `PlayerInputController` | 设置控制器的输入设备（快捷调用 `EUInputController` 静态方法）。 |
| `int GetPlayerInputControllerId(this PlayerInputController)` | `PlayerInputController` | 获取控制器的 ID。 |
| `bool Exists(this PlayerInputController)` | `PlayerInputController` | 判断该控制器是否在管理系统中存在。 |
| `bool Exists(this InputDevice)` | `InputDevice` | 判断该设备是否在管理系统中存在。 |
| `PlayerInputController GetPlayerInputController(this InputDevice)` | `InputDevice` | 获取该设备绑定的控制器。 |
| `string GetBindingsJson(this PlayerInputController)` | `PlayerInputController` | 获取当前按键映射的 JSON 字符串（用于保存设置）。 |
| `void SetBindings(this PlayerInputController, string json, bool removeExisting)` | `PlayerInputController` | 从 JSON 字符串加载按键映射。 |

---

## 6. 数据结构

### EUMainInputControllerChangeData
主控制器变更事件的数据结构。
*   `LastPlayerInputController`: 变更前的主控制器。
*   `CurrentPlayerInputController`: 变更后的主控制器。

### EUPlayerInputOfDeviceChangeData
控制器设备变更事件的数据结构。
*   `ChangeOfPlayerInputController`: 发生变更的控制器。
*   `LastGamepad`: 变更前绑定的手柄。
*   `CurrentGamepad`: 变更后绑定的手柄。
