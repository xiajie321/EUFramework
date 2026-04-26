# Panels

EUUI 主编辑器窗口中各 Tab 对应的面板实现，均实现 `IEUUIEditorPanel` 接口。
面板由 `EUUIEditorWindow` 统一管理，每个面板对应一个功能 Tab。

| 文件 | 对应 Tab | 说明 |
|---|---|---|
| `EUUIExtensionPanel.cs` | Extensions | 管理静态扩展模板的启用/禁用，生成或删除对应的 `.Generated.cs` 文件，并同步更新 `EUUI.asmdef` 引用 |
| `EUUIExtensionTemplateAssetCreator.cs` | Extensions | 扩展模板创建的文件写入、sidecar JSON 生成、AssetDatabase 刷新与定位 |
| `EUUIModulePanel.cs` | Modules | 生成默认 SO 配置、重算程序集引用、批量重新生成/删除扩展生成文件 |
| `EUUIOrchestrationPanel.cs` | Auto | 面板编排工具，负责批量扫描场景中的 UI 节点并生成绑定代码 |
| `EUUIResourcePanel.cs` | Export | 资源导出面板，将面板 Prefab 及相关资源按规则导出到指定目录 |
| `EUUISOConfigPanel.cs` | Config | SO 配置面板，用于查看和修改 `EUUIEditorConfig` / `EUUITemplateConfig` 等 ScriptableObject 配置 |
