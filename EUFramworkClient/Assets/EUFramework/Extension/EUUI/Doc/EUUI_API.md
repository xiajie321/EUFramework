# EUUI 对外业务 API

> 本文档面向业务开发者，整理游戏逻辑中常用的 EUUI Runtime API。
> Editor 工具、代码生成器、模板系统等内部 API 不在本文档范围内。

## 1. 初始化

游戏启动时需要先初始化 EUUI。

```csharp
EUUIKit.Initialize(gameObject);
```

`Initialize` 会创建或复用以下运行时对象：

- `EUUIRoot`
- `UICamera`
- `EUUICacheRoot`
- UI 层级节点
- Canvas / CanvasScaler / GraphicRaycaster
- EventSystem

通常建议在游戏启动入口调用一次。

```csharp
public class GameBoot : MonoBehaviour
{
    private void Start()
    {
        EUUIKit.Initialize(gameObject);
    }
}
```

## 2. 面板打开与关闭

### 打开面板

```csharp
await EUUIKit.OpenAsync<WndMain>();
await EUUIKit.OpenAsync<WndMain>(data);
```

`OpenAsync` 用于普通打开，不记录导航历史。适合弹窗、提示框、独立 UI。

### 关闭面板

```csharp
EUUIKit.Close<WndMain>();
EUUIKit.Close(typeof(WndMain));
```

### 获取已打开面板

```csharp
var panel = EUUIKit.GetPanel<WndMain>();
```

### 关闭全部面板

```csharp
EUUIKit.CloseAll();
```

### 关闭除指定面板外的所有面板

```csharp
EUUIKit.CloseAllExcept<WndMain>();
```

## 3. 页面导航

导航 API 适合主流程页面，例如大厅、背包、设置、角色详情等。

### 导航到页面

```csharp
await EUUIKit.NavigateToAsync<WndBag>();
```

会隐藏当前栈顶页面，并把新页面压入导航栈。

### 返回上一页

```csharp
await EUUIKit.BackAsync();
```

### 返回到指定页面

```csharp
await EUUIKit.BackToAsync<WndMain>();
```

### 清空历史

```csharp
await EUUIKit.ClearHistoryAsync();
```

### 独占打开

```csharp
await EUUIKit.OpenExclusiveAsync<WndBattle>();
```

适合从登录进入主界面、从大厅进入战斗等大流程切换。

## 4. 状态查询

```csharp
EUUIKit.IsPanelOpen<WndMain>();
EUUIKit.IsPanelInStack<WndMain>();
EUUIKit.GetCurrentPanelName();
EUUIKit.GetHistoryCount();
```

LRU 缓存状态：

```csharp
EUUIKit.IsPanelCached<WndMain>();
EUUIKit.GetCachedPanelCount();
```

## 5. 焦点与导航

### 设置焦点

```csharp
EUUIKit.SetDefaultSelection(button);
```

单人模式下设置当前选中的 UI 元素。通常业务不需要手动调用，面板可通过重写 `GetDefaultSelectable` 自动设置。

```csharp
protected override Selectable GetDefaultSelectable()
{
    return btnStart;
}
```

### 清除焦点

```csharp
EUUIKit.ClearSelection();
```

### 方向导航

```csharp
EUUIKit.Navigate(Vector2.up);
EUUIKit.Navigate(Vector2.down);
EUUIKit.Navigate(Vector2.left);
EUUIKit.Navigate(Vector2.right);
```

### Submit / Cancel

```csharp
EUUIKit.SubmitOrCancel(true);   // Submit
EUUIKit.SubmitOrCancel(false);  // BackAsync
```

## 6. 多人同屏

### 进入多人模式

```csharp
EUUIKit.EnterMultiplayerMode(
    playerCount: 2,
    layout: MultiplayerLayoutMode.Linear,
    axis: MultiplayerLayoutAxis.X
);
```

四宫格模式：

```csharp
EUUIKit.EnterMultiplayerMode(4, MultiplayerLayoutMode.Grid);
```

### 退出多人模式

```csharp
EUUIKit.ExitMultiplayerMode();
```

### 切换布局

```csharp
EUUIKit.SetMultiplayerLayout(
    playerCount: 2,
    layout: MultiplayerLayoutMode.Linear,
    axis: MultiplayerLayoutAxis.Y
);
```

### 为指定玩家打开面板

```csharp
await EUUIKit.OpenForPlayerAsync<WndPlayerHUD>(0);
await EUUIKit.OpenForPlayerAsync<WndPlayerHUD>(1);
```

### 关闭指定玩家面板

```csharp
EUUIKit.CloseForPlayer<WndPlayerHUD>(0);
EUUIKit.CloseAllForPlayer(0);
```

### 玩家导航

```csharp
await EUUIKit.NavigateForPlayerAsync<WndPlayerBag>(0);
await EUUIKit.BackForPlayerAsync(0);
await EUUIKit.BackForPlayerToAsync<WndPlayerMain>(0);
await EUUIKit.ClearPlayerHistoryAsync(0);
```

### 玩家状态查询

```csharp
EUUIKit.GetPlayerPanel<WndPlayerHUD>(0);
EUUIKit.IsPanelOpenForPlayer<WndPlayerHUD>(0);
EUUIKit.GetPlayerHistoryCount(0);
EUUIKit.GetPlayerCurrentPanelName(0);
```

### 玩家焦点

