# Helpers

无 UI 的纯工具/辅助类，被 Panels、Window、Templates 等其他模块调用。

| 文件 | 说明 |
|---|---|
| `EUUIAsmdefHelper.cs` | `EUUI.asmdef` / `EUUI.Editor.asmdef` 引用管理工具。提供单条引用增删（`SetAssembly`）、批量重算（`RecalculateFromGeneratedFiles`）、sidecar JSON 读取、程序集可用性检测等静态方法。`k_RuntimeBaseRefs` 常量定义了始终保留的基础引用 |
| `EUUIGeneratedFileWatcher.cs` | 监听 UIKit 生成目录下 `.Generated.cs` 的新增、删除和移动，自动同步 `EUUI_EXTENSIONS_GENERATED` 宏并重算 asmdef 引用 |
| `EUUITemplateLocator.cs` | 模板文件定位器，通过搜索 `EUUI.Editor.asmdef` 所在路径动态解析 Editor 目录、Templates 目录及注册表 SO 资产路径，与项目部署位置无关 |
| `EUUIModuleInfo.cs` | 模块信息数据结构（`EUUIModuleInfo`）及内置模块注册表（`EUUIBuiltinModules`），描述模块名称、版本、依赖程序集等元数据，供 `EUUIModulePanel` 使用 |
