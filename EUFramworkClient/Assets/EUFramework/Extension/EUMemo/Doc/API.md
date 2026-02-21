# EU Memo API 文档

## 核心类

### EUMemoWindow

`EUFramework.Extension.EUMemo.Editor.EUMemoWindow`

备忘录主窗口类，继承自 `EditorWindow`。

#### 功能
- 负责 UI 的绘制和交互。
- 管理备忘录数据的加载和保存。

### EUMemoData

`EUFramework.Extension.EUMemo.Editor.EUMemoData`

备忘录数据模型类。

#### 属性
- `string id`: 唯一标识符。
- `string title`: 标题。
- `string content`: 内容。
- `string createTime`: 创建时间。
- `string updateTime`: 更新时间。

## 数据存储

数据以 JSON 格式存储在 `Application.persistentDataPath/EUMemo/memos.json` 文件中。
这种存储方式确保了数据不会被版本控制系统（如 Git）追踪，适合存储个人备忘信息。
