using System;
using System.Collections;
using System.Collections.Generic;
using EUFramework.Extension.EUInputController;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputTest : MonoBehaviour
{
    private void Start()
    {
        EUInputController.Instance.GetMainPlayerInputController();
    }
}
