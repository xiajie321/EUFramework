# Remote 目录

## 📦 用途
存放**远程热更新资源**，可以通过服务器动态下载和更新。

## 🎯 适用场景
- **主机模式** (HostPlayMode)
- **Web 模式** (WebPlayMode)
- 需要热更新的游戏内容
- 频繁变化的运营资源

## 📋 推荐内容
- 游戏关卡、场景资源
- UI 界面（非核心框架）
- 角色、特效、音效资源
- 配置表和数据文件
- 运营活动相关资源
- 所有 Shader（启用 AutoCollectShaders）

## ⚠️ 注意事项
- 资源会**上传到资源服务器**
- 可以**不更新应用**的情况下更新内容
- 首次运行需要**联网下载**
- 建议资源按功能模块划分，便于按需下载
- 大文件建议分包管理

## 🔧 YooAsset 设置
- **Package Name**: Remote
- **Play Mode**: HostPlayMode / WebPlayMode
- **Auto Collect Shaders**: true（收集所有 Shader）
- **Enable Addressable**: true（支持资源寻址）
- **Directory**: Assets/EUResources/Remote

## 🌐 热更新流程
1. 检查资源版本
2. 下载更新的资源
3. 验证资源完整性
4. 应用新资源
