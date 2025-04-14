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

// public enum XRDeviceState { HMD, LeftController, RightController };

public class InteractoBot : MonoBehaviour
{
    public SceneExplore explorer;
    public InteractableIdentification interactableIdentification;
    // public ControllerAction controllerAction;
    public Dictionary<GameObject, InteractableIdentification.InteractableInfo> interactables = new Dictionary<GameObject, InteractableIdentification.InteractableInfo>();
    // public XRDeviceState deviceState = XRDeviceState.HMD;
    public GameObject leftController;
    public GameObject rightController;
    public GameObject cubeInteractable;

    // Input device references
    private InputDevice simulatedLeftControllerDevice;
    private InputDevice simulatedHMDDevice;
    private float gameSpeed = 3.0f; // May alter gameSpeed to speed up the test execution process
    // Movement parameters
    private float moveSpeed = 3.0f;
    private float updateInterval = 0.05f;
    private float timeSinceLastUpdate = 0f;
    private float interactionDistance = 2.0f; // The distance for transiting from user movement to controller interaction
    // Controller manipulation state
    private enum ControllerManipulationState
    {
        None,
        LeftController,
        RightController,
        Both,
        HMD
    }

    // Current state of controller manipulation
    private ControllerManipulationState currentManipulationState = ControllerManipulationState.None;

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

    void Awake()
    {
        explorer = new SceneExplore(transform);
        interactableIdentification = new InteractableIdentification();
        // controllerAction = new ControllerAction("left");
    }

