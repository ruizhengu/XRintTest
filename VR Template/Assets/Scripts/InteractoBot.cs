using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public List<Utils.InteractableObject> interactableObjects;
    public int interactionCount = 0;
    public GameObject rightController;
    private float gameSpeed = 2.0f; // May alter gameSpeed to speed up the test execution process
    // Movement parameters
    private float moveSpeed = 1.0f;
    private float rotateSpeed = 1.0f;
    private float updateInterval = 0.05f;
    private float timeSinceLastUpdate = 0f;
    private float interactionAngle = 5.0f; // The angle for transiting from rotation to interaction
    private float controllerMovementThreshold = 0.05f; // The distance of controller movement to continue interaction
    private float interactionOffset = 0.05f; // Small distance in front of the target for interaction
    private float stateTransitionDelay = 0.1f; // Delay between state transitions
    private bool isControllerMoving = false; // Flag to track if controller is currently moving
    private ControllerState currentControllerState = ControllerState.None; // Default state
    private ExplorationState currentExplorationState = ExplorationState.Navigation; // Default state
    private bool isMovedController = false; // Track if controller has been moved
    private float lastInteractionTime = 0f;
    private float interactionCooldown = 0.2f; // Cooldown period in seconds
    private int triggerActionCount = 0;
    private string current3DInteractionPattern = ""; // Store the current 3D interaction pattern
    private bool isGrabHeld = false; // Track if grab is currently held
    private int grabActionCount = 0; // Track number of grab actions
    private int combinedActionCount = 0; // Track number of combined actions
    private float reportInterval = 30f; // Report interval in seconds
    private float reportTimer = 0f; // Timer for report interval
    private float totalTime = 0f; // Total time of the test
    private float minuteCount = 0.5f;
    private float timeBudget = 600f; // 10 minutes time budget in seconds
    private float startTime; // Time when the program started
    private bool isTimeBudgetExceeded = false; // Flag to track if time budget is exceeded
    private enum ControllerState // Controller manipulation state
    {
        None,
        LeftController,
        RightController,
        Both,
        HMD
    }
    private enum ExplorationState
    {
        // None,
        Navigation,
        ControllerMovement,
        ThreeDInteraction,
        TwoDInteraction
    }

    void Start()
    {
        interactableObjects = Utils.GetInteractableObjects();
        interactionCount = Utils.GetInteractableEventsCount(interactableObjects);
        RegisterListeners();
        Utils.FindSimulatedDevices(); // Find the simulated devices
    }

    void Update()
    {
        Time.timeScale = gameSpeed;
        reportTimer += Time.deltaTime;
        totalTime += Time.deltaTime;
        if (reportTimer >= reportInterval)
        {
            int currentInteracted = Utils.CountInteracted(interactableObjects);
            Debug.Log($"Current Interacted {minuteCount}m: {currentInteracted} / {interactionCount} ({currentInteracted / interactionCount * 100}%)");
            minuteCount += 0.5f;
            reportTimer = 0f;
        }
        if (!isTimeBudgetExceeded && Time.time - startTime >= timeBudget)
        {
            isTimeBudgetExceeded = true;
            Debug.Log($"Time budget exceeded. Stopping script execution.");
            int currentInteracted = Utils.CountInteracted(interactableObjects, true);
            Debug.Log($"Interaction Results: {currentInteracted} / {interactionCount} ({currentInteracted / interactionCount * 100}%)");
            this.enabled = false;
            return;
        }
        // Handle different exploration states
        switch (currentExplorationState)
        {
            case ExplorationState.Navigation:
                Navigation();
                break;
            case ExplorationState.ControllerMovement:
                ControllerMovement();
                break;
            case ExplorationState.ThreeDInteraction:
                ThreeDInteraction();
                break;
            case ExplorationState.TwoDInteraction:
                TwoDInteraction();
                break;
        }
    }

    /// <summary>
    /// Handle navigation state - move towards the closest interactable
    /// </summary>
    private void Navigation()
    {
        Utils.InteractableObject closestInteractable = GetCloestInteractable();
        if (closestInteractable == null)
        {
            if (interactableObjects.All(obj => obj.Visited) &&
                (isGrabHeld || grabActionCount > 0 || combinedActionCount > 0))
            {
                return; // Don't end the test yet, let the interaction complete
            }
            Debug.Log($"Test End: execution time {totalTime}s");
            int currentInteracted = Utils.CountInteracted(interactableObjects, true);
            Debug.Log($"Number of Interacted Interactables: {currentInteracted} / {interactionCount} ({currentInteracted / interactionCount * 100}%)");
            this.enabled = false;
            return;
        }
        ResetControllerPosition();

        GameObject closestObject = closestInteractable.Interactable;
        Vector3 currentPos = transform.position;
        Vector3 targetPos = closestObject.transform.position;

        // Rotation (only rotate y-axis)
        Vector3 targetDirection = (targetPos - currentPos).normalized;
        targetDirection.y = 0;
        float angle = Vector3.Angle(transform.forward, targetDirection);
        if (angle > interactionAngle)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
            return; // Don't proceed with controller actions until angle difference is small enough
        }

        // Player Movement (calculate distance ignoring Y axis)
        Vector3 flatCurrentPos = new Vector3(currentPos.x, 0, currentPos.z);
        Vector3 flatTargetPos = new Vector3(targetPos.x, 0, targetPos.z);
        float viewportDistance = Utils.GetUserViewportDistance(flatCurrentPos, flatTargetPos);
        float interactionDistance = Utils.GetInteractionDistance();
        if (viewportDistance > interactionDistance)
        {
            Vector3 newPosition = Vector3.MoveTowards(
                new Vector3(currentPos.x, currentPos.y, currentPos.z),
                new Vector3(targetPos.x, currentPos.y, targetPos.z),
                moveSpeed * Time.deltaTime
            );
            transform.position = newPosition;
            return; // Don't proceed with controller actions until close enough
        }
        // If we're close enough, switch to controller movement with delay
        StartCoroutine(TransitionToState(ExplorationState.ControllerMovement));
    }

    /// <summary>
    /// Handle controller movement state - move the controller to the target
    /// </summary>
    private void ControllerMovement()
    {
        rightController = GameObject.Find("Right Controller");
        if (rightController == null)
        {
            return;
        }

        Utils.InteractableObject closestInteractable = GetCloestInteractable();
        if (closestInteractable == null)
        {
            StartCoroutine(TransitionToState(ExplorationState.Navigation));
            return;
        }

        GameObject closestObject = closestInteractable.Interactable;
        timeSinceLastUpdate += Time.deltaTime;
        if (timeSinceLastUpdate >= updateInterval)
        {
            timeSinceLastUpdate = 0f;
            // Controller Movement
            Vector3 controllerCurrentPos = rightController.transform.position;
            Vector3 controllerTargetPos = closestObject.transform.position;
            // Add offset in front of the target in the z axis
            controllerTargetPos += new Vector3(0, 0, interactionOffset);

            Vector3 controllerWorldDirection = Utils.GetControllerWorldDirection(controllerCurrentPos, controllerTargetPos);
            float distanceToTarget = Vector3.Distance(controllerCurrentPos, controllerTargetPos);

            if (distanceToTarget > controllerMovementThreshold)
            {
                // Set to the right controller state
                SwitchControllerState(ControllerState.RightController);
                // Move towards the target
                MoveControllerInDirection(controllerWorldDirection.normalized);
                isControllerMoving = true;
            }
            else
            {
                if (isControllerMoving) // Only proceed if the controller has stopped moving
                {
                    isControllerMoving = false;
                    closestInteractable.Visited = true;
                    var events = closestInteractable.Events;
                    current3DInteractionPattern = string.Join(",", events);
                    bool intersection = Utils.GetIntersected(closestInteractable.Interactable, rightController);
                    closestInteractable.Intersected = intersection;
                    StartCoroutine(TransitionToState(ExplorationState.ThreeDInteraction));
                }
            }
        }
    }

    /// <summary>
    /// Handle 3D interaction state
    /// </summary>
    private void ThreeDInteraction()
    {
        // Grab and trigger action
        if (current3DInteractionPattern.Contains("grab") && current3DInteractionPattern.Contains("trigger"))
        {
            if (!isGrabHeld && grabActionCount == 0 && combinedActionCount == 0)
            {
                StartCoroutine(HoldGrabAndTrigger());
            }
        }
        // Normal grab action
        else if (current3DInteractionPattern.Contains("grab"))
        {
            if (grabActionCount < 2)
            {
                ControllerGrabAction();
                grabActionCount++;
                if (grabActionCount >= 2)
                {
                    StartCoroutine(TransitionToState(ExplorationState.Navigation));
                }
            }
        }
    }

    private IEnumerator HoldGrabAndTrigger()
    {
        if (isGrabHeld) yield break;
        isGrabHeld = true;
        var keyboard = InputSystem.GetDevice<Keyboard>();
        if (keyboard == null) yield break;
        // Hold grab
        if (grabActionCount == 0)
        {
            yield return new WaitForSeconds(0.5f); // Wait a moment to ensure grab is registered
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.G));
            grabActionCount++;
            yield return new WaitForSeconds(0.5f);
        }
        // Execute trigger action while grab is held
        if (grabActionCount > 0 && combinedActionCount == 0)
        {
            yield return new WaitForSeconds(0.5f);
            Key[] keys = { Key.T, Key.G };
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            combinedActionCount++;
            yield return new WaitForSeconds(0.5f);
        }
        // Keep grab held after trigger
        if (grabActionCount > 0 && combinedActionCount > 0)
        {
            yield return new WaitForSeconds(0.5f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.G));
            yield return new WaitForSeconds(0.5f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            isGrabHeld = false;
            StartCoroutine(TransitionToState(ExplorationState.Navigation));
        }
    }

    /// <summary>
    /// Handle 2D interaction state
    /// </summary>
    private void TwoDInteraction()
    {
        // Check cool down time since the last interaction
        if (Time.time - lastInteractionTime < interactionCooldown)
        {
            return;
        }
        // Only perform the trigger action if haven't completed two actions
        if (triggerActionCount < 2)
        {
            ControllerTriggerAction();
            triggerActionCount++;
            lastInteractionTime = Time.time;
            // Only transition to Navigation after two trigger actions are completed
            if (triggerActionCount == 2)
            {
                StartCoroutine(TransitionToState(ExplorationState.Navigation));
            }
        }
    }

    /// <summary>
    /// Ensure we're in the desired controller manipulation state
    /// </summary>
    void SwitchControllerState(ControllerState targetState)
    {
        if (currentControllerState == targetState)
            return;
        Key key = Key.None;
        switch (targetState)
        {
            case ControllerState.LeftController:
                key = Key.LeftBracket;
                break;
            case ControllerState.RightController:
                key = Key.RightBracket;
                break;
        }
        if (key != Key.None)
        {
            StartCoroutine(ExecuteKeyWithDuration(key, 0.1f));
            currentControllerState = targetState;
        }
    }

    IEnumerator ExecuteKeyWithDuration(Key key, float duration)
    {
        var keyboard = InputSystem.GetDevice<Keyboard>();
        if (keyboard == null) yield break;
        // Press the key
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        // Wait for the specified duration
        yield return new WaitForSeconds(duration);
        // Release the key
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
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
        if (isMovedController) return;
        float threshold = controllerMovementThreshold;
        float absX = Mathf.Abs(x);
        float absY = Mathf.Abs(y);
        float absZ = Mathf.Abs(z);
        // Forward-first policy: move the controller towards the target first, then tweak the x and y axis
        if (absZ > threshold)
        {
            Key zKey = z > 0 ? Key.W : Key.S;
            StartCoroutine(ExecuteKeyWithDuration(zKey, 0.01f));
            return;
        }
        if (absX > threshold)
        {
            Key xKey = x > 0 ? Key.D : Key.A;
            StartCoroutine(ExecuteKeyWithDuration(xKey, 0.01f));
            return;
        }
        if (absY > threshold)
        {
            Key yKey = y > 0 ? Key.E : Key.Q;
            StartCoroutine(ExecuteKeyWithDuration(yKey, 0.01f));
            return;
        }
        isMovedController = true;
    }

    /// <summary>
    /// Reset controller position by XR Interaction Simulator shortcut
    /// </summary>
    void ResetControllerPosition()
    {
        Key resetKey = Key.R;
        StartCoroutine(ExecuteKeyWithDuration(resetKey, 0.1f));
    }

    void ControllerGrabAction()
    {
        // Debug.Log("Controller Grab Action");
        Key grabKey = Key.G;
        StartCoroutine(ExecuteKeyWithDuration(grabKey, 0.1f));
    }

    void ControllerTriggerAction()
    {
        Key triggerKey = Key.T;
        StartCoroutine(ExecuteKeyWithDuration(triggerKey, 0.1f));
    }

    /// <summary>
    /// Greedy policy: move to and interact with the closest interactable based on the current position
    /// </summary>
    /// <returns></returns>
    public Utils.InteractableObject GetCloestInteractable()
    {
        Utils.InteractableObject closest = null;
        float minDistance = Mathf.Infinity;
        foreach (Utils.InteractableObject interactable in interactableObjects)
        {
            if (!interactable.Visited)
            {
                float distance = Vector3.Distance(transform.position, interactable.Interactable.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = interactable;
                }
            }
        }
        return closest;
    }

    void RegisterListeners()
    {
        foreach (var obj in interactableObjects)
        {
            var baseInteractable = obj.Interactable.GetComponent<XRBaseInteractable>();
            if (baseInteractable != null)
            {
                baseInteractable.selectEntered.AddListener(OnSelectEntered);
                baseInteractable.activated.AddListener(OnActivated);
            }
        }
    }

    void SetObjectGrabbed(string interactableName)
    {
        foreach (var obj in interactableObjects)
        {
            if (obj.Interactable.name == interactableName && !obj.Interacted)
            {
                obj.Grabbed = true;
                if (!obj.IsTrigger)
                {
                    obj.Interacted = true;
                }
                Debug.Log("Grabbed: " + obj.Name + " " + obj.Interactable.name);
                break;
            }
        }
    }

    void SetObjectTriggered(string interactableName)
    {
        foreach (var obj in interactableObjects)
        {
            if (obj.Interactable.name == interactableName && !obj.Interacted)
            {
                obj.Triggered = true;
                if (obj.Grabbed)
                {
                    obj.Interacted = true;
                }
                Debug.Log("Triggered: " + obj.Name + " " + obj.Interactable.name);
                break;
            }
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        var xrInteractable = args.interactableObject;
        // Debug.Log("OnSelectEntered: " + xrInteractable.transform.name);
        SetObjectGrabbed(xrInteractable.transform.name);
    }

    private void OnActivated(ActivateEventArgs args)
    {
        var interactable = args.interactableObject;
        // Debug.Log($"OnActivated: {interactable.transform.name}");
        SetObjectTriggered(interactable.transform.name);
    }

    /// <summary>
    /// Transition to a new state with a delay
    /// </summary>
    private IEnumerator TransitionToState(ExplorationState newState)
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(stateTransitionDelay);
        // Reset the action flags when transitioning to a new state
        if (newState != ExplorationState.ThreeDInteraction)
        {
            current3DInteractionPattern = ""; // Clear the interaction pattern when leaving 3D state
            isGrabHeld = false; // Ensure grab is released when leaving state
            grabActionCount = 0; // Reset grab action count
            combinedActionCount = 0; // Reset combined action count
        }
        if (newState != ExplorationState.TwoDInteraction)
        {
            triggerActionCount = 0; // Reset trigger action count when leaving 2D interaction state
        }
        currentExplorationState = newState;
    }

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
            activatedEvent.AddListener((args) =>
            {
                Debug.Log("Blaster activated event was fired!");
            });
            // You can also check if there are any listeners attached to this event
            var hasListeners = activatedEvent.GetPersistentEventCount() > 0;
            Debug.Log($"Blaster Activate Event has listeners: {hasListeners}");
            for (int i = 0; i < activatedEvent.GetPersistentEventCount(); i++)
            {
                var target = activatedEvent.GetPersistentTarget(i);
                var method = activatedEvent.GetPersistentMethodName(i);
                Debug.Log($"Activate Listener {i}: Target={target}, Method={method}");
            }
        }
    }
}
