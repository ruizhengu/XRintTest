using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;


public class InteractoBot : MonoBehaviour
{
    // public SceneExplore explorer;
    // public Dictionary<GameObject, InteractableObject> interactables = new Dictionary<GameObject, InteractableObject>();
    // public List<InteractionEvent> interactionEvents = new List<InteractionEvent>();
    List<InteractableObject> interactableObjects;
    public int interactionCount = 0;
    public GameObject rightController;
    // Input device references
    private InputDevice simulatedControllerDevice;
    // private InputDevice simulatedHMDDevice;
    private float gameSpeed = 3.0f; // May alter gameSpeed to speed up the test execution process
    // Movement parameters
    private float moveSpeed = 1.0f;
    private float rotateSpeed = 1.0f;
    private float updateInterval = 0.05f;
    private float timeSinceLastUpdate = 0f;
    private float interactionAngle = 5.0f; // The angle for transiting from rotation to interaction
    private float controllerMovementThreshold = 0.05f; // The distance of controller movement to continue interaction
    private enum ControllerState // Controller manipulation state
    {
        None,
        LeftController,
        RightController,
        Both,
        HMD
    }
    private ControllerState currentControllerState = ControllerState.None; // Default state
    // Queue for processing movement commands one at a time
    private Queue<KeyCommand> keyCommandQueue = new Queue<KeyCommand>();
    private bool isProcessingKeyCommands = false;
    // Struct to store key commands
    private struct KeyCommand
    {
        public Key key;
        public bool press; // true for press, false for release
        public KeyCommand(Key key, bool press)
        {
            this.key = key;
            this.press = press;
        }
    }

    void Start()
    {
        interactableObjects = GetInteractableObjects();
        RegisterListener(); // Register listeners for interactables and UIs
        FindSimulatedDevices(); // Find the simulated devices
    }

    /// <summary>
    /// Find simulator devices (i.e., controllers and HMD)
    /// </summary>
    void FindSimulatedDevices()
    {
        var devices = InputSystem.devices;
        foreach (var device in devices)
        {
            if (device.name == "XRSimulatedController")
            {
                simulatedControllerDevice = device;
                Debug.Log("Found simulated left controller: " + device.name);
                break;
            }
            // TODO: could check what does "XRSimulatedController1" do
        }
        if (simulatedControllerDevice == null)
        {
            Debug.LogWarning("Couldn't find simulated left controller device. Movement won't work.");
        }
    }

    void Update()
    {
        Time.timeScale = gameSpeed;
        InteractableObject closestInteractable = GetCloestInteractable();
        Debug.Log("Closest Interactable: " + closestInteractable.GetObject().name);
        // rightController = GameObject.Find("Right Controller");
        // if (rightController == null)
        // {
        //     return;
        // }
        // if (closestInteractable != null)
        // {
        //     GameObject closestObject = closestInteractable.GetObject();
        //     Vector3 currentPos = transform.position;
        //     Vector3 targetPos = closestObject.transform.position;
        //     // Rotation (only rotate y-axis)
        //     Vector3 targetDirection = (targetPos - currentPos).normalized;
        //     targetDirection.y = 0;
        //     float angle = Vector3.Angle(transform.forward, targetDirection);
        //     if (angle > interactionAngle)
        //     {
        //         Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        //         transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        //         return; // Don't proceed with controller actions until angle difference is small enough
        //     }
        //     // Player Movement (calculate distance ignoring Y axis)
        //     Vector3 flatCurrentPos = new Vector3(currentPos.x, 0, currentPos.z);
        //     Vector3 flatTargetPos = new Vector3(targetPos.x, 0, targetPos.z);
        //     float viewportDistance = Utils.GetUserViewportDistance(flatCurrentPos, flatTargetPos);
        //     float interactionDistance = Utils.GetInteractionDistance();
        //     if (viewportDistance > interactionDistance)
        //     {
        //         Vector3 newPosition = Vector3.MoveTowards(
        //             new Vector3(currentPos.x, currentPos.y, currentPos.z),
        //             new Vector3(targetPos.x, currentPos.y, targetPos.z),
        //             moveSpeed * Time.deltaTime
        //         );
        //         transform.position = newPosition;
        //         return; // Don't proceed with controller actions until close enough
        //     }
        //     timeSinceLastUpdate += Time.deltaTime;
        //     if (timeSinceLastUpdate >= updateInterval)
        //     {
        //         timeSinceLastUpdate = 0f;
        //         // Controller Movement
        //         Vector3 controllerCurrentPos = rightController.transform.position;
        //         Vector3 controllerTargetPos = closestObject.transform.position;
        //         Vector3 controllerWorldDirection = Utils.GetControllerWorldDirection(controllerCurrentPos, controllerTargetPos);
        //         if (Vector3.Distance(controllerCurrentPos, controllerTargetPos) > controllerMovementThreshold)
        //         {
        //             // Set to the right controller state
        //             SwitchControllerState(ControllerState.RightController);
        //             // Move towards the target
        //             MoveControllerInDirection(controllerWorldDirection.normalized);
        //         }
        //         else
        //         {
        //             closestInteractable.SetVisited(true);
        //             if (closestInteractable.GetObjectType() == "3d")
        //             {
        //                 ControllerGripAction();
        //             }
        //             else if (closestInteractable.GetObjectType() == "2d")
        //             {
        //                 // TODO: for UIs, go to the position, and go backward to forward to trigger and reset the UI element
        //                 ControllerTriggerAction();
        //             }
        //             // Wait for grip success confirmation
        //             // while (gripCheckTimer < gripCheckTimeout && !gripSuccess)
        //             // {
        //             //     gripCheckTimer += Time.deltaTime;
        //             //     // return;
        //             // }

        //             // if (gripSuccess)
        //             // {
        //             //     Debug.Log("Grip action successful - object selected");
        //             // }
        //             // else
        //             // {
        //             //     Debug.Log("Grip action failed - no object selected");
        //             // }
        //             ResetControllerPosition();
        //         }
        //     }
        // }
        // else
        // {
        //     // TODO: add report (success rate)
        //     Debug.Log("Test End");
        //     Debug.Log("Number of Interacted Interactables: " + CountInteracted() + " / " + interactionCount);
        // }
        // // Process the command queue
        // if (!isProcessingKeyCommands && keyCommandQueue.Count > 0)
        // {
        //     StartCoroutine(ProcessKeyCommandQueue());
        // }
    }

