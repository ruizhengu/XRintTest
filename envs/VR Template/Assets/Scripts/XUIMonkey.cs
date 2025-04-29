using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class XUIMonkey : MonoBehaviour
{
    private InteractoBot interactoBot;
    private float actionInterval = 1.0f; // Time between random actions
    private float lastActionTime = 0f;
    private bool isExecutingAction = false;

    // Action types that can be randomly selected
    private enum ActionType
    {
        MoveForward,
        MoveBackward,
        MoveLeft,
        MoveRight,
        MoveUp,
        MoveDown,
        GripAction,
        TriggerAction,
        ResetController
    }

    void Start()
    {
        interactoBot = GetComponent<InteractoBot>();
        if (interactoBot == null)
        {
            Debug.LogError("InteractoBot component not found!");
        }
    }

    void Update()
    {
        if (interactoBot == null) return;

        // Check if it's time to execute a new random action
        if (Time.time - lastActionTime >= actionInterval && !isExecutingAction)
        {
            StartCoroutine(ExecuteRandomAction());
        }
    }

    private IEnumerator ExecuteRandomAction()
    {
        isExecutingAction = true;
        lastActionTime = Time.time;

        // Get a random action type
        ActionType randomAction = (ActionType)Random.Range(0, System.Enum.GetValues(typeof(ActionType)).Length);

        // Execute the selected action
        switch (randomAction)
        {
            case ActionType.MoveForward:
                yield return ExecuteKeyWithDuration(Key.W, 0.1f);
                break;
            case ActionType.MoveBackward:
                yield return ExecuteKeyWithDuration(Key.S, 0.1f);
                break;
            case ActionType.MoveLeft:
                yield return ExecuteKeyWithDuration(Key.A, 0.1f);
                break;
            case ActionType.MoveRight:
                yield return ExecuteKeyWithDuration(Key.D, 0.1f);
                break;
            case ActionType.MoveUp:
                yield return ExecuteKeyWithDuration(Key.E, 0.1f);
                break;
            case ActionType.MoveDown:
                yield return ExecuteKeyWithDuration(Key.Q, 0.1f);
                break;
            case ActionType.GripAction:
                yield return ExecuteKeyWithDuration(Key.G, 0.1f);
                break;
            case ActionType.TriggerAction:
                yield return ExecuteKeyWithDuration(Key.T, 0.1f);
                break;
                // case ActionType.ResetController:
                //     yield return ExecuteKeyWithDuration(Key.R, 0.1f);
                //     break;
        }

        isExecutingAction = false;
    }

    private IEnumerator ExecuteKeyWithDuration(Key key, float duration)
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
}
