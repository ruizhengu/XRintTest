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
    public class TestPlay01
    {

        GameObject origin;
        GameObject rightController;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Load the scene containing the cube
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);

            // Wait for scene to fully load
            yield return new WaitForSeconds(0.1f);

            origin = GameObject.Find("XR Origin (XR Rig)");
            Assert.IsNotNull(origin, "Origin not found in the scene.");

            rightController = GameObject.Find("Right Controller");
            Assert.IsNotNull(rightController, "Right Controller not found in the scene.");
        }

        [UnityTest]
        public IEnumerator TestGrabCube()
        {
            var cubeObj = FindXRObject("Cube Interactable");
            var cubePosition = cubeObj.transform.position;
            Assert.IsNotNull(cubeObj, "Cube Interactable not found in the scene.");
            // 1. Navigate origin to Cube Interactable
            // yield return NavigateToObject(origin, cubeObj);
            yield return new ActionBuilder()
                    .NavigateTo(origin, cubeObj)
                    .Execute();
            // 2. Move controller to Cube Interactable
            // yield return MoveControllerToObject(rightController, cubeObj);
            yield return new ActionBuilder()
                    .MoveControllerTo(rightController, cubeObj)
                    .Execute();
            // 3. Grab the cube and move it using ActionBuilder pattern
            var action = new ActionBuilder();
            action.GrabHold(1.0f)
                  .MoveUp(0.5f)
                  .ReleaseAllKeys();
            yield return action.Execute();
            AssertGrabbed(cubeObj, "Cube should have been grabbed");
            AssertTranslated(cubeObj, cubePosition, "Cube should have been moved");
            yield return new WaitForSeconds(1.0f);
        }

        [UnityTest]
        public IEnumerator TestTriggerBlaster()
        {
            var blasterObj = FindXRObject("Blaster Variant");
            var blasterRotation = blasterObj.transform.rotation;
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
                  .RotateLeft(0.1f)
                  .Trigger()
                  .ReleaseAllKeys();
            yield return action.Execute();
            AssertGrabbed(blasterObj, "Blaster should have been grabbed");
            AssertTriggered(blasterObj, "Blaster should have been triggered");
            AssertRotated(blasterObj, blasterRotation, "Blaster should have been rotated");
            yield return new WaitForSeconds(1.0f);
        }

        [UnityTest]
        public IEnumerator TestMoveCubeToBlaster()
        {
            var cubeObj = FindXRObject("Cube Interactable");
            var cubePosition = cubeObj.transform.position;
            Assert.IsNotNull(cubeObj, "Cube Interactable not found in the scene.");
            var blasterObj = FindXRObject("Blaster Variant");
            var blasterRotation = blasterObj.transform.rotation;
            Assert.IsNotNull(blasterObj, "Blaster Interactable not found in the scene.");

            // 1. Navigate origin to the cube
            yield return new ActionBuilder()
                    .NavigateTo(origin, cubeObj)
                    .Execute();
            // 2. Move controller to the cube
            yield return new ActionBuilder()
                    .MoveControllerTo(rightController, cubeObj)
                    .Execute();
            // 3. Grab the cube and move it to the blaster
            var action = new ActionBuilder();
            action.GrabHold(1.0f)
                  .NavigateTo(origin, blasterObj)
                  .MoveControllerTo(rightController, blasterObj)
                  .ReleaseAllKeys();
            yield return action.Execute();
            AssertGrabbed(cubeObj, "Cube should have been grabbed");
            AssertTranslated(cubeObj, cubePosition, "Cube should have been moved");
            yield return new WaitForSeconds(1.0f);
        }
    }
}
