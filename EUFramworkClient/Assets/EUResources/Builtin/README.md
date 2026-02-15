# Builtin 目录

## 📦 用途
存放**内置资源**，这些资源会直接打包到应用程序中。

## 🎯 适用场景
- **编辑器模拟模式** (EditorSimulateMode)
- **离线模式** (OfflinePlayMode)
- **必须随应用一起发布的核心资源**

## 📋 推荐内容
- 启动 Logo、Splash 界面
- 核心 UI 框架和基础界面
- 必需的配置文件
- 启动流程所需的关键资源

## ⚠️ 注意事项
- 内置资源会**增加应用包体大小**
- 一旦发布，**无法通过热更新修改**
- 建议只放置启动必需的最小资源集
- 资源更新需要重新发布应用

## 🔧 YooAsset 设置
- **Package Name**: Builtin
- **Play Mode**: OfflinePlayMode / EditorSimulateMode
- **Directory**: Assets/EUResources/Builtin
