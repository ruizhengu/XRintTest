using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class InteractableObject
{
  // TODO add a ''visited'' property, distinguish ''interacted'' and ''visited''
  GameObject interactable;
  bool interacted;
  bool visited;
  String type;
  public InteractableObject(GameObject go, String type)
  {
    this.interactable = go;
    this.type = type;
    this.interacted = false;
  }
  public GameObject GetObject()
  {
    return this.interactable;
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
