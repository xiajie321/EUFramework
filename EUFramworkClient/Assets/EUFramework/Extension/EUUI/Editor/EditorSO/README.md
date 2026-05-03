# EditorSO

EUUI 编辑器使用的 ScriptableObject 类型、配置资产和路径管理。

## 资产分层

| 目录 | 类型 | 说明 |
|---|---|---|
| `Config/` | 用户配置 | 用户长期维护的配置资产，以及创建/同步这些配置的脚本 |
| `Cache/` | 生成缓存 | 工具扫描/刷新生成的过程资产，以及生成/监听缓存的脚本 |
| `Workspace/` | 工作区配置 | 编辑器工具布局和编排结果，以及加载/创建工作区配置的脚本 |

## 资产说明

| 文件 | 分类 | 说明 |
|---|---|---|
| `Config/EUUIEditorConfig.asset` | 用户配置 | 编辑器全局配置，存储分辨率、UI 场景路径、Prefab 路径等 |
| `Config/EUUITemplateConfig.asset` | 用户配置 | 模板与代码生成配置，记录命名空间、架构集成、输出目录和扩展模板启用状态 |
| `Cache/EUUITemplateRegistry.asset` | 生成缓存 | 模板注册表，由工具扫描 `Templates/Sbn` 自动生成或刷新 |
| `Workspace/EUHotboxConfig.asset` | 工作区配置 | Hotbox 功能编排配置，存储快捷操作区域和条目布局 |

`Resources/EUUIKitConfig.asset` 是运行时配置，由 `EUUIEditorConfig` 同步生成，不放在 `EditorSO/` 下。

## 代码说明

| 文件 | 说明 |
|---|---|
| `EUUIEditorSOPaths.cs` | `EditorSO/Config`、`EditorSO/Cache`、`EditorSO/Workspace` 的统一路径入口 |
| `Config/EUUIEditorConfig.cs` | 编辑器全局配置 SO 类型 |
| `Config/EUUITemplateConfig.cs` | 模板与代码生成配置 SO 类型 |
| `Config/EUUIEditorConfigEditor.cs` | `EUUIEditorConfig` 和 `EUUITemplateConfig` 的创建入口与自定义 Inspector |
| `Config/EUUIEditorConfigSync.cs` | 将 `EUUIEditorConfig` 同步到运行时 `EUUIKitConfig` 的同步逻辑 |
| `Cache/EUUITemplateRegistryAsset.cs` | 模板注册表 SO 类型 |
| `Cache/EUUITemplateRegistryGenerator.cs` | 注册表生成器，扫描 `Templates/Sbn` 目录下所有 `.sbn` 文件 |
| `Cache/EUUITemplateRegistryInitializer.cs` | 编辑器启动时自动初始化注册表，确保首次打开工程时注册表存在且有效 |
| `Cache/EUUITemplateDirectoryWatcher.cs` | `AssetPostprocessor` 监听器，当模板目录文件发生变更时自动刷新注册表 |
| `Workspace/EUHotboxConfigSO.cs` | Hotbox 配置 SO 类型 |
| `Workspace/EUHotboxConfigProvider.cs` | Hotbox 配置 SO 的加载与自动创建入口 |
