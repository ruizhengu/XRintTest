using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class InteractableIdentification
{
  private static Dictionary<GameObject, InteractableObject> interactables = new();
  private static GameObject triggered;
  // private GameObject leftController;
  // private GameObject rightController;
  // private float controllerMovementStep = 0.1f;

  public InteractableIdentification()
  {
    GameObject[] gos = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
    foreach (GameObject go in gos)
    {
      // TODO seperate control and interacatable
      FetchControl(go);
      FetchInteractable(go);
    }
  }

  // public Dictionary<GameObject, ControlInfo> getControls()
  // {
  //   return controls;
  // }

  // public void MoveLeftController(Vector3 dest)
  // {
  //   leftController = GameObject.FindWithTag("LeftController");
  //   Vector3 pos = leftController.transform.position
  //   pos = Vector3.MoveTowards(
  //     pose,
  //     dest,
  //     controllerMovementStep * Time.deltaTime
  //   );
  // }

  public Dictionary<GameObject, InteractableObject> GetInteractables()
  {
    return interactables;
  }

  protected void FetchControl(GameObject go)
  {
    EventTrigger r = go.GetComponent<EventTrigger>();
    if (r != null && !interactables.ContainsKey(go))
    {
      // Debug.Log("Found Triggerable: " + go.name);
      interactables[go] = new InteractableObject(go, "2d");

      // EventTrigger.Entry entry = new EventTrigger.Entry();
      // entry.eventID = EventTriggerType.PointerClick;
      // entry.callback.AddListener((eventData) => { UpdateTrigger(); });
      // r.triggers.Add(entry);
    }
  }

  protected void FetchInteractable(GameObject go)
  {
    UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable xrInteractable = go.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
    if (xrInteractable != null && !interactables.ContainsKey(go))
    {
      // Debug.Log("Found Interactable: " + go.name);
      interactables[go] = new InteractableObject(go, "3d");
    }
  }

  public static void UpdateTrigger()
  {
    // if (controls.ContainsKey(triggered))
    // {
    //   Debug.Log("Triggered Recorded:" + controls[triggered]);
    //   controls[triggered].SetTrigger();
    // }
  }
}
