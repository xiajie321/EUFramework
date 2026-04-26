# UIToolKit

EUUI 主编辑器窗口的 UI Toolkit 布局文件（`.uxml`）和样式文件（`.uss`）。
每个 `.uxml` 对应主窗口中的一个 Tab，由对应的 Panel 类在初始化时加载。

| 文件 | 对应 Panel |
|---|---|
| `EUUIEditorWindow.uxml` / `.uss` | 主窗口根布局及全局样式 |
| `ExtensionsTab.uxml` | `EUUIExtensionPanel` — 扩展模板管理 |
| `ModulesTab.uxml` | `EUUIModulePanel` — 模块管理 |
| `SceneTab.uxml` | `EUUISceneEditor` — 场景工具 |
| `ExportTab.uxml` | `EUUIResourcePanel` — 资源导出 |
| `ConfigTab.uxml` | `EUUISOConfigPanel` — SO 配置 |
| `BindTab.uxml` | `EUUINodeBindEditor` — 节点绑定 |
| `AutoTab.uxml` | `EUUIOrchestrationPanel` — 批量编排 |
| `LocateTab.uxml` | 定位/查找面板 |
| `TemplatesManagementTab.uxml` | 模板注册表管理 |
| `CreateExtensionTab.uxml` | 扩展模板创建向导 |