    void Start()
    {
        interactables = interactableIdentification.GetInteractables();
        ResigterListener();
        // Find the simulated devices
        FindSimulatedDevices();
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
                simulatedLeftControllerDevice = device;
                Debug.Log("Found simulated left controller: " + device.name);
            }
            // TODO: could check what does "XRSimulatedController1" do
            else if (device.name == "XRSimulatedHMD")
            {
                simulatedHMDDevice = device;
                Debug.Log("Found simulated HMD: " + device.name);
            }
        }
        if (simulatedLeftControllerDevice == null)
        {
            Debug.LogWarning("Couldn't find simulated left controller device. Movement won't work.");
        }
    }

    void Update()
    {
        Time.timeScale = gameSpeed;
        cubeInteractable = GameObject.Find("Cube Interactable");
        rightController = GameObject.Find("Right Controller");
        if (cubeInteractable != null)
        {
            Vector3 currentPos = transform.position;
            Vector3 targetPos = cubeInteractable.transform.position;
            float distanceToTarget = Vector3.Distance(currentPos, targetPos);
            // Move InteractoBot towards the target if too far from it
            if (distanceToTarget > interactionDistance)
            {
                Vector3 direction = (targetPos - currentPos).normalized;
                transform.position = Vector3.MoveTowards(currentPos, targetPos, moveSpeed * Time.deltaTime);
                Debug.DrawLine(currentPos, targetPos, Color.blue, Mathf.Infinity);
                return; // Don't proceed with controller actions until close enough
            }

            // Once it's close enough, proceed with controller actions
            timeSinceLastUpdate += Time.deltaTime;
            if (timeSinceLastUpdate >= updateInterval)
            {
                timeSinceLastUpdate = 0f;
                if (rightController != null)
                {
                    // Calculate direction to move
                    Vector3 controllerCurrentPos = rightController.transform.position;
                    Vector3 controllerTargetPos = cubeInteractable.transform.position;
                    Vector3 direction = (controllerTargetPos - controllerCurrentPos).normalized;
                    Debug.DrawLine(controllerCurrentPos, controllerCurrentPos + direction * 10, Color.red, Mathf.Infinity);

                    // Only move if not already at the target
                    if (Vector3.Distance(controllerCurrentPos, controllerTargetPos) > 0.1f)
                    {
                        // Set to the left controller manipulation state
                        EnsureControllerManipulationState(ControllerManipulationState.RightController);
                        // Move towards the target
                        MoveControllerInDirection(direction);
                    }
                    else
                    {
                        ControllerGripAction();
                        ResetControllerPosition();
                    }
                }
            }
        }

        // Process the command queue
        if (!isProcessingKeyCommands && keyCommandQueue.Count > 0)
        {
            StartCoroutine(ProcessKeyCommandQueue());
        }
    }

    // Ensure we're in the desired controller manipulation state
    void EnsureControllerManipulationState(ControllerManipulationState targetState)
    {
        if (currentManipulationState == targetState)
            return;

        // Determine which key to press to get to the desired state
        Key key = Key.None;

        switch (targetState)
        {
            case ControllerManipulationState.LeftController:
                key = Key.LeftBracket;
                break;
            case ControllerManipulationState.RightController:
                key = Key.RightBracket;
                break;
            case ControllerManipulationState.Both:
                // Press both keys simultaneously (handled specially)
                EnqueueKeyCommand(new KeyCommand(Key.LeftBracket, true));
                EnqueueKeyCommand(new KeyCommand(Key.RightBracket, true));
                EnqueueKeyCommand(new KeyCommand(Key.LeftBracket, false));
                EnqueueKeyCommand(new KeyCommand(Key.RightBracket, false));
                currentManipulationState = targetState;
                return;
                // case ControllerManipulationState.HMD:
                //     key = Key.Digit0;
                //     break;
        }
        Debug.Log("Key: " + key);
        if (key != Key.None)
        {
            // Enqueue key press and release
            EnqueueKeyCommand(new KeyCommand(key, true));
            EnqueueKeyCommand(new KeyCommand(key, false));
            currentManipulationState = targetState;
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
            yield return new WaitForSeconds(0.01f);
        }
        isProcessingKeyCommands = false;
    }

    // Execute a single key command
    void ExecuteKeyCommand(KeyCommand command)
    {
        var keyboard = InputSystem.GetDevice<Keyboard>();
        if (keyboard == null) return;
        if (command.press)
        {
            // Pressing the key
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(command.key));
        }
        else
        {
            // Releasing the key
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        }
    }

    // Move the controller in the given direction using input simulation
    void MoveControllerInDirection(Vector3 direction)
    {
        float xAxis = direction.x;  // forward/back
        float yAxis = direction.y;  // up/down
        float zAxis = direction.z;  // left/right
        if (xAxis != 0 || yAxis != 0 || zAxis != 0)
        {
            // Normalise to ensure don't exceed 1.0 magnitude
            float magnitude = Mathf.Sqrt(xAxis * xAxis + yAxis * yAxis + zAxis * zAxis);
            xAxis /= magnitude;
            yAxis /= magnitude;
            zAxis /= magnitude;
            // Reduce magnitude to avoid extreme movements
            xAxis *= 0.5f;
            yAxis *= 0.5f;
            zAxis *= 0.5f;
            // Send input events to simulate controller movement
            EnqueueMovementKeys(xAxis, yAxis, zAxis);
            // Debug.Log($"Movement direction: X={xAxis}, Y={yAxis}, Z={zAxis}");
        }
    }

    /// <summary>
    /// Enqueue movement keys based on direction
    /// Greedy approach: move to the direction with largest distance first
    /// Using key commands for movement
    /// </summary>
    void EnqueueMovementKeys(float x, float y, float z)
    {
        var directions = new[] { Mathf.Abs(x), Mathf.Abs(y), Mathf.Abs(z) };
        if (Mathf.Abs(x) == Mathf.Max(directions) && Mathf.Abs(x) > 0.1f)
        {
            Key key = x > 0 ? Key.W : Key.S;
            EnqueueKeyCommand(new KeyCommand(key, true));
            EnqueueKeyCommand(new KeyCommand(key, false));
        }
        if (Mathf.Abs(y) == Mathf.Max(directions) && Mathf.Abs(y) > 0.1f)
        {
            Key key = y > 0 ? Key.E : Key.Q;
            EnqueueKeyCommand(new KeyCommand(key, true));
            EnqueueKeyCommand(new KeyCommand(key, false));
        }
        if (Mathf.Abs(z) == Mathf.Max(directions) && Mathf.Abs(z) > 0.1f)
        {
            Key key = z > 0 ? Key.A : Key.D;
            EnqueueKeyCommand(new KeyCommand(key, true));
            EnqueueKeyCommand(new KeyCommand(key, false));
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
        Debug.Log("Controller reset.");
    }

    void ControllerGripAction()
    {
        Key gripKey = Key.G;
        EnqueueKeyCommand(new KeyCommand(gripKey, true));
        EnqueueKeyCommand(new KeyCommand(gripKey, false));
        Debug.Log("Grip action executed.");
    }

    void ControllerTriggerAction()
    {
        Key triggerKey = Key.T;
        EnqueueKeyCommand(new KeyCommand(triggerKey, true));
        EnqueueKeyCommand(new KeyCommand(triggerKey, false));
        Debug.Log("Trigger action executed.");
    }

    // public void SwitchDeviceState(XRDeviceState state)
    // {
    //     if (deviceState != state)
    //     {
    //         if (state == XRDeviceState.HMD)
    //         {
    //             Utils.SwitchDeviceStateHMD();
    //             deviceState = XRDeviceState.HMD;
    //         }
    //         else if (state == XRDeviceState.LeftController)
    //         {
    //             Utils.SwitchDeviceStateLeftController();
    //             deviceState = XRDeviceState.LeftController;
    //         }
    //         else if (state == XRDeviceState.RightController)
    //         {
    //             Utils.SwitchDeviceStateRightController();
    //             deviceState = XRDeviceState.RightController;
    //         }
    //         Debug.Log("Swtich device state to: " + deviceState);
    //     }
    // }

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
        foreach (KeyValuePair<GameObject, InteractableIdentification.InteractableInfo> entry in interactables) // test with the first interactable
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
                // Debug.Log(entry.Key);
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

    // public class ControllerAction
    // {
    //     private GameObject controller;
    //     private float controllerMovementStep = 1f;
    //     private bool movementCompleted;
    //     private bool interactionCompleted;
    //     // private string controllerType;

    //     public ControllerAction(string controllerType)
    //     {
    //         if (controllerType == "left")
    //         {
    //             controller = GameObject.FindWithTag("LeftController");
    //         }
    //         else if (controllerType == "right")
    //         {
    //             controller = GameObject.FindWithTag("RightController");
    //         }
    //         else
    //         {
    //             Debug.LogError("Please create the controller with a valid type");
    //         }
    //     }

    //     public void GetControllerInstance(string controllerType)
    //     {
    //         if (controllerType == "left")
    //         {
    //             controller = GameObject.FindWithTag("LeftController");
    //         }
    //         else if (controllerType == "right")
    //         {
    //             controller = GameObject.FindWithTag("RightController");
    //         }
    //         else
    //         {
    //             Debug.LogError("Please create the controller with a valid type");
    //         }
    //     }
    //     public void ControllerMovement(string controllerType, Vector3 targetPos)
    //     {
    //         GetControllerInstance(controllerType);
    //         Debug.Log("ControllerMovement--Controller Pose" + controller.transform.position);
    //         Debug.Log("ControllerMovement--Target Pose" + targetPos);
    //         controller.transform.position = Vector3.MoveTowards(
    //             controller.transform.position,
    //             targetPos,
    //             controllerMovementStep * Time.deltaTime
    //         );
    //         // yield return new WaitForSeconds(0.0001f);
    //     }

    //     public void SetMovementCompleted(bool flag)
    //     {
    //         movementCompleted = flag;
    //     }

    //     public bool GetMovementCompleted()
    //     {
    //         return movementCompleted;
    //     }

    // }
}