```csharp
EUUIKit.SetPlayerSelection(0, button);
EUUIKit.ClearPlayerSelection(0);
```

### 输入设备分配

启用 `EUInputController` 扩展后，可使用默认设备分配：

```csharp
EUUIKit.AssignDefaultDevices();
```

也可以手动分配：

```csharp
EUUIKit.AssignKeyboardPlayer(0);
EUUIKit.AssignGamepadPlayer(1, gamepad);
```

## 7. 面板基类

普通面板继承：

```csharp
public partial class WndMain : EUUIPanelBase<WndMain>
{
}
```

弹窗面板继承：

```csharp
public partial class WndConfirm : EUUIPopupPanelBase<WndConfirm>
{
}
```

业务面板需要实现生命周期方法：

```csharp
public override bool OnCanOpen()
{
    return true;
}

protected override void OnOpen()
{
}

protected override void OnShow()
{
}

protected override void OnHide()
{
}

protected override void OnClose()
{
}
```

常用辅助方法：

```csharp
SetText(txtTitle, "标题");
SetImage(imgIcon, sprite);

AddClick(btnClose, () => EUUIKit.Close<WndMain>());
AddClick(goItem, () => { });

AddLongPressRepeat(goButton, OnRepeat);
AddLongPressHold(goButton, OnHold);

AddDrag(handle);
AddDragSource(goItem, data);
AddDropTarget<ItemData>(goTarget, OnDrop);

SetupNavigationChain(true, true, btn1, btn2, btn3);
```

启用 EURes 扩展后，面板会额外获得资源加载辅助方法：

```csharp
SetImage(imgIcon, "AtlasName/SpriteName");

var sprite = LoadSprite("AtlasName/SpriteName");
var prefab = LoadPrefab("Some/PrefabPath");
var prefabAsync = await LoadPrefabAsync("Some/PrefabPath");
```

## 8. 弹窗面板

`EUUIPopupPanelBase<T>` 默认挂载到 `Popup` 层，并在显示时创建遮罩。

可重写遮罩配置：

```csharp
protected override bool EnableMask => true;
protected override Color MaskColor => new Color(0, 0, 0, 0.7f);
```

点击遮罩默认关闭当前弹窗。需要自定义关闭逻辑时，可重写：

```csharp
protected override void CloseSelf()
{
    EUUIKit.Close<WndConfirm>();
}
```

## 9. OSA 列表扩展

如果业务使用 `OSAExtension`，常用类型包括：

```csharp
FrameworkListAdapter<TData, TVH>
FrameworkGridAdapter<TData, TCellVH>
FrameworkListViewsHolder<TData>
FrameworkGridViewsHolder<TData>
EUOSAInputBridge
```

### Adapter 数据操作

```csharp
adapter.SetData(items);
adapter.AddItem(item);
adapter.AddItems(items);
adapter.Clear();
adapter.RefreshAll();
adapter.RefreshItem(index);
```

`FrameworkListAdapter` 额外支持：

```csharp
adapter.InsertAt(index, item);
adapter.RemoveAt(index);
```

### item 点击

```csharp
adapter.OnItemClick = (index, data) =>
{
};
```

`FrameworkGridAdapter` 使用事件方式：

```csharp
adapter.OnItemClick += (index, data) =>
{
};
```

清理点击监听：

```csharp
adapter.ClearItemClickListeners();
```

### 注入 SpriteLoader

```csharp
this.BindSpriteLoader(adapter);
```

该方法会检查当前面板是否实现 `IEUSpriteProvider`。如果启用了 `EUUIPanelBase.EURes.Generated.cs`，面板会自动具备该能力。

### OSA 输入桥接

```csharp
adapter.InputBridge.SetEventSystem(OwnerEventSystem);

adapter.InputBridge.OnItemFocused = index => { };
adapter.InputBridge.OnItemSubmitted = index => { };
adapter.InputBridge.OnExitList = () => { };

adapter.InputBridge.EnterList();
adapter.InputBridge.ExitList();
adapter.InputBridge.ResetLastIndex();
```

## 10. 推荐使用场景

普通页面：

```csharp
await EUUIKit.NavigateToAsync<WndMain>();
```

弹窗：

```csharp
await EUUIKit.OpenAsync<WndConfirm>();
```

流程切换：

```csharp
await EUUIKit.OpenExclusiveAsync<WndBattle>();
```

多人 UI：

```csharp
EUUIKit.EnterMultiplayerMode(2, MultiplayerLayoutMode.Linear);
await EUUIKit.OpenForPlayerAsync<WndPlayerHUD>(0);
await EUUIKit.OpenForPlayerAsync<WndPlayerHUD>(1);
```

OSA 列表：

```csharp
adapter.SetData(items);
adapter.OnItemClick = OnClickItem;
this.BindSpriteLoader(adapter);
```

## 11. 使用建议

- 页面流转优先使用 `NavigateToAsync` / `BackAsync`。
- 弹窗、提示框优先使用 `OpenAsync` / `Close`。
- 大流程切换优先使用 `OpenExclusiveAsync`。
- `Generated.cs` 不要手动修改，业务逻辑写在普通 `.cs` 文件中。
- 多人模式下使用 `OpenForPlayerAsync`、`SetPlayerSelection` 和 `OwnerEventSystem`。
- OSA 列表中不要直接依赖 `EUResLoader`，优先通过 `BindSpriteLoader` 注入面板级 Sprite 加载能力。
