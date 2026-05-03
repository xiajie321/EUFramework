# Sbn 模板

EUUI 使用 Scriban 的 `.sbn` 文件生成 C# 代码。用户可以直接编辑 `Templates/Sbn` 下的 `.sbn` 文件，也可以在 `Static/` 下新增模板。

## 目录约定

| 目录 | 用途 | 导出方式 |
|---|---|---|
| `Static/` | 静态扩展模板，例如扩展 `EUUIKit`、`EUUIPanelBase` 或生成公共辅助类 | 在「生成绑定模板」中启用并导出 |
| `WithData/` | 面板绑定代码、MVC 接入等需要面板数据的模板 | 由面板自动绑定导出流程使用 |

用户扩展通常放在 `Static/`。`WithData/` 属于框架生成流程，不建议作为普通扩展入口。

## 命名约定

| 扩展类型 | 文件名示例 | 生成目标 |
|---|---|---|
| UIKit 扩展 | `EUUIKit.MyLoader.sbn` | `EUUIKit.MyLoader.Generated.cs` |
| PanelBase 扩展 | `EUUIPanelBase.MySprite.sbn` | `EUUIPanelBase.MySprite.Generated.cs` |

文件名建议使用 `目标类.扩展名.sbn`，扩展名只使用字母、数字和下划线，并以字母开头。

## 模板变量

导出 `Static/` 模板时，导出器会注入基础变量 `extension_name`（从文件名提取扩展名部分），并读取同名 sidecar JSON 的 `namespaceVariables`，把对应程序集的 `rootNamespace` 注入模板。例如：

```sbn
using {{ eu_res_namespace }};
```

配合同名 `EUUIKit.EURes.json`：

```json
{
    "requiredAssemblies": ["EURes", "YooAsset"],
    "namespaceVariables": [
        { "key": "eu_res_namespace", "value": "EURes" }
    ]
}
```

导出时会读取 `EURes.asmdef` 的 `rootNamespace`，填入 `eu_res_namespace`。如果找不到 `rootNamespace`，会回退为 `value` 填写的程序集名。

## Sidecar JSON

每个 `.sbn` 可以有一个同名 `.json`，用于声明程序集引用和命名空间变量。该 JSON 由 Unity `JsonUtility` 读取，格式必须是固定字段和数组结构。

```json
{
    "requiredAssemblies": ["RuntimeAssembly"],
    "editorAssemblies": ["EditorAssembly"],
    "namespaceVariables": [
        { "key": "runtime_namespace", "value": "RuntimeAssembly" }
    ]
}
```

字段说明：

| 字段 | 说明 |
|---|---|
| `requiredAssemblies` | 生成代码运行时需要写入 `EUUI.asmdef` 的程序集引用 |
| `editorAssemblies` | 生成代码或工具需要写入 `EUUI.Editor.asmdef` 的编辑器程序集引用 |
| `namespaceVariables` | 模板变量名到程序集名的映射，用于动态注入目标程序集的 `rootNamespace` |

不要把 `namespaceVariables` 写成对象字典，例如 `{ "xxx": "yyy" }`。`JsonUtility` 不支持这种字典结构，应使用 `{ "key": "...", "value": "..." }` 数组。

## 拓展流程

1. 直接在 `Static/` 新建或编辑 `.sbn`。
2. 如需额外程序集或动态命名空间，在 `.sbn` 旁创建同名 `.json`。
3. 在「模板管理」确认模板能被扫描到。
4. 在「生成绑定模板」勾选需要导出的模板，点击生成。
5. 检查输出目录下的 `.Generated.cs`，确认编译通过。

## 编写建议

- `EUUIKit.*.sbn` 适合实现 `EUUIKit` 的核心加载入口，例如资源加载器注册和 `OnPanelClosed(string panelName)`。
- `EUUIPanelBase.*.sbn` 适合给面板基类补充通用能力，例如 `SetImage`、`LoadSprite`、动画或第三方控件适配。
- 模板中只放通用框架能力，不建议写具体业务面板逻辑。
- 需要用户项目自行实现的地方保留清晰的 TODO，避免生成不可用但难以定位的代码。
