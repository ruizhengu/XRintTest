using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public class XRController
{
  public InputDevice xrControllerDevice;
  public AxisControl selectControl;
  public AxisControl activateControl;
  public void ResigterControlers()
  {
    xrControllerDevice = InputSystem.GetDevice<UnityEngine.InputSystem.XR.XRController>();
    if (xrControllerDevice == null)
    {
      Debug.LogError("No XR controller device found");
    }
    selectControl = xrControllerDevice.TryGetChildControl<AxisControl>("grip");
    activateControl = xrControllerDevice.TryGetChildControl<AxisControl>("trigger");
  }

  public void SetSelectValue(float value)
  {
    if (selectControl == null)
    {
      Debug.LogError("Select Control not found");
    }
    else
    {
      // Debug.Log($"Setting select/grip value to {value}");
      XRSimulatedControllerState xrLeft = new XRSimulatedControllerState();
      xrLeft.grip = value;
      InputSystem.QueueDeltaStateEvent(selectControl, xrLeft);
      InputSystem.Update();
    }
  }

  public void SetActivateValue(float value)
  {
    if (activateControl == null)
    {
      Debug.LogError("Activate Control not found");
    }
    else
    {
      // Debug.Log($"Setting activate/trigger value to {value}");
      InputSystem.QueueDeltaStateEvent(activateControl, value);
      InputSystem.Update();
    }
  }
}
