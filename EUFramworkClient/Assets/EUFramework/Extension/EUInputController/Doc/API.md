# EUInputController API 参考文档

本文档提供了 `EUInputController` 模块的核心 API 详解。所有类均位于 `EUFramework.Extension.EUInputControllerKit` 命名空间下。

---

## 1. EUInputController (静态核心类)

`EUInputController` 是整个输入系统的核心管理类，负责玩家控制器的生命周期管理、设备自动绑定以及全局事件分发。

### 1.1 属性

#### `int MaxPlayerInputControllers`
*   **描述**：获取或设置系统支持的最大玩家控制器数量。
*   **默认值**：4
*   **注意**：
    *   最小值为 1。
    *   如果设置的值小于当前已存在的控制器数量，系统会自动移除多余的控制器（从列表末尾开始移除）。

#### `int CurrentPlayerInputControllerCount`
*   **描述**：获取当前已创建并活跃的玩家控制器数量。

#### `int CurrentPlayerInputDeviceCount`
*   **描述**：获取当前已连接且被系统识别的输入设备（主要是 Gamepad）数量。

### 1.2 控制器管理

#### `int AddPlayerInputController()`
*   **描述**：创建一个新的玩家控制器。
*   **返回值**：新创建控制器的 ID。如果已达到 `MaxPlayerInputControllers` 限制，则可能无法创建（具体行为取决于实现，通常会打印错误日志）。

#### `void RemovePlayerInputController(int playerId)`
*   **描述**：根据 ID 移除指定的玩家控制器。
*   **参数**：
    *   `playerId`：要移除的控制器 ID。
*   **注意**：如果该控制器是当前的主控制器（Main Player），则不会被移除。

#### `PlayerInputController GetMainPlayerInputController()`
*   **描述**：获取当前的主玩家控制器。
*   **用途**：通常用于单人游戏或多人游戏中的 UI 控制权归属。默认情况下，ID 为 0 的控制器是主控制器。

#### `void SetMainPlayerInputController(int playerId)`
*   **描述**：将指定 ID 的控制器设置为主控制器。
*   **触发事件**：`OnMainInputControllerChange`

#### `PlayerInputController GetPlayerInputController(int playerId)`
*   **描述**：根据 ID 获取玩家控制器实例。
*   **返回值**：如果 ID 存在则返回对应实例，否则返回 `null`。

### 1.3 设备管理

#### `void SetPlayerInputControllerOfDevice(PlayerInputController controller, InputDevice device)`
*   **描述**：将指定输入设备绑定到目标控制器。
*   **参数**：
    *   `controller`：目标玩家控制器。
    *   `device`：要绑定的输入设备（通常是 `Gamepad`）。如果传入 `null`，则该控制器将回退到键盘/鼠标模式。
*   **逻辑**：
    *   如果该设备已经被其他控制器绑定，系统会先解除旧的绑定关系。
    *   绑定成功后会触发 `OnPlayerInputControllerOfDeviceChange` 事件。

#### `PlayerInputController GetPlayerInputDeviceOfPlayerInputController(InputDevice device)`
*   **描述**：查找指定设备当前绑定的玩家控制器。
*   **返回值**：如果该设备未绑定任何控制器，返回 `null`。

#### `InputDevice[] GetIdlePlayerInputDeviceList()`
*   **描述**：获取当前所有未被绑定的空闲设备列表。
*   **用途**：可用于在多人游戏加入界面中，自动为新加入的玩家分配空闲手柄。

### 1.4 全局事件监听

以下方法用于添加或移除全局事件监听。建议在 `OnEnable` 中添加，在 `OnDisable` 中移除。

| 添加监听方法 | 移除监听方法 | 描述 |
| :--- | :--- | :--- |
| `AddMainPlayerInputControllerChangeListener` | `RemoveMainPlayerInputControllerChangeListener` | 当主控制器发生切换时触发。 |
| `AddPlayerInputControllerOfDeviceChangeListener` | `RemovePlayerInputControllerOfDeviceChangeListener` | 当任意控制器的设备绑定发生变化时触发（如手柄断开/连接）。 |
| `AddPlayerInputDeviceAddedListener` | `RemovePlayerInputDeviceAddedListener` | 当有新设备（手柄）连接到系统时触发。 |
| `AddPlayerInputDeviceRemovedListener` | `RemovePlayerInputDeviceRemovedListener` | 当有设备（手柄）从系统断开时触发。 |

