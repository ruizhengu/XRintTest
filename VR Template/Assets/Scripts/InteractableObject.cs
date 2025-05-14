using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class InteractableObject
{
  public GameObject Interactable { get; set; }

  public string Name { get; set; }

  public bool Interacted { get; set; }

  public List<string> Events { get; set; }

  public bool Intersected { get; set; }

  public bool Visited { get; set; }

  public bool IsTrigger { get; set; }

  public bool Triggered { get; set; }

  public bool Grabbed { get; set; }

  public InteractableObject(string name, GameObject go, bool isTrigger, List<string> events)
  {
    this.Name = name;
    this.Interactable = go;
    this.IsTrigger = isTrigger;
    this.Visited = false;
    this.Interacted = false;
    this.Intersected = false;
    this.Events = events;
  }

  // public string Name { get; set; }

  // public bool Interacted { get; set; }

  // public bool Intersected { get; set; }

  // public bool Visited { get; set; }

  // public bool IsTrigger { get; set; }

  // public bool Triggered { get; set; }

  // public bool Grabbed { get; set; }

  // public List<string> Events { get; set; }

  // public void SetEvents(List<string> events)
  // {
  //   this.events = events;
  // }

  // public List<string> GetEvents()
  // {
  //   return this.events;
  // }

  // public void SetName(string name)
  // {
  //   this.name = name;
  // }

  // public GameObject GetObject()
  // {
  //   return this.interactable;
  // }

  // public void SetGameObject(GameObject go)
  // {
  //   this.interactable = go;
  // }

  // public void SetInteracted(bool flag)
  // {
  //   this.interacted = flag;
  // }
  // public bool GetInteracted()
  // {
  //   return this.interacted;
  // }

  // public void SetVisited(bool flag)
  // {
  //   this.visited = flag;
  // }

  // public bool GetVisited()
  // {
  //   return this.visited;
  // }

  // public void SetIntersected(bool flag)
  // {
  //   this.intersected = flag;
  // }

  // public bool GetIntersected()
  // {
  //   return this.intersected;
  // }

  // public bool GetIsTrigger()
  // {
  //   return this.isTrigger;
  // }

  // public void SetIsTrigger(bool flag)
  // {
  //   this.isTrigger = flag;
  // }

  // public bool GetTriggered()
  // {
  //   return this.triggered;
  // }

  // public bool GetGrabbed()
  // {
  //   return this.grabbed;
  // }

  // public void SetTriggered(bool flag)
  // {
  //   this.triggered = flag;
  // }

  // public void SetGrabbed(bool flag)
  // {
  //   this.grabbed = flag;
  // }
}
