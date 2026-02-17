# EUUI 模板系统说明

## 📋 概述

EUUI 使用 **ScriptableObject 元数据** 自动管理模板文件，按**生成类型**分类。

## 🔄 自动检测机制

### 1. **启动时自动扫描**
- Unity 启动时自动扫描 `Templates/` 目录
- 生成 `TemplateRegistry.asset` 注册表
- 记录所有 `.sbn` 模板文件

### 2. **文件变化自动刷新**
- 添加新模板 → 自动刷新注册表
- 删除模板 → 自动刷新注册表
- 移动模板 → 自动刷新注册表

### 3. **手动刷新**
菜单: `EUFramework/EUUI/刷新模板注册表`

## 📁 目录结构（按职责分类）

```
Templates/
├── Sbn/                            ← 📄 模板源文件（数据层）
│   ├── WithData/                   ⚠️ 需要额外数据的模板
│   │   ├── EUUIPanel.Generated.sbn # 需要从场景采集 UI 节点数据
│   │   └── MVC.sbn                 # MVC 架构集成（需要面板名、命名空间）
│   │
│   └── Static/                     ✅ 直接生成的模板
│       ├── PanelBase/              # PanelBase 扩展
│       │   └── EURes.sbn           # EURes 资源加载扩展
│       │
│       └── UIKit/                  # UIKit 扩展
│           ├── EURes.sbn           # EURes 资源加载器
│           ├── Custom.sbn          # 自定义加载器示例
│           ├── Addressables.sbn    # Addressables 加载器（示例）
│           └── Resources.sbn       # Resources 加载器（示例）
│
├── ExportsCS/                      ← 🔧 导出器脚本（逻辑层）
│   ├── EUUIStaticExporter.cs       # 静态模板导出器（通用）
│   └── EUUIPanelExporter.cs        # Panel 动态导出器（专用）
│
└── README.md                       # 本文档
```

## 🎯 生成类型说明

### ⚠️ **需要额外数据（WithData）**
**位置:** `WithData/`

**特点:**
- 需要从 Unity 场景中采集复杂数据
- 每个 Panel 都要单独生成
- 必须配合专门的编辑器工具使用

**示例:**
- `EUUIPanel.Generated.sbn` - 需要采集 UI 节点、组件类型、变量名等
- `MVC.sbn` - 需要面板名、命名空间（在导出 Panel 时自动生成）

**使用方式:**
在编辑器窗口 `EUFramework/EUUI 配置工具` 中：
1. 选择 UIRoot 场景对象
2. 点击"导出"按钮
3. 自动采集数据并生成代码（包括 Panel 代码和 MVC 集成代码）

### ✅ **直接生成模板（Static）**
**位置:** `Static/`

**特点:**
- 模板内容固定或仅需简单配置
- 不需要复杂的数据采集
- 可以统一批量生成

**示例:**
- `PanelBase/EURes.sbn` - 静态扩展方法
- `UIKit/EURes.sbn` - 静态分部类

**使用方式:**
在编辑器窗口的"扩展模板"标签中点击"生成扩展代码"

## 🎯 使用方式

### **方式 1: 使用框架默认模板（推荐）**

不需要任何配置，框架会自动检测并使用 `Templates/` 目录下的模板。

### **方式 2: 覆盖特定模板**

在 `EUUIEditorConfig.asset` 中添加覆盖：

```
核心模板管理（自动检测）
└── 自定义模板覆盖
    └── Element 0
        ├── Template Id: "PanelGenerated"
        ├── Custom Path: "Assets/MyGame/Templates/MyPanel.sbn"
        └── Enabled: ✓
```

**覆盖优先级：**
```
自定义覆盖 > 框架默认模板
```

## 📝 添加新模板

### 1. **添加框架级模板**

直接在 `Sbn/` 目录下添加 `.sbn` 文件，框架会自动检测。

**示例：添加 DoTween 扩展模板**

```
Templates/
└── Sbn/
    └── Static/
        └── PanelBase/
            ├── EURes.sbn
            └── DoTween.sbn  ← 新增
```

保存后自动刷新注册表，无需修改代码！静态模板会由 `StaticExporter` 自动处理。

### 2. **添加项目级模板**

在项目中创建 `.sbn` 文件，然后在配置中引用：

```
Assets/MyGame/Templates/MyCustomPanel.sbn
```

在 `EUUIEditorConfig.asset` 中添加覆盖指向此文件。

## 🔍 查看注册表

打开 `Assets/EUFramework/Extension/EUUI/Editor/EUITemplateRegistry.asset`：

