# NuGetForUnity 使用文档

## 什么是 NuGetForUnity？

NuGetForUnity 是一个从零开始构建的 NuGet 客户端，旨在 Unity 编辑器中运行。NuGet 是一个包管理系统，可以轻松创建分发在服务器上并供用户使用的包。NuGet 支持包的语义版本控制以及对其他包的依赖。

您可以在此处了解有关 NuGet 的更多信息：[nuget.org](https://www.nuget.org/)

NuGetForUnity 提供了一个可视化编辑器窗口，用于查看服务器上的可用包、已安装的包以及可用的包更新。还提供了一个可视化界面来创建和编辑 `.nuspec` 文件，以便在 Unity 中定义和发布您自己的 NuGet 包。

## 如何安装 NuGetForUnity？

将提供的 Unity 包安装到您的 Unity 项目中。

## 如何使用 NuGetForUnity？

要启动，请在菜单栏选择 **NuGet → Manage NuGet Packages**。

几秒钟后（查询服务器上的包可能需要一些时间），您应该会看到一个管理窗口。

### Online (在线) 标签

显示 NuGet 服务器上可用的包。

*   **Show All Versions (显示所有版本)**：启用以列出包的所有旧版本（不适用于 nuget.org）。禁用以仅显示包的最新版本。
*   **Show Prerelease (显示预发布)**：启用以列出包的预发布版本（alpha、beta、候选发布版等）。禁用以仅显示稳定版本。
*   **Search (搜索)**：在搜索框中输入搜索词以过滤显示的内容。
*   **Refresh (刷新)**：按刷新按钮以使用最新的查询设置刷新窗口。（在将新包推送到服务器并希望在不关闭并重新打开窗口的情况下查看它时很有用。）

列表会显示包的名称、版本（在方括号中）和描述。

*   点击 **View License** 在 Web 浏览器中打开许可证。
*   点击 **Install** 安装包。
    *   **注意**：如果包已安装，将显示 **Uninstall** 按钮，允许您卸载包。

### Installed (已安装) 标签

显示当前 Unity 项目中已安装的包。

*   点击 **Uninstall** 按钮卸载包。

### Updates (更新) 标签

显示当前安装的且在服务器上有可用更新的包。

左侧括号中的版本是新版本号。更新按钮中括号内的版本是当前安装的版本。

*   点击 **Update** 按钮卸载当前包并安装新包。

## NuGetForUnity 如何工作？

NuGetForUnity 加载 Unity 项目中的 `NuGet.config` 文件（如果尚不存在，则自动创建），以确定应从哪个服务器拉取包以及将包推送到哪个服务器。默认情况下，此服务器设置为 nuget.org 包源。

默认的 `NuGet.config` 文件内容如下：

```xml
<?xml version="1.0" encoding="utf-8"?> 
<configuration> 
  <packageSources> 
    <add key="NuGet" value="http://www.nuget.org/api/v2/" /> 
  </packageSources> 
  <activePackageSource> 
    <add key="NuGet" value="http://www.nuget.org/api/v2/" /> 
  </activePackageSource> 
  <config> 
    <add key="repositoryPath" value="./Packages" /> 
    <add key="DefaultPushSource" value="http://www.nuget.org/api/v2/" /> 
  </config> 
</configuration> 
```

您可以将其更改为任何其他 NuGet 服务器（例如 NuGet.Server 或 ProGet - 见下文）。如果您正在编辑 `NuGet.config` 文件，可以使用 **NuGet → Reload NuGet.config** 菜单项重新加载配置。

在此处查看有关 `NuGet.config` 文件的更多信息：[NuGet Config Settings](https://docs.nuget.org/consume/nuget-config-settings)

NuGetForUnity 将包安装到 `NuGet.config` 文件中定义的本地存储库路径 (`repositoryPath`)。默认情况下，这设置为 `Assets/Packages` 文件夹。在 `NuGet.config` 文件中，这可以是完整路径，也可以是基于项目 `Assets` 文件夹的相对路径。

**注意**：您可能希望版本控制软件忽略您的 `Packages` 文件夹，以防止 NuGet 包在您的存储库中进行版本控制。

安装包时，项目中的 `packages.config` 文件会自动更新为特定的包信息以及所有已安装的依赖项。这允许随时从头开始恢复包。每次打开项目或重新编译项目代码时，都会自动运行恢复操作。可以通过选择 **NuGet → Restore Packages** 菜单项手动运行它。

**注意**：根据您需要安装的包的大小和数量，恢复操作可能需要很长时间，请耐心等待。如果 Unity 似乎无法启动或无响应，请在尝试终止进程之前再等待几分钟。

如果您对 NuGetForUnity 遵循的过程感兴趣，或者试图调试问题，可以强制 NuGetForUnity 使用详细日志记录将增加的数据量输出到 Unity 控制台。在 `NuGet.config` 文件的 `<config>` 元素中添加行 `<add key="verbose" value="true" />`。您可以通过将值设置为 `false` 或完全删除该行来禁用详细日志记录。

从 NuGet 服务器下载的 `.nupkg` 文件缓存在当前用户的应用程序数据文件夹中 (`C:\Users\[username]\AppData\Local\NuGet\Cache`)。以前安装的包通过缓存文件夹安装，而不是再次从服务器下载。

## 如何在 Unity 中创建自己的 NuGet 包？

首先，您需要创建一个定义包的 `.nuspec` 文件。在“Project”窗口中，右键单击希望 `.nuspec` 文件所在的位置，然后选择 **NuGet → Create Nuspec File**。

选择新的 `.nuspec` 文件，您应该会看到配置界面。
输入包的适当信息（ID、版本、作者、描述等）。请务必包含包所需的任何依赖项。

*   点击 **Pack** 按钮将包打包为 `.nupkg` 文件，该文件保存在 `C:\Users\[username]\AppData\Local\NuGet\Cache` 文件夹中。
*   点击 **Push** 按钮将包推送到服务器。请务必设置正确的 API 密钥，以便您有权推送到服务器（如果您的服务器配置为使用 API 密钥）。

## 如何创建自己的 NuGet 服务器来托管 NuGet 包？

您可以使用 NuGet.Server、NuGet Gallery、ProGet 等来创建自己的 NuGet 服务器。
或者，您可以使用“本地源”，它只是硬盘或网络共享上的文件夹。
请务必在 `NuGet.config` 文件中设置正确的 URL/路径，然后就可以开始了！

在此处阅读更多信息：[Hosting Your Own NuGet Feeds](http://docs.nuget.org/create/hosting-your-own-nuget-feeds)
