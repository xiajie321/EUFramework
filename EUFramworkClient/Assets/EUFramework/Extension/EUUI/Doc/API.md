# EUUI API 文档

## EUUIKit 类

UI 系统的核心管理类。

### 方法

#### 初始化
- `static void Initialize()`: 初始化 UI 系统。

#### 面板管理
- `static UniTask<T> OpenAsync<T>(UIOpenData openData = null)`: 异步打开面板。
- `static UniTask<T> NavigateToAsync<T>(UIOpenData openData = null)`: 导航到面板（记录历史）。
- `static void Close<T>()`: 关闭面板。
- `static T GetPanel<T>()`: 获取已打开的面板实例。

#### 资源加载（需扩展支持）
- `static UniTask<GameObject> LoadUIPrefabAsync(string packageName, string panelName, bool isRemote = false)`: 加载 UI Prefab。
- `static SpriteAtlas LoadAtlas(string atlasName, bool isRemote = false)`: 加载图集。

## EUUIPanelBase<T> 类

所有 UI 面板的基类。

### 需实现属性
- `string PackageName`: 包名。
- `string PanelName`: 面板名。

### 生命周期方法 (Override)
- `bool OnCanOpen()`: 是否可以打开。
- `void OnOpen()`: 面板打开时调用（初始化）。
- `void OnShow()`: 面板显示时调用。
- `void OnHide()`: 面板隐藏时调用。
- `void OnClose()`: 面板关闭时调用（清理）。

### 扩展方法 (需 EURes 支持)
- `void SetImage(Image img, string url)`: 设置图片（格式：atlasName/spriteName）。
- `Sprite LoadSprite(string url)`: 加载 Sprite。
- `UniTask<GameObject> LoadPrefabAsync(string path)`: 加载 Prefab。

## 组件

### EUUIPanelDescription
挂载在 UI 根节点，描述面板信息。
- `string PackageName`
- `PanelType PanelType`
- `string Namespace`

### EUUINodeBind
挂载在 UI 节点，用于自动绑定代码。
- `ComponentType ComponentType`
- `string MemberName`

## 配置

### EUUIKitConfig (Resources)
- `Vector2 referenceResolution`
- `float matchWidthOrHeight`
- `string builtinPrefabPath`
- `string remotePrefabPath`
- `string builtinAtlasPath`
- `string remoteAtlasPath`

### EUUITemplateConfig (Editor)
- `string namespace`
- `bool useArchitecture`
- `string architectureName`
- `string architectureNamespace`
