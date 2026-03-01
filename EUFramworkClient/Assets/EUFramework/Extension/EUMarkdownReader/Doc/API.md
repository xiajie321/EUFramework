# Markdown文档管理器 API 文档

## 核心组件

### EUMarkdownDocReaderWindow

主窗口类，负责UI构建和文档管理。

- **命名空间**: `EUFramework.Extension.EUMarkdownDocManager.Editor`
- **继承**: `EditorWindow`

#### 主要职责
- 初始化 UI 界面 (VisualElement 构建)
- 处理文档树的构建和渲染
- 处理 Markdown 内容的解析和显示
- 响应用户交互 (点击、搜索、刷新)

### DocNode

文档节点数据结构，用于树形展示。

#### 属性
- `string Name`: 节点名称 (文件名或文件夹名)
- `string Path`: 完整路径
- `bool IsFolder`: 是否为文件夹
- `List<DocNode> Children`: 子节点列表

### Markdown 渲染器

负责解析 Markdown 文本并将其转换为 UIToolkit 的 VisualElement 树。

#### 支持特性
- **流式加载**: 大型文档分帧解析，避免阻塞主线程
- **异步渲染**: 使用 `EditorApplication.update` 进行分时渲染
- **样式隔离**: 使用 USS 类名隔离不同 Markdown 元素的样式

## 系统机制

### 文档扫描机制

1. **获取扩展列表**: 通过 `EUExtensionLoader` 获取所有已安装的扩展信息。
2. **定位文档目录**: 遍历每个扩展的根目录，查找名为 `Doc` 的子文件夹。
3. **递归扫描**: 递归扫描 `Doc` 文件夹下的所有 `.md` 文件和子文件夹。
4. **构建树**: 将扫描结果构建为 `DocNode` 树形结构，用于 UI 的 `TreeView` 显示。

### 样式系统

使用 USS (Unity Style Sheets) 定义界面样式。

- **文件位置**: `Editor/EUMarkdownDocReader.uss`
- **主要样式类**:
    - `.markdown-h1` ~ `.markdown-h6`: 标题样式
    - `.markdown-paragraph`: 段落样式
    - `.markdown-code-block`: 代码块样式
    - `.markdown-table`: 表格样式
    - `.markdown-blockquote`: 引用块样式

## 扩展开发接口

目前本模块主要作为编辑器工具使用，暂未开放运行时 API。
若需扩展 Markdown 渲染能力，可修改 `EUMarkdownDocReaderWindow.cs` 中的解析逻辑。
