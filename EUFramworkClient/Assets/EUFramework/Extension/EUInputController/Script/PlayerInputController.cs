using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EUFramework.Extension.EUInputControllerKit
{
    
    public sealed class PlayerInputController
    {
        private InputController _controller;//控制器
        private Gamepad _gamepad;//手柄绑定
        private PlayerInputEvent _playerInputEvent;
        private UIInputEvent _uiInputEvent;
        public Gamepad Gamepad
        {
            get => _gamepad;
            internal set => BindGamepad(value);
        }
        public InputController Controller => _controller;
        public PlayerInputEvent PlayerInputControllerEvent => _playerInputEvent;
        public UIInputEvent UIInputControllerEvent => _uiInputEvent;
        internal PlayerInputController()
        {
            _controller = new InputController();
            _playerInputEvent = new();
            _uiInputEvent = new();
            BindGamepad(null);
            _controller.Player.SetCallbacks(_playerInputEvent.Event);
            _controller.UI.SetCallbacks(_uiInputEvent.Event);
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
                _gamepad = null;
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
    }
}