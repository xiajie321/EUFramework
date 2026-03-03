using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EUFramework.Extension.EUInputControllerKit.Example
{
    public class InputPlayerTest:MonoBehaviour
    {
        public Text text;
        [SerializeField] private Transform root;
        private PlayerInputController _playerInputController;
        private static bool _init = false;
        Camera cam;
        private void Start()
        {
            cam ??= Camera.main;
        }

        private void OnEnable()
        {
            if (_playerInputController == null) Init();
            EUInputController.AddPlayerInputDeviceAddedListener(OnInputDeviceAdded);
            EUInputController.AddPlayerInputDeviceRemovedListener(OnInputDeviceRemoved);
            var ls = EUInputController.GetIdlePlayerInputDeviceList();
            _playerInputController?.PlayerInputControllerEvent.AddMoveListener(Move);
            if(ls.Length == 0) return;
            EUInputController.SetPlayerInputControllerOfDevice(_playerInputController, ls[0]);
            text.text = ls[0].ToString();
        }

        private void Init()
        {
            if (_playerInputController == null)
            {
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
        }
        private void OnInputDeviceAdded(InputDevice inputDevice)
        {
            if(inputDevice.GetPlayerInputController() != null) return;
            if (_playerInputController == null) Init();
            if(_playerInputController?.Gamepad != null) return;//如果输入设备已经存在则不进行设置
            EUInputController.SetPlayerInputControllerOfDevice(_playerInputController, inputDevice);
            text.text = inputDevice.ToString();
        }

        private void OnInputDeviceRemoved(InputDevice inputDevice)
        {
            PlayerInputController ls = inputDevice.GetPlayerInputController();//判断该PlayerInputController是否与当玩家输入控制器相连
            if(ls == null) return;
            if(ls !=  _playerInputController) return;
            EUInputController.SetPlayerInputControllerOfDevice(_playerInputController,null);
            text.text = "无设备";
        }

        public void Move(InputAction.CallbackContext context)
        {
            Vector2 pos = context.ReadValue<Vector2>();
            transform.position += new Vector3(pos.x, pos.y, 0);
            Debug.Log($"{context.ToString()} : {context.ReadValue<Vector2>()}");
        }
        
        private void Update()
        {
            text.transform.position = cam.WorldToScreenPoint(root.position);
        }

        private void OnDisable()
        {
            EUInputController.RemovePlayerInputDeviceAddedListener(OnInputDeviceAdded);
            EUInputController.RemovePlayerInputDeviceRemovedListener(OnInputDeviceRemoved);
            if(_playerInputController == null) return;
            _playerInputController.PlayerInputControllerEvent.RemoveMoveListener(Move);
            EUInputController.RemovePlayerInputController(_playerInputController);
            _playerInputController = null;//防止野引用所以要重置一下
        }
    }
}