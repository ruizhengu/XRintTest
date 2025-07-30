using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace XRintTestLib
{
    public static class TestLib
    {
        private static float controllerMovementThreshold = 0.05f; // The distance of controller movement to continue interaction

        public static GameObject FindGameObjectWithName(string name)
        {
            return GameObject.Find(name);
        }
        public static IEnumerator NavigateToObject(Transform player, Transform target, float moveSpeed = 1.0f, float interactionAngle = 5.0f, float interactionDistance = 1.0f)
        {
            while (true)
            {
                Vector3 currentPos = player.position;
                Vector3 targetPos = target.position;

                // Rotation (only rotate y-axis)
                Vector3 targetDirection = (targetPos - currentPos).normalized;
                targetDirection.y = 0;
                float angle = Vector3.Angle(player.forward, targetDirection);
                if (angle > interactionAngle)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                    player.rotation = Quaternion.Slerp(player.rotation, targetRotation, moveSpeed * Time.deltaTime);
                    yield return null;
                    continue;
                }

                // Player Movement (calculate distance ignoring Y axis)
                Vector3 flatCurrentPos = new Vector3(currentPos.x, 0, currentPos.z);
                Vector3 flatTargetPos = new Vector3(targetPos.x, 0, targetPos.z);
                float distance = Vector3.Distance(flatCurrentPos, flatTargetPos);
                if (distance > interactionDistance)
                {
                    Vector3 newPosition = Vector3.MoveTowards(
                        new Vector3(currentPos.x, currentPos.y, currentPos.z),
                        new Vector3(targetPos.x, currentPos.y, targetPos.z),
                        moveSpeed * Time.deltaTime
                    );
                    player.position = newPosition;
                    yield return null;
                    continue;
                }
                break; // Close enough
            }
        }

        public static IEnumerator MoveControllerToObject(Transform controller, Transform target, float moveSpeed = 1.0f, float threshold = 0.05f)
        {
            yield return ExecuteKeyWithDuration(Key.RightBracket, 0.01f);
            while (true)
            {
                Vector3 controllerCurrentPos = controller.position;
                Vector3 controllerTargetPos = target.position;
                float distanceToTarget = Vector3.Distance(controllerCurrentPos, controllerTargetPos);
                if (distanceToTarget <= threshold)
                    break;

                Vector3 controllerWorldDirection = GetControllerWorldDirection(controllerCurrentPos, controllerTargetPos);
                yield return MoveControllerInDirection(controller, controllerWorldDirection.normalized);
                yield return null; // Wait a frame before next move
            }
        }

        public static IEnumerator ExecuteKeyWithDuration(Key key, float duration)
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

        public static IEnumerator PressKeys(Key[] keys)
        {
            var keyboard = InputSystem.GetDevice<Keyboard>();
            if (keyboard == null) yield break;
            // Press the key and keep it pressed
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            yield return null; // Wait one frame to ensure the key press is registered
        }

        public static IEnumerator PressKey(Key key)
        {
            return PressKeys(new Key[] { key });
        }

        public static IEnumerator ReleaseKey(Key key)
        {
            var keyboard = InputSystem.GetDevice<Keyboard>();
            if (keyboard == null) yield break;
            // Release the key
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null; // Wait one frame to ensure the key release is registered
        }

        public static IEnumerator ReleaseAllKeys()
        {
            var keyboard = InputSystem.GetDevice<Keyboard>();
            if (keyboard == null) yield break;
            // Release all keys
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null; // Wait one frame to ensure the key release is registered
        }

        // ===== GRAB ACTIONS =====
        public static IEnumerator GrabStart()
        {
            yield return PressKey(Key.G);
        }

        public static IEnumerator GrabEnd()
        {
            yield return ReleaseKey(Key.G);
        }

        public static IEnumerator GrabAndHold(float duration = 1.0f)
        {
            yield return GrabStart();
            yield return new WaitForSeconds(duration);
            yield return GrabEnd();
        }

        // ===== MOVEMENT ACTIONS =====
        public static IEnumerator MoveStart(Key movementKey)
        {
            yield return PressKey(movementKey);
        }

        public static IEnumerator MoveEnd(Key movementKey)
        {
            yield return ReleaseKey(movementKey);
        }

        public static IEnumerator MoveAndHold(Key movementKey, float duration = 1.0f)
        {
            yield return MoveStart(movementKey);
            yield return new WaitForSeconds(duration);
            yield return MoveEnd(movementKey);
        }

        // ===== COMBINED ACTIONS =====
        public static IEnumerator GrabAndMove(Key movementKey, float duration = 1.0f)
        {
            // Start both grab and movement simultaneously
            Key[] combinedKeys = { Key.G, movementKey };
            yield return PressKeys(combinedKeys);

            // Hold for specified duration
            yield return new WaitForSeconds(duration);

            // Release both keys
            yield return ReleaseAllKeys();
        }

        public static IEnumerator GrabAndMoveContinuous(Key movementKey, float duration = 1.0f)
        {
            // Start grab first
            yield return GrabStart();

            // Then start movement while grab is held
            yield return MoveStart(movementKey);

            // Hold for specified duration
            yield return new WaitForSeconds(duration);

            // Release movement first, then grab
            yield return MoveEnd(movementKey);
            yield return GrabEnd();
        }

        // ===== DIRECTIONAL MOVEMENT HELPERS =====
        public static IEnumerator GrabAndMoveLeft(float duration = 1.0f)
        {
            yield return GrabAndMove(Key.A, duration);
        }

        public static IEnumerator GrabAndMoveRight(float duration = 1.0f)
        {
            yield return GrabAndMove(Key.D, duration);
        }

        public static IEnumerator GrabAndMoveForward(float duration = 1.0f)
        {
            yield return GrabAndMove(Key.W, duration);
        }

        public static IEnumerator GrabAndMoveBackward(float duration = 1.0f)
        {
            yield return GrabAndMove(Key.S, duration);
        }

        public static IEnumerator GrabAndMoveUp(float duration = 1.0f)
        {
            yield return GrabAndMove(Key.E, duration);
        }

        public static IEnumerator GrabAndMoveDown(float duration = 1.0f)
        {
            yield return GrabAndMove(Key.Q, duration);
        }

        // ===== COMPLEX MOVEMENT PATTERNS =====
        public static IEnumerator GrabAndMovePattern(Key[] movementSequence, float durationPerMove = 0.5f)
        {
            yield return GrabStart();

            foreach (Key movementKey in movementSequence)
            {
                yield return MoveStart(movementKey);
                yield return new WaitForSeconds(durationPerMove);
                yield return MoveEnd(movementKey);
            }

            yield return GrabEnd();
        }

        // public static IEnumerator GrabAndMoveSquare(float durationPerSide = 0.5f)
        // {
        //     Key[] squarePattern = { Key.W, Key.D, Key.S, Key.A }; // Forward, Right, Back, Left
        //     yield return GrabAndMovePattern(squarePattern, durationPerSide);
        // }

        // public static IEnumerator GrabAndMoveCircle(float durationPerMove = 0.3f)
        // {
        //     Key[] circlePattern = { Key.W, Key.E, Key.D, Key.Q, Key.S, Key.Q, Key.A, Key.E }; // Complex circular pattern
        //     yield return GrabAndMovePattern(circlePattern, durationPerMove);
        // }

        // ===== LEGACY COMPATIBILITY =====
        public static IEnumerator ControllerGrabAction()
        {
            yield return GrabStart();
        }

        public static IEnumerator ControllerReleaseGrabAction()
        {
            yield return GrabEnd();
        }

        public static IEnumerator ControllerGrabAndMove(Key movementKey)
        {
            yield return GrabAndMove(movementKey, 1.0f);
        }

        // ===== INTERACTION LISTENER MANAGEMENT =====
        public class InteractionListener
        {
            public bool WasGrabbed { get; private set; } = false;
            public bool WasTriggered { get; private set; } = false;
            public bool WasSelected { get; private set; } = false;
            public bool WasActivated { get; private set; } = false;
            public string LastInteractableName { get; private set; } = "";
            public System.DateTime LastInteractionTime { get; private set; }

            public void Reset()
            {
                WasGrabbed = false;
                WasTriggered = false;
                WasSelected = false;
                WasActivated = false;
                LastInteractableName = "";
                LastInteractionTime = System.DateTime.MinValue;
            }

            public void OnSelectEntered(SelectEnterEventArgs args)
            {
                WasSelected = true;
                WasGrabbed = true;
                LastInteractableName = args.interactableObject.transform.name;
                LastInteractionTime = System.DateTime.Now;
                Debug.Log($"InteractionListener: Object '{LastInteractableName}' was grabbed/selected at {LastInteractionTime}");
            }

            public void OnActivated(ActivateEventArgs args)
            {
                WasActivated = true;
                WasTriggered = true;
                LastInteractableName = args.interactableObject.transform.name;
                LastInteractionTime = System.DateTime.Now;
                Debug.Log($"InteractionListener: Object '{LastInteractableName}' was activated/triggered at {LastInteractionTime}");
            }

            public void OnSelectExited(SelectExitEventArgs args)
            {
                Debug.Log($"InteractionListener: Object '{args.interactableObject.transform.name}' was released at {System.DateTime.Now}");
            }

            public void OnDeactivated(DeactivateEventArgs args)
            {
                Debug.Log($"InteractionListener: Object '{args.interactableObject.transform.name}' was deactivated at {System.DateTime.Now}");
            }
        }

        public static InteractionListener RegisterInteractionListener(GameObject interactableObject)
        {
            var listener = new InteractionListener();
            var xrInteractable = interactableObject.GetComponent<XRBaseInteractable>();

            if (xrInteractable != null)
            {
                xrInteractable.selectEntered.AddListener(listener.OnSelectEntered);
                xrInteractable.selectExited.AddListener(listener.OnSelectExited);
                xrInteractable.activated.AddListener(listener.OnActivated);
                xrInteractable.deactivated.AddListener(listener.OnDeactivated);

                Debug.Log($"InteractionListener registered for '{interactableObject.name}'");
            }
            else
            {
                Debug.LogError($"No XRBaseInteractable component found on '{interactableObject.name}'");
            }

            return listener;
        }

        public static void UnregisterInteractionListener(GameObject interactableObject, InteractionListener listener)
        {
            var xrInteractable = interactableObject.GetComponent<XRBaseInteractable>();

            if (xrInteractable != null && listener != null)
            {
                xrInteractable.selectEntered.RemoveListener(listener.OnSelectEntered);
                xrInteractable.selectExited.RemoveListener(listener.OnSelectExited);
                xrInteractable.activated.RemoveListener(listener.OnActivated);
                xrInteractable.deactivated.RemoveListener(listener.OnDeactivated);

                Debug.Log($"InteractionListener unregistered from '{interactableObject.name}'");
            }
        }

        // public static InteractionListener RegisterInteractionListenerByName(string interactableName)
        // {
        //     var interactableObject = GameObject.Find(interactableName);
        //     if (interactableObject == null)
        //     {
        //         Debug.LogError($"Interactable object '{interactableName}' not found in scene");
        //         return null;
        //     }

        //     return RegisterInteractionListener(interactableObject);
        // }

        // ===== ASSERTION HELPERS =====
        public static void AssertGrabbed(InteractionListener listener, string message = "Object was not grabbed")
        {
            if (!listener.WasGrabbed)
            {
                throw new System.Exception($"{message}. Last interaction: {listener.LastInteractableName} at {listener.LastInteractionTime}");
            }
        }

        public static void AssertTriggered(InteractionListener listener, string message = "Object was not triggered")
        {
            if (!listener.WasTriggered)
            {
                throw new System.Exception($"{message}. Last interaction: {listener.LastInteractableName} at {listener.LastInteractionTime}");
            }
        }

        public static void AssertInteracted(InteractionListener listener, string message = "Object was not interacted with")
        {
            if (!listener.WasGrabbed && !listener.WasTriggered)
            {
                throw new System.Exception($"{message}. Last interaction: {listener.LastInteractableName} at {listener.LastInteractionTime}");
            }
        }

        public static void AssertInteractionWithinTime(InteractionListener listener, float maxWaitTimeSeconds, string message = "Interaction did not occur within expected time")
        {
            var timeSinceInteraction = System.DateTime.Now - listener.LastInteractionTime;
            if (timeSinceInteraction.TotalSeconds > maxWaitTimeSeconds)
            {
                throw new System.Exception($"{message}. Last interaction was {timeSinceInteraction.TotalSeconds:F2} seconds ago");
            }
        }

        public static IEnumerator EnqueueMovementKeys(float x, float y, float z)
        {
            float threshold = controllerMovementThreshold;
            float absX = Mathf.Abs(x);
            float absY = Mathf.Abs(y);
            float absZ = Mathf.Abs(z);
            if (absZ > threshold)
            {
                Key zKey = z > 0 ? Key.W : Key.S;
                yield return ExecuteKeyWithDuration(zKey, 0.01f);
                yield break;
            }
            if (absX > threshold)
            {
                Key xKey = x > 0 ? Key.D : Key.A;
                yield return ExecuteKeyWithDuration(xKey, 0.01f);
                yield break;
            }
            if (absY > threshold)
            {
                Key yKey = y > 0 ? Key.E : Key.Q;
                yield return ExecuteKeyWithDuration(yKey, 0.01f);
                yield break;
            }
            yield break;
        }

        public static IEnumerator ControllerMovementSustained(Key movementKey, Key actionKey, float duration = 1.0f)
        {
            Key[] composeKeys = { movementKey, actionKey };
            yield return PressKeys(composeKeys);
            yield return new WaitForSeconds(duration);
            yield return ReleaseKey(movementKey);
        }

        public static IEnumerator MoveControllerInDirection(Transform controller, Vector3 direction)
        {
            Vector3 controllerForward = controller.forward;
            Vector3 controllerRight = controller.right;
            Vector3 controllerUp = controller.up;
            float zAxis = Vector3.Dot(direction, controllerForward);
            float xAxis = Vector3.Dot(direction, controllerRight);
            float yAxis = Vector3.Dot(direction, controllerUp);
            yield return EnqueueMovementKeys(xAxis, yAxis, zAxis);
        }

        public static Vector3 GetControllerWorldDirection(Vector3 currentPos, Vector3 targetPos)
        {
            Vector3 controllerCurrentViewport = Camera.main.WorldToViewportPoint(currentPos);
            Vector3 controllerTargetViewport = Camera.main.WorldToViewportPoint(targetPos);
            Vector3 viewportDirection = controllerTargetViewport - controllerCurrentViewport;
            Vector3 worldDirection = Camera.main.ViewportToWorldPoint(controllerCurrentViewport + viewportDirection.normalized * Time.deltaTime) - currentPos;
            return worldDirection;
        }
    }
}
