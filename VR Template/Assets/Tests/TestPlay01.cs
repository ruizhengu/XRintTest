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
            // 1. Navigate origin to Cube Interactable
            yield return NavigateToObject(origin, cubeObj);
            // 2. Move controller to Cube Interactable
            yield return MoveControllerToObject(rightController, cubeObj);
            // 3. Grab the cube and move it using ActionBuilder pattern
            var action = new ActionBuilder();
            action.GrabHold(1.0f)
                  .MoveUp(0.5f)
                  .GrabRelease();
            yield return action.Execute();
            // Wait 5 seconds to end the test session
            yield return new WaitForSeconds(3.0f);
            AssertGrabbed(cubeObj, "Cube should have been grabbed");
        }
    }
}
