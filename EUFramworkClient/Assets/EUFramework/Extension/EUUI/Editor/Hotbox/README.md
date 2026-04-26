# Hotbox

EUUI Hotbox 快捷操作功能，在 Scene 视图按下 Space 弹出快捷面板，再次按下 Space 关闭，提供常用 UI 操作的快速入口。

| 文件 | 说明 |
|---|---|
| `EUUIHotboxPopup.cs` | Hotbox 弹出层 UI，负责绘制快捷操作按钮（创建面板、绑定节点、刷新注册表等），并处理用户点击事件 |
| `EUHotboxEntryScanner.cs` | Hotbox 入口扫描器，通过反射或 Attribute 收集所有注册了 `[EUHotboxEntry]` 的操作项，动态构建 Hotbox 菜单列表 |

## 配置资产

Hotbox 的布局配置保存在 `EditorSO/Workspace/EUHotboxConfig.asset`。该资产记录区域和条目 ID，只保存“哪些条目放在哪个区域”，不保存具体执行逻辑。

条目来源由扫描器自动发现：

- 静态无参方法：标记 `[EUHotboxEntry]`。
- 独立 Action 类：实现 `IEUHotboxAction`，并提供无参构造函数。

新增或删除条目后，可以在 EUUI 配置工具的功能编排面板中刷新扫描结果，再把条目拖入目标区域。

## 方式一：静态方法

适合给已有工具方法增加 Hotbox 入口。方法必须是静态、无参、非泛型，返回值通常使用 `void`。

```csharp
using EUFramework.Extension.EUUI;
using UnityEditor;

public static class MyEUUIHotboxEntries
{
    [EUHotboxEntry("打开 UI 配置", "EUUI", "打开 EUUI 配置面板")]
    private static void OpenEUUIConfig()
    {
        EditorApplication.ExecuteMenuItem("EUFramework/拓展/EUUI 配置工具");
    }
}
```

`EUHotboxEntryAttribute` 参数说明：

| 参数 | 说明 |
|---|---|
| `label` | 在功能编排面板和 Hotbox 弹窗中显示的名称 |
| `group` | 条目分组，未填写时使用 `通用` |
| `tooltip` | 鼠标悬停提示，可为空 |

扫描器会用 `TypeFullName::MethodName` 作为条目 ID。移动类或改方法名会改变 ID，已配置到 `EUHotboxConfig.asset` 的旧条目需要重新配置。

## 方式二：实现接口

适合无法给第三方静态方法加 Attribute，或希望把复杂逻辑封装成一个类的场景。

```csharp
using EUFramework.Extension.EUUI;
using UnityEngine;

public sealed class RefreshMyUIToolsAction : IEUHotboxAction
{
    public string Label => "刷新 UI 工具";
    public string Group => "EUUI";
    public string Tooltip => "重新扫描并刷新自定义 UI 工具缓存";

    public void Execute()
    {
        Debug.Log("Refresh custom UI tools.");
    }
}
```

接口条目的要求：

- 类型不能是抽象类或接口。
- 类型必须有无参构造函数。
- `Execute()` 中执行真正的操作。
- 扫描器会用类型完整名作为条目 ID。移动命名空间或改类名会改变 ID。

## 使用建议

- 编辑器专用操作建议放在 Editor 程序集中，避免运行时程序集引用 `UnityEditor`。
- 条目逻辑应尽量短，只负责打开窗口、调用已有工具入口或触发一次明确操作。
- 如果操作依赖当前选中对象，请在 `Execute()` 中自行判断 `Selection.activeObject` 或场景上下文，并给出清晰日志。
- 改名、移动命名空间或删除条目后，需要回到功能编排面板刷新并检查 `EUHotboxConfig.asset` 中的区域配置。
