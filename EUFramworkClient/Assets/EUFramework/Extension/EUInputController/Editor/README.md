# EUInputController 代码生成器

## 概述

本工具基于 Scriban 模板引擎，自动读取 `InputController.inputactions` 文件并生成对应的 C# 代码，确保运行时代码与 InputSystem 配置保持同步。

## 生成的文件

| 文件 | 说明 |
|------|------|
| `Script/PlayerInputController.cs` | 玩家输入控制器，封装 InputController 并管理各 ActionMap 的事件回调 |
| `Script/PlayerInputControllerEvent.cs` | Player ActionMap 的事件监听包装类和回调实现类 |
| `Script/UIInputControllerEvent.cs` | UI ActionMap 的事件监听包装类和回调实现类 |

## 使用方式

### 自动生成
当 `InputController.inputactions` 文件被修改并重新导入时，代码生成器会自动检测变化并触发重新生成。

### 手动生成
通过 Unity 菜单 `Tools > EUInputController > 生成输入控制器代码` 手动触发。

## 模板文件

模板位于 `Editor/Templates/` 目录下：

- `PlayerInputController.scriban` — 控制器主体模板
- `InputControllerEvent.scriban` — 事件类通用模板（用于每个 ActionMap）

## 路径说明

所有路径均基于生成器脚本自身位置自动推算，无需配置绝对路径。文件夹可放在项目的任意位置均可正常工作。

## 注意事项

- **请勿手动修改生成的 `.cs` 文件**，它们会在下次生成时被覆盖。
- 如需自定义生成逻辑，请修改 `Editor/Templates/` 下的 `.scriban` 模板文件。
- 需要项目中已安装 Scriban NuGet 包。
