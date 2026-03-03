// =============================================
// 此文件由 InputControllerCodeGenerator 自动生成
// 请勿手动修改此文件，修改将在下次生成时被覆盖
// =============================================
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
        public Gamepad Gamepad
        {
            get => _gamepad;
            internal set => BindGamepad(value);
        }
        public InputController Controller => _controller;
        public PlayerInputEvent PlayerInputControllerEvent => _playerInputEvent;
        internal PlayerInputController()
        {
            _controller = new InputController();
            _playerInputEvent = new();
            BindGamepad(null);
            _controller.Player.SetCallbacks(_playerInputEvent.Event);
            _controller.Player.Enable();
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
                _controller.devices = new InputDevice[]
                {
                    Keyboard.current,
                    Mouse.current
                };
                return;
            }
            _gamepad = gamepad;
            _controller.devices= new InputDevice[]
            {
                _gamepad,
                Mouse.current
            };
        }
    }
}
