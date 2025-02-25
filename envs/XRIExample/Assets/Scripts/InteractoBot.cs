using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class InteractoBot : MonoBehaviour
{
    public SceneExplore explorer;
    public InteractableIdentification interactableIdentification;
    public Dictionary<GameObject, InteractableIdentification.InteractableInfo> interactables = new Dictionary<GameObject, InteractableIdentification.InteractableInfo>();
    void Awake()
    {
        explorer = new SceneExplore(transform);
        interactableIdentification = new InteractableIdentification();
    }

    void Start()
    {
        interactables = interactableIdentification.GetInteractables();
        ResigterListener();
    }

    void Update()
    {

        // transform.position = explorer.RandomExploration();
        // SetSelectValue(1.0f);
        // SetActivateValue(1.0f);

        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            Utils.StartSelect();
        }
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            StartCoroutine(ActivateAndRelease(0.5f));
        }
        // transform.position = explorer.RandomExploration();
        GameObject targetInteractable = GetCloestInteractable();
        if (targetInteractable)
        {
            var (updatePos, updateRot) = explorer.GreedyExploration(targetInteractable);
            transform.position = updatePos;
            transform.rotation = updateRot;
            // Debug.Log(targetInteractable.name + " visited");
            // StartCoroutine(MoveAndRotate(targetInteractable, 0.5f));

            // interactables[targetInteractable].SetInteractFlag(true);
        }
        else
        {
            Debug.Log("All interactables are interacted. Test stop.");
        }
    }

    IEnumerator MoveAndRotate(GameObject target, float duration)
    {
        var targetPositionOffset = 1.0f;
        Vector3 targetForward = target.transform.forward;
        targetForward.y = 0f;
        targetForward.Normalize();
        var destPos = new Vector3(
            target.transform.position.x - targetForward.x * targetPositionOffset,
            transform.position.y,
            target.transform.position.z - targetForward.z * targetPositionOffset
        );
        var destRot = target.transform.rotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // TODO only rotate y axis to avoid the agent ''falling''
            transform.position = Vector3.MoveTowards(transform.position, destPos, elapsed / duration);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, destRot, elapsed / duration);
            elapsed += Time.deltaTime;
            if (transform.position == destPos && transform.rotation == destRot)
            {
                Debug.Log(target.name + " visited");
                interactables[target].SetVisited(true);
            }
            yield return null;
        }
        transform.position = destPos;
        transform.rotation = destRot;
    }

    IEnumerator ActivateAndRelease(float duration)
    {
        var device = InputSystem.GetDevice<Mouse>();
        InputSystem.QueueStateEvent(device, new MouseState().WithButton(MouseButton.Left));
        yield return new WaitForSeconds(duration);
        InputSystem.QueueStateEvent(device, new MouseState().WithButton(MouseButton.Left, false));
    }

    // public void SetSelectValue(float value)
    // {
    //     var device = InputSystem.GetDevice<Keyboard>();
    //     InputSystem.QueueStateEvent(device, new KeyboardState(Key.G));
    // }

    // public void SetActivateValue(float value)
    // {
    //     var device = InputSystem.GetDevice<Mouse>();
    //     InputSystem.QueueStateEvent(device, new MouseState().WithButton(MouseButton.Left));
    // }

    public GameObject GetCloestInteractable()
    {
        GameObject closest = null;
        float minDistance = Mathf.Infinity;
        foreach (KeyValuePair<GameObject, InteractableIdentification.InteractableInfo> entry in interactables)
        {
            var interactableInfo = entry.Value;
            if (!interactableInfo.GetVisited())
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
        foreach (KeyValuePair<GameObject, InteractableIdentification.InteractableInfo> entry in interactables)
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
