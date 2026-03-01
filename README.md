# 框架简介(临时)

目前框架属于非常早期的开发阶段,目前推荐仅学习参考,许多功能还需项目测试验证,且并未补充详细文档,目前请不要直接应用于正式项目！！！

本框架是专门服务于2D与2.5D开发者的高性能高效的OOP开发框架，分层来自于qf框架并对其核心逻辑进行重构，尽量去避免框架本身带来的GC开销，使用更加高效的分层通信方式，大部分扩展工具可以使用类似Unity的包管理器去管理卸载与装载，大部分EU框架内置的工具都会进行多线程优化，减少独立开发者重复造轮子的压力且插件文件文件夹乱飞的情况。

# FramworkCore分支

该分支是进行工具链整合的分支,内容包含:Unity客户端框架、工具的编辑器集成。

## 已经完成的工作

- 引入Luban并附带一个Tools\Luban\Data\GamConfig\Luban配置工具.exe便于配置鲁班路径跟解析方式。

- 引入PrimeTween动画库。

- 引入Zlinq查询库。

- 引入NuGetForUnity集成在Unity内的NuGet管理库。

- 引入UniTask。

- 引入YooAsset资源管理方案。

- 引入HybridCLR。

- 引入Scriban。

- Unity编辑器中的工具管理器(使用UIToolKit可视化进行对工具包的管理<查看、删除>)。

- 基本的拓展模块工具(UI、Audio、ObjectPool、Log、Singleton、FSM、MD文档查看器、资源管理)

## 正在进行的工作


## 计划

- 将框架核心移植到Godot中。

---

## 引用

- [LuBan][focus-creative-games/luban： luban是一个强大、易用、优雅、稳定的游戏配置解决方案。鲁班是一款功能强大、易用、优雅稳定的游戏配置方案。](https://github.com/focus-creative-games/luban)

- [YooAsset] [tuyoogame/YooAsset: unity3d resources management system](https://github.com/tuyoogame/YooAsset)

- [HybridCLR][focus-creative-games/hybridclr： HybridCLR是一个特性完整、零成本、高性能、低内存的Unity全平台原生c#热更新解决方案。HybridCLR 是一种功能齐全、零成本、高性能、低内存的解决方案，适用于 Unity 的全平台原生 c# 热更新。](https://github.com/focus-creative-games/hybridclr)

- [ZLinq][Cysharp/ZLinq：零分配 LINQ，适用于所有 .NET 平台和 Unity、Godot，具有 LINQ to Span、LINQ to SIMD 和 LINQ to Tree（文件系统、JSON、GameObject 等）。](https://github.com/Cysharp/ZLinq)

- [NuGetForUnity][GlitchEnzo/NuGetForUnity: A NuGet Package Manager for Unity](https://github.com/GlitchEnzo/NuGetForUnity)

- [UniTask][Cysharp/UniTask: Provides an efficient allocation free async/await integration for Unity.](https://github.com/Cysharp/UniTask)

- [Scriban] [scriban/scriban：一种快速、强大、安全且轻量级的.NET脚本语言和引擎](https://github.com/scriban/scriban)
## 推荐仓库
- [PrimeTween][KyryloKuzyk/PrimeTween：用于 Unity 的高性能、免分配补间库。在一行代码中创建动画、延迟和序列。](https://github.com/KyryloKuzyk/PrimeTween)
