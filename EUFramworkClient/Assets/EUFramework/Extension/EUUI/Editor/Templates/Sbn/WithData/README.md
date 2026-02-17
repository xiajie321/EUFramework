# WithData 模板目录

## ⚠️ 需要额外数据的模板（WithData Templates）

此目录下的模板需要从 Unity 场景中采集复杂数据，不能简单地批量生成。

## 📋 当前模板

### `EUUIPanel.Generated.sbn`

**用途:** 生成 UI Panel 的绑定代码和业务逻辑基类

**需要的数据:**
- `namespace_name` - 命名空间
- `class_name` - 类名
- `members` - UI 节点列表（从场景采集）
  - `type` - 组件类型（Button、Text、Image 等）
  - `name` - 变量名

**生成流程:**
1. 在场景中创建 UIRoot 节点
2. 添加 `EUUINodeBind` 组件标记需要绑定的节点
3. 在编辑器窗口点击"导出"
4. 自动采集节点数据 → 填充模板 → 生成代码

**配套导出器:** `ExportsCS/EUIPanelExporter.cs`

---

### `MVC.sbn`

**用途:** 为每个 Panel 生成 MVC 架构集成代码（IController 分部类）

**需要的数据:**
- `namespace_name` - 命名空间（从场景/配置获取）
- `class_name` - 面板类名（从场景名获取）
- `need_get_architecture` - 是否需要 GetArchitecture() 方法
- `architecture_name` - 架构名称（如 GameApp，从配置获取）
- `architecture_namespace` - 架构命名空间（从配置获取）

**生成流程:**
1. 在导出 Panel 时自动调用
2. 从配置和场景信息获取面板名和命名空间
3. 根据配置决定是否生成 `GetArchitecture()` 方法
4. 生成 `{ClassName}.IController.Generated.cs` 文件

**配套导出器:** `ExportsCS/EUIPanelExporter.cs` (在 `GenerateMVCIntegration()` 中调用)

**注意:** 此模板需要动态数据（面板名、命名空间），因此归类为 `WithData/` 模板，而非静态模板。

## 🚫 不要在此目录添加静态模板

如果你的模板不需要从场景采集数据，请放在 `Static/` 目录下。

## 📝 添加新的数据驱动模板

如果需要添加类似的模板（需要采集场景数据）：

1. 在此目录创建 `.sbn` 文件
2. 在对应的 Editor 类中实现数据采集逻辑
3. 调用模板渲染

**示例:**
```csharp
// 采集数据
var members = CollectUIMembers(sceneObject);

// 填充模板
var context = new { 
    namespace_name = "Game.UI", 
    class_name = "MyPanel",
    members = members 
};

// 渲染模板
string templatePath = EUUITemplateManager.GetTemplatePath(TemplateType.PanelGenerated);
var template = Scriban.Template.Parse(File.ReadAllText(templatePath));
string code = template.Render(context);
```
