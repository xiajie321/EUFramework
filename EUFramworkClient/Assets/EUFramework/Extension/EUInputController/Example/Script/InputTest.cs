using EUFramework.Extension.EUInputControllerKit;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
namespace EUFramework.Extension.EUInputControllerKit.Example
{
    public class InputTest : MonoBehaviour
    {
        private void Start()
        {
            //---以下代码为初始化的示例代码---
            var ls = EUInputController.GetIdlePlayerInputControllerList();//获取空闲的PlayerInputController(玩家控制器)
            var ls2 = EUInputController.GetIdlePlayerInputDeviceList();//获取空闲的InputDevice(手柄设备)
            int inputControllerCount = ls.Length;
            int inputControllerIndex = 0;
            for (int i = 0; i < ls2.Length; i++)
            {
                PlayerInputController v;
                if (inputControllerCount == 0)
                {
                    v = EUInputController.GetPlayerInputController(EUInputController.AddPlayerInputController());//如果没有空闲的角色控制器则新建一个角色控制器并获取其引用
                }
                else
                {
                    v = ls[inputControllerIndex];
                    inputControllerIndex++;
                    inputControllerCount--;
                }
                EUInputController.SetPlayerInputControllerOfDevice(v,ls2[i]);//绑定角色控制器的对应设备
                v.PlayerInputControllerEvent.AddMoveListener(Move);//向对应的手柄映射添加对应的方法
                //v.PlayerInputControllerEvent.RemoveMoveListener(Move);//移除手柄映射对应的方法
            }
            //---添加事件---
            EUInputController.AddPlayerInputDeviceAddedListener(PlayerInputDeviceAdded);//当设备连接时触发的回调
            EUInputController.AddPlayerInputDeviceRemovedListener(PlayerInputDeviceRemoved);//当设备拔出时触发的回调
        }

        private void PlayerInputDeviceAdded(InputDevice inputDevice)
        {
            PlayerInputController v;
            var ls = EUInputController.GetIdlePlayerInputControllerList();//获取空闲的PlayerInputController
            if(ls.Length == 0) v = EUInputController.GetPlayerInputController(EUInputController.AddPlayerInputController());//如果没有空闲的角色控制器则新建一个角色控制器并获取其引用
            else v = ls[0];
            v.PlayerInputControllerEvent.AddMoveListener(Move);
            EUInputController.SetPlayerInputControllerOfDevice(v,inputDevice);
        }

        private void PlayerInputDeviceRemoved(InputDevice inputDevice)
        {
            //AddPlayerInputDeviceRemovedListener会在内部自动处理对应设备的玩家控制器的解绑问题,移除引用就行了。
            inputDevice.GetPlayerInputController()?.PlayerInputControllerEvent.RemoveMoveListener(Move);
        }
    
        private void Move(InputAction.CallbackContext context)
        {
            Debug.Log($"{context.ToString()} : {context.ReadValue<Vector2>()}");
        }
    }
}
#endif
