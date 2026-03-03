using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EUFramework.Extension.EUInputControllerKit.MonoComponent
{
    public class EUPlayerInputControllerMono:MonoBehaviour
    {
        private PlayerInputController _playerInputController;
        private static bool _init = false;
        private Action<InputAction.CallbackContext> _onMove;
        private Action<InputAction.CallbackContext> _onJump;
        private Action<InputAction.CallbackContext> _onInteraction;
        private Action<InputAction.CallbackContext> _onRaise;
        private Action<InputAction.CallbackContext> _onPickUp;
        private Action<InputAction.CallbackContext> _onPushPull;
        private Action<InputAction.CallbackContext> _onDiscard;
        private Action<InputAction.CallbackContext> _onDisassemble;
        
        /// <summary>
        /// 设置玩家输入控制器的输入设备
        /// </summary>
        public void SetPlayerInputControllerOfDevice(InputDevice inputDevice) => _playerInputController?.SetPlayerInputControllerOfDevice(inputDevice);
        /// <summary>
        /// 获取角色输入控制器的手柄设备(如果没有则会返回null)
        /// </summary>
        public Gamepad GetPlayerInputControllerGamepadDevice()
        {
            return _playerInputController.Gamepad;
        }
        
        /// <summary>
        /// 获取具体的输入设备
        /// </summary>
        public InputDevice GetPlayerInputControllerInputDevice()
        {
            return _playerInputController.Controller.devices?[0];
        }
        public void AddMoveListener(Action<InputAction.CallbackContext> action) => _onMove = action;
        public void RemoveMoveListener(Action<InputAction.CallbackContext> action) => _onMove -= action;
        public void RemoveAllMoveListener() => _onMove = null;
        public void AddJumpListener(Action<InputAction.CallbackContext> action) => _onJump = action;
        public void RemoveJumpListener(Action<InputAction.CallbackContext> action) => _onJump -= action;
        public void RemoveAllJumpListener() => _onJump = null;
        public void AddInteractionListener(Action<InputAction.CallbackContext> action) => _onInteraction = action;
        public void RemoveInteractionListener(Action<InputAction.CallbackContext> action) => _onInteraction -= action;
        public void RemoveAllInteractionListener() => _onInteraction = null;
        public void AddRaiseListener(Action<InputAction.CallbackContext> action) => _onRaise = action;
        public void RemoveRaiseListener(Action<InputAction.CallbackContext> action) => _onRaise -= action;
        public void RemoveAllRaiseListener() => _onRaise = null;
        public void AddPickUpListener(Action<InputAction.CallbackContext> action) => _onPickUp = action;
        public void RemovePickUpListener(Action<InputAction.CallbackContext> action) => _onPickUp -= action;
        public void RemoveAllPickUpListener() => _onPickUp = null;
        public void AddPushPullListener(Action<InputAction.CallbackContext> action) => _onPushPull = action;
        public void RemovePushPullListener(Action<InputAction.CallbackContext> action) => _onPushPull -= action;
        public void RemoveAllPushPullListener() => _onPushPull = null;
        public void AddDiscardListener(Action<InputAction.CallbackContext> action) => _onDiscard = action;
        public void RemoveDiscardListener(Action<InputAction.CallbackContext> action) => _onDiscard -= action;
        public void RemoveAllDiscardListener() => _onDiscard = null;
        public void AddDisassembleListener(Action<InputAction.CallbackContext> action) => _onDisassemble = action;
        public void RemoveDisassembleListener(Action<InputAction.CallbackContext> action) => _onDisassemble -= action;
        public void RemoveAllDisassembleListener() => _onDisassemble = null;
        private void Register()
        {
            if(_playerInputController == null) return;
            _playerInputController.PlayerInputControllerEvent.AddMoveListener(_onMove);
            _playerInputController.PlayerInputControllerEvent.AddJumpListener(_onJump);
            _playerInputController.PlayerInputControllerEvent.AddDisassembleListener(_onDisassemble);
            _playerInputController.PlayerInputControllerEvent.AddDiscardListener(_onDiscard);
            _playerInputController.PlayerInputControllerEvent.AddInteractionListener(_onInteraction);
            _playerInputController.PlayerInputControllerEvent.AddPickUpListener(_onPickUp);
            _playerInputController.PlayerInputControllerEvent.AddPushPullListener(_onPushPull);
            _playerInputController.PlayerInputControllerEvent.AddRaiseListener(_onRaise);
        }

        private void UnRegister()
        {
            if(_playerInputController == null) return;
            _playerInputController.PlayerInputControllerEvent.RemoveMoveListener(_onMove);
            _playerInputController.PlayerInputControllerEvent.RemoveJumpListener(_onJump);
            _playerInputController.PlayerInputControllerEvent.RemoveDisassembleListener(_onDisassemble);
            _playerInputController.PlayerInputControllerEvent.RemoveDiscardListener(_onDiscard);
            _playerInputController.PlayerInputControllerEvent.RemoveInteractionListener(_onInteraction);
            _playerInputController.PlayerInputControllerEvent.RemovePickUpListener(_onPickUp);
            _playerInputController.PlayerInputControllerEvent.RemovePushPullListener(_onPushPull);
            _playerInputController.PlayerInputControllerEvent.RemoveRaiseListener(_onRaise);
        }
        private void OnEnable()
        {
            if (_playerInputController == null) Init();
            EUInputController.AddPlayerInputDeviceAddedListener(OnInputDeviceAdded);
            EUInputController.AddPlayerInputDeviceRemovedListener(OnInputDeviceRemoved);
            var ls = EUInputController.GetIdlePlayerInputDeviceList();
            Register();
            if(ls.Length == 0) return;
            EUInputController.SetPlayerInputControllerOfDevice(_playerInputController, ls[0]);
        }
        private void Init()
        {
            if (_playerInputController != null) return;
            if (!_init)
            {
                _init = true;
                _playerInputController = EUInputController.GetMainPlayerInputController();
            }
            else
            {
                _playerInputController = EUInputController.GetPlayerInputController(EUInputController.AddPlayerInputController());
            }
        }
        private void OnInputDeviceAdded(InputDevice inputDevice)
        {
            if(inputDevice.GetPlayerInputController() != null) return;
            if (_playerInputController == null) Init();
            if(_playerInputController?.Gamepad != null) return;//如果输入设备已经存在则不进行设置
            EUInputController.SetPlayerInputControllerOfDevice(_playerInputController, inputDevice);
        }

        private void OnInputDeviceRemoved(InputDevice inputDevice)
        {
            PlayerInputController ls = inputDevice.GetPlayerInputController();//判断该PlayerInputController是否与当玩家输入控制器相连
            if(ls == null) return;
            if(ls !=  _playerInputController) return;
            EUInputController.SetPlayerInputControllerOfDevice(_playerInputController,null);
        }

        private void OnDisable()
        {
            EUInputController.RemovePlayerInputDeviceAddedListener(OnInputDeviceAdded);
            EUInputController.RemovePlayerInputDeviceRemovedListener(OnInputDeviceRemoved);
            if(_playerInputController == null) return;
            UnRegister();
            if (EUInputController.GetMainPlayerInputController() != _playerInputController)
                EUInputController.RemovePlayerInputController(_playerInputController);
            else
                _init = false;
            _playerInputController = null;//防止野引用所以要重置一下
        }
    }
}