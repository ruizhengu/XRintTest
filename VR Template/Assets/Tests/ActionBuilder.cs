using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace XRintTestLib
{
    /// <summary>
    /// ActionBuilder provides a fluent API for chaining XR interaction actions.
    /// Allows for method chaining like: ActionBuilder().GrabHold().MoveUp().Execute()
    /// </summary>
    public class ActionBuilder
    {
        private bool _grabbed = false;
        private bool _triggered = false;
        private readonly List<ActionStep> _actionSteps = new List<ActionStep>();

        public ActionBuilder()
        {
            // Default constructor
        }

        /// <summary>
        /// Represents a single action step in the chain
        /// </summary>
        private class ActionStep
        {
            public Func<IEnumerator> Action { get; set; }
            public string Description { get; set; }
            public float? Duration { get; set; }

            public ActionStep(Func<IEnumerator> action, string description = "", float? duration = null)
            {
                Action = action;
                Duration = duration;
            }
        }

        #region Grab Actions

        /// <summary>
        /// Instant grab action
        /// </summary>
        public ActionBuilder Grab()
        {
            _actionSteps.Add(new ActionStep(
                () => TestLib.Grab(),
                $"Grab"
            ));
            return this;
        }

        /// <summary>
        /// Grab and hold for a duration
        /// </summary>
        public ActionBuilder GrabHold(float duration = 1.0f)
        {
            _actionSteps.Add(new ActionStep(
                () => TestLib.GrabAndHold(duration),
                $"Grab Hold ({duration}s)",
                duration
            ));
            _grabbed = true;
            return this;
        }

        public ActionBuilder ReleaseAllKeys()
        {
            _actionSteps.Add(new ActionStep(
                () => TestLib.ReleaseAllKeys(),
                "Release All Keys"
            ));
            _grabbed = false;
            _triggered = false;
            return this;
        }

        #endregion

        #region Trigger Actions

        /// <summary>
        /// Instant trigger action
        /// </summary>
        public ActionBuilder Trigger()
        {
            if (_grabbed == true)
            {
                _actionSteps.Add(new ActionStep(
                    () => TestLib.GrabHoldAndTrigger(),
                    "Grab Hold and Trigger"
                ));
            }
            else
            {
                _actionSteps.Add(new ActionStep(
                    () => TestLib.Trigger(),
                    "Trigger"
                ));
            }
            return this;
        }

        public ActionBuilder TriggerHold(float duration = 1.0f)
        {
            if (_grabbed == true)
            {
                _actionSteps.Add(new ActionStep(
                    () => TestLib.GrabHoldAndTriggerHold(duration),
                    $"Grab Hold and Trigger Hold ({duration}s)",
                    duration
                ));
            }
            else
            {
                _actionSteps.Add(new ActionStep(
                    () => TestLib.TriggerAndHold(duration),
                    $"Trigger Hold ({duration}s)",
                    duration
                ));
            }
            _triggered = true;
            return this;
        }

        #endregion

        #region Movement Actions

        /// <summary>
        /// Move and hold in a direction
        /// </summary>
        public ActionBuilder MoveHold(Key movementKey, float duration = 1.0f)
        {
            if (_grabbed == true)
            {
                _actionSteps.Add(new ActionStep(
                    () => TestLib.GrabAndMoveHold(movementKey, duration),
                    $"Move Hold {movementKey} ({duration}s)",
                    duration
                ));
            }
            else
            {
                _actionSteps.Add(new ActionStep(
                    () => TestLib.MoveHold(movementKey, duration),
                    $"Move Hold {movementKey} ({duration}s)",
                    duration
                ));
            }
            return this;
        }

        #endregion

        #region Directional Movement/Rotation Helpers

        /// <summary>
        /// Move up (E key)
        /// </summary>
        public ActionBuilder MoveUp(float duration = 1.0f)
        {
            return MoveHold(Key.E, duration);
        }

        /// <summary>
        /// Move down (Q key)
        /// </summary>
        public ActionBuilder MoveDown(float duration = 1.0f)
        {
            return MoveHold(Key.Q, duration);
        }

        /// <summary>
        /// Move forward (W key)
        /// </summary>
        public ActionBuilder MoveForward(float duration = 1.0f)
        {
            return MoveHold(Key.W, duration);
        }

        /// <summary>
        /// Move backward (S key)
        /// </summary>
        public ActionBuilder MoveBackward(float duration = 1.0f)
        {
            return MoveHold(Key.S, duration);
        }

        /// <summary>
        /// Move left (A key)
        /// </summary>
        public ActionBuilder MoveLeft(float duration = 1.0f)
        {
            return MoveHold(Key.A, duration);
        }

        /// <summary>
        /// Move right (D key)
        /// </summary>
        public ActionBuilder MoveRight(float duration = 1.0f)
        {
            return MoveHold(Key.D, duration);
        }

        /// <summary>
        /// Rotate yaw left (Left Arrow key)
        /// </summary>
        public ActionBuilder RotateLeft(float duration = 0.1f)
        {
            return MoveHold(Key.LeftArrow, duration);
        }

        /// <summary>
        /// Rotate yaw right (Right Arrow key)
        /// </summary>
        public ActionBuilder RotateRight(float duration = 0.1f)
        {
            return MoveHold(Key.RightArrow, duration);
        }

        /// <summary>
        /// Rotate pitch up (Up Arrow key)
        /// </summary>
        public ActionBuilder RotateUp(float duration = 0.1f)
        {
            return MoveHold(Key.UpArrow, duration);
        }

        /// <summary>
        /// Rotate pitch down (Down Arrow key)
        /// </summary>
        public ActionBuilder RotateDown(float duration = 0.1f)
        {
            return MoveHold(Key.DownArrow, duration);
        }

        #endregion

        #region Navigation Actions

        /// <summary>
        /// Navigate player to a target object
        /// </summary>
        public ActionBuilder NavigateTo(GameObject player, GameObject target, float moveSpeed = 1.0f, float interactionAngle = 5.0f, float interactionDistance = 1.0f)
        {
            if (_grabbed == true)
            {
                _actionSteps.Add(new ActionStep(
                    () => TestLib.GrabHoldNavigateToObject(player, target),
                    $"Grab Hold and Navigate to {target.name}"
                ));
            }
            else
            {
                _actionSteps.Add(new ActionStep(
                    () => TestLib.NavigateToObject(player, target),
                    $"Navigate to {target.name}"
                ));
            }
            return this;
        }

        /// <summary>
        /// Move controller to a target object
        /// </summary>
        public ActionBuilder MoveControllerTo(GameObject controller, GameObject target)
        {
            if (_grabbed == true)
            {
                _actionSteps.Add(new ActionStep(
                    () => TestLib.MoveControllerToObject(controller, target, true),
                    $"Move controller to {target.name} while holding grab"
                ));
            }
            else
            {
                _actionSteps.Add(new ActionStep(
                    () => TestLib.MoveControllerToObject(controller, target),
                    $"Move controller to {target.name}"
                ));
            }
            return this;
        }

        #endregion

        #region Execution

        /// <summary>
        /// Execute all actions in the chain sequentially
        /// </summary>
        public IEnumerator Execute()
        {
            Debug.Log($"ActionBuilder: Executing {_actionSteps.Count} actions");

            foreach (var step in _actionSteps)
            {
                yield return step.Action();
            }

            Debug.Log("ActionBuilder: All actions completed");
        }


        /// <summary>
        /// Clear all actions from the chain
        /// </summary>
        public ActionBuilder Clear()
        {
            _actionSteps.Clear();
            return this;
        }

        /// <summary>
        /// Get the number of actions in the chain
        /// </summary>
        public int Count => _actionSteps.Count;

        #endregion
    }
}
