using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class Utils
{
  // public static void StartSelect()
  // {
  //   var device = InputSystem.GetDevice<Keyboard>();
  //   InputSystem.QueueStateEvent(device, new KeyboardState(Key.G));
  // }

  // public static void ActivateOnce()
  // {
  //   var device = InputSystem.GetDevice<Mouse>();
  //   InputSystem.QueueStateEvent(device, new MouseState().WithButton(MouseButton.Left));
  //   InputSystem.QueueStateEvent(device, new MouseState().WithButton(MouseButton.Left, false));
  // }

  // public static void SwitchDeviceStateHMD()
  // {
  //   var device = InputSystem.GetDevice<Keyboard>();
  //   InputSystem.QueueStateEvent(device, new KeyboardState(Key.U));
  // }

  // public static void SwitchDeviceStateLeftController()
  // {
  //   var device = InputSystem.GetDevice<Keyboard>();
  //   InputSystem.QueueStateEvent(device, new KeyboardState(Key.T));
  // }

  // public static void SwitchDeviceStateRightController()
  // {
  //   var device = InputSystem.GetDevice<Keyboard>();
  //   InputSystem.QueueStateEvent(device, new KeyboardState(Key.Y));
  // }

  public static Dictionary<GameObject, InteractableObject> GetInteractables()
  {
    Dictionary<GameObject, InteractableObject> interactables = new Dictionary<GameObject, InteractableObject>();
    GameObject[] gos = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
    foreach (GameObject go in gos)
    {
      EventTrigger trigger = go.GetComponent<EventTrigger>();
      UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable xrInteractable =
        go.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();

      if (trigger != null && !interactables.ContainsKey(go))
      {
        // interactables[go] = new InteractableObject(go, "2d");
      }
      else if (xrInteractable != null && !interactables.ContainsKey(go))
      {
        interactables[go] = new InteractableObject(go, "3d");
      }
    }
    return interactables;
  }
}
