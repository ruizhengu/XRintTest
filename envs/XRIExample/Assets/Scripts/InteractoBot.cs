using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public class InteractoBot : MonoBehaviour
{
    public SceneExplore explorer;
    public InteractableIdentification interactableIdentification;
    public XRController xrController;
    public InputDevice xrControllerDevice;
    public ButtonControl selectControl;
    public AxisControl activateControl;

    void Awake()
    {
        // xrController = new XRController();
        explorer = new SceneExplore(transform);
        interactableIdentification = new InteractableIdentification();
    }

    void Start()
    {
        // var interactables = interactableIdentification.getInteractables();
        // xrController.ResigterControlers();
        ResigterListener();
        // xrControllerDevice = InputSystem.GetDevice<UnityEngine.InputSystem.XR.XRController>();
        // if (xrControllerDevice == null)
        // {
        //     Debug.LogError("No XR controller device found");
        // }
        // selectControl = xrControllerDevice.TryGetChildControl<ButtonControl>("Grip");
        // activateControl = xrControllerDevice.TryGetChildControl<AxisControl>("trigger");
    }

    void Update()
    {
        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            // xrController.SetSelectValue(1.0f);
            SetSelectValue(1.0f);
        }
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            // xrController.SetActivateValue(1.0f);
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
        // if (selectControl == null)
        // {
        //     Debug.LogError("Select Control not found");
        // }
        // else
        // {
        //     // Debug.Log($"Setting select/grip value to {value}");
        //     XRSimulatedControllerState xrLeft = new XRSimulatedControllerState();
        //     xrLeft.grip = value;
        //     xrLeft.WithButton(ControllerButton.GripButton, true);

        //     InputSystem.QueueDeltaStateEvent(selectControl, xrLeft);
        //     InputSystem.Update();
        // }

    }

    public GameObject GetCloestInteractable()
    {
        GameObject closest = null;
        float minDistance = Mathf.Infinity;
        foreach (KeyValuePair<GameObject, InteractableIdentification.InteractableInfo> entry in interactableIdentification.getInteractables())
        {
            var interactableInfo = entry.Value;
            if (!interactableInfo.GetInteractFlag())
            {
                GameObject obj = interactableInfo.GetObject();
                float distance = Vector3.Distance(transform.position, obj.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = obj;
                }
            }
        }
        return closest;
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

    void ResigterListener()
    {
        foreach (KeyValuePair<GameObject, InteractableIdentification.InteractableInfo> entry in interactableIdentification.getInteractables())
        {
            var grabInteractable = entry.Key.GetComponent<XRGrabInteractable>();
            var interactableType = entry.Value.GetObjectType();
            if (grabInteractable != null && interactableType == "3d")
            {
                grabInteractable.selectEntered.AddListener(OnSelectEntered);
                grabInteractable.selectExited.AddListener(OnSelectExited);
            }
            var baseInteractable = entry.Key.GetComponent<XRBaseInteractable>();
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
}
