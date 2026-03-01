// =============================================
// 此文件由 InputControllerCodeGenerator 自动生成
// 请勿手动修改此文件，修改将在下次生成时被覆盖
// =============================================
using System;
using UnityEngine.InputSystem;

namespace EUFramework.Extension.EUInputControllerKit
{
    
    public sealed class PlayerInputEvent
    {
        internal readonly PlayerInputControllerEvent Event = new PlayerInputControllerEvent();
        public void AddMoveListener(Action<InputAction.CallbackContext> action) => Event._onMove = action;
        public void RemoveMoveListener(Action<InputAction.CallbackContext> action) => Event._onMove -= action;
        public void RemoveAllMoveListener() => Event._onMove = null;
        public void AddJumpListener(Action<InputAction.CallbackContext> action) => Event._onJump = action;
        public void RemoveJumpListener(Action<InputAction.CallbackContext> action) => Event._onJump -= action;
        public void RemoveAllJumpListener() => Event._onJump = null;
        public void AddInteractionListener(Action<InputAction.CallbackContext> action) => Event._onInteraction = action;
        public void RemoveInteractionListener(Action<InputAction.CallbackContext> action) => Event._onInteraction -= action;
        public void RemoveAllInteractionListener() => Event._onInteraction = null;
        public void AddRaiseListener(Action<InputAction.CallbackContext> action) => Event._onRaise = action;
        public void RemoveRaiseListener(Action<InputAction.CallbackContext> action) => Event._onRaise -= action;
        public void RemoveAllRaiseListener() => Event._onRaise = null;
        public void AddPickUpListener(Action<InputAction.CallbackContext> action) => Event._onPickUp = action;
        public void RemovePickUpListener(Action<InputAction.CallbackContext> action) => Event._onPickUp -= action;
        public void RemoveAllPickUpListener() => Event._onPickUp = null;
        public void AddPushPullListener(Action<InputAction.CallbackContext> action) => Event._onPushPull = action;
        public void RemovePushPullListener(Action<InputAction.CallbackContext> action) => Event._onPushPull -= action;
        public void RemoveAllPushPullListener() => Event._onPushPull = null;
        public void AddDiscardListener(Action<InputAction.CallbackContext> action) => Event._onDiscard = action;
        public void RemoveDiscardListener(Action<InputAction.CallbackContext> action) => Event._onDiscard -= action;
        public void RemoveAllDiscardListener() => Event._onDiscard = null;
        public void AddDisassembleListener(Action<InputAction.CallbackContext> action) => Event._onDisassemble = action;
        public void RemoveDisassembleListener(Action<InputAction.CallbackContext> action) => Event._onDisassemble -= action;
        public void RemoveAllDisassembleListener() => Event._onDisassemble = null;
    }
    internal sealed class PlayerInputControllerEvent:InputController.IPlayerActions
    {
        internal PlayerInputControllerEvent(){}
        internal Action<InputAction.CallbackContext> _onMove;
        internal Action<InputAction.CallbackContext> _onJump;
        internal Action<InputAction.CallbackContext> _onInteraction;
        internal Action<InputAction.CallbackContext> _onRaise;
        internal Action<InputAction.CallbackContext> _onPickUp;
        internal Action<InputAction.CallbackContext> _onPushPull;
        internal Action<InputAction.CallbackContext> _onDiscard;
        internal Action<InputAction.CallbackContext> _onDisassemble;
        public void OnMove(InputAction.CallbackContext context)
        {
            _onMove?.Invoke(context);
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            _onJump?.Invoke(context);
        }

        public void OnInteraction(InputAction.CallbackContext context)
        {
            _onInteraction?.Invoke(context);
        }

        public void OnRaise(InputAction.CallbackContext context)
        {
            _onRaise?.Invoke(context);
        }

        public void OnPickUp(InputAction.CallbackContext context)
        {
            _onPickUp?.Invoke(context);
        }

        public void OnPushPull(InputAction.CallbackContext context)
        {
            _onPushPull?.Invoke(context);
        }

        public void OnDiscard(InputAction.CallbackContext context)
        {
            _onDiscard?.Invoke(context);
        }

        public void OnDisassemble(InputAction.CallbackContext context)
        {
            _onDisassemble?.Invoke(context);
        }
    }
}
