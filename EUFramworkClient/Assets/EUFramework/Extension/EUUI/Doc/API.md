# EUUI API 文档

## EUUIKit 类

UI 系统的核心静态管理类。

### 初始化

- `static void Initialize()`
  - 初始化 UI 系统（创建 UIRoot, EventSystem, UICamera 等）。建议在游戏启动时调用一次。

### 面板管理 (基础)

- `static UniTask<T> OpenAsync<T>(IEUUIPanelData data = null)`
  - 异步打开面板。**不记录**导航历史。适用于弹窗、提示框等。
  - `data`: 传递给面板的初始化数据。

- `static void Close<T>()`
  - 关闭指定类型的面板。

- `static T GetPanel<T>()`
  - 获取当前已打开的面板实例。如果未打开则返回 `null`。

- `static bool IsPanelOpen<T>()`
  - 检查面板是否处于打开状态。

### 面板导航 (栈管理)

- `static UniTask NavigateToAsync<T>(IEUUIPanelData data = null)`
  - 导航到指定面板。
  - **行为**：将当前栈顶面板隐藏（Hide），打开新面板并压入栈顶。记录导航历史。

- `static UniTask BackAsync(bool showNext = true)`
  - 返回上一个面板。
  - **行为**：关闭（Close）当前栈顶面板，显示（Show）前一个面板。

- `static UniTask BackToAsync<T>()`
  - 返回到指定的历史面板。
  - **行为**：关闭栈顶到目标面板之间的所有面板，直到目标面板成为栈顶并显示。

- `static UniTask ClearHistoryAsync()`
  - 清空导航历史栈（关闭栈中所有面板）。

- `static UniTask OpenExclusiveAsync<T>(IEUUIPanelData data = null)`
  - 独占式打开面板。
  - **行为**：关闭所有其他面板，清空导航历史，仅保留目标面板。适用于切换主场景（如大厅 -> 战斗）。

- `static void CloseAll()`
  - 关闭所有面板并清空历史。

- `static void CloseAllExcept<T>()`
  - 关闭除指定类型外的所有面板。

### 资源加载 (需扩展支持)

以下方法依赖于生成的资源加载扩展（如 `EUUIKit.EURes.Generated.cs`）：

- `static UniTask<GameObject> LoadUIPrefabAsync(string packageName, string panelName, bool isRemote = false)`
- `static SpriteAtlas LoadAtlas(string atlasName, bool isRemote = false)`

## EUUIPanelBase<T> 类

所有 UI 面板的基类。继承自 `MonoBehaviour`。

### 核心属性

- `string PackageName`: 包名（抽象，需实现）。
- `string PanelName`: 面板名（抽象，需实现）。
- `EUUILayerEnum DefaultLayer`: 默认层级（Normal, Popup, Top, System）。
- `IEUUIPanelData uiPanelData`: 打开面板时传递的数据。

### 生命周期方法 (可重写)

- `bool OnCanOpen()`: 是否可以打开。返回 `false` 则阻止打开。
- `void OnOpen()`: 面板打开时调用。**在此处进行初始化、事件注册、数据读取**。
- `void OnShow()`: 面板从隐藏状态变为显示状态时调用。
- `void OnHide()`: 面板从显示状态变为隐藏状态时调用。
- `void OnClose()`: 面板关闭时调用。**在此处清理资源、取消订阅**。

### UI 交互辅助方法 (Helper)

基类提供了便捷的方法来绑定 UI 事件，自动管理生命周期（面板关闭时自动解绑）。

#### 点击事件
- `void AddClick(Button button, Action action)`
- `void AddClick(GameObject go, Action action)`
- `void AddClick<T>(Button button, T param, Action<T> action)`

#### 长按事件
- `void AddLongPressRepeat(GameObject go, Action onRepeat, float interval = 0.1f, float delay = 0.3f)`
  - 长按连点（按住持续触发）。
- `void AddLongPressHold(GameObject go, Action onHold, float holdTime = 0.5f, Action<float> onProgress = null)`
  - 长按触发（按住一定时间后触发一次），支持进度回调。

#### 拖拽事件
- `void AddDrag(GameObject handle, Transform target = null)`
  - 简单的拖拽移动物体。
- `void AddDragSource<T>(GameObject go, T data, float ghostAlpha, Action<T> onBegin, Action<T> onEnd)`
  - 拖拽源（支持生成幻影）。
- `void AddDropTarget<T>(GameObject go, Action<T> onDrop)`
  - 接收拖拽释放。

### 资源加载辅助 (需扩展支持)

以下方法依赖于生成的 `EUUIPanelBase` 扩展：

- `void SetImage(Image img, string url)`: 设置图片（格式：atlasName/spriteName）。
- `Sprite LoadSprite(string url)`: 加载 Sprite。
- `UniTask<GameObject> LoadPrefabAsync(string path)`: 加载 Prefab。

## 组件

### EUUIPanelDescription
挂载在 UI Prefab 根节点，用于描述面板元数据。
- `string PackageName`
- `PanelType PanelType`
- `string Namespace`

### EUUINodeBind
挂载在 UI 节点上，用于代码生成器自动绑定字段。
- `ComponentType ComponentType`: 绑定的组件类型（Button, Text, Image 等）。
- `string MemberName`: 生成代码中的变量名。

## 配置

### EUUIKitConfig (Resources)
运行时配置，位于 `Resources/EUUIKitConfig.asset`。
- `Vector2 referenceResolution`: 设计分辨率。
- `float matchWidthOrHeight`: 适配模式。
- `string builtinPrefabPath`: 内置 Prefab 路径前缀。
- `string remotePrefabPath`: 远程 Prefab 路径前缀。

### EUUITemplateConfig (Editor)
编辑器配置，位于 `Assets/EUFramework/Extension/EUUI/Editor/EditorSO/`。
- `string namespace`: 生成代码的默认命名空间。
- `bool useArchitecture`: 是否集成 MVC 架构。
- `string architectureName`: 架构类名。
