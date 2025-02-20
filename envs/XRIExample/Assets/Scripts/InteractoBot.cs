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
    public InputDevice xrControllerDevice;
    // Exploration related
    public SceneExplore explorer;

    void Awake()
    {
        explorer = new SceneExplore(transform);
    }

    void Start()
    {
        xrControllerDevice = InputSystem.GetDevice<UnityEngine.InputSystem.XR.XRController>();
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
        // transform.position = explorer.RandomExploration();
        GameObject targetInteractable = explorer.getCloestInteractable();
        if (targetInteractable)
        {
            var (updatePos, updateRot) = explorer.GreedyExploration(targetInteractable);
            transform.position = updatePos;
            transform.rotation = updateRot;
            // StartCoroutine(MoveAndRotate(updatePos, updateRot, 1.0f));
        }
        else
        {
            transform.position = explorer.RandomExploration();
        }
    }

    // IEnumerator MoveAndRotate(Vector3 targetPos, Quaternion targetRot, float duration)
    // {
    //     Debug.Log(transform.position);
    //     Vector3 startPos = transform.position;
    //     Quaternion startRot = transform.rotation;
    //     float elapsed = 0f;
    //     while (elapsed < duration)
    //     {
    //         transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
    //         transform.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / duration);
    //         elapsed += Time.deltaTime;
    //         yield return null;
    //     }
    //     transform.position = targetPos;
    //     transform.rotation = targetRot;
    // }

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
