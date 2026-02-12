# ResKit 配置工具使用说明

## 📌 功能概述

ResKit 配置工具是一个基于 Unity UI Toolkit 的编辑器窗口，用于快速配置和生成 EURes 资源管理系统所需的文件。

## 🚀 打开窗口

在 Unity 编辑器菜单栏中：
```
EUFramework → 拓展 → ResKit 配置工具
```

## 🛠️ 功能说明

### 1. 配置文件状态

点击"配置文件状态"按钮，智能检查配置文件是否存在：

**自动检测功能：**
- ✓ **已创建**：显示"配置 XX 文件"按钮，可直接跳转配置
- ✗ **未创建**：显示"创建 XX 文件"按钮，单独创建该配置文件

**配置文件列表：**
- **AssetBundleCollectorSetting.asset** - YooAsset 资源收集配置
  - 已创建：点击"配置资源收集"打开 YooAsset 窗口
  - 未创建：点击"创建配置文件"立即创建
- **ResServerConfig.asset** - 资源服务器配置
  - 已创建：点击"配置服务器信息"进入编辑界面
  - 未创建：点击"创建配置文件"立即创建
- **YooAssetSettings.asset** - YooAsset 全局设置
  - 已创建：点击"配置 YooAsset 设置"进入编辑界面
  - 未创建：点击"创建配置文件"立即创建

配置文件路径：`Assets/EUFramework/Resources/ResKitSettings/`

### 2. 编辑配置

点击"编辑配置"按钮（或在状态页点击"配置服务器信息"），进入配置编辑界面：

**AssetBundle 收集配置管理：**
- 显示当前配置文件路径
- 一键打开 YooAsset 配置窗口进行详细设置

**ResServerConfig 配置编辑：**
- 协议类型：HTTP / HTTPS / Custom（自定义完整URL）
- 服务器地址：IP 或域名
- 端口号：1-65535（滑动条）
- 应用版本：版本号字符串
- 实时显示完整服务器地址预览
- 修改后自动保存

**YooAssetSettings 配置编辑：**
- YooAsset 文件夹名称：缓存和资源目录名称（默认 "yoo"）
- 资源清单前缀：多包配置时使用的前缀名称
- 修改后自动保存

### 3. 生成 UI Prefab

点击"生成 UI Prefab"按钮，将在 `Assets/EUFramework/Extension/EURes/Prefabs/` 目录下创建：

- **ResKitUserOpePopUp.prefab** - 默认的用户操作弹窗预制体

预制体包含：
- Canvas (ScreenSpaceOverlay)
- Panel 背景面板
- Title 标题文本
- Content 内容文本
- BtnConfirm 确认按钮
- BtnCancel 取消按钮

### 4. 生成 ResKit 代码

点击"生成 ResKit 代码"按钮，将根据模板 `DefaultResKit.Generated.sbn` 生成：

- **ResKit.Generated.cs** - 资源管理工具类

生成的代码包含：
- `InitPackageResAsync()` - 初始化资源包
- `GetPackage()` - 获取资源包实例
- `SetDefaultPackage()` - 设置默认资源包
- `IsInitialized()` - 检查初始化状态

## 📁 目录结构

```
Assets/EUFramework/
├─ Extension/EURes/
│  ├─ Editor/
│  │  ├─ UI/
│  │  │  ├─ ResKitEditorWindow.uxml      # UI 布局文件
│  │  │  └─ ResKitEditorWindow.uss       # 样式表
│  │  ├─ Templates/
│  │  │  └─ DefaultResKit.Generated.sbn  # 代码生成模板
│  │  └─ ResKitEditorWindow.cs           # 窗口逻辑
│  ├─ Script/
│  │  └─ Generated/
│  │     └─ ResKit.Generated.cs          # 生成的代码
│  └─ Prefabs/
│     └─ ResKitUserOpePopUp.prefab       # 生成的预制体
└─ Resources/
   └─ ResKitSettings/                    # 生成的配置文件
      ├─ AssetBundleCollectorSetting.asset
      ├─ ResServerConfig.asset
      └─ YooAssetSettings.asset
```

## 💡 使用示例

### 初始化资源系统

```csharp
using EUFramework.Extension.EURes;
using UnityEngine;

public class GameLauncher : MonoBehaviour
{
    async void Start()
    {
        // 初始化资源包
        bool success = await ResKit.InitPackageResAsync("DefaultPackage", EPlayMode.HostPlayMode);
        
        if (success)
        {
            Debug.Log("资源系统初始化成功！");
            
            // 获取资源包
            var package = ResKit.GetPackage();
            
            // 使用资源包加载资源
            var handle = package.LoadAssetAsync<GameObject>("Assets/Prefabs/UI/ResKitUserOpePopUp.prefab");
            await handle.ToUniTask();
            
            var prefab = handle.AssetObject as GameObject;
            Instantiate(prefab);
        }
    }
}
```

## ⚠️ 注意事项

1. 生成文件前会检查是否已存在，避免覆盖已配置的文件
2. 所有生成的文件都会在 Unity Project 窗口中自动选中和高亮显示
3. 命名空间统一为 `EUFramework`，文件夹路径也已修正
4. 生成的代码使用 `partial class`，可以在其他文件中扩展功能

## 🔧 自定义

如需修改生成的代码，请编辑模板文件：
```
Assets/EUFramework/Extension/EURes/Editor/Templates/DefaultResKit.Generated.sbn
```

模板使用变量：
- `{{ namespace }}` - 命名空间（默认: EUFramework.Extension.EURes）
- `{{ class_name }}` - 类名（默认: ResKit）

## 📝 更新日志

- 2026.02.12: v1.2 智能化改进
  - 新增"配置文件状态"功能，智能检查文件是否存在
  - 根据文件状态动态显示"创建"或"配置"按钮
  - 支持单独创建/配置每个配置文件
  - 移除自动打开 YooAsset 窗口，改为用户主动选择
  - 优化用户体验，更加直观易用

- 2026.02.12: v1.1 功能增强
  - 新增"管理配置"功能，支持窗口内直接编辑 ResServerConfig
  - 移除 YooAssetSettings 创建（YooAsset 2.x 在 AssetBundleCollector 中管理）
  - 统一配置文件路径到 Resources/ResKitSettings
  - 菜单移至"拓展"子菜单下

- 2024.02.12: v1.0 初始版本
  - 创建 UI Toolkit 编辑器窗口
  - 实现配置文件、预制体、代码生成功能
  - 修正命名空间拼写错误
