# Inspector

Unity 自定义 Editor 工具及场景辅助编辑器，在 Inspector 或 Scene 视图中提供额外交互能力。

| 文件 | 说明 |
|---|---|
| `EUUISceneEditor.cs` | 场景工具入口，提供创建 UI 场景、定位 UIRoot/Prefab、创建或更新 Area 等操作 |
| `EUUISceneCreateWindow.cs` | 创建 UI 场景时的输入窗口，负责面板名和 `EUUIPanelDescription` 编辑 |
| `EUUIAreaCreateWindow.cs` | 创建 Area 设计参考框的输入窗口，负责玩家数和分屏布局选择 |
| `EUUIEditorWindowExtensions.cs` | EditorWindow 通用扩展方法，例如居中显示窗口 |
| `EUUINodeBindEditor.cs` | 节点绑定编辑器，扫描 Prefab 层级中带有 `EUUIBind` 标记的节点，在 Inspector 中提供批量绑定与代码生成入口 |
