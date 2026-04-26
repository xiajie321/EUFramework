# Templates

EUUI 代码生成模板资产及导出逻辑。

```
Templates/
├── Sbn/                  Scriban (.sbn) 模板文件，按类型分类
│   ├── Static/           静态扩展模板（生成后内容固定，不依赖面板数据）
│   │   ├── UIKit/        EUUIKit 分部类扩展（EURes 加载、InputController 集成等）
│   │   └── PanelBase/    EUUIPanelBase 分部类扩展（Sprite 加载等）
│   └── WithData/         数据驱动模板（需要面板名、命名空间等运行时数据）
│       ├── PanelGenerated.sbn     面板生成代码模板
│       └── MVCArchitecture.sbn    MVC 架构模板
└── ExportsCS/            C# 导出器，负责读取模板、渲染输出、绑定字段并保存 Prefab
    ├── EUUIBaseExporter.cs          基础导出器：模板路径解析、Scriban 渲染、文件写入
    ├── EUUIPanelExporter.cs         自动绑定导出的流程编排入口
    ├── EUUIPanelCodeGenerator.cs    面板 Generated / 业务逻辑 / IController 代码生成
    ├── EUUIPrefabBinder.cs          编译后反射绑定字段到 UIRoot 组件
    └── EUUIPrefabExporter.cs        保存 Prefab 并移除编辑器用 EUUINodeBind
```

更多 `.sbn` 用户扩展写法、命名规则和 sidecar JSON 格式见 `Sbn/README.md`。

**sidecar JSON 规范**

每个 `.sbn` 文件旁可放同名 `.json`，声明该模板生成后需要向 `EUUI.asmdef` 添加的程序集引用：

```json
{
    "requiredAssemblies": ["EURes", "YooAsset"],
    "editorAssemblies":   [],
    "namespaceVariables": [
        { "key": "eu_res_namespace", "value": "EURes" }
    ]
}
```
