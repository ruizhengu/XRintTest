using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
public class SceneExplore
{
    private float moveStep = 0.5f;
    private float turnStep = 20f;
    private Vector3 botPos;
    private Quaternion botRot;
    private Vector3 destPos;
    private Quaternion destRot;
    private Vector3 moveUpperBound = new Vector3(7f, 4.4f, 11f);
    private Vector3 moveLowerBound = new Vector3(-14f, 4.3f, -1f);
    private Vector3 turnUpperBound = new Vector3(60f, 180f, 0f);
    private Vector3 turnLowerBound = new Vector3(-60f, -180f, 0f);
    // public InteractableIdentification interactableIdentification;
    private Vector3[] moveDirections = {
        new (1f, 0f, 0f),
        new (-1f, 0f, 0f),
        new (0f, 1f, 0f),
        new (0f, -1f, 0f),
        new (0f, 0f, 1f),
        new (0f, 0f, -1f)
    };
    private Vector3[] turnDirections = {
        new Vector3(1f, 0f, 0f),
        new Vector3(-1f, 0f, 0f),
        new Vector3(0f, 1f, 0f),
        new Vector3(0f, -1f, 0f),
        new Vector3(0f, 0f, 1f),
        new Vector3(0f, 0f, -1f)
    };
    private readonly float targetPositionOffset = 1.0f;
    private readonly Queue<Vector3> visitedDest = new Queue<Vector3>();
    private readonly int visitedMemory = 10;


    public SceneExplore(Transform initTrans)
    {
        botPos = initTrans.position;
        destPos = initTrans.position;
        botRot = initTrans.rotation;
        destRot = initTrans.rotation;
    }

    public Vector3 RandomExploration()
    {
        botPos = Vector3.MoveTowards(
            botPos,
            destPos,
            moveStep * Time.deltaTime
        );
        // Update destination when reaching target
        if (botPos == destPos)
        {
            destPos = GetRandomDestination();
        }
        return botPos;
    }

    public (Vector3 position, Quaternion rotation) GreedyExploration(GameObject go)
    {
        destRot = go.transform.rotation;
        // TODO consider occlusion
        botPos = Vector3.MoveTowards(
            botPos,
            destPos,
            moveStep * Time.deltaTime
        );
        // botRot = Quaternion.RotateTowards(
        //     botRot,
        //     destRot,
        //     turnStep * Time.deltaTime
        // );
        // Update destination when reaching target
        if (botPos == destPos)
        {
            destPos = GetGODestination(go);
        }
        return (botPos, botRot);
    }

