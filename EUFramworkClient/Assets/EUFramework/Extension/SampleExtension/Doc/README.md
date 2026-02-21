# 示例拓展工具 (Sample Extension)

## 概述

这是一个用于验证 EU 拓展管理器功能的测试插件示例。它展示了标准 EU 扩展的目录结构和配置方式。

## 目录结构

一个标准的 EU 扩展应包含以下目录：

- `extension.json`: 扩展元数据配置文件
- `Doc/`: 文档目录，建议包含 README.md, API.md, Update.md
- `Editor/`: 编辑器扩展代码（如 Window, Inspector）
- `Script/`: 运行时核心逻辑代码
- `Example/`: 示例场景和脚本

## 快速开始

复制本目录并重命名，修改 `extension.json` 中的信息，即可创建你自己的扩展。

## 文档说明

- **API文档**：请查阅 [API.md](API.md) 获取详细的接口说明。
- **更新日志**：请查阅 [Update.md](Update.md) 获取版本更新历史。
