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
using UnityEngine.XR.Content.Interaction;
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
            var blaster = FindXRObject("Blaster Variant");
            Assert.IsNotNull(blaster, "Blaster Interactable not found in the scene.");
            // 1. Navigate origin to Blaster Interactable
            yield return new ActionBuilder()
                    .NavigateTo(origin, blaster)
                    .Execute();
            // 2. Move controller to Cube Interactable
            yield return new ActionBuilder()
                    .MoveControllerTo(rightController, blaster)
                    .Execute();
            // 3. Grab the blaster and trigger it using ActionBuilder pattern
            var action = new ActionBuilder();
            action.GrabHold(1.0f)
                  .Trigger()
                  .ReleaseAllKeys();
            yield return action.Execute();
            AssertGrabbed(blaster, "Blaster should have been grabbed");
            AssertTriggered(blaster, "Blaster should have been triggered");
            yield return new WaitForSeconds(1.0f);
        }

        [UnityTest]
        public IEnumerator TestSocket()
        {
            var socketSrc = FindXRObject("SimpleSocketShape");
            var socketTag = FindXRObject("SimpleSocket", true);
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

        [UnityTest]
        public IEnumerator TestSlider()
        {
            var slider = FindXRObject("Slider Variant", true);
            Assert.IsNotNull(slider, "Slider not found in the scene.");

            // Get the XRSlider component
            var xrSlider = slider.GetComponent<XRSlider>();
            Assert.IsNotNull(xrSlider, "XRSlider component not found on the slider object.");

            // Store the initial value
            float initialValue = xrSlider.value;
            Debug.Log($"Initial slider value: {initialValue}");

            // 1. Navigate origin to Slider Interactable
            yield return new ActionBuilder()
                    .NavigateTo(origin, slider)
                    .Execute();
            // 2. Move controller to Slider Interactable
            yield return new ActionBuilder()
                    .MoveControllerTo(rightController, slider)
                    .Execute();
            // 3. Grab the slider and move it using ActionBuilder pattern
            var action = new ActionBuilder();
            action.GrabHold(1.0f)
                  .MoveLeft(0.5f)
                  .ReleaseAllKeys();
            yield return action.Execute();

            // Wait a bit for the slider to settle
            yield return new WaitForSeconds(0.5f);
            // Check the final value
            float finalValue = xrSlider.value;
            Debug.Log($"Final slider value: {finalValue}");
            // Assert that the value has changed (optional - you can modify this based on your needs)
            Assert.AreNotEqual(initialValue, finalValue, "Slider value should have changed after interaction");
            yield return new WaitForSeconds(1.0f);
        }

        [UnityTest]
        public IEnumerator TestLever()
        {
            var lever = FindXRObject("Lever Variant", true);
            Assert.IsNotNull(lever, "Lever not found in the scene.");

            var xrLever = lever.GetComponent<XRLever>();
            Assert.IsNotNull(xrLever, "XRLever component not found on the slider object.");

            bool initialValue = xrLever.value;
            Debug.Log($"Initial lever value: {initialValue}");

            yield return new ActionBuilder()
                    .NavigateTo(origin, lever)
                    .Execute();
            yield return new ActionBuilder()
                    .MoveControllerTo(rightController, lever)
                    .Execute();
            var action = new ActionBuilder();
            action.GrabHold(1.0f)
                  .MoveLeft(0.5f)
                  .ReleaseAllKeys();
            yield return action.Execute();

            yield return new WaitForSeconds(0.5f);
            bool finalValue = xrLever.value;
            Debug.Log($"Final lever value: {finalValue}");
            Assert.AreNotEqual(initialValue, finalValue, "Lever value should have changed after interaction");
            yield return new WaitForSeconds(1.0f);
        }

        [UnityTest]
        public IEnumerator TestGripButton()
        {
            var button = FindXRObject("GripButton");
            Assert.IsNotNull(button, "GripButton not found in the scene.");

            yield return new ActionBuilder()
                    .NavigateTo(origin, button)
                    .Execute();
            yield return new ActionBuilder()
                    .MoveControllerTo(rightController, button)
                    .Execute();
            var action = new ActionBuilder();
            action.GrabHold(1.0f)
                  .ReleaseAllKeys();
            yield return action.Execute();

            yield return new WaitForSeconds(0.5f);
            AssertGrabbed(button, "GripButton should have been grabbed");
            yield return new WaitForSeconds(1.0f);
        }

        [UnityTest]
        public IEnumerator TestDial()
        {
            var dial = FindXRObject("Dial Variant");
            Assert.IsNotNull(dial, "Dial not found in the scene.");

            var xrDial = dial.GetComponent<XRKnob>();
            Assert.IsNotNull(xrDial, "XRKnob component not found on the slider object.");
            float initialValue = xrDial.value;

            yield return new ActionBuilder()
                    .NavigateTo(origin, dial)
                    .Execute();
            yield return new ActionBuilder()
                    .MoveControllerTo(rightController, dial)
                    .Execute();
            var action = new ActionBuilder();
            action.GrabHold(1.0f)
                  .MoveLeft(0.5f)
                  .MoveBackward(0.5f)
                  .ReleaseAllKeys();
            yield return action.Execute();

            yield return new WaitForSeconds(0.5f);
            float finalValue = xrDial.value;
            Debug.Log($"Final dial value: {finalValue}");
            Assert.AreNotEqual(initialValue, finalValue, "Dial value should have changed after interaction");
            yield return new WaitForSeconds(1.0f);
        }

        [UnityTest]
        public IEnumerator TestDoor()
        {
            var door = FindXRObject("DoorLocked", true);
            Assert.IsNotNull(door, "Door not found in the scene.");
            var doorHandle = FindXRObject("DoorHandle Point", true);
            Assert.IsNotNull(door, "DoorHandle not found in the scene.");

            var doorRot = door.transform.rotation;

            yield return new ActionBuilder()
                    .NavigateTo(origin, doorHandle)
                    .Execute();
            yield return new ActionBuilder()
                    .MoveControllerTo(rightController, doorHandle)
                    .Execute();
            var action = new ActionBuilder();
            action.GrabHold(1.0f)
                  .MoveDown(0.5f)
                  .MoveRight(0.5f)
                  .MoveForward(0.5f)
                  .ReleaseAllKeys();
            yield return action.Execute();

            yield return new WaitForSeconds(0.5f);
            AssertRotated(door, doorRot, "Door should have been rotated");
            yield return new WaitForSeconds(1.0f);
        }

        [UnityTest]
        public IEnumerator TestDrawer()
        {
            var drawerHandle = FindXRObject("Drawer 01 Attach", true);
            Assert.IsNotNull(drawerHandle, "Drawer Handle not found in the scene.");
            var drawerPos = drawerHandle.transform.position;

            yield return new ActionBuilder()
                    .NavigateTo(origin, drawerHandle)
                    .Execute();
            yield return new ActionBuilder()
                    .MoveControllerTo(rightController, drawerHandle)
                    .Execute();
            var action = new ActionBuilder();
            action.GrabHold(1.0f)
                  .MoveLeft(1.0f)
                  .ReleaseAllKeys();
            yield return action.Execute();

            yield return new WaitForSeconds(0.5f);
            AssertTranslated(drawerHandle, drawerPos, "Drawer should have been moved");
            yield return new WaitForSeconds(1.0f);
        }
    }
}
