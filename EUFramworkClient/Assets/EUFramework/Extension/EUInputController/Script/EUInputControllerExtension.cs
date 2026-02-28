using Unity.Collections;
using UnityEngine.InputSystem;

namespace EUFramework.Extension.EUInputControllerKit
{
    public static class EUInputControllerExtension
    {
        /// <summary>
        /// 设置玩家输入控制器的输入设备
        /// </summary>
        /// <param name="playerInputController"></param>
        /// <param name="inputDevice"></param>
        public static void SetPlayerInputControllerOfDevice(this PlayerInputController playerInputController,
            InputDevice inputDevice)
        {
            EUInputController.SetPlayerInputControllerOfDevice(playerInputController, inputDevice);
        }
        /// <summary>
        /// 获取玩家输入控制器的Id
        /// </summary>
        /// <param name="playerInputController"></param>
        /// <returns></returns>
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
    }
}