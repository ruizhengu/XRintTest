using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

public class TestPlay01
{
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
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.

        // Load the scene containing the cube
        yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);

        // Wait for scene to fully load
        yield return new WaitForSeconds(0.1f);

        var cubeObj = GameObject.Find("Cube Interactable");
        Assert.IsNotNull(cubeObj, "Cube Interactable not found in the scene.");

        var player = GameObject.Find("XR Origin (XR Rig)")?.transform;
        Assert.IsNotNull(player, "Player or Camera not found in the scene.");

        var rightController = GameObject.Find("Right Controller");
        Assert.IsNotNull(rightController, "Right Controller not found in the scene.");

        // 1. Navigate player to Cube Interactable
        yield return NavigateToObject(player, cubeObj.transform);

        yield return MoveControllerToObject(rightController.transform, cubeObj.transform);

        yield return null;
    }

    public IEnumerator NavigateToObject(Transform player, Transform target, float moveSpeed = 1.0f, float interactionAngle = 5.0f, float interactionDistance = 1.0f)
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

    public IEnumerator MoveControllerToObject(Transform controller, Transform target, float moveSpeed = 1.0f, float threshold = 0.05f)
    {
        // TODO: Implement this
    }
}
