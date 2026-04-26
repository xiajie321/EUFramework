# EUUI Editor

EUUI 编辑器侧代码按职责分为窗口界面、场景工具、模板导出、配置资产、Hotbox 和通用辅助逻辑。

| 目录 | 职责 |
|---|---|
| `Window/` | 主配置窗口、编辑器面板接口、窗口 UI 工具、功能面板和 UI Toolkit 资源 |
| `Window/Panels/` | 主窗口各功能页，只负责 UI 编排和用户交互 |
| `Window/UIToolKit/` | UI Toolkit 的 UXML/USS 布局资源 |
| `Inspector/` | 场景制作工具、自定义 Inspector、节点绑定和 Sprite 辅助 |
| `Templates/` | `.sbn` 模板、模板注册表以及代码/Prefab 导出流程 |
| `EditorSO/` | ScriptableObject 类型与资产分层：用户配置 `Config/`、生成缓存 `Cache/`、工作区配置 `Workspace/` |
| `Helpers/` | asmdef、模板定位、内置模块描述等通用工具 |
| `Hotbox/` | Scene 视图快捷操作弹窗及入口扫描 |

## 约定

- `Window/Panels/` 中的面板实现 `IEUUIEditorPanel`，不要和运行时 `IEUUIPanel` 混用。
- 主流程类保留外部入口，具体文件写入、代码生成、Prefab 绑定等副作用放到专门 helper/exporter 中。
- `EUUIPanelExporter` 只编排自动绑定导出流程；代码生成、字段绑定、Prefab 保存分别由独立类处理。
- 编辑器工具可以依赖 UnityEditor，运行时 `Script/` 目录不能反向依赖 `Editor/`。
