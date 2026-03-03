using System;
using EUFramework.Extension.EUInputControllerKit.MonoComponent;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EUFramework.Extension.EUInputControllerKit.Example
{
    public class InputPlayerTest : MonoBehaviour
    {
        public Text text;
        [SerializeField] private Transform root;
        [SerializeField] private float speed = 10;
        EUPlayerInputController _playerInputController;
        Camera cam;

        private void Start()
        {
            cam ??= Camera.main;
            _playerInputController = new();
            _playerInputController.PlayerInputController.PlayerInputControllerEvent.AddMoveListener(Move);
            _playerInputController.PlayerInputController.PlayerInputControllerEvent.AddJumpListener(Jump);
            _playerInputController.AddInputDeviceAdded(OnInputDeviceAdded);
            _playerInputController.AddInputDeviceRemoved(OnInputDeviceRemoved);
            _playerInputController.Enable(); //在注册完信息后使用该方法绑定默认手柄控制器
            text.text = _playerInputController.GetPlayerInputControllerGamepadDevice() != null
                ? _playerInputController.GetPlayerInputControllerGamepadDevice().ToString()
                : "默认设备";
        }

        private Vector2 pos;

        public void Move(InputAction.CallbackContext context)
        {
            pos = context.ReadValue<Vector2>() * Time.deltaTime * speed;
        }

        public void Jump(InputAction.CallbackContext context)
        {
            if (context.performed)
                transform.position += (Vector3)fx;
        }

        private void OnInputDeviceAdded(InputDevice inputDevice)
        {
            text.text = inputDevice.ToString();
        }

        private void OnInputDeviceRemoved(InputDevice inputDevice)
        {
            text.text = "默认设备";
        }
        private Vector3 lastPos;
        private Vector2 fx;
        private void Update()
        {
            text.transform.position = cam.WorldToScreenPoint(root.position);
            lastPos = transform.position;
            transform.position += new Vector3(pos.x, pos.y, 0);
            fx = (transform.position - lastPos).normalized;
        }

        private void OnDestroy()
        {
            _playerInputController.Disable();//解除绑定
        }
    }
}