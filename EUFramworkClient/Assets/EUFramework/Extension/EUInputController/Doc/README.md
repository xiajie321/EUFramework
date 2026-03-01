# EUInputController 输入控制系统

## 简介
EUInputController 是一个基于 Unity Input System 的输入管理扩展模块，旨在简化多玩家输入控制、设备自动绑定以及运行时设备热插拔的处理流程。它提供了统一的接口来管理 `PlayerInputController`，支持手柄与键盘的自动映射，并提供强类型的事件系统。

## 功能特性
*   **多玩家支持**：默认支持最多 4 个玩家控制器，可动态扩展，每个控制器拥有独立的 Input Action 状态。
*   **智能设备绑定**：
    *   自动识别连接的手柄 (Gamepad) 并分配给空闲的玩家控制器。
    *   无手柄连接时，自动回退使用键盘/鼠标作为默认输入设备。
*   **热插拔支持**：实时监听设备连接与断开事件，自动处理控制器的重新绑定与释放，无需手动编写复杂的设备管理逻辑。
*   **事件驱动架构**：基于 Input System 的 Action 生成强类型事件接口，避免使用字符串名称获取 Action，减少运行时错误。
*   **代码生成**：配合 Editor 扩展自动生成 Input Action 的 C# 包装类，确保代码与配置始终同步（详见 Editor 目录下文档）。
*   **键位配置扩展**：提供便捷的扩展方法用于保存和加载用户自定义的键位设置。

## 目录结构
```text
EUInputController/
├── Doc/                    # 文档目录
│   ├── API.md              # API 参考文档
│   ├── README.md           # 说明文档
│   └── Update.md           # 更新日志
├── Editor/                 # 编辑器扩展
│   ├── Templates/          # 代码生成模板 (Scriban)
│   └── InputControllerCodeGenerator.cs # 代码生成器逻辑
├── Example/                # 示例场景与脚本
├── Script/                 # 核心脚本
│   ├── InputSystem/        # Input System 配置文件与生成类
│   ├── EUInputController.cs # 核心管理类
│   ├── EUInputControllerExtension.cs # 扩展方法
│   ├── PlayerInputController.cs # 玩家控制器封装
│   ├── PlayerInputControllerEvent.cs # 玩家输入事件封装
│   └── UIInputControllerEvent.cs # UI 输入事件封装
└── extension.json          # 扩展描述文件
```

## 快速开始

### 1. 初始化
系统通过 `[RuntimeInitializeOnLoadMethod]` 特性在游戏启动时自动初始化，无需在场景中挂载任何 MonoBehaviour。默认会自动创建一个 ID 为 0 的主玩家控制器。

### 2. 获取控制器
```csharp
using EUFramework.Extension.EUInputControllerKit;

// 获取主玩家控制器（通常是 ID 0，对应主手柄或键盘）
var mainController = EUInputController.GetMainPlayerInputController();

// 获取指定 ID 的控制器（例如 P2）
var p2Controller = EUInputController.GetPlayerInputController(1);
```

### 3. 监听输入事件
模块自动为 Input Action 生成了事件包装类，可以直接通过 Lambda 表达式或方法添加监听。

**监听玩家操作 (Player Map)：**
```csharp
// 监听 Move 事件 (Vector2)
mainController.PlayerInputControllerEvent.AddMoveListener(OnMove);

// 监听 Jump 事件 (Button)
mainController.PlayerInputControllerEvent.AddJumpListener(context => {
    if (context.performed) {
        Debug.Log("Jump!");
    }
});

private void OnMove(InputAction.CallbackContext context)
{
    var value = context.ReadValue<Vector2>();
    // 处理移动逻辑
}
```

**监听 UI 操作 (UI Map)：**
```csharp
// 监听 Submit (确认) 事件
mainController.UIInputControllerEvent.AddSubmitListener(context => {
    if (context.performed) {
        Debug.Log("UI Submit");
    }
});
```

### 4. 处理设备变更
可以监听控制器或设备的变更事件，用于显示“手柄已断开”提示或更新 UI 图标。

```csharp
// 监听主控制器切换（例如从键盘切换到手柄）
EUInputController.AddMainPlayerInputControllerChangeListener(data => {
    Debug.Log($"主控制器变更: {data.LastPlayerInputController} -> {data.CurrentPlayerInputController}");
});

// 监听控制器设备绑定变更（例如 P1 的手柄断开，自动切换回空）
EUInputController.AddPlayerInputControllerOfDeviceChangeListener(data => {
    Debug.Log($"控制器 {data.ChangeOfPlayerInputController} 设备变更: {data.LastGamepad?.displayName} -> {data.CurrentGamepad?.displayName}");
});
```

### 5. 保存与加载键位
使用扩展方法可以方便地处理用户自定义键位。

```csharp
// 保存键位配置为 JSON 字符串
string bindingJson = mainController.GetBindingsJson();
PlayerPrefs.SetString("KeyBindings", bindingJson);

// 加载键位配置
if (PlayerPrefs.HasKey("KeyBindings")) {
    string savedJson = PlayerPrefs.GetString("KeyBindings");
    mainController.SetBindings(savedJson, removeExisting: true);
}
```

## 依赖项
*   com.unity.inputsystem
