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
  public InteractableObject(string name, GameObject go, string type)
  {
    this.name = name;
    this.interactable = go;
    this.type = type;
    this.visited = false;
    this.interacted = false;
  }

  public string GetName()
  {
    return this.name;
  }

  public void SetName(string name)
  {
    this.name = name;
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
