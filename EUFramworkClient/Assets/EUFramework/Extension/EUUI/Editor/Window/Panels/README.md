# Panels

EUUI 主编辑器窗口中各 Tab 对应的面板实现，均实现 `IEUUIEditorPanel` 接口。
面板由 `EUUIEditorWindow` 统一管理，每个面板对应一个功能 Tab。

| 文件 | 对应 Tab | 说明 |
|---|---|---|
| `EUUIExtensionPanel.cs` | Extensions | 管理静态扩展模板的启用/禁用，生成或删除对应的 `.Generated.cs` 文件，并同步更新 `EUUI.asmdef` 引用 |
| `EUUIModulePanel.cs` | Modules | 生成默认 SO 配置、重算程序集引用、批量重新生成/删除扩展生成文件 |
| `EUUIOrchestrationPanel.cs` | Orchestration | Hotbox 功能编排工具，负责扫描 `[EUHotboxEntry]` 并配置快捷面板布局 |
| `EUUISOConfigPanel.cs` | Config | SO 配置面板，用于查看和修改 `EUUIEditorConfig` / `EUUITemplateConfig` 等 ScriptableObject 配置 |
