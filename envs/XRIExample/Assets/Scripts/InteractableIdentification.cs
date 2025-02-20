using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;


public class InteractableIdentification
{
  protected static Dictionary<GameObject, ControlInfo> controls = new Dictionary<GameObject, ControlInfo>();
  protected static GameObject triggered;

  public void IdentifyInteraction()
  {
    GameObject[] gos = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
    foreach (GameObject go in gos)
    {
      FetchControl(go);
      FetchInteractable(go);
    }
  }

  protected void FetchControl(GameObject go)
  {
    EventTrigger r = go.GetComponent<EventTrigger>();
    if (r != null && !controls.ContainsKey(go))
    {
      controls[go] = new ControlInfo(go);
      Debug.Log("Found Triggerable: " + go.name);
      EventTrigger.Entry entry = new EventTrigger.Entry();
      entry.eventID = EventTriggerType.PointerClick;
      entry.callback.AddListener((eventData) => { UpdateTrigger(); });
      r.triggers.Add(entry);
    }
  }

  protected void FetchInteractable(GameObject go)
  {
    UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable xrInteractable = go.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
    // if (xrInteractable != null && !controls.ContainsKey(go))
    if (xrInteractable != null)
    {
      Debug.Log("Found Interactable: " + go.name);
    }
  }

  public static void UpdateTrigger()
  {
    if (controls.ContainsKey(triggered))
    {
      Debug.Log("Triggered Recorded:" + controls[triggered]);
      controls[triggered].SetTrigger();
    }
  }

  protected class ControlInfo
  {
    GameObject control;
    int triggered;
    public ControlInfo(GameObject obj)
    {
      this.control = obj;
      this.triggered = 0;
    }
    public GameObject getObject()
    {
      return this.control;
    }
    public int getTriggered()
    {
      return this.triggered;
    }
    public void SetTrigger()
    {
      this.triggered = this.triggered + 1;
    }
  }
}
