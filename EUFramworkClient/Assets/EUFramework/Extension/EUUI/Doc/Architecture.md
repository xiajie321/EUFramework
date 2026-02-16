# EUUI 架构设计说明

## 核心设计原则

EUUI 采用**核心 + 扩展**的插件化架构，遵循以下原则：

### 1. 依赖倒置原则
- **核心层**：只依赖 Unity 原生 API 和通用库（如 UniTask）
- **扩展层**：通过分部类（partial class）和模板生成，按需引入框架依赖

### 2. 可移植性
核心类（`EUUIKit.cs`、`EUUIPanelBase.cs` 等）可以独立发布为 Unity Package，在任何项目中使用。

### 3. 可插拔架构
所有框架特定功能通过 `.sbn` 模板生成 `.Generated.cs` 文件，实现可选性扩展。

---

## 架构层次

```
┌─────────────────────────────────────────────┐
│          核心层（Core Layer）                │
│  - EUUIKit.cs                               │
│  - EUUIPanelBase.cs                         │
│  - 只依赖: Unity + UniTask                  │
│  - 提供: UI 管理、生命周期、层级控制        │
└─────────────────────────────────────────────┘
                    ↓ (partial class)
┌─────────────────────────────────────────────┐
│         扩展层（Extension Layer）            │
│  - EUUIKit.EURes.Generated.cs               │
│  - EUUIPanelBaseEUResExtensions.Generated.cs│
│  - 依赖: EUFramework.Extension.EURes        │
│  - 提供: 资源加载（YooAssets）              │
└─────────────────────────────────────────────┘
                    ↓ (可选)
┌─────────────────────────────────────────────┐
│        业务层（Business Layer）              │
│  - GamePanelBase.cs (中间基类)              │
│  - WndTestPanel.cs (具体面板)               │
│  - 依赖: 项目架构（如 MVC）                 │
└─────────────────────────────────────────────┘
```

---

## 核心类职责

### EUUIKit.cs
**职责**：UI 系统管理、面板生命周期
**依赖**：
```csharp
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
```

**提供方法**：
- `Initialize()` - 初始化 UI 系统
- `OpenAsync<T>()` - 打开面板
- `Close<T>()` - 关闭面板
- `GetPanel<T>()` - 获取面板实例
- `GetLayer()` - 获取层级 Transform

**不包含**：
- ❌ 资源加载逻辑（由扩展提供）
- ❌ 框架依赖（如 EUResKit）

### EUUIPanelBase.cs
**职责**：面板基类、UI 事件、生命周期
**依赖**：Unity 原生 API
**提供功能**：
- 面板生命周期回调（OnOpen/OnShow/OnHide/OnClose）
- UI 事件管理（AddClick、AddLongPress 等）
- 层级管理（DefaultLayer）

**不包含**：
- ❌ 图片加载（如 SetImage(Image, string url)，由扩展提供）
- ❌ Prefab 加载

---

## 扩展机制

### 分部类扩展（EUUIKit）
通过 `public static partial class EUUIKit` 机制，由模板生成扩展方法。

#### EUUIKit.EURes.Generated.cs
**生成自**：`EUUIKit.EURes.sbn` 模板

**提供方法**：
```csharp
// 核心方法：为 OpenAsync<T> 提供加载能力
private static async UniTask<GameObject> LoadPanelPrefabAsync<T>()
    where T : EUUIPanelBase<T>

// 内部方法：实际加载逻辑
private static async UniTask<GameObject> LoadPanelPrefabAsync(
    string prefabPath, bool isRemote)

// 公开方法：供外部直接使用
public static async UniTask<GameObject> LoadUIPrefabAsync(
    string packageName, string panelName, bool isRemote)
public static SpriteAtlas LoadAtlas(string atlasName, bool isRemote)
```

**依赖**：
```csharp
using EUFramework.Extension.EURes;
```

### 静态扩展方法（EUUIPanelBase）
通过 `public static class` 为 `EUUIPanelBase<T>` 添加扩展方法。

#### EUUIPanelBaseEUResExtensions.Generated.cs
**生成自**：`EUUIPanelBase.EURes.sbn` 模板

**提供方法**：
```csharp
// 为面板提供图片加载能力
public static void SetImage<T>(this EUUIPanelBase<T> panel, 
    Image image, string url, bool? isRemote = null, bool isSetNativeSize = true)

public static Sprite LoadSprite<T>(this EUUIPanelBase<T> panel, 
    string url, bool? isRemote = null)

public static async UniTask<GameObject> LoadPrefabAsync<T>(
    this EUUIPanelBase<T> panel, string path)
```

---

## 扩展示例

### 场景一：使用 EUResKit（当前默认）
1. 在 `EUUIEditorConfig` 中启用 `enableEUResExtension = true`
2. 点击"生成扩展代码"按钮
3. 生成文件：
   - `EUUIKit.EURes.Generated.cs`
   - `EUUIPanelBaseEUResExtensions.Generated.cs`

