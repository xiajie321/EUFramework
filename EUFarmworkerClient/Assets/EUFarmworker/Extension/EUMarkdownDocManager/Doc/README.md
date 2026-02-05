# Markdown文档管理器

## 概述

Markdown文档管理器是一个基于UIToolkit的文档阅读工具，用于统一管理和浏览EUFarmworker框架中所有扩展的文档。

## 功能特性

- 自动扫描所有已安装扩展的Doc文件夹
- 树形结构展示文档层级
- 实时渲染Markdown内容
- 支持标题、段落、列表、代码块等常见Markdown语法
- 深色主题界面，与Unity编辑器风格一致

## 使用方法

### 打开文档阅读器

在Unity编辑器菜单栏中选择：

```
EUFarmworker -> 文档阅读器
```

### 浏览文档

1. 窗口左侧显示文档树，按扩展分组
2. 点击扩展名称可展开/折叠该扩展的文档列表
3. 点击具体的文档文件名，右侧将显示文档内容
4. 使用顶部的"刷新"按钮可重新扫描所有文档

### 文档组织规范

为了让文档能被正确识别和显示，请遵循以下规范：

1. 在扩展根目录下创建`Doc`文件夹
2. 将Markdown文档（.md文件）放入Doc文件夹
3. 可以在Doc文件夹下创建子文件夹来组织文档
4. 建议至少包含一个README.md作为主文档

### 目录结构示例

```
YourExtension/
├── extension.json
├── Doc/
│   ├── README.md          # 主文档
│   ├── QuickStart.md      # 快速开始
│   ├── API/               # API文档目录
│   │   ├── Core.md
│   │   └── Utils.md
│   └── Examples/          # 示例文档目录
│       └── BasicUsage.md
├── Script/
└── ConfigPanel/
```

## 支持的Markdown语法

### 标题

```markdown
# 一级标题
## 二级标题
### 三级标题
#### 四级标题
##### 五级标题
###### 六级标题
```

### 段落

普通文本段落，支持自动换行。

### 列表

无序列表：
```markdown
- 项目1
- 项目2
- 项目3
```

有序列表：
```markdown
1. 第一项
2. 第二项
3. 第三项
```

### 代码块

使用三个反引号包裹代码：

````markdown
```csharp
public class Example
{
    public void Method()
    {
        Debug.Log("Hello World");
    }
}
```
````

支持指定语言类型，如：csharp、javascript、python等。

### 行内代码

使用单个反引号包裹：`代码`

### 粗体和斜体

- **粗体文本**
- *斜体文本*

### 链接

```markdown
[链接文本](URL)
```

注意：当前版本链接会显示为普通文本。

## 技术实现

### 核心组件

- **EUMarkdownDocReaderWindow**: 主窗口类，负责UI构建和文档管理
- **DocNode**: 文档节点数据结构，用于树形展示
- **Markdown渲染器**: 解析和渲染Markdown内容

### 文档扫描机制

1. 通过EUExtensionLoader获取所有已安装的扩展
2. 遍历每个扩展的folderPath，查找Doc子文件夹
3. 递归扫描Doc文件夹下的所有.md文件和子文件夹
4. 构建树形数据结构用于显示

### 样式系统

使用USS（Unity Style Sheets）定义界面样式，样式文件位于：
```
ConfigPanel/EUMarkdownDocReader.uss
```

## 注意事项

1. 文档文件必须使用UTF-8编码
2. 文件名不要包含特殊字符
3. 大型文档可能需要一定加载时间
4. 当前版本不支持图片显示
5. 不支持表格语法
6. 不支持HTML标签

## 扩展开发建议

为你的扩展编写文档时，建议包含以下内容：

1. **README.md**: 扩展概述、功能介绍、快速开始
2. **API文档**: 详细的API说明和参数描述
3. **使用示例**: 常见使用场景的代码示例
4. **配置说明**: 如果有配置项，详细说明每个配置的作用
5. **常见问题**: FAQ和问题排查指南

## 更新日志

### v1.0.0 (2026-02-05)

- 初始版本发布
- 支持基础Markdown语法渲染
- 实现文档树形浏览
- 集成EUExtensionManager扫描机制

## 技术支持

如遇到问题或有改进建议，请通过以下方式反馈：

- 在EUFarmworker社区仓库提交Issue
- 联系框架维护者

## 许可证

本扩展遵循EUFarmworker框架的许可证协议。