    IEnumerator MoveAndRotate(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            botPos = Vector3.Lerp(botPos, destPos, elapsed / duration);
            botRot = Quaternion.Slerp(botRot, destRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        botPos = destPos;
        botRot = destRot;
    }

    // public GameObject getCloestInteractable()
    // {
    //     GameObject closest = null;
    //     float minDistance = Mathf.Infinity;
    //     foreach (KeyValuePair<GameObject, InteractableIdentification.InteractableInfo> entry in interactableIdentification.getInteractables())
    //     {
    //         var interactableInfo = entry.Value;
    //         if (!interactableInfo.GetInteractFlag())
    //         {
    //             GameObject obj = interactableInfo.GetObject();
    //             float distance = Vector3.Distance(botPos, obj.transform.position);
    //             if (distance < minDistance)
    //             {
    //                 minDistance = distance;
    //                 closest = obj;
    //             }
    //         }
    //     }
    //     return closest;
    // }

    private bool CheckVisited(Vector3 dest)
    {
        foreach (Vector3 v in visitedDest)
        {
            if (v == dest)
            {
                return true;
            }
        }
        return false;
    }

    private void UpdateVisited(Vector3 dest)
    {
        visitedDest.Enqueue(dest);
        if (visitedDest.Count > visitedMemory)
        {
            visitedDest.Dequeue();
        }
    }
    /// <summary>
    /// Get a valid movement considering the boundary and occlusion (via raycast)
    /// </summary>
    /// <param name="position">Target position of the movement</param>
    /// <param name="direction">Target direction of the movement</param>
    /// <returns>If the movement is valid or not</returns>
    private bool IsMoveValid(Vector3 position, Vector3 direction)
    {
        return (direction.x == 0 || position.x + direction.x * moveStep >= moveLowerBound.x && position.x + direction.x * moveStep <= moveUpperBound.x) &&
               (direction.y == 0 || position.y + direction.y * moveStep >= moveLowerBound.y && position.y + direction.y * moveStep <= moveUpperBound.y) &&
               (direction.z == 0 || position.z + direction.z * moveStep >= moveLowerBound.z && position.z + direction.z * moveStep <= moveUpperBound.z) &&
               !Physics.Raycast(position, direction, moveStep);
    }

    private bool IsTurnValid(Vector3 rotation, Vector3 direction)
    {
        return (direction.x == 0 || (rotation.x + direction.x * turnStep >= turnLowerBound.x && rotation.x + direction.x * turnStep <= turnUpperBound.x)) &&
               (direction.y == 0 || (rotation.y + direction.y * turnStep >= turnLowerBound.y && rotation.y + direction.y * turnStep <= turnUpperBound.y)) &&
               (direction.z == 0 || (rotation.z + direction.z * turnStep >= turnLowerBound.z && rotation.z + direction.z * turnStep <= turnUpperBound.z));
    }

    private Vector3 GetRandomDestination()
    {
        var validMoves = GetValidMoves();
        if (validMoves.Count > 0)
        {
            return destPos + validMoves[Random.Range(0, validMoves.Count)] * moveStep;
        }
        return destPos;
    }

    private List<Vector3> GetValidMoves()
    {
        var validMoves = new List<Vector3>();
        for (int i = 0; i < moveDirections.Length; i++)
        {
            if (IsMoveValid(destPos, moveDirections[i]))
            {
                validMoves.Add(moveDirections[i]);
            }
        }
        return validMoves;
    }

    /// <summary>
    /// Get an offsetted position of the target game object.
    /// Avoid get into the same position of the game object, which may disable further interaction
    /// </summary>
    /// <param name="go"></param>
    /// <returns></returns>
    private Vector3 GameObjectOffset(GameObject go)
    {
        Vector3 targetForward = go.transform.forward;
        targetForward.y = 0f;
        targetForward.Normalize();
        return new Vector3(
            go.transform.position.x - targetForward.x * targetPositionOffset,
            botPos.y,
            go.transform.position.z - targetForward.z * targetPositionOffset
        );
    }

    /// <summary>
    /// Get the desitnation based on the target game object
    /// Consider both offset and occlusion
    /// </summary>
    /// <param name="go"></param>
    /// <returns></returns>
    private Vector3 GetGODestination(GameObject go)
    {
        // Vector3 targetPos = go.transform.position;
        Vector3 targetPos = GameObjectOffset(go);
        var validMoves = GetValidMoves();
        if (validMoves.Count == 0)
        {
            return destPos;
        }
        // Get the best position for the next movement
        Vector3 bestMove = Vector3.zero;
        float bestDistance = Mathf.Infinity;
        foreach (Vector3 move in validMoves)
        {
            // If the target movement desitation is visited, try the next one
            Vector3 moveCandidate = destPos + move * moveStep;
            if (CheckVisited(moveCandidate))
            {
                continue;
            }
            float distance = Vector3.Distance(moveCandidate, targetPos);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestMove = move;
            }
        }
        if (bestMove != Vector3.zero)
        {
            Vector3 dest = destPos + bestMove * moveStep;
            // Debug.Log("Target Go: " + go.name + " GO Position: " + targetPos + " Dest Position: " + dest);
            UpdateVisited(dest);
            return dest;
        }
        else
        {
            System.Random rnd = new System.Random();
            int n = rnd.Next(0, validMoves.Count);
            return destPos + validMoves[n] * moveStep;
        }
    }
}


