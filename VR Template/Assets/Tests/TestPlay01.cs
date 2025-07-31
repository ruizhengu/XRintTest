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
        public IEnumerator TestPlay01WithEnumeratorPasses()
        {
            var cubeObj = FindGameObjectWithName("Cube Interactable");
            Assert.IsNotNull(cubeObj, "Cube Interactable not found in the scene.");
            // TODO: integrate teh listener registration in finding object, and the assertion could just use the object

            // Register interaction listener using the reusable API
            var cubeListener = RegisterInteractionListener(cubeObj);
            Assert.IsNotNull(cubeListener, "Failed to register interaction listener for cube.");

            // 1. Navigate origin to Cube Interactable
            yield return NavigateToObject(origin.transform, cubeObj.transform);

            // 2. Move controller to Cube Interactable
            yield return MoveControllerToObject(rightController.transform, cubeObj.transform);

            // 3. Grab the cube and move it using ActionBuilder pattern
            Debug.Log("Attempting to grab and move the cube using ActionBuilder...");

            // Example of the fluent API chain: GrabHold().MoveUp()
            var action = new ActionBuilder();
            action.GrabHold(1.0f)
                  .MoveUp(0.5f)
                  .GrabRelease();

            yield return action.Execute();

            // Wait 5 seconds to end the test session
            yield return new WaitForSeconds(3.0f);

            AssertGrabbed(cubeListener, "Cube should have been grabbed");
            UnregisterInteractionListener(cubeObj, cubeListener);
        }
    }
}
