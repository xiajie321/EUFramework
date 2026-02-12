# ResKit 配置工具使用说明

## 📌 功能概述

ResKit 配置工具是一个基于 Unity UI Toolkit 的编辑器窗口，用于快速配置和生成 EURes 资源管理系统所需的文件。

## 🚀 打开窗口

在 Unity 编辑器菜单栏中：
```
EUFramework → ResKit 配置工具
```

## 🛠️ 功能说明

### 1. 创建配置文件

点击"创建配置文件"按钮，将在 `Assets/EUFarmworker/Resources/ResKitSettings/` 目录下创建：

- **AssetBundleCollectorSetting.asset** - YooAsset 资源收集配置
- **YooAssetSettings.asset** - YooAsset 构建设置
- **ResServerConfig.asset** - 资源服务器配置（IP、端口、版本等）

### 2. 生成 UI Prefab

点击"生成 UI Prefab"按钮，将在 `Assets/EUFarmworker/Extension/EURes/Prefabs/` 目录下创建：

- **ResKitUserOpePopUp.prefab** - 默认的用户操作弹窗预制体

预制体包含：
- Canvas (ScreenSpaceOverlay)
- Panel 背景面板
- Title 标题文本
- Content 内容文本
- BtnConfirm 确认按钮
- BtnCancel 取消按钮

### 3. 生成 ResKit 代码

点击"生成 ResKit 代码"按钮，将根据模板 `DefaultResKit.Generated.sbn` 生成：

- **ResKit.Generated.cs** - 资源管理工具类

生成的代码包含：
- `InitPackageResAsync()` - 初始化资源包
- `GetPackage()` - 获取资源包实例
- `SetDefaultPackage()` - 设置默认资源包
- `IsInitialized()` - 检查初始化状态

## 📁 目录结构

```
Assets/EUFarmworker/Extension/EURes/
├─ Editor/
│  ├─ UI/
│  │  ├─ ResKitEditorWindow.uxml      # UI 布局文件
│  │  └─ ResKitEditorWindow.uss       # 样式表
│  ├─ Templates/
│  │  └─ DefaultResKit.Generated.sbn  # 代码生成模板
│  └─ ResKitEditorWindow.cs           # 窗口逻辑
├─ Script/
│  └─ Generated/
│     └─ ResKit.Generated.cs          # 生成的代码
├─ Prefabs/
│  └─ ResKitUserOpePopUp.prefab       # 生成的预制体
└─ Resources/
   └─ ResKitSettings/                 # 生成的配置文件
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
3. 命名空间已从 `EUFarmworker` 统一修改为 `EUFramework`
4. 生成的代码使用 `partial class`，可以在其他文件中扩展功能

## 🔧 自定义

如需修改生成的代码，请编辑模板文件：
```
Assets/EUFarmworker/Extension/EURes/Editor/Templates/DefaultResKit.Generated.sbn
```

模板使用变量：
- `{{ namespace }}` - 命名空间（默认: EUFramework.Extension.EURes）
- `{{ class_name }}` - 类名（默认: ResKit）

## 📝 更新日志

- 2024.02.12: 初始版本
  - 创建 UI Toolkit 编辑器窗口
  - 实现配置文件、预制体、代码生成功能
  - 修正命名空间拼写错误
