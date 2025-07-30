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
        private bool cubeWasGrabbed = false;

        // A Test behaves as an ordinary method
        // [Test]
        // public void TestPlay01SimplePasses()
        // {
        //     // Use the Assert class to test conditions
        // }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator TestPlay01WithEnumeratorPasses()
        {
            // Load the scene containing the cube
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);

            // Wait for scene to fully load
            yield return new WaitForSeconds(0.1f);

            var cubeObj = FindGameObjectWithName("Cube Interactable");
            Assert.IsNotNull(cubeObj, "Cube Interactable not found in the scene.");

            var xrBaseInteractable = cubeObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
            Assert.IsNotNull(xrBaseInteractable, "XRBaseInteractable not found on Cube Interactable.");

            // Register the listener
            xrBaseInteractable.selectEntered.AddListener((args) =>
            {
                Debug.Log("Cube OnSelectEntered triggered!");
                cubeWasGrabbed = true;
            });

            var player = GameObject.Find("XR Origin (XR Rig)");
            Assert.IsNotNull(player, "Player or Camera not found in the scene.");

            var rightController = GameObject.Find("Right Controller");
            Assert.IsNotNull(rightController, "Right Controller not found in the scene.");

            // 1. Navigate player to Cube Interactable
            yield return NavigateToObject(player.transform, cubeObj.transform);

            // 2. Move controller to Cube Interactable
            yield return MoveControllerToObject(rightController.transform, cubeObj.transform);

            // 3. Grab the cube
            yield return ControllerGrabAction();

            Assert.IsTrue(cubeWasGrabbed, "Cube's OnSelectEntered was not triggered.");

            yield return null;
        }
    }
}
