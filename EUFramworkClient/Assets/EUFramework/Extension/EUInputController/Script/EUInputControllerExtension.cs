using Unity.Collections;
using UnityEngine.InputSystem;

namespace EUFramework.Extension.EUInputControllerKit
{
    public static class EUInputControllerExtension
    {
        /// <summary>
        /// 设置玩家输入控制器的输入设备
        /// </summary>
        public static void SetPlayerInputControllerOfDevice(this PlayerInputController playerInputController,
            InputDevice inputDevice)
        {
            EUInputController.SetPlayerInputControllerOfDevice(playerInputController, inputDevice);
        }
        /// <summary>
        /// 获取玩家输入控制器的Id
        /// </summary>
        public static int GetPlayerInputControllerId(this PlayerInputController playerInputController)
        {
            return EUInputController.GetPlayerInputControllerId(playerInputController);
        }
        /// <summary>
        /// 判断该控件是否存在
        /// </summary>
        public static bool Exists(this PlayerInputController playerInputController)
        {
            return EUInputController.PlayerInputControllerMapId.ContainsKey(playerInputController);
        }
        /// <summary>
        /// 判断该控件是否存在
        /// </summary>
        public static bool Exists(this InputDevice inputDevice)
        {
            return EUInputController.PlayerInputDeviceMap.ContainsKey(inputDevice.deviceId);
        }
        /// <summary>
        /// 获取设备对应的角色控制器(如果返回值为空表示没有设备没有对应的角色控制器)
        /// </summary>
        public static PlayerInputController GetPlayerInputController(this InputDevice inputDevice)
        {
            return EUInputController.GetPlayerInputDeviceOfPlayerInputController(inputDevice.deviceId);
        }
        /// <summary>
        /// 获取按键映射Json文件(注意:这部分会有直接的序列化操作)
        /// </summary>
        public static string GetBindingsJson(this PlayerInputController playerInputController)
        {
            return playerInputController.Controller.SaveBindingOverridesAsJson();
        }
        /// <summary>
        /// 设置按键映射文件(注意:这部分会有直接的反序列化操作)
        /// </summary>
        public static void SetBindings(this PlayerInputController playerInputController,string bindingsJson,bool removeExisting)
        {
            playerInputController.Controller.LoadBindingOverridesFromJson(bindingsJson,removeExisting);
        }
    }
}