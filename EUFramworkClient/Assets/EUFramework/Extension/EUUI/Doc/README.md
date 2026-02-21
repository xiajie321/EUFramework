# EUUI UI管理器

## 概述

EUUI 是一个可扩展的 UI 管理框架，支持 MVC 架构集成、自动化代码生成、资源加载策略扩展以及灵活的面板导航管理。

> 适用版本：EUFramework EUUI Extension

## 目录

1. [快速开始](#1-快速开始)
2. [配置工具总览](#2-配置工具总览)
3. [面板导出流程](#3-面板导出流程)
4. [资源加载扩展](#4-资源加载扩展)
5. [MVC 架构集成](#5-mvc-架构集成)
6. [模板扩展](#6-模板扩展)
7. [生成文件说明](#7-生成文件说明)
8. [常见问题](#8-常见问题)
9. [附录：面板生命周期](#附录面板生命周期)

---

## 1. 快速开始

### 打开配置工具

菜单栏 -> `EUFramework` -> `拓展` -> `EUUI 配置工具`

工具窗口分为三个标签页：

| 标签页 | 用途 |
|---|---|
| SO 配置 | 查看和修改运行时配置（EUUIKitConfig）和代码生成配置（EUUITemplateConfig） |
| 模板拓展 | 创建自定义 `.sbn` 扩展模板 |
| 生成绑定模板 | 管理和生成资源加载扩展代码 |

### 初始化 UI 系统

在游戏启动脚本中调用一次：

```csharp
EUUIKit.Initialize();
```

### 打开一个面板

```csharp
// 异步打开（不记录导航历史，适合弹窗）
var panel = await EUUIKit.OpenAsync<WndTestPanel>();

// 导航打开（记录历史，适合主流程页面）
await EUUIKit.NavigateToAsync<WndTestPanel>();

// 关闭面板
EUUIKit.Close<WndTestPanel>();
```

---

## 2. 配置工具总览

### SO 配置标签页

#### EUUIKitConfig（运行时配置）

存放于 `Resources/EUUIKitConfig.asset`，控制 UI 系统的运行时参数：

| 字段 | 说明 |
|---|---|
| referenceResolution | Canvas 参考分辨率，默认 1920x1080 |
| matchWidthOrHeight | 屏幕适配模式，0=宽适配，1=高适配，0.5=均衡 |
| builtinPrefabPath | 首包 UI Prefab 的 Addressables 路径前缀 |
| remotePrefabPath | 远程 UI Prefab 的 Addressables 路径前缀 |
| builtinAtlasPath | 首包图集路径前缀 |
| remoteAtlasPath | 远程图集路径前缀 |

#### EUUITemplateConfig（代码生成配置）

控制代码生成行为：

| 字段 | 说明 |
|---|---|
| namespace | 生成代码的命名空间（如 `Game.UI`） |
| useArchitecture | 是否生成 MVC 架构集成代码 |
| architectureName | 架构类名（如 `GameArchitecture`），留空则不生成 `GetArchitecture()` |
| architectureNamespace | 架构类所在命名空间，同时作为 `IController` 的 using 来源 |
| manualExtensions | 手动管理的扩展模板列表（生成绑定模板标签页使用） |

---

## 3. 面板导出流程

### 3.1 创建 UI 场景

1. 在 Unity 中创建一个新场景
2. 在场景根节点挂载 `EUUIPanelDescription` 组件，填写：
   - **PackageName**：UI 包名（用于组织目录结构）
   - **PanelType**：面板类型（Normal / Popup 等）
   - **Namespace**：生成代码的命名空间
3. 在场景中创建 UI 结构，将需要绑定代码的节点挂上 `EUUINodeBind` 组件

### 3.2 配置节点绑定

`EUUINodeBind` 组件字段说明：

| 字段 | 说明 |
|---|---|
| ComponentType | 要绑定的组件类型（Button、Image、Text 等） |
| MemberName | 生成的字段变量名（留空则使用 GameObject 名称） |

> 节点命名规则：只允许字母、数字、下划线，不能以数字开头，不能与其他节点重名。

### 3.3 执行导出

菜单栏 -> `EUFramework` -> `EUUI` -> `导出当前面板`

导出流程自动完成：
1. 收集场景中所有 `EUUINodeBind` 节点信息
2. 生成 `{PanelName}.Generated.cs`（字段绑定代码，每次覆盖）
3. 生成 `{PanelName}.cs`（业务逻辑代码，仅首次创建，之后不覆盖）
4. 等待编译完成，自动通过反射将字段赋值绑定到组件
5. 导出 Prefab（不含 `EUUINodeBind` 组件）

若启用了 MVC 架构集成，还会生成：
- `{PanelName}.IController.Generated.cs`（仅首次，不覆盖）

### 3.4 实现面板逻辑

在生成的 `{PanelName}.cs` 中实现以下抽象方法：

```csharp
public partial class WndTestPanel : EUUIPanelBase<WndTestPanel>
{
    public override string PackageName => "Test";
    public override string PanelName => "WndTestPanel";

    public override bool OnCanOpen() => true;

    protected override void OnOpen()
    {
        // 面板打开时调用，初始化 UI 数据
        AddClick(btnClose, () => EUUIKit.Close<WndTestPanel>());
    }

    protected override void OnShow()  { /* 面板显示时 */ }
    protected override void OnHide()  { /* 面板隐藏时 */ }
    protected override void OnClose() { /* 面板关闭时，清理资源 */ }
}
```

---

## 4. 资源加载扩展

EUUI 核心不包含资源加载逻辑，需要通过扩展模板生成。

### 4.1 生成 EURes 扩展（推荐）

在「生成绑定模板」标签页中：
1. 勾选 `EUUIKit.EURes`（Kit 层扩展，提供面板 Prefab 加载）
2. 勾选 `EUUIPanelBase.EURes`（Panel 层扩展，提供 Sprite/Prefab 加载方法）
3. 点击「生成扩展代码」

生成完成后，会自动定义编译宏 `EUUI_EXTENSIONS_GENERATED`，核心占位方法被替换为真实实现。

### 4.2 EURes 扩展提供的能力

**Kit 层（EUUIKit）**：
```csharp
// OpenAsync<T> 内部自动使用 EURes 加载 Prefab
var panel = await EUUIKit.OpenAsync<WndTestPanel>();

// 手动加载 UI Prefab
var go = await EUUIKit.LoadUIPrefabAsync("Test", "WndTestPanel", isRemote: true);

// 加载图集
var atlas = EUUIKit.LoadAtlas("TestAtlas", isRemote: true);
```

**Panel 层（EUUIPanelBase 扩展方法）**：
```csharp
// 面板内部使用（SetImage 扩展方法）
// url 格式：atlasName/spriteName
this.SetImage(imgIcon, "UIAtlas/icon_avatar");

// 加载 Sprite
Sprite sprite = this.LoadSprite("UIAtlas/icon_avatar");

// 异步加载 Prefab
var prefab = await this.LoadPrefabAsync("Items/ItemCell");
```

### 4.3 切换为其他资源方案

如需使用 Addressables 或 Resources.Load，可以：
1. 在「模板拓展」标签页创建新的 `KitExtension` 模板（选择 `StaticExtension` 预设作为起点）
2. 实现与 `EUUIKit.EURes.sbn` 相同签名的 `LoadPanelPrefabAsync<T>` 方法
3. 删除原有的 `EUUIKit.EURes.Generated.cs`
4. 生成新文件

> 同一时间只能存在一个 Kit 层资源加载实现。若两个文件同时存在，编译器会报 `CS0111` 错误（方法重复定义），删除其中一个即可。

---

## 5. MVC 架构集成

EUUI 支持可选的 MVC 架构集成，让面板自动实现 `IController` 接口。

### 5.1 配置

在「SO 配置」->「EUUITemplateConfig」中：

```
Use Architecture    [x]
Architecture Name   GameArchitecture          <- 你的架构类名
Architecture Namespace  EUFramework.Core.MVC.Interface  <- 架构类所在命名空间
```

- **Architecture Name 留空**：生成 `IController` 分部类，但不生成 `GetArchitecture()` 方法（适用于全局单一架构场景，由 `IController` 扩展自动处理）
- **Architecture Name 填写**：生成完整的 `GetArchitecture()` 实现

> `Architecture Namespace` 同时作为生成文件中 `using` 语句的来源。填写该字段后，`IController` 和 `IArchitecture` 均通过此命名空间解析。

### 5.2 生成效果

执行面板导出后，会额外生成 `WndTestPanel.IController.Generated.cs`：

```csharp
// 填写了 architectureName = "GameArchitecture" 时
using EUFramework.Core.MVC.Interface;

namespace Game.UI
{
    public partial class WndTestPanel : EUFramework.Core.MVC.Interface.IController
    {
        public EUFramework.Core.MVC.Interface.IArchitecture GetArchitecture()
            => GameArchitecture.Interface;
    }
}
```

### 5.3 在面板中使用 MVC

```csharp
protected override void OnOpen()
{
    // 获取 Model
    var playerModel = this.GetModel<IPlayerModel>();
    
    // 获取 System
    var inventorySystem = this.GetSystem<IInventorySystem>();
    
    // 发送 Command
    this.SendCommand<OpenShopCommand>();
    
    // 注册事件
    this.RegisterEvent<PlayerLevelUpEvent>(OnPlayerLevelUp)
        .UnRegisterWhenGameObjectDestroyed(gameObject);
}
```

> `GetModel`、`GetSystem`、`SendCommand` 等方法由 `IController` 扩展方法自动提供，无需手动实现。

### 5.4 注意事项

- `{PanelName}.IController.Generated.cs` **仅在首次导出时生成，之后不会覆盖**。若需重新生成，手动删除该文件后重新导出。
- 若不需要 MVC 集成，将 `useArchitecture` 设为 `false` 即可，不影响面板正常使用。

---

## 6. 模板扩展

「模板拓展」标签页用于创建自定义 `.sbn` 模板文件。

### 6.1 扩展类型

| 类型 | 目标 | 生成位置 |
|---|---|---|
| KitExtension | 扩展 `EUUIKit`（partial class） | `Static/UIKit/` |
| PanelExtension | 扩展 `EUUIPanelBase`（静态扩展类） | `Static/PanelBase/` |

### 6.2 模板预设

| 预设 | 说明 |
|---|---|
| Empty | 空模板，仅包含注释头 |
| ResourceLoader | 资源加载相关方法的代码框架 |
| StaticExtension | 通用静态扩展类示例 |

### 6.3 创建流程

1. 在「模板拓展」标签页填写扩展名称（如 `OSA`）
2. 选择扩展类型和预设
3. 点击「创建模板」，文件生成到对应目录
4. 编辑 `.sbn` 文件，实现具体逻辑
5. 在「生成绑定模板」标签页中可以看到新模板出现在列表中
6. 勾选并点击「生成代码」

### 6.4 Scriban 模板语法参考

`.sbn` 文件使用 [Scriban](https://github.com/scriban/scriban) 模板引擎。

**基本变量**（`KitExtension` 可用）：

| 变量 | 说明 |
|---|---|
| `extension_name` | 扩展名称（你在创建时填写的名称） |

**条件渲染**：
```scriban
{{ if some_flag -}}
// 条件为 true 时渲染
{{- end }}
```

> **注意**：Scriban 中只有 `null` 和 `false` 是假值，**空字符串 `""` 是真值**。  
> 请使用布尔字段（如 `has_xxx`）做条件判断，而不要直接判断字符串是否为空。

---

## 7. 生成文件说明

### 生成文件结构

```
Assets/
├── Script/Generate/UI/
│   ├── WndTestPanel.Generated.cs           <- 节点绑定（每次覆盖）
│   ├── WndTestPanel.cs                     <- 业务逻辑（仅首次生成）
│   └── WndTestPanel.IController.Generated.cs  <- MVC 集成（仅首次生成）
│
└── EUFramework/Extension/EUUI/Script/Generate/
    ├── UIKit/
    │   └── EUUIKit.EURes.Generated.cs      <- Kit 层资源扩展（可覆盖）
    └── PanelBase/
        └── EUUIPanelBase.EURes.Generated.cs  <- Panel 层资源扩展（可覆盖）
```

### 文件覆盖规则

| 文件 | 是否覆盖 | 说明 |
|---|---|---|
| `*.Generated.cs`（节点绑定） | 每次覆盖 | 每次导出都会重新生成 |
| `*.cs`（业务逻辑） | 仅首次 | 已存在则跳过，保护用户代码 |
| `*.IController.Generated.cs` | 仅首次 | 已存在则跳过 |
| 扩展代码（EURes 等） | 可手动覆盖 | 通过工具点击「生成」按钮覆盖 |

---

## 8. 常见问题

### Q: 打开面板报错 "资源加载扩展未生成"

未生成资源加载扩展代码。打开「生成绑定模板」标签页，勾选 `EUUIKit.EURes` 后点击「生成代码」。

### Q: 生成的 IController 文件报 `using ;` 错误

`architectureNamespace` 字段包含空格或不可见字符。在 `EUUITemplateConfig` Inspector 中清空该字段，重新填写后删除报错文件并重新导出。

### Q: 两个资源加载器同时存在导致 CS0111 编译错误

`LoadPanelPrefabAsync` 被定义了两次。在「生成绑定模板」标签页使用「删除生成文件」删除其中一个，或手动删除 `Generate/UIKit/` 目录下多余的 `.Generated.cs` 文件。

### Q: 节点绑定后字段没有自动赋值

可能原因：
1. 代码生成后编译失败，检查 Console 中是否有错误
2. 面板类型或命名空间与实际不符，检查 `EUUIPanelDescription` 配置
3. `EUUINodeBind` 上的 `MemberName` 字段与实际字段名不匹配

### Q: 如何重新生成 IController 文件

手动删除 `{PanelName}.IController.Generated.cs` 文件，然后在 UI 场景中重新执行导出流程。

### Q: 使用 MVC 时不需要 GetArchitecture() 方法

将 `Architecture Name` 字段留空即可。生成的分部类只实现 `IController` 接口，`GetModel`/`GetSystem` 等方法由 `IController` 的全局扩展方法提供，无需 `GetArchitecture()`。

### Q: 模板列表中出现了已删除的 .sbn 文件条目

刷新模板注册表：打开「生成绑定模板」标签页，工具会在打开时自动清理不存在的条目。若未自动清理，点击「刷新」按钮。

---

## 附录：面板生命周期

```
OpenAsync(data)
    │
    ├── CanOpen() -> false -> 中止
    │
    ├── Clear()           清理上次遗留的事件监听
    ├── OnOpen()          <- 在此初始化 UI、注册点击事件、加载数据
    ├── UniTask.Yield()   等待一帧
    └── Show()
            └── OnShow()  <- 面板可见后调用

Hide()
    └── OnHide()          <- 面板隐藏时调用（不销毁）

Close()
    ├── Clear()           清理所有事件监听
    ├── OnClose()         <- 在此释放资源、取消订阅
    └── Destroy(gameObject)
```
