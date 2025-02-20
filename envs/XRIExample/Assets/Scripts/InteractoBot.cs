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

public class InteractoBot : MonoBehaviour
{
    // Control/interaction related
    // protected static Dictionary<GameObject, string> interactables = new Dictionary<GameObject, string>();
    // public XRDeviceControllerControls xRDeviceControllerControls;
    public InputDevice xrControllerDevice;
    // Exploration related
    public SceneExplore explorer;
    public InteractableIdentification interactableIdentification;
    public Dictionary<GameObject, InteractableIdentification.InteractableInfo> interactables;
    // protected bool navStart;


    void Awake()
    {
        explorer = new SceneExplore(transform.position);
        interactableIdentification = new InteractableIdentification();
    }

    void Start()
    {
        xrControllerDevice = InputSystem.GetDevice<UnityEngine.InputSystem.XR.XRController>();
        // interactableIdentification.IdentifyInteraction();
        interactables = interactableIdentification.getInteractables();
        Debug.Log(interactables);
        if (xrControllerDevice == null)
        {
            Debug.LogError("No XR controller device found");
        }

    }

    void Update()
    {
        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            SetGripValue(1.0f);
        }
        transform.position = explorer.RandomExploration();
    }

    public void SetGripValue(float value)
    {
        AxisControl gripControl = xrControllerDevice.TryGetChildControl<AxisControl>("grip");
        if (gripControl == null)
        {
            Debug.LogError("Grip Control not found");
        }
        else
        {
            Debug.Log($"Setting grip value to {value}");
            InputSystem.QueueDeltaStateEvent(gripControl, value);
            InputSystem.Update();
        }
    }

    void getPlayerTransform()
    {
        GameObject mainCamera = GameObject.FindWithTag("MainCamera");
        // Debug.Log("mainCamera: (" + mainCamera.transform.position + ") (" + mainCamera.transform.rotation + ")");
        GameObject leftController = GameObject.FindWithTag("LeftController");
        if (leftController)
        {
            // Debug.Log("leftController: (" + leftController.transform.position + ") (" + leftController.transform.rotation + ")");
        }
        else
        {
            // Debug.Log("leftController not found");
        }
        GameObject rightController = GameObject.FindWithTag("RightController");
        if (rightController)
        {
            // Debug.Log("rightController: (" + rightController.transform.position + ") (" + rightController.transform.rotation + ")");
        }
        else
        {
            // Debug.Log("rightController not found");
        }
    }
}
