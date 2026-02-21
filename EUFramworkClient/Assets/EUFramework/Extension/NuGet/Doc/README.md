# NuGetForUnity 使用文档

## 概述

NuGetForUnity 是一个从零开始构建的 NuGet 客户端，旨在 Unity 编辑器中运行。它提供了一个可视化编辑器窗口，可查看服务器上的可用包、已安装的包以及可用的包更新。

## 功能特性

- **可视化管理**：提供编辑器窗口管理 NuGet 包。
- **在线搜索**：搜索并安装 NuGet 服务器上的包。
- **依赖管理**：自动解析并安装依赖项。
- **本地缓存**：使用本地缓存避免重复下载。
- **包创建**：提供界面创建 `.nuspec` 文件并打包发布。

## 如何使用

要启动，请在菜单栏选择 `NuGet -> Manage NuGet Packages`。

### 界面说明

- **Online (在线)**：显示 NuGet 服务器上可用的包。支持搜索、显示所有版本、显示预发布版本。
- **Installed (已安装)**：显示当前 Unity 项目中已安装的包。支持卸载。
- **Updates (更新)**：显示有可用更新的包。支持一键更新。

## 配置文件 (NuGet.config)

NuGetForUnity 使用 `NuGet.config` 文件配置包源和安装路径。默认配置文件会自动创建。

```xml
<?xml version="1.0" encoding="utf-8"?> 
<configuration> 
  <packageSources> 
    <add key="NuGet" value="http://www.nuget.org/api/v2/" /> 
  </packageSources> 
  <config> 
    <add key="repositoryPath" value="./Packages" /> 
  </config> 
</configuration> 
```

## 文档说明

- **API文档**：请查阅 [API.md](API.md) 获取详细的接口说明。
- **更新日志**：请查阅 [Update.md](Update.md) 获取版本更新历史。
