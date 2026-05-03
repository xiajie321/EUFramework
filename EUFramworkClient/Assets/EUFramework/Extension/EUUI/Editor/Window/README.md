# Window

EUUI 编辑器主窗口、功能面板和 UI Toolkit 布局资源。

| 文件 | 说明 |
|---|---|
| `IEUUIEditorPanel.cs` | 主窗口各功能页的统一接口，避免与运行时 `IEUUIPanel` 混淆 |
| `EUUIEditorWindow.cs` | 主编辑器窗口入口，负责创建窗口实例、初始化 Tab 系统，并将各 Panel 挂载到对应 Tab |
| `EUUIEditorWindowHelper.cs` | 窗口共用 UI 辅助逻辑（UXML 加载、Tab 样式、Header 创建等） |
| `Panels/` | 主窗口各功能页实现 |
| `UIToolKit/` | 主窗口和各 Tab 使用的 `.uxml` / `.uss` 资源 |
