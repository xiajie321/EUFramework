# ExportsCS 导出器目录

## 🔧 导出器脚本（逻辑层）

此目录存放所有模板导出器脚本，负责处理 `.sbn` 模板并生成代码。

---

## 📋 当前导出器

### `EUUIStaticExporter.cs` - 静态模板通用导出器

**职责:** 处理所有 `Sbn/Static/` 目录下的模板

**特点:**
- ✅ 无需额外数据采集
- ✅ 直接读取 `.sbn` → 渲染 → 输出
- ✅ 一个导出器处理所有静态模板

**处理模板:**
- `Static/PanelBase/EURes.sbn` → PanelBase 扩展
- `Static/UIKit/EURes.sbn` → UIKit 资源加载器
- `Static/UIKit/Custom.sbn` → 自定义加载器
- 用户自定义的附加扩展（Static 目录下）

**使用方式:**
```csharp
// 在编辑器中调用
StaticExporter.ExportAll();
```

**菜单项:** `EUFramework/EUUI/生成所有扩展代码`

---

### `EUUIPanelExporter.cs` - Panel 动态导出器

**职责:** 处理 `Sbn/WithData/` 目录下的动态模板

**处理模板:**
- `WithData/EUUIPanel.Generated.sbn` → Panel 绑定代码
- `WithData/MVC.sbn` → MVC 架构集成（IController 分部类）

**特点:**
- ⚠️ 需要从 Unity 场景采集 UI 节点数据
- ⚠️ 需要专门的数据采集逻辑
- ⚠️ 每个 Panel 单独导出

**流程:**
1. 从场景采集 UI 节点（`EUUINodeBind` 组件）
2. 构建模板上下文（namespace、className、members）
3. 渲染 `EUUIPanel.Generated.sbn` 生成 `.Generated.cs`
4. 如果启用架构，渲染 `MVC.sbn` 生成 `.IController.Generated.cs`
5. 触发编译后自动绑定
6. 导出 Prefab

**使用方式:**
```csharp
// 选择 UIRoot GameObject
PanelExporter.StartExportProcess();
```

---

## 🚀 添加新的导出器

### 何时需要添加新导出器？

当需要处理**复杂的动态场景**（需要采集特殊数据）时，添加专用导出器。

**示例场景:**
- 对话框系统（需要采集对话选项、分支逻辑）
- 无限滚动列表（需要采集 Item 模板）
- 动画序列（需要采集时间轴数据）

### 添加步骤

#### 1. 创建导出器脚本

```csharp
// ExportsCS/DialogExporter.cs
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EUFramework.Extension.EUUI.Editor.Templates
{
    /// <summary>
    /// Dialog 动态导出器 - 处理对话框系统
    /// </summary>
    public static class DialogExporter
    {
        /// <summary>
        /// 从场景导出 Dialog 代码
        /// </summary>
        public static void ExportFromScene(GameObject dialogRoot, string outputPath)
        {
            // 1. 数据采集
            var options = CollectDialogOptions(dialogRoot);
            
            // 2. 构建模板上下文
            var context = new {
                namespace_name = "Game.UI",
                class_name = dialogRoot.name,
                dialog_options = options
            };
            
            // 3. 读取模板
            string templatePath = Path.Combine(
                EUUITemplateManager.GetTemplatesDirectory(),
                "WithData/Dialog.Generated.sbn");
            
            // 4. 渲染
            var template = Scriban.Template.Parse(File.ReadAllText(templatePath));
            string result = template.Render(context);
            
            // 5. 输出
            File.WriteAllText(outputPath, result, System.Text.Encoding.UTF8);
            
            Debug.Log($"[EUUI] Dialog 已导出: {outputPath}");
        }
        
        private static List<DialogOption> CollectDialogOptions(GameObject root)
        {
            // 实现数据采集逻辑
            // ...
        }
    }
}
```

#### 2. 创建对应的模板

在 `Sbn/WithData/` 下创建 `Dialog.Generated.sbn`:

```scriban
using UnityEngine;
using System.Collections.Generic;

namespace {{ namespace_name }}
{
    public partial class {{ class_name }} : EUUIPanelBase<{{ class_name }}>
    {
        // 对话选项
        {{~ for option in dialog_options ~}}
        public Button btn{{ option.name }};
        {{~ end ~}}
        
        protected override void OnInit()
        {
            {{~ for option in dialog_options ~}}
            btn{{ option.name }}.onClick.AddListener(On{{ option.name }}Clicked);
            {{~ end ~}}
        }
    }
}
```

#### 3. 在编辑器中集成

在 `EUUIEditorWindow.cs` 或其他编辑器脚本中调用：

```csharp
if (GUILayout.Button("导出对话框"))
{
    GameObject dialogRoot = Selection.activeGameObject;
    string outputPath = GetDialogOutputPath(dialogRoot.name);
    DialogExporter.ExportFromScene(dialogRoot, outputPath);
}
```

---

## 📝 命名约定

| 导出器类型 | 命名规则 | 示例 |
|-----------|---------|------|
| **通用静态** | `EUUIStaticExporter.cs` | EUUIStaticExporter |
| **专用动态** | `EUUI{场景}Exporter.cs` | EUUIPanelExporter<br>EUUIDialogExporter<br>EUUIPopupExporter<br>EUUIInfiniteScrollExporter |

---

## ⚙️ 导出器 vs 模板

| 对比项 | 模板 (.sbn) | 导出器 (.cs) |
|-------|------------|-------------|
| **职责** | 定义生成内容 | 处理模板并输出 |
| **位置** | `Sbn/` 目录 | `ExportsCS/` 目录 |
| **类型** | 数据层（Scriban 模板） | 逻辑层（C# 脚本） |
| **复用性** | 可被多个导出器使用 | 可处理多个模板 |
| **扩展性** | 修改生成内容 | 修改生成逻辑 |

**关系:** 一个导出器可以处理多个模板，一个模板可以被多个导出器使用。

---

## 🔍 相关文件

### 基础设施（Editor 根目录）
- `EUUITemplateManager.cs` - 模板路径管理
- `EUUITemplateRegistryAsset.cs` - 模板注册表
- `EUUITemplateRegistryGenerator.cs` - 自动扫描生成器

### 模板源文件
- `Sbn/WithData/` - 需要额外数据的模板
- `Sbn/Static/` - 直接生成的模板

### 文档
- `Templates/README.md` - 总体文档
- `Sbn/WithData/README.md` - WithData 模板说明
- `Sbn/Static/README.md` - Static 模板说明
