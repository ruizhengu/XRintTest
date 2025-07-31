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
        Key grabKey = Key.G;
        Key triggerKey = Key.T;
        private bool _grabbed = false;
        private bool _triggered = false;
        private readonly List<ActionStep> _actionSteps = new List<ActionStep>();
        // private MonoBehaviour _coroutineRunner;

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
            public float? Duration { get; set; }

            public ActionStep(Func<IEnumerator> action, float? duration = null)
            {
                Action = action;
                Duration = duration;
            }
        }

        #region Grab Actions

        /// <summary>
        /// Start grabbing (press grab key)
        /// </summary>
        public ActionBuilder GrabStart()
        {
            _actionSteps.Add(new ActionStep(
                () => TestLib.GrabStart(),
                "Grab Start"
            ));
            return this;
        }

        /// <summary>
        /// End grabbing (release grab key)
        /// </summary>
        public ActionBuilder GrabEnd()
        {
            _actionSteps.Add(new ActionStep(
                () => TestLib.GrabEnd(),
                "Grab End"
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

        public ActionBuilder GrabRelease()
        {
            _actionSteps.Add(new ActionStep(
                () => TestLib.GrabRelease(),
                "Grab Release"
            ));
            _grabbed = false;
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
                // If grabbed, use PressKeys to hold both grab and movement keys
                _actionSteps.Add(new ActionStep(
                    () => TestLib.PressKeys(new Key[] { grabKey, movementKey }),
                    $"Move Hold {movementKey} while grabbed ({duration}s)",
                    duration
                ));

                // Add a wait action
                _actionSteps.Add(new ActionStep(
                    () => WaitForSeconds(duration),
                    $"Wait while holding {movementKey}",
                    duration
                ));

                // Release all keys
                _actionSteps.Add(new ActionStep(
                    () => TestLib.ReleaseAllKeys(),
                    $"Release all keys"
                ));
            }
            else
            {
                // If not grabbed, use normal MoveAndHold
                // TODO: support both list Key[] and single Key
                _actionSteps.Add(new ActionStep(
                    () => TestLib.MoveAndHold(new Key[] { movementKey }, duration),
                    $"Move Hold {movementKey} ({duration}s)",
                    duration
                ));
            }
            return this;
        }

        #endregion

        #region Directional Movement Helpers

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

        #region Helper Methods

        private IEnumerator WaitForSeconds(float duration)
        {
            yield return new WaitForSeconds(duration);
        }

        #endregion
    }
}
