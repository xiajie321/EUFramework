using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EUFramework.Extension.EUInputController
{
    public class PlayerInputController:InputController.IPlayerActions
    {
        private InputController _controller;//控制器
        private Gamepad _gamepad;//手柄绑定
        private Action<InputAction.CallbackContext> _onMove;
        private Action<InputAction.CallbackContext> _onJump;
        private Action<InputAction.CallbackContext> _onInteraction;
        private Action<InputAction.CallbackContext> _onRaise;
        private Action<InputAction.CallbackContext> _onPickUp;
        private Action<InputAction.CallbackContext> _onPushPull;
        private Action<InputAction.CallbackContext> _onDiscard;
        private Action<InputAction.CallbackContext> _onDisassemble;

        //TODO 还需要处理按键映射
        public Gamepad Gamepad
        {
            get => _gamepad;
            internal set => BindGamepad(value);
        }

        public InputController Controller => _controller;
        internal PlayerInputController()
        {
            _controller = new InputController();
            BindGamepad(null);
            _controller.Player.SetCallbacks(this);
            _controller.Player.Enable();
            _controller.UI.Enable();
        }
        /// <summary>
        /// 绑定游戏手柄
        /// </summary>
        /// <param name="gamepad">(注意: 该值设置为空时默认使用键盘)</param>
        internal void BindGamepad(Gamepad gamepad)
        {
            if (gamepad == null)
            {
                _controller.devices = new[]
                {
                    Keyboard.current
                };
                return;
            }
            _gamepad = gamepad;
            _controller.devices= new[]
            {
                _gamepad
            };
        }
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