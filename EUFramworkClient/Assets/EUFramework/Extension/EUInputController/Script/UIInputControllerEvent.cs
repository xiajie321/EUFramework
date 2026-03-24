// =============================================
// 此文件由 InputControllerCodeGenerator 自动生成
// 请勿手动修改此文件，修改将在下次生成时被覆盖
// =============================================
using System;
using UnityEngine.InputSystem;

namespace EUFramework.Extension.EUInputControllerKit
{
    
    public sealed class UIInputEvent
    {
        internal readonly UIInputControllerEvent Event = new UIInputControllerEvent();
        public void AddNavigateListener(Action<InputAction.CallbackContext> action) => Event._onNavigate += action;
        public void RemoveNavigateListener(Action<InputAction.CallbackContext> action) => Event._onNavigate -= action;
        public void RemoveAllNavigateListener() => Event._onNavigate = null;
        public void AddSubmitListener(Action<InputAction.CallbackContext> action) => Event._onSubmit += action;
        public void RemoveSubmitListener(Action<InputAction.CallbackContext> action) => Event._onSubmit -= action;
        public void RemoveAllSubmitListener() => Event._onSubmit = null;
        public void AddCancelListener(Action<InputAction.CallbackContext> action) => Event._onCancel += action;
        public void RemoveCancelListener(Action<InputAction.CallbackContext> action) => Event._onCancel -= action;
        public void RemoveAllCancelListener() => Event._onCancel = null;
        public void AddPointListener(Action<InputAction.CallbackContext> action) => Event._onPoint += action;
        public void RemovePointListener(Action<InputAction.CallbackContext> action) => Event._onPoint -= action;
        public void RemoveAllPointListener() => Event._onPoint = null;
        public void AddClickListener(Action<InputAction.CallbackContext> action) => Event._onClick += action;
        public void RemoveClickListener(Action<InputAction.CallbackContext> action) => Event._onClick -= action;
        public void RemoveAllClickListener() => Event._onClick = null;
        public void AddScrollWheelListener(Action<InputAction.CallbackContext> action) => Event._onScrollWheel += action;
        public void RemoveScrollWheelListener(Action<InputAction.CallbackContext> action) => Event._onScrollWheel -= action;
        public void RemoveAllScrollWheelListener() => Event._onScrollWheel = null;
        public void AddMiddleClickListener(Action<InputAction.CallbackContext> action) => Event._onMiddleClick += action;
        public void RemoveMiddleClickListener(Action<InputAction.CallbackContext> action) => Event._onMiddleClick -= action;
        public void RemoveAllMiddleClickListener() => Event._onMiddleClick = null;
        public void AddRightClickListener(Action<InputAction.CallbackContext> action) => Event._onRightClick += action;
        public void RemoveRightClickListener(Action<InputAction.CallbackContext> action) => Event._onRightClick -= action;
        public void RemoveAllRightClickListener() => Event._onRightClick = null;
        public void AddTrackedDevicePositionListener(Action<InputAction.CallbackContext> action) => Event._onTrackedDevicePosition += action;
        public void RemoveTrackedDevicePositionListener(Action<InputAction.CallbackContext> action) => Event._onTrackedDevicePosition -= action;
        public void RemoveAllTrackedDevicePositionListener() => Event._onTrackedDevicePosition = null;
        public void AddTrackedDeviceOrientationListener(Action<InputAction.CallbackContext> action) => Event._onTrackedDeviceOrientation += action;
        public void RemoveTrackedDeviceOrientationListener(Action<InputAction.CallbackContext> action) => Event._onTrackedDeviceOrientation -= action;
        public void RemoveAllTrackedDeviceOrientationListener() => Event._onTrackedDeviceOrientation = null;
    }
    internal sealed class UIInputControllerEvent:InputController.IUIActions
    {
        internal UIInputControllerEvent(){}
        internal Action<InputAction.CallbackContext> _onNavigate;
        internal Action<InputAction.CallbackContext> _onSubmit;
        internal Action<InputAction.CallbackContext> _onCancel;
        internal Action<InputAction.CallbackContext> _onPoint;
        internal Action<InputAction.CallbackContext> _onClick;
        internal Action<InputAction.CallbackContext> _onScrollWheel;
        internal Action<InputAction.CallbackContext> _onMiddleClick;
        internal Action<InputAction.CallbackContext> _onRightClick;
        internal Action<InputAction.CallbackContext> _onTrackedDevicePosition;
        internal Action<InputAction.CallbackContext> _onTrackedDeviceOrientation;
        public void OnNavigate(InputAction.CallbackContext context)
        {
            _onNavigate?.Invoke(context);
        }

        public void OnSubmit(InputAction.CallbackContext context)
        {
            _onSubmit?.Invoke(context);
        }

        public void OnCancel(InputAction.CallbackContext context)
        {
            _onCancel?.Invoke(context);
        }

        public void OnPoint(InputAction.CallbackContext context)
        {
            _onPoint?.Invoke(context);
        }

        public void OnClick(InputAction.CallbackContext context)
        {
            _onClick?.Invoke(context);
        }

        public void OnScrollWheel(InputAction.CallbackContext context)
        {
            _onScrollWheel?.Invoke(context);
        }

        public void OnMiddleClick(InputAction.CallbackContext context)
        {
            _onMiddleClick?.Invoke(context);
        }

        public void OnRightClick(InputAction.CallbackContext context)
        {
            _onRightClick?.Invoke(context);
        }

        public void OnTrackedDevicePosition(InputAction.CallbackContext context)
        {
            _onTrackedDevicePosition?.Invoke(context);
        }

        public void OnTrackedDeviceOrientation(InputAction.CallbackContext context)
        {
            _onTrackedDeviceOrientation?.Invoke(context);
        }
    }
}
