using EUFramework.Extension.EUInputController;
using EUFramwork.Extension.EUFSMKit;
using UnityEngine;

public class InputTest : MonoBehaviour
{
    private void Start()
    {
        EUInputController.Instance.GetMainPlayerInputController();
    }
}
