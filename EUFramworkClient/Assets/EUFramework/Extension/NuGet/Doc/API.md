# NuGetForUnity API 文档

NuGetForUnity 主要作为编辑器工具使用，但也提供了一些可扩展的接口。

## 菜单项

- `NuGet -> Manage NuGet Packages`: 打开包管理窗口。
- `NuGet -> Restore Packages`: 恢复/重新安装所有包。
- `NuGet -> Create Nuspec File`: 创建新的包描述文件。

## 核心类 (Editor)

### NugetHelper
提供 NuGet 操作的核心功能。
- `InstallIdentifier`: 安装指定包。
- `Uninstall`: 卸载指定包。
- `Update`: 更新指定包。
- `Restore`: 恢复所有包。

### NugetWindow
包管理窗口类。

## 配置文件

### nuspec
NuGet 包描述文件，XML 格式。包含包的 ID、版本、作者、描述、依赖等信息。
可以通过 `NuGet -> Create Nuspec File` 创建。
