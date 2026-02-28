using EUFramework.Extension.EUInputController;
using EUFramwork.Extension.EUFSMKit;
using UnityEngine;

public class InputTest : MonoBehaviour
{
    private void Start()
    {
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
