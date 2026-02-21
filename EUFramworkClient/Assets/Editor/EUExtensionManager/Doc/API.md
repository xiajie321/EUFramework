# EU 拓展管理器 API 文档

## 配置文件 (extension.json)

每个扩展必须在根目录包含 `extension.json` 文件。

### JSON 格式示例

```json
{
  "name": "com.yourname.extension-name",
  "displayName": "拓展显示名称",
  "version": "1.0.0",
  "description": "拓展描述",
  "author": "作者名称",
  "category": "分类名称",
  "dependencies": [
    {
      "name": "com.eu.core",
      "gitUrl": "https://github.com/user/repo",
      "installPath": "Assets/EUFramework/Core",
      "version": "1.0.0"
    }
  ]
}
```

### 字段说明

| 字段 | 类型 | 必需 | 说明 |
| --- | --- | --- | --- |
| name | string | 是 | 唯一标识符，建议使用反向域名格式 |
| displayName | string | 是 | 显示在列表中的名称 |
| version | string | 是 | 版本号，格式：主版本.次版本.修订版本 |
| description | string | 是 | 简短描述 |
| author | string | 否 | 作者名称 |
| category | string | 否 | 分类。若设为 "框架" 或 "Core"，默认安装到核心路径 |
| downloadUrl | string | 否 | 下载链接 (通常由仓库自动生成) |
| sourceUrl | string | 否 | 源码链接 |
| dependencies | array | 否 | 依赖列表 |

### 依赖项对象字段

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| name | string | 依赖项的包名 (必需) |
| gitUrl | string | 依赖项所在的 GitHub 仓库地址 (可选，用于直接下载) |
| version | string | 最低版本要求 (可选) |
| installPath | string | 指定安装路径 (可选) |

## 核心类

### EUExtensionInfo

`EUFramework.Editor.EUExtensionInfo`

表示一个扩展的元数据信息。

#### 属性
- `string Name`: 扩展唯一标识符
- `string DisplayName`: 显示名称
- `string Version`: 版本号
- `string Description`: 描述
- `string Author`: 作者
- `string Category`: 分类
- `string FolderPath`: 本地文件夹路径
- `bool IsInstalled`: 是否已安装

### EUExtensionLoader

`EUFramework.Editor.EUExtensionLoader`

负责加载和管理扩展信息。

#### 方法
- `static void RefreshExtensions()`: 刷新本地扩展列表
- `static EUExtensionInfo GetExtension(string name)`: 获取指定名称的扩展信息
- `static List<EUExtensionInfo> GetAllExtensions()`: 获取所有已安装的扩展
