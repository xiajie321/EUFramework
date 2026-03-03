using System;
using UnityEngine.InputSystem;

namespace EUFramework.Extension.EUInputControllerKit.MonoComponent
{
    public class EUPlayerInputController
    {
        public EUPlayerInputController()
        {
            Init();
        }

        ~EUPlayerInputController()
        {
            Disable();
        }
        private PlayerInputController _playerInputController;
        private static bool _init = false;
        private Action<InputDevice> _onInputDeviceAdded;
        private Action<InputDevice> _onInputDeviceRemoved;
        public Action<InputDevice> AddInputDeviceAdded(Action<InputDevice> onInputDeviceAdded) =>  _onInputDeviceAdded += onInputDeviceAdded;
        public Action<InputDevice> RemoveInputDeviceAdded(Action<InputDevice> onInputDeviceAdded) =>  _onInputDeviceAdded -= onInputDeviceAdded;
        public Action<InputDevice> RemoveAllInputDeviceAdded() => _onInputDeviceAdded = null;
        public Action<InputDevice> AddInputDeviceRemoved(Action<InputDevice> onInputDeviceRemoved) => _onInputDeviceRemoved += onInputDeviceRemoved;
        public Action<InputDevice> RemoveInputDeviceRemoved(Action<InputDevice> onInputDeviceRemoved) => _onInputDeviceRemoved -= onInputDeviceRemoved;
        public Action<InputDevice> RemoveAllInputDeviceRemoved() => _onInputDeviceRemoved = null;
        public PlayerInputController PlayerInputController => _playerInputController;
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
        /// 获取玩家输入控制器
        /// </summary>
        /// <returns></returns>
        public PlayerInputController GetPlayerInputController() => _playerInputController;
        
        /// <summary>
        /// 获取具体的输入设备
        /// </summary>
        public InputDevice GetPlayerInputControllerInputDevice()
        {
            return _playerInputController.Controller.devices?[0];
        }
        /// <summary>
        /// 自动绑定手柄
        /// </summary>
        public void Enable()
        {
            if (_playerInputController == null) Init();
            EUInputController.AddPlayerInputDeviceAddedListener(OnInputDeviceAdded);
            EUInputController.AddPlayerInputDeviceRemovedListener(OnInputDeviceRemoved);
            var ls = EUInputController.GetIdlePlayerInputDeviceList();
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
            _onInputDeviceAdded?.Invoke(inputDevice);
            EUInputController.SetPlayerInputControllerOfDevice(_playerInputController, inputDevice);
        }

        private void OnInputDeviceRemoved(InputDevice inputDevice)
        {
            PlayerInputController ls = inputDevice.GetPlayerInputController();//判断该PlayerInputController是否与当玩家输入控制器相连
            if(ls == null) return;
            if(ls !=  _playerInputController) return;
            _onInputDeviceRemoved?.Invoke(inputDevice);
            EUInputController.SetPlayerInputControllerOfDevice(_playerInputController,null);
        }
        /// <summary>
        /// 解除绑定
        /// </summary>
        public void Disable()
        {
            EUInputController.RemovePlayerInputDeviceAddedListener(OnInputDeviceAdded);
            EUInputController.RemovePlayerInputDeviceRemovedListener(OnInputDeviceRemoved);
            if(_playerInputController == null) return;
            if (EUInputController.GetMainPlayerInputController() != _playerInputController)
                EUInputController.RemovePlayerInputController(_playerInputController);
            else
                _init = false;
            _playerInputController = null;//防止野引用所以要重置一下
        }
    }
}