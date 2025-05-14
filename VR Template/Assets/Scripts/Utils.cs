using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SimpleJSON;
using Newtonsoft.Json;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Linq;

public static class Utils
{

  /// <summary>
  /// Find simulator devices (i.e., controllers and HMD)
  /// </summary>
  public static void FindSimulatedDevices()
  {
    InputDevice simulatedControllerDevice = null;
    var devices = InputSystem.devices;
    foreach (var device in devices)
    {
      if (device.name == "XRSimulatedController")
      {
        simulatedControllerDevice = device;
        break;
      }
      // TODO: could check what does "XRSimulatedController1" do
    }
    if (simulatedControllerDevice == null)
    {
      Debug.LogWarning("Couldn't find simulated left controller device. Movement won't work.");
    }
  }


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

  /// <summary>
  /// Get the interaction events from the interaction_results.json file
  /// </summary>
  public static List<InteractionEvent> ParseInteractionGraph()
  {
    string jsonPath = Path.Combine(Application.dataPath, "Scripts/scene_graph.json");
    using (StreamReader r = new StreamReader(jsonPath))
    {
      string json = r.ReadToEnd();
      List<InteractionEvent> interactionEvents = JsonConvert.DeserializeObject<List<InteractionEvent>>(json);
      return interactionEvents;
    }
  }

  public static void ExecuteKey(Key key)
  {
    Debug.Log("Executing key: " + key);
    var keyboard = InputSystem.GetDevice<Keyboard>();
    if (keyboard == null) return;
    InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
  }

  public static void ExecuteKeyImmediate(Key key)
  {
    var keyboard = InputSystem.GetDevice<Keyboard>();
    if (keyboard == null) return;

    // Press and release immediately
    InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
  }

  public static List<InteractableObject> GetInteractableObjects()
  {
    var interactionEvents = ParseInteractionGraph();
    var interactableObjects = new List<InteractableObject>();

    foreach (var interactionEvent in interactionEvents)
    {
      var interactable = GameObject.Find(interactionEvent.interactable);
      if (interactable == null) continue;

      if (interactionEvent.interaction_type == "trigger")
      {
        interactableObjects.Add(new InteractableObject(interactionEvent.interactable, interactable, true, new List<string> { "trigger", "grab" }));
      }
      else if (interactionEvent.interaction_type == "grab")
      {
        interactableObjects.Add(new InteractableObject(interactionEvent.interactable, interactable, false, new List<string> { "grab" }));
      }
    }
    LogInteractables(interactableObjects);
    return interactableObjects;
  }

  private static void LogInteractables(List<InteractableObject> interactables)
  {
    foreach (var interactable in interactables)
    {
      Debug.Log($"Interactable: {interactable.Name} <{string.Join(", ", interactable.Events)}> ({interactable.Interactable.name})");
    }
  }

  public static int CountInteracted(List<InteractableObject> interactableObjects)
  {
    int count = 0;
    foreach (var obj in interactableObjects)
    {
      if (obj.Interacted)
      {
        count++;
      }
      else if (!obj.Interacted && obj.Intersected)
      {
        Debug.Log("Could be a bug: " + obj.Name);
      }
      else
      {
        Debug.Log("Not Interacted Interactable: " + obj.Name);
      }
    }
    return count;
  }

  public static bool GetIntersected(GameObject target, GameObject controller)
  {
    Collider[] interactableColliders = target.GetComponentsInChildren<Collider>();
    Collider controllerCollider = controller.GetComponent<Collider>();
    if (interactableColliders.Length > 0 && controllerCollider != null)
    {
      Bounds combinedBounds = interactableColliders[0].bounds;
      for (int i = 1; i < interactableColliders.Length; i++)
      {
        combinedBounds.Encapsulate(interactableColliders[i].bounds);
      }
      return combinedBounds.Intersects(controllerCollider.bounds);
    }
    return false;
  }
}
