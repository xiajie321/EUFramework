using EUFramework.Extension.EUInputControllerKit;
using UnityEngine;
#if UNITY_EDITOR
public class InputTest : MonoBehaviour
{
    private void Start()
    {
        //----------回调注册可以在初始化前去提前注册,这样在调用非回调的方法时就会初始化已经接入的设备连接情况
        EUInputController.AddMainPlayerInputControllerChangeListener(v =>
        {
            //Debug.Log($"回调测试 {v.CurrentPlayerInputController}");
        });
        EUInputController.AddPlayerInputControllerOfDeviceChangeListener(v =>
        {
            //Debug.Log($"回调测试 {v.ChangeOfPlayerInputController}");
        });
        EUInputController.AddPlayerInputDeviceAddedListener(v=>
        {
            var ls = EUInputController.GetIdlePlayerInputControllerList();
            //Debug.Log(ls.Length);
            if (ls.Length != 0)
            {
                EUInputController.SetPlayerInputControllerOfDevice(ls[0],v);
                //Debug.Log($"回调测试 {v.deviceId}");
            }
            //Debug.Log($"回调测试 {v.deviceId}");
        });
        EUInputController.AddPlayerInputDeviceRemovedListener(v=>
        {
            //Debug.Log($"回调测试 {v.deviceId}");
        });
        Debug.Log(EUInputController.GetMainPlayerInputController());
    }
}
#endif
