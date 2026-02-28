using EUFramework.Extension.EUInputController;
using EUFramwork.Extension.EUFSMKit;
using UnityEngine;

public class InputTest : MonoBehaviour
{
    private void Start()
    {
        EUInputController.Instance.AddMainPlayerInputControllerChangeListener(v =>
        {
            Debug.Log($"回调测试 {v.CurrentPlayerInputController}");
        });
        EUInputController.Instance.AddPlayerInputControllerOfDeviceChangeListener(v =>
        {
            Debug.Log($"回调测试 {v.ChangeOfPlayerInputController}");
        });
        EUInputController.Instance.AddPlayerInputDeviceAddedListener(v=>
        {
            var ls = EUInputController.Instance.GetIdlePlayerInputControllerList();
            if (ls.Length != 0)
            {
                EUInputController.Instance.SetPlayerInputControllerOfDevice(ls[0],v);
                Debug.Log($"回调测试 {v.deviceId}");
            }
            Debug.Log($"回调测试 {v.deviceId}");
        });
        EUInputController.Instance.AddPlayerInputDeviceRemovedListener(v=>
        {
            Debug.Log($"回调测试 {v.deviceId}");
        });
        EUInputController.Instance.GetMainPlayerInputController();
    }
}