**使用示例**：
```csharp
// 打开面板（自动使用 EURes 加载）
var panel = await EUUIKit.OpenAsync<WndTestPanel>();

// 面板内加载图片（使用扩展方法）
this.SetImage(imgIcon, "Atlas/icon", isRemote: true);

// 加载 Prefab
var prefab = await this.LoadPrefabAsync("Items/ItemIcon");
```

### 场景二：切换到 Unity Addressables
1. 创建 `EUUIKit.Addressables.sbn` 模板
2. 实现同样签名的方法：
   ```csharp
   private static async UniTask<GameObject> LoadPanelPrefabAsync<T>() {...}
   ```
3. 在配置中切换扩展类型
4. 重新生成 → **无需修改核心代码和业务代码**

### 场景三：最小化依赖（仅 Resources）
1. 创建 `EUUIKit.Resources.sbn` 模板
2. 使用 `Resources.LoadAsync()` 实现加载
3. 业务代码调用方式完全不变

---

## MVC 集成（可选）

MVC 集成同样遵循可插拔原则，通过 `EUUI.MVC.sbn` 模板生成。

### 集成方式：分部类

```csharp
// 主文件：UI 功能（EUUIPanel.Generated.sbn 生成）
public partial class WndTestPanel : EUUIPanelBase<WndTestPanel>
{
    public override string PackageName => "Test";
    // ... 业务逻辑
}

// 分部类：MVC 功能（EUUI.MVC.sbn 生成）
public partial class WndTestPanel : IController
{
    public IArchitecture GetArchitecture() => GameApp.Interface;
}
```

### 架构模式支持

**模式一：CoreExtension 全局架构（框架内部）**
```csharp
// EUUIEditorConfig: architectureName 留空
// 生成的代码不包含 GetArchitecture() 方法
// IController 扩展直接使用 CoreExtension.GetArchitecture()
```

**模式二：显式架构（QFramework 重构模式）**
```csharp
// EUUIEditorConfig: architectureName = "GameApp"
// 生成的代码包含：
public IArchitecture GetArchitecture() => GameApp.Interface;
```

### 高级需求：自定义中间层

如果需要在所有面板之间插入通用逻辑，可以手动创建中间基类：

```csharp
// 手动创建（不由框架生成）
public abstract class GamePanelBase<T> : EUUIPanelBase<T>, IController 
    where T : GamePanelBase<T>
{
    public IArchitecture GetArchitecture() => GameApp.Interface;
    
    // 项目级通用逻辑
    protected virtual void LogPanelOpen() { /* 埋点 */ }
}

// 业务面板继承（修改生成代码的基类）
public partial class WndTestPanel : GamePanelBase<WndTestPanel>
{
    // 既有 UI 功能，又有 MVC 功能，还有项目级功能
}
```

---

## 配置选项

### EUUIEditorConfig.cs
```csharp
[Header("扩展模块")]
public bool enableEUResExtension = true;  // 启用 EURes 资源加载

[Header("代码生成-架构集成")]
public bool useArchitecture = true;       // 启用 MVC 架构
public string architectureName = "";      // 架构名称（留空=CoreExtension 全局架构）
public string architectureNamespace = ""; // 架构命名空间（如 Game.Architecture）
```

---

## 优势总结

✅ **可移植性**：核心层无框架依赖，可独立发布
✅ **可扩展性**：通过模板生成，支持任意资源方案
✅ **类型安全**：编译时检查，无运行时委托注入
✅ **IDE 友好**：自动补全、跳转定义、类型提示
✅ **按需生成**：不需要的扩展不生成，保持代码清洁
✅ **开闭原则**：对扩展开放，对修改封闭

---

## 文件组织

```
EUUI/
├── Script/                         # 核心层（无框架依赖）
│   ├── EUUIKit.cs                  ✓ 核心管理类
│   ├── EUUIPanelBase.cs            ✓ 面板基类
│   ├── EUUIEnum.cs                 ✓ 枚举定义
│   ├── EUUIKitConfig.cs            ✓ 运行时配置
│   └── *.Generated.cs              🔌 扩展生成文件
│
├── Editor/
│   ├── Templates/                  # 模板层
│   │   ├── EUUIPanel.Generated.sbn     面板代码模板
│   │   ├── EUUI.MVC.sbn                MVC 集成模板
│   │   ├── EUUIKit.EURes.sbn           EURes 扩展模板
│   │   └── EUUIPanelBase.EURes.sbn     面板扩展模板
│   │
│   ├── EUUIEditorConfig.cs         # 编辑器配置
│   └── EUUIPrefabExportEditor.cs   # 代码生成器
│
└── Doc/
    └── Architecture.md             # 本文档
```

---

**设计者**：EUFramework Team  
**最后更新**：2026-02-13