    // Ensure we're in the desired controller manipulation state
    void SwitchControllerState(ControllerState targetState)
    {
        if (currentControllerState == targetState)
            return;
        // Determine which key to press to get to the desired state
        Key key = Key.None;
        switch (targetState)
        {
            case ControllerState.LeftController:
                key = Key.LeftBracket;
                break;
            case ControllerState.RightController:
                key = Key.RightBracket;
                break;
                // case ControllerState.Both:
                //     // Press both keys simultaneously (handled specially)
                //     EnqueueKeyCommand(new KeyCommand(Key.LeftBracket, true));
                //     EnqueueKeyCommand(new KeyCommand(Key.RightBracket, true));
                //     EnqueueKeyCommand(new KeyCommand(Key.LeftBracket, false));
                //     EnqueueKeyCommand(new KeyCommand(Key.RightBracket, false));
                //     currentControllerState = targetState;
                //     return;
                // case ControllerState.HMD:
                //     key = Key.Digit0;
                //     break;
        }
        // Debug.Log("Key: " + key);
        if (key != Key.None)
        {
            // Enqueue key press and release
            EnqueueKeyCommand(new KeyCommand(key, true));
            EnqueueKeyCommand(new KeyCommand(key, false));
            currentControllerState = targetState;
        }
    }

    // Add a key command to the queue
    void EnqueueKeyCommand(KeyCommand command)
    {
        keyCommandQueue.Enqueue(command);
    }

    // Process key commands from the queue one at a time
    IEnumerator ProcessKeyCommandQueue()
    {
        isProcessingKeyCommands = true;
        while (keyCommandQueue.Count > 0)
        {
            var command = keyCommandQueue.Dequeue();
            ExecuteKeyCommand(command);
            // Small delay between commands (granularity of movement)
            yield return new WaitForSeconds(0.005f);
        }
        isProcessingKeyCommands = false;
    }

