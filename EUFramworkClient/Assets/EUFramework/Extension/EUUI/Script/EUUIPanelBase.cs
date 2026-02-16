using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// EUUI 面板基类（参考 Doc/UIPanelBase.cs.txt）
    /// </summary>
    public abstract class EUUIPanelBase<TPanel> : MonoBehaviour, IEUUIPanel
        where TPanel : EUUIPanelBase<TPanel>
    {
        public abstract string PackageName { get; }
        public abstract string PanelName { get; }

        public IEUUIPanelData uiPanelData;

        /// <summary>面板默认所属层级（子类可重写）</summary>
        public virtual EUUILayerEnum DefaultLayer => EUUILayerEnum.Normal;

        public virtual bool EnableClose => true;

        private bool _isOpen;
        private bool _isVisible;

        private List<Button> _buttons = new List<Button>();
        private List<EventTrigger> _clickTriggers = new List<EventTrigger>();
        private List<EventTrigger> _longPressTriggers = new List<EventTrigger>();
        private List<CancellationTokenSource> _longPressCts = new List<CancellationTokenSource>();
        private List<EventTrigger> _dragTriggers = new List<EventTrigger>();

        private static object CurrentDragData { get; set; }
        private static GameObject _dragGhost;
        private static Canvas _dragRootCanvas;

        public virtual bool CanOpen()
        {
            if (_isOpen) return false;
            return OnCanOpen();
        }

        public abstract bool OnCanOpen();

        public virtual async UniTask OpenAsync(IEUUIPanelData data = null)
        {
            uiPanelData = data;
            if (!CanOpen())
            {
                Debug.LogError($"[EUUI] Panel {PanelName} cannot open (CanOpen returned false)");
                return;
            }
            _isOpen = true;
            Clear();
            OnOpen();
            await UniTask.Yield();
            Show();
        }

        public virtual void Show()
        {
            if (_isVisible) return;
            _isVisible = true;
            gameObject.SetActive(_isVisible);
            OnShow();
        }

        public virtual void Hide()
        {
            if (!_isVisible) return;
            _isVisible = false;
            gameObject.SetActive(_isVisible);
            OnHide();
        }

        public virtual void Close()
        {
            if (!_isOpen) return;
            try
            {
                Clear();
                try { OnClose(); }
                catch (Exception e) { Debug.LogError($"[EUUI] OnClose error: {PanelName}\n{e}"); }
            }
            finally
            {
                _isVisible = false;
                _isOpen = false;
                Destroy(gameObject);
            }
        }

        private void Clear()
        {
            foreach (var button in _buttons)
                button.onClick.RemoveAllListeners();
            _buttons.Clear();

            foreach (var trigger in _clickTriggers)
                trigger?.triggers.RemoveAll(e => e.eventID == EventTriggerType.PointerClick);
            _clickTriggers.Clear();

            foreach (var cts in _longPressCts)
            {
                cts?.Cancel();
                cts?.Dispose();
            }
            _longPressCts.Clear();

            foreach (var trigger in _longPressTriggers)
                trigger?.triggers.RemoveAll(e =>
                    e.eventID == EventTriggerType.PointerDown ||
                    e.eventID == EventTriggerType.PointerUp ||
                    e.eventID == EventTriggerType.PointerExit);
            _longPressTriggers.Clear();

            foreach (var trigger in _dragTriggers)
                trigger?.triggers.RemoveAll(e =>
                    e.eventID == EventTriggerType.BeginDrag ||
                    e.eventID == EventTriggerType.Drag ||
                    e.eventID == EventTriggerType.EndDrag ||
                    e.eventID == EventTriggerType.Drop);
            _dragTriggers.Clear();
        }

        #region UI Helpers

        protected void SetText(Text text, string content)
        {
            if (text != null) text.text = content;
        }

        protected void SetImage(Image image, Sprite sprite, bool isSetNativeSize = true)
        {
            if (image != null) image.sprite = sprite;
            if (isSetNativeSize && image != null) image.SetNativeSize();
        }

        // SetImage(Image, string url) 已移至 EUUIPanelBase 扩展模板中（如 EUUIPanelBaseEUResExtensions）
        // 若需使用请在 EUUIEditorConfig 中启用对应的扩展模块并生成扩展代码

        protected void AddClick(Button button, Action action)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action?.Invoke());
            _buttons.Add(button);
        }

        protected void AddClick<T>(Button button, T param, Action<T> action)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action?.Invoke(param));
            _buttons.Add(button);
        }

        protected void AddClick(GameObject go, Action action)
        {
            if (go == null) return;
            AddClickToGameObject(go, action);
        }

        protected void AddClick<T>(GameObject go, T param, Action<T> action)
        {
            if (go == null) return;
            AddClickToGameObject(go, () => action?.Invoke(param));
        }

        private void AddClickToGameObject(GameObject go, Action action)
        {
            var trigger = GetOrAddEventTrigger(go);
            trigger.triggers.RemoveAll(e => e.eventID == EventTriggerType.PointerClick);
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(_ => action?.Invoke());
            trigger.triggers.Add(entry);
            _clickTriggers.Add(trigger);
        }

        protected void RemoveClick(Button button)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                _buttons.Remove(button);
            }
        }

        protected void RemoveClick(GameObject go)
        {
            if (go == null) return;
            var trigger = go.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                trigger.triggers.RemoveAll(e => e.eventID == EventTriggerType.PointerClick);
                _clickTriggers.Remove(trigger);
            }
        }

        protected void AddLongPressRepeat(GameObject go, Action onRepeat, float interval = 0.1f, float delay = 0.3f)
        {
            if (go == null || onRepeat == null) return;
            var trigger = GetOrAddEventTrigger(go);
            CancellationTokenSource cts = null;
            AddTriggerEntry(trigger, EventTriggerType.PointerDown, _ =>
            {
                cts = new CancellationTokenSource();
                _longPressCts.Add(cts);
                LongPressRepeatAsync(onRepeat, interval, delay, cts.Token).Forget();
            });
            AddTriggerEntry(trigger, EventTriggerType.PointerUp, _ => CancelLongPress(ref cts));
            AddTriggerEntry(trigger, EventTriggerType.PointerExit, _ => CancelLongPress(ref cts));
            _longPressTriggers.Add(trigger);
        }

        protected void AddLongPressHold(GameObject go, Action onHold, float holdTime = 0.5f, Action<float> onProgress = null)
        {
            if (go == null || onHold == null) return;
            var trigger = GetOrAddEventTrigger(go);
            CancellationTokenSource cts = null;
            AddTriggerEntry(trigger, EventTriggerType.PointerDown, _ =>
            {
                cts = new CancellationTokenSource();
                _longPressCts.Add(cts);
                LongPressHoldAsync(onHold, holdTime, onProgress, cts.Token).Forget();
            });
            AddTriggerEntry(trigger, EventTriggerType.PointerUp, _ => { CancelLongPress(ref cts); onProgress?.Invoke(0f); });
            AddTriggerEntry(trigger, EventTriggerType.PointerExit, _ => { CancelLongPress(ref cts); onProgress?.Invoke(0f); });
            _longPressTriggers.Add(trigger);
        }

        private async UniTaskVoid LongPressRepeatAsync(Action onRepeat, float interval, float delay, CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);
                while (!token.IsCancellationRequested)
                {
                    onRepeat?.Invoke();
                    await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: token);
                }
            }
            catch (OperationCanceledException) { }
        }

        private async UniTaskVoid LongPressHoldAsync(Action onHold, float holdTime, Action<float> onProgress, CancellationToken token)
        {
            try
            {
                float elapsed = 0f;
                while (elapsed < holdTime)
                {
                    await UniTask.Yield(token);
                    elapsed += Time.deltaTime;
                    onProgress?.Invoke(Mathf.Clamp01(elapsed / holdTime));
                }
                onHold?.Invoke();
            }
            catch (OperationCanceledException) { }
        }

        private void CancelLongPress(ref CancellationTokenSource cts)
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                _longPressCts.Remove(cts);
                cts = null;
            }
        }

        protected void RemoveLongPress(GameObject go)
        {
            if (go == null) return;
            var trigger = go.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                trigger.triggers.RemoveAll(e =>
                    e.eventID == EventTriggerType.PointerDown ||
                    e.eventID == EventTriggerType.PointerUp ||
                    e.eventID == EventTriggerType.PointerExit);
                _longPressTriggers.Remove(trigger);
            }
        }

        protected void AddDrag(GameObject handle, Transform target = null)
        {
            if (handle == null) return;
            if (target == null) target = handle.transform;
            var trigger = GetOrAddEventTrigger(handle);
            Vector2 dragOffset = Vector2.zero;
            AddTriggerEntry(trigger, EventTriggerType.BeginDrag, data =>
            {
                var pointerData = data as PointerEventData;
                if (pointerData != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    target.parent as RectTransform, pointerData.position, pointerData.pressEventCamera, out Vector2 localPoint))
                    dragOffset = (Vector2)target.localPosition - localPoint;
            });
            AddTriggerEntry(trigger, EventTriggerType.Drag, data =>
            {
                var pointerData = data as PointerEventData;
                if (pointerData != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    target.parent as RectTransform, pointerData.position, pointerData.pressEventCamera, out Vector2 localPoint))
                    target.localPosition = localPoint + dragOffset;
            });
            _dragTriggers.Add(trigger);
        }

        protected void AddDragSource<T>(GameObject go, T data, Action<T> onBeginDrag = null, Action<T> onEndDrag = null)
        {
            AddDragSource(go, data, 0f, onBeginDrag, onEndDrag);
        }

        protected void AddDragSource<T>(GameObject go, T data, float ghostAlpha, Action<T> onBeginDrag = null, Action<T> onEndDrag = null)
        {
            if (go == null) return;
            var trigger = GetOrAddEventTrigger(go);
            AddTriggerEntry(trigger, EventTriggerType.BeginDrag, baseData =>
            {
                CurrentDragData = data;
                if (ghostAlpha > 0f)
                {
                    _dragGhost = CreateDragGhost(go, ghostAlpha);
                    if (baseData is PointerEventData pointerData) UpdateGhostPosition(pointerData);
                }
                onBeginDrag?.Invoke(data);
            });
            AddTriggerEntry(trigger, EventTriggerType.Drag, baseData =>
            {
                if (_dragGhost != null && baseData is PointerEventData pointerData)
                    UpdateGhostPosition(pointerData);
            });
            AddTriggerEntry(trigger, EventTriggerType.EndDrag, _ =>
            {
                if (_dragGhost != null) { Destroy(_dragGhost); _dragGhost = null; }
                _dragRootCanvas = null;
                onEndDrag?.Invoke(data);
                CurrentDragData = null;
            });
            _dragTriggers.Add(trigger);
        }

        private GameObject CreateDragGhost(GameObject source, float alpha)
        {
            _dragRootCanvas = source.GetComponentInParent<Canvas>()?.rootCanvas;
            if (_dragRootCanvas == null) return null;
            var ghost = Instantiate(source, _dragRootCanvas.transform);
            ghost.name = "DragGhost";
            var rect = ghost.GetComponent<RectTransform>();
            if (rect != null) rect.SetAsLastSibling();
            var canvasGroup = ghost.GetComponent<CanvasGroup>() ?? ghost.AddComponent<CanvasGroup>();
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            return ghost;
        }

        private void UpdateGhostPosition(PointerEventData pointerData)
        {
            if (_dragGhost == null || _dragRootCanvas == null) return;
            var ghostRect = _dragGhost.GetComponent<RectTransform>();
            if (ghostRect == null) return;
            Camera cam = _dragRootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _dragRootCanvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _dragRootCanvas.transform as RectTransform, pointerData.position, cam, out Vector2 localPoint))
                ghostRect.anchoredPosition = localPoint;
        }

        protected void AddDropTarget<T>(GameObject go, Action<T> onDrop)
        {
            if (go == null) return;
            var trigger = GetOrAddEventTrigger(go);
            AddTriggerEntry(trigger, EventTriggerType.Drop, _ =>
            {
                if (CurrentDragData is T data) onDrop?.Invoke(data);
            });
            _dragTriggers.Add(trigger);
        }

        protected void RemoveDrag(GameObject go)
        {
            if (go == null) return;
            var trigger = go.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                trigger.triggers.RemoveAll(e =>
                    e.eventID == EventTriggerType.BeginDrag ||
                    e.eventID == EventTriggerType.Drag ||
                    e.eventID == EventTriggerType.EndDrag ||
                    e.eventID == EventTriggerType.Drop);
                _dragTriggers.Remove(trigger);
            }
        }

        private void AddTriggerEntry(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            trigger.triggers.RemoveAll(e => e.eventID == type);
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(callback);
            trigger.triggers.Add(entry);
        }

        private static EventTrigger GetOrAddEventTrigger(GameObject go)
        {
            var trigger = go.GetComponent<EventTrigger>();
            if (trigger == null) trigger = go.AddComponent<EventTrigger>();
            return trigger;
        }

        #endregion

        protected abstract void OnOpen();
        protected abstract void OnShow();
        protected abstract void OnHide();
        protected abstract void OnClose();
    }
}
