using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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
        private static Key grabKey = Key.G;
        private static Key triggerKey = Key.T;
        private static float controllerMovementThreshold = 0.05f; // The distance of controller movement to continue interaction

        // Dictionary to track existing listeners to avoid duplicates
        private static Dictionary<GameObject, InteractionListener> registeredListeners = new Dictionary<GameObject, InteractionListener>();

        public static GameObject FindGameObjectWithName(string name)
        {
            GameObject foundObject = GameObject.Find(name);
            if (foundObject == null)
            {
                Debug.LogError($"GameObject with name '{name}' not found in the scene.");
                return null;
            }

            // Check if listener already exists for this object
            if (registeredListeners.TryGetValue(foundObject, out InteractionListener existingListener))
            {
                Debug.Log($"InteractionListener already exists for '{name}', using existing listener.");
                return foundObject;
            }

            // Register new listener
            InteractionListener newListener = RegisterInteractionListener(foundObject);
            if (newListener != null)
            {
                registeredListeners[foundObject] = newListener;
                Debug.Log($"Automatically registered new InteractionListener for '{name}'.");
            }

            return foundObject;
        }

        // Overload for backward compatibility - returns only the GameObject
        public static GameObject FindGameObjectWithName(string name, bool returnOnlyGameObject = true)
        {
            var result = FindGameObjectWithName(name);
            return result;
        }
        public static IEnumerator NavigateToObject(GameObject player, GameObject target, float moveSpeed = 1.0f, float interactionAngle = 5.0f, float interactionDistance = 1.0f)
        {
            while (true)
            {
                Vector3 currentPos = player.transform.position;
                Vector3 targetPos = target.transform.position;

                // Rotation (only rotate y-axis)
                Vector3 targetDirection = (targetPos - currentPos).normalized;
                targetDirection.y = 0;
                float angle = Vector3.Angle(player.transform.forward, targetDirection);
                if (angle > interactionAngle)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                    player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRotation, moveSpeed * Time.deltaTime);
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
                    player.transform.position = newPosition;
                    yield return null;
                    continue;
                }
                break; // Close enough
            }
        }

        public static IEnumerator MoveControllerToObject(GameObject controller, GameObject target, float moveSpeed = 1.0f, float threshold = 0.05f)
        {
            yield return ExecuteKeyWithDuration(Key.RightBracket, 0.01f);
            while (true)
            {
                Vector3 controllerCurrentPos = controller.transform.position;
                Vector3 controllerTargetPos = target.transform.position;
                float distanceToTarget = Vector3.Distance(controllerCurrentPos, controllerTargetPos);
                if (distanceToTarget <= threshold)
                    break;

                Vector3 controllerWorldDirection = GetControllerWorldDirection(controllerCurrentPos, controllerTargetPos);
                yield return MoveControllerInDirection(controller.transform, controllerWorldDirection.normalized);
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

        public static IEnumerator PressKeys(Key key)
        {
            return PressKeys(new Key[] { key });
        }

        public static IEnumerator PressKey(Key key)
        {
            return PressKeys(new Key[] { key });
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

        public static IEnumerator GrabAndHold(float duration = 1.0f)
        {
            yield return GrabStart();
            yield return new WaitForSeconds(duration);
        }

        public static IEnumerator GrabRelease()
        {
            yield return ReleaseAllKeys();
        }

        // ===== MOVEMENT ACTIONS =====
        public static IEnumerator GrabAndMoveHold(Key movementKey, float duration = 1.0f)
        {
            Key[] composeKey = { grabKey, movementKey };
            yield return PressKeys(composeKey);
            yield return new WaitForSeconds(duration);
        }

        public static IEnumerator MoveHold(Key movementKey, float duration = 1.0f)
        {
            yield return PressKeys(movementKey);
            yield return new WaitForSeconds(duration);
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

        public static void UnregisterInteractionListener(GameObject interactableObject)
        {
            if (registeredListeners.TryGetValue(interactableObject, out InteractionListener listener))
            {
                UnregisterInteractionListener(interactableObject, listener);
                registeredListeners.Remove(interactableObject);
            }
        }

        // ===== ASSERTION HELPERS =====
        public static void AssertGrabbed(GameObject gameObject, string message = "Object was not grabbed")
        {
            if (!registeredListeners.TryGetValue(gameObject, out InteractionListener listener))
            {
                throw new System.Exception($"No InteractionListener found for GameObject '{gameObject.name}'. Make sure to call FindGameObjectWithName first.");
            }
            // AssertGrabbed(listener, message);
            if (!listener.WasGrabbed)
            {
                throw new System.Exception($"{message}. Last interaction: {listener.LastInteractableName} at {listener.LastInteractionTime}");
            }
        }

        public static void AssertTriggered(GameObject gameObject, string message = "Object was not triggered")
        {
            if (!registeredListeners.TryGetValue(gameObject, out InteractionListener listener))
            {
                throw new System.Exception($"No InteractionListener found for GameObject '{gameObject.name}'. Make sure to call FindGameObjectWithName first.");
            }
            // AssertTriggered(listener, message);
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