    // Execute a single key command
    void ExecuteKeyCommand(KeyCommand command)
    {
        var keyboard = InputSystem.GetDevice<Keyboard>();
        if (keyboard == null) return;
        if (command.press) // Pressing the key
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(command.key));
        }
        else // Releasing the key
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        }
    }

    /// <summary>
    /// Move the controller in the given direction using input simulation
    /// </summary>
    /// <param name="direction">Direction from the controller to the target</param>
    void MoveControllerInDirection(Vector3 direction)
    {
        // Move in the controller's local direction, rather than in the world space's direction
        Vector3 controllerForward = rightController.transform.forward;
        Vector3 controllerRight = rightController.transform.right;
        Vector3 controllerUp = rightController.transform.up;
        float zAxis = Vector3.Dot(direction, controllerForward);
        float xAxis = Vector3.Dot(direction, controllerRight);
        float yAxis = Vector3.Dot(direction, controllerUp);
        EnqueueMovementKeys(xAxis, yAxis, zAxis);
    }

    /// <summary>
    /// Enqueue movement keys based on direction
    /// Greedy approach: move to the direction with largest distance first
    /// Using key commands for movement
    /// </summary>
    void EnqueueMovementKeys(float x, float y, float z)
    {
        float threshold = controllerMovementThreshold * 0.8f;
        float absX = Mathf.Abs(x);
        float absY = Mathf.Abs(y);
        float absZ = Mathf.Abs(z);
        // Forward-first policy: move the controller towards the target first, then tweak the x and y axis
        if (absZ > threshold)
        {
            Key zKey = z > 0 ? Key.W : Key.S;
            EnqueueKeyCommand(new KeyCommand(zKey, true));
            EnqueueKeyCommand(new KeyCommand(zKey, false));
            return;
        }
        if (absX > threshold)
        {
            Key xKey = x > 0 ? Key.D : Key.A;
            EnqueueKeyCommand(new KeyCommand(xKey, true));
            EnqueueKeyCommand(new KeyCommand(xKey, false));
            return;
        }
        if (absY > threshold)
        {
            Key yKey = y > 0 ? Key.E : Key.Q;
            EnqueueKeyCommand(new KeyCommand(yKey, true));
            EnqueueKeyCommand(new KeyCommand(yKey, false));
            return;
        }
    }

    /// <summary>
    /// Reset controller position by XR Interaction Simulator shortcut
    /// </summary>
    void ResetControllerPosition()
    {
        Key resetKey = Key.R;
        EnqueueKeyCommand(new KeyCommand(resetKey, true));
        EnqueueKeyCommand(new KeyCommand(resetKey, false));
    }

    void ControllerGripAction()
    {
        // StartCoroutine(HoldGripKey());
        Key gripKey = Key.G;
        EnqueueKeyCommand(new KeyCommand(gripKey, true));
        EnqueueKeyCommand(new KeyCommand(gripKey, false));
    }

    IEnumerator HoldGripKey()
    {
        Key gripKey = Key.G;
        // Press the key
        EnqueueKeyCommand(new KeyCommand(gripKey, true));
        // Hold for a short duration (100ms)
        yield return new WaitForSeconds(0.1f);
        // Release the key
        EnqueueKeyCommand(new KeyCommand(gripKey, false));
    }

    void ControllerTriggerAction()
    {
        StartCoroutine(HoldTriggerKey());
    }

    IEnumerator HoldTriggerKey()
    {
        Key triggerKey = Key.T;
        // Press the key
        EnqueueKeyCommand(new KeyCommand(triggerKey, true));
        // Hold for a short duration (100ms)
        yield return new WaitForSeconds(0.1f);
        // Release the key
        EnqueueKeyCommand(new KeyCommand(triggerKey, false));
    }

    /// <summary>
    /// Greedy policy: move to and interact with the closest interactable based on the current position
    /// </summary>
    /// <returns></returns>
    public InteractableObject GetCloestInteractable()
    {
        InteractableObject closest = null;
        float minDistance = Mathf.Infinity;
        foreach (InteractableObject interactable in interactableObjects)
        {
            // GameObject interactable = GameObject.Find(interactionEvent.interactable);
            // InteractableObject interactable = entry.Value;
            if (!interactable.GetVisited())
            {
                float distance = Vector3.Distance(transform.position, interactable.GetObject().transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = interactable;
                }
            }
        }
        return closest;
    }

    public List<InteractableObject> GetInteractableObjects()
    {
        List<InteractionEvent> interactionEvents = Utils.GetInteractionEvents();
        List<InteractableObject> interactableObjects = new List<InteractableObject>();
        foreach (InteractionEvent interactionEvent in interactionEvents)
        {
            GameObject interactable = GameObject.Find(interactionEvent.interactable);
            if (interactable == null)
            {
                // If exact match not found, search for objects containing the name
                GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains(interactionEvent.interactable))
                    {
                        interactable = obj;
                        break;
                    }
                }
            }
            if (interactable != null)
            {
                interactableObjects.Add(new InteractableObject(interactable, interactionEvent.type));
            }
        }
        // foreach (GameObject interactable in interactableObjects)
        // {
        //     Debug.Log("Interactable: " + interactable.name);
        // }
        // Debug.Log("Number of Interactable Objects: " + interactableObjects.Count);
        return interactableObjects;
    }

    void RegisterListener()
    {
        // Register listeners for common interactable types
        foreach (var obj in interactableObjects)
        {
            var baseInteractable = obj.GetObject().GetComponent<XRBaseInteractable>();
            if (baseInteractable != null)
            {
                baseInteractable.selectEntered.AddListener(OnSelectEntered);
                baseInteractable.selectExited.AddListener(OnSelectExited);
                baseInteractable.activated.AddListener(OnActivated);
                baseInteractable.deactivated.AddListener(OnDeactivated);
            }
        }
        // Register EventTrigger listeners for UI elements
        EventTrigger[] uiTriggers = FindObjectsOfType<EventTrigger>();
        foreach (EventTrigger trigger in uiTriggers)
        {
            // Create entry for pointer enter
            // EventTrigger.Entry pointerEnterEntry = new EventTrigger.Entry();
            // pointerEnterEntry.eventID = EventTriggerType.PointerEnter;
            // pointerEnterEntry.callback.AddListener((data) => { OnPointerEnter((PointerEventData)data); });
            // trigger.triggers.Add(pointerEnterEntry);

            // // Create entry for pointer exit
            // EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry();
            // pointerExitEntry.eventID = EventTriggerType.PointerExit;
            // pointerExitEntry.callback.AddListener((data) => { OnPointerExit((PointerEventData)data); });
            // trigger.triggers.Add(pointerExitEntry);

            // Create entry for pointer click
            EventTrigger.Entry pointerClickEntry = new EventTrigger.Entry();
            pointerClickEntry.eventID = EventTriggerType.PointerClick;
            pointerClickEntry.callback.AddListener((data) => { OnPointerClick((PointerEventData)data); });
            trigger.triggers.Add(pointerClickEntry);

            // Debug.Log($"Registered UI listeners for: {trigger.gameObject.name}");
        }
    }

    void SetObjectInteracted(string interactableName)
    {
        // InteractableObject objectToRemove = null;
        foreach (var obj in interactableObjects)
        {
            if (obj.GetObject().name == interactableName)
            {
                obj.SetInteracted(true);
                // Debug.Log($"Inteacted: {interactableName} set to {entry.Value.GetInteracted()}");
                break;
            }
        }
    }

    private int CountInteracted()
    {
        int count = 0;
        foreach (var obj in interactableObjects)
        {
            if (obj.GetInteracted())
            {
                count++;
            }
            else
            {
                Debug.Log("Not Interacted Interactable: " + obj.GetObject().name);
            }
        }
        return count;
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        var xrInteractable = args.interactableObject;
        Debug.Log("OnSelectEntered: " + xrInteractable.transform.name);
        SetObjectInteracted(xrInteractable.transform.name);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        var xrInteractable = args.interactableObject;
        Debug.Log("OnSelectExited: " + xrInteractable.transform.name);
    }

    private void OnActivated(ActivateEventArgs args)
    {
        var interactable = args.interactableObject;
        Debug.Log($"OnActivated: {interactable.transform.name}");
    }

    private void OnDeactivated(DeactivateEventArgs args)
    {
        var interactable = args.interactableObject;
        Debug.Log($"OnDeactivated: {interactable.transform.name}");
    }

    private void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"Pointer entered UI: {eventData.pointerEnter.name}");
    }

    private void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"Pointer exited UI: {eventData.pointerEnter.name}");
    }

    private void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"Pointer clicked UI: {eventData.pointerEnter.name}");
        SetObjectInteracted(eventData.pointerEnter.name);
    }

    /// <summary>
    /// An example of getting the attributes of interactables for analysis/behaviour modelling, maybe useful, maybe not
    /// </summary>
    private void GetComponentAttributes()
    {
        GameObject blaster = GameObject.Find("Blaster").transform.parent.gameObject;
        Debug.Log(blaster.GetComponent<XRGrabInteractable>());
        var grabInteractable = blaster.GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            // Get the activated event
            var activatedEvent = grabInteractable.activated;
            Debug.Log($"Blaster Activate Event: {activatedEvent}");
            // You can also check if there are any listeners attached to this event
            var hasListeners = activatedEvent.GetPersistentEventCount() > 0;
            Debug.Log($"Blaster Activate Event has listeners: {hasListeners}");
            var deactivatedEvent = grabInteractable.deactivated;
            Debug.Log($"Blaster DeActivate Event: {deactivatedEvent}");
            var dehasListeners = deactivatedEvent.GetPersistentEventCount() > 0;
            Debug.Log($"Blaster DeActivate Event has listeners: {dehasListeners}");
        }
    }
}
