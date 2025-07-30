using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

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

        public static IEnumerator ControllerGrabAction()
        {
            Key grabKey = Key.G;
            yield return ExecuteKeyWithDuration(grabKey, 0.01f);
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
