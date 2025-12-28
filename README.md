# FramworkerCore分支

该分支是进行工具链整合的分支,内容包含:Unity客户端框架、工具的编辑器集成。

## 已经完成的工作

- 引入Luban并附带一个Tools\Luban\Data\GamConfig\Luban配置工具.exe便于配置鲁班路径跟解析方式。

- 引入PrimeTween动画库。

- 引入Zlinq查询库。

- 引入NuGetForUnity集成在Unity内的NuGet管理库。

- 引入UniTask。

- 引入YooAsset资源管理方案。

- 引入HybridCLR。

## 正在进行的工作

- 引入Fantasy框架。

## 计划

- 将LuBan工具集成到Unity编辑器中,而非外置,可以通过编辑器内面板修改数据映射到Exl再进行生成Json的操作。
- 将框架核心移植到Godot中。

---

# PureCore分支

该框架的PureCore分支仅包含常用库,无任何额外的编辑器工具或者框架,省去游戏开发新建项目时引入常用库时的各种导包操作,仅需下载该分支作为模板即可。

注: 该模板创建版本为Unity 2022.3.62f2c1版本 渲染管线为urp(仅安装了2d相关库,如需3d开发请自行安装)

## 已经完成的工作

- 引入Luban并附带一个Tools\Luban\Data\GamConfig\Luban配置工具.exe便于配置鲁班路径跟解析方式。

- 引入PrimeTween动画库。

- 引入Zlinq查询库。

- 引入NuGetForUnity集成在Unity内的NuGet管理库。

- 引入UniTask。

- 引入YooAsset资源管理方案。

- 引入HybridCLR。

## 正在进行的工作

暂无

## 计划

暂无

---

## 引用

- [LuBan][focus-creative-games/luban： luban是一个强大、易用、优雅、稳定的游戏配置解决方案。鲁班是一款功能强大、易用、优雅稳定的游戏配置方案。](https://github.com/focus-creative-games/luban)

- [YooAsset] [tuyoogame/YooAsset: unity3d resources management system](https://github.com/tuyoogame/YooAsset)

- [HybridCLR][focus-creative-games/hybridclr： HybridCLR是一个特性完整、零成本、高性能、低内存的Unity全平台原生c#热更新解决方案。HybridCLR 是一种功能齐全、零成本、高性能、低内存的解决方案，适用于 Unity 的全平台原生 c# 热更新。](https://github.com/focus-creative-games/hybridclr)

- [ZLinq][Cysharp/ZLinq：零分配 LINQ，适用于所有 .NET 平台和 Unity、Godot，具有 LINQ to Span、LINQ to SIMD 和 LINQ to Tree（文件系统、JSON、GameObject 等）。](https://github.com/Cysharp/ZLinq)

- [NuGetForUnity][GlitchEnzo/NuGetForUnity: A NuGet Package Manager for Unity](https://github.com/GlitchEnzo/NuGetForUnity)

- [PrimeTween][KyryloKuzyk/PrimeTween：用于 Unity 的高性能、免分配补间库。在一行代码中创建动画、延迟和序列。](https://github.com/KyryloKuzyk/PrimeTween)

- [UniTask][Cysharp/UniTask: Provides an efficient allocation free async/await integration for Unity.](https://github.com/Cysharp/UniTask)

- [Fantasy] [qq362946/奇幻：C # 游戏框架，但不限于游戏。可用于非游戏业务开发](https://github.com/qq362946/Fantasy)([GitHub - annulusgames/Alchemy: Provides a rich set of editor extensions and serialization extensions for Unity.](https://github.com/annulusgames/Alchemy))**
