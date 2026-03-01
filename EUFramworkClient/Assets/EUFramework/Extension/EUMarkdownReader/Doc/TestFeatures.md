# Markdown 阅读器功能测试

这是一个用于测试 **EUMarkdownDocManager** 所有功能的测试文档。

## 1. 文本格式测试

这是普通段落文本。

这是 **粗体文本** 测试。
这是 *斜体文本* 测试。
这是 `行内代码` 测试。
这是 ~~删除线文本~~ 测试。
这是 **[粗体链接](https://unity.com)** 测试。
这是混合了 **粗体**、*斜体*、`代码` 和 ~~删除线~~ 的复杂文本。

## 2. 列表测试

### 无序列表
- 项目 1
- 项目 2
  - 子项目 A
  - 子项目 B
- 项目 3

### 有序列表
1. 第一步
2. 第二步
3. 第三步

### 任务列表 (Task List)
- [ ] 待办事项 1
- [x] 已完成事项 2
- [ ] 待办事项 3
- [x] ~~已废弃的任务~~

## 3. 引用块测试

> 这是一个引用块。
> 可以包含多行文本。
> 也可以包含 [链接](https://google.com)。

## 4. 代码块测试

```csharp
// C# 代码示例
public class TestClass : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Hello World");
    }
}
```

```json
{
    "name": "Test",
    "version": "1.0.0"
}
```

## 5. 表格测试 (Table)

| 标题1 | 标题2 | 标题3 |
| :--- | :---: | ---: |
| 左对齐 | 居中对齐 | 右对齐 |
| 内容 A | 内容 B | 内容 C |
| 较长的文本内容 | **加粗内容** | [链接](https://unity.com) |

## 6. 链接跳转测试

### 网页链接
*   [Unity 官网](https://unity.com) (点击应打开浏览器)
*   [百度](https://www.baidu.com)
*   [Google](https://google.com)

### 内部文档链接
*   [查看 README 文档](README.md) (点击应跳转到同目录下的 README.md)
*   [跳转到上一级目录文档](../Doc/README.md) (相对路径测试)

### 锚点跳转 (页内跳转)
*   [跳转到文本格式测试](#1-文本格式测试)
*   [跳转到列表测试](#2-列表测试)
*   [跳转到图片测试](#7-图片显示测试)
*   [回到顶部](#markdown-阅读器功能测试)

## 7. 图片显示测试

### 网络图片
![Unity Logo](https://create.unity.com/hubfs/Unity-2023-Branding/Unity-Logo-White.png)

### 本地图片 (如果存在)
*(注：请确保项目中有相应的图片文件用于测试，以下路径为示例)*

![示例图片](Icon.png) 
*(使用同目录下的 Icon.png)*

### 视频链接测试
*   [Bilibili 视频测试](https://www.bilibili.com/video/BV1C7JazfEEC/?spm_id_from=333.1387.homepage.video_card.click)

## 8. 混合排版测试

这是一段包含 [链接](https://unity.com) 的文本，链接后面还有文字。
这是一段包含 `代码` 和 **粗体** 以及 [链接](https://unity.com) 的复杂文本。

---
*文档结束*