```yaml
Version: 1.0.0
Last Updated: 2024-12-19 15:30:00
Templates Directory: Assets/EUFramework/Extension/EUUI/Editor/Templates

Templates:
  - ID: PanelGenerated
    Name: 面板生成模板
    Category: Core
    Path: Core/EUUIPanel.Generated
    Required: ✓
  
  - ID: MVCArchitecture
    Name: MVC 架构集成
    Category: Architecture
    Path: Architecture/EUUI.MVC
    Required: ✗
  
  # ... 更多模板
```

## ⚙️ 高级配置

### **模板 ID 映射规则**

| 文件名 | 生成的 ID |
|--------|-----------|
| `EUUIPanel.Generated.sbn` | `PanelGenerated` |
| `EUUI.MVC.sbn` | `MVCArchitecture` |
| `EUUIPanelBase.EURes.sbn` | `PanelBaseEURes` |
| `EUUIKit.EURes.sbn` | `KitEURes` |
| `EUUIKit.Custom.sbn` | `KitCustom` |
| `MyCustom.Template.sbn` | `MyCustomTemplate` |

### **自定义模板元数据**

编辑 `TemplateRegistryGenerator.cs` 中的 `CoreTemplateConfig` 字典：

```csharp
private static readonly Dictionary<string, (string name, string desc, bool required)> CoreTemplateConfig = new()
{
    { "PanelGenerated", ("面板生成模板", "生成 Panel 绑定代码", true) },
    // 添加你的自定义模板配置
    { "MyCustomTemplate", ("我的自定义模板", "自定义功能描述", false) }
};
```

## 🚀 迁移指南

### 从旧版本（硬编码路径）迁移

**旧版本（EUUIEditorConfig.cs）：**
```csharp
public string panelGeneratedTemplate = "Core/EUUIPanel.Generated";
public string mvcArchitectureTemplate = "Architecture/EUUI.MVC";
```

**新版本：**
- ❌ 删除这些字段（已自动检测）
- ✅ 如需覆盖，使用 `templateOverrides` 列表

## ❓ 常见问题

### Q: 注册表没有更新？
**A:** 手动执行菜单: `EUFramework/EUUI/刷新模板注册表`

### Q: 如何添加自己的模板？
**A:** 两种方式：
1. 直接在 `Templates/` 目录添加（自动检测）
2. 在项目中创建，通过配置引用（手动覆盖）

### Q: 删除模板后还能用吗？
**A:** 不能。删除后注册表会自动更新，调用时会报错。

### Q: 可以重命名模板吗？
**A:** 可以。重命名后会自动刷新注册表，但会生成新的 ID。

## 📚 相关文件

- `EUUITemplateRegistryAsset.cs` - 注册表数据结构
- `EUUITemplateRegistryGenerator.cs` - 注册表生成器
- `EUUITemplateDirectoryWatcher.cs` - 文件变化监听器
- `EUUITemplateRegistryInitializer.cs` - 启动时初始化
- `ExportsCS/EUIStaticExporter.cs` - 静态模板通用导出器
- `ExportsCS/EUIPanelExporter.cs` - Panel 动态导出器

## 🔧 添加自定义导出器

如果需要处理复杂的动态场景（如从场景采集数据），可以添加专用导出器：

### 步骤：

1. **在 `ExportsCS/` 下创建导出器脚本**
   ```csharp
   // ExportsCS/DialogExporter.cs
   namespace EUFramework.Extension.EUUI.Editor.Templates
   {
       public static class DialogExporter
       {
           public static void Export(GameObject dialogRoot, string outputPath)
           {
               // 1. 采集对话框特定数据
               var dialogData = CollectDialogData(dialogRoot);
               
               // 2. 加载模板
               string templatePath = "Templates/Sbn/WithData/Dialog.Generated.sbn";
               
               // 3. 渲染
               var template = Scriban.Template.Parse(File.ReadAllText(templatePath));
               string result = template.Render(dialogData);
               
               // 4. 输出
               File.WriteAllText(outputPath, result);
           }
       }
   }
   ```

2. **创建对应的 `.sbn` 模板**
   - 在 `Sbn/WithData/` 下创建 `Dialog.Generated.sbn`

3. **在编辑器中调用**
   ```csharp
   DialogExporter.Export(selectedGameObject, outputPath);
   ```

**命名约定：** `EUUI{场景}Exporter.cs` (如 `EUUIPanelExporter`、`EUUIDialogExporter`、`EUUIPopupExporter`)
- `EUUITemplateManager.cs` - 模板路径管理器

---

**提示：** 此系统完全自动化，正常使用无需手动配置！🎉
