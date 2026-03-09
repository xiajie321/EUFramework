# EUUI 框架使用手册

> 基于 Unity 的 UI 管理框架，支持单人 / 多人同屏，集成新 Input System 导航，提供 OSA 列表接入方案。

---

## 目录

1. [快速入门](#快速入门)
2. [资源制作](#资源制作)
   - [UI 场景制作流程](#ui-场景制作流程)
   - [多人同屏 Area 创建](#多人同屏-area-创建)
   - [Prefab 定位](#prefab-定位)
3. [代码使用](#代码使用)
   - [初始化](#初始化)
   - [单人面板管理](#单人面板管理)
   - [面板栈导航](#面板栈导航)
   - [面板状态查询](#面板状态查询)
   - [多人模式](#多人模式)
   - [输入导航](#输入导航)
   - [OSA 列表导航](#osa-列表导航)
4. [面板基类](#面板基类)
5. [完整使用示例](#完整使用示例)

---

## 快速入门

### 1. 初始化框架

在游戏启动脚本（GameBoot）中初始化：

```csharp
void Start()
{
    EUUIKit.Initialize(this.gameObject);
    await EUUIKit.OpenAsync<WndMain>();
}
```

### 2. 创建面板

在 EUUI 配置工具中（菜单：`EUFramework → 拓展 → EUUI 配置工具`）：

1. **资源制作** 标签页 → 在 CreateUI 场景下制作 UI
2. **自动绑定导出** → 生成绑定代码（`WndXxx.Generated.cs`）
3. 在 `WndXxx.cs` 中编写业务逻辑

### 3. 打开面板

```csharp
await EUUIKit.OpenAsync<WndMain>();
```

---

## 资源制作

### UI 场景制作流程

EUUI 使用独立的 CreateUI 场景制作 UI 资源，**不在游戏场景中直接制作 UI**。

**流程：**

```
1. 在 CreateUIScenes 目录下创建/编辑 UI 场景
2. 在场景中搭建面板节点（根节点命名为面板名，如 WndLoading）
3. 在 EUUI 配置工具 → 资源制作 → 选择对应面板
4. 点击「自动绑定导出」，生成绑定代码和 Prefab
5. 在生成的 WndXxx.cs 中编写业务逻辑
```

**注意：**
- 修改 UI 布局应在 CreateUI 场景中修改，**不要直接修改 Prefab**
- 添加新节点后需重新「自动绑定导出」刷新绑定

---

### 多人同屏 Area 创建

多人模式下每个玩家拥有独立的屏幕区域（Area），用于限定该玩家 UI 的显示范围。

在 EUUI 配置工具 → **创建 Area** 中配置：

| 参数 | 说明 |
|---|---|
| 玩家数量 | 分屏数量（2～4） |
| 布局模式 | Linear（线性）/ Grid（网格） |
| 分割轴向 | X轴（左右分屏）/ Y轴（上下分屏） |

**Area 锚点设计原则：**

- Area 使用**拉伸（Stretch）锚点**，随父节点自适应分辨率
- 参考分辨率在 EUUIEditorConfig 中统一管理，Area 不修改 CanvasScaler
- 创建时自动计算比例区域，切换分辨率后自动适配

---

### Prefab 定位

在 EUUI 配置工具 → **定位 Prefab** 中，快速在 Project 窗口中定位当前面板对应的 Prefab 资源。

---

## 代码使用

### 初始化

```csharp
// 游戏启动时调用一次，自动创建 UIRoot / UICamera / EventSystem
EUUIKit.Initialize(gameObject);
```

---

### 单人面板管理

| 方法 | 说明 |
|---|---|
| `OpenAsync<T>(data?)` | 打开面板（不记录历史，适合弹窗/提示框） |
| `Close<T>()` | 关闭指定面板 |
| `CloseAll()` | 关闭所有面板 |
| `CloseAllExcept<T>()` | 关闭除指定面板外的所有面板 |
| `OpenExclusiveAsync<T>(data?)` | 独占式打开（关闭其他所有面板，适合流程切换） |
| `GetPanel<T>()` | 获取当前激活的面板实例 |

```csharp
// 打开弹窗
await EUUIKit.OpenAsync<WndDialog>();

// 关闭
EUUIKit.Close<WndDialog>();

// 独占式打开（如从大厅进入战斗）
await EUUIKit.OpenExclusiveAsync<WndBattle>();
```

---

### 面板栈导航

适用于主流程页面，支持返回上一页。

```csharp
await EUUIKit.NavigateToAsync<T>();    // 导航到新面板（隐藏当前，记录历史）
await EUUIKit.BackAsync();             // 返回上一个面板
await EUUIKit.BackToAsync<T>();        // 返回到指定面板（关闭中间所有）
await EUUIKit.ClearHistoryAsync();     // 清空历史栈
```

---

### 面板状态查询

```csharp
EUUIKit.IsPanelOpen<T>()          // 面板是否已打开
EUUIKit.IsPanelInStack<T>()       // 面板是否在导航栈中
EUUIKit.IsPanelCached<T>()        // 面板是否在 LRU 缓存中
EUUIKit.GetCurrentPanelName()     // 当前栈顶面板名称
EUUIKit.GetHistoryCount()         // 导航历史深度
EUUIKit.GetCachedPanelCount()     // 当前 LRU 缓存数量
```

---

### 多人模式

#### 进入多人模式

```csharp
// 进入多人（禁用单人 EventSystem，按布局切分屏幕）
EUUIKit.EnterMultiplayerMode(playerCount, layout, axis?);

// 分配设备（键鼠/手柄自动分配到对应玩家槽位）
EUUIKit.AssignDefaultDevices();

// 为每个玩家打开面板
EUUIKit.OpenForPlayerAsync<WndGame>(0).Forget();
EUUIKit.OpenForPlayerAsync<WndGame>(1).Forget();
```

#### 布局模式

| 枚举 | 说明 |
|---|---|
| `MultiplayerLayoutMode.Linear` | 线性分割（条状） |
| `MultiplayerLayoutMode.Grid` | 网格分割 |

| 枚举 | 说明 |
|---|---|
| `MultiplayerLayoutAxis.X` | 沿 X 轴分割（左右分屏） |
| `MultiplayerLayoutAxis.Y` | 沿 Y 轴分割（上下分屏） |

#### 切换与退出

```csharp
EUUIKit.SetMultiplayerLayout(playerCount, layout, axis?);   // 仅切换布局
EUUIKit.ExitMultiplayerMode();                              // 退出多人，恢复单人
EUUIKit.IsMultiplayerMode                                   // 当前是否多人模式
```

#### 多人面板管理

```csharp
await EUUIKit.OpenForPlayerAsync<T>(playerIndex, data?)     // 为玩家打开面板
EUUIKit.CloseForPlayer<T>(playerIndex)                      // 关闭玩家面板
EUUIKit.CloseAllForPlayer(playerIndex)                      // 关闭玩家所有面板

await EUUIKit.NavigateForPlayerAsync<T>(playerIndex, data?) // 玩家面板栈导航
await EUUIKit.BackForPlayerAsync(playerIndex)               // 玩家返回上一面板
await EUUIKit.BackForPlayerToAsync<T>(playerIndex)          // 玩家返回到指定面板
await EUUIKit.ClearPlayerHistoryAsync(playerIndex)          // 清空玩家历史栈
```

#### 多人状态查询

```csharp
EUUIKit.GetMultiplayerRoot(index)           // 获取玩家根节点
EUUIKit.GetMultiplayerEventSystem(index)    // 获取玩家 EventSystem
EUUIKit.GetMultiplayerPlayerAsset(index)    // 获取玩家 InputActionAsset
EUUIKit.GetPlayerLayer(playerIndex, layer)  // 获取玩家指定 UI 层级
EUUIKit.GetPlayerPanel<T>(playerIndex)      // 获取玩家当前面板实例
EUUIKit.IsPanelOpenForPlayer<T>(index)      // 玩家面板是否已打开
EUUIKit.GetPlayerHistoryCount(index)        // 玩家历史栈深度
EUUIKit.GetPlayerCurrentPanelName(index)    // 玩家当前栈顶面板名
EUUIKit.MultiplayerPlayerCount             // 当前玩家数量
```

---

### 输入导航

#### 设备分配模式

在 EUUIKitConfig 中可选择两种多人输入模式：

| 模式 | 说明 |
|---|---|
| `MultiplayerUIEvent` | 使用 Resources 中的独立 InputActionAsset，由 EUUI 管理设备分配 |
| `InputControllerAsset` | 直接使用 EUInputController 的 InputActionAsset，设备由 EUInputController 管理 |

#### 导航焦点

**单人 / 多人统一写法（推荐）：**

```csharp
// 面板内使用 OwnerEventSystem，自动判断单人/多人
this.OwnerEventSystem.SetSelectedGameObject(Btn_1.gameObject);
```

**多人专用：**

```csharp
EUUIKit.SetPlayerSelection(playerIndex, selectable);  // 为玩家设置焦点
EUUIKit.ClearPlayerSelection(playerIndex);            // 清除玩家焦点
```

#### 面板默认焦点

重写 `GetDefaultSelectable()` 设置面板打开时的默认焦点：

```csharp
protected override Selectable GetDefaultSelectable()
{
    return Btn_Start;
}
```

---

### OSA 列表导航

EUUI 通过 `EUOSAInputBridge` 组件将 OSA 虚拟化列表接入 Unity 新 Input System 导航。

#### 工作原理

```
手柄/键盘输入
    ↓
InputSystemUIInputModule（Navigate / Submit / Cancel Action）
    ↓
EventSystem 派发到 currentSelectedGameObject
    ↓
EUOSAInputBridge（IMoveHandler / ISubmitHandler / ICancelHandler）
    ↓
移动 OSA Index / 触发回调
```

#### 获取 Bridge

通过 Adapter 的 `InputBridge` 属性获取，**未挂载时自动添加**：

```csharp
var bridge = myListAdapter.InputBridge;
```

#### Inspector 导航参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Loop At Extremity | false | 导航到首/尾时是否循环，否则退出列表 |
| Remember Last Index | true | 退出时缓存位置，下次进入从该位置恢复 |

#### Bridge API

```csharp
// 注入 EventSystem（OnOpen/OnShow 里调用）
bridge.SetEventSystem(this.OwnerEventSystem);

// 进入列表（入口按钮 onClick 里调用）
bridge.EnterList();                            // 使用 Inspector 默认参数
bridge.EnterList(restoreLastIndex: true);      // 显式控制是否恢复位置
bridge.EnterList(loopAtExtremity: false);      // 显式控制是否循环
bridge.EnterList(true, false);                 // 两个都显式设置

// 数据刷新后重置缓存位置（SetData 后调用，防止越界）
bridge.ResetLastIndex();

// 手动退出列表（通常由 Cancel 输入自动触发）
bridge.ExitList();

// 回调（赋值用 = ，清理置 null）
bridge.OnItemFocused   = (index) => { /* 更新高亮 */ };
bridge.OnItemSubmitted = (index) => { /* 处理选中 */ };
bridge.OnExitList      = () => { /* 焦点还给面板按钮 */ };
```

#### 面板使用模板

```csharp
LoadingListAdapter _list;

protected override void OnOpen()
{
    _list = LoadingList.GetComponent<LoadingListAdapter>();
    RegisterUIEvent();
    RegisterEvent();
}

protected override void OnShow()
{
    this.BindSpriteLoader(_list);
    LoadingList.gameObject.SetActive(true);  // 先激活，让 OSA.Start() 执行
    _list.SetData(testData);                 // 再设数据

    _list.InputBridge.SetEventSystem(this.OwnerEventSystem);
}

private void RegisterUIEvent()
{
    this.AddClick(BtnEnterList, () => _list.InputBridge.EnterList());

    _list.InputBridge.OnItemFocused   = (index) => { /* 高亮逻辑 */ };
    _list.InputBridge.OnItemSubmitted = (index) => { /* 选中逻辑 */ };
    _list.InputBridge.OnExitList      = () =>
    {
        this.OwnerEventSystem.SetSelectedGameObject(BtnEnterList.gameObject);
    };
}

private void UnRegisterUIEvent()
{
    _list.InputBridge.OnItemFocused   = null;
    _list.InputBridge.OnItemSubmitted = null;
    _list.InputBridge.OnExitList      = null;
}
```

---

## 面板基类

所有面板继承 `EUUIPanelBase<T>`，可重写以下方法：

| 方法/属性 | 说明 |
|---|---|
| `OnOpen()` | 面板首次打开时调用，用于注册事件 |
| `OnShow()` | 面板每次显示时调用，用于刷新数据 |
| `OnHide()` | 面板隐藏时调用 |
| `OnClose()` | 面板关闭时调用，用于注销事件 |
| `OnCanOpen()` | 返回 false 阻止面板打开 |
| `GetDefaultSelectable()` | 返回面板显示时默认聚焦的按钮 |
| `DefaultLayer` | 面板所属 UI 层级（默认 Normal） |
| `EnableClose` | 是否允许关闭（默认 true） |

**面板内置属性：**

```csharp
OwnerPlayerIndex    // 所属玩家槽位（单人为 -1，多人为 0～3）
OwnerEventSystem    // 所属 EventSystem（自动判断单人/多人）
```

---

## 完整使用示例

### GameBoot — 框架初始化

```csharp
public class GameBoot : MonoBehaviour
{
    void Start() => InitAsync().Forget();

    async UniTaskVoid InitAsync()
    {
        await EUResKit.InitializeAllPackagesAsync();
        EUUIKit.Initialize(gameObject);

        // 创建玩家输入控制器（多人时按需创建）
        var p1 = new EUPlayerInputController(); p1.Enable();
        var p2 = new EUPlayerInputController(); p2.Enable();

        await EUUIKit.OpenAsync<WndLoading>();
    }
}
```

### WndLoading — 切换到多人模式

```csharp
private void RegisterUIEvent()
{
    this.AddClick(BtnStart, () =>
    {
        EUUIKit.Close<WndLoading>();
        EUUIKit.EnterMultiplayerMode(2, MultiplayerLayoutMode.Linear);
        EUUIKit.AssignDefaultDevices();
        EUUIKit.OpenForPlayerAsync<WndGame>(0).Forget();
        EUUIKit.OpenForPlayerAsync<WndGame>(1).Forget();
    });
}
```

### WndGame — 多人面板 + OSA 导航

```csharp
public partial class WndGame : EUUIPanelBase<WndGame>
{
    LoadingListAdapter _list;

    protected override void OnOpen()
    {
        _list = LoadingList.GetComponent<LoadingListAdapter>();
        RegisterUIEvent();
    }

    protected override void OnShow()
    {
        Text.text = $"Player {OwnerPlayerIndex + 1}";

        // 设置面板默认焦点
        this.OwnerEventSystem.SetSelectedGameObject(Btn_1.gameObject);

        // OSA 数据初始化
        LoadingList.gameObject.SetActive(true);
        _list.SetData(GetTestData());
        _list.InputBridge.SetEventSystem(this.OwnerEventSystem);
    }

    private void RegisterUIEvent()
    {
        // 进入列表
        this.AddClick(Btn_1, () => _list.InputBridge.EnterList());

        // 列表回调
        _list.InputBridge.OnItemFocused   = index => { /* 高亮第 index 项 */ };
        _list.InputBridge.OnItemSubmitted = index => Debug.Log($"选中: {index}");
        _list.InputBridge.OnExitList      = () =>
        {
            this.OwnerEventSystem.SetSelectedGameObject(Btn_1.gameObject);
        };
    }

    private void UnRegisterUIEvent()
    {
        _list.InputBridge.OnItemFocused   = null;
        _list.InputBridge.OnItemSubmitted = null;
        _list.InputBridge.OnExitList      = null;
    }

    protected override Selectable GetDefaultSelectable() => Btn_1;
    protected override void OnHide()  { }
    protected override void OnClose() { UnRegisterUIEvent(); }
}
```

---

## 相关文档

| 文档 | 内容 |
|---|---|
| EUUI -- 介绍 快速入门 | 框架整体介绍与快速上手 |
| EUUI -- 全局_单人面板 | 单人面板生命周期与工作流 |
| EUUI -- 输入导航处理 | 单人 / 多人同屏输入导航详解 |
| EUUI -- 同人多屏的UI资源制作处理 | 多人同屏 UI 资源制作规范 |
| EUUI -- OSA接入InputSystem后的导航 | OSA 列表导航接入方案 |
| EUUI -- API处理 | 完整 API 速查 |
