using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class InteractableObject
{
  GameObject interactable;
  string name;
  bool interacted;
  bool visited;
  string type;
  string eventType;
  string condition;
  public InteractableObject(GameObject go, string type)
  {
    this.interactable = go;
    this.type = type;
    this.visited = false;
    this.interacted = false;
  }

  public GameObject GetObject()
  {
    return this.interactable;
  }

  public void SetGameObject(GameObject go)
  {
    this.interactable = go;
  }

  public void SetInteracted(bool flag)
  {
    this.interacted = flag;
  }
  public bool GetInteracted()
  {
    return this.interacted;
  }

  public void SetVisited(bool flag)
  {
    this.visited = flag;
  }

  public bool GetVisited()
  {
    return this.visited;
  }

  public String GetObjectType()
  {
    return this.type;
  }
}
