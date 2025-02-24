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
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class InteractoBot : MonoBehaviour
{
    public InputDevice xrControllerDevice;
    public SceneExplore explorer;
    public InteractableIdentification interactableIdentification;

    void Awake()
    {
        // explorer = new SceneExplore(transform);
        interactableIdentification = new InteractableIdentification();
    }

    void Start()
    {
        // var interactables = interactableIdentification.getInteractables();
        ResigterListener();
        xrControllerDevice = InputSystem.GetDevice<UnityEngine.InputSystem.XR.XRController>();
        if (xrControllerDevice == null)
        {
            Debug.LogError("No XR controller device found");
        }

    }

    void ResigterListener()
    {
        foreach (KeyValuePair<GameObject, InteractableIdentification.InteractableInfo> entry in interactableIdentification.getInteractables())
        {
            var grabInteractable = entry.Key.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            var interactableType = entry.Value.GetObjectType();
            if (grabInteractable != null && interactableType == "3d")
            {
                grabInteractable.selectEntered.AddListener(OnSelectEntered);
                grabInteractable.selectExited.AddListener(OnSelectExited);
            }
            var baseInteractable = entry.Key.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
            if (baseInteractable != null && interactableType == "3d")
            {
                baseInteractable.activated.AddListener(OnActivated);
                baseInteractable.deactivated.AddListener(OnDeactivated);
            }
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        var xrInteractable = args.interactableObject as UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable;
        Debug.Log("OnSelectEntered: " + xrInteractable.gameObject.name);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        var xrInteractable = args.interactableObject as UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable;
        Debug.Log("OnSelectExited: " + xrInteractable.gameObject.name);
    }

    private void OnActivated(ActivateEventArgs args)
    {
        var xrInteractable = args.interactableObject as UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable;
        Debug.Log("OnActivated: " + xrInteractable.gameObject.name);
    }

    private void OnDeactivated(DeactivateEventArgs args)
    {
        var xrInteractable = args.interactableObject as UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable;
        Debug.Log("OnDeactivated: " + xrInteractable.gameObject.name);
    }

    void Update()
    {
        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            SetSelectValue(1.0f);
        }
        // transform.position = explorer.RandomExploration();
        // GameObject targetInteractable = explorer.getCloestInteractable();
        // if (targetInteractable)
        // {
        //     var (updatePos, updateRot) = explorer.GreedyExploration(targetInteractable);
        //     transform.position = updatePos;
        //     transform.rotation = updateRot;
        //     // StartCoroutine(MoveAndRotate(updatePos, updateRot, 1.0f));
        // }
        // else
        // {
        //     transform.position = explorer.RandomExploration();
        // }
    }

    public void SetSelectValue(float value)
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

    public void SetActivateValue(float value)
    {

    }

    void GetPlayerTransform()
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
