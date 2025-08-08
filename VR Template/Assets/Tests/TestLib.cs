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
        private static float moveSpeed = 1.0f;
        private static float interactionAngle = 5.0f;
        private static float interactionDistance = 1.0f;
        // Dictionary to track existing listeners to avoid duplicates
        private static Dictionary<GameObject, InteractionListener> registeredListeners = new Dictionary<GameObject, InteractionListener>();

        public static GameObject FindXRObject(string name, bool returnOnlyGameObject = false)
        {
            GameObject foundObject = GameObject.Find(name);
            if (foundObject == null)
            {
                Debug.LogError($"GameObject with name '{name}' not found in the scene.");
                return null;
            }

            if (returnOnlyGameObject)
            {
                // Only return the GameObject, do not register or check listeners
                return foundObject;
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

        /// <summary>
        /// Navigates the player to the target object, optionally holding the grab key during navigation.
        /// </summary>
        /// <param name="player">The player GameObject to move.</param>
        /// <param name="target">The target GameObject to move towards.</param>
        /// <param name="moveSpeed">Movement speed.</param>
        /// <param name="interactionAngle">Angle threshold for rotation.</param>
        /// <param name="interactionDistance">Distance threshold for stopping.</param>
        /// <param name="holdGrabKey">If true, will press the grab key each frame.</param>
        public static IEnumerator NavigateToObject(
            GameObject player,
            GameObject target,
            bool holdGrabKey = false)
        {
            while (true)
            {
                if (holdGrabKey)
                {
                    yield return PressKey(grabKey);
                }

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

        /// <summary>
        /// Navigates the player to the target object while holding the grab key.
        /// </summary>
        public static IEnumerator GrabHoldNavigateToObject(
            GameObject player,
            GameObject target)
        {
            yield return NavigateToObject(player, target, true);
        }

        public static IEnumerator MoveControllerToObject(GameObject controller, GameObject target, bool holdGrabKey = false)
        {
            if (!holdGrabKey)
            {
                yield return PressKey(Key.RightBracket);

            }
            while (true)
            {
                Vector3 controllerCurrentPos = controller.transform.position;
                Vector3 controllerTargetPos = target.transform.position;
                float distanceToTarget = Vector3.Distance(controllerCurrentPos, controllerTargetPos);
                if (distanceToTarget <= controllerMovementThreshold)
                    break;

                Vector3 controllerWorldDirection = GetControllerWorldDirection(controllerCurrentPos, controllerTargetPos);
                yield return MoveControllerInDirection(controller.transform, controllerWorldDirection.normalized, holdGrabKey);
                yield return null; // Wait a frame before next move
            }
        }

        public static IEnumerator EnqueueMovementKeys(float x, float y, float z, Key[] additionalKeys = null)
        {
            float threshold = controllerMovementThreshold;
            float absX = Mathf.Abs(x);
            float absY = Mathf.Abs(y);
            float absZ = Mathf.Abs(z);

            List<Key> keysToPress = new List<Key>();

            // Add movement keys
            if (absZ > threshold)
            {
                Key zKey = z > 0 ? Key.W : Key.S;
                keysToPress.Add(zKey);
            }
            else if (absX > threshold)
            {
                Key xKey = x > 0 ? Key.D : Key.A;
                keysToPress.Add(xKey);
            }
            else if (absY > threshold)
            {
                Key yKey = y > 0 ? Key.E : Key.Q;
                keysToPress.Add(yKey);
            }

            // Add additional keys (like grab key)
            if (additionalKeys != null)
            {
                keysToPress.AddRange(additionalKeys);
            }

            // Press all keys simultaneously
            if (keysToPress.Count > 0)
            {
                yield return PressKeys(keysToPress.ToArray());
            }
        }

        public static IEnumerator MoveControllerInDirection(Transform controller, Vector3 direction, bool holdGrabKey = false)
        {
            Vector3 controllerForward = controller.forward;
            Vector3 controllerRight = controller.right;
            Vector3 controllerUp = controller.up;
            float zAxis = Vector3.Dot(direction, controllerForward);
            float xAxis = Vector3.Dot(direction, controllerRight);
            float yAxis = Vector3.Dot(direction, controllerUp);

            Key[] additionalKeys = holdGrabKey ? new Key[] { grabKey } : null;
            yield return EnqueueMovementKeys(xAxis, yAxis, zAxis, additionalKeys);
        }

        public static Vector3 GetControllerWorldDirection(Vector3 currentPos, Vector3 targetPos)
        {
            Vector3 controllerCurrentViewport = Camera.main.WorldToViewportPoint(currentPos);
            Vector3 controllerTargetViewport = Camera.main.WorldToViewportPoint(targetPos);
            Vector3 viewportDirection = controllerTargetViewport - controllerCurrentViewport;
            Vector3 worldDirection = Camera.main.ViewportToWorldPoint(controllerCurrentViewport + viewportDirection.normalized * Time.deltaTime) - currentPos;
            return worldDirection;
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
        public static IEnumerator Grab()
        {
            yield return PressKey(Key.G);
        }

        public static IEnumerator GrabAndHold(float duration = 1.0f)
        {
            yield return Grab();
            yield return new WaitForSeconds(duration);
        }

        public static IEnumerator GrabRelease()
        {
            yield return ReleaseAllKeys();
        }

        // ===== TRIGGER ACTIONS =====
        public static IEnumerator Trigger()
        {
            yield return PressKey(Key.T);
        }

        public static IEnumerator TriggerAndHold(float duration = 1.0f)
        {
            yield return Trigger();
            yield return new WaitForSeconds(duration);
        }

        public static IEnumerator TriggerRelease()
        {
            yield return ReleaseAllKeys();
        }

        public static IEnumerator GrabHoldAndTrigger()
        {
            Key[] composeKey = { grabKey, triggerKey };
            yield return PressKeys(composeKey);
        }

        public static IEnumerator GrabHoldAndTriggerHold(float duration = 1.0f)
        {
            yield return GrabHoldAndTrigger();
            yield return new WaitForSeconds(duration);
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
                throw new System.Exception($"No InteractionListener found for GameObject '{gameObject.name}'. Make sure to call FindXRObject first.");
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
                throw new System.Exception($"No InteractionListener found for GameObject '{gameObject.name}'. Make sure to call FindXRObject first.");
            }
            // AssertTriggered(listener, message);
            if (!listener.WasTriggered)
            {
                throw new System.Exception($"{message}. Last interaction: {listener.LastInteractableName} at {listener.LastInteractionTime}");
            }
        }

        public static void AssertRotated(GameObject gameObject, Quaternion rotation, string message = "Object was not rotated")
        {
            if (gameObject.transform.rotation == rotation)
            {
                throw new System.Exception($"{message}. Last interaction: {gameObject.transform.name} at {System.DateTime.Now}");
            }
        }

        public static void AssertTranslated(GameObject gameObject, Vector3 position, string message = "Object was not moved")
        {
            if (gameObject.transform.position == position)
            {
                throw new System.Exception($"{message}. Last interaction: {gameObject.transform.name} at {System.DateTime.Now}");
            }
        }

        public static void AssertInteracted(InteractionListener listener, string message = "Object was not interacted with")
        {
            if (!listener.WasGrabbed && !listener.WasTriggered)
            {
                throw new System.Exception($"{message}. Last interaction: {listener.LastInteractableName} at {listener.LastInteractionTime}");
            }
        }
    }
}
