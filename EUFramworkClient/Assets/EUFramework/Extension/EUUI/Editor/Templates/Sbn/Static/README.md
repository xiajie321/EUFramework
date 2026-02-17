# Static 模板目录

## ✅ 直接生成模板（Static Templates）

此目录下的模板是**静态的**，不需要复杂的数据采集，可以统一批量生成。

## 📁 子目录分类

### `PanelBase/` - EUUIPanelBase 扩展
为 `EUUIPanelBase` 类添加静态扩展方法。

**当前模板:**
- `EURes.sbn` - EURes 资源加载扩展

**使用方式:**
```csharp
// 生成后可以在 PanelBase 中使用
protected override async UniTask OnInit()
{
    await this.LoadSpriteAsync("icon");  // EURes 扩展方法
}
```

**导出器:** `ExportsCS/EUIStaticExporter.cs` (通用静态导出器)

---

### `UIKit/` - EUUIKit 扩展
为 `EUUIKit` 类添加分部静态类。

**当前模板:**
- `EURes.sbn` - EURes 资源加载器（框架默认）
- `Custom.sbn` - 自定义加载器示例模板
- `Addressables.sbn` - Addressables 加载器（示例）
- `Resources.sbn` - Unity Resources 加载器（示例）

**使用方式:**
```csharp
// 生成后可以在 UIKit 中使用
await EUUIKit.OpenAsync<MainPanel>();  // 自动使用配置的加载器
```

**导出器:** `ExportsCS/EUIStaticExporter.cs` (通用静态导出器)

---

## 🚀 添加新扩展模板

### 方式 1: 直接添加到此目录（推荐）

**步骤:**
1. 在对应的子目录创建 `.sbn` 文件
2. 保存文件
3. 框架自动检测并刷新注册表
4. 立即可用！

**示例: 添加 DoTween 扩展**
```
Static/
└── PanelBase/
    ├── EURes.sbn
    └── DoTween.sbn  ← 新增
```

创建 `DoTween.sbn`:
```csharp
using DG.Tweening;

namespace EUFramework.Extension.EUUI
{
    public static class EUUIPanelBaseDoTweenExtensions
    {
        public static Tween FadeIn<T>(this EUUIPanelBase<T> panel, float duration = 0.3f) 
            where T : EUUIPanelBase<T>
        {
            // DoTween 实现
        }
    }
}
```

保存后，注册表会自动更新，无需修改任何代码！

### 方式 2: 在配置中添加附加扩展

在 `EUUIEditorConfig.asset` 的 `manualExtensions` 列表中添加：
```
名称: DoTween
目标: EUUIPanelBase
模板路径: Assets/MyGame/Templates/DoTween.sbn
启用: ✓
```

---

## 🎯 模板类型区分

| 目录 | 类型 | 特点 | 生成方式 |
|------|------|------|---------|
| `WithData/` | 需要额外数据 | 需要场景数据 | 逐个导出 |
| `Static/PanelBase/` | 直接生成 | 静态扩展 | 批量生成 |
| `Static/UIKit/` | 直接生成 | 分部类 | 批量生成 |
| `Static/Architecture/` | 直接生成 | 简单配置 | 逐个生成 |

---

## 📝 模板命名约定

### PanelBase 扩展
```
PanelBase/
├── EURes.sbn           → ID: PanelBaseEURes
├── DoTween.sbn         → ID: PanelBaseDoTween
└── OSA.sbn             → ID: PanelBaseOSA
```

### UIKit 扩展
```
UIKit/
├── EURes.sbn           → ID: KitEURes
├── Custom.sbn          → ID: KitCustom
├── Addressables.sbn    → ID: KitAddressables
└── MyLoader.sbn        → ID: KitMyLoader
```

### 生成规则
- 文件名去除扩展名后作为部分 ID
- 结合所在目录生成完整 ID
- 特殊名称有预定义映射（如 `EURes.sbn` → `PanelBaseEURes`）

---

## ⚙️ 相关文件

- `EUUITemplateRegistry.asset` - 注册表资产（自动生成）
- `EUUITemplateRegistryGenerator.cs` - 注册表生成器
- `TemplateDirectoryWatcher.cs` - 文件变化监听器
- `EUUITemplateManager.cs` - 模板路径管理器