---

## 2. PlayerInputController (玩家控制器类)

`PlayerInputController` 是对 Unity `InputController` 的封装，代表一个具体的玩家输入实体。

### 2.1 属性

#### `Gamepad Gamepad`
*   **描述**：获取当前绑定的手柄设备。
*   **返回值**：
    *   `Gamepad` 对象：表示当前绑定了具体的手柄。
    *   `null`：表示当前使用 **键盘和鼠标** 作为输入源。

#### `InputController Controller`
*   **描述**：获取底层的 Unity Input System 生成类实例。通常不需要直接访问，除非需要进行非常底层的操作。

#### `PlayerInputEvent PlayerInputControllerEvent`
*   **描述**：获取 **玩家操作**（如移动、跳跃、攻击）的事件代理对象。

#### `UIInputEvent UIInputControllerEvent`
*   **描述**：获取 **UI 操作**（如导航、确认、取消）的事件代理对象。

---

## 3. PlayerInputEvent (事件代理类)

该类封装了具体的 Input Action 事件，提供了强类型的订阅接口。

### 3.1 使用示例

```csharp
// 获取控制器
var controller = EUInputController.GetMainPlayerInputController();

// 1. 监听移动 (Vector2)
controller.PlayerInputControllerEvent.AddMoveListener(OnMove);

// 2. 监听跳跃 (Button - 按下)
controller.PlayerInputControllerEvent.AddJumpListener(context => {
    if (context.performed) {
        Debug.Log("跳跃！");
    }
});

// 回调函数
private void OnMove(InputAction.CallbackContext context)
{
    Vector2 input = context.ReadValue<Vector2>();
    // ...
}
```

### 3.2 可用事件列表

*   `AddMoveListener` / `RemoveMoveListener`：移动 (Vector2)
*   `AddJumpListener` / `RemoveJumpListener`：跳跃 (Button)
*   `AddInteractionListener` / `RemoveInteractionListener`：交互 (Button)
*   `AddRaiseListener` / `RemoveRaiseListener`：举起 (Button)
*   `AddPickUpListener` / `RemovePickUpListener`：拾取 (Button)
*   `AddPushPullListener` / `RemovePushPullListener`：推拉 (Button)
*   `AddDiscardListener` / `RemoveDiscardListener`：丢弃 (Button)
*   `AddDisassembleListener` / `RemoveDisassembleListener`：分解 (Button)

*(注：具体事件取决于 Input Actions 配置文件，以上为默认配置)*

---

## 4. EUInputControllerExtension (扩展方法)

提供了针对 `PlayerInputController` 的便捷扩展功能。

### 4.1 键位配置 (Rebinding)

#### `string GetBindingsJson(this PlayerInputController controller)`
*   **描述**：获取当前控制器所有按键映射的 JSON 字符串。
*   **用途**：用于将用户自定义的键位保存到本地（如 `PlayerPrefs` 或文件）。

#### `void SetBindings(this PlayerInputController controller, string json, bool removeExisting)`
*   **描述**：从 JSON 字符串加载按键映射。
*   **参数**：
    *   `json`：由 `GetBindingsJson` 生成的 JSON 字符串。
    *   `removeExisting`：是否先清除当前的自定义映射。通常设为 `true`。

### 4.2 其他扩展

#### `bool Exists(this PlayerInputController controller)`
*   **描述**：判断该控制器实例是否仍然在 `EUInputController` 的管理列表中。

#### `int GetPlayerInputControllerId(this PlayerInputController controller)`
*   **描述**：获取该控制器的 ID。

---

## 5. 数据结构

### `EUMainInputControllerChangeData`
用于 `OnMainInputControllerChange` 事件。
```csharp
public struct EUMainInputControllerChangeData
{
    public PlayerInputController LastPlayerInputController;    // 变更前的主控制器
    public PlayerInputController CurrentPlayerInputController; // 变更后的主控制器
}
```

### `EUPlayerInputOfDeviceChangeData`
用于 `OnPlayerInputControllerOfDeviceChange` 事件。
```csharp
public struct EUPlayerInputOfDeviceChangeData
{
    public PlayerInputController ChangeOfPlayerInputController; // 发生变更的控制器
    public Gamepad LastGamepad;    // 变更前绑定的手柄 (可能为 null)
    public Gamepad CurrentGamepad; // 变更后绑定的手柄 (可能为 null)
}
