using EUFramework.Extension.EUInputControllerKit;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
public class InputTest : MonoBehaviour
{
    private void Start()
    {
        var ls = EUInputController.GetIdlePlayerInputControllerList();
        var ls2 = EUInputController.GetIdlePlayerInputDeviceList();
        int inputControllerCount = ls.Length;
        int inputControllerIndex = 0;
        for (int i = 0; i < ls2.Length; i++)
        {
            PlayerInputController v;
            if (inputControllerCount == 0)
            {
                v = EUInputController.GetPlayerInputController(EUInputController.AddPlayerInputController());
            }
            else
            {
                v = ls[inputControllerIndex];
                inputControllerIndex++;
                inputControllerCount--;
            }
            EUInputController.SetPlayerInputControllerOfDevice(v,ls2[i]);
            v.PlayerInputControllerEvent.AddMoveListener(Move);
        }
        EUInputController.AddPlayerInputDeviceAddedListener(PlayerInputDeviceAdded);
        EUInputController.AddPlayerInputDeviceRemovedListener(PlayerInputControllerRemoved);
    }

    private void PlayerInputDeviceAdded(InputDevice inputDevice)
    {
        var ls = EUInputController.GetIdlePlayerInputControllerList();
        if(ls.Length == 0) return;
        EUInputController.SetPlayerInputControllerOfDevice(ls[0],inputDevice);
    }

    private void PlayerInputControllerRemoved(InputDevice inputDevice)
    {
        
    }
    
    private void Move(InputAction.CallbackContext context)
    {
        Debug.Log($"{context.ToString()} : {context.ReadValue<Vector2>()}");
    }
}
#endif
