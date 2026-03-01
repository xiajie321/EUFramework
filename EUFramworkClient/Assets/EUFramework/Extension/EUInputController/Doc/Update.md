# 更新日志

## [1.0.0] - 2026-03-02

### 新增
- **核心功能**
    - 实现 `EUInputController` 静态管理类，支持多玩家输入控制器的管理。
    - 实现 `PlayerInputController` 类，封装 Unity Input System 的 `InputController`。
    - 实现自动设备绑定逻辑：支持手柄自动分配给空闲控制器，无手柄时回退到键盘。
    - 支持运行时设备热插拔（连接/断开）的自动处理。

- **事件系统**
    - 基于 Input Action 生成强类型事件接口 `PlayerInputEvent` 和 `UIInputEvent`。
    - 提供主控制器切换事件 `OnMainInputControllerChange`。
    - 提供控制器设备绑定变更事件 `OnPlayerInputControllerOfDeviceChange`。

- **扩展功能**
    - `EUInputControllerExtension`：提供便捷的扩展方法，如 `GetBindingsJson` (保存键位)、`SetBindings` (加载键位) 等。

- **编辑器工具**
    - 提供基于 Scriban 的代码生成器，支持根据 `.inputactions` 文件自动生成 C# 事件包装类。
