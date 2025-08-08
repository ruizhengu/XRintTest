using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using static XRintTestLib.TestLib;

namespace XRintTestLib
{
    public class TestEvaluation
    {
        GameObject origin;
        GameObject rightController;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Load the scene containing the cube
            yield return SceneManager.LoadSceneAsync("EvaluationScene", LoadSceneMode.Single);

            // Wait for scene to fully load
            yield return new WaitForSeconds(0.1f);

            origin = GameObject.Find("XR Origin (XR Rig)");
            Assert.IsNotNull(origin, "Origin not found in the scene.");

            rightController = GameObject.Find("Right Controller");
            Assert.IsNotNull(rightController, "Right Controller not found in the scene.");
        }

        [UnityTest]
        public IEnumerator TestBlaster()
        {
            var blasterObj = FindXRObject("Blaster Variant");
            Assert.IsNotNull(blasterObj, "Blaster Interactable not found in the scene.");
            // 1. Navigate origin to Blaster Interactable
            yield return new ActionBuilder()
                    .NavigateTo(origin, blasterObj)
                    .Execute();
            // 2. Move controller to Cube Interactable
            yield return new ActionBuilder()
                    .MoveControllerTo(rightController, blasterObj)
                    .Execute();
            // 3. Grab the blaster and trigger it using ActionBuilder pattern
            var action = new ActionBuilder();
            action.GrabHold(1.0f)
                  .Trigger()
                  .ReleaseAllKeys();
            yield return action.Execute();
            AssertGrabbed(blasterObj, "Blaster should have been grabbed");
            AssertTriggered(blasterObj, "Blaster should have been triggered");
            yield return new WaitForSeconds(1.0f);
        }
        [UnityTest]
        public IEnumerator TestSocket()
        {
            var socketSrc = FindXRObject("SimpleSocketShape");
            var socketTag = FindObject("SimpleSocket");
            Assert.IsNotNull(socketSrc, "SimpleSocketShape not found in the scene.");
            Assert.IsNotNull(socketTag, "SimpleSocket not found in the scene.");
            // 1. Navigate origin to Blaster Interactable
            yield return new ActionBuilder()
                    .NavigateTo(origin, socketSrc)
                    .Execute();
            // 2. Move controller to Cube Interactable
            yield return new ActionBuilder()
                    .MoveControllerTo(rightController, socketSrc)
                    .Execute();
            // 3. Grab the blaster and trigger it using ActionBuilder pattern
            var action = new ActionBuilder();
            action.GrabHold(1.0f)
                  .MoveControllerTo(rightController, socketTag)
                  .ReleaseAllKeys();
            yield return action.Execute();
            AssertGrabbed(socketSrc, "SimpleSocketShape should have been grabbed");
            yield return new WaitForSeconds(1.0f);
        }
    }
}
