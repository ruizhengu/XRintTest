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
  /// <summary>
  /// Get the interaction distance considering resolution
  /// </summary>
  /// <returns></returns>
  public static float GetInteractionDistance()
  {
    float interactionDistance = 0.5f; // The distance for transiting from movement to interaction
    float dpiScale = Screen.dpi / 96f; // Normalize to 96DPI base
    float adjustedInteractionDistance = interactionDistance * dpiScale; // Ajusted distance based on the screen size
    return adjustedInteractionDistance;
  }

  /// <summary>
  /// Get the distance between user and target considering resolution (ignore y axis)
  /// </summary>
  /// <param name="currentPos"></param>
  /// <param name="targetPos"></param>
  /// <returns></returns>
  public static float GetUserViewportDistance(Vector3 currentPos, Vector3 targetPos)
  {
    Vector3 currentViewport = Camera.main.WorldToViewportPoint(currentPos);
    Vector3 targetViewport = Camera.main.WorldToViewportPoint(targetPos);
    float distanceToTarget = Vector3.Distance(currentPos, targetPos);
    float viewportDistance = Vector2.Distance(
        new Vector2(currentViewport.x, currentViewport.z),
        new Vector2(targetViewport.x, targetViewport.z)
    );
    return viewportDistance;
  }


  /// <summary>
  /// Get the direction from controller to target considering resolution
  /// </summary>
  /// <param name="currentPos"></param>
  /// <param name="targetPos"></param>
  /// <returns></returns>
  public static Vector3 GetControllerWorldDirection(Vector3 currentPos, Vector3 targetPos)
  {
    // Convert to viewport space for resolution independence
    Vector3 controllerCurrentViewport = Camera.main.WorldToViewportPoint(currentPos);
    Vector3 controllerTargetViewport = Camera.main.WorldToViewportPoint(targetPos);
    Vector3 viewportDirection = controllerTargetViewport - controllerCurrentViewport;
    Vector3 worldDirection = Camera.main.ViewportToWorldPoint(controllerCurrentViewport + viewportDirection.normalized * Time.deltaTime) - currentPos;
    return worldDirection;
  }



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
